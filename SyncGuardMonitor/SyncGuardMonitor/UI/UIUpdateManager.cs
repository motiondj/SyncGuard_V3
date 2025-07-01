using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
using SyncGuardMonitor.Models;
using SyncGuardMonitor.Services;

namespace SyncGuardMonitor.UI
{
    /// <summary>
    /// UI 업데이트 중앙 관리자
    /// </summary>
    public class UIUpdateManager
    {
        private readonly DataGridView clientGrid;
        private readonly RichTextBox logBox;
        private readonly ToolStripStatusLabel statusLabel;
        private readonly Label statsLabel;
        private readonly System.Windows.Forms.Timer refreshTimer;
        
        // UI 설정
        private readonly object uiLock = new object();
        private readonly Queue<LogEntry> logQueue = new Queue<LogEntry>();
        private readonly Dictionary<string, DataGridViewRow> rowCache = new();
        private readonly Dictionary<SyncState, Bitmap> iconCache = new();
        
        // 통계
        private int uiUpdateCount = 0;
        private DateTime lastUpdateTime = DateTime.Now;
        
        public UIUpdateManager(
            DataGridView grid, 
            RichTextBox log, 
            ToolStripStatusLabel status,
            Label stats)
        {
            clientGrid = grid;
            logBox = log;
            statusLabel = status;
            statsLabel = stats;
            
            InitializeGrid();
            InitializeIcons();
            
            // UI 새로고침 타이머 (100ms)
            refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 100
            };
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
        }
        
        /// <summary>
        /// 그리드 초기화
        /// </summary>
        private void InitializeGrid()
        {
            clientGrid.AutoGenerateColumns = false;
            clientGrid.AllowUserToAddRows = false;
            clientGrid.AllowUserToDeleteRows = false;
            clientGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            clientGrid.MultiSelect = false;
            clientGrid.RowHeadersVisible = false;
            clientGrid.BackgroundColor = Color.White;
            clientGrid.BorderStyle = BorderStyle.None;
            clientGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            clientGrid.GridColor = Color.LightGray;
            clientGrid.DefaultCellStyle.SelectionBackColor = Color.LightBlue;
            clientGrid.DefaultCellStyle.SelectionForeColor = Color.Black;
            
            // 컬럼 정의
            DataGridViewColumn[] columns = new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn
                {
                    Name = "colIP",
                    HeaderText = "IP 주소",
                    Width = 120,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colName",
                    HeaderText = "이름",
                    Width = 150,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colState",
                    HeaderText = "상태",
                    Width = 80,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colLastReceived",
                    HeaderText = "마지막 수신",
                    Width = 150,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colDuration",
                    HeaderText = "지속시간",
                    Width = 100,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colMessages",
                    HeaderText = "메시지",
                    Width = 80,
                    ReadOnly = true,
                    DefaultCellStyle = new DataGridViewCellStyle 
                    { 
                        Alignment = DataGridViewContentAlignment.MiddleRight 
                    }
                },
                new DataGridViewImageColumn
                {
                    Name = "colStatus",
                    HeaderText = "●",
                    Width = 30,
                    ImageLayout = DataGridViewImageCellLayout.Zoom
                }
            };
            
            clientGrid.Columns.AddRange(columns);
            
