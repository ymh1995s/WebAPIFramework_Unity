# GameClient Developer Guide

> 이 문서는 **이 Unity 프로젝트 내부에서 코드를 추가/수정할 때 따라야 할 컨벤션**을 담는다.
> 백엔드 엔드포인트 명세(요청 형식·응답 형식·에러 처리)는 `../CLIENT_GUIDE.md` 참조.

---

## Api 호출 패턴 (async/await + `ApiResult<T>`)

`WebFramework/Api/` 하위 클래스는 모두 `static class`이며, 메서드는 `Task<ApiResult<TRes>>`를 반환한다. 콜백(`Action<T> onSuccess, Action<ApiError> onError`) 시그니처는 사용하지 않는다.

### 표준 호출 패턴 — early return 평탄화

```csharp
// 진입점 — UI 이벤트 핸들러
private async void OnClickStart()
{
    var result = await StageApi.GetMastersAsync();
    if (!result.IsSuccess)
    {
        PopupService.ShowError(result.Error);
        return;
    }
    _stages = result.Value;
    RefreshDisplay();
}
```

- `await` 한 번 + `if (!result.IsSuccess)` 단일 분기 + early return
- 에러 처리 본문은 `PopupService.ShowError(result.Error)` 표준
- UI 이벤트 핸들러/`Start`/`OnEnable` 등 진입점만 `async void`. 내부 헬퍼는 `async Task`
- 401 자동 토큰 갱신, 503 점검 인터셉터, 429 백오프는 `ApiClient` 내부가 자동 처리 — 호출부에서 별도 처리 금지

---

## 환경별 서버 설정 (NetworkConfig)

환경에 따라 달라지는 값(서버 URL 등)은 `Config/NetworkConfig.cs` ScriptableObject에서 관리한다.

| 심볼 / 빌드 조건 | 사용 URL 필드 |
|---|---|
| `UNITY_EDITOR` 또는 `DEVELOPMENT_BUILD` | `DevBaseUrl` |
| `STAGING` Define Symbol | `StagingBaseUrl` |
| 릴리즈 빌드 | `ProductionBaseUrl` |

**환경 전환 방법**: `Build Settings > Player Settings > Scripting Define Symbols`에 `STAGING` 추가/제거.

**접근 방법**: `DataManager.Instance.NetworkConfig.BaseUrl` — `ApiClient` 내부에서 자동 참조. 호출부에서 URL을 직접 다루지 말 것.

**에셋 최초 생성 (클론 후 1회만)**:
1. Unity Editor Project 창에서 `Assets/Resources/PreLoad/Config/` 폴더 우클릭
2. `Create > Config > NetworkConfig` 선택 → `NetworkConfig.asset` 파일 생성됨
3. 생성된 파일 선택 → Inspector에서 필드 입력:
   - **Dev Base Url**: `http://localhost:5058` (로컬 개발 서버)
   - **Staging Base Url**: 스테이징 서버 URL (추후)
   - **Production Base Url**: 프로덕션 서버 URL (추후)
4. `NetworkConfig.asset`은 git에 커밋해도 됨 (URL은 비밀값 아님)

> **[CreateAssetMenu]란?** `NetworkConfig.cs`에 붙은 어트리뷰트로, Unity Editor 우클릭 메뉴에 항목을 등록해 주는 것. ScriptableObject의 인스턴스(`.asset` 파일)를 Editor UI에서 클릭 한 번으로 만들게 해주는 편의 기능.

> **원칙 요약**: 서버 URL은 `NetworkConfig`에, 엔드포인트 경로(`/api/xxx`)는 `ApiConfig.cs`에, 런타임 키는 `Define.cs/PlayerPrefsKey`에.

---

## 인증 설정 (AuthConfig)

Google Sign-In WebClientId 등 인증 관련 환경값은 `Config/AuthConfig.cs` ScriptableObject에서 관리한다.

**접근 방법**: `DataManager.Instance.AuthConfig.WebClientId` — `GoogleSignInProvider.Configure()` 내부에서 자동 참조. 호출부에서 직접 문자열을 다루지 말 것.

**에셋 최초 생성 (클론 후 1회만)**:
1. Unity Editor Project 창에서 `Assets/Resources/PreLoad/Config/` 폴더 우클릭
2. `Create > Config > AuthConfig` 선택 → `AuthConfig.asset` 파일 생성됨
3. 생성된 파일 선택 → Inspector에서 필드 입력:
   - **Web Client Id**: `1080787057808-g3elodns8j77qk67hoh67hm8nbess0r9.apps.googleusercontent.com`
     (Google Cloud Console → 사용자 인증 정보 → OAuth 2.0 클라이언트 ID에서 확인)
4. `AuthConfig.asset`은 git에 커밋해도 됨 (OAuth2 Web Client ID는 공개값)

