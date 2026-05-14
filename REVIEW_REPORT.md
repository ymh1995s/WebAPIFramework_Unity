# GameClient 풀리뷰 보고서

**라운드**: round_20260514  
**검토 일시**: 2026-05-14  
**검토 범위**: `UnityClient/Assets/@Scripts/` 전수 (92 + α 파일, 9개 청크)  
**에이전트**: unity-architect (P1.x) / unity-qa (P2.x, P3.x) / orchestrator (P4.1)

---

## Executive Summary

| 항목 | 수치 |
|---|---|
| 완료 청크 | 9 / 9 |
| Critical | **3건** (2건 해결, 1건 잔존) |
| High | **11건** (7건 해결) |
| Medium | **19건** (2건 해결) |
| Low / Info | **14건** |

### Top 5 즉시 조치

| 순위 | 조치 | 파일 | 예상 소요 |
|---|---|---|---|
| 1 | ~~Unity Editor 포커스 → csproj 재생성 (빌드 차단 해소)~~ ✅ 해결 | `Assembly-CSharp.csproj` | — |
| 2 | ~~`BootstrapFlow` 자동 로그인 성공 경로에 `DataManager.LoadData()` 추가~~ ✅ 해결 | `BootstrapFlow.cs:206` | — |
| 3 | `EventManager.TriggerEvent` null-conditional 적용 (`?.Invoke()`) | `EventManager.cs:27` | 5분 |
| 4 | ~~`SaveManager.Load()` try-catch 추가 (JSON 손상 시 Reset 복구)~~ ✅ 해결 | `SaveManager.cs:72` | — |
| 5 | Domain Reload 미대응 static 필드 일괄 리셋 (`[RuntimeInitializeOnLoadMethod]`) | ApiClient, BootstrapFlow, AppResumeFlow 외 8개 | 2시간 |

---

## 0장. 직전 라운드 결함 추적

이전 라운드 없음. 해당 없음.

---

## 1장. Phase 1 — 아키텍처 (unity-architect)

### 1.1 폴더구조·의존성·Singleton (A1, A2, A3)

**A1 폴더 구조 — WARN**

- `@Scripts/Bootstrap/` 폴더가 CLAUDE.md 규정 폴더 목록에 없음 (MAJOR)
- `Managers/SceneManager.cs`가 `UnityEngine.SceneManagement.SceneManager`와 동일 식별자 → `LoadingScene.cs:39` 우회 주석으로 박제됨 (MAJOR)
- `Managers/PoolManager.cs:7` Pool 클래스 동거, `Managers/ResourceManager.cs:44` ResourcesLoader 동거 (MINOR)
- `Utils/PriorityQueue.cs:4` 전 코드베이스 글로벌 네임스페이스인데 단독 `namespace Rookiss` (MINOR)

**A2 Singleton — PASS**

