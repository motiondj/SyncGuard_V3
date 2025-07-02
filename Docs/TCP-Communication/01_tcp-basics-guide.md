# SyncGuard TCP 데이터 전송 기본 가이드

## 📋 개요

SyncGuard는 TCP 소켓 통신을 통해 외부 시스템에 동기화 상태를 실시간으로 전송할 수 있습니다. 이 문서는 외부 프로그램에서 SyncGuard의 데이터를 받아오기 위한 기본적인 정보를 제공합니다.

## 🎯 목적

- 외부 프로그램에서 SyncGuard 데이터 수신 방법 안내
- TCP 통신 프로토콜 및 데이터 형식 설명
- 기본 설정 및 연결 방법 가이드
- 다중 서버 환경에서의 활용 방법

## 🔧 기본 설정 정보

### 네트워크 설정
- **기본 포트**: `8080`
- **프로토콜**: TCP
- **인코딩**: UTF-8
- **방향**: SyncGuard → 외부 시스템 (단방향 전송)
- **타임아웃**: 5초
- **다중 연결**: 지원 (여러 클라이언트 동시 연결 가능)

### 서버 정보
- **IP 주소**: SyncGuard가 실행되는 컴퓨터의 IP 주소
- **포트**: 8080 (기본값, 설정에서 변경 가능)
- **연결 방식**: TCP 클라이언트-서버 모델

## 📡 데이터 통신 형식

### 요청-응답 방식
```
외부 프로그램 → SyncGuard: "GET_STATUS"
SyncGuard → 외부 프로그램: "Synced" 또는 "Free"
```

### 상태 정의
- **"Synced"**: 동기화 정상 (Locked 상태)
- **"Free"**: 동기화 안됨 (UnSynced, Slave 등 모든 비정상 상태)

### 내부 메시지 형식 (고급 사용자용)
```
IP_state (예: 192.168.0.201_state2)
```
- `state0`: Error (동기화 오류)
- `state1`: Slave (슬레이브 모드)  
- `state2`: Master (마스터 모드)

## 🔌 연결 방법

### 1. 기본 연결 과정
1. SyncGuard가 TCP 서버 시작 (포트 8080)
2. 외부 프로그램이 SyncGuard IP:8080으로 TCP 연결
3. 외부 프로그램이 "GET_STATUS" 요청 전송
4. SyncGuard가 현재 상태 응답
5. 연결 유지 (다음 요청 대기)

### 2. 연결 예시 (Python)
```python
import socket

# SyncGuard 서버에 연결
client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
client.connect(('192.168.0.201', 8080))

# 상태 요청
client.send(b'GET_STATUS')

# 응답 수신
response = client.recv(1024).decode('utf-8')
print(f"SyncGuard 상태: {response}")

client.close()
```

### 3. 연결 예시 (C#)
```csharp
using System.Net.Sockets;
using System.Text;

TcpClient client = new TcpClient();
client.Connect("192.168.0.201", 8080);

NetworkStream stream = client.GetStream();
byte[] request = Encoding.UTF8.GetBytes("GET_STATUS");
stream.Write(request, 0, request.Length);

byte[] response = new byte[1024];
int bytesRead = stream.Read(response, 0, response.Length);
string status = Encoding.UTF8.GetString(response, 0, bytesRead);

Console.WriteLine($"SyncGuard 상태: {status}");
client.Close();
```

## 🌐 다중 서버 환경

### 네트워크 구성
```
서버A (SyncGuard) → 포트 8080 → 수신 서버
서버B (SyncGuard) → 포트 8080 → 수신 서버  
서버C (SyncGuard) → 포트 8080 → 수신 서버
```

### 특징
- **수신 서버**: 하나의 포트(8080)에서 다중 클라이언트 연결 처리
- **각 SyncGuard**: 동일한 포트(8080)로 연결
- **IP 주소 구분**: 수신 서버가 각 SyncGuard의 IP로 구분하여 개별 응답

### 다중 서버 모니터링 예시
```python
import socket
import threading
import time

def monitor_syncguard(ip_address, port=8080):
    while True:
        try:
            client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            client.settimeout(5)
            client.connect((ip_address, port))
            
            client.send(b'GET_STATUS')
            response = client.recv(1024).decode('utf-8')
            
            print(f"[{ip_address}] 상태: {response}")
            client.close()
            
        except Exception as e:
            print(f"[{ip_address}] 연결 실패: {e}")
        
        time.sleep(10)  # 10초마다 확인

# 여러 SyncGuard 서버 모니터링
servers = ['192.168.0.201', '192.168.0.202', '192.168.0.203']
threads = []

for server in servers:
    thread = threading.Thread(target=monitor_syncguard, args=(server,))
    thread.daemon = True
    thread.start()
    threads.append(thread)

# 메인 스레드 유지
for thread in threads:
    thread.join()
```

