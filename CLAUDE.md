# GameClient (Unity)

## 프로젝트 목적

이 Unity 프로젝트는 두 가지 목적을 갖습니다.

1. **현재** — `../` 경로의 WebAPIFramework 백엔드(Framework.Api)와의 HTTP REST 통신을 검증하는 더미 클라이언트
2. **목표** — 이후 신규 게임 프로젝트의 시작점이 될 수 있는 Unity 클라이언트 프레임워크

<!-- [이 프로젝트 한정] 아래 백엔드 경로는 현재 프로젝트 구성 기준입니다.
     다른 프로젝트에서 이 GameClient를 템플릿으로 사용할 경우, 연동 대상 백엔드 경로와 프로젝트 목록을 실제 환경에 맞게 수정하세요. -->

### 연동 백엔드 (현재 프로젝트)

`../` 경로의 **WebAPIFramework**는 다음 프로젝트로 구성됩니다:

- `Framework.Api` — ASP.NET Core Web API 서버 (실제 연동 대상)
- `Framework.Application` — 애플리케이션 레이어 (유스케이스)
- `Framework.Domain` — 도메인 모델 및 비즈니스 로직
- `Framework.Infrastructure` — EF Core 기반 데이터 접근
- `Framework.Admin` — Blazor Server 관리 도구

> **⚠ READ-ONLY** — `../Framework/*` 백엔드는 **읽기 전용**이다. 컨트롤러/엔티티/DTO/마이그레이션 등 어떠한 서버 측 코드도 GameClient 작업 중에는 수정·추가·삭제하지 않는다. 백엔드 부재 항목은 클라이언트 측 우회 또는 요구사항 축소로 해결한다.

## 프레임워크 방향

- 씬 관리, UI, 리소스, 사운드, 네트워크 등 공통 시스템을 재사용 가능한 구조로 구축
- 백엔드 연동(HTTP REST) 패턴을 표준화하여 다른 프로젝트에서도 그대로 활용 가능하도록 설계
- `@Scripts` 하위 구조를 유지하며 점진적으로 확장

---

## 🚨 재사용 우선 원칙 (READ FIRST)

**새 코드를 작성하기 전에 기존 자산을 먼저 검색하라.** 비슷한 기능이 이미 있는데 모르고 새로 만드는 것이 이 프로젝트에서 가장 자주 발생하는 실수다. 신규 클래스·매니저·헬퍼·상수 클래스를 만들기 전에 **다음 절차를 무조건 거친다**:

### 작업 시작 전 체크리스트

1. **`@Scripts/Managers/`** 카탈로그를 본다 — 비슷한 책임을 가진 매니저가 이미 있는가?
2. **`@Scripts/Utils/`** 4개 파일을 본다 — Define / Extension / Utils / PriorityQueue. 추가하려는 enum·확장 메서드·정적 헬퍼·자료구조가 이미 있는가?
3. **`@Scripts/UI/`** PopupService·UI_Base·UI_Toast·SafeAreaFitter 등 헬퍼가 이미 있는가?
4. **`@Scripts/WebFramework/`** 백엔드 호출은 반드시 이 폴더 안에서. `*Api`·`ApiClient`·`ApiResult<T>` 패턴 재사용.
5. **유사 기능 검색**: `Grep`으로 키워드(메서드명 후보, 클래스명 후보, 매직 스트링) 사전 검색 필수.

**검색 없이 새 파일을 만들면 거의 100% 컨벤션 위반 또는 중복 구현으로 끝난다.** 실제 사례: `Utils/PrefabKey.cs` 전역 상수 클래스를 만들었으나 프로젝트 컨벤션은 `ShoutManager.HUD_PREFAB_NAME`처럼 **클래스 내부 `private const string`** — 재작업 후 삭제. (2026-05-14)

### 컨벤션 결정 시 표본 3개 확인

특정 패턴을 적용해야 할 때(상수 명명, 키 관리, 매니저 구조 등), **단일 파일만 보지 말고 같은 영역의 3개 이상 표본을 비교해서 정식 컨벤션을 도출**한다. 한 곳의 패턴이 표준이 아닐 수 있다.

예시:
- 프리팹 키 컨벤션 결정 시 → `ShoutManager`, `UIManager`, `UI_UGUI` 의 `ResourceManager.Instantiate` 호출부 모두 확인
- 매니저 구조 결정 시 → 기존 `Manager` 클래스 3개 이상의 Singleton 상속 여부·메서드 명명·필드 명명 비교

---

## 스크립트 구조 + 자산 카탈로그