19개 매니저 전수 `Singleton<T>` 베이스 통일. 자체 static instance 중복 0건. `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 정적 리셋 적용 모범.

**A3 의존성 방향 — FAIL**

역방향 의존 4건:
- `Managers/CrashReportManager.cs:37,38,77,78` → `AuthManager.OnLoginSuccess/OnLogout`
- `Managers/AppLifecycleManager.cs:51,72` → `AuthManager.Instance`
- `Managers/ShoutManager.cs:45,160,179` → `AuthManager`, `ShoutApi`, `ServerTime`
- `WebFramework/Core/ApiClient.cs:241` → `UI/PopupService.ShowToast`

UI→Managers, Scenes→Managers, WebFramework/Api→Core·Models 방향은 PASS.

### 1.2 WebFramework 구조·재사용·Api 매핑 (A4, A5, A6)

**A4 Core — WARN**

- `ApiClient.cs:241` `PopupService.ShowToast` 직접 호출 (WebFramework→UI 위반)
- `ApiClient.cs:342,373,391,420,473` `AuthManager.Instance` 직접 호출 5건 → Core↔Auth 양방향 결합

RestLogger 이벤트 발행 패턴, ApiResult readonly struct, 단일 UnityWebRequest 진입점은 모범.

**A5 Api/Models — PASS**

12개 Api 클래스 전수 ApiClient만 의존, UnityWebRequest 중복 0건. Models 13개 전수 `[Serializable]` + 순수 DTO.

**A6 Auth — WARN**

Auth→Api 의존 0건(정상). Core가 `AuthManager` 구체 타입에 직접 의존하는 양방향 결합 존재.  
**권고**: `ITokenProvider` 인터페이스 도입 (약 7라인 변경 + 인터페이스 1개) → Core↔Auth 결합 해소.  
**ShoutManager 위치**: Managers 유지 + 이벤트 디커플 + ServerTime 공용 정적 예외 명시.

### 1.3 씬흐름·UI베이스·Bootstrap (A7, A8, A9)

**A7 씬 흐름 — WARN**

- `LoadingScene.cs:40` `UnityEngine.SceneManagement.SceneManager.LoadSceneAsync` 직접 호출 (래퍼 미커버) — SceneManager 이름 충돌 비용을 코드로 박제
- `BaseScene.cs` Enter/Exit 추상화 부재 — 씬 이탈 정리 패턴 비일관

**A8 UI 베이스·UIManager — PASS**

UI 직접 인스턴스화 0건. UIManager/PopupService 단일 진입점 강제. Popup/Scene/Toast 3개 루트 분리. 단, `UI_Toolkit` 베이스 미사용 + 4개 Scene UI의 `IUI_Scene` 미선언.

**A9 Bootstrap — WARN**

- `Bootstrap/` 폴더 명세 위반 (CLAUDE.md 갱신 필요)
- 매니저 워밍업 순서가 `BootstrapScene.Start`에 산재
- GameManager는 매니저 참조 허브 역할 없이 GameData만 보유
- BootstrapFlow 자체 흐름(버전/공지/자동로그인/약관 4단계 + 503 폴링)은 모범

---

## 2장. Phase 2 — 구현 품질 (unity-qa)

### 2.1 Managers + Bootstrap + Utils + 컴파일 (Q1, Q2, Q3)

**Q3 컴파일 — FAIL**

```
error CS0103: 'CrashReportManager' 이름이 현재 컨텍스트에 없습니다. (BootstrapFlow.cs:18)
```

`CrashReportManager.cs`가 `Assembly-CSharp.csproj` `<Compile Include>`에 누락. Unity Editor 포커스 후 csproj 재생성 필요. (Unity Editor 내부 컴파일은 정상일 수 있음.)

경고 8건: MSB3277 버전 충돌 2건(MCPForUnity.Editor.dll), CS0649 DTO 역직렬화 필드 4건, CS0414 FadeManager._isFading 미사용 1건.

**Q1 Managers — WARN**

- `EventManager.cs:27` TriggerEvent에서 delegate null → NRE 가능 (`?.Invoke()` 필요)
- `UIManager.cs:23` SceneUI getter 매 호출 `FindObjectsByType` 전수 탐색
- `DataManager.cs:53` LoadJson textAsset null 미체크
- `AdsManager.cs` 이벤트 구독 16건, `OnDestroy` 해제 없음
- `IAPManager.cs` 이벤트 구독 8건, `OnDestroy` 해제 없음

**Q2 Bootstrap + 주변 — WARN**

- **Critical**: 자동 로그인 성공 경로(`BootstrapFlow.cs:206`)에서 `LoginScene` 건너뜀 → `DataManager.LoadData()` 미호출 → Config/TextDict null → NRE 확정
- Domain Reload 미대응: `BootstrapFlow._subscribed/_pollingActive`, `AppResumeFlow._resuming`, `StageSession` (3개)

### 2.2 WebFramework 구현 품질 (Q4, Q5, Q6, Q7)

**Q4 ApiClient — WARN**

- `ApiClient.cs:42,50,63,72,84,97,109` async void 7개 — 내부 예외가 삼켜져 프로세스 크래시 가능 (High)
- `ApiClient.cs:27,30,35` static 3개 Domain Reload 미대응 — 에디터 재Play 시 토큰 갱신 데드락/점검 차단 (High)
- CancellationToken 미구현 — 씬 전환 중 MissingReferenceException 가능 (Medium)
- `AuthManager.Instance` null 미체크 5건 (Medium)
- 버튼 중복 클릭 방지 패턴 부재 (Medium)

**Q5 Api — WARN**

- `ItemApi.cs:12` + `InventoryModels.cs:5-9` `UseItemRequest.quantity` 필드 누락 — CLIENT_GUIDE 명세 불일치

**Q6 AuthManager — WARN**

- `AuthManager.cs:36-37` RefreshToken PlayerPrefs 평문 저장 (Critical → 3장에서 상세)
- `AuthManager.cs:9,13` static event 2개 Domain Reload 미대응

**Q7 Models — WARN**

- `StageModels.cs:6-28` StageMasterDto 서버 필드 3개(`rewardTableCode` 등) 누락

### 2.3 Scenes + UI + 한국어 주석 (Q8, Q9, Q10)

**Q8 Scenes — WARN**

- `LoginScene.cs:26`, `BootstrapFlow.cs:206` DataManager.LoadData() 자동 로그인 경로 누락 (Critical 재확인)
- 씬 전환 중 API 요청 취소 미처리(CancellationToken 미전파)

**Q9 UI — WARN**

- `UI_WithdrawPopup.cs:54-67` 이중 클릭 방지 플래그 누락 (High — 탈퇴/종료 등 파괴적 동작)
- `UI_MailPopup.cs:29`, `UI_InquiryPopup.cs:26`, `UI_InventoryPopup.cs:39`, `UI_ShopPopup.cs:58` async void OnEnable 4건 (High)
- `PopupService.cs:14,65` + `UI_Log.cs:10` + `UI_MainGame.cs:9` Domain Reload 미대응 static 필드 4개

**Q10 한국어 주석 — WARN (경미)**

- 96개 파일 중 19개(19.8%) 한국어 주석 전무 — 대부분 초기 프레임워크/외부 템플릿 유래
- WebFramework 34개 파일은 100% 한국어 주석 보유
- 영문 전용 주석 위반 2건: `AdsManager.cs`, `UI_Toolkit.cs`
- 기준(20%) 약간 초과이나 초기 코드 기원으로 경미 판정

---

## 3장. Phase 3 — 안정성·보안 (unity-qa)

### 3.1 인증·토큰·세션 보안 (S1, S2, S3)

**S1 토큰 저장 — FAIL (Critical)**

`AuthManager.cs:58-59` RefreshToken/PlayerId를 `PlayerPrefs` 평문 저장. Android SharedPreferences / iOS NSUserDefaults에서 루팅·탈옥 기기가 직접 읽기 가능. 탈취 시 무기한 세션 재발급 가능. AccessToken은 메모리 전용 — 양호.

**S2 서버 URL·키 하드코딩 — FAIL**

- `ApiConfig.cs:5` `http://localhost:5058` 하드코딩 — 프로덕션 MITM 노출 (High)
- `GoogleSignInProvider.cs:10` Google Web Client ID 소스 직접 박힘 (Medium)
- dev/staging/prod 환경 분리 구조 없음 (Medium)

