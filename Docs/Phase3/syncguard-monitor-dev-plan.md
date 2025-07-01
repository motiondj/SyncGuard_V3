# 📋 SyncGuard Monitor - 개발계획서

## 1. 프로젝트 개요

### 1.1 프로젝트명
**SyncGuard Monitor** (테스트용 수신 모니터링 소프트웨어)

### 1.2 목적
- SyncGuard에서 전송하는 TCP 메시지를 수신하여 실시간 모니터링
- 여러 PC의 Sync 상태를 한눈에 파악
- 상태 변화를 시각적으로 표현

### 1.3 핵심 요구사항
- TCP 서버 기능 (포트 설정 가능)
- IP별 상태 실시간 표시
- 상태별 색상 구분 (Master/Slave/Error)
- GUI 기반 인터페이스

---

## 2. 기능 명세

### 2.1 서버 기능
```
✅ TCP 서버 시작/중지
✅ 포트 설정 (기본: 8080)
✅ 다중 클라이언트 동시 접속 지원
✅ 비동기 메시지 수신
✅ 연결 관리 (접속/해제 감지)
```

### 2.2 데이터 처리
```
✅ 메시지 파싱 (IP_state 형식)
✅ IP별 데이터 저장 및 관리
✅ 타임스탬프 자동 기록
✅ 상태 변경 감지
✅ 수신 데이터 검증
```

### 2.3 UI 기능
```
✅ 실시간 그리드 업데이트
✅ 상태별 색상 표시
✅ 정렬 및 필터링
✅ 상세 정보 표시
✅ 로그 뷰어
```

### 2.4 부가 기능
```
✅ 로그 파일 저장
✅ 설정 저장/불러오기
✅ 데이터 내보내기 (CSV)
✅ 연결 통계 표시
```

---

## 3. 기술 스택

### 3.1 개발 환경
- **언어**: C# (.NET 6.0)
- **UI 프레임워크**: Windows Forms
- **IDE**: Visual Studio 2022
- **타겟 OS**: Windows 10/11 (64bit)

### 3.2 주요 라이브러리
- **System.Net.Sockets**: TCP 서버
- **System.Threading.Tasks**: 비동기 처리
- **System.Windows.Forms**: GUI
- **System.Text.Json**: 설정 저장
- **System.IO**: 파일 처리

---

## 4. 화면 설계

### 4.1 메인 화면 레이아웃
```
┌─────────────────────────────────────────────────────┐
│  SyncGuard Monitor v1.0              [─] [□] [X]   │
├─────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────┐ │
│ │ 서버 상태: ● 실행중  포트: [8080] [시작] [중지] │ │
│ │ 연결된 클라이언트: 3개  총 수신: 1,234 패킷    │ │
│ └─────────────────────────────────────────────────┘ │
│                                                     │
│ ┌─────────────────────────────────────────────────┐ │
│ │ IP 주소      상태    마지막수신   지속시간  상태│ │
│ │ ─────────────────────────────────────────────── │ │
│ │ 192.168.0.201 Master 14:23:45    00:05:23   ●  │ │
│ │ 192.168.0.202 Slave  14:23:44    00:05:22   ●  │ │
│ │ 192.168.0.203 Error  14:23:20    00:00:00   ●  │ │
│ │ 192.168.0.204 -      14:20:00    -          ●  │ │
│ └─────────────────────────────────────────────────┘ │
│                                                     │
│ ┌─────────────────────────────────────────────────┐ │
│ │ [로그]                                          │ │
│ │ 14:23:45 - 192.168.0.201 상태 변경: Slave→Master│ │
│ │ 14:23:44 - 192.168.0.202 연결됨                │ │
│ │ 14:23:20 - 192.168.0.203 상태 변경: Master→Error│ │
│ └─────────────────────────────────────────────────┘ │
│ [상태바] 서버 실행중 | 수신률: 3msg/s | CPU: 2%    │
└─────────────────────────────────────────────────────┘
```

### 4.2 컨트롤 상세

#### 상단 패널 (ControlPanel)
- **서버 상태 표시등**: PictureBox (초록/빨강)
- **포트 입력**: TextBox (숫자만 입력)
- **시작 버튼**: Button (서버 시작)
- **중지 버튼**: Button (서버 중지)
- **통계 레이블**: Label (연결 수, 패킷 수)

#### 메인 그리드 (DataGridView)
- **컬럼 구성**:
  - IP 주소 (string): 클라이언트 IP
  - 상태 (string): Master/Slave/Error/Unknown
  - 마지막 수신 (DateTime): 최근 메시지 시간
  - 지속 시간 (TimeSpan): 현재 상태 유지 시간
  - 상태 표시 (Image): 색상 아이콘

#### 로그 뷰어 (RichTextBox)
- **자동 스크롤**: 최신 로그 자동 표시
- **색상 코딩**: 이벤트 타입별 색상
- **최대 라인**: 1000줄 (순환 버퍼)

#### 상태바 (StatusStrip)
- **서버 상태**: ToolStripStatusLabel
- **수신률**: ToolStripStatusLabel
- **시스템 리소스**: ToolStripStatusLabel

### 4.3 메뉴 구성
```
파일(F)
├─ 설정 불러오기
├─ 설정 저장
├─ 로그 내보내기
└─ 종료

보기(V)
├─ 항상 위
├─ 로그 창 표시/숨김
└─ 새로고침

도구(T)
├─ 옵션
├─ 데이터 초기화
└─ 통계

도움말(H)
├─ 사용법
└─ 정보
```

---

## 5. 데이터 구조

### 5.1 클라이언트 정보 클래스
```csharp
public class ClientInfo
{
    public string IpAddress { get; set; }
    public SyncState CurrentState { get; set; }
    public SyncState PreviousState { get; set; }
    public DateTime LastReceived { get; set; }
    public DateTime StateChangedTime { get; set; }
    public TimeSpan StateDuration => DateTime.Now - StateChangedTime;
    public int TotalMessages { get; set; }
    public bool IsActive => (DateTime.Now - LastReceived).TotalSeconds < 30;
    public List<StateHistory> History { get; set; }
}

public enum SyncState
{
    Unknown = -1,
    Error = 0,      // state0
    Slave = 1,      // state1
    Master = 2      // state2
}

public class StateHistory
{
    public DateTime Timestamp { get; set; }
    public SyncState FromState { get; set; }
    public SyncState ToState { get; set; }
}
```

### 5.2 메시지 구조
```csharp
public class SyncMessage
{
    public string RawData { get; set; }
    public string IpAddress { get; set; }
    public SyncState State { get; set; }
    public DateTime ReceivedTime { get; set; }
    
    public static SyncMessage Parse(string data)
    {
        // "192.168.0.201_state2" 파싱
    }
}
```

### 5.3 서버 설정
```csharp
public class ServerConfig
{
    public int Port { get; set; } = 8080;
    public bool AutoStart { get; set; } = true;
    public bool LogToFile { get; set; } = true;
    public string LogPath { get; set; } = "logs";
    public int MaxLogLines { get; set; } = 1000;
    public int InactiveTimeout { get; set; } = 30; // 초
}
```

