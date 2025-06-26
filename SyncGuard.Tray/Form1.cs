using System;
using System.Drawing;
using System.Windows.Forms;
using SyncGuard.Core;
using System.Runtime.Versioning;
using System.IO;

namespace SyncGuard.Tray
{
    [SupportedOSPlatform("windows")]
    public partial class Form1 : Form
    {
        private NotifyIcon? notifyIcon;
        private SyncChecker? syncChecker;
        private System.Windows.Forms.Timer? syncTimer;
        private SyncChecker.SyncStatus lastStatus = SyncChecker.SyncStatus.Unknown;
        private readonly string logFilePath = "syncguard_log.txt";
        private readonly int maxLogSizeMB = 10; // 10MB
        
        public Form1()
        {
            InitializeComponent();
            
            // 폼을 숨기기
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            
            // 로그 시스템 초기화
            InitializeLogging();
            
            InitializeTrayIcon();
            
            try
            {
                // SyncChecker 초기화 시도
                LogMessage("INFO", "SyncChecker 초기화 시작...");
                syncChecker = new SyncChecker();
                
                // Sync 상태 변경 이벤트 구독
                syncChecker.SyncStatusChanged += OnSyncStatusChanged;
                
                LogMessage("INFO", "SyncChecker 초기화 성공!");
                
                InitializeSyncTimer();
                ShowToastNotification("SyncGuard 시작됨", "Quadro Sync 모니터링이 시작되었습니다.");
            }
            catch (Exception ex)
            {
                // 상세한 오류 정보 로그
                LogMessage("ERROR", $"SyncChecker 초기화 실패: {ex.GetType().Name}");
                LogMessage("ERROR", $"오류 메시지: {ex.Message}");
                LogMessage("ERROR", $"스택 트레이스: {ex.StackTrace}");
                
                // 사용자에게 알림
                MessageBox.Show($"SyncGuard 초기화 실패:\n\n{ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // 기본 상태로 시작
                syncChecker = null;
                ShowToastNotification("SyncGuard 시작됨 (제한된 모드)", "NVAPI 초기화 실패로 기본 모드로 실행됩니다.");
            }
        }

        private void InitializeLogging()
        {
            try
            {
                // 로그 파일 크기 체크 및 로테이션
                CheckAndRotateLogFile();
                
                LogMessage("INFO", "SyncGuard 로그 시스템 초기화 완료");
            }
            catch (Exception ex)
            {
                // 로그 초기화 실패 시 콘솔에 출력
                Console.WriteLine($"로그 시스템 초기화 실패: {ex.Message}");
            }
        }
        
        private void LogMessage(string level, string message)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                
                // 콘솔 출력 (디버깅용)
                Console.WriteLine(logEntry);
                
                // 파일 로그 (UTF-8 인코딩 사용)
                File.AppendAllText(logFilePath, logEntry + Environment.NewLine, System.Text.Encoding.UTF8);
                
                // 로그 파일 크기 체크
                CheckAndRotateLogFile();
            }
            catch
            {
                // 로그 기록 실패 시 무시
            }
        }
        