**S3 OAuth 흐름 — WARN**

- `UI_MainGame.cs:253-266` 로그아웃/탈퇴 시 `GoogleSignInProvider.SignOut()` 미호출 — 공유 기기 구글 세션 잔존 (High)
- 회원 탈퇴 시 Google `Disconnect(Revoke)` 미호출 (Medium)
- Authorization 헤더 `Bearer {token}` RFC 6750 준수 — 양호
- Google ID Token 서버 측 검증 표준 흐름 준수 — 양호
- `Clear()` 호출 경로 5곳 모두 `PlayerPrefs.DeleteKey` 완전 삭제 확인 — 양호

### 3.2 네트워크 안정성·에러 처리·429 재시도 (S4, S5, S6)

**S4 ApiClient 안정성 — WARN**

- CancellationToken/Abort 미구현 → 씬 전환 시 MissingReferenceException (High)
- `ApiClient.cs:27-36` static 3개 Domain Reload 미대응 (High, Q4-02 재확인)
- 429 재시도(지수 백오프 5s/10s/20s, 최대 3회, Retry-After 헤더 우선) — **모범 구현**
- Retry-After 음수 값 시 `Task.Delay` ArgumentOutOfRangeException (Medium)

**S5 에러 분기 — WARN**

- `ApiClient.Handle401` → `EEventType.SessionExpired` 발행하나 **구독자 없음** → LoginScene 전환 누락 (Medium)
- `UI_InventoryPopup` Status 기반 분기 vs `UI_ShopPopup` ErrorCode 기반 분기 불일치 (Medium)
- 503 점검 폴링 흐름(인터셉터→이벤트→팝업→30초 폴링→재개) — **완결**
- Api/ 12개 IsSuccess 체크 누락 없음 — 양호

