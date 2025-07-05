# 🎯 SyncGuard Floating Monitor - 완전 가이드

## 📋 프로젝트 개요
- **이름**: SyncGuard Floating Monitor
- **용도**: SyncGuard 클라이언트들의 동기화 상태를 실시간으로 표시하는 플로팅 윈도우
- **특징**: 항상 최상위, 컴팩트 디자인, Synced/Free 상태 표시

---

## 1️⃣ 완전한 소스 코드

### 📄 FloatingMonitor.cs (메인 파일)
```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SyncGuardFloatingMonitor
{
    public class FloatingMonitor : Form
    {
        // UI 컨트롤
        private Panel titleBar;
        private Label titleLabel;
        private Button closeButton;
        private Button minimizeButton;
        private Panel clientPanel;
        private Label statusLabel;
        private NotifyIcon trayIcon;
        
        // 서버
        private TcpListener tcpListener;
        private CancellationTokenSource cancellationTokenSource;
        private bool isRunning = false;
        private int serverPort = 8080;
        
        // 클라이언트 데이터
        private readonly ConcurrentDictionary<string, ClientInfo> clients = new();
        private readonly System.Windows.Forms.Timer refreshTimer;
        
        public FloatingMonitor()
        {
            InitializeUI();
            InitializeTrayIcon();
            LoadSettings();
            
            // 1초마다 UI 새로고침
            refreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            refreshTimer.Tick += (s, e) => RefreshUI();
            refreshTimer.Start();
            
            // 자동 시작
            _ = StartServerAsync();
        }
        
        private void InitializeUI()
        {
            // 폼 설정
            this.Text = "";
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(260, 180);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Right - 270, 10);
            this.TopMost = true;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ShowInTaskbar = false;
            
            // 둥근 모서리 (Windows 11 스타일)
            this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 10, 10));
            
            // 타이틀바
            titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Color.FromArgb(40, 40, 40),
                Cursor = Cursors.SizeAll
            };
            
            titleLabel = new Label
            {
                Text = "SyncGuard Monitor",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Location = new Point(8, 6),
                AutoSize = true
            };
            
            // 최소화 버튼
            minimizeButton = new Button
            {
                Text = "─",
                Size = new Size(24, 24),
                Location = new Point(204, 2),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 40, 40),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8f)
            };
            minimizeButton.FlatAppearance.BorderSize = 0;
            minimizeButton.Click += (s, e) => { this.Hide(); trayIcon.Visible = true; };
            
            // 닫기 버튼
            closeButton = new Button
            {
                Text = "×",
                Size = new Size(24, 24),
                Location = new Point(230, 2),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 40, 40),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 12f)
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.Red;
            closeButton.Click += (s, e) => Application.Exit();
            
            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(minimizeButton);
            titleBar.Controls.Add(closeButton);
            
            // 클라이언트 패널
            clientPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(5)
            };
            
            // 상태 레이블
            statusLabel = new Label
            {
                Text = "서버 시작 중...",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8f),
                Dock = DockStyle.Bottom,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(25, 25, 25)
            };
            
            this.Controls.Add(clientPanel);
            this.Controls.Add(statusLabel);
            this.Controls.Add(titleBar);
            
            // 드래그 이동
            MakeDraggable();
        }
        
        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "SyncGuard Monitor",
                Visible = false
            };
            
            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("열기", null, (s, e) => { this.Show(); trayIcon.Visible = false; });
            trayMenu.Items.Add("포트 설정", null, ShowPortSettings);
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("종료", null, (s, e) => Application.Exit());
            
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.DoubleClick += (s, e) => { this.Show(); trayIcon.Visible = false; };
        }
        
        private void MakeDraggable()
        {
            bool isDragging = false;
            Point dragStart = Point.Empty;
            
            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    isDragging = true;
                    dragStart = new Point(e.X, e.Y);
                }
            };
            
            titleBar.MouseMove += (s, e) =>
            {
                if (isDragging)
                {
                    Point p = PointToScreen(e.Location);
                    Location = new Point(p.X - dragStart.X, p.Y - dragStart.Y);
                }
            };
            
            titleBar.MouseUp += (s, e) => isDragging = false;
        }
        
        private async Task StartServerAsync()
        {
            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                tcpListener = new TcpListener(IPAddress.Any, serverPort);
                tcpListener.Start();
                
                isRunning = true;
                UpdateStatus($"포트 {serverPort}에서 실행 중");
                
                await Task.Run(() => AcceptClientsAsync());
            }
            catch (Exception ex)
            {
                UpdateStatus($"서버 오류: {ex.Message}");
            }
        }
        
        private async Task AcceptClientsAsync()
        {
            while (isRunning && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await tcpListener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(tcpClient));
                }
                catch
                {
                    if (isRunning) await Task.Delay(100);
                }
            }
        }
        
        private async Task HandleClientAsync(TcpClient client)
        {
            var buffer = new byte[1024];
            var stream = client.GetStream();
            var messageBuffer = new StringBuilder();
            
            try
            {
                while (client.Connected && isRunning)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                    
                    messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                    
                    // 완전한 메시지 처리
                    string data = messageBuffer.ToString();
                    int lastIndex = data.LastIndexOf("\r\n");
                    
                    if (lastIndex >= 0)
                    {
                        string messages = data.Substring(0, lastIndex);
                        messageBuffer.Clear();
                        if (lastIndex + 2 < data.Length)
                            messageBuffer.Append(data.Substring(lastIndex + 2));
                        
                        foreach (var msg in messages.Split(new[] { "\r\n" }, 
                            StringSplitOptions.RemoveEmptyEntries))
                        {
                            ProcessMessage(msg);
                        }
                    }
                }
            }
            catch { }
            finally
            {
                client.Close();
            }
        }
        
        private void ProcessMessage(string message)
        {
            try
            {
                // 형식: "192.168.0.201_state2"
                var parts = message.Split('_');
                if (parts.Length != 2) return;
                
                string ip = parts[0];
                string stateStr = parts[1].Replace("state", "");
                
                ClientState state = stateStr switch
                {
                    "0" => ClientState.Error,
                    "1" => ClientState.Slave,
                    "2" => ClientState.Master,
                    _ => ClientState.Unknown
                };
                
                clients.AddOrUpdate(ip,
                    new ClientInfo { IP = ip, State = state, LastSeen = DateTime.Now },
                    (key, existing) =>
                    {
                        existing.State = state;
                        existing.LastSeen = DateTime.Now;
                        return existing;
                    });
            }
            catch { }
        }
        
        private void RefreshUI()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshUI));
                return;
            }
            
            clientPanel.Controls.Clear();
            
            // 30초 타임아웃 처리
            var now = DateTime.Now;
            foreach (var client in clients.Values)
            {
                if ((now - client.LastSeen).TotalSeconds > 30)
                {
                    client.IsActive = false;
                }
                else
                {
                    client.IsActive = true;
                }
            }
            
            // Master 존재 여부 확인
            bool hasMaster = clients.Values.Any(c => c.IsActive && c.State == ClientState.Master);
            bool hasActiveSlave = clients.Values.Any(c => c.IsActive && c.State == ClientState.Slave);
            bool isSynced = hasMaster && hasActiveSlave;
            
            // 클라이언트 표시
            int y = 5;
            foreach (var client in clients.Values.OrderBy(c => c.IP))
            {
                var label = new ClientLabel
                {
                    Location = new Point(5, y),
                    Size = new Size(245, 22),
                    Client = client,
                    IsSynced = isSynced && client.IsActive && 
                              (client.State == ClientState.Master || client.State == ClientState.Slave)
                };
                
                clientPanel.Controls.Add(label);
                y += 24;
            }
            
            // 상태 업데이트
            int activeCount = clients.Values.Count(c => c.IsActive);
            statusLabel.Text = $"활성: {activeCount}/{clients.Count} | 포트: {serverPort}";
        }
        
        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(UpdateStatus), message);
                return;
            }
            
            statusLabel.Text = message;
        }
        
        private void ShowPortSettings(object sender, EventArgs e)
        {
            using (var dialog = new Form())
            {
                dialog.Text = "포트 설정";
                dialog.Size = new Size(250, 150);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                
                var label = new Label
                {
                    Text = "TCP 포트:",
                    Location = new Point(20, 20),
                    AutoSize = true
                };
                
                var textBox = new TextBox
                {
                    Text = serverPort.ToString(),
                    Location = new Point(20, 45),
                    Size = new Size(190, 20)
                };
                
                var okButton = new Button
                {
                    Text = "확인",
                    Location = new Point(55, 80),
                    Size = new Size(60, 25),
                    DialogResult = DialogResult.OK
                };
                
                var cancelButton = new Button
                {
                    Text = "취소",
                    Location = new Point(125, 80),
                    Size = new Size(60, 25),
                    DialogResult = DialogResult.Cancel
                };
                
                dialog.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (int.TryParse(textBox.Text, out int newPort) && 
                        newPort > 0 && newPort <= 65535)
                    {
                        serverPort = newPort;
                        SaveSettings();
                        MessageBox.Show("포트가 변경되었습니다. 프로그램을 재시작하세요.", 
                                      "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
        
        private void LoadSettings()
        {
            try
            {
                string settingsFile = "monitor_settings.txt";
                if (System.IO.File.Exists(settingsFile))
                {
                    string portStr = System.IO.File.ReadAllText(settingsFile).Trim();
                    if (int.TryParse(portStr, out int port))
                    {
                        serverPort = port;
                    }
                }
            }
            catch { }
        }
        
        private void SaveSettings()
        {
            try
            {
                System.IO.File.WriteAllText("monitor_settings.txt", serverPort.ToString());
            }
            catch { }
        }
        
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            isRunning = false;
            cancellationTokenSource?.Cancel();
            tcpListener?.Stop();
            refreshTimer?.Stop();
            trayIcon?.Dispose();
            base.OnFormClosed(e);
        }
        
        // Win32 API for rounded corners
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int width, int height);
    }
    
    // 클라이언트 정보
    public class ClientInfo
    {
        public string IP { get; set; } = "";
        public ClientState State { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsActive { get; set; } = true;
    }
    
    public enum ClientState
    {
        Unknown,
        Error,    // state0
        Slave,    // state1
        Master    // state2
    }
    
    // 커스텀 클라이언트 레이블
    public class ClientLabel : Control
    {
        public ClientInfo Client { get; set; }
        public bool IsSynced { get; set; }
        
        public ClientLabel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.UserPaint | 
                    ControlStyles.DoubleBuffer, true);
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // 상태 아이콘
            Color iconColor;
            if (!Client.IsActive)
            {
                iconColor = Color.FromArgb(80, 80, 80); // 회색
            }
            else
            {
                iconColor = Client.State switch
                {
                    ClientState.Master => Color.FromArgb(0, 255, 0),    // 초록
                    ClientState.Slave => Color.FromArgb(255, 200, 0),   // 노랑
                    ClientState.Error => Color.FromArgb(255, 0, 0),     // 빨강
                    _ => Color.FromArgb(100, 100, 100)
                };
            }
            
            using (var brush = new SolidBrush(iconColor))
            {
                g.FillEllipse(brush, 4, 5, 12, 12);
            }
            
            // IP 주소
            using (var font = new Font("Consolas", 10f))
            {
                var ipColor = Client.IsActive ? Color.White : Color.FromArgb(128, 128, 128);
                using (var brush = new SolidBrush(ipColor))
                {
                    g.DrawString(Client.IP, font, brush, 22, 3);
                }
            }
            
            // 동기화 상태
            string syncText;
            Color syncColor;
            
            if (!Client.IsActive)
            {
                syncText = "---";
                syncColor = Color.FromArgb(80, 80, 80);
            }
            else if (Client.State == ClientState.Error)
            {
                syncText = "Free";
                syncColor = Color.FromArgb(255, 100, 100);
            }
            else if (IsSynced && (Client.State == ClientState.Master || Client.State == ClientState.Slave))
            {
                syncText = "Synced";
                syncColor = Color.FromArgb(100, 255, 100);
            }
            else
            {
                syncText = "Free";
                syncColor = Color.FromArgb(200, 200, 0);
            }
            
            using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var brush = new SolidBrush(syncColor))
            {
                var textSize = g.MeasureString(syncText, font);
                g.DrawString(syncText, font, brush, Width - textSize.Width - 5, 3);
            }
        }
    }
    
    // 프로그램 진입점
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FloatingMonitor());
        }
    }
}
```