```
@Scripts/
├── Bootstrap/      앱 부팅·재진입 흐름 (BootstrapFlow, AppResumeFlow)
├── Config/         공통 설정 (AdsConfig, GameConfig, IAPConfig, LocalizationConfig)
├── Controllers/    오브젝트 제어 (ObjectBase, Player)
├── Data/           데이터 모델 (ItemData, TextData, StageSession)
├── Editor/         에디터 전용 도구 (DataTransformer, SaveManagerEditor)
├── Managers/       공통 매니저 — 신규 매니저 만들기 전에 카탈로그 확인 필수 (아래 표)
├── Scenes/         씬 베이스 클래스 (BaseScene 상속)
├── UI/             UI 베이스·헬퍼·구체 UI (UI_Base, PopupService, UI_Toast, UI_BusyMask 등)
├── Utils/          유틸리티 — Define / Extension / Utils / PriorityQueue (아래 표)
└── WebFramework/   백엔드 연동 전용 (Core / Api / Models / Auth)
```

### Managers/ 카탈로그 — 신규 매니저 만들기 전에 이걸 먼저 봐라

| 매니저 | 책임 | 신규 작업 시 확장 vs 신설 판단 기준 |
|---|---|---|
| `Singleton<T>` | 싱글톤 베이스 클래스 | 새 매니저는 반드시 이걸 상속. 상태·라이프사이클이 없으면 `static class`로 가는 게 정석 |
| `UIManager` | UI 인스턴스 생성, 팝업 스택, 토스트 루트, **BusyMask(전화면 입력 차단)** | UI 표시 관련 신규 기능은 거의 다 여기에 추가 |
| `EventManager` | 전역 이벤트 발행/구독 (`Define.EEventType`) | 매니저 간 결합 회피 시 이벤트 사용. 새 이벤트 타입은 `Define.EEventType` enum에 추가 |
| `ResourceManager` | `Resources.LoadAll("PreLoad")` 일괄 캐시, 키 기반 Instantiate/Get | **새 폴더 경로 만들지 말 것** — 모든 프리팹/에셋은 `Resources/PreLoad/Prefabs/...` 하위에 두면 자동 캐시 |
| `SceneManager` | 씬 전환, 페이드, 진입 직전 `UIManager.ForceClearBusy` 호출 | 씬 전환 hook 추가 시 여기 확장 |
| `DataManager` | JSON·SO 로드 (`ResourceManager.Get<T>` 경유) | 새 데이터 타입 추가 시 LoadJson 패턴 그대로 |
| `SoundManager` | BGM/Effect 재생, `AudioClip` 캐시 | 새 사운드는 `Define.ESound` + AudioClip 등록 |
| `SaveManager` | 로컬 세이브 (JSON) | 세이브 항목 추가 시 모델 갱신 + try-catch 정책 따를 것 |
| `PoolManager` / `ObjectManager` | 풀링 / 게임 오브젝트 라이프사이클 | 게임 오브젝트 양산 시 풀링 우선 검토 |
| `FadeManager` | 화면 페이드 인/아웃 | `SceneManager.LoadScene`이 자동 호출 |
| `BackButtonHandler` | Android 뒤로가기 (`BaseScene.OnBackButton` 가상 메서드) | 새 씬에서 뒤로가기 동작 다르게 하려면 BaseScene 오버라이드 |
| `AppLifecycleManager` | 포그라운드 복귀, 토큰 갱신, 점검 재확인 | 앱 생명주기 hook 추가 시 여기 |
| `IAPManager` / `AdsManager` | IAP / 광고 SDK 래퍼 | 외부 SDK 통합은 모두 매니저로 1차 래핑 |
| `CrashReportManager` | Unity Engine Diagnostics 크래시 수집 | Unity Cloud 연결 시 자동 동작 |
| `LocalizationManager` | 다국어 텍스트·폰트 (`Define.ELanguage`) | TMP_Text.SetLocalizedText 확장 사용 |
| `ShoutManager` | HUD 외침 표시 | 프리팹 키 컨벤션 표본: `HUD_PREFAB_NAME` (클래스 내부 `private const string`) |
| `RemoteConfigManager` | 서버 원격 설정 부팅 1회 페치 후 인메모리 캐시 (`GetString/GetBool/GetInt/GetFloat`) | 서버 주도 설정값 조회 시 여기. `BootstrapFlow`가 부팅 시 `FetchAsync` 1회 호출 |
| `GameManager` | 게임 전체 부트스트랩 컨텍스트 | 매니저 등록 위치 |

### Config/ 카탈로그 — 환경설정값은 반드시 여기

> **원칙**: 빌드 환경별로 달라지는 설정값(URL, SDK 키, 피처 플래그 등)은 모두 `Config/` ScriptableObject에 넣는다. `Define.cs`·`ApiConfig.cs`·코드 리터럴에 직접 박지 말 것.

