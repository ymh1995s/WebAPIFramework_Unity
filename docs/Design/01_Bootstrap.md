# 01. 부트스트랩 (앱 부팅 ~ LoginScene 진입)

## 1. 요구사항

요구사항.md `## 게입 시작 ~ 로그인씬 진입 사이`:
1. 점검 중 → `UI_ServerMaintenancePopup`, Confirm 시 앱 종료
2. 업데이트 필요 → `UI_UpdatePopup`, Confirm 시 디버그 로그만 찍고 진행
3. 자동 로그인 → 기존 로그인 정보 보유 시 MainScene 직행
4. 이용 약관 → 첫 실행 시 `UI_TermsPopup`, Confirm 시 진행. 점검·업데이트 통과 후

`CLIENT_GUIDE.md` 1~3번 + 8번 + 11~12번.

## 2. 핵심 결정 (1차에서 유지)

- **BootstrapScene 신설**(이전 라운드 사용자 승인됨). 자동 로그인 깜빡임 제거 + 부팅 책임 분리.
- 약관 팝업 타이밍 = 자동 로그인 분기 종료 후 LoginScene 직전 (1차 결정 유지).

## 3. 1차 대비 변경점

### 3.1 점검(503) 처리 — endpoint 호출 제거, 인터셉터로 대체

**1차 오류**: `VersionApi.CheckMaintenanceAsync()` 별도 호출 가정. **실측: 그런 endpoint 없음**. 점검 모드는 백엔드 미들웨어가 모든 요청에 503+`{"message":"서버 점검 중..."}`을 반환하는 패턴(CLIENT_GUIDE 1번, `Framework.Api/Program.cs:170-186`).

**정정**: 부트스트랩 첫 호출은 **버전 체크**(`GET /api/version/check?version=...`)이며, 그 응답이 503이면 ApiClient의 503 인터셉터가 `EventManager.MaintenanceDetected` 이벤트를 발행. BootstrapFlow는 이 이벤트를 1회 구독해서 `UI_ServerMaintenancePopup`으로 분기. 자세한 503 인터셉터 명세는 `07_WebFramework_Plan.md`.

### 3.2 버전 응답 필드 정정

```json
{ "isForceUpdate": false, "latestVersion": "1.2.0" }
```
1차 설계의 `requiresUpdate`는 폐기. `VersionCheckResponse.isForceUpdate` 사용.

### 3.3 최신 공지 표시 — 부트스트랩에 추가

CLIENT_GUIDE 3번 — 버전 통과 직후, 로그인 전 1회 표시. 1차 설계는 메인 화면 버튼에만 두었으나 가이드는 부팅 단계 표시를 권장한다(`Notice` 단건만 존재함을 활용).

- 호출: `GET /api/notices/latest` (인증 불필요)
- 200 → `{id, content}` 수신, **PlayerPrefs `lastSeenNoticeId != id`** 일 때만 `UI_AnnouncementPopup`으로 표시. 닫을 때 `lastSeenNoticeId = id` 저장
- 204 → 활성 공지 없음, 스킵
- 503 → 점검 인터셉터가 처리

### 3.4 자동 로그인 = `POST /api/auth/refresh`

1차와 동일. 다만:
- 응답에 `isGoogleLinked` 없음 → `AuthManager.IsGoogleLinked`는 PlayerPrefs 캐시에서 복원(자세한 결정 근거: `07_WebFramework_Plan.md`)
- refresh 실패 시 RefreshToken 무효화로 간주, `AuthManager.Clear()` 후 LoginScene 진입

## 4. 씬 흐름

```
Scene_Bootstrap.Start
  │
  ▼
ResourceManager.LoadAll(onComplete=>RunBootstrapAsync())
  │
  ▼
[1] VersionApi.Check(Application.version)
  ├─ 503 → MaintenanceDetected 이벤트 → UI_ServerMaintenancePopup → Quit
  ├─ 200 isForceUpdate=true → UI_UpdatePopup → Confirm 후 ContinueAfterVersion()
  └─ 200 isForceUpdate=false → ContinueAfterVersion()

ContinueAfterVersion()
  │
  ▼
[2] NoticeApi.GetLatest()  (실패 시 무시하고 계속)
  ├─ 200 + 신규 id → UI_AnnouncementPopup 노출, 닫으면 다음 단계
  └─ 204 / 동일 id / 에러 → 즉시 다음 단계

  ▼
[3] AuthManager.LoadSavedToken()
  ├─ RefreshToken 있음 → AuthApi.Refresh
  │   ├─ 성공 → AuthManager.SaveToken → SceneManager.LoadScene(MainScene)
  │   └─ 실패(401 등) → AuthManager.Clear → 다음 분기
  └─ RefreshToken 없음 → 다음 분기

[4] 약관 — 점검·업데이트 통과 + 자동 로그인 실패/부재일 때만
  ├─ PlayerPrefs.TermsAccepted == 0 → UI_TermsPopup → 동의 후 LoginScene
  └─ TermsAccepted == 1 → LoginScene
```

