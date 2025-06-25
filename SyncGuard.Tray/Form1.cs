using System;
using System.Drawing;
using System.Windows.Forms;
using SyncGuard.Core;

namespace SyncGuard.Tray
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon;
        private SyncChecker syncChecker;
        private System.Windows.Forms.Timer syncTimer;
        private SyncChecker.SyncStatus lastStatus = SyncChecker.SyncStatus.Unknown;
        
        // Master/Slave 메뉴 아이템 참조 추가
        private ToolStripMenuItem masterMenuItem;
        private ToolStripMenuItem slaveMenuItem;

        public Form1()
        {
            InitializeComponent();
            
            // 폼을 숨기기
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            
            InitializeTrayIcon();
            
            try
            {
                // SyncChecker 초기화 시도
                Console.WriteLine("SyncChecker 초기화 시작...");
                syncChecker = new SyncChecker();
                
                // Sync 상태 변경 이벤트 구독
                syncChecker.SyncStatusChanged += OnSyncStatusChanged;
                
                Console.WriteLine("SyncChecker 초기화 성공!");
                
                InitializeSyncTimer();
                ShowToastNotification("SyncGuard 시작됨", "Quadro Sync 모니터링이 시작되었습니다.");
            }
            catch (Exception ex)
            {
                // 상세한 오류 정보 출력
                Console.WriteLine($"SyncChecker 초기화 실패: {ex.GetType().Name}");
                Console.WriteLine($"오류 메시지: {ex.Message}");
                Console.WriteLine($"스택 트레이스: {ex.StackTrace}");
                
                // 파일로 로그 저장
                System.IO.File.AppendAllText("syncguard_log.txt", $"SyncChecker 초기화 실패: {ex.GetType().Name}\n오류 메시지: {ex.Message}\n스택 트레이스: {ex.StackTrace}\n\n");
                
                // 사용자에게 알림
                MessageBox.Show($"SyncGuard 초기화 실패:\n\n{ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // 기본 상태로 시작
                syncChecker = null;
                ShowToastNotification("SyncGuard 시작됨 (제한된 모드)", "NVAPI 초기화 실패로 기본 모드로 실행됩니다.");
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
            contextMenu.Items.Add("Sync 상태 확인", null, OnCheckSyncStatus);
            contextMenu.Items.Add("리프레시", null, OnRefreshSyncStatus);
            contextMenu.Items.Add("WMI 클래스 탐색", null, OnExploreWmiClasses);
            contextMenu.Items.Add("-"); // 구분선
            
            // Master/Slave 설정 메뉴
            var roleMenu = new ToolStripMenuItem("역할 설정");
            masterMenuItem = new ToolStripMenuItem("Master", null, OnSetMaster);
            slaveMenuItem = new ToolStripMenuItem("Slave", null, OnSetSlave);
            roleMenu.DropDownItems.Add(masterMenuItem);
            roleMenu.DropDownItems.Add(slaveMenuItem);
            contextMenu.Items.Add(roleMenu);
            
            contextMenu.Items.Add("-"); // 구분선
            contextMenu.Items.Add("종료", null, OnExit);
            
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += OnTrayIconDoubleClick;
            
            // 초기 체크 표시 업데이트
            UpdateRoleMenuCheckmarks();
        }
        
        // 역할 메뉴 체크 표시 업데이트
        private void UpdateRoleMenuCheckmarks()
        {
            if (syncChecker != null)
            {
                var currentRole = syncChecker.GetUserRole();
                masterMenuItem.Checked = (currentRole == SyncGuard.Core.SyncChecker.SyncRole.Master);
                slaveMenuItem.Checked = (currentRole == SyncGuard.Core.SyncChecker.SyncRole.Slave);
            }
            else
            {
                // SyncChecker가 없으면 기본값(Slave)으로 체크
                masterMenuItem.Checked = false;
                slaveMenuItem.Checked = true;
            }
        }

        private void InitializeSyncTimer()
        {
            syncTimer = new System.Windows.Forms.Timer();
            syncTimer.Interval = 5000; // 5초마다 체크
            syncTimer.Tick += OnSyncTimerTick;
            syncTimer.Start();
        }

        private void OnSyncTimerTick(object sender, EventArgs e)
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
                    ShowToastNotification("Sync 상태 변경", message);
                    lastStatus = status;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sync 체크 중 오류: {ex.Message}");
                UpdateTrayIcon(SyncChecker.SyncStatus.Unknown);
            }
        }

        private void UpdateTrayIcon(SyncChecker.SyncStatus status)
        {
            // 상태에 따른 아이콘 색상 변경
            Color iconColor = status switch
            {
                SyncChecker.SyncStatus.Synced => Color.Green,      // 초록색: 동기화됨
                SyncChecker.SyncStatus.Free => Color.Blue,         // 파란색: 동기화 안됨
                SyncChecker.SyncStatus.ConfigConflict => Color.Red, // 빨간색: 설정 충돌
                SyncChecker.SyncStatus.Unknown => Color.Yellow,    // 노란색: 알 수 없음
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
                SyncChecker.SyncStatus.Synced => "Sync Synced (동기화됨)",
                SyncChecker.SyncStatus.Free => "Sync Free (동기화 안됨)",
                SyncChecker.SyncStatus.ConfigConflict => "Sync Config Conflict (설정 충돌)",
                SyncChecker.SyncStatus.Unknown => "Sync Unknown (알 수 없음)",
                _ => "Sync Unknown (알 수 없음)"
            };
        }

        private void ShowToastNotification(string title, string message)
        {
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

        private void OnCheckSyncStatus(object sender, EventArgs e)
        {
            if (syncChecker == null)
            {
                MessageBox.Show("NVAPI 초기화 실패로 Sync 상태를 확인할 수 없습니다.", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var status = syncChecker.GetSyncStatus();
                string message = GetStatusMessage(status);
                MessageBox.Show($"현재 Sync 상태: {message}", "Sync 상태", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sync 상태 확인 중 오류: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnTrayIconDoubleClick(object sender, EventArgs e)
        {
            OnCheckSyncStatus(sender, e);
        }

        private void OnExit(object sender, EventArgs e)
        {
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
                syncChecker?.Dispose();
                notifyIcon?.Dispose();
            }
            base.OnFormClosing(e);
        }

        private void OnSyncStatusChanged(object sender, SyncChecker.SyncStatus newStatus)
        {
            // 실시간 상태 변경 처리
            UpdateTrayIcon(newStatus);
            string message = GetStatusMessage(newStatus);
            ShowToastNotification("Sync 상태 변경", message);
        }

        private void OnRefreshSyncStatus(object sender, EventArgs e)
        {
            if (syncChecker == null)
            {
                MessageBox.Show("NVAPI 초기화 실패로 Sync 상태를 새로고침할 수 없습니다.", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Console.WriteLine("=== 수동 리프레시 실행 ===");
                syncChecker.RefreshSyncStatus();
                
                // 리프레시 후 상태 확인
                var status = syncChecker.GetSyncStatus();
                string message = GetStatusMessage(status);
                
                // 사용자에게 알림
                ShowToastNotification("Sync 상태 새로고침 완료", message);
                
                Console.WriteLine($"리프레시 후 상태: {status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"리프레시 중 오류: {ex.Message}");
                MessageBox.Show($"Sync 상태 새로고침 중 오류: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSetMaster(object sender, EventArgs e)
        {
            if (syncChecker == null)
            {
                MessageBox.Show("NVAPI 초기화 실패로 역할을 설정할 수 없습니다.", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                syncChecker.SetUserRole(SyncGuard.Core.SyncChecker.SyncRole.Master);
                UpdateRoleMenuCheckmarks(); // 체크 표시 업데이트
                ShowToastNotification("역할 설정", "Master로 설정되었습니다.");
                Console.WriteLine("역할을 Master로 설정했습니다.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Master 설정 중 오류: {ex.Message}");
                MessageBox.Show($"Master 설정 중 오류: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void OnSetSlave(object sender, EventArgs e)
        {
            if (syncChecker != null)
            {
                syncChecker.SetUserRole(SyncGuard.Core.SyncChecker.SyncRole.Slave);
                UpdateRoleMenuCheckmarks();
                ShowToastNotification("역할 설정", "Slave로 설정되었습니다.");
            }
        }
        
        private void OnExploreWmiClasses(object sender, EventArgs e)
        {
            if (syncChecker != null)
            {
                try
                {
                    syncChecker.ExploreNvidiaWmiClasses();
                    ShowToastNotification("WMI 탐색", "NVIDIA WMI 클래스 탐색이 완료되었습니다. 로그를 확인하세요.");
                }
                catch (Exception ex)
                {
                    ShowToastNotification("WMI 탐색 실패", $"오류: {ex.Message}");
                }
            }
        }
    }
}
