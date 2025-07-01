using System;
using System.Drawing;
using System.Windows.Forms;
using SyncGuardMonitor.Core;
using SyncGuardMonitor.Models;
using SyncGuardMonitor.Services;
using SyncGuardMonitor.UI;

namespace SyncGuardMonitor
{
    public partial class MainForm : Form
    {
        private TcpServer? tcpServer = null!;
        private DataManager? dataManager = null!;
        private UIUpdateManager? uiManager = null!;
        private System.Windows.Forms.Timer? refreshTimer = null!;
        
        // UI 컨트롤
        private DataGridView dgvClients = null!;
        private RichTextBox rtbLog = null!;
        private ToolStripStatusLabel statusLabel = null!;
        private Label lblStats = null!;
        private Button btnStart = null!;
        private Button btnStop = null!;
        private TextBox txtPort = null!;
        private Label lblPort = null!;
        private Label lblStatus = null!;
        private StatusStrip statusStrip = null!;
        
        public MainForm()
        {
            InitializeComponent();
            InitializeUI();
            SetupEventHandlers();
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // 폼 설정
            this.Text = "SyncGuard Monitor v1.0 - 독립 모니터링 소프트웨어";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 600);
            
            // 컨트롤 생성
            CreateControls();
            LayoutControls();
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        
        private void CreateControls()
        {
            // 포트 입력
            lblPort = new Label
            {
                Text = "포트:",
                Location = new Point(10, 15),
                Size = new Size(40, 20)
            };
            
            txtPort = new TextBox
            {
                Text = "8080",
                Location = new Point(55, 12),
                Size = new Size(60, 23),
                MaxLength = 5
            };
            
            // 시작 버튼
            btnStart = new Button
            {
                Text = "▶ 시작",
                Location = new Point(125, 10),
                Size = new Size(80, 28),
                BackColor = Color.LightGreen
            };
            
            // 중지 버튼
            btnStop = new Button
            {
                Text = "■ 중지",
                Location = new Point(210, 10),
                Size = new Size(80, 28),
                BackColor = Color.LightCoral,
                Enabled = false
            };
            
            // 상태 레이블
            lblStatus = new Label
            {
                Text = "서버 상태: ● 중지됨",
                Location = new Point(300, 15),
                Size = new Size(200, 20),
                ForeColor = Color.Red
            };
            
            // 통계 레이블
            lblStats = new Label
            {
                Text = "클라이언트: 0/0 | 메시지: 0 (0.0/s)",
                Location = new Point(510, 15),
                Size = new Size(400, 20)
            };
            
            // 클라이언트 그리드
            dgvClients = new DataGridView
            {
                Location = new Point(10, 50),
                Size = new Size(1160, 400),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            
            // 로그 박스
            rtbLog = new RichTextBox
            {
                Location = new Point(10, 460),
                Size = new Size(1160, 250),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = new Font("Consolas", 9)
            };
            
            // 상태바
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("준비");
            statusStrip.Items.Add(statusLabel);
            
            // 컨트롤 추가
            this.Controls.AddRange(new Control[]
            {
                lblPort, txtPort, btnStart, btnStop, lblStatus, lblStats,
                dgvClients, rtbLog, statusStrip
            });
        }
        
        private void LayoutControls()
        {
            // 상태바 위치 설정
            statusStrip.Location = new Point(0, this.ClientSize.Height - statusStrip.Height);
            statusStrip.Size = new Size(this.ClientSize.Width, statusStrip.Height);
        }
        
        private void InitializeUI()
        {
            // 타이머 설정 (UI 새로고침용)
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 1000; // 1초마다
            refreshTimer.Tick += RefreshTimer_Tick;
            
            // 초기 UI 상태
            UpdateServerStatusUI(false);
        }
        
        private void SetupEventHandlers()
        {
            // 버튼 이벤트
            btnStart.Click += BtnStart_Click;
            btnStop.Click += BtnStop_Click;
            txtPort.KeyPress += TxtPort_KeyPress;
            
            // 폼 이벤트
            this.FormClosing += MainForm_FormClosing;
            this.Resize += MainForm_Resize;
        }
        
        // 서버 시작 버튼 클릭
        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!int.TryParse(txtPort.Text, out int port) || port < 1 || port > 65535)
                {
                    MessageBox.Show("유효한 포트 번호를 입력하세요 (1-65535)", "오류", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // UI 상태 변경
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                txtPort.Enabled = false;
                
                // 서버 초기화
                tcpServer = new TcpServer();
                dataManager = DataManager.Instance;
                uiManager = new UIUpdateManager(dgvClients, rtbLog, statusLabel, lblStats);
                
                // 이벤트 연결
                tcpServer.MessageReceived += OnMessageReceived;
                tcpServer.ClientConnected += OnClientConnected;
                tcpServer.ClientDisconnected += OnClientDisconnected;
                tcpServer.ServerStatusChanged += OnServerStatusChanged;
                
                dataManager.ClientUpdated += OnClientUpdated;
                dataManager.StateChanged += OnStateChanged;
                
                // 서버 시작
                await tcpServer.StartAsync(port);
                
                // 타이머 시작
                refreshTimer?.Start();
                
                uiManager.AddLog($"[시작버튼] 서버가 포트 {port}에서 시작되었습니다", LogLevel.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"서버 시작 실패: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // UI 원복
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                txtPort.Enabled = true;
                
                // 에러 로그 추가
                if (uiManager != null)
                {
                    uiManager.AddLog($"[시작버튼-에러] {ex.Message}", LogLevel.Error);
                }
            }
        }
        
        // 서버 중지 버튼 클릭
        private async void BtnStop_Click(object? sender, EventArgs e)
        {
            try
            {
                // UI 상태 변경
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                txtPort.Enabled = true;
                
                // 디버깅 로그
                uiManager?.AddLog("[중지버튼] 중지 시작...", LogLevel.Debug);
                
                // 서버 중지
                if (tcpServer != null)
                {
                    uiManager?.AddLog("[중지버튼] tcpServer.StopAsync() 호출", LogLevel.Debug);
                    await tcpServer.StopAsync();
                    uiManager?.AddLog("[중지버튼] tcpServer.StopAsync() 완료", LogLevel.Debug);
                }
                else
                {
                    uiManager?.AddLog("[중지버튼] tcpServer가 null입니다", LogLevel.Warning);
                }
                
                // 타이머 중지
                refreshTimer?.Stop();
                uiManager?.AddLog("[중지버튼] 타이머 중지됨", LogLevel.Debug);
                
                uiManager?.AddLog("[중지버튼] 서버가 중지되었습니다", LogLevel.Info);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"서버 중지 실패: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // 에러 로그 추가
                uiManager?.AddLog($"[중지버튼-에러] {ex.Message}", LogLevel.Error);
            }
        }
        
        // 메시지 수신 이벤트 처리
        private void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
        {
            try
            {
                // 메시지 파싱
                var syncMessage = SyncMessage.Parse(e.Message, e.ClientId);
                
                // 데이터 업데이트
                dataManager?.ProcessMessage(syncMessage, e.ClientId);
                
                // 로그 추가
                uiManager?.AddLog($"[{e.ClientId}] 수신: {e.Message}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                uiManager?.AddLog($"메시지 처리 오류: {ex.Message}", LogLevel.Error);
            }
        }
        
        // 클라이언트 업데이트 이벤트 처리
        private void OnClientUpdated(object? sender, ClientUpdateEventArgs e)
        {
            // UI 업데이트
            uiManager?.UpdateClient(e.Client);
            
            if (e.IsNewClient)
            {
                uiManager?.AddLog($"새 클라이언트 추가: {e.Client.IpAddress}", LogLevel.Info);
            }
        }
        
        // 상태 변경 이벤트 처리
        private void OnStateChanged(object? sender, StateChangeEventArgs e)
        {
            var message = $"상태 변경: {e.Client.IpAddress} - {e.PreviousState.ToDisplayString()} → {e.NewState.ToDisplayString()}";
            uiManager?.AddLog(message, LogLevel.Success);
        }
        
        // 클라이언트 연결 이벤트
        private void OnClientConnected(object? sender, ClientConnectionEventArgs e)
        {
            uiManager?.AddLog($"클라이언트 연결됨: {e.ClientId}", LogLevel.Info);
        }
        
        // 클라이언트 연결 해제 이벤트
        private void OnClientDisconnected(object? sender, ClientConnectionEventArgs e)
        {
            uiManager?.AddLog($"클라이언트 연결 해제: {e.ClientId}", LogLevel.Info);
        }
        
        // 서버 상태 변경 이벤트
        private void OnServerStatusChanged(object? sender, ServerStatusEventArgs e)
        {
            UpdateServerStatusUI(e.IsRunning);
        }
        
        // 타이머 틱 이벤트 (1초마다)
        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            // 통계 업데이트
            if (dataManager != null)
            {
                var stats = dataManager.GetStatistics();
                uiManager?.UpdateStatistics(stats);
                
                // 비활성 클라이언트 표시 업데이트
                foreach (var client in dataManager.GetAllClients())
                {
                    uiManager?.UpdateClient(client);
                }
            }
        }
        
        // 서버 상태 UI 업데이트
        private void UpdateServerStatusUI(bool isRunning)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateServerStatusUI(isRunning)));
                return;
            }
            
            if (isRunning)
            {
                lblStatus.Text = "서버 상태: ● 실행중";
                lblStatus.ForeColor = Color.Green;
            }
            else
            {
                lblStatus.Text = "서버 상태: ● 중지됨";
                lblStatus.ForeColor = Color.Red;
            }
        }
        
        // 포트 입력 제한
        private void TxtPort_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
        
        // 폼 크기 변경
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            LayoutControls();
        }
        
        // 폼 종료
        private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                if (tcpServer?.IsRunning == true)
                {
                    await tcpServer.StopAsync();
                }
                
                refreshTimer?.Stop();
                uiManager?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"폼 종료 중 오류: {ex.Message}");
            }
        }
    }
} 