---

## 6. 클래스 설계

### 6.1 TCP 서버 클래스
```csharp
public class TcpServer
{
    private TcpListener listener;
    private CancellationTokenSource cts;
    private readonly ConcurrentDictionary<string, TcpClient> clients;
    
    public event EventHandler<MessageReceivedEventArgs> MessageReceived;
    public event EventHandler<ClientEventArgs> ClientConnected;
    public event EventHandler<ClientEventArgs> ClientDisconnected;
    
    public async Task StartAsync(int port)
    public async Task StopAsync()
    private async Task AcceptClientsAsync()
    private async Task HandleClientAsync(TcpClient client)
    private void ProcessMessage(string clientIp, string message)
}
```

### 6.2 데이터 관리자
```csharp
public class DataManager
{
    private readonly ConcurrentDictionary<string, ClientInfo> clients;
    
    public event EventHandler<ClientUpdateEventArgs> ClientUpdated;
    
    public void UpdateClient(string ip, SyncState state)
    public ClientInfo GetClient(string ip)
    public IEnumerable<ClientInfo> GetAllClients()
    public void ClearInactiveClients()
    public void ExportToCsv(string filename)
}
```

### 6.3 UI 업데이트 관리자
```csharp
public class UIUpdateManager
{
    private readonly DataGridView grid;
    private readonly RichTextBox logBox;
    private readonly System.Windows.Forms.Timer updateTimer;
    
    public void UpdateGrid(ClientInfo client)
    public void AddLog(string message, LogLevel level)
    public void RefreshAll()
    private Color GetStateColor(SyncState state)
    private void AnimateRow(int rowIndex)
}
```

---

## 7. 핵심 메서드 구현

### 7.1 메시지 수신 처리
```
HandleClientAsync 메서드:
1. NetworkStream에서 데이터 읽기
2. 버퍼에서 완전한 메시지 추출 (\r\n 기준)
3. 메시지 파싱 및 검증
4. DataManager에 업데이트
5. UI 이벤트 발생
6. 예외 처리 및 재연결 대기
```

### 7.2 UI 업데이트 로직
```
UpdateGrid 메서드:
1. IP로 기존 행 검색
2. 없으면 새 행 추가
3. 있으면 데이터 업데이트
4. 상태 변경 시 애니메이션
5. 색상 및 아이콘 업데이트
6. 정렬 유지
```

### 7.3 상태 관리
```
UpdateClient 메서드:
1. IP로 클라이언트 조회
2. 이전 상태 저장
3. 새 상태 적용
4. 상태 변경 시간 기록
5. 히스토리 추가
6. 이벤트 발생
```

---

## 8. 에러 처리

### 8.1 네트워크 에러
- **포트 충돌**: 사용 중인 포트 감지 및 알림
- **연결 끊김**: 자동 클라이언트 제거
- **수신 타임아웃**: 30초 후 비활성 표시

### 8.2 데이터 에러
- **잘못된 형식**: 파싱 실패 시 로그 기록
- **인코딩 문제**: UTF-8 강제 적용
- **버퍼 오버플로**: 메시지 크기 제한

### 8.3 UI 에러
- **크로스 스레드**: Invoke 사용
- **메모리 누수**: 타이머 및 이벤트 정리
- **응답 없음**: 백그라운드 작업 분리

---

## 9. 개발 일정

### Phase 1: 기본 구조 (2일)
- [ ] 프로젝트 생성 및 설정
- [ ] TCP 서버 클래스 구현
- [ ] 메시지 파싱 로직
- [ ] 기본 데이터 구조

### Phase 2: UI 구현 (3일)
- [ ] 메인 폼 디자인
- [ ] DataGridView 설정
- [ ] 컨트롤 패널 구현
- [ ] 로그 뷰어 구현
- [ ] 메뉴 및 상태바

### Phase 3: 기능 통합 (2일)
- [ ] 서버-UI 연결
- [ ] 실시간 업데이트
- [ ] 상태 색상 표시
- [ ] 애니메이션 효과

### Phase 4: 부가 기능 (2일)
- [ ] 설정 저장/불러오기
- [ ] 로그 파일 기록
- [ ] CSV 내보내기
- [ ] 통계 기능

### Phase 5: 테스트 및 마무리 (1일)
- [ ] 다중 클라이언트 테스트
- [ ] 성능 최적화
- [ ] 버그 수정
- [ ] 문서 작성

**총 개발 기간: 10일**

---

## 10. 테스트 계획

### 10.1 단위 테스트
- 메시지 파싱 정확성
- 상태 변경 감지
- 데이터 저장/조회

### 10.2 통합 테스트
- 다중 클라이언트 동시 접속
- 대량 메시지 처리
- 장시간 실행 안정성

### 10.3 UI 테스트
- 그리드 업데이트 성능
- 메모리 사용량 모니터링
- 사용자 인터랙션 응답성

### 10.4 시나리오 테스트
```
1. 10개 클라이언트 동시 연결
2. 초당 10개 메시지 수신
3. 상태 무작위 변경
4. 1시간 연속 실행
5. 네트워크 장애 시뮬레이션
```

---

## 11. 향후 확장 계획

### 11.1 단기 (v1.1)
- 웹 대시보드 추가
- 알림 기능 (상태 변경 시)
- 다국어 지원

### 11.2 중기 (v2.0)
- 양방향 통신 (명령 전송)
- 데이터베이스 연동
- 히스토리 분석 기능

### 11.3 장기 (v3.0)
- 클라우드 모니터링
- 모바일 앱 연동
- AI 기반 이상 감지

---

## 12. 개발 시 주의사항

### 12.1 성능 고려
- UI 업데이트는 최소 100ms 간격
- 로그는 순환 버퍼 사용
- 비활성 클라이언트 자동 정리

### 12.2 보안 고려
- IP 화이트리스트 옵션
- 로컬 네트워크만 허용 옵션
- 로그 파일 암호화

### 12.3 사용성 고려
- 직관적인 색상 코딩
- 툴팁으로 상세 정보 제공
- 단축키 지원

---

## 13. 배포 계획

### 13.1 빌드 설정
- Release 모드 빌드
- 단일 실행 파일 생성
- 디지털 서명 적용

### 13.2 설치 패키지
- 설치 마법사 (NSIS)
- 포터블 버전 제공
- 자동 업데이트 기능

### 13.3 문서화
- 사용자 매뉴얼
- 관리자 가이드
- API 문서 (확장용)

---

## 14. 주요 기능 구현 예시

### 14.1 메시지 파싱 로직
```csharp
public static SyncMessage Parse(string data)
{
    // 예시: "192.168.0.201_state2\r\n"
    data = data.TrimEnd('\r', '\n');
    var parts = data.Split('_');
    
    if (parts.Length != 2)
        throw new FormatException($"Invalid message format: {data}");
    
    var stateStr = parts[1].Replace("state", "");
    if (!int.TryParse(stateStr, out int stateValue))
        throw new FormatException($"Invalid state value: {parts[1]}");
    
    return new SyncMessage
    {
        RawData = data,
        IpAddress = parts[0],
        State = (SyncState)stateValue,
        ReceivedTime = DateTime.Now
    };
}
```

