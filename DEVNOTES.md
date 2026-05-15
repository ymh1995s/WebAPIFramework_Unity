# GameClient Dev Notes

## [주의] 배포 전 교체 항목

### Google OAuth - Android 서명 키 등록

현재 Google Cloud Console에 **debug.keystore**의 SHA-1 지문으로 Android OAuth 클라이언트 ID가 등록되어 있음.

| 항목 | 현재 값 (개발용) | 출시 시 |
|------|-----------------|---------|
| SHA-1 지문 | `BA:19:19:19:9D:44:5F:0E:79:DE:AF:0A:F7:A6:FC:67:91:75:8C:0B` | release keystore에서 새로 추출 |
| OAuth 클라이언트 이름 | WebFramework Android Debug | release용 별도 생성 필요 |

**출시 시 할 일:**
1. release keystore 생성
2. release keystore SHA-1 추출
3. Google Cloud Console에서 Android OAuth 클라이언트 ID 추가 (release용)

### SHA-1 추출 명령어
```
keytool -list -v -keystore <keystore경로> -alias <alias> -storepass <password>
```

---

### 약관 비동의 시 앱 종료 — 에디터 임시 우회

약관 팝업의 비동의 콜백이 `#if UNITY_EDITOR` 분기로 에디터에서는 `Debug.Log`만 출력. 빌드는 `Application.Quit()` 정상 동작. 빌드 시점 재검증 필수.

**위치**: `UnityClient/Assets/@Scripts/UI/PopupService.cs` `ShowTerms` `onCancel` 콜백 (L199-204)

---

### 문의 메시지 고정 문구

`UI_InquiryPopup.OnClickInquiry`가 고정 문구 `"문의합니다. 처리 부탁드립니다."`를 전송. 프리팹에 InputField가 없어 임시 처리한 상태이며, 입력 UI 추가 시 교체 필요.

**위치**: `UnityClient/Assets/@Scripts/UI/UI_InquiryPopup.cs:75-77`

---

## [TODO] 미구현 핵심 항목 — 출시 차단 수준

### IAP 영수증 검증

클라이언트 코드 완성 (`IapApi`, `IapModels`, `IAPManager` 검증 흐름). **Google Play Console 작업만 남음**:
- Google Play Console 상품 ID 등록 (`com.rookiss.s2.100gold` 등)
- Google 서비스 계정 생성 + 백엔드 연동
- 실기기 E2E 테스트

**근거**: `../CLIENT_GUIDE.md` 19/20번

---

### 광고 SDK PlayerId 미전달 (SSV 매핑 불가)

클라이언트 흐름 코드 완성 (`TryApplyDynamicUserId` + 케이스 A/B 분기 + Logout 리셋). **LevelPlay SDK 인프라 설정 후 주석 해제만 하면 동작**.

남은 작업:
- LevelPlay 대시보드 App Key / 광고 유닛 ID 등록 후 `AdsManager.cs:169` 주석 해제 (`IronSource.Agent.setDynamicUserId(playerId)`)
- 실기기 E2E 테스트 (광고 시청 → SSV → 우편 보상 수령)

**근거**: `../CLIENT_GUIDE.md` 21번

---

## [TODO] 출시 전 보강 항목

### RefreshToken 보안 저장 (Keystore / Keychain)

**현재**: `AuthManager.cs:27,49` PlayerPrefs 평문 저장. 루팅·탈옥 기기에서 RefreshToken 노출 → 계정 도용 가능.
**우선순위 메모**: 1인 인디 출시 직전에는 필수 아님. **결제 매출이 붙기 시작하면(월 100만↑/MAU 1만↑) 우선순위 급상승**.
**필요 작업**
- 1차: AES 래퍼로 30분 투자 (키 바이너리에 박히지만 평문보단 100배 안전)
- 2차: 네이티브 플러그인 — Android `EncryptedSharedPreferences`(Jetpack Security) + iOS `Keychain Services`
**근거**: `../CLIENT_GUIDE.md` 부록 C

