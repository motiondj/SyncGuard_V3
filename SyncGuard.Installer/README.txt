SyncGuard V3 - NVIDIA Quadro Sync 모니터링 도구
================================================

개요
----
SyncGuard V3는 NVIDIA Quadro Sync 상태를 실시간으로 모니터링하고 
외부 서버로 상태 정보를 전송하는 Windows 시스템 트레이 애플리케이션입니다.

주요 기능
---------
• NVIDIA Quadro Sync 상태 실시간 모니터링
• 시스템 트레이 아이콘으로 상태 표시
• TCP 클라이언트를 통한 외부 서버 연결
• 설정 관리 및 저장
• 자동 로그 기록 및 로테이션
• 다중 디스플레이 지원

시스템 요구사항
--------------
• Windows 10/11 (64비트)
• NVIDIA Quadro 그래픽 카드
• NVIDIA 드라이버 (최신 버전 권장)
• .NET 6.0 Runtime

설치 방법
---------
1. SyncGuard_V3_Setup.msi 파일을 실행합니다.
2. 설치 마법사의 지시에 따라 설치를 진행합니다.
3. 설치 완료 후 자동으로 시작됩니다.

사용법
------
• 시스템 트레이 아이콘을 더블클릭하여 상태 확인
• 우클릭 메뉴에서 설정 변경 가능
• 설정 > TCP 서버 IP/포트 설정
• 리프레시로 수동 상태 업데이트

로그 파일
---------
• 위치: [설치경로]\logs\syncguard_log.txt
• 자동 로테이션 (10MB 초과 시)
• UTF-8 인코딩

설정 파일
---------
• 위치: [설치경로]\config\syncguard_config.txt
• JSON 형식
• TCP 서버 IP/포트 설정

문제 해결
---------
• NVIDIA 드라이버가 최신 버전인지 확인
• 방화벽에서 TCP 포트 허용
• 관리자 권한으로 실행 시도

지원
----
• GitHub: https://github.com/syncguard
• 이메일: support@syncguard.com

라이선스
--------
MIT License - 자유롭게 사용, 수정, 배포 가능

버전 정보
---------
• 버전: 3.0.0
• 빌드 날짜: 2024년 1월
• 개발자: SyncGuard Team 