### 14.2 그리드 색상 업데이트
```csharp
private void UpdateRowColor(DataGridViewRow row, SyncState state)
{
    Color backColor = state switch
    {
        SyncState.Master => Color.LightGreen,   // state2
        SyncState.Slave => Color.LightYellow,   // state1
        SyncState.Error => Color.LightCoral,    // state0
        _ => Color.LightGray                    // Unknown
    };
    
    foreach (DataGridViewCell cell in row.Cells)
    {
        cell.Style.BackColor = backColor;
    }
}
```

### 14.3 비동기 메시지 수신
```csharp
private async Task HandleClientAsync(TcpClient client)
{
    var buffer = new byte[1024];
    var stream = client.GetStream();
    var messageBuilder = new StringBuilder();
    
    while (client.Connected && !cancellationToken.IsCancellationRequested)
    {
        try
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            if (bytesRead == 0) break;
            
            messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            
            // 완전한 메시지 처리
            string messages = messageBuilder.ToString();
            int lastIndex = messages.LastIndexOf("\r\n");
            
            if (lastIndex >= 0)
            {
                string completeMessages = messages.Substring(0, lastIndex);
                messageBuilder.Clear();
                messageBuilder.Append(messages.Substring(lastIndex + 2));
                
                // 각 메시지 처리
                foreach (var message in completeMessages.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    ProcessMessage(client.Client.RemoteEndPoint.ToString(), message);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Client handling error: {ex.Message}");
            break;
        }
    }
}
```

## 15. 상세 구현 가이드

### 14.1 Phase 1 상세: TCP 서버 구현

#### TcpServer 클래스 전체 구현
```csharp
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class TcpServer
{
    private TcpListener? listener;
    private CancellationTokenSource? cts;
    private readonly ConcurrentDictionary<string, TcpClient> clients;
    private bool isRunning = false;
    
    // 이벤트 정의
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    public event EventHandler<ClientEventArgs>? ClientConnected;
    public event EventHandler<ClientEventArgs>? ClientDisconnected;
    public event EventHandler<ServerEventArgs>? ServerStatusChanged;
    
    public bool IsRunning => isRunning;
    public int ClientCount => clients.Count;
    
    public TcpServer()
    {
        clients = new ConcurrentDictionary<string, TcpClient>();
    }
    
    // 서버 시작
    public async Task StartAsync(int port)
    {
        if (isRunning)
            throw new InvalidOperationException("Server is already running");
            
        try
        {
            cts = new CancellationTokenSource();
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            
            isRunning = true;
            ServerStatusChanged?.Invoke(this, new ServerEventArgs { IsRunning = true, Port = port });
            
            // 비동기로 클라이언트 연결 대기
            await Task.Run(() => AcceptClientsAsync(cts.Token));
        }
        catch (Exception ex)
        {
            isRunning = false;
            throw new Exception($"Failed to start server: {ex.Message}", ex);
        }
    }
    
    // 서버 중지
    public async Task StopAsync()
    {
        if (!isRunning) return;
        
        isRunning = false;
        cts?.Cancel();
        
        // 모든 클라이언트 연결 종료
        foreach (var client in clients.Values)
        {
            try
            {
                client.Close();
            }
            catch { }
        }
        
        clients.Clear();
        listener?.Stop();
        
        ServerStatusChanged?.Invoke(this, new ServerEventArgs { IsRunning = false });
        
        await Task.Delay(100); // 정리 시간
    }
    
    // 클라이언트 연결 대기
    private async Task AcceptClientsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && isRunning)
        {
            try
            {
                var tcpClient = await listener!.AcceptTcpClientAsync();
                var clientEndPoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";
                
                // 클라이언트 추가
                clients.TryAdd(clientEndPoint, tcpClient);
                
                // 연결 이벤트 발생
                ClientConnected?.Invoke(this, new ClientEventArgs 
                { 
                    ClientIP = clientEndPoint,
                    ConnectedTime = DateTime.Now 
                });
                
                // 비동기로 클라이언트 처리
                _ = Task.Run(() => HandleClientAsync(tcpClient, clientEndPoint, token));
            }
            catch (ObjectDisposedException)
            {
                // 서버 중지 시 정상적인 예외
                break;
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    Logger.Error($"Accept client error: {ex.Message}");
                }
            }
        }
    }
    
    // 클라이언트 메시지 처리
    private async Task HandleClientAsync(TcpClient client, string clientIP, CancellationToken token)
    {
        var buffer = new byte[1024];
        var stream = client.GetStream();
        var messageBuilder = new StringBuilder();
        
        try
        {
            while (client.Connected && !token.IsCancellationRequested)
            {
                // 타임아웃 설정
                stream.ReadTimeout = 5000; // 5초
                
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead == 0)
                {
                    // 연결 종료
                    break;
                }
                
                // 받은 데이터를 문자열로 변환
                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                
                // 완전한 메시지 추출 (\r\n 단위)
                ProcessCompleteMessages(messageBuilder, clientIP);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Client handling error [{clientIP}]: {ex.Message}");
        }
        finally
        {
            // 클라이언트 제거
            clients.TryRemove(clientIP, out _);
            
            try
            {
                client.Close();
            }
            catch { }
            
            // 연결 해제 이벤트
            ClientDisconnected?.Invoke(this, new ClientEventArgs 
            { 
                ClientIP = clientIP,
                DisconnectedTime = DateTime.Now 
            });
        }
    }
    
    // 완전한 메시지 처리
    private void ProcessCompleteMessages(StringBuilder builder, string clientIP)
    {
        string messages = builder.ToString();
        int lastIndex = messages.LastIndexOf("\r\n");
        
        if (lastIndex >= 0)
        {
            // 완전한 메시지들 추출
            string completeMessages = messages.Substring(0, lastIndex);
            
            // 버퍼에서 처리한 부분 제거
            builder.Clear();
            if (lastIndex + 2 < messages.Length)
            {
                builder.Append(messages.Substring(lastIndex + 2));
            }
            
            // 각 메시지 처리
            var messageArray = completeMessages.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var message in messageArray)
            {
                try
                {
                    // 메시지 수신 이벤트 발생
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs
                    {
                        ClientIP = clientIP,
                        Message = message,
                        ReceivedTime = DateTime.Now
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error($"Message processing error: {ex.Message}");
                }
            }
        }
    }
}

// 이벤트 아규먼트 클래스들
public class MessageReceivedEventArgs : EventArgs
{
    public string ClientIP { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime ReceivedTime { get; set; }
}

public class ClientEventArgs : EventArgs
{
    public string ClientIP { get; set; } = "";
    public DateTime ConnectedTime { get; set; }
    public DateTime DisconnectedTime { get; set; }
}

public class ServerEventArgs : EventArgs
{
    public bool IsRunning { get; set; }
    public int Port { get; set; }
}
```