---

### 광고 제거 IAP 처리

클라이언트 코드 완성 (`OnCheckEntitlement` → `ApplyNoAdsEntitlement()`, `AdsManager.IsAdsRemoved` 가드). **LevelPlay SDK 인프라 설정 후 즉시 동작 가능**.

남은 인프라 작업:
- LevelPlay 대시보드 App Key 등록 + `AdsConfig` 실제 값 입력
- 전면 광고 / 보상형 광고 유닛 ID 등록
- 실기기 E2E 테스트 (광고 표시 → noads 구매 → 광고 차단 확인)

---

### Local Notification — 트랜잭션 알림 한정

**목적**: 출석 리셋·우편 만료 임박·에너지 충전 완료 등 게임 내부 이벤트 알림.
외부 푸시 서버(FCM) 없이 기기 자체 예약 → 서버 인프라 추가 없이 재방문율 ↑.

**필요 작업**
- 패키지 `com.unity.mobile.notifications` 도입 (Unity Package Manager)
- iOS: `UNUserNotificationCenter.requestAuthorization` 권한 다이얼로그 (첫 등록 시)
- Android 13+: `POST_NOTIFICATIONS` 런타임 권한 요청
- 앱 진입 시 예약 알림 취소 (이미 받은 보상 중복 알림 방지)
- 앱 내 알림 수신 토글 UI (사용자가 끌 수 있어야 함 — 마켓 권장)

**범위 제한**: 광고성 알림("신규 이벤트!", "할인 중!")은 절대 보내지 말 것.
보내려면 정보통신망법 §50 사전 동의 + §50의5 야간(21~08) 동의 + 백엔드 동의 컬럼 추가 필요.

**우선순위**: M (가성비 큼) / **작업량**: 1일

---

### GoogleSignIn WEB_CLIENT_ID 하드코딩

`GoogleSignInProvider.cs:10` 에 OAuth 클라이언트 ID가 문자열 리터럴로 박혀 있음. Dev/Staging/Prod 빌드별 분기 불가.

**중요도**: 낮음 — Dev/Prod OAuth 클라이언트 ID를 분리할 계획이 없으면 그냥 놔둬도 무방. 환경이 하나라면 현재 코드로 기능상 문제 없음.

**필요 작업**
- `Config/` 하위에 `AuthConfig.cs` ScriptableObject 신설 (또는 `GameConfig`에 필드 추가)
- `GoogleSignInProvider`가 `DataManager.Instance.AuthConfig.WebClientId` 로 읽도록 교체
- CLAUDE.md 원칙: "빌드 환경별 값은 `Config/` SO에 집중"

**근거**: CLAUDE.md Config/ 카탈로그 원칙

---

### IsGoogleLinked 다기기 불일치

`AuthManager.cs:23-24` — `IsGoogleLinked` 를 PlayerPrefs 로컬로만 추적. 재설치 / 다기기 / 자동 로그인 시 구글 연동 여부가 false 로 잘못 표시될 수 있음 → IAP 흐름에서 불필요한 구글 연동 안내가 뜰 가능성.

**중요도**: 낮음 — 실제 연동 여부는 서버가 기준이므로 기능 자체는 정상 동작. 최악의 경우 재설치 직후 불필요한 "구글 연동 필요" UI가 뜨는 UX 이슈 수준.

**필요 작업**
- 토큰 갱신(`POST /api/auth/refresh`) 응답 또는 자동 로그인 직후 서버 상태 기준으로 동기화
- 백엔드 TokenResponse에 `isGoogleLinked` 필드 추가가 어려우면: 로그인 직후 별도 상태 조회 (백엔드 제약 있으면 우선 우회책으로 Refresh 응답 파싱 시 Google IdToken 존재 여부 추론)

---

### 퀘스트 API 클라이언트 미작성

