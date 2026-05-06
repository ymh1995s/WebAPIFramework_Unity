# 06. 공통 팝업 디스패처

## 1. 요구사항

요구사항.md `## 기타`:
- `UI_ErrorPopup` — 에러 메시지, OK 클릭 시 **게임 재시작**
- `UI_AnnouncementPopup` — 안내 메시지, OK 클릭 시 팝업 닫기

부트스트랩에서 사용:
- `UI_ServerMaintenancePopup` — Confirm 시 앱 종료
- `UI_UpdatePopup` — Confirm 시 디버그 로그 + 진행
- `UI_TermsPopup` — Confirm 시 진행

5종 팝업 + `UI_MailPopup`/`UI_ConfirmPopup`/`UI_SelectPopup`/`UI_InquiryPopup`(03 문서) + `UI_RewardPopup`(05 문서) — **총 9종**.

## 2. 1차 대비 변경점

### 2.1 `PopupService` 메서드 시그니처 — `ApiError` 도입에 따른 정리

`UI_ErrorPopup`은 ProblemDetails(`status`, `errorCode`, `detail`)을 받을 수 있도록 오버로드 추가:

```csharp
public static class PopupService
{
    public static UI_ErrorPopup ShowError(string message);
    public static UI_ErrorPopup ShowError(ApiError error);   // 신규 — error.UserMessage 사용
    public static UI_AnnouncementPopup ShowAnnouncement(string message);
    public static UI_ServerMaintenancePopup ShowMaintenance(string message, Action onConfirm);
    public static UI_UpdatePopup ShowUpdate(string latestVersion, Action onConfirm);
    public static UI_TermsPopup ShowTerms(Action onConfirm);

    // 신규 — 구글 충돌 해소 / 계정 삭제 등에서 재사용
    public static UI_SelectPopup ShowSelect(string message, Action onOk, Action onCancel);
}
```

`ApiError`는 07 문서에서 정의. `UserMessage`는 다음 우선순위로 결정:
1. `errorCode` 있으면 매핑된 한국어 메시지
2. `detail` 있으면 그대로
3. fallback `"요청 처리 중 오류가 발생했습니다 ({status})"`

### 2.2 "게임 재시작" 동작 — 1차 결정 유지

UI_ErrorPopup OK 시 `BootstrapScene` LoadScene + UIManager.Clear. AuthManager는 보존(에러가 인증 문제인지 단정 못 함).

```csharp
private static void RestartGame()
{
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    UIManager.Instance.Clear();
    SceneManager.Instance.LoadScene(Define.EScene.BootstrapScene);
#endif
}
```

### 2.3 점검 인터셉터와의 관계

`ApiClient`의 503 인터셉터가 `EventManager.MaintenanceDetected` 이벤트를 발행 → `PopupService.ShowMaintenance` 호출. **단, 인터셉터는 PopupService를 직접 호출하지 않는다** — 메인스레드 + UIManager 의존이라 의존 방향이 어색해짐. EventManager 경유가 자연.

`MaintenanceFlag`(이미 표시 중인지) 가드는 `PopupService` 내부 정적 bool로 보유:
```csharp
static bool _maintenanceShown;
public static UI_ServerMaintenancePopup ShowMaintenance(...)
{
    if (_maintenanceShown) return null;
    _maintenanceShown = true;
    var p = ...;
    return p;
}
```
`Clear` 시 false로 리셋(BootstrapScene 진입 시).

## 3. 5종 팝업 컨트롤러 공통 패턴

```csharp
public class UI_ErrorPopup : UI_UGUI, IUI_Popup
{
    enum Texts   { MessageTxt }
    enum Buttons { OkBtn }

    public Action OnOk { get; set; }

    protected override void Awake()
    {
        base.Awake();
        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));
        GetButton((int)Buttons.OkBtn).onClick.AddListener(() => OnOk?.Invoke());
    }

    public void SetText(string message)
        => GetText((int)Texts.MessageTxt).text = message;
}
```

각 팝업의 자식 GameObject 이름과 enum 값이 1:1 정합되어야 함(`UI_UGUI.BindTexts/BindButtons`가 이름 기반). 프리팹 자식 이름 매핑 작업은 unity-programmer 단계에서 수행.

## 4. UI_SelectPopup 시그니처

구글 충돌 해소(02), 계정 삭제(03) 양쪽에서 재사용:

```csharp
public class UI_SelectPopup : UI_UGUI, IUI_Popup
{
    enum Texts   { MessageTxt }
    enum Buttons { OkBtn, CancelBtn }

    public Action OnOk { get; set; }
    public Action OnCancel { get; set; }

    public void SetText(string message);
}
```

## 5. UI_MailPopup / UI_InquiryPopup / UI_ConfirmPopup / UI_RewardPopup

03/05 문서에서 시그니처 정의. 본 문서에선 컨트롤러 클래스명이 프리팹명과 일치해야 한다는 점만 박제.

## 6. 영향받는 파일

**신규 스크립트**
- `UI/PopupService.cs`
- `UI/UI_ErrorPopup.cs`
- `UI/UI_AnnouncementPopup.cs`
- `UI/UI_ServerMaintenancePopup.cs`
- `UI/UI_UpdatePopup.cs`
- `UI/UI_TermsPopup.cs`
- `UI/UI_SelectPopup.cs` (03 문서와 공유)
- `UI/UI_MailPopup.cs` (03 문서)
- `UI/UI_ConfirmPopup.cs` (03 문서)
- `UI/UI_InquiryPopup.cs` (03 문서)
- `UI/UI_RewardPopup.cs` (05 문서)

**프리팹** — 모두 이미 존재 (`Resources/PreLoad/Prefabs/UI/`). 인스펙터 자식 이름 정합 작업만 unity-programmer.

## 7. DoD

- [ ] PopupService 정적 헬퍼 7개 메서드 (ShowError × 2 / ShowAnnouncement / ShowMaintenance / ShowUpdate / ShowTerms / ShowSelect)
- [ ] 5개 일반 팝업 컨트롤러 + 4개 도메인 팝업 컨트롤러
- [ ] 각 컨트롤러가 자기 프리팹 자식 이름과 enum 정합
- [ ] UI_ErrorPopup OK → BootstrapScene 재진입
- [ ] UI_AnnouncementPopup OK → 팝업 닫힘만
- [ ] UI_ServerMaintenancePopup Confirm → Application.Quit
- [ ] UI_UpdatePopup Confirm → Debug.Log + 진행
- [ ] UI_TermsPopup Confirm → PlayerPrefs.TermsAccepted=1 + 진행
- [ ] UI_SelectPopup OK/Cancel 분기

## 8. 트레이드오프 — `PopupService` static vs Singleton

| 옵션 | 장점 | 단점 |
|---|---|---|
| A. static (채택) | 상태 거의 없음. 의존 적음 | 정적 플래그 1~2개 보유는 가능 |
| B. Singleton<T> | 다른 매니저들과 일관 | MonoBehaviour 라이프사이클 불필요 |

**A 채택**. 1차 결정 유지. `_maintenanceShown` 플래그 정도라 static field로 충분.