#### 메시지 파싱 클래스 구현
```csharp
public class SyncMessage
{
    public string RawData { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public SyncState State { get; set; }
    public DateTime ReceivedTime { get; set; }
    
    // 메시지 파싱 메서드
    public static SyncMessage Parse(string data, string clientIP)
    {
        try
        {
            // 예시: "192.168.0.201_state2"
            data = data.Trim();
            var parts = data.Split('_');
            
            if (parts.Length != 2)
            {
                throw new FormatException($"Invalid message format: {data}");
            }
            
            // IP 주소 검증
            string messageIP = parts[0];
            if (!IsValidIP(messageIP))
            {
                throw new FormatException($"Invalid IP format: {messageIP}");
            }
            
            // state 값 파싱
            string stateStr = parts[1];
            if (!stateStr.StartsWith("state"))
            {
                throw new FormatException($"Invalid state format: {stateStr}");
            }
            
            stateStr = stateStr.Replace("state", "");
            if (!int.TryParse(stateStr, out int stateValue))
            {
                throw new FormatException($"Invalid state value: {parts[1]}");
            }
            
            // 유효한 state 값인지 확인
            if (!Enum.IsDefined(typeof(SyncState), stateValue))
            {
                stateValue = -1; // Unknown
            }
            
            return new SyncMessage
            {
                RawData = data,
                IpAddress = messageIP,
                State = (SyncState)stateValue,
                ReceivedTime = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            Logger.Error($"Message parsing error: {ex.Message}");
            throw;
        }
    }
    
    // IP 주소 유효성 검사
    private static bool IsValidIP(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;
            
        string[] parts = ip.Split('.');
        if (parts.Length != 4)
            return false;
            
        foreach (string part in parts)
        {
            if (!int.TryParse(part, out int num))
                return false;
            if (num < 0 || num > 255)
                return false;
        }
        
        return true;
    }
}
```

### 14.2 Phase 2 상세: UI 구현

#### 메인 폼 코드 구조
```csharp
public partial class MainForm : Form
{
    private TcpServer? tcpServer;
    private DataManager? dataManager;
    private UIUpdateManager? uiManager;
    private System.Windows.Forms.Timer? refreshTimer;
    private ServerConfig config;
    
    public MainForm()
    {
        InitializeComponent();
        InitializeComponents();
        LoadConfiguration();
        SetupEventHandlers();
    }
    
    private void InitializeComponents()
    {
        // DataGridView 설정
        SetupDataGridView();
        
        // 타이머 설정 (UI 새로고침용)
        refreshTimer = new System.Windows.Forms.Timer();
        refreshTimer.Interval = 1000; // 1초마다
        refreshTimer.Tick += RefreshTimer_Tick;
        
        // 초기 UI 상태
        UpdateServerStatusUI(false);
    }
    
    private void SetupDataGridView()
    {
        // 컬럼 설정
        dgvClients.AutoGenerateColumns = false;
        dgvClients.AllowUserToAddRows = false;
        dgvClients.AllowUserToDeleteRows = false;
        dgvClients.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvClients.MultiSelect = false;
        
        // 컬럼 추가
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colIP",
            HeaderText = "IP 주소",
            DataPropertyName = "IpAddress",
            Width = 120,
            ReadOnly = true
        });
        
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colState",
            HeaderText = "상태",
            DataPropertyName = "StateText",
            Width = 80,
            ReadOnly = true
        });
        
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colLastReceived",
            HeaderText = "마지막 수신",
            DataPropertyName = "LastReceivedText",
            Width = 150,
            ReadOnly = true
        });
        
        dgvClients.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colDuration",
            HeaderText = "지속시간",
            DataPropertyName = "DurationText",
            Width = 100,
            ReadOnly = true
        });
        
        // 상태 아이콘 컬럼
        var iconColumn = new DataGridViewImageColumn
        {
            Name = "colStatus",
            HeaderText = "상태",
            Width = 50,
            ImageLayout = DataGridViewImageCellLayout.Zoom
        };
        dgvClients.Columns.Add(iconColumn);
        
        // 더블 버퍼링 활성화 (깜빡임 방지)
        typeof(DataGridView).InvokeMember("DoubleBuffered",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
            null, dgvClients, new object[] { true });
    }
    
    private void SetupEventHandlers()
    {
        // 버튼 이벤트
        btnStart.Click += BtnStart_Click;
        btnStop.Click += BtnStop_Click;
        txtPort.KeyPress += TxtPort_KeyPress;
        
        // 메뉴 이벤트
        menuFileExit.Click += (s, e) => Application.Exit();
        menuToolsClear.Click += MenuToolsClear_Click;
        menuViewAlwaysOnTop.Click += MenuViewAlwaysOnTop_Click;
        
        // 폼 이벤트
        this.FormClosing += MainForm_FormClosing;
    }
}
```