서버 구현 완료(`GET /api/quests`, `POST /api/quests/{questId}/claim`), 클라이언트 Api 클래스·DTO·UI 전무.

**필요 작업**
- `WebFramework/Api/QuestApi.cs` (static class)
- `WebFramework/Models/QuestModels.cs` (`QuestDto`, `QuestClaimResponse`)
- `ApiConfig`에 퀘스트 경로 상수 추가
- UI: 퀘스트 목록 팝업 or MainScene 퀘스트 탭

**근거**: `../CLIENT_GUIDE.md` 부록 B

---

### 튜토리얼 API 클라이언트 미작성

서버 구현 완료(`GET /api/tutorial`, `PUT /api/tutorial/{key}`), 클라이언트 미연동.

**필요 작업**
- `WebFramework/Api/TutorialApi.cs`
- `WebFramework/Models/TutorialModels.cs` (`TutorialStateDto`)
- 튜토리얼 흐름 연동 (씬/UI TBD)

**근거**: `../CLIENT_GUIDE.md` 부록 B

---

### 리모트 설정 API 클라이언트 미작성

서버 구현 완료(`GET /api/remoteconfig`, key-value 사전 반환), 클라이언트 미연동.

**필요 작업**
- `WebFramework/Api/RemoteConfigApi.cs`
- `WebFramework/Models/RemoteConfigModels.cs`
- 캐시 정책 결정 (앱 시작 1회 조회 vs 주기 갱신)

**근거**: `../CLIENT_GUIDE.md` 부록 B

---

### ResourceManager 호출 매직 스트링 일괄 정리

`ResourceManager.Instance.Instantiate(...)` / `Get<T>(...)` 호출부에 프리팹·에셋 키가 문자열 리터럴로 박혀 있음. 컨벤션은 `ShoutManager.HUD_PREFAB_NAME` (클래스 내부 `private const string`, UPPER_SNAKE_CASE)이고 `UIManager.BUSY_MASK_PREFAB_NAME`이 같은 형태로 정리됨. 남은 매직 스트링도 동일 패턴으로 흡수.

**잔존 위치**:
- `UnityClient/Assets/@Scripts/Managers/UIManager.cs:180` — `"UI_Toast"`
- `UnityClient/Assets/@Scripts/UI/UI_UGUI.cs:16` — `"EventSystem"`
- 향후 추가될 `ResourceManager` 호출처 동일 원칙

**필요 작업**: 각 사용 클래스 내부에 `private const string XXX_PREFAB_NAME = "..."` 정의 후 치환.

**전역 상수 클래스 금지**: `Utils/PrefabKey` 같은 전역 컨벤션은 이 프로젝트에 없음. 사용처-국지 const가 정식.

**우선순위**: L (기능 무관) / **작업량**: 10분

---

## 기능 현황