## 5. 매니저 책임

| 매니저 | 책임 | 신규/기존 |
|---|---|---|
| `Scene_Bootstrap` | 라이프사이클 진입점, ResourceManager 프리로드 트리거 | 신규 |
| `BootstrapFlow` (정적) | 위 1~4 흐름 | 신규 |
| `VersionApi` | `/api/version/check` 호출 | 신규 |
| `NoticeApi` | `/api/notices/latest` 호출 | 신규 |
| `AuthApi.Refresh` | 자동 로그인 | 기존 (확장) |
| `EventManager` | `MaintenanceDetected` 이벤트 발행 통로 | 기존 (Define.EEventType 확장) |
| `PopupService` | 5종 팝업 디스패처 | 신규 |
| `SceneManager` | LoginScene/MainScene 전환 | 기존 |

`Define.EEventType`에 `MaintenanceDetected` 추가 1줄.

## 6. WebFramework 매핑

| 기능 | Endpoint | 인증 | DTO |
|---|---|---|---|
| 버전 체크 | `GET /api/version/check?version={ver}` | X | 응답: `{isForceUpdate, latestVersion}` |
| 최신 공지 | `GET /api/notices/latest` | X | 응답: `{id, content}` 또는 204 |
| Refresh | `POST /api/auth/refresh` | X (RefreshToken 본문) | `{accessToken, refreshToken, playerId, isNewPlayer}` |

## 7. 데이터 흐름

```
Scene_Bootstrap → BootstrapFlow → VersionApi → ApiClient.Get → /api/version/check
                          ↓ (이벤트)
                     PopupService.ShowMaintenance / ShowUpdate / ShowTerms
                          ↓
                     SceneManager.LoadScene(LoginScene 또는 MainScene)
```

## 8. 영향받는 파일/에셋

**신규 스크립트**
- `Assets/@Scripts/Scenes/Scene_Bootstrap.cs`
- `Assets/@Scripts/Bootstrap/BootstrapFlow.cs`
- `Assets/@Scripts/UI/PopupService.cs`
- 5개 팝업 컨트롤러(06 문서)
- `VersionApi`, `NoticeApi`, `VersionModels`, `NoticeModels`(07 문서)

**신규 씬/프리팹**
- `Assets/@Scenes/BootstrapScene.unity` — Scene_Bootstrap 부착된 단일 GameObject
- 팝업 프리팹 5종은 이미 `Resources/PreLoad/Prefabs/UI/` 에 존재

**수정**
- `Assets/@Scripts/Utils/Define.cs` — `EScene.BootstrapScene`, `EEventType.MaintenanceDetected` 추가
- Build Settings: BootstrapScene index 0 등록

## 9. Application.Quit 헬퍼

```csharp
public static void QuitApplication()
{
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
}
```
`PopupService.ShowMaintenance(onConfirm: QuitApplication)` 으로 호출.

## 10. 트레이드오프

### 점검 인터셉터 위치 — ApiClient 내부 vs 외부 핸들러

| 옵션 | 장점 | 단점 |
|---|---|---|
| A. ApiClient 내부에서 503 감지 후 EventManager 발행 | 모든 호출처가 자동 보호. 코드 1곳에서 처리 | ApiClient가 EventManager에 의존 (수용 가능 — 매니저 재사용 원칙) |
| B. 호출처마다 응답 검사 | ApiClient 단순 유지 | 누락 위험. 점검 처리 일관성 깨짐 |

**A 채택**. 503 + Body 내 `"message":"서버 점검"` 패턴 매칭. 자세한 구현은 07 문서.

## 11. 위험 요소

- 503 인터셉터가 무한 루프(점검 알림이 점검 알림을 또 띄움)에 빠지지 않도록 `_maintenancePopupShown` 플래그를 EventManager 또는 PopupService에 둔다.
- 부트스트랩 중 `EventManager` 구독은 RunBootstrapAsync 진입 시 1회 등록 + LoginScene/MainScene 전환 직전 해제.
- `Application.version` 형식이 `"1.0.0"`(Major.Minor.Patch)이 아니면 백엔드 400. ProjectSettings 빌드 버전 형식 합의 필요.
