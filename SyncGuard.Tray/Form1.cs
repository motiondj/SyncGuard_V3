using System;
using System.Drawing;
using System.Windows.Forms;
using SyncGuard.Core;
using System.Runtime.Versioning;
using System.IO;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace SyncGuard.Tray
{
    [SupportedOSPlatform("windows")]
public partial class Form1 : Form
{
        private NotifyIcon? notifyIcon;
        private SyncChecker? syncChecker;
        private System.Windows.Forms.Timer? syncTimer;
        private SyncChecker.SyncStatus lastStatus = SyncChecker.SyncStatus.Unknown;
        private bool isTcpClientEnabled = false;
        private int tcpServerPort = 8080;
        private string targetIpAddress = "127.0.0.1";
        private readonly string configFilePath = "syncguard_config.txt";
        
    public Form1()
    {
        InitializeComponent();
            
            // 설정 파일에서 값 불러오기
            LoadConfig();
            
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
                Logger.Info("SyncChecker 초기화 시작...");
                syncChecker = new SyncChecker();
                
                // TCP 클라이언트 자동 시작 (저장된 설정으로)
                StartTcpClient();
                
                // Sync 상태 변경 이벤트 구독
                syncChecker.SyncStatusChanged += OnSyncStatusChanged;
                
                Logger.Info("SyncChecker 초기화 성공!");
                
                InitializeSyncTimer();
                ShowToastNotification("SyncGuard 시작됨", "Quadro Sync 모니터링이 시작되었습니다.");
            }
            catch (Exception ex)
            {
                // 상세한 오류 정보 로그
                Logger.Error($"SyncChecker 초기화 실패: {ex.GetType().Name}");
                Logger.Error($"오류 메시지: {ex.Message}");
                Logger.Error($"스택 트레이스: {ex.StackTrace}");
                
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
                Logger.Info("SyncGuard 로그 시스템 초기화 완료");
            }
            catch (Exception ex)
            {
                // 로그 초기화 실패 시 콘솔에 출력
                Console.WriteLine($"로그 시스템 초기화 실패: {ex.Message}");
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
            
            // TCP 서버 상태
            var tcpStatusItem = contextMenu.Items.Add($"TCP 서버: {(isTcpClientEnabled ? "활성" : "비활성")}");
            tcpStatusItem.Enabled = false;
            
            contextMenu.Items.Add("-"); // 구분선
            contextMenu.Items.Add("설정...", null, OnSettings);
            contextMenu.Items.Add("리프레시", null, OnRefreshSyncStatus);
            contextMenu.Items.Add("-"); // 구분선
            contextMenu.Items.Add("종료", null, OnExit);
            
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += OnTrayIconDoubleClick;
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
                Size = new Size(400, 300),
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

            // 외부 전송 활성화 체크박스
            var chkEnable = new CheckBox { Text = "외부 전송 활성화", Location = new Point(20, 80), Size = new Size(150, 20), Checked = true };

            // 연결 테스트 버튼
            var btnTest = new Button { Text = "연결 테스트", Location = new Point(20, 110), Size = new Size(100, 30) };
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
            var btnSave = new Button { Text = "저장", Location = new Point(200, 200), Size = new Size(80, 30) };
            btnSave.Click += (s, e) =>
            {
                try
                {
                    tcpServerPort = int.Parse(txtPort.Text);
                    targetIpAddress = txtIp.Text;
                    Logger.Info($"설정 저장: IP={targetIpAddress}, Port={tcpServerPort}");
                    
                    // 설정 파일에 저장
                    SaveConfig();
                    
                    // TCP 클라이언트 재시작
                    StopTcpClient();
                    StartTcpClient();
                    
                    settingsForm.Close();
                }
                catch
                {
                    MessageBox.Show("잘못된 설정입니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // 취소 버튼
            var btnCancel = new Button { Text = "취소", Location = new Point(290, 200), Size = new Size(80, 30) };
            btnCancel.Click += (s, e) => settingsForm.Close();

            // 컨트롤 추가
            settingsForm.Controls.AddRange(new Control[] { lblIp, txtIp, lblPort, txtPort, chkEnable, btnTest, btnSave, btnCancel });

            settingsForm.ShowDialog();
        }

        private void UpdateTrayMenu()
        {
            if (notifyIcon?.ContextMenuStrip == null) return;
            
            try
            {
                var contextMenu = notifyIcon.ContextMenuStrip;
                
                // TCP 서버 상태 업데이트 (첫 번째 항목)
                if (contextMenu.Items.Count > 0)
                {
                    contextMenu.Items[0].Text = $"TCP 서버: {(isTcpClientEnabled ? "활성" : "비활성")}";
                }
                
                Logger.Info("트레이 메뉴 업데이트 완료");
            }
            catch (Exception ex)
            {
                Logger.Error($"트레이 메뉴 업데이트 실패: {ex.Message}");
            }
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
                Logger.Info($"감지된 상태: {status}, 이전 상태: {lastStatus}");
                
                // 항상 트레이 아이콘 업데이트
                UpdateTrayIcon(status);
                
                // 상태 변경 시 또는 초기 상태일 때 알림 및 메뉴 업데이트
                if (status != lastStatus || lastStatus == SyncChecker.SyncStatus.Unknown)
                {
                    string message = GetStatusMessage(status);
                    Logger.Info($"Sync 상태 변경: {lastStatus} -> {status}");
                    ShowToastNotification("Sync 상태 변경", message);
                    lastStatus = status;
                }
                
                // 주기적으로 TCP 전송 (상태 변경 여부와 관계없이)
                if (isTcpClientEnabled && syncChecker != null)
                {
                    _ = Task.Run(async () => 
                    {
                        try
                        {
                            await syncChecker.SendStatusToServer();
                            Logger.Info("주기적 TCP 전송 완료");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"주기적 TCP 전송 실패: {ex.Message}");
                        }
                    });
                }
                
                // 항상 트레이 메뉴 업데이트 (상태 표시를 위해)
                UpdateTrayMenu();
            }
            catch (Exception ex)
            {
                Logger.Error($"Sync 체크 중 오류: {ex.Message}");
                UpdateTrayIcon(SyncChecker.SyncStatus.Unknown);
            }
        }

        private void UpdateTrayIcon(SyncChecker.SyncStatus status)
        {
            if (notifyIcon == null) return;
            
            // 디버깅: 상태값 로그 추가
            Logger.Info($"UpdateTrayIcon 호출됨 - 상태: {status}");
            
            // 상태에 따른 아이콘 색상 변경
            Color iconColor = status switch
            {
                SyncChecker.SyncStatus.Master => Color.Green,      // 초록색: 마스터 (State 2)
                SyncChecker.SyncStatus.Slave => Color.Yellow,      // 노랑색: 슬레이브 (State 1)
                SyncChecker.SyncStatus.Error => Color.Red,         // 빨간색: 오류
                SyncChecker.SyncStatus.Unknown => Color.Red,       // 빨간색: 알 수 없음
                _ => Color.Red
            };

            // 디버깅: 선택된 색상 로그 추가
            Logger.Info($"선택된 색상: {iconColor.Name}");

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
            // 더블클릭 시 리프레시 실행
            OnRefreshSyncStatus(sender, e);
        }

        private void OnExit(object? sender, EventArgs e)
        {
            Logger.Info("SyncGuard 종료됨");
            syncChecker?.Dispose();
            notifyIcon?.Dispose();
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // TCP 서버 중지
                StopTcpClient();
                
                // 타이머 중지
                syncTimer?.Stop();
                syncTimer?.Dispose();
                
                // SyncChecker 정리
                if (syncChecker != null)
                {
                    syncChecker.SyncStatusChanged -= OnSyncStatusChanged;
                    syncChecker.Dispose();
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
                
                // UI 스레드에서 실행
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => OnSyncStatusChanged(sender, newStatus)));
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
                var (serverIP, serverPort) = ConfigManager.LoadConfig();
                targetIpAddress = serverIP;
                tcpServerPort = serverPort;
                Logger.Info($"설정 로드 완료: IP={targetIpAddress}, Port={tcpServerPort}");
            }
            catch (Exception ex)
            {
                Logger.Error($"설정 로드 실패: {ex.Message}");
            }
        }
        
        // 설정 파일에 값 저장하기
        private void SaveConfig()
        {
            try
            {
                ConfigManager.SaveConfig(targetIpAddress, tcpServerPort);
                Logger.Info($"설정 저장 완료: IP={targetIpAddress}, Port={tcpServerPort}");
            }
            catch (Exception ex)
            {
                Logger.Error($"설정 저장 실패: {ex.Message}");
            }
        }
    }
}