#### UI 업데이트 관리자 구현
```csharp
public class UIUpdateManager
{
    private readonly DataGridView grid;
    private readonly RichTextBox logBox;
    private readonly ToolStripStatusLabel statusLabel;
    private readonly object lockObject = new object();
    private int messagePerSecond = 0;
    private DateTime lastMessageTime = DateTime.Now;
    
    public UIUpdateManager(DataGridView gridView, RichTextBox logTextBox, ToolStripStatusLabel status)
    {
        grid = gridView;
        logBox = logTextBox;
        statusLabel = status;
    }
    
    // 그리드 업데이트 (스레드 세이프)
    public void UpdateGrid(ClientInfo client)
    {
        if (grid.InvokeRequired)
        {
            grid.BeginInvoke(new Action(() => UpdateGrid(client)));
            return;
        }
        
        lock (lockObject)
        {
            try
            {
                // 기존 행 찾기
                DataGridViewRow? existingRow = null;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.Cells["colIP"].Value?.ToString() == client.IpAddress)
                    {
                        existingRow = row;
                        break;
                    }
                }
                
                if (existingRow == null)
                {
                    // 새 행 추가
                    int index = grid.Rows.Add();
                    existingRow = grid.Rows[index];
                    
                    // 추가 애니메이션
                    AnimateNewRow(existingRow);
                }
                
                // 데이터 업데이트
                UpdateRowData(existingRow, client);
                
                // 색상 업데이트
                UpdateRowColor(existingRow, client.CurrentState);
                
                // 상태 아이콘 업데이트
                UpdateStatusIcon(existingRow, client);
                
                // 상태 변경 시 애니메이션
                if (client.PreviousState != client.CurrentState && client.PreviousState != SyncState.Unknown)
                {
                    AnimateStateChange(existingRow);
                }
                
                // 정렬 유지
                grid.Sort(grid.Columns["colIP"], ListSortDirection.Ascending);
            }
            catch (Exception ex)
            {
                Logger.Error($"Grid update error: {ex.Message}");
            }
        }
    }
    
    // 행 데이터 업데이트
    private void UpdateRowData(DataGridViewRow row, ClientInfo client)
    {
        row.Cells["colIP"].Value = client.IpAddress;
        row.Cells["colState"].Value = GetStateText(client.CurrentState);
        row.Cells["colLastReceived"].Value = client.LastReceived.ToString("yyyy-MM-dd HH:mm:ss");
        row.Cells["colDuration"].Value = FormatDuration(client.StateDuration);
        
        // 비활성 클라이언트 표시
        if (!client.IsActive)
        {
            row.DefaultCellStyle.ForeColor = Color.Gray;
            row.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Italic);
        }
        else
        {
            row.DefaultCellStyle.ForeColor = Color.Black;
            row.DefaultCellStyle.Font = grid.Font;
        }
    }
    
    // 행 색상 업데이트
    private void UpdateRowColor(DataGridViewRow row, SyncState state)
    {
        Color backColor = state switch
        {
            SyncState.Master => Color.FromArgb(144, 238, 144),  // LightGreen
            SyncState.Slave => Color.FromArgb(255, 255, 224),   // LightYellow
            SyncState.Error => Color.FromArgb(255, 182, 193),   // LightPink
            _ => Color.FromArgb(211, 211, 211)                  // LightGray
        };
        
        foreach (DataGridViewCell cell in row.Cells)
        {
            if (cell.ColumnIndex != grid.Columns["colStatus"].Index)
            {
                cell.Style.BackColor = backColor;
            }
        }
    }
    
    // 상태 아이콘 업데이트
    private void UpdateStatusIcon(DataGridViewRow row, ClientInfo client)
    {
        var bitmap = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            Color color = client.CurrentState switch
            {
                SyncState.Master => Color.Green,
                SyncState.Slave => Color.Orange,
                SyncState.Error => Color.Red,
                _ => Color.Gray
            };
            
            if (!client.IsActive)
                color = Color.FromArgb(128, color); // 반투명
            
            using (var brush = new SolidBrush(color))
            {
                g.FillEllipse(brush, 1, 1, 14, 14);
            }
            
            using (var pen = new Pen(Color.Black, 1))
            {
                g.DrawEllipse(pen, 1, 1, 14, 14);
            }
        }
        
        row.Cells["colStatus"].Value = bitmap;
    }
    
    // 로그 추가 (스레드 세이프)
    public void AddLog(string message, LogLevel level = LogLevel.Info)
    {
        if (logBox.InvokeRequired)
        {
            logBox.BeginInvoke(new Action(() => AddLog(message, level)));
            return;
        }
        
        try
        {
            // 시간 추가
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}\r\n";
            
            // 색상 설정
            Color textColor = level switch
            {
                LogLevel.Error => Color.Red,
                LogLevel.Warning => Color.Orange,
                LogLevel.Success => Color.Green,
                LogLevel.Debug => Color.Gray,
                _ => Color.Black
            };
            
            // 로그 추가
            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionLength = 0;
            logBox.SelectionColor = textColor;
            logBox.AppendText(logEntry);
            
            // 최대 라인 수 유지
            if (logBox.Lines.Length > 1000)
            {
                var lines = logBox.Lines.Skip(100).ToArray();
                logBox.Lines = lines;
            }
            
            // 스크롤 최하단으로
            logBox.SelectionStart = logBox.TextLength;
            logBox.ScrollToCaret();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Log error: {ex.Message}");
        }
    }
    
    // 애니메이션 효과
    private async void AnimateStateChange(DataGridViewRow row)
    {
        var originalColor = row.DefaultCellStyle.BackColor;
        
        for (int i = 0; i < 3; i++)
        {
            row.DefaultCellStyle.BackColor = Color.White;
            grid.Refresh();
            await Task.Delay(100);
            
            row.DefaultCellStyle.BackColor = originalColor;
            grid.Refresh();
            await Task.Delay(100);
        }
    }
    
    private async void AnimateNewRow(DataGridViewRow row)
    {
        row.DefaultCellStyle.BackColor = Color.LightBlue;
        grid.Refresh();
        await Task.Delay(500);
        // 색상은 나중에 UpdateRowColor에서 설정됨
    }
    
    // 유틸리티 메서드
    private string GetStateText(SyncState state)
    {
        return state switch
        {
            SyncState.Master => "Master",
            SyncState.Slave => "Slave",
            SyncState.Error => "Error",
            _ => "Unknown"
        };
    }
    
    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays}d {duration.Hours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
        else
            return $"{duration.Hours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }
}

public enum LogLevel
{
    Debug,
    Info,
    Success,
    Warning,
    Error
}
```

### 14.3 Phase 3 상세: 데이터 관리

