using System;
using System.Drawing;
using System.Windows.Forms;
using SyncGuard.Core;
using System.Runtime.Versioning;
using System.IO;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SyncGuard.Tray
{
    [SupportedOSPlatform("windows")]
    public partial class Form1 : Form
    {
        private NotifyIcon? notifyIcon;
        private SyncChecker? syncChecker;
        private System.Windows.Forms.Timer? syncTimer;
        
        // 🔥 최적화를 위한 상태 추적
        private SyncChecker.SyncStatus lastStatus = SyncChecker.SyncStatus.Unknown;
        private SyncChecker.SyncStatus lastUiStatus = SyncChecker.SyncStatus.Unknown;
        private DateTime lastStatusChangeTime = DateTime.Now;
        
        private bool isTcpClientEnabled = false;
        private int tcpServerPort = 8080;
        private string targetIpAddress = "127.0.0.1";
        private int tcpTransmissionInterval = 1000; // TCP 전송 간격 (밀리초, 기본값: 1초)
        
        // 🔥 아이콘 캐시
        private readonly Dictionary<SyncChecker.SyncStatus, Icon> iconCache = new();
        
        // 🔥 성능 모니터링
        private System.Windows.Forms.Timer? statsTimer;
        private ToolStripMenuItem? statsMenuItem;
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
        
        public Form1()
        {
            InitializeComponent();
            
            // 설정 파일에서 값 불러오기
            LoadConfig();
            
            // 첫 실행 확인
            CheckFirstRun();
            
            // 폼을 숨기기
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            
            // 로그 시스템 초기화
            InitializeLogging();
            
            // 🔥 아이콘 캐시 초기화
            InitializeIconCache();
            
            InitializeTrayIcon();
            UpdateTrayIcon(lastStatus); // 항상 커스텀 아이콘으로 초기화
            ShowUnsupportedNoticeIfNeeded();
            
            try
            {
                // SyncChecker 초기화 시도
                Logger.Info("SyncChecker 초기화 시작...");
                syncChecker = new SyncChecker();
                
                // TCP 클라이언트 설정에 따라 시작
                if (isTcpClientEnabled)
                {
                    StartTcpClient();
                }
                else
                {
                    Logger.Info("외부 전송이 비활성화되어 TCP 클라이언트를 시작하지 않습니다.");
                }
                
                // Sync 상태 변경 이벤트 구독
                syncChecker.SyncStatusChanged += OnSyncStatusChanged;
                
                Logger.Info("SyncChecker 초기화 성공!");
                
                InitializeSyncTimer();
                
                // 🔥 통계 타이머 초기화
                InitializeStatsTimer();
                
                ShowToastNotification("SyncGuard 시작됨", "Quadro Sync 모니터링이 시작되었습니다.");
            }
            catch (Exception ex)
            {
                // 상세한 오류 정보 로그
                Logger.Error($"SyncChecker 초기화 실패: {ex.GetType().Name}");
                Logger.Error($"오류 메시지: {ex.Message}");
                Logger.Error($"스택 트레이스: {ex.StackTrace}");
                
                // 팝업 없이 내부 상태만 Unknown(state0)으로 처리
                syncChecker = null;
                lastStatus = SyncChecker.SyncStatus.Unknown; // state0 처리
                ShowToastNotification("SyncGuard 제한 모드", "이 시스템은 Sync 기능이 없는 GPU입니다. (state0)");
                // 트레이 아이콘을 Unknown(회색)으로 강제 설정
                UpdateTrayIcon(SyncChecker.SyncStatus.Unknown);
            }
        }

        // 🔥 아이콘 캐시 초기화
        private void InitializeIconCache()
        {
            iconCache[SyncChecker.SyncStatus.Master] = CreateColorIcon(Color.Green);
            iconCache[SyncChecker.SyncStatus.Slave] = CreateColorIcon(Color.Yellow);
            iconCache[SyncChecker.SyncStatus.Error] = CreateColorIcon(Color.Red);
            iconCache[SyncChecker.SyncStatus.Unknown] = CreateColorIcon(Color.Gray); // 미지원/오류 환경은 회색
        }
        
        // 🔥 색상 아이콘 생성
        private Icon CreateColorIcon(Color color)
        {
            var bitmap = new Bitmap(16, 16);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // 원 그리기
                using (var brush = new SolidBrush(color))
                {
                    graphics.FillEllipse(brush, 1, 1, 14, 14);
                }
                
                // 테두리
                using (var pen = new Pen(Color.FromArgb(64, 0, 0, 0), 1))
                {
                    graphics.DrawEllipse(pen, 1, 1, 14, 14);
                }
            }
            
            IntPtr hIcon = bitmap.GetHicon();
            Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
            DestroyIcon(hIcon); // 핸들 해제
            return icon;
        }

        private void InitializeLogging()
        {
            try
            {
                Logger.Info("=== SyncGuard 시작 ===");
                Logger.Info($"버전: 3.0 (최적화)");
                Logger.Info($"로그 레벨: {Environment.GetEnvironmentVariable("SYNCGUARD_LOG_LEVEL") ?? "INFO"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"로그 시스템 초기화 실패: {ex.Message}");
            }
        }

        private void InitializeTrayIcon()
        {
            try
            {
                if (notifyIcon != null)
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                }
                notifyIcon = new NotifyIcon();
                // notifyIcon.Icon = SystemIcons.Application; // 기본 아이콘 할당 제거
                // 미지원 환경이면 안내 툴팁
                string tip = "SyncGuard - ";
                if (syncChecker != null && syncChecker.GetSyncStatus() == SyncChecker.SyncStatus.Unknown)
                {
                    tip += "지원되지 않는 환경(Unknown)";
                }
                else
                {
                    tip += "Quadro Sync 모니터링";
                }
                notifyIcon.Text = tip;
                
                // 컨텍스트 메뉴 생성
                var contextMenu = new ContextMenuStrip();
                
                // TCP 서버 상태 (첫 번째 항목)
                var tcpStatusItem = new ToolStripMenuItem($"TCP 서버: {(isTcpClientEnabled ? "활성" : "비활성")}");
                tcpStatusItem.Enabled = false;
                contextMenu.Items.Add(tcpStatusItem);
                
                // 🔥 성능 통계 메뉴
                statsMenuItem = new ToolStripMenuItem("성능 통계", null, OnShowStats);
                contextMenu.Items.Add(statsMenuItem);
                
                // 구분선
                contextMenu.Items.Add(new ToolStripSeparator());
                
                // 설정 메뉴
                var settingsItem = new ToolStripMenuItem("설정...", null, OnSettings);
                contextMenu.Items.Add(settingsItem);
                
                // 리프레시 메뉴
                var refreshItem = new ToolStripMenuItem("리프레시", null, OnRefreshSyncStatus);
                contextMenu.Items.Add(refreshItem);
                
                // 구분선
                contextMenu.Items.Add(new ToolStripSeparator());
                
                // 종료 메뉴
                var exitItem = new ToolStripMenuItem("종료", null, OnExit);
                contextMenu.Items.Add(exitItem);
                
                // 컨텍스트 메뉴를 NotifyIcon에 연결
                notifyIcon.ContextMenuStrip = contextMenu;
                
                // 더블클릭 이벤트 연결
                notifyIcon.DoubleClick += OnTrayIconDoubleClick;
                
                // 트레이 아이콘 표시
                notifyIcon.Visible = true;
                
                Logger.Info("트레이 아이콘 초기화 완료");
            }
            catch (Exception ex)
            {
                Logger.Error($"트레이 아이콘 초기화 실패: {ex.Message}");
                MessageBox.Show($"트레이 아이콘 초기화 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetStatusText(SyncChecker.SyncStatus status)
        {
            return status switch
            {
                SyncChecker.SyncStatus.Master => "Synced",
                SyncChecker.SyncStatus.Slave => "Free",
                SyncChecker.SyncStatus.Error => "Free",
                SyncChecker.SyncStatus.Unknown => "Unknown",
                _ => "Unknown"
            };
        }

        private void OnSettings(object? sender, EventArgs e)
        {
            // 설정 창 생성
            var settingsForm = new Form
            {
                Text = "SyncGuard 설정",
                Size = new Size(400, 350),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            // IP 주소 입력
            var lblIp = new Label { Text = "대상 IP 주소:", Location = new Point(20, 20), Size = new Size(100, 20) };
            var txtIp = new TextBox { Location = new Point(130, 20), Size = new Size(200, 20), Text = targetIpAddress };

            // 포트 입력
            var lblPort = new Label { Text = "포트:", Location = new Point(20, 50), Size = new Size(100, 20) };
            var txtPort = new TextBox { Location = new Point(130, 50), Size = new Size(200, 20), Text = tcpServerPort.ToString() };

            // TCP 전송 간격 선택
            var lblInterval = new Label { Text = "전송 간격:", Location = new Point(20, 80), Size = new Size(100, 20) };
            var cmbInterval = new ComboBox 
            { 
                Location = new Point(130, 80), 
                Size = new Size(200, 20), 
                DropDownStyle = ComboBoxStyle.DropDownList 
            };
            
            // 간격 옵션 추가
            cmbInterval.Items.AddRange(new object[] 
            {
                "1초",
                "5초", 
                "10초",
                "30초",
                "60초"
            });
            
            // 현재 설정된 간격 선택
            var currentIntervalText = tcpTransmissionInterval switch
            {
                1000 => "1초",
                5000 => "5초",
                10000 => "10초",
                30000 => "30초",
                60000 => "60초",
                _ => "1초"
            };
            cmbInterval.SelectedItem = currentIntervalText;

            // 외부 전송 활성화 체크박스
            var chkEnable = new CheckBox { Text = "외부 전송 활성화", Location = new Point(20, 110), Size = new Size(150, 20), Checked = isTcpClientEnabled };

            // 연결 테스트 버튼
            var btnTest = new Button { Text = "연결 테스트", Location = new Point(20, 140), Size = new Size(100, 30) };
            btnTest.Click += async (s, e) =>
            {
                try
                {
                    var ip = txtIp.Text;
                    var port = int.Parse(txtPort.Text);
                    
                    btnTest.Enabled = false;
                    btnTest.Text = "테스트 중...";
                    
                    // 실제 TCP 연결 테스트
                    using var client = new TcpClient();
                    var connectTask = client.ConnectAsync(ip, port);
                    var timeoutTask = Task.Delay(3000); // 3초 타임아웃
                    
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    
                    if (completedTask == connectTask)
                    {
                        await connectTask; // 예외가 있다면 여기서 발생
                        MessageBox.Show($"연결 성공!\nIP: {ip}\n포트: {port}", "연결 테스트", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"연결 실패: 타임아웃 (3초)\nIP: {ip}\n포트: {port}", "연결 테스트", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"연결 실패: {ex.Message}\nIP: {txtIp.Text}\n포트: {txtPort.Text}", "연결 테스트", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btnTest.Enabled = true;
                    btnTest.Text = "연결 테스트";
                }
            };

            // 저장 버튼
            var btnSave = new Button { Text = "저장", Location = new Point(200, 250), Size = new Size(80, 30) };
            btnSave.Click += (s, e) =>
            {
                try
                {
                    tcpServerPort = int.Parse(txtPort.Text);
                    targetIpAddress = txtIp.Text;
                    
                    // 외부 전송 활성화 상태 저장
                    isTcpClientEnabled = chkEnable.Checked;
                    
                    // 선택된 간격을 밀리초로 변환
                    tcpTransmissionInterval = cmbInterval.SelectedItem?.ToString() switch
                    {
                        "1초" => 1000,
                        "5초" => 5000,
                        "10초" => 10000,
                        "30초" => 30000,
                        "60초" => 60000,
                        _ => 1000
                    };
                    
                    Logger.Info($"설정 저장: IP={targetIpAddress}, Port={tcpServerPort}, Interval={tcpTransmissionInterval}ms, ExternalSend={isTcpClientEnabled}");
                    
                    // 설정 파일에 저장
                    SaveConfig();
                    
                    // 타이머 간격 업데이트
                    if (syncTimer != null)
                    {
                        syncTimer.Interval = tcpTransmissionInterval;
                    }
                    
                    // TCP 클라이언트 상태 업데이트
                    if (isTcpClientEnabled)
                    {
                        StartTcpClient();
                        Logger.Info("외부 전송 활성화됨 - TCP 클라이언트 시작");
                    }
                    else
                    {
                        StopTcpClient();
                        Logger.Info("외부 전송 비활성화됨 - TCP 클라이언트 중지");
                    }
                    
                    // 트레이 메뉴 업데이트
                    UpdateTrayMenu();
                    
                    // 설정 변경 알림
                    MessageBox.Show($"설정이 저장되었습니다.\n외부 전송: {(isTcpClientEnabled ? "활성화" : "비활성화")}", "설정 저장", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    settingsForm.Close();
                }
                catch
                {
                    MessageBox.Show("잘못된 설정입니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // 취소 버튼
            var btnCancel = new Button { Text = "취소", Location = new Point(290, 250), Size = new Size(80, 30) };
            btnCancel.Click += (s, e) => settingsForm.Close();

            // 컨트롤 추가
            settingsForm.Controls.AddRange(new Control[] { lblIp, txtIp, lblPort, txtPort, lblInterval, cmbInterval, chkEnable, btnTest, btnSave, btnCancel });

            settingsForm.ShowDialog();
        }

        private void UpdateTrayMenu()
        {
            try
            {
                // Form이 유효한지 확인
                if (this.IsDisposed || !this.IsHandleCreated)
                {
                    Logger.Warning("Form이 유효하지 않아 트레이 메뉴 업데이트를 건너뜁니다.");
                    return;
                }
                
                // NotifyIcon이 유효한지 확인
                if (notifyIcon == null)
                {
                    Logger.Warning("NotifyIcon이 null입니다.");
                    return;
                }
                
                // ContextMenuStrip이 유효한지 확인
                if (notifyIcon.ContextMenuStrip == null)
                {
                    Logger.Warning("ContextMenuStrip이 null입니다. 트레이 아이콘을 재초기화합니다.");
                    InitializeTrayIcon();
                    return;
                }
                
                var contextMenu = notifyIcon.ContextMenuStrip;
                
                // 컨텍스트 메뉴가 유효한지 확인
                if (contextMenu.IsDisposed)
                {
                    Logger.Warning("ContextMenuStrip이 disposed 상태입니다. 트레이 아이콘을 재초기화합니다.");
                    InitializeTrayIcon();
                    return;
                }
                
                // TCP 서버 상태 업데이트 (첫 번째 항목)
                if (contextMenu.Items.Count > 0 && contextMenu.Items[0] is ToolStripMenuItem tcpStatusItem)
                {
                    tcpStatusItem.Text = $"TCP 서버: {(isTcpClientEnabled ? "활성" : "비활성")}";
                    Logger.Info($"TCP 상태 메뉴 업데이트: {(isTcpClientEnabled ? "활성" : "비활성")}");
                }
                else
                {
                    Logger.Warning("TCP 상태 메뉴 항목을 찾을 수 없습니다.");
                }
                
                Logger.Info("트레이 메뉴 업데이트 완료");
            }
            catch (Exception ex)
            {
                Logger.Error($"트레이 메뉴 업데이트 실패: {ex.Message}");
                // 오류 발생 시 트레이 아이콘 재초기화 시도
                try
                {
                    InitializeTrayIcon();
                }
                catch (Exception reinitEx)
                {
                    Logger.Error($"트레이 아이콘 재초기화 실패: {reinitEx.Message}");
                }
            }
        }

        private void InitializeSyncTimer()
        {
            syncTimer = new System.Windows.Forms.Timer();
            syncTimer.Interval = tcpTransmissionInterval; // 설정된 간격으로 설정
            syncTimer.Tick += OnSyncTimerTick;
            syncTimer.Start();
        }
        
        // 🔥 통계 타이머 초기화
        private void InitializeStatsTimer()
        {
            statsTimer = new System.Windows.Forms.Timer();
            statsTimer.Interval = 60000; // 1분마다
            statsTimer.Tick += (s, e) =>
            {
                if (syncChecker != null)
                {
                    var stats = syncChecker.GetPerformanceStats();
                    Logger.Debug($"[통계] 메시지: {stats.messages}, 처리율: {stats.messagesPerSec:F1}/s, 효율: {stats.connectionEfficiency * 100:F1}%");
                }
            };
            statsTimer.Start();
        }
        
        // 🔥 성능 통계 표시
        private void OnShowStats(object? sender, EventArgs e)
        {
            if (syncChecker == null) return;
            
            var stats = syncChecker.GetPerformanceStats();
            var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
            
            var message = $@"=== SyncGuard 성능 통계 ===

실행 시간: {uptime:hh\:mm\:ss}

전송 통계:
• 총 메시지: {stats.messages:N0}개
• 총 데이터: {stats.bytes:N0} bytes ({stats.bytes / 1024.0:F1} KB)
• 전송률: {stats.messagesPerSec:F1} msg/s

연결 효율성: {stats.connectionEfficiency * 100:F1}%

현재 상태: {GetStatusText(lastStatus)}
마지막 변경: {lastStatusChangeTime:HH:mm:ss}";
            
            MessageBox.Show(message, "성능 통계", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // 🔥 최적화된 타이머 이벤트
        private void OnSyncTimerTick(object? sender, EventArgs e)
        {
            if (syncChecker == null)
            {
                UpdateTrayIcon(SyncChecker.SyncStatus.Unknown);
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    var status = syncChecker.GetSyncStatus();
                    
                    // UI 업데이트는 상태 변경 시에만
                    if (status != lastUiStatus)
                    {
                        lastUiStatus = status;
                        
                        if (!this.IsDisposed && this.IsHandleCreated)
                        {
                            this.BeginInvoke(() =>
                            {
                                try
                                {
                                    UpdateTrayIcon(status);
                                    
                                    if (lastStatus != SyncChecker.SyncStatus.Unknown)
                                    {
                                        lastStatusChangeTime = DateTime.Now;
                                        ShowToastNotification("Sync 상태 변경", GetStatusMessage(status));
                                    }
                                    
                                    lastStatus = status;
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error($"UI 업데이트 중 오류: {ex.Message}");
                                }
                            });
                        }
                    }
                    
                    // TCP 전송 (상태 변경과 무관하게)
                    if (isTcpClientEnabled && syncChecker != null)
                    {
                        await syncChecker.SendStatusToServer();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Sync 체크 중 오류: {ex.Message}");
                }
            });
        }

        // 🔥 최적화된 트레이 아이콘 업데이트
        private void UpdateTrayIcon(SyncChecker.SyncStatus status)
        {
            if (notifyIcon == null) return;
            
            // 캐시된 아이콘 사용
            if (iconCache.TryGetValue(status, out var icon))
            {
                notifyIcon.Icon = icon;
            }
            
            notifyIcon.Text = $"SyncGuard - {GetStatusMessage(status)}";
        }

        private string GetStatusMessage(SyncChecker.SyncStatus status)
        {
            return status switch
            {
                SyncChecker.SyncStatus.Master => "Master (마스터)",
                SyncChecker.SyncStatus.Slave => "Slave (슬레이브)",
                SyncChecker.SyncStatus.Error => "Error (오류)",
                SyncChecker.SyncStatus.Unknown => "Unknown (알 수 없음)",
                _ => "Unknown (알 수 없음)"
            };
        }

        private void ShowToastNotification(string title, string message)
        {
            if (notifyIcon == null) return;
            
            // Windows 10/11 토스트 알림
            try
            {
                // var toast = new Microsoft.Toolkit.Uwp.Notifications.ToastNotification(
                //     new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                //         .AddText(title)
                //         .AddText(message)
                //         .GetToastContent());
                // 
                // Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
                // 토스트 알림 대신 풍선 도움말만 사용
                notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
            }
            catch
            {
                // 토스트 알림 실패 시 기본 알림
                notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
            }
        }

        private void OnTrayIconDoubleClick(object? sender, EventArgs e)
        {
            try
            {
                // 더블클릭 시 컨텍스트 메뉴 표시
                if (notifyIcon?.ContextMenuStrip != null && !notifyIcon.ContextMenuStrip.IsDisposed)
                {
                    // 메뉴를 화면 중앙에 표시하여 시작 메뉴바 자동 숨김 문제 해결
                    var screen = Screen.PrimaryScreen;
                    var menuLocation = new Point(screen.WorkingArea.Width / 2, screen.WorkingArea.Height / 2);
                    
                    notifyIcon.ContextMenuStrip.Show(menuLocation);
                    Logger.Info("더블클릭으로 트레이 메뉴 표시됨");
                }
                else
                {
                    Logger.Warning("컨텍스트 메뉴를 표시할 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"더블클릭 메뉴 표시 실패: {ex.Message}");
            }
        }

        private void OnExit(object? sender, EventArgs e)
        {
            Logger.Info("SyncGuard 종료 중...");
            
            // 🔥 통계 출력
            if (syncChecker != null)
            {
                var stats = syncChecker.GetPerformanceStats();
                Logger.Info($"[최종 통계] 메시지: {stats.messages}, 데이터: {stats.bytes} bytes, 효율: {stats.connectionEfficiency * 100:F1}%");
            }
            
            syncChecker?.Dispose();
            notifyIcon?.Dispose();
            
            // 🔥 아이콘 캐시 정리
            foreach (var icon in iconCache.Values)
            {
                icon?.Dispose();
            }
            
            Logger.Info("SyncGuard 종료됨");
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                StopTcpClient();
                
                syncTimer?.Stop();
                syncTimer?.Dispose();
                
                statsTimer?.Stop();
                statsTimer?.Dispose();
                
                if (syncChecker != null)
                {
                    syncChecker.SyncStatusChanged -= OnSyncStatusChanged;
                    syncChecker.Dispose();
                }
                
                notifyIcon?.Dispose();
                
                foreach (var icon in iconCache.Values)
                {
                    icon?.Dispose();
                }
                
                // 트레이 아이콘 정리
                notifyIcon?.Dispose();
                
                Logger.Info("SyncGuard 종료됨");
            }
            catch (Exception ex)
            {
                Logger.Error($"종료 중 오류: {ex.Message}");
            }
            
            base.OnFormClosing(e);
        }

        private void OnSyncStatusChanged(object? sender, SyncChecker.SyncStatus newStatus)
        {
            try
            {
                Logger.Info($"Sync 상태 변경: {lastStatus} → {newStatus}");
                
                // UI 스레드에서 실행 (Form이 유효한 경우에만)
                if (this.InvokeRequired && !this.IsDisposed && this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() => OnSyncStatusChanged(sender, newStatus)));
                    return;
                }
                
                lastStatus = newStatus;
                
                // 트레이 아이콘 업데이트
                UpdateTrayIcon(newStatus);
                
                // 트레이 메뉴 업데이트
                UpdateTrayMenu();
                
                // TCP 전송 (상태 변경 시)
                if (isTcpClientEnabled && syncChecker != null)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            await syncChecker.SendStatusToServer();
                            Logger.Info("상태 변경 시 TCP 전송 완료");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"상태 변경 시 TCP 전송 실패: {ex.Message}");
                        }
                    });
                }
                
                // 알림 표시
                var message = GetStatusMessage(newStatus);
                ShowToastNotification("SyncGuard 상태 변경", message);
                
                Logger.Info($"상태 변경 처리 완료: {newStatus}");
            }
            catch (Exception ex)
            {
                Logger.Error($"상태 변경 처리 중 오류: {ex.Message}");
            }
        }

        private void OnRefreshSyncStatus(object? sender, EventArgs e)
        {
            if (syncChecker == null)
            {
                Logger.Warning("NVAPI 초기화 실패로 Sync 상태를 새로고침할 수 없습니다.");
                MessageBox.Show("NVAPI 초기화 실패로 Sync 상태를 새로고침할 수 없습니다.", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Logger.Info("수동 리프레시 실행");
                syncChecker.RefreshSyncStatus();
                
                // 리프레시 후 상태 확인
                var status = syncChecker.GetSyncStatus();
                string message = GetStatusMessage(status);
                
                // 사용자에게 알림
                ShowToastNotification("Sync 상태 새로고침 완료", message);
                
                Logger.Info($"리프레시 후 상태: {status}");
            }
            catch (Exception ex)
            {
                Logger.Error($"리프레시 중 오류: {ex.Message}");
                MessageBox.Show($"Sync 상태 새로고침 중 오류: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartTcpClient()
        {
            try
            {
                if (syncChecker != null)
                {
                    syncChecker.StartTcpClient(targetIpAddress, tcpServerPort);
                    isTcpClientEnabled = true;
                    Logger.Info($"TCP 클라이언트 시작됨 - {targetIpAddress}:{tcpServerPort}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"TCP 클라이언트 시작 실패: {ex.Message}");
                isTcpClientEnabled = false;
            }
        }

        private void StopTcpClient()
        {
            try
            {
                if (syncChecker != null)
                {
                    syncChecker.StopTcpClient();
                    isTcpClientEnabled = false;
                    Logger.Info("TCP 클라이언트 중지됨");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"TCP 클라이언트 중지 실패: {ex.Message}");
            }
        }

        // 설정 파일에서 값 불러오기
        private void LoadConfig()
        {
            try
            {
                Logger.Info("Form1.LoadConfig 시작");
                
                var (serverIP, serverPort, transmissionInterval, enableExternalSend) = ConfigManager.LoadConfig();
                
                Logger.Info($"ConfigManager.LoadConfig 결과: IP={serverIP}, Port={serverPort}, Interval={transmissionInterval}ms, ExternalSend={enableExternalSend}");
                
                targetIpAddress = serverIP;
                tcpServerPort = serverPort;
                tcpTransmissionInterval = transmissionInterval;
                isTcpClientEnabled = enableExternalSend;
                
                Logger.Info($"Form1.LoadConfig 완료: IP={targetIpAddress}, Port={tcpServerPort}, Interval={tcpTransmissionInterval}ms, ExternalSend={isTcpClientEnabled}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Form1.LoadConfig 실패: {ex.Message}");
                Logger.Error($"Form1.LoadConfig 스택 트레이스: {ex.StackTrace}");
            }
        }
        
        // 설정 파일에 값 저장하기
        private void SaveConfig()
        {
            try
            {
                Logger.Info($"Form1.SaveConfig 시작 - IP={targetIpAddress}, Port={tcpServerPort}, Interval={tcpTransmissionInterval}ms, ExternalSend={isTcpClientEnabled}");
                
                ConfigManager.SaveConfig(targetIpAddress, tcpServerPort, tcpTransmissionInterval, isTcpClientEnabled);
                
                Logger.Info($"Form1.SaveConfig 완료 - ConfigManager.SaveConfig 호출됨");
            }
            catch (Exception ex)
            {
                Logger.Error($"Form1.SaveConfig 실패: {ex.Message}");
                Logger.Error($"Form1.SaveConfig 스택 트레이스: {ex.StackTrace}");
            }
        }
        
        private void CheckFirstRun()
        {
            string firstRunFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SyncGuard",
                "first_run_complete.txt"
            );
            
            if (!File.Exists(firstRunFile))
            {
                // 첫 실행
                ShowToastNotification("SyncGuard에 오신 것을 환영합니다!", 
                    "설정 메뉴에서 TCP 서버를 구성해주세요.");
                
                // 첫 실행 완료 표시
                try
                {
                    var dir = Path.GetDirectoryName(firstRunFile);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    
                    File.WriteAllText(firstRunFile, DateTime.Now.ToString());
                }
                catch (Exception ex)
                {
                    Logger.Error($"첫 실행 파일 생성 실패: {ex.Message}");
                }
            }
        }

        private void ShowUnsupportedNoticeIfNeeded()
        {
            // 미지원 환경이면 안내 메시지 1회 표시
            if (syncChecker != null && syncChecker.GetSyncStatus() == SyncChecker.SyncStatus.Unknown)
            {
                ShowToastNotification("SyncGuard 안내", "이 시스템은 GPU 동기화 기능을 지원하지 않습니다. NVIDIA Quadro GPU 및 Sync 카드가 필요합니다.");
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateTrayIcon(lastStatus); // 폼이 완전히 로드된 후에도 아이콘을 한 번 더 설정
        }
    }
}