> **[CreateAssetMenu]란?** `AuthConfig.cs`에 붙은 어트리뷰트로, Unity Editor 우클릭 메뉴에 항목을 등록해 주는 것. 위 2번 단계에서 `Create > Config > AuthConfig` 메뉴가 나타나는 이유.

---

## 신규 도메인 추가 절차 (예: `Friend`)

작업 위치 3 + 옵션 1.

### 1. `WebFramework/Core/ApiConfig.cs` — 엔드포인트 경로 상수 블록 (경로만, URL 없음)

```csharp
public static class Friend
{
    public const string List = "/api/friends";
    public const string Add  = "/api/friends/{0}/add"; // {0} = friendId
}
```

### 2. `WebFramework/Models/FriendModels.cs` — 요청/응답 DTO

`[Serializable]` + camelCase 필드 (ASP.NET Core 기본 직렬화 정책 대응).

```csharp
[Serializable] public class FriendDto
{
    public int    id;
    public string nickname;
}
```

### 3. `WebFramework/Api/FriendApi.cs` — `static class` + 정적 Task 반환 메서드

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;

// 친구 도메인 API — 목록 조회 / 친구 추가
public static class FriendApi
{
    // 친구 목록 조회 — GET /api/friends
    public static Task<ApiResult<List<FriendDto>>> GetListAsync()
        => ApiClient.Instance.GetListAsync<FriendDto>(ApiConfig.Friend.List);

    // 친구 추가 — POST /api/friends/{friendId}/add (본문 없음)
    public static Task<ApiResult<EmptyResponse>> AddAsync(int friendId)
        => ApiClient.Instance.PostAsync<EmptyResponse>(
            string.Format(ApiConfig.Friend.Add, friendId));
}
```

### 4. (옵션) 호출지점 — UI/Manager에서 사용

```csharp
var result = await FriendApi.GetListAsync();
if (!result.IsSuccess) { PopupService.ShowError(result.Error); return; }
_friends = result.Value;
```

### 커밋 시 함께 포함할 파일

- `.cs.meta` 파일 (Unity Editor가 자동 생성)
- `UnityClient/Assembly-CSharp.csproj` (Unity Editor가 자동 갱신)

---

## ApiClient 메서드 매핑

| HTTP 메서드 | 본문 유무 | `ApiClient` 메서드 |
|---|---|---|
| GET | — | `GetAsync<TRes>(endpoint)` |
| GET (쿼리) | — | `GetWithQueryAsync<TRes>(endpoint, Dictionary<string,string>)` |
| GET (최상위 배열) | — | `GetListAsync<T>(endpoint)` |
| GET (`Dictionary` 등 복합 타입) | — | `GetAsyncNewtonsoft<TRes>(endpoint)` — Newtonsoft.Json 파서 사용 |
| POST | 있음 | `PostAsync<TReq, TRes>(endpoint, body)` |
| POST | 없음 | `PostAsync<TRes>(endpoint)` |
| PUT | 있음 | `PutAsync<TReq, TRes>(endpoint, body)` |
| DELETE | — | `DeleteAsync<TRes>(endpoint)` |

- 응답 본문 없음(204 또는 빈 200): `TRes = EmptyResponse` 사용
- URL 경로 치환: `string.Format(ApiConfig.X.Y, id)` 패턴
- 멱등성 키 필요한 POST(예: `ItemApi.UseAsync`): `clientRequestId = Guid.NewGuid().ToString()` 호출 시점에 생성

---

## 씬 구성 및 전환

### 씬 목록 (Build Index 순)

| # | 씬 | 역할 |
|---|---|---|
| 0 | `BootstrapScene` | 앱 진입점. `ResourceManager.LoadAll` 완료 후 `BootstrapFlow.Run()`에 흐름 위임 |
| 1 | `LoadingScene` | **모든 일반 씬 전환의 중간 단계.** 진행률 표시 + 대상 씬 비동기 로드 |
| 2 | `LoginScene` | 게스트/구글 로그인 UI |
| 3 | `MainScene` | 로그인 후 홈/로비 |
| 4 | `StageSelectScene` | 스테이지 선택 |
| 5 | `GameScene` | 인게임 |

### 부팅 흐름 — `BootstrapFlow.Run()`

`BootstrapScene.Start()` → `ResourceManager.LoadAll` 콜백에서 호출. (`UnityClient/Assets/@Scripts/Bootstrap/BootstrapFlow.cs`)

```
[1] VersionApi.CheckAsync            503 점검 → 자동 재시도 폴링 / isForceUpdate → 스토어 이동 + Quit
[2] RemoteConfigManager.FetchAsync   원격 설정 캐시 (실패 무시 — defaultValue 폴백)
[3] NoticeApi.GetLatestAsync         신규 공지 팝업 (실패 무시)
[4] AuthApi.RefreshAsync             저장된 RefreshToken으로 자동 로그인
        ├ 성공             → MainScene
        ├ AUTH_BANNED      → 밴 팝업 후 재시작
        └ 실패/토큰 없음    → 다음 단계