#### DataManager 클래스 구현
```csharp
public class DataManager
{
    private readonly ConcurrentDictionary<string, ClientInfo> clients;
    private readonly object historyLock = new object();
    
    public event EventHandler<ClientUpdateEventArgs>? ClientUpdated;
    public event EventHandler<StateChangeEventArgs>? StateChanged;
    
    public DataManager()
    {
        clients = new ConcurrentDictionary<string, ClientInfo>();
    }
    
    // 클라이언트 업데이트
    public void UpdateClient(string ip, SyncState newState)
    {
        var isNewClient = false;
        var previousState = SyncState.Unknown;
        
        var client = clients.AddOrUpdate(ip,
            // 새 클라이언트 추가
            (key) =>
            {
                isNewClient = true;
                return new ClientInfo
                {
                    IpAddress = ip,
                    CurrentState = newState,
                    PreviousState = SyncState.Unknown,
                    LastReceived = DateTime.Now,
                    StateChangedTime = DateTime.Now,
                    TotalMessages = 1,
                    History = new List<StateHistory>()
                };
            },
            // 기존 클라이언트 업데이트
            (key, existing) =>
            {
                previousState = existing.CurrentState;
                existing.PreviousState = existing.CurrentState;
                existing.CurrentState = newState;
                existing.LastReceived = DateTime.Now;
                existing.TotalMessages++;
                
                // 상태가 변경된 경우
                if (existing.PreviousState != newState)
                {
                    existing.StateChangedTime = DateTime.Now;
                    
                    // 히스토리 추가
                    lock (historyLock)
                    {
                        existing.History.Add(new StateHistory
                        {
                            Timestamp = DateTime.Now,
                            FromState = existing.PreviousState,
                            ToState = newState
                        });
                        
                        // 최대 100개 히스토리 유지
                        if (existing.History.Count > 100)
                        {
                            existing.History.RemoveAt(0);
                        }
                    }
                }
                
                return existing;
            });
        
        // 이벤트 발생
        ClientUpdated?.Invoke(this, new ClientUpdateEventArgs
        {
            Client = client,
            IsNewClient = isNewClient
        });
        
        // 상태 변경 이벤트
        if (!isNewClient && previousState != newState)
        {
            StateChanged?.Invoke(this, new StateChangeEventArgs
            {
                IpAddress = ip,
                PreviousState = previousState,
                NewState = newState,
                Timestamp = DateTime.Now
            });
        }
    }
    
    // 클라이언트 조회
    public ClientInfo? GetClient(string ip)
    {
        clients.TryGetValue(ip, out var client);
        return client;
    }
    
    // 모든 클라이언트 조회
    public IEnumerable<ClientInfo> GetAllClients()
    {
        return clients.Values.OrderBy(c => c.IpAddress);
    }
    
    // 활성 클라이언트만 조회
    public IEnumerable<ClientInfo> GetActiveClients()
    {
        return clients.Values.Where(c => c.IsActive).OrderBy(c => c.IpAddress);
    }
    
    // 비활성 클라이언트 정리
    public void ClearInactiveClients(int timeoutSeconds = 300)
    {
        var cutoffTime = DateTime.Now.AddSeconds(-timeoutSeconds);
        var inactiveClients = clients.Where(kvp => kvp.Value.LastReceived < cutoffTime).ToList();
        
        foreach (var kvp in inactiveClients)
        {
            if (clients.TryRemove(kvp.Key, out _))
            {
                Logger.Info($"Removed inactive client: {kvp.Key}");
            }
        }
    }
    
    // CSV 내보내기
    public void ExportToCsv(string filename)
    {
        try
        {
            using var writer = new StreamWriter(filename, false, Encoding.UTF8);
            
            // 헤더
            writer.WriteLine("IP Address,Current State,Last Received,Total Messages,State Duration,Is Active");
            
            // 데이터
            foreach (var client in GetAllClients())
            {
                writer.WriteLine($"{client.IpAddress},{client.CurrentState},{client.LastReceived:yyyy-MM-dd HH:mm:ss}," +
                    $"{client.TotalMessages},{client.StateDuration},{client.IsActive}");
            }
            
            Logger.Info($"Exported {clients.Count} clients to {filename}");
        }
        catch (Exception ex)
        {
            Logger.Error($"CSV export error: {ex.Message}");
            throw;
        }
    }
    
    // 통계 정보
    public Statistics GetStatistics()
    {
        var allClients = clients.Values.ToList();
        
        return new Statistics
        {
            TotalClients = allClients.Count,
            ActiveClients = allClients.Count(c => c.IsActive),
            MasterCount = allClients.Count(c => c.CurrentState == SyncState.Master),
            SlaveCount = allClients.Count(c => c.CurrentState == SyncState.Slave),
            ErrorCount = allClients.Count(c => c.CurrentState == SyncState.Error),
            TotalMessages = allClients.Sum(c => c.TotalMessages),
            LastUpdateTime = allClients.Any() ? allClients.Max(c => c.LastReceived) : DateTime.MinValue
        };
    }
    
    // 초기화
    public void Clear()
    {
        clients.Clear();
        Logger.Info("All client data cleared");
    }
}

// 이벤트 아규먼트
public class ClientUpdateEventArgs : EventArgs
{
    public ClientInfo Client { get; set; } = new ClientInfo();
    public bool IsNewClient { get; set; }
}

public class StateChangeEventArgs : EventArgs
{
    public string IpAddress { get; set; } = "";
    public SyncState PreviousState { get; set; }
    public SyncState NewState { get; set; }
    public DateTime Timestamp { get; set; }
}

// 통계 클래스
public class Statistics
{
    public int TotalClients { get; set; }
    public int ActiveClients { get; set; }
    public int MasterCount { get; set; }
    public int SlaveCount { get; set; }
    public int ErrorCount { get; set; }
    public int TotalMessages { get; set; }
    public DateTime LastUpdateTime { get; set; }
}
```

### 14.4 Phase 4 상세: 설정 관리

#### 설정 관리 구현
```csharp
public class ConfigManager
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SyncGuardMonitor",
        "config.json"
    );
    
    // 설정 저장
    public static void SaveConfig(ServerConfig config)
    {
        try
        {
            // 디렉토리 생성
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            // JSON으로 저장
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            File.WriteAllText(ConfigPath, json);
            Logger.Info("Configuration saved successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save configuration: {ex.Message}");
            throw;
        }
    }
    
    // 설정 불러오기
    public static ServerConfig LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<ServerConfig>(json);
                
                if (config != null)
                {
                    Logger.Info("Configuration loaded successfully");
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load configuration: {ex.Message}");
        }
        
        // 기본 설정 반환
        return new ServerConfig();
    }
}
```

### 14.5 로깅 시스템 구현

```csharp
public static class Logger
{
    private static readonly object lockObject = new object();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SyncGuardMonitor",
        "Logs"
    );
    
    static Logger()
    {
        if (!Directory.Exists(LogDirectory))
        {
            Directory.CreateDirectory(LogDirectory);
        }
    }
    
    public static void Log(LogLevel level, string message)
    {
        lock (lockObject)
        {
            try
            {
                var logFile = Path.Combine(LogDirectory, $"monitor_{DateTime.Now:yyyyMMdd}.log");
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
                
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
                
                // 콘솔 출력 (디버그 모드)
                #if DEBUG
                Console.WriteLine(logEntry);
                #endif
            }
            catch
            {
                // 로깅 실패 시 무시
            }
        }
    }
    
    public static void Debug(string message) => Log(LogLevel.Debug, message);
    public static void Info(string message) => Log(LogLevel.Info, message);
    public static void Warning(string message) => Log(LogLevel.Warning, message);
    public static void Error(string message) => Log(LogLevel.Error, message);
}
```

### 14.6 메인 폼 통합 코드

```csharp
public partial class MainForm : Form
{
    // 서버 시작 버튼 클릭
    private async void BtnStart_Click(object sender, EventArgs e)
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
            dataManager = new DataManager();
            uiManager = new UIUpdateManager(dgvClients, rtbLog, statusLabel);
            
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
            
            // 설정 저장
            config.Port = port;
            ConfigManager.SaveConfig(config);
            
            uiManager.AddLog($"서버가 포트 {port}에서 시작되었습니다", LogLevel.Success);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"서버 시작 실패: {ex.Message}", "오류", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            // UI 원복
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            txtPort.Enabled = true;
        }
    }
    
    // 메시지 수신 이벤트 처리
    private void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        try
        {
            // 메시지 파싱
            var syncMessage = SyncMessage.Parse(e.Message, e.ClientIP);
            
            // 데이터 업데이트
            dataManager?.UpdateClient(syncMessage.IpAddress, syncMessage.State);
            
            // 로그 추가
            uiManager?.AddLog($"[{e.ClientIP}] 수신: {e.Message}", LogLevel.Debug);
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
        uiManager?.UpdateGrid(e.Client);
        
        if (e.IsNewClient)
        {
            uiManager?.AddLog($"새 클라이언트 추가: {e.Client.IpAddress}", LogLevel.Info);
        }
    }
    
    // 상태 변경 이벤트 처리
    private void OnStateChanged(object? sender, StateChangeEventArgs e)
    {
        var message = $"상태 변경: {e.IpAddress} - {e.PreviousState} → {e.NewState}";
        uiManager?.AddLog(message, LogLevel.Success);
    }
    
    // 타이머 틱 이벤트 (1초마다)
    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        // 통계 업데이트
        if (dataManager != null)
        {
            var stats = dataManager.GetStatistics();
            UpdateStatistics(stats);
            
            // 비활성 클라이언트 표시 업데이트
            foreach (var client in dataManager.GetAllClients())
            {
                uiManager?.UpdateGrid(client);
            }
        }
    }
    
    // 통계 UI 업데이트
    private void UpdateStatistics(Statistics stats)
    {
        lblConnectedClients.Text = $"연결된 클라이언트: {stats.ActiveClients}개";
        lblTotalMessages.Text = $"총 수신: {stats.TotalMessages} 패킷";
        
        // 상태바 업데이트
        statusLabel.Text = $"서버 실행중 | Master: {stats.MasterCount} | " +
            $"Slave: {stats.SlaveCount} | Error: {stats.ErrorCount}";
    }
}
```