            // 더블 버퍼링 활성화
            EnableDoubleBuffering(clientGrid);
        }
        
        /// <summary>
        /// 아이콘 초기화
        /// </summary>
        private void InitializeIcons()
        {
            iconCache[SyncState.Master] = CreateStatusIcon(Color.Green);
            iconCache[SyncState.Slave] = CreateStatusIcon(Color.Orange);
            iconCache[SyncState.Error] = CreateStatusIcon(Color.Red);
            iconCache[SyncState.Unknown] = CreateStatusIcon(Color.Gray);
        }
        
        /// <summary>
        /// 클라이언트 업데이트
        /// </summary>
        public void UpdateClient(ClientInfo client)
        {
            if (clientGrid.InvokeRequired)
            {
                clientGrid.BeginInvoke(new Action(() => UpdateClient(client)));
                return;
            }
            
            lock (uiLock)
            {
                try
                {
                    DataGridViewRow row;
                    
                    // 캐시에서 행 찾기
                    if (!rowCache.TryGetValue(client.IpAddress, out row))
                    {
                        // 새 행 생성
                        int index = clientGrid.Rows.Add();
                        row = clientGrid.Rows[index];
                        rowCache[client.IpAddress] = row;
                        
                        // 추가 애니메이션
                        AnimateNewRow(row);
                    }
                    
                    // 데이터 업데이트
                    row.Cells["colIP"].Value = client.IpAddress;
                    row.Cells["colName"].Value = client.DisplayName;
                    row.Cells["colState"].Value = client.CurrentState.ToDisplayString();
                    row.Cells["colLastReceived"].Value = client.LastReceived.ToString("HH:mm:ss");
                    row.Cells["colDuration"].Value = FormatDuration(client.StateDuration);
                    row.Cells["colMessages"].Value = client.TotalMessages.ToString("N0");
                    row.Cells["colStatus"].Value = iconCache[client.CurrentState];
                    
                    // 행 스타일 업데이트
                    UpdateRowStyle(row, client);
                    
                    // 상태 변경 애니메이션
                    if (client.PreviousState != client.CurrentState && 
                        client.PreviousState != SyncState.Unknown)
                    {
                        AnimateStateChange(row);
                    }
                    
                    uiUpdateCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UI 업데이트 오류: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 행 스타일 업데이트
        /// </summary>
        private void UpdateRowStyle(DataGridViewRow row, ClientInfo client)
        {
            // 배경색
            Color backColor = client.CurrentState switch
            {
                SyncState.Master => Color.FromArgb(240, 255, 240),  // 연한 초록
                SyncState.Slave => Color.FromArgb(255, 250, 240),   // 연한 주황
                SyncState.Error => Color.FromArgb(255, 240, 240),   // 연한 빨강
                _ => Color.White
            };
            
            // 비활성 상태
            if (!client.IsActive)
            {
                backColor = Color.FromArgb(245, 245, 245);  // 연한 회색
                row.DefaultCellStyle.ForeColor = Color.Gray;
                row.DefaultCellStyle.Font = new Font(clientGrid.Font, FontStyle.Italic);
            }
            else
            {
                row.DefaultCellStyle.ForeColor = Color.Black;
                row.DefaultCellStyle.Font = clientGrid.Font;
            }
            
            foreach (DataGridViewCell cell in row.Cells)
            {
                cell.Style.BackColor = backColor;
            }
        }
        
        /// <summary>
        /// 로그 추가
        /// </summary>
        public void AddLog(string message, LogLevel level = LogLevel.Info, string? category = null)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = message,
                Level = level,
                Category = category ?? "System"
            };
            
            lock (logQueue)
            {
                logQueue.Enqueue(entry);
                
                // 큐 크기 제한
                while (logQueue.Count > 100)
                {
                    logQueue.Dequeue();
                }
            }
        }
        
        /// <summary>
        /// 상태바 업데이트
        /// </summary>
        public void UpdateStatus(string message)
        {
            if (statusLabel.GetCurrentParent()?.InvokeRequired == true)
            {
                statusLabel.GetCurrentParent().BeginInvoke(
                    new Action(() => UpdateStatus(message)));
                return;
            }
            
            statusLabel.Text = message;
        }
        
        /// <summary>
        /// 통계 레이블 업데이트
        /// </summary>
        public void UpdateStatistics(DataStatistics stats)
        {
            if (statsLabel.InvokeRequired)
            {
                statsLabel.BeginInvoke(new Action(() => UpdateStatistics(stats)));
                return;
            }
            
            statsLabel.Text = $"클라이언트: {stats.ActiveClients}/{stats.TotalClients} | " +
                             $"메시지: {stats.TotalMessages:N0} ({stats.MessagesPerSecond:F1}/s) | " +
                             $"Master: {stats.StateDistribution.GetValueOrDefault(SyncState.Master)} | " +
                             $"Slave: {stats.StateDistribution.GetValueOrDefault(SyncState.Slave)} | " +
                             $"Error: {stats.StateDistribution.GetValueOrDefault(SyncState.Error)}";
        }
        
        /// <summary>
        /// 타이머 틱 이벤트
        /// </summary>
        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            // 로그 처리
            ProcessLogQueue();
            
            // 지속시간 업데이트
            UpdateDurations();
            
            // 성능 모니터링
            if ((DateTime.Now - lastUpdateTime).TotalSeconds >= 1)
            {
                var ups = uiUpdateCount;
                uiUpdateCount = 0;
                lastUpdateTime = DateTime.Now;
                
                UpdateStatus($"준비 | UI 업데이트: {ups}/s");
            }
        }
        
        /// <summary>
        /// 로그 큐 처리
        /// </summary>
        private void ProcessLogQueue()
        {
            lock (logQueue)
            {
                while (logQueue.Count > 0)
                {
                    var entry = logQueue.Dequeue();
                    AppendLogEntry(entry);
                }
            }
        }
        
        /// <summary>
        /// 로그 엔트리 추가
        /// </summary>
        private void AppendLogEntry(LogEntry entry)
        {
            if (logBox.InvokeRequired)
            {
                logBox.BeginInvoke(new Action(() => AppendLogEntry(entry)));
                return;
            }
            
            try
            {
                // 아이콘
                string icon = entry.Level switch
                {
                    LogLevel.Debug => "🔍",
                    LogLevel.Info => "ℹ️",
                    LogLevel.Success => "✅",
                    LogLevel.Warning => "⚠️",
                    LogLevel.Error => "❌",
                    _ => "📝"
                };
                
                // 색상
                Color color = entry.Level switch
                {
                    LogLevel.Debug => Color.Gray,
                    LogLevel.Info => Color.Black,
                    LogLevel.Success => Color.Green,
                    LogLevel.Warning => Color.Orange,
                    LogLevel.Error => Color.Red,
                    _ => Color.Black
                };
                
                // 텍스트 추가
                string logText = $"[{entry.Timestamp:HH:mm:ss}] {icon} {entry.Message}\n";
                
                logBox.SelectionStart = logBox.TextLength;
                logBox.SelectionLength = 0;
                logBox.SelectionColor = color;
                logBox.AppendText(logText);
                
                // 최대 라인 수 유지
                if (logBox.Lines.Length > 1000)
                {
                    var lines = logBox.Lines.Skip(100).ToArray();
                    logBox.Lines = lines;
                }
                
                // 스크롤
                logBox.SelectionStart = logBox.TextLength;
                logBox.ScrollToCaret();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"로그 추가 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 지속시간 업데이트
        /// </summary>
        private void UpdateDurations()
        {
            if (clientGrid.InvokeRequired)
            {
                clientGrid.BeginInvoke(new Action(() => UpdateDurations()));
                return;
            }
            // 모든 클라이언트 정보를 한 번에 가져와서 캐싱
            var clientDict = DataManager.Instance.GetAllClientsList().ToDictionary(c => c.IpAddress);
            foreach (DataGridViewRow row in clientGrid.Rows)
            {
#pragma warning disable CS8600 // null 변환 경고 무시
                var ipObj = row.Cells["colIP"].Value;
                var ip = ipObj is string s ? s : ipObj?.ToString();
#pragma warning restore CS8600
                if (string.IsNullOrEmpty(ip))
                    continue;
                var client = DataManager.Instance.GetClient(ip);
                if (client != null)
                {
                    row.Cells["colDuration"].Value = FormatDuration(client.StateDuration);
                }
            }
        }
        
        /// <summary>
        /// 애니메이션 효과
        /// </summary>
        private async void AnimateNewRow(DataGridViewRow row)
        {
            var originalColor = row.DefaultCellStyle.BackColor;
            row.DefaultCellStyle.BackColor = Color.LightBlue;
            clientGrid.Refresh();
            
            await Task.Delay(500);
            
            row.DefaultCellStyle.BackColor = originalColor;
            clientGrid.Refresh();
        }
        
        private async void AnimateStateChange(DataGridViewRow row)
        {
            // 모든 클라이언트 정보를 한 번에 가져와서 캐싱
            var clientDict = DataManager.Instance.GetAllClientsList().ToDictionary(c => c.IpAddress);
            for (int i = 0; i < 3; i++)
            {
                row.DefaultCellStyle.BackColor = Color.White;
                clientGrid.Refresh();
                await Task.Delay(100);
                var ip = row.Cells["colIP"].Value?.ToString() ?? "";
                if (clientDict.TryGetValue(ip, out var client))
                {
                    UpdateRowStyle(row, client);
                }
                clientGrid.Refresh();
                await Task.Delay(100);
            }
        }
        
        /// <summary>
        /// 상태 아이콘 생성
        /// </summary>
        private Bitmap CreateStatusIcon(Color color)
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                
                using (var brush = new SolidBrush(color))
                {
                    g.FillEllipse(brush, 2, 2, 12, 12);
                }
                
                using (var pen = new Pen(Color.FromArgb(64, Color.Black), 1))
                {
                    g.DrawEllipse(pen, 2, 2, 12, 12);
                }
            }
            
            return bitmap;
        }
        
        /// <summary>
        /// 더블 버퍼링 활성화
        /// </summary>
        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, control, new object[] { true });
        }
        
        /// <summary>
        /// 시간 포맷
        /// </summary>
        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays}d {duration:hh\\:mm\\:ss}";
            else
                return duration.ToString(@"hh\:mm\:ss");
        }
        
        /// <summary>
        /// 정리
        /// </summary>
        public void Dispose()
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            
            foreach (var icon in iconCache.Values)
            {
                icon?.Dispose();
            }
            
            iconCache.Clear();
            rowCache.Clear();
        }
    }
    
    /// <summary>
    /// 로그 엔트리
    /// </summary>
    internal class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = "";
        public LogLevel Level { get; set; }
        public string Category { get; set; } = "";
    }
    
    /// <summary>
    /// 로그 레벨
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Success,
        Warning,
        Error
    }
} 