## ⚙️ 설정 방법

### 1. 트레이 메뉴 설정 (추천)
- SyncGuard 트레이 아이콘 우클릭
- "설정" 메뉴 선택
- 다음 항목 구성:
  - 대상 IP 주소 입력
  - 포트 번호 입력 (기본값: 8080)
  - 외부 전송 활성화 체크박스
  - 연결 테스트 버튼
  - 저장/취소 버튼

### 2. 설정 파일 (백업)
- 파일명: `syncguard_config.txt`
- 위치: SyncGuard 실행 디렉토리
- 형식:
```
# SyncGuard 외부 전송 설정
TARGET_IP=192.168.1.100
TARGET_PORT=8080
ENABLE_EXTERNAL_SEND=true
```

### 3. 다중 서버 설정 예시
```
# 서버A 설정
TARGET_IP=192.168.1.100
TARGET_PORT=8080

# 서버B 설정 (동일)
TARGET_IP=192.168.1.100
TARGET_PORT=8080

# 서버C 설정 (동일)
TARGET_IP=192.168.1.100
TARGET_PORT=8080
```

## 🔄 동작 흐름

### 1. 초기화 단계
1. SyncGuard 시작
2. 설정 파일 로드
3. 외부 전송 기능 초기화
4. TCP 서버 시작 (포트 8080)

### 2. 상태 모니터링
1. WMI를 통한 동기화 상태 감지 (1초 간격)
2. 상태 변경 시 로그 기록
3. 트레이 아이콘 업데이트

### 3. 외부 전송
1. 외부 시스템 연결 대기
2. "GET_STATUS" 요청 수신
3. 현재 상태 응답 전송
4. 연결 유지

## 📊 로그 및 모니터링

### 로그 항목
- 외부 연결 시도/성공/실패
- 데이터 전송 성공/실패
- 연결 상태 변경
- 설정 변경 이력

### 로그 예시
```
[2025-06-26 10:30:00] [INFO] 외부 전송 설정 로드: 192.168.1.100:8080
[2025-06-26 10:30:01] [INFO] 외부 시스템 연결 성공
[2025-06-26 10:30:05] [INFO] 상태 전송: Synced
[2025-06-26 10:30:15] [INFO] 상태 전송: Synced
```

## 🎯 사용 시나리오

### 시나리오 1: 정상 동작
1. SyncGuard가 "Locked" 상태 감지
2. 외부 시스템이 "GET_STATUS" 요청
3. SyncGuard가 "Synced" 응답
4. 외부 CMS에서 녹색 상태 표시

### 시나리오 2: 동기화 문제
1. SyncGuard가 "UnSynced" 상태 감지
2. 외부 시스템이 "GET_STATUS" 요청
3. SyncGuard가 "Free" 응답
4. 외부 CMS에서 빨간색 경고 표시
5. 사용자가 수동으로 복구 작업 수행

## ⚠️ 주의사항

### 보안 고려사항
- 방화벽에서 포트 8080 허용 필요
- 네트워크 보안 정책 확인
- 불필요한 포트 노출 방지

### 성능 고려사항
- **리소스 사용량**: 최소 메모리 사용
- **네트워크**: 매우 낮은 대역폭 (텍스트 기반)
- **CPU**: 거의 사용하지 않음
- **폴링 간격**: 5-10초 간격 권장

### 에러 처리
- 연결 실패 시 재연결 시도 (30초 간격)
- 외부 시스템 연결 상태 모니터링
- 로그 기록으로 문제 추적

## 🔮 향후 확장 계획

### 예정 기능
- SSL/TLS 암호화 지원
- JSON 형식 데이터 전송
- 실시간 이벤트 알림
- 웹소켓 지원
- REST API 제공

### 호환성
- 다양한 프로그래밍 언어 지원
- 표준 프로토콜 사용으로 확장성 확보
- 기존 시스템과의 연동 용이성

---

## 📞 지원 및 문의

이 문서에 대한 질문이나 추가 정보가 필요하시면 개발팀에 문의해주세요.

**문서 버전**: 1.0  
**최종 업데이트**: 2025-01-27  
**작성자**: SyncGuard 개발팀 