| 기능 | 상태 | 비고 |
|------|------|------|
| 게스트 로그인 | 완료 | DeviceId 기반 |
| 구글 로그인 | 구현완료/테스트필요 | google-signin-unity SDK, 실기기 빌드 필요 |
| 인벤토리 조회 / 아이템 사용 | 완료 | `ItemApi` 통합 (GET /api/items/inventory, POST /api/items/{id}/use), `UI_InventoryPopup` 프리팹 |
| 씬 전환 로딩 + 페이드 | 완료 | `FadeManager`, `LoadingScene`, `UI_LoadingScene` |
| Android 뒤로가기 | 완료 | `BackButtonHandler`, `BaseScene.OnBackButton` 가상 메서드 |
| Safe Area | 완료 | `SafeAreaFitter` 컴포넌트 — 모든 씬/팝업 프리팹 적용 |
| 앱 생명주기 (포그라운드 복귀) | 완료 | `AppLifecycleManager` — 토큰 갱신·점검 재확인 |
| 토스트 UI | 완료 | `UI_Toast`, `PopupService.ShowToast` |
| 부팅 흐름 (버전·공지·자동로그인·약관) | 완료 | `BootstrapFlow.Run` 4단계 — 503 시 30초 자동 폴링 후 흐름 재개 (`OnMaintenanceDetected` / `StartMaintenancePollingAsync`) |
| 강제 업데이트 | 완료 | 요구사항(Confirm 후 속행)과 다름 — **스토어 이동 후 `Application.Quit()`으로 진행 차단** (`BootstrapFlow.CheckVersionAsync`) |
| 약관 동의 팝업 | 완료 | 전용 `UI_TermsPopup` 프리팹 없음 — `UI_WithdrawPopup`(Select) 재사용, `PlayerPrefs "TermsAccepted"`로 1회 저장 |
| 점검/업데이트/보상/안내/에러/약관 팝업 | 완료 | 전용 프리팹 없음 — **`UI_ConfirmPopup` 단일 프리팹을 `PopupService` 헬퍼로 재사용** |
| 에러 팝업 OK 동작 | 완료 | OK 시 `RestartGame` — 에디터는 PlayMode 종료, 빌드는 `BootstrapScene` 재진입 (`PopupService.RestartGame`) |
| 광고 / IAP 버튼 | 미구현(의도) | 버튼 존재, 클릭 시 `"준비 중입니다."` 토스트 (`UI_MainGame.OnClickInAppPurchase` / `OnClickAds`) |
| IAP 영수증 검증 | 코드완료/인프라미준비 | `IapApi` + `IapModels` 신설, `IAPManager` 검증 흐름 완성. Google Play Console 상품 등록 후 즉시 동작 |
| 광고 제거 IAP | 코드완료/인프라미준비 | `OnCheckEntitlement` → `AdsManager.DisableInterstitial()` + `PlayerPrefs` 캐싱. LevelPlay SDK 설정 후 동작 |
| 광고 SSV PlayerId 전달 | 코드완료/인프라미준비 | `TryApplyDynamicUserId` 케이스 A/B 구현. SDK 설정 후 `AdsManager.cs:169` 주석 해제로 활성화 |
| 랭킹 조회 | 완료 | 요구사항(디버그 로그)보다 확장 — `PopupService.ShowAnnouncement`로 순위 / 닉네임 / 최고점수 표시 |
| 스테이지 선택 프레임워크 | 완료 | `StageApi.GetProgress` — `sortOrder` 정렬, `isLocked` 잠금, `StageSession.StageId` 정적 전달 |
| 구글 계정 충돌 해소 | 완료 | 로그인 시 409 `GOOGLE_ACCOUNT_CONFLICT` → 전환 확인 팝업 → `ResolveGoogleConflict` (`UI_LoginScene`) |
| 일일 출석 | 완료 | 세션당 1회 `DailyLoginApi.Process` (static `_dailyLoginChecked`), 보상 시 안내 팝업 (`UI_MainGame.ProcessDailyLogin`) |
| 세션 만료 처리 | 완료 | `AppLifecycleManager.NotifySessionExpired` → 토큰 클리어 후 LoginScene 이동 |
| 서버 시간 동기화 | 완료 | `ServerTime` static class — HTTP 응답 `Date` 헤더로 오프셋 보정. `ApiClient.SendAsync`/`DoRefresh` 삽입, `UI_HUDShout`·`ShoutManager` 호출부 치환 |
| 429 Rate Limit 재시도 | 완료 | `bool isRetry` → `int retryCount`. 지수 백오프 5s/10s/20s, 최대 3회, 초과 시 토스트 안내. Retry-After 헤더 우선. 401/429 파라미터 독립 분리 |
| 크래시 수집 (CrashReportManager) | 코드완료/Cloud미연결 | Unity Engine Diagnostics 빌트인. 상세 내용은 하단 별도 섹션 참고 |
| 퀘스트 | 미구현 | 서버 API 완료. 클라 `QuestApi` + DTO + UI 미작성 — TODO 섹션 참고 |
| 튜토리얼 | 미구현 | 서버 API 완료. 클라 `TutorialApi` + DTO 미작성 — TODO 섹션 참고 |
| 리모트 설정 | 미구현 | 서버 API 완료. 클라 `RemoteConfigApi` + DTO 미작성 — TODO 섹션 참고 |
| IsGoogleLinked 동기화 | 부족 | PlayerPrefs 로컬 추적만 — 다기기/재설치 시 불일치 가능. TODO 섹션 참고 |