**S6 CrashReportManager·AppLifecycle — PASS**

CrashReportHandler 기반 Unity Cloud 전송, 이벤트 구독/해제 짝, AppResumeFlow 재진입 가드, 포그라운드 복귀 흐름 모두 양호.

### 3.3 메모리·이벤트·리소스 (S7, S8, S9)

**S7 이벤트 구독·해제 — WARN**

- `AdsManager.cs` LevelPlay/RewardedAd/InterstitialAd 총 18개 `+=` 구독, `OnDestroy` 자체 미선언 — Domain Reload Off 시 이중 구독 확정 (High)
- `IAPManager.cs` StoreController 8개 이벤트 해제 누락 (Medium)

**S8 DontDestroyOnLoad·FindObject·Resources — WARN**

- `UIManager.cs:23` SceneUI getter 매 접근마다 `FindObjectsByType` 전수 탐색, 캐시 무시 (Medium)
- `BackButtonHandler.cs:31` Escape 키 입력마다 `FindFirstObjectByType<BaseScene>()` 호출 (Low)
- DontDestroyOnLoad 중복 생성 위험 없음 — 양호

**S9 코루틴·Update·SaveManager — WARN**

- `SaveManager.cs:72` `JsonConvert.DeserializeObject` 예외 미처리 → JSON 손상 시 앱 크래시 (High)
- Domain Reload 미대응: `BootstrapFlow._subscribed/_pollingActive`, `AppResumeFlow._resuming`, `UI_MainGame._dailyLoginChecked` (4개, ApiClient 3개와 합산 총 **7개 이상**)
- `SaveManager.cs:58` 동기 IO 메인 스레드 10초마다 실행 (현 규모 수용 가능, 확장 시 비동기 전환)
- `OnApplicationFocus` 미구현 → Android 일부 기기 Focus-only 이탈 시 복귀 흐름 미트리거 (Low)
- 코루틴 StartCoroutine/StopCoroutine 짝 전수 확인 — 양호

---

## 4장. Critical Issues

### C1 — CS0103 빌드 실패 (CrashReportManager.cs csproj 누락) ✅ 해결

| 항목 | 내용 |
|---|---|
| 심각도 | ~~Critical~~ **해결** |
| 점검 ID | Q3 |
| 파일·라인 | `BootstrapFlow.cs:18` (참조측) |
| 이슈 | `CrashReportManager.cs`가 `Assembly-CSharp.csproj`에 누락 → 외부 빌드(CI/CD, Rider/VS) 차단 |
| 해결 | `Assembly-CSharp.csproj` + `CrashReportManager.cs.meta`를 `702ce2c` 커밋에 amend → `2257beb` |

### C2 — 자동 로그인 경로 DataManager.LoadData() 미호출 ✅ 해결

| 항목 | 내용 |
|---|---|
| 심각도 | ~~Critical~~ **해결** |
| 점검 ID | Q2-05, Q8-01 |
| 파일·라인 | `BootstrapFlow.cs:206`, `LoginScene.cs:26` |
| 이슈 | RefreshToken 자동 로그인 성공 시 `LoginScene`을 건너뛰어 MainScene 직행 → `DataManager.LoadData()` 미호출 → Config/TextDict null → AdsManager.Init(), IAPManager.FetchProducts() 등에서 NRE 확정 |
| 해결 | `BootstrapFlow.TryAutoLoginAsync` 성공 분기에 `DataManager.Instance.LoadData()` 추가. `DataManager`에 `_isLoaded` 가드 + `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` AutoInit 추가 → 어느 씬 직접 진입해도 보장 |