        private void CheckAndRotateLogFile()
        {
            try
            {
                if (File.Exists(logFilePath))
                {
                    var fileInfo = new FileInfo(logFilePath);
                    var sizeInMB = fileInfo.Length / (1024 * 1024);
                    
                    if (sizeInMB >= maxLogSizeMB)
                    {
                        // 백업 파일명 생성 (날짜_시간 포함)
                        var backupFileName = $"syncguard_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                        
                        // 기존 로그 파일을 백업으로 이동
                        File.Move(logFilePath, backupFileName);
                        
                        // 새로운 로그 파일에 로테이션 메시지 기록 (UTF-8 인코딩 사용)
                        var rotationMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] 로그 파일 로테이션 완료: {backupFileName}";
                        File.WriteAllText(logFilePath, rotationMessage + Environment.NewLine, System.Text.Encoding.UTF8);
                    }
                }
            }
            catch
            {
                // 로그 로테이션 실패 시 무시
            }
        }

        private void InitializeTrayIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Application;
            notifyIcon.Text = "SyncGuard - Quadro Sync 모니터링";
            notifyIcon.Visible = true;

            // 컨텍스트 메뉴 생성
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("리프레시", null, OnRefreshSyncStatus);
            contextMenu.Items.Add("-"); // 구분선
            contextMenu.Items.Add("종료", null, OnExit);
            
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += OnTrayIconDoubleClick;
        }

        private void InitializeSyncTimer()
        {
            syncTimer = new System.Windows.Forms.Timer();
            syncTimer.Interval = 1000; // 1초마다 체크
            syncTimer.Tick += OnSyncTimerTick;
            syncTimer.Start();
        }

        private void OnSyncTimerTick(object? sender, EventArgs e)
        {
            if (syncChecker == null)
            {
                // SyncChecker가 없으면 기본 상태 표시
                UpdateTrayIcon(SyncChecker.SyncStatus.Unknown);
                return;
            }

            try
            {
                var status = syncChecker.GetSyncStatus();
                UpdateTrayIcon(status);
                
                // 상태 변경 시 알림
                if (status != lastStatus)
                {
                    string message = GetStatusMessage(status);
                    LogMessage("INFO", $"Sync 상태 변경: {lastStatus} -> {status}");
                    ShowToastNotification("Sync 상태 변경", message);
                    lastStatus = status;
                }
            }
            catch (Exception ex)
            {
                LogMessage("ERROR", $"Sync 체크 중 오류: {ex.Message}");
                UpdateTrayIcon(SyncChecker.SyncStatus.Unknown);
            }
        }

        private void UpdateTrayIcon(SyncChecker.SyncStatus status)
        {
            if (notifyIcon == null) return;
            
            // 상태에 따른 아이콘 색상 변경
            Color iconColor = status switch
            {
                SyncChecker.SyncStatus.Locked => Color.Green,      // 초록색: 마스터 (State: 2)
                SyncChecker.SyncStatus.Unknown => Color.Yellow,    // 노란색: 슬레이브 (State: 1)
                SyncChecker.SyncStatus.Error => Color.Red,         // 빨간색: 동기화 안됨 (State: 0)
                SyncChecker.SyncStatus.Unlocked => Color.Blue,     // 파란색: 기타
                _ => Color.Gray
            };

            // 간단한 아이콘 생성 (실제로는 더 정교한 아이콘이 필요)
            using (var bitmap = new Bitmap(16, 16))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(iconColor);
                notifyIcon.Icon = Icon.FromHandle(bitmap.GetHicon());
            }

            // 툴크 업데이트
            notifyIcon.Text = $"SyncGuard - {GetStatusMessage(status)}";
        }

        private string GetStatusMessage(SyncChecker.SyncStatus status)
        {
            return status switch
            {
                SyncChecker.SyncStatus.Locked => "Master (마스터)",
                SyncChecker.SyncStatus.Unknown => "Slave (슬레이브)",
                SyncChecker.SyncStatus.Error => "UnSynced (동기화 안됨)",
                SyncChecker.SyncStatus.Unlocked => "Unknown (알 수 없음)",
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
            // 더블클릭 시 리프레시 실행
            OnRefreshSyncStatus(sender, e);
        }

        private void OnExit(object? sender, EventArgs e)
        {
            LogMessage("INFO", "SyncGuard 종료됨");
            syncChecker?.Dispose();
            notifyIcon?.Dispose();
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
            }
            else
            {
                LogMessage("INFO", "SyncGuard 종료됨");
                syncChecker?.Dispose();
                notifyIcon?.Dispose();
            }
            base.OnFormClosing(e);
        }

        private void OnSyncStatusChanged(object? sender, SyncChecker.SyncStatus newStatus)
        {
            // UI 스레드에서 실행
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnSyncStatusChanged(sender, newStatus)));
                return;
            }
            
            UpdateTrayIcon(newStatus);
            string message = GetStatusMessage(newStatus);
            ShowToastNotification("Sync 상태 변경", message);
        }

        private void OnRefreshSyncStatus(object? sender, EventArgs e)
        {
            if (syncChecker == null)
            {
                LogMessage("WARNING", "NVAPI 초기화 실패로 Sync 상태를 새로고침할 수 없습니다.");
                MessageBox.Show("NVAPI 초기화 실패로 Sync 상태를 새로고침할 수 없습니다.", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                LogMessage("INFO", "수동 리프레시 실행");
                syncChecker.RefreshSyncStatus();
                
                // 리프레시 후 상태 확인
                var status = syncChecker.GetSyncStatus();
                string message = GetStatusMessage(status);
                
                // 사용자에게 알림
                ShowToastNotification("Sync 상태 새로고침 완료", message);
                
                LogMessage("INFO", $"리프레시 후 상태: {status}");
            }
            catch (Exception ex)
            {
                LogMessage("ERROR", $"리프레시 중 오류: {ex.Message}");
                MessageBox.Show($"Sync 상태 새로고침 중 오류: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