---

## 크래시 수집 (CrashReportManager) — 활성화 대기 중

`CrashReportManager` (Singleton) — 앱 부팅 시 `BootstrapFlow.Run()`에서 `Init()` 1회 호출.
Unity Engine Diagnostics API로 크래시 자동 포착.
로그인·로그아웃 시 `AuthManager.OnLoginSuccess` / `OnLogout` 이벤트로 PlayerId 메타데이터 실시간 갱신 → 크래시 발생 유저 특정 가능.

**현재 상태**: 코드는 동작하나 Unity Cloud 미연결로 데이터가 서버에 전송되지 않음 (수집 자체가 안 됨).

**활성화 방법 (Unity Editor)**:
1. Project Settings → Services → Unity Cloud 프로젝트 링크
2. Project Settings → Player → Other Settings → `Enable CrashReport API` 체크
3. 이후 빌드부터 크래시 자동 수집 + Unity Dashboard에서 확인 가능

**대안**: Unity Cloud 미사용 시 Sentry Unity SDK로 교체.
sentry.io 무료 계정 + DSN 발급 후 `CrashReportManager` 코드를 Sentry 초기화 코드로 교체.

---

## [법적 의무] 탈퇴 고지 텍스트 관리

**위치**: `UI_MainGame.cs` `OnClickWithdraw()` 팝업 메시지 문자열 (하드코딩)

**법적 근거**: 개인정보보호법 §22 / Google Play User Data Policy / Apple App Store Review 5.1.1(v)
**상세 기준**: `CLIENT_GUIDE.md` 10번 + 부록 A 의무 동작 체크리스트

**고지 항목 5개** — 서버 `PlayerWithdrawalCleaner.PurgeGameDataAsync` 처리 범위에서 도출:

| # | 고지 내용 | 서버 처리 |
|---|---|---|
| 1 | 인게임 프로필·보유 아이템·스테이지 진행도 영구 삭제 | `PlayerProfile`, `PlayerItem`, `StageClear` hard delete |
| 2 | 우편함 및 미수령 보상 전부 소실 | `Mail`, `MailItem` hard delete |
| 3 | 결제로 획득한 아이템·보상 소실 (결제 이력 자체는 법적 의무로 5년 보관) | `IapPurchase` 보존(전자상거래법), 아이템은 `PlayerItem` 삭제 |
| 4 | 재가입 시 기존 데이터 복구 불가 | 모든 게임 데이터 즉시 hard delete |
| 5 | 개인정보(기기 ID·구글 계정·닉네임) 즉시 익명화 | `DeviceId`/`GoogleId` → null, 닉네임 → `"탈퇴유저-{id}"` |

**변경 트리거**: 서버 `WithdrawAsync` 처리 범위 변경(새 테이블 추가·삭제) 시 이 텍스트도 동반 업데이트 필수.

**하드코딩 의도**: 법적 고지 텍스트를 JSON/설정 파일로 분리하면 실수 수정·누락 위험이 높아짐 — 의도적 코드 변경(코드 리뷰 경유)만 허용.

---

## [설계 결정]

### 환경별 설정값 — NetworkConfig ScriptableObject 단일화 (2026-05-15)

`ApiConfig.cs`에 하드코딩된 `http://localhost:5058`을 제거하고 `Config/NetworkConfig` ScriptableObject로 이전.