## 16. 프로젝트 파일 구조

### 16.1 솔루션 구조
```
SyncGuardMonitor/
├── SyncGuardMonitor.sln
├── SyncGuardMonitor/
│   ├── SyncGuardMonitor.csproj
│   ├── Program.cs
│   ├── Forms/
│   │   ├── MainForm.cs
│   │   ├── MainForm.Designer.cs
│   │   └── MainForm.resx
│   ├── Core/
│   │   ├── TcpServer.cs
│   │   ├── DataManager.cs
│   │   └── UIUpdateManager.cs
│   ├── Models/
│   │   ├── ClientInfo.cs
│   │   ├── SyncMessage.cs
│   │   └── ServerConfig.cs
│   ├── Utils/
│   │   ├── Logger.cs
│   │   └── ConfigManager.cs
│   └── Resources/
│       ├── icon.ico
│       └── Images/
├── Tests/
│   ├── SyncGuardMonitor.Tests.csproj
│   └── TcpServerTests.cs
└── README.md
```

### 16.2 프로젝트 파일 (.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <ApplicationIcon>Resources\icon.ico</ApplicationIcon>
    <AssemblyName>SyncGuardMonitor</AssemblyName>
    <RootNamespace>SyncGuardMonitor</RootNamespace>
    <Version>1.0.0</Version>
    <Authors>Your Team</Authors>
    <Product>SyncGuard Monitor</Product>
    <Description>TCP 모니터링 도구 for SyncGuard</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Text.Json" Version="7.0.0" />
  </ItemGroup>

  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

## 17. 실제 사용 시나리오

### 17.1 프로그램 시작 및 초기 설정
```csharp
// Program.cs
[STAThread]
static void Main()
{
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    
    // 단일 인스턴스 확인
    using (var mutex = new Mutex(true, "SyncGuardMonitor", out bool createdNew))
    {
        if (createdNew)
        {
            Application.Run(new MainForm());
        }
        else
        {
            MessageBox.Show("프로그램이 이미 실행 중입니다.", "알림", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
```

### 17.2 에러 처리 패턴
```csharp
public class ErrorHandler
{
    public static void HandleError(Exception ex, string context, UIUpdateManager? uiManager = null)
    {
        // 로그 기록
        Logger.Error($"[{context}] {ex.GetType().Name}: {ex.Message}");
        Logger.Debug($"StackTrace: {ex.StackTrace}");
        
        // UI 업데이트 (있는 경우)
        uiManager?.AddLog($"오류 발생: {ex.Message}", LogLevel.Error);
        
        // 특정 예외 타입별 처리
        switch (ex)
        {
            case SocketException socketEx:
                HandleSocketError(socketEx, context);
                break;
                
            case FormatException formatEx:
                HandleFormatError(formatEx, context);
                break;
                
            case IOException ioEx:
                HandleIOError(ioEx, context);
                break;
        }
    }
    
    private static void HandleSocketError(SocketException ex, string context)
    {
        string message = ex.SocketErrorCode switch
        {
            SocketError.AddressAlreadyInUse => "포트가 이미 사용 중입니다.",
            SocketError.AccessDenied => "포트 접근 권한이 없습니다.",
            SocketError.ConnectionReset => "연결이 재설정되었습니다.",
            SocketError.NetworkUnreachable => "네트워크에 접근할 수 없습니다.",
            _ => $"네트워크 오류: {ex.SocketErrorCode}"
        };
        
        Logger.Warning($"[{context}] {message}");
    }
}
```

### 17.3 메뉴 기능 구현
```csharp
// 파일 메뉴 - 로그 내보내기
private void MenuFileExportLog_Click(object sender, EventArgs e)
{
    using var saveDialog = new SaveFileDialog
    {
        Filter = "텍스트 파일|*.txt|모든 파일|*.*",
        DefaultExt = "txt",
        FileName = $"SyncGuardMonitor_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
    };
    
    if (saveDialog.ShowDialog() == DialogResult.OK)
    {
        try
        {
            File.WriteAllText(saveDialog.FileName, rtbLog.Text);
            uiManager?.AddLog($"로그 저장 완료: {saveDialog.FileName}", LogLevel.Success);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"로그 저장 실패: {ex.Message}", "오류", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

// 도구 메뉴 - 통계
private void MenuToolsStatistics_Click(object sender, EventArgs e)
{
    if (dataManager == null) return;
    
    var stats = dataManager.GetStatistics();
    var message = $@"=== 연결 통계 ===
전체 클라이언트: {stats.TotalClients}개
활성 클라이언트: {stats.ActiveClients}개

=== 상태별 현황 ===
Master: {stats.MasterCount}개
Slave: {stats.SlaveCount}개
Error: {stats.ErrorCount}개

=== 메시지 통계 ===
총 수신 메시지: {stats.TotalMessages:N0}개
마지막 업데이트: {stats.LastUpdateTime:yyyy-MM-dd HH:mm:ss}";
    
    MessageBox.Show(message, "통계 정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
```

### 17.4 그리드 컨텍스트 메뉴
```csharp
private void SetupGridContextMenu()
{
    var contextMenu = new ContextMenuStrip();
    
    // 상세 정보 보기
    var menuDetails = new ToolStripMenuItem("상세 정보");
    menuDetails.Click += (s, e) =>
    {
        if (dgvClients.SelectedRows.Count > 0)
        {
            var ip = dgvClients.SelectedRows[0].Cells["colIP"].Value?.ToString();
            if (!string.IsNullOrEmpty(ip))
            {
                ShowClientDetails(ip);
            }
        }
    };
    
    // 히스토리 보기
    var menuHistory = new ToolStripMenuItem("상태 변경 이력");
    menuHistory.Click += (s, e) =>
    {
        if (dgvClients.SelectedRows.Count > 0)
        {
            var ip = dgvClients.SelectedRows[0].Cells["colIP"].Value?.ToString();
            if (!string.IsNullOrEmpty(ip))
            {
                ShowClientHistory(ip);
            }
        }
    };
    
    contextMenu.Items.AddRange(new ToolStripItem[] 
    { 
        menuDetails, 
        menuHistory,
        new ToolStripSeparator(),
        new ToolStripMenuItem("복사", null, (s, e) => CopySelectedIP()),
        new ToolStripMenuItem("삭제", null, (s, e) => RemoveSelectedClient())
    });
    
    dgvClients.ContextMenuStrip = contextMenu;
}

private void ShowClientHistory(string ip)
{
    var client = dataManager?.GetClient(ip);
    if (client == null) return;
    
    var historyForm = new Form
    {
        Text = $"상태 변경 이력 - {ip}",
        Size = new Size(600, 400),
        StartPosition = FormStartPosition.CenterParent
    };
    
    var listBox = new ListBox
    {
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 9)
    };
    
    foreach (var history in client.History.OrderByDescending(h => h.Timestamp))
    {
        listBox.Items.Add($"[{history.Timestamp:yyyy-MM-dd HH:mm:ss}] " +
            $"{history.FromState} → {history.ToState}");
    }
    
    historyForm.Controls.Add(listBox);
    historyForm.ShowDialog(this);
}
```