| 파일 | 내용 | 추가 시 |
|---|---|---|
| `NetworkConfig.cs` | **환경별 API 서버 URL** — Dev/Staging/Prod 3개 필드, `BaseUrl` 게터(`#if UNITY_EDITOR\|DEVELOPMENT_BUILD` → Dev, `#elif STAGING` → Staging, `else` → Prod). `ApiClient`가 `DataManager.Instance.NetworkConfig.BaseUrl`로 조회 | 새 서버 URL·엔드포인트 호스트 변경 시 여기 필드 추가 |
| `AdsConfig.cs` | LevelPlay AppKey, 광고 UnitId (Android/iOS 플랫폼 분리) | 광고 SDK 키 변경 시 |
| `AuthConfig.cs` | 구글 로그인 OAuth2 Web Client ID (`webClientId`, 배포 전 교체 필요) | 인증 SDK 키 변경 시 |
| `GameConfig.cs` | 게임 수치 설정 (스토어 URL, 복귀 임계초, 초기 골드/레벨) | 게임 밸런스 수치 |
| `IAPConfig.cs` | IAP 상품 ID | 상품 추가 시 |
| `LocalizationConfig.cs` | 언어별 폰트·로케일 매핑 | 언어 추가 시 |

**에셋 저장 경로**: `Assets/Resources/PreLoad/Config/{ConfigName}.asset` — `ResourceManager.LoadAll("PreLoad")`가 자동 캐시, `DataManager.LoadData()`에서 `LoadScriptableObject<T>()` 로 접근.

**ApiConfig.cs와의 역할 분리**:
- `ApiConfig.cs` → 엔드포인트 경로 상수만 (`/api/auth/guest` 등). URL 없음.
- `NetworkConfig` → 서버 호스트 URL만. 경로 없음.

### Utils/ 카탈로그 — 헬퍼 추가 전에 이걸 먼저 봐라

| 파일 | 내용 | 추가 시 |
|---|---|---|
| `Define.cs` | 모든 전역 enum (`EScene`, `EEventType`, `ESound`, `ELanguage`, `EAnimation`, `ECatState`, `EBusyScope`) + `PlayerPrefsKey` 런타임 키 상수 | 새 enum / 런타임 PlayerPrefs 키는 여기. **환경설정값(URL·SDK 키)은 여기 두지 말 것 — `NetworkConfig` 등 Config SO가 정식** |
| `Extension.cs` | `GameObject.GetOrAddComponent<T>()`, `Transform.DestroyChildren()`, `TMP_Text.SetLocalizedText(templateID)` | 새 확장 메서드는 여기. `static class Extension`에 메서드만 추가 |
| `Utils.cs` | 정적 헬퍼: `GetOrAddComponent`, `FindChildGameObject`, `FindChildComponent`, `GetRootTransform` | 새 정적 헬퍼는 여기 |
| `PriorityQueue.cs` | 우선순위 큐 자료구조 | 자료구조 신설 시 여기 |

### UI/ 헬퍼 — UI 작성 전에 확인

| 파일 | 책임 | 호출 방법 |
|---|---|---|
| `UI_Base` | 모든 UI의 베이스. 언어 변경 이벤트 자동 구독/해제, `_inflight` HashSet, **`RunWithBusyAsync(action, scope, gateButton, busyLabel)`**, **`GuardReentry(action)`** | 비동기 버튼 핸들러는 항상 `RunWithBusyAsync` 경유 (Layer A 글로벌 마스크 / Layer B 버튼 비활성 / Layer C 재진입 가드) |
| `UI_UGUI` | UGUI 베이스 (Bind, GetButton, GetText, GetImage, GetObject 인덱싱) | UGUI 화면은 모두 `UI_UGUI` 상속 |
| `UI_Toolkit` | UI Toolkit 베이스 | UI Toolkit 화면 |
| `PopupService` (static) | 팝업 표시 한 곳: `ShowError`, `ShowAnnouncement`, `ShowToast`, `ShowReward`, `ShowSelect`, `ShowMaintenance`, `ShowTerms` | **`UIManager.ShowPopupUI<T>` 직접 호출 금지** — 항상 PopupService 경유 |
| `UI_ConfirmPopup` | 확인 팝업 단일 프리팹 — Error/Notice/Reward/Select/Terms 모두 이걸 재사용 | PopupService 헬퍼가 알아서 사용 |
| `UI_Toast` | 토스트 알림 | `PopupService.ShowToast(message)` |
| `UI_BusyMask` | 전화면 입력 차단 마스크 | `UIManager.BeginBusy(label)` 또는 `UI_Base.RunWithBusyAsync(scope:Global)`이 자동 사용 |
| `SafeAreaFitter` | Safe Area 자동 적용 컴포넌트 | 새 씬/팝업 프리팹 루트에 부착 |

### Scenes/