### 📄 프로젝트 파일 (.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ApplicationIcon>icon.ico</ApplicationIcon>
    <AssemblyName>SyncGuardMonitor</AssemblyName>
    <Product>SyncGuard Floating Monitor</Product>
    <Version>1.0.0</Version>
  </PropertyGroup>

</Project>
```

---

## 2️⃣ 빌드 가이드

### 📦 방법 1: Visual Studio 사용
1. Visual Studio 2022 설치
2. 새 프로젝트 → Windows Forms 앱 (.NET)
3. FloatingMonitor.cs 내용 복사
4. F5로 실행 또는 Release 빌드

### 📦 방법 2: 명령줄 빌드
```bash
# 1. 프로젝트 폴더 생성
mkdir SyncGuardMonitor
cd SyncGuardMonitor

# 2. 파일 저장
# FloatingMonitor.cs 파일로 저장

# 3. 프로젝트 파일 생성
dotnet new winforms -n SyncGuardMonitor

# 4. 빌드
dotnet build -c Release

# 5. 단일 실행 파일 생성
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## 3️⃣ Inno Setup 설치 프로그램

### 📄 setup.iss
```pascal
#define MyAppName "SyncGuard Floating Monitor"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Your Company"
#define MyAppExeName "SyncGuardMonitor.exe"

[Setup]
AppId={{A1234567-B890-C123-D456-E78901234567}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\SyncGuardMonitor
DefaultGroupName=SyncGuard Monitor
DisableProgramGroupPage=yes
OutputDir=installer
OutputBaseFilename=SyncGuardMonitor_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startup"; Description: "시작할 때 자동 실행"; GroupDescription: "추가 옵션:"

[Files]
Source: "bin\Release\net6.0-windows\SyncGuardMonitor.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "icon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "SyncGuardMonitor"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;
```