### C3 — RefreshToken PlayerPrefs 평문 저장

| 항목 | 내용 |
|---|---|
| 심각도 | Critical |
| 점검 ID | S1-1, Q6-01 |
| 파일·라인 | `AuthManager.cs:58-59` |
| 이슈 | RefreshToken + PlayerId가 `PlayerPrefs`에 평문 저장 → 루팅·탈옥 기기에서 직접 탈취 → 무기한 세션 재발급 가능 |
| 권고 | Android Keystore / iOS Keychain 마이그레이션. 단기: `Clear()` 확실성 확인 완료(5곳), 중기: 플랫폼 보안 저장소 전환 |

---

## 5장. High Issues

| # | 점검 ID | 파일·라인 | 이슈 | 권고 |
|---|---|---|---|---|
| ~~H1~~ ✅ | A3 | `Managers/CrashReportManager.cs:37,38,77,78` | ~~Managers→WebFramework/Auth 역방향 의존~~ | **해결**: `AuthManager.OnLoginSuccess/OnLogout` 정적 이벤트 제거 → `EventManager.TriggerEvent(LoginSuccess, playerId)` / `TriggerEvent(Logout)` 경유 전환. `EventManager`에 `Action<object>` 페이로드 오버로드 3종 신규 추가. `CrashReportManager` 구독 대상 EventManager로 교체, `OnLoginSuccess(object)` 시그니처 변경. |
| ~~H2~~ ✅ | A3 | `Managers/AppLifecycleManager.cs:51,72` | ~~Managers→WebFramework/Auth 역방향 의존~~ | **해결**: `AuthManager.Instance.IsLoggedIn` → `_isLoggedIn` 로컬 캐시(`LoginSuccess`/`Logout` 이벤트 동기화)로 대체. `AuthManager.Instance.Clear()` 직접 호출 → `AuthManager`가 `SessionExpired` 자가 구독하여 `Clear()` 수행(책임 이관). `AppLifecycleManager.OnSessionExpired` 정적 이벤트·`NotifySessionExpired()` 제거. |
| H3 | A3 | `Managers/ShoutManager.cs:45,160,179` | Managers→Api/Auth/Core 역방향 다중 결합 | Managers 유지 + PlayerId EventManager 캐싱 + ServerTime 공용 정적 예외 CLAUDE.md 명시 |
| ~~H4~~ ✅ | A4·A6 | `Core/ApiClient.cs:241,342,373,391,420,473` | ~~Core↔Auth·UI 양방향 결합 6건~~ | **해결**: `ITokenProvider` 인터페이스 신규(`Core/`) + `AuthManager : ITokenProvider` 구현 + Awake 자가 등록. `ApiClient` 내 `AuthManager` 직접 참조 5곳 → `TokenProvider?.` 치환. QA 승인. |
| ~~H5~~ ✅ | Q4-01 | `ApiClient.cs:42,50,63,72,84,97,109` | ~~async void 7개 — 예외 삼킴~~ | **해결**: `async Task` 전환 + 내부 try-catch → `HandleCallbackException` 헬퍼로 중복 제거. QA 승인. |
| H6 ⏸ | Q4-02 | `ApiClient.cs:27,30,35` | static 3개 Domain Reload 미대응 — 토큰 갱신 데드락 | **보류** — 에디터 전용 현상(빌드·실유저 영향 0%). Domain Reload 기능을 끈 환경에서만 발현. 릴리즈 직전 필요 시 재검토. |
| ~~H7~~ ✅ | Q9-01 | `UI_WithdrawPopup.cs:54-67` (→ UI_MainGame.DoWithdraw) | ~~이중 클릭 방지 누락~~ **해결** — `UI_Base.RunWithBusyAsync`(Layer A 글로벌 마스크) 도입, 시각 피드백까지 격상 |
| ~~H8~~ ✅ | Q9-02 | `UI_MailPopup.cs:29` 외 3건 | ~~async void OnEnable 4건 — 팝업 닫힘 후 비활성 객체 접근~~ | **해결**: `UI_Base`에 `_enableCts`/`EnableToken` CTS 인프라 추가. OnEnable에서 발급, OnDisable에서 Cancel→Dispose. 4개 팝업 OnEnable → try/catch(OCE) 래핑, LoadXxxAsync(CancellationToken) + await 직후 ThrowIfCancellationRequested() 일괄 적용. QA 승인. **후속 수정**: `catch (OperationCanceledException)` 추가 시 `using System;` 누락 → `dotnet build` 실패. Unity 에디터는 암묵적 참조 덕분에 통과했으나 외부 빌드에서 strict 오류. `using System;` 추가로 해결. |
| ~~H9~~ ✅ | S2-1 | `ApiConfig.cs:5` | ~~`http://localhost:5058` 하드코딩 — 프로덕션 MITM~~ | **해결**: `NetworkConfig` SO 신설(Dev/Staging/Prod URL 필드, `#if` 환경 분기), `ApiConfig.cs`에서 `BaseUrl` 제거, `ApiClient` 15곳 → `DataManager.Instance.NetworkConfig.BaseUrl` 전환. 릴리즈 빌드 `http://` 감지 `LogError`. QA 승인. |
| H10 | S3-3 | `UI_MainGame.cs:253-266` | 로그아웃/탈퇴 시 GoogleSignInProvider.SignOut() 미호출 | `AuthManager.Clear()` 전후에 `SignOut()` 호출 추가 |
| ~~H11~~ ✅ | S7 | `AdsManager.cs` 전체 | ~~LevelPlay/RewardedAd/InterstitialAd 18개 이벤트 해제 전무~~ | ~~`OnDestroy()` 추가 + `Init()` `_initialized` 가드~~ **해결**: `OnDestroy()` 추가(18개 이벤트 전량 `-=`), `bool _initialized` 가드로 중복 Init 방지, `SdkInitializationCompletedEvent`에 `if (this == null) return;` 경쟁 상태 가드 추가. |
| ~~H12~~ ✅ | S9 | `SaveManager.cs:72` | ~~JSON 역직렬화 예외 미처리 → JSON 손상 시 앱 크래시~~ | ~~try-catch 추가, 실패 시 `Reset()` 기본 데이터 복구~~ **해결**: 단일 `catch (System.Exception)` + 손상 파일 `.corrupted` 백업 + `DeserializeObject` null 가드 + `Reset()` 복구. QA 승인. |