[5] 약관 동의 확인                    → LoginScene
```

### 일반 씬 전환 — `SceneManager.Instance.LoadScene()`

**모든 UI/Manager의 씬 전환은 이 메서드만 사용한다.** 페이드 + LoadingScene 경유 흐름이 표준이며, 즉시 전환은 금지(아래 참고).

```csharp
// UI 버튼 클릭 핸들러 등에서 호출 — 한 줄이면 끝
SceneManager.Instance.LoadScene(Define.EScene.GameScene);
```

내부 시퀀스 (`SceneManager.cs:36-53` + `LoadingScene.cs:19-64`):

```
1. FadeOut 0.15s                                    검정 화면
2. LoadingScene 진입 (PendingScene = 대상)          정적 변수로 대상 전달
3. FadeIn 0.15s                                     로딩 UI 노출
4. LoadSceneAsync(대상, allowActivation=false)      진행률 0~90% (Unity 제약)
5. WaitForSeconds(0.5s)                             최소 표시 시간 (깜빡임 방지)
6. FadeOut → ScheduleFadeIn 예약                    DDOL FadeManager가 다음 씬에서 실행
7. allowSceneActivation = true                      대상 씬 활성화 / LoadingScene 파괴
```

### `LoadSceneImmediate()` — 사용 금지

`SceneManager` 내부에서 LoadingScene 자체로 진입할 때만 쓴다. UI/Manager 코드에서 직접 호출하면 페이드/로딩 화면이 생략되어 화면이 거칠어진다.

### BaseScene 상속 패턴

신규 씬 스크립트는 항상 `BaseScene`을 상속하고 `Awake`에서 `SceneType`을 설정한다 — `SceneManager.CurrentSceneType`이 이 값을 참조한다.

```csharp
// 스테이지 선택 씬 — 메인에서 진입, 스테이지 선택 시 GameScene으로 전환
public class StageSelectScene : BaseScene
{
    // 씬 활성화 직후 호출 — 씬 타입 등록 필수
    protected override void Awake()
    {
        base.Awake();
        SceneType = Define.EScene.StageSelectScene;
    }

    // Android 백 버튼 처리 — 메인으로 복귀
    private void OnBack()
    {
        SceneManager.Instance.LoadScene(Define.EScene.MainScene);
    }
}
```

### 호출 매트릭스 — 어디서 어디로 가는가

`SceneManager.Instance.LoadScene(...)` 호출처 정리. 모두 LoadingScene을 경유한다.

| From | To | 트리거 / 호출 위치 |
|---|---|---|
| `BootstrapScene` | `MainScene` | 자동 로그인 성공 — `BootstrapFlow.cs:214` |
| `BootstrapScene` | `LoginScene` | 약관 동의 후 / 자동 로그인 실패 — `BootstrapFlow.cs:227,234` |
| 어디서나 | `LoginScene` | 세션 만료 감지 — `AppLifecycleManager.cs:83` |
| 어디서나 | `BootstrapScene` | 재시작 트리거 (밴 OK 등) — `PopupService.cs:270` |
| `LoginScene` | `MainScene` | 로그인 성공 — `UI_LoginScene.cs:111` |
| `MainScene` | `LoginScene` / `StageSelectScene` | 로그아웃·탈퇴 / 스테이지 진입 — `UI_MainGame.cs:240,262,275` |
| `StageSelectScene` | `GameScene` / `MainScene` | 스테이지 선택 / 뒤로가기 — `UI_StageSelect.cs:76,82` |
| `GameScene` | `StageSelectScene` | 인게임 종료 — `GameScene.cs:17`, `UI_InGame.cs:55,61` |

### 신규 씬 추가 체크리스트

1. `UnityClient/Assets/@Scenes/{NewScene}.unity` 생성 (Camera + Directional Light 포함)
2. `UnityClient/Assets/@Scripts/Scenes/{NewScene}.cs` — `BaseScene` 상속, `Awake`에서 `SceneType` 설정
3. `Utils/Define.cs`의 `EScene` enum에 값 추가
4. `ProjectSettings/EditorBuildSettings.asset`의 Build Settings에 등록 (`enabled: 1`)
5. 진입/전환 트리거 코드에서 `SceneManager.Instance.LoadScene(Define.EScene.{NewScene})` 호출
6. 씬 내 UI 스크립트는 `UI_Base`/`UI_UGUI` 상속, UIManager 경유로 표시

---

## 관련 문서

| 문서 | 내용 |
|---|---|
| `../CLIENT_GUIDE.md` | 백엔드 엔드포인트 명세 — 요청/응답/에러 처리 (백엔드 팀 관리) |
| `DEVNOTES.md` `[설계 결정]` | 본 패턴의 채택 근거 박제 (2026-05-12) |
| `CLAUDE.md` | AI 에이전트 행동 규칙 + 폴더 구조 |