---

## 4️⃣ 설치 프로그램 만들기

### 🛠️ 준비물
1. **Inno Setup** 다운로드: https://jrsoftware.org/isdl.php
2. 빌드된 실행 파일

### 📝 단계별 가이드

#### 1단계: 프로젝트 빌드
```bash
cd SyncGuardMonitor
dotnet publish -c Release -r win-x64 --self-contained false
```

#### 2단계: 폴더 구조
```
SyncGuardMonitor/
├── bin/
│   └── Release/
│       └── net6.0-windows/
│           └── SyncGuardMonitor.exe
├── installer/
├── setup.iss
└── icon.ico
```

#### 3단계: Inno Setup 실행
1. Inno Setup Compiler 실행
2. File → Open → setup.iss 선택
3. Build → Compile (Ctrl+F9)
4. `installer` 폴더에 `SyncGuardMonitor_Setup.exe` 생성됨

---

## 5️⃣ 사용 방법

### 🚀 실행
1. 설치 프로그램 실행 또는 exe 직접 실행
2. 우측 상단에 플로팅 윈도우 표시
3. SyncGuard 클라이언트들이 연결되면 자동으로 표시

### ⚙️ 설정
- **포트 변경**: 트레이 아이콘 우클릭 → 포트 설정
- **위치 변경**: 타이틀바 드래그
- **최소화**: ─ 버튼 클릭 (트레이로 이동)