---

## 6장. Medium Issues

| # | 점검 ID | 파일·라인 | 이슈 | 권고 |
|---|---|---|---|---|
| M1 | A1 | `@Scripts/Bootstrap/` | 규정 외 폴더 — CLAUDE.md 명세 위반 | CLAUDE.md 폴더 목록에 `Bootstrap` 추가 (권고) |
| M2 | A1, A7 | `Managers/SceneManager.cs:5`, `LoadingScene.cs:39` | `UnityEngine.SceneManagement.SceneManager`와 식별자 충돌 + 우회 주석 박제 | `AppSceneManager`로 개명 |
| ~~M3~~ ✅ | Q1-01, S9-M3 | `EventManager.cs:27` | TriggerEvent delegate null → NRE | ~~`_events[eventType]?.Invoke()`~~ **해결** |
| M4 | Q1-05 | `DataManager.cs:53` | LoadJson textAsset null 미체크 | null 체크 + LogError 추가 |
| M5 ⏸ | Q2-01~03 | `BootstrapFlow.cs:9,12` / `AppResumeFlow.cs:10` / `StageSession.cs` | static 필드 Domain Reload 미대응 (3개) | **보류** — H6와 동일 사유. |
| M6 | Q4-03 | `ApiClient.cs` 전체 | CancellationToken 미구현 — 씬 전환 MissingRef | `destroyCancellationToken` 또는 호출자 단위 CTS |
| ~~M7~~ ✅ | Q4-04 | UI 전체 | ~~버튼 중복 클릭 방지 부재~~ **해결** — 3계층 가드(Layer A 글로벌 마스크 7 + Layer B 버튼 비활성 5 + Layer C 재진입 1) + `UI_Base.RunWithBusyAsync`/`GuardReentry` |
| M8 | Q4-05 | `ApiClient.cs:342,373,391,420,473` | `AuthManager.Instance` null 미체크 5건 | null-conditional `?.` 적용 |
| M9 | Q5-02, Q7-02 | `ItemApi.cs:12`, `InventoryModels.cs:5-9` | `UseItemRequest.quantity` 필드 누락 (CLIENT_GUIDE 불일치) | quantity 필드 추가, ItemApi.UseAsync 시그니처 확장 |
| M10 ⏸ | Q6-02 | `AuthManager.cs:9,13` | static event 2개 Domain Reload 미대응 | **보류** — H6와 동일 사유. |
| M11 | Q7-01 | `StageModels.cs:6-28` | StageMasterDto 서버 필드 3개 누락 | `rewardTableCode` 등 누락 필드 추가 |
| M12 ⏸ | Q9-04,05 | `PopupService.cs:14,65`, `UI_Log.cs:10` | static 필드 Domain Reload 미대응 (2개) | **보류** — H6와 동일 사유. |
| M13 | Q9-06, S8-M1 | `UIManager.cs:23` | SceneUI getter 매 접근 `FindObjectsByType` — 캐시 무시 | `if (_sceneUI != null) return _sceneUI;` 선행 체크 |
| M14 | S2-2 | `GoogleSignInProvider.cs:10` | Google Web Client ID 소스 하드코딩 | `Config/AuthConfig.asset` ScriptableObject 분리 |
| M15 | S2-4 | `ApiConfig.cs` | dev/staging/prod 환경 분리 없음 | ScriptableObject 또는 #if 전처리 분기 도입 |
| M16 | S3-4 | `GoogleSignInProvider.cs` | 회원 탈퇴 시 Google Disconnect(Revoke) 미호출 | `Disconnect()` wrapper 추가 + 탈퇴 흐름 연결 |
| ~~M17~~ ✅ | S5 | `AppLifecycleManager.cs:9` vs `Define.cs:25` | ~~`EEventType.SessionExpired` 구독자 없음 — Handle401 세션 만료 시 LoginScene 전환 누락~~ | **해결**: H2 해소 과정에서 `AppLifecycleManager`가 `EventManager.AddEvent(SessionExpired, HandleSessionExpired)`로 구독. `ApiClient` → `EventManager.TriggerEvent(SessionExpired)` → `AppLifecycleManager.HandleSessionExpired()` → `LoadScene(LoginScene)` 흐름 완성. `AppLifecycleManager.OnSessionExpired` 정적 이벤트 및 `NotifySessionExpired()` 헬퍼 제거. |
| M18 | S5 | `UI_InventoryPopup.cs:126-153` vs `UI_ShopPopup.cs` | 에러 분기 Status 기반 vs ErrorCode 기반 불일치 | ErrorCode 기반으로 통일 |
| M19 | S7 | `IAPManager.cs` 전체 | StoreController 8개 이벤트 해제 누락 | `OnDestroy()` 추가하여 이벤트 `-=` 해제 |

