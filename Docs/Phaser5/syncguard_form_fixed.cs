// 📁 파일 경로: SyncGuard.Tray/Form1.cs
// 🔧 수정 방법: 아래 수정사항들을 찾아서 각각 교체하세요.
// 🎯 수정 목적: 새로운 Unsupported 상태 처리 및 UI 개선

// =============================================================================
// 수정 1: InitializeIconCache() 메서드 수정
// 기존 코드를 찾아서 교체하세요.
// =============================================================================

// 🔥 아이콘 캐시 초기화
private void InitializeIconCache()
{
    iconCache[SyncChecker.SyncStatus.Master] = CreateColorIcon(Color.Green);
    iconCache[SyncChecker.SyncStatus.Slave] = CreateColorIcon(Color.Yellow);
    iconCache[SyncChecker.SyncStatus.Error] = CreateColorIcon(Color.Red);
    iconCache[SyncChecker.SyncStatus.Unknown] = CreateColorIcon(Color.Gray);
    iconCache[SyncChecker.SyncStatus.Unsupported] = CreateColorIcon(Color.LightGray); // 🔥 미지원 환경은 연한 회색
}

// =============================================================================
// 수정 2: GetStatusMessage() 메서드 수정
// 기존 코드를 찾아서 교체하세요.
// =============================================================================

private string GetStatusMessage(SyncChecker.SyncStatus status)
{
    return status switch
    {
        SyncChecker.SyncStatus.Master => "Master (마스터)",
        SyncChecker.SyncStatus.Slave => "Slave (슬레이브)",
        SyncChecker.SyncStatus.Error => "Error (오류)",
        SyncChecker.SyncStatus.Unknown => "Unknown (알 수 없음)",
        SyncChecker.SyncStatus.Unsupported => "Unsupported (미지원)", // 🔥 새로운 상태 추가
        _ => "Unknown (알 수 없음)"
    };
}

// =============================================================================
// 수정 3: GetStatusText() 메서드 수정
// 기존 코드를 찾아서 교체하세요.
// =============================================================================

private string GetStatusText(SyncChecker.SyncStatus status)
{
    return status switch
    {
        SyncChecker.SyncStatus.Master => "Synced",
        SyncChecker.SyncStatus.Slave => "Free",
        SyncChecker.SyncStatus.Error => "Free",
        SyncChecker.SyncStatus.Unknown => "Unknown",
        SyncChecker.SyncStatus.Unsupported => "Unsupported", // 🔥 새로운 상태 추가
        _ => "Unknown"
    };
}

// =============================================================================
// 수정 4: ShowUnsupportedNoticeIfNeeded() 메서드 수정
// 기존 코드를 찾아서 교체하세요.
// =============================================================================

private void ShowUnsupportedNoticeIfNeeded()
{
    // 🔥 미지원 환경이면 안내 메시지 1회 표시
    if (syncChecker != null)
    {
        var status = syncChecker.GetSyncStatus();
        if (status == SyncChecker.SyncStatus.Unsupported)
        {
            ShowToastNotification("SyncGuard 안내", 
                "이 시스템은 NVIDIA Quadro Sync를 지원하지 않습니다.\n" +
                "TCP 메시지는 정상적으로 전송됩니다 (state0).");
        }
    }
}

// =============================================================================
// 수정 5: InitializeTrayIcon() 메서드 일부 수정
// 아래 부분을 찾아서 교체하세요.
// =============================================================================

// 기존 코드에서 이 부분을 찾아서:
// string tip = "SyncGuard - ";
// if (syncChecker != null && syncChecker.GetSyncStatus() == SyncChecker.SyncStatus.Unknown)
// {
//     tip += "지원되지 않는 환경(Unknown)";
// }
// else
// {
//     tip += "Quadro Sync 모니터링";
// }

// 🔥 아래 코드로 교체:
string tip = "SyncGuard - ";
if (syncChecker != null)
{
    var status = syncChecker.GetSyncStatus();
    if (status == SyncChecker.SyncStatus.Unsupported)
    {
        tip += "지원되지 않는 환경";
    }
    else if (status == SyncChecker.SyncStatus.Unknown)
    {
        tip += "상태 확인 중";
    }
    else
    {
        tip += "Quadro Sync 모니터링";
    }
}
else
{
    tip += "초기화 중";
}

// =============================================================================
// 수정 6: OnRefreshSyncStatus() 메서드 수정
// 기존 코드를 찾아서 교체하세요.
// =============================================================================

private void OnRefreshSyncStatus(object? sender, EventArgs e)
{
    if (syncChecker == null)
    {
        Logger.Warning("SyncChecker가 초기화되지 않았습니다.");
        MessageBox.Show("SyncChecker가 초기화되지 않았습니다.", "오류", 
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
        
        // 🔥 상태에 따른 메시지 개선
        if (status == SyncChecker.SyncStatus.Unsupported)
        {
            message += "\n이 시스템은 NVIDIA Quadro Sync를 지원하지 않습니다.";
        }
        
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

// =============================================================================
// 수정 7: OnSyncTimerTick() 메서드 일부 수정
// 아래 부분을 찾아서 교체하세요.
// =============================================================================

// 기존 코드에서 이 부분을 찾아서:
// if (status != lastUiStatus)
// {
//     lastUiStatus = status;
//     
//     if (!this.IsDisposed && this.IsHandleCreated)
//     {
//         this.BeginInvoke(() =>
//         {
//             try
//             {
//                 UpdateTrayIcon(status);
//                 
//                 if (lastStatus != SyncChecker.SyncStatus.Unknown)
//                 {
//                     lastStatusChangeTime = DateTime.Now;
//                     ShowToastNotification("Sync 상태 변경", GetStatusMessage(status));
//                 }
//                 
//                 lastStatus = status;
//             }

// 🔥 아래 코드로 교체:
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
                
                // 🔥 Unknown과 Unsupported 상태 구분
                if (lastStatus != SyncChecker.SyncStatus.Unknown && 
                    lastStatus != SyncChecker.SyncStatus.Unsupported)
                {
                    lastStatusChangeTime = DateTime.Now;
                    ShowToastNotification("Sync 상태 변경", GetStatusMessage(status));
                }
                // 🔥 Unsupported 상태는 처음 한 번만 알림
                else if (status == SyncChecker.SyncStatus.Unsupported && 
                         lastStatus != SyncChecker.SyncStatus.Unsupported)
                {
                    lastStatusChangeTime = DateTime.Now;
                    ShowToastNotification("SyncGuard 안내", 
                        "이 시스템은 NVIDIA Quadro Sync를 지원하지 않습니다.\n" +
                        "TCP 메시지는 정상적으로 전송됩니다 (state0).");
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