## 18. 테스트 코드 예시

### 18.1 단위 테스트
```csharp
[TestClass]
public class SyncMessageTests
{
    [TestMethod]
    public void Parse_ValidMessage_ShouldReturnCorrectValues()
    {
        // Arrange
        string message = "192.168.0.201_state2";
        
        // Act
        var result = SyncMessage.Parse(message, "192.168.0.100");
        
        // Assert
        Assert.AreEqual("192.168.0.201", result.IpAddress);
        Assert.AreEqual(SyncState.Master, result.State);
    }
    
    [TestMethod]
    [ExpectedException(typeof(FormatException))]
    public void Parse_InvalidFormat_ShouldThrowException()
    {
        // Arrange
        string message = "invalid_format";
        
        // Act
        SyncMessage.Parse(message, "192.168.0.100");
    }
    
    [TestMethod]
    public void Parse_AllStates_ShouldParseCorrectly()
    {
        // Arrange & Act & Assert
        var state0 = SyncMessage.Parse("192.168.0.201_state0", "");
        Assert.AreEqual(SyncState.Error, state0.State);
        
        var state1 = SyncMessage.Parse("192.168.0.201_state1", "");
        Assert.AreEqual(SyncState.Slave, state1.State);
        
        var state2 = SyncMessage.Parse("192.168.0.201_state2", "");
        Assert.AreEqual(SyncState.Master, state2.State);
    }
}
```

### 18.2 통합 테스트
```csharp
[TestClass]
public class TcpServerIntegrationTests
{
    private TcpServer? server;
    private const int TestPort = 18080;
    
    [TestInitialize]
    public void Setup()
    {
        server = new TcpServer();
    }
    
    [TestCleanup]
    public async Task Cleanup()
    {
        if (server != null)
        {
            await server.StopAsync();
        }
    }
    
    [TestMethod]
    public async Task Server_StartStop_ShouldWork()
    {
        // Start server
        await server!.StartAsync(TestPort);
        Assert.IsTrue(server.IsRunning);
        
        // Stop server
        await server.StopAsync();
        Assert.IsFalse(server.IsRunning);
    }
    
    [TestMethod]
    public async Task Server_ReceiveMessage_ShouldTriggerEvent()
    {
        // Arrange
        string? receivedMessage = null;
        var resetEvent = new ManualResetEventSlim(false);
        
        server!.MessageReceived += (s, e) =>
        {
            receivedMessage = e.Message;
            resetEvent.Set();
        };
        
        await server.StartAsync(TestPort);
        
        // Act - Send test message
        using (var client = new TcpClient())
        {
            await client.ConnectAsync("localhost", TestPort);
            var data = Encoding.UTF8.GetBytes("192.168.0.201_state2\r\n");
            await client.GetStream().WriteAsync(data, 0, data.Length);
        }
        
        // Assert
        Assert.IsTrue(resetEvent.Wait(TimeSpan.FromSeconds(5)));
        Assert.AreEqual("192.168.0.201_state2", receivedMessage);
    }
}
```

## 19. 빌드 및 배포

### 19.1 빌드 스크립트
```batch
@echo off
echo === SyncGuard Monitor 빌드 ===

REM 클린 빌드
dotnet clean

REM Release 빌드
dotnet build -c Release

REM 단일 파일로 발행
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

echo 빌드 완료!
pause
```

### 19.2 설치 프로그램 (NSIS 스크립트 예시)
```nsis
!define APPNAME "SyncGuard Monitor"
!define APPVERSION "1.0.0"
!define APPEXE "SyncGuardMonitor.exe"

Name "${APPNAME} ${APPVERSION}"
OutFile "SyncGuardMonitor_Setup_${APPVERSION}.exe"
InstallDir "$PROGRAMFILES64\${APPNAME}"
RequestExecutionLevel admin

Section "MainSection" SEC01
    SetOutPath "$INSTDIR"
    File "bin\Release\net6.0-windows\win-x64\publish\${APPEXE}"
    
    ; 시작 메뉴 바로가기
    CreateDirectory "$SMPROGRAMS\${APPNAME}"
    CreateShortcut "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk" "$INSTDIR\${APPEXE}"
    CreateShortcut "$SMPROGRAMS\${APPNAME}\Uninstall.lnk" "$INSTDIR\uninstall.exe"
    
    ; 바탕화면 바로가기 (옵션)
    CreateShortcut "$DESKTOP\${APPNAME}.lnk" "$INSTDIR\${APPEXE}"
    
    ; 언인스톨러 생성
    WriteUninstaller "$INSTDIR\uninstall.exe"
SectionEnd

Section "Uninstall"
    Delete "$INSTDIR\${APPEXE}"
    Delete "$INSTDIR\uninstall.exe"
    RMDir "$INSTDIR"
    
    Delete "$SMPROGRAMS\${APPNAME}\*.*"
    RMDir "$SMPROGRAMS\${APPNAME}"
    Delete "$DESKTOP\${APPNAME}.lnk"
SectionEnd
```

## 20. 트러블슈팅 가이드

### 20.1 일반적인 문제 해결
| 문제 | 원인 | 해결 방법 |
|------|------|----------|
| 서버 시작 실패 | 포트 사용 중 | netstat -ano로 포트 확인 후 변경 |
| 메시지 수신 안됨 | 방화벽 차단 | Windows 방화벽에서 포트 허용 |
| UI 멈춤 | 동기 작업 | 비동기 처리로 변경 |
| 메모리 누수 | 이벤트 핸들러 미해제 | Dispose에서 이벤트 해제 |

### 20.2 디버깅 팁
```csharp
// 디버그 모드 전용 로깅
#if DEBUG
    Logger.Debug($"[DEBUG] Client count: {clients.Count}");
    Logger.Debug($"[DEBUG] Message buffer: {messageBuilder}");
#endif

// 조건부 컴파일
#if DEBUG
    private const int DEFAULT_PORT = 18080; // 테스트용 포트
#else
    private const int DEFAULT_PORT = 8080;  // 실제 포트
#endif
```

---

이 계획서를 기반으로 체계적이고 확장 가능한 SyncGuard Monitor를 개발할 수 있습니다! 

개발 과정에서 궁금한 점이나 추가로 필요한 부분이 있다면 언제든지 문의하세요. 😊