---

## 7장. Low / 추적 항목

| 점검 ID | 파일·라인 | 이슈 | 권고 |
|---|---|---|---|
| A1 | `Managers/BackButtonHandler.cs` | 명명 일관성 (`*Manager`가 아님) | `BackButtonManager` 개명 검토 |
| A1 | `Utils/PriorityQueue.cs:4` | 단독 `namespace Rookiss` — 전 코드베이스 글로벌 네임스페이스 불일치 | 글로벌로 통일 |
| A8 | `UI_Toolkit.cs` | 베이스 정의되어 있으나 상속 클래스 0 | 유지 명시(DEVNOTES) 또는 제거 결정 |
| Q1-02 | `UIManager.cs:23` | SceneUI getter 성능 (M13과 동일, 중복) | — |
| Q1-04 | `SoundManager.cs:26-33` | null AudioClip 캐싱 후 전파 | null 체크 추가 |
| Q1-06 | `PoolManager.cs:118` | Clear()에서 풀링 오브젝트 미파괴 → 메모리 잔류 | Pool별 오브젝트 파괴 로직 추가 |
| Q8-04 | `LoginScene.cs:41-48` 외 2건 | QuitApplication() 3중 중복 | 유틸리티 메서드로 통합 |
| Q10-01 | 19개 파일 | 한국어 주석 전무 (초기 프레임워크 코드) | 점진적 추가 — 우선순위 낮음 |
| Q10-02 | `AdsManager.cs`, `UI_Toolkit.cs` | 영문 전용 주석 (CLAUDE.md 위반) | 한국어로 교체 |
| S1-4 | `AuthModels.cs:12-18` | JWT exp claim 미관리 | expiresIn 필드 추가 검토 |
| S4 | `ApiClient.cs:512-519` | Retry-After HTTP-date 형식 미지원 | CDN 경유 대비 date 파싱 추가 검토 |
| S8 | `BackButtonHandler.cs:31` | Escape 키 입력마다 `FindFirstObjectByType` 호출 | `SceneManager.Instance.CurrentScene` 대체 |
| S9 | `AppLifecycleManager.cs` | `OnApplicationFocus` 미구현 → Android 일부 기기 복귀 흐름 미트리거 | Android 타겟 시 Focus 핸들러 보조 구현 |
| S9 | `SaveManager.cs:58` | File.WriteAllText 동기 IO 메인 스레드 10초마다 (현 규모 수용 가능) | GameData 확장 시 비동기 IO 전환 |