**원칙**: 빌드 환경에 따라 달라지는 설정값(서버 URL, SDK 키, 피처 플래그 등)은 모두 `Config/` ScriptableObject에 모은다. 코드 리터럴·`Define.cs`·`ApiConfig.cs`에 직접 박지 않는다.

**역할 분리**:
- `NetworkConfig` — 서버 호스트 URL (Dev/Staging/Prod). `#if UNITY_EDITOR||DEVELOPMENT_BUILD` → Dev, `#elif STAGING` → Staging, `else` → Prod
- `ApiConfig.cs` — 엔드포인트 경로 상수만 (`/api/auth/guest` 등). URL 없음
- `Define.cs/PlayerPrefsKey` — 런타임 PlayerPrefs 키 (환경 무관 상수)
- `AdsConfig`, `IAPConfig`, `GameConfig`, `LocalizationConfig` — 각 도메인 환경 설정

**보안**: 릴리즈 빌드에서 ProductionBaseUrl이 `http://`로 시작하면 `Debug.LogError` 출력.

**근거 커밋**: `9b767e0` (H11) → `bce5396` (H4) → 이 작업 (H9)

---

### WebFramework Api 레이어 — async/await + ApiResult<T> + static class (2026-05-12)

기존 콜백 패턴(`Action<T> onSuccess, Action<ApiError> onError`) + `Singleton<MonoBehaviour>` 상속을 폐기. 호출부 가독성 + 신규 도메인 추가 비용 + 안티패턴 청산을 동시 달성.

**채택안**: A(async/await + `ApiResult<T>`) + B'(Api 정적 클래스). 옵션 C(Endpoint 객체)는 URL 경로 치환 컴파일타임 표현 한계 + AOT/IL2CPP 깊은 제네릭 정적 인스턴스 우려로 거부.

- **반환 타입**: 모든 Api 메서드는 `Task<ApiResult<TRes>>` 반환. `ApiResult<T>`는 `readonly struct` — `IsSuccess`/`Value`/`Error(=ApiError)` + `Ok`/`Fail` 정적 팩토리
- **클래스 형태**: `XxxApi`는 `public static class`. 무상태/무라이프사이클이므로 MonoBehaviour Singleton 폐기 (안티패턴 청산 — 빈 GameObject 10개 + `.Instance.` 잡음 + 가짜 라이프사이클 신호 제거)
- **호출부 규칙**: UI 이벤트 핸들러/Start/OnEnable 진입점만 `async void`. 내부 헬퍼는 `async Task`
- **인터셉터 보존**: 401 자동 토큰 갱신, 503 점검, 429 백오프는 `ApiClient` 내부가 자동 처리 — 호출부에서 별도 처리 금지
- **Singleton 잔존**: `ApiClient`, `AuthManager`는 코루틴/세마포어/PlayerPrefs 영속화를 실제 사용 — 그대로 `Singleton<T>` 유지. **상태·라이프사이클이 있는 매니저만 Singleton 정당**
- **WebFramework 외 매니저**: 동일 기준 적용 — `MonoBehaviour` 기능을 실제 사용하지 않으면 `static class`로 작성

**왜 Task인가 (Awaitable 아님)**: `UnityWebRequest.SendWebRequest().GetAwaiter()`는 Task awaitable과 자연스럽게 통합. `Task.Delay`(429 백오프), `SemaphoreSlim.WaitAsync`(401 동시성), 외부 SDK(Google Sign-In `Task<GoogleSignInUser>`) 와의 호환. 게임 로직/부팅 흐름은 Unity 권장인 `Awaitable<T>`를 써도 무방 — 경계에서 `await someTask` 식 혼용 가능

**근거 커밋**: `e8d2e30` (Step 1) → `4c19e43` (Step 2) → `bf1991d`/`e48e3a4`/`a4570fd` (Step 3 A·B·C) → `c591df0` (Step 4)

**개발자 가이드**: 호출 패턴·신규 도메인 추가 절차·ApiClient 메서드 매핑은 `DEVELOPER_GUIDE.md` 참조.