- `BaseScene` 상속. 새 씬은 `BaseScene` + `Define.EScene` enum 추가.
- `OnBackButton()` 가상 메서드 오버라이드로 뒤로가기 동작 커스터마이즈.

---

## 코딩 컨벤션

### 매직 스트링 — 클래스 내부 `private const string`

프리팹 키, 리소스 키, 매직 스트링은 **사용처 클래스 내부 `private const string`** (UPPER_SNAKE_CASE) 로 둔다.

```csharp
// ShoutManager.cs — 정식 표본
private const string HUD_PREFAB_NAME = "UI_HUDShout";

// UIManager.cs — 같은 패턴
private const string BUSY_MASK_PREFAB_NAME = "UI_BusyMask";

// 사용
GameObject go = ResourceManager.Instance.Instantiate(HUD_PREFAB_NAME);
```

**전역 상수 클래스 만들지 말 것** (`PrefabKey` 같은 것). 이 프로젝트에 그런 패턴 없음.

### 한국어 주석 의무 (CLAUDE 부모 룰)

모든 코드에 한국어 주석. 영문 주석은 외부 라이브러리/API 명만 허용. 한국어 주석 없는 코드는 미완성으로 간주.

### Singleton vs static class

- 상태·라이프사이클(코루틴, PlayerPrefs 영속화, 외부 SDK 핸들)이 있으면 `Singleton<T>` 상속
- 무상태 헬퍼는 `static class` (`PopupService`, `XxxApi` 전체)
- MonoBehaviour Singleton 빈 GameObject 양산 안티패턴 회피

### 폴더 원칙

- 매니저는 `@Scripts/Managers/` 만
- UI 컴포넌트는 `@Scripts/UI/` 만
- 백엔드 호출은 `@Scripts/WebFramework/` 만 (외부 폴더에서 `ApiClient` 직접 호출 금지)
- 새 폴더 만들지 말 것 — 기존 분류로 다 들어간다

---

## WebFramework 폴더 구조 및 원칙

```
@Scripts/WebFramework/
├── Core/    UnityWebRequest 래퍼 (ApiClient), ApiResult<T>, ApiError, JsonHelper, RestLogger, ServerTime
├── Api/     엔드포인트별 정적 서비스 (AuthApi, ItemApi, ShopApi, ... 14개) — 모두 static class
├── Models/  DTO — 요청/응답 데이터 모델
└── Auth/    토큰·세션 관리 (AuthManager, GoogleSignInProvider)
```

### 재사용 원칙 (WebFramework 한정 추가 규칙)

- 새 API 추가 시: `Api/XxxApi.cs` (static class) + `Models/XxxModels.cs`. `ApiClient.Instance.GetAsync<TRes>` 류 호출, `Task<ApiResult<TRes>>` 반환
- 새 매니저 필요 시 `Singleton<T>` 상속 (`AuthManager`가 표본)
- 401/429/503 인터셉터는 `ApiClient` 내부 자동 처리 — 호출부에서 별도 처리 금지
- 토큰 상태는 `AuthManager.Instance.AccessToken` 단일 경로
- `Api/*` 정적 서비스와 `Core/*` 정적 유틸(`ServerTime`, `JsonHelper` 등)은 어느 레이어에서든 직접 호출 가능. 단, `AuthManager` 등 도메인 상태를 가진 구체 매니저는 직접 참조하지 말고 `EventManager` 경유

> 개발 가이드:
> - Api 호출 패턴 / 신규 도메인 추가 절차 → `DEVELOPER_GUIDE.md`
> - 패턴 채택 근거 박제 → `DEVNOTES.md` `[설계 결정]` 섹션
> - 백엔드 엔드포인트 명세 → `../CLIENT_GUIDE.md`

---

## 씬 구성

- `BootstrapScene` — 앱 부팅 (버전체크/자동로그인/약관)
- `LoginScene` — 인증 API 테스트 및 로그인 흐름
- `MainScene` — 메인 게임 진입
- `StageSelectScene` — 스테이지 목록
- `GameScene` — 게임플레이 관련 API 테스트
- `LoadingScene` — 씬 전환 중 페이드 전용

---

# Unity Agents
You are UnityAgent, an AI assistant that controls the Unity Editor.

You are an interactive agent that helps users with Unity editor automation,
scene editing, GameObject and component management, prefab workflows,
asset management, and script generation or modification tasks.
Use the instructions below and the tools available to you to assist the user.

IMPORTANT: You must NEVER guess object names, asset paths, scene paths,
component values, or script contents. Always query the current state first.
IMPORTANT: Before deleting assets, removing objects, overwriting scripts,
or making bulk destructive changes (100+ objects, project settings,
scenes, or prefabs), always confirm with the user first.
IMPORTANT: When modifying scripts or other assets, consider Unity's
compilation and refresh flow, and verify the result after changes.