---

## 8장. DEVNOTES.md 갱신 권고

다음 항목을 `DEVNOTES.md`의 `[설계 결정]` 또는 `[TODO]` 섹션에 추가할 것을 권고합니다.

1. **`Bootstrap/` 폴더 명세**: CLAUDE.md `스크립트 구조` 표에 `Bootstrap/` 항목 추가 (앱 초기화 흐름 정적 헬퍼)
2. **ServerTime 재사용 원칙 표 추가**: CLAUDE.md WebFramework 재사용 원칙 표에 `Core/ServerTime` 공용 정적 유틸 항목 추가 — 모든 폴더에서 자유롭게 참조 허용 명시
3. **ITokenProvider 인터페이스 도입 계획**: Core↔Auth 결합 해소 로드맵 (약 7라인 변경 + 인터페이스 1개)
4. **AuthManager static event → EventManager 통합 계획**: `OnLoginSuccess/OnLogout`을 `EEventType.LoginSuccess/Logout`으로 이전 시 Managers 역방향 의존 3건 동시 해소
5. **Domain Reload 미대응 일괄 처리 계획**: `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 패턴을 `ApiClient`, `BootstrapFlow`, `AppResumeFlow`, `AuthManager`, `PopupService`, `UI_Log`, `UI_MainGame`, `RestLogger`에 일괄 적용 (8개 static 필드)
6. **AppSceneManager 개명 계획**: `Managers/SceneManager` → `AppSceneManager` 개명 + `LoadingScene` 우회 주석 제거
7. **RefreshToken 보안 저장소 마이그레이션 계획**: 릴리즈 전 Android Keystore / iOS Keychain 전환 필수

---

## 부록 A. 청크 산출물 인덱스

| 청크 | 에이전트 | 파일 | 점검 ID |
|---|---|---|---|
| P1.1 | unity-architect | `review/round_20260514/p1_1_structure_dependency.md` | A1, A2, A3 |
| P1.2 | unity-architect | `review/round_20260514/p1_2_webframework.md` | A4, A5, A6 |
| P1.3 | unity-architect | `review/round_20260514/p1_3_scene_ui_bootstrap.md` | A7, A8, A9 |
| P2.1 | unity-qa | `review/round_20260514/p2_1_managers_compile.md` | Q1, Q2, Q3 |
| P2.2 | unity-qa | `review/round_20260514/p2_2_webframework_quality.md` | Q4, Q5, Q6, Q7 |
| P2.3 | unity-qa | `review/round_20260514/p2_3_scenes_ui_comments.md` | Q8, Q9, Q10 |
| P3.1 | unity-qa | `review/round_20260514/p3_1_auth_security.md` | S1, S2, S3 |
| P3.2 | unity-qa | `review/round_20260514/p3_2_network_stability.md` | S4, S5, S6 |
| P3.3 | unity-qa | `review/round_20260514/p3_3_memory_events_resources.md` | S7, S8, S9 |