### 📊 상태 표시
- 🟢 **Master + Synced**: 동기화 그룹의 마스터
- 🟡 **Slave + Synced**: 동기화 그룹의 슬레이브
- 🟡 **Slave + Free**: 동기화되지 않은 슬레이브
- 🔴 **Error + Free**: 동기화 오류
- ⚫ **---**: 30초 이상 신호 없음 (오프라인)

---

## 6️⃣ 문제 해결

### ❓ 자주 묻는 질문

**Q: 서버가 시작되지 않아요**
- A: 포트가 이미 사용 중일 수 있습니다. 다른 포트로 변경해보세요.

**Q: 클라이언트가 표시되지 않아요**
- A: Windows 방화벽에서 포트를 허용해야 합니다.
```bash
netsh advfirewall firewall add rule name="SyncGuard Monitor" dir=in action=allow protocol=TCP localport=8080
```

**Q: 창이 보이지 않아요**
- A: 시스템 트레이를 확인하세요. 아이콘을 더블클릭하면 나타납니다.

---

## 7️⃣ 최종 체크리스트

✅ **개발 완료**
- [ ] FloatingMonitor.cs 코드 작성
- [ ] 프로젝트 빌드
- [ ] 실행 테스트

✅ **배포 준비**
- [ ] Inno Setup 설치
- [ ] setup.iss 파일 준비
- [ ] 설치 프로그램 생성
- [ ] 설치 테스트

✅ **문서화**
- [ ] 사용 설명서 작성
- [ ] 버전 정보 업데이트
- [ ] 배포

---

이제 완성입니다! 🎉 간단하게 따라하실 수 있도록 모든 코드와 가이드를 정리했습니다.