# 07. WebFramework 확장 계획

## 1. 현재 상태

```
WebFramework/
├── Core/    ApiClient (POST만), ApiConfig (Auth만, /api prefix 누락 버그), RestLogger
├── Auth/    AuthManager, GoogleSignInProvider
├── Api/     AuthApi (Guest/Google/Refresh)
└── Models/  AuthModels (Guest/Google/Refresh/Token)
```

## 2. 1차 설계 대비 핵심 변경

| 영역 | 1차 | 본 라운드 |
|---|---|---|
| ApiConfig.Auth 경로 | `/auth/...` (버그) | **`/api/auth/...`** 정정 |
| 9개 별도 Api 클래스 | (Version/Maintenance/Mail/Inventory/Ranking/Notice/Inquiry/Stage/Account) | **8개로 축소** (Maintenance·Inventory 폐기) |
| ApiClient 메서드 시그니처 | `Action<TRes>, Action<string>` | **`Action<TRes>, Action<ApiError>`** |
| 401 자동 회전 | "별도 라운드" 미루기 | **본 라운드 포함** (이유 §6) |
| 503 점검 인터셉터 | 문서 흩어짐 | **ApiClient 내부에 박제** |
| `Account.LinkGoogle` URL | 추정 | 실측 `/api/auth/link/google` |
| TokenResponse `isGoogleLinked` 필드 | 협의 가정 | **응답에 없음**. AuthManager 클라 캐시 |

## 3. 백엔드 부재 항목 결정 (사용자 결정 권한 위임)

### 3.1 인벤토리 조회

**현황**: `Framework.Api/Controllers/Player/`에 `InventoryController`/`PlayerItemsController` **부재**. Admin (`AdminPlayerItemsController`)만 존재. 도메인 엔티티 `PlayerItem` 은 `{Id, PlayerId, ItemId, Quantity}` 구조 (만료 개념 없음).

**선택지**

| 옵션 | 평가 |
|---|---|
| A. 백엔드 신설 (`GET /api/inventory`) | 깔끔. Player 본인 인벤토리 조회 1개. 기존 `PlayerItemRepository` 재사용. **권장 1순위**. |
| B. 클라 로컬 누적 (메일 수령 응답으로 갱신) | 신뢰성 매우 낮음. 앱 재설치/캐시 손실 시 0. |
| C. 본 라운드는 미지원 안내만 (`UI_AnnouncementPopup`) | 단순함 우선 원칙에 부합. 백엔드 신설 비용을 별도 라운드로 미룸. |

**권고**: **C로 본 라운드 진행 + A를 사용자에게 별도 백엔드 라운드로 제안**.

근거:
- 더미 검증 단계에서 인벤토리 표시는 핵심 가치가 낮다(요구사항.md도 `UI_ConfirmPopup`에 텍스트만 출력).
- 백엔드 신설 분량이 작음(1 endpoint, 1 service, 1 controller, 1 dto) → 제안만 하면 빠름.
- B는 명백히 위험하므로 배제.

### 3.2 공지 리스트

**현황**: `NoticesController`에 `GetLatest` 1개. `GET /api/notices` (전체 리스트) 부재.

**선택지**

| 옵션 | 평가 |
|---|---|
| A. 백엔드 신설 (`GET /api/notices?limit=N`) | 페이징/정렬 등 정의 필요. 비용 중간. |
| B. 단건만 표시 (현 백엔드 그대로) | 요구사항 "공지사항 리스트" 와 일부 충돌하지만 표시 1건 = "리스트 길이 1"로 해석 가능. |
| C. 부트스트랩의 `lastSeenNoticeId` 사용 안 한 1건만 | 메인 화면 버튼은 항상 최신 1건만 보여줌 |

**권고**: **B 채택**. 단건 표시.

근거:
- 단순함 우선. 더미 단계에서 공지 페이징은 과한 기능.
- 운영팀이 공지를 활성화하면 항상 가장 최신 1건이 노출되므로 운영상 가치 충분.
- 클라가 `UI_ConfirmPopup`에 1건 표시는 자연스럽고 요구사항 위배 정도가 미미.

### 3.3 계정 상태 (`isGoogleLinked` 등)

**현황**: 백엔드 `TokenResponse` = `{accessToken, refreshToken, playerId, isNewPlayer}`. 연동 상태 필드 없음. Player 프로필 조회 endpoint 부재.

**선택지**

| 옵션 | 평가 |
|---|---|
| A. 백엔드 신설 (`GET /api/auth/me`) | 닉네임/level/연동 여부 등 종합 응답. 비용 중간. **권장 2순위**. |
| B. 클라 PlayerPrefs 캐시 (각 인증 흐름에서 갱신) | 다기기 시나리오에서 캐시 불일치. 더미 단계 수용 가능. |
| C. TokenResponse에 필드 추가 협의 | 백엔드 수정 + 응답 모델 변경 → 영향 범위 큼 |

**권고**: **B 채택 + A를 별도 백엔드 라운드로 제안**.

근거:
- 단일 기기 가정의 더미 검증이라 캐시 불일치 위험 낮음.
- 요구사항에서 닉네임이 필요한 곳은 문의 메시지(`{닉네임}의 문의입니다`) 1곳뿐. 임시로 `Player {playerId}`로 대체.
- A를 도입하면 닉네임 외에 향후 레벨/캐릭터 종합 표시도 자연스러워짐 → 신규 게임 프로젝트 시작점으로 가치 높음.

### 3.4 결정 요약 (신설 권고 백엔드)

본 라운드는 클라 우회로 진행하되, 사용자가 별도 백엔드 라운드를 원하면 다음 2개 endpoint를 제안:

1. `GET /api/inventory` — Player 본인 보유 아이템 조회 (`List<{itemId, itemName, quantity}>`)
2. `GET /api/auth/me` — Player 본인 프로필 조회 (`{playerId, nickname, level, isGoogleLinked, ...}`)

분량: 컨트롤러 2 + 서비스 메서드 2 + DTO 2. 작은 라운드.

## 4. Core 확장

### 4.1 `ApiConfig` 정정 + 엔드포인트 추가

```csharp
public static class ApiConfig
{
    public const string BaseUrl = "http://192.168.219.101:5058";

    public static class Auth
    {
        public const string Guest         = "/api/auth/guest";
        public const string Google        = "/api/auth/google";
        public const string Refresh       = "/api/auth/refresh";
        public const string Logout        = "/api/auth/logout";
        public const string LinkGoogle    = "/api/auth/link/google";
        public const string ResolveGoogle = "/api/auth/google/resolve-conflict";
        public const string Withdraw      = "/api/auth/withdraw";
    }

    public static class Version
    {
        public const string Check = "/api/version/check"; // ?version={ver}
    }

    public static class Notice
    {
        public const string Latest = "/api/notices/latest";
    }

    public static class Mail
    {
        public const string List  = "/api/mails";
        public const string Claim = "/api/mails/{0}/claim";
    }

    public static class Stage
    {
        public const string List     = "/api/stages";
        public const string Progress = "/api/stages/progress";
        public const string Complete = "/api/stages/{0}/complete";
    }

    public static class Ranking
    {
        public const string Me = "/api/ranking/me";
    }

    public static class Inquiry
    {
        public const string Submit = "/api/inquiries";
        public const string List   = "/api/inquiries";
    }

    public static class DailyLogin
    {
        public const string Process = "/api/dailylogin";
    }

    public static class Shout
    {
        public const string Active = "/api/shouts/active";
    }
}
```

> **폐기**: `Maintenance.Check`(별도 endpoint 없음), `Inventory.List`(부재 결정).

### 4.2 `ApiClient` — GET / POST / PUT / DELETE 일반화 + 인터셉터

#### 시그니처

```csharp
public async void Get<TRes>(string endpoint,
    Action<TRes> onSuccess, Action<ApiError> onError = null);

public async void GetWithQuery<TRes>(string endpoint, IDictionary<string,string> query,
    Action<TRes> onSuccess, Action<ApiError> onError = null);

public async void Post<TReq, TRes>(string endpoint, TReq body,
    Action<TRes> onSuccess, Action<ApiError> onError = null);

public async void Post<TRes>(string endpoint,
    Action<TRes> onSuccess, Action<ApiError> onError = null);  // 본문 없음

public async void Put<TReq, TRes>(string endpoint, TReq body,
    Action<TRes> onSuccess, Action<ApiError> onError = null);

public async void Delete<TRes>(string endpoint,
    Action<TRes> onSuccess, Action<ApiError> onError = null);
```

> **본문 없는 POST(`POST /api/dailylogin`)** 와 **응답 본문 없는 호출(`logout`/`withdraw` 204, `claim` 200 빈 본문)** 둘 다 발생. `EmptyResponse` DTO + 빈 본문 처리 분기.

#### 공통 동작

1. URL 조립: `BaseUrl + endpoint` (또는 query string 부착)
2. JWT 자동 첨부: `AuthManager.AccessToken` 비어있지 않으면 `Authorization: Bearer ...`
3. `Content-Type: application/json` (본문 있는 요청만)
4. 타임아웃 60초 (`req.timeout = 60`) — CLIENT_GUIDE 27번 권장
5. **응답 처리**:
   - 200 + 본문 비어있음 → `EmptyResponse` 반환
   - 200~299 → `JsonUtility.FromJson<TRes>` 후 `onSuccess`
   - 401 → §5 자동 회전 시도 → 성공 시 1회 재시도, 실패 시 `onError`
   - 503 + 점검 패턴 → §6 인터셉터 → `onError(ApiError.Maintenance)` 동시에 EventManager 발행
   - 429 → §7 백오프 1회 자동 재시도, 그래도 실패 시 `onError`
   - 그 외 4xx/5xx → ProblemDetails 파싱 시도 → `ApiError` 객체 → `onError`

#### `ApiError` 데이터 구조

```csharp
[Serializable]
public class ApiError
{
    public long   Status;        // HTTP status
    public string ErrorCode;     // ProblemDetails errorCode (없으면 null)
    public string Title;
    public string Detail;
    public string TraceId;
    public string RawBody;
    public bool   IsNetworkError;

    // 사용자 표시용 메시지 — UI 계층에서 사용
    public string UserMessage =>
        !string.IsNullOrEmpty(Detail)    ? Detail :
        !string.IsNullOrEmpty(Title)     ? Title  :
        IsNetworkError                   ? "네트워크 오류가 발생했습니다." :
                                           $"요청 처리 중 오류 ({Status})";

    public static ApiError FromHttp(long code, string body, bool isNetwork) { ... }
    public static readonly ApiError Maintenance = new ApiError { Status = 503, Title = "서버 점검 중" };
}
```

`Models/CommonModels.cs` 또는 `Core/ApiError.cs`에 배치.

## 5. 401 자동 회전 (CLIENT_GUIDE 8번/28번)

### 도입 결정

**본 라운드 포함**. 1차 설계에서 "별도 라운드"로 미뤘으나 다음 이유로 본 라운드에 박제:

1. AccessToken 만료(약 1시간)는 게임 세션 중에 **반드시 발생**. 사용자가 메인에서 1시간 머문 후 메일 박스 진입 → 401 → 호출처마다 RefreshToken 재시도를 손으로 짜면 보일러플레이트가 8개 Api 클래스 전부에 퍼짐. 회전을 ApiClient에 두면 호출처가 단순.
2. 부트스트랩에서 `AuthApi.Refresh`를 직접 호출하는 자동 로그인 흐름과 구별: **자동 로그인은 명시적 호출**, **회전은 인터셉터**. 두 흐름이 같은 `Refresh` 메서드를 공유.
3. CLIENT_GUIDE 부록 A 의무 동작 체크리스트에 "JWT 만료(401) 인터셉터 (전역)" 가 명시.

### 흐름

```
[원 요청] → 401 응답
   ↓
ApiClient가 RefreshLock(mutex) 획득
   ↓
AuthApi.Refresh(currentRefreshToken)
   ├─ 200 → AuthManager.SaveToken(new) → RefreshLock 해제
   │       → 원 요청 재구성 (새 AccessToken으로 헤더 교체)
   │       → 1회 재시도 (이때 다시 401이면 즉시 onError, 무한 루프 차단)
   │
   └─ 401/4xx → AuthManager.Clear() → EventManager.SessionExpired 이벤트
              → SceneManager.LoadScene(LoginScene) (또는 BootstrapScene 권장)
              → 원 요청은 onError(ApiError.SessionExpired)
```

### 동시성

여러 호출이 거의 동시에 401을 받을 때 refresh를 중복 호출하지 않도록 **`SemaphoreSlim`(1, 1)**로 직렬화.

```csharp
static readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

async Task<bool> TryRefreshOnce()
{
    await _refreshLock.WaitAsync();
    try
    {
        if (_refreshTriedRecently()) return _lastRefreshSucceeded; // 캐시
        return await DoRefresh();
    }
    finally { _refreshLock.Release(); }
}
```

> Unity 메인스레드 한정에서 `SemaphoreSlim`은 `await` 가능. UnityWebRequest의 awaitable 패턴과 호환.

### 회전 자체가 401인 경우

`AuthApi.Refresh`는 ApiClient를 우회 (회전 인터셉터를 또 타지 않도록). `AuthApi.Refresh`만 ApiClient의 raw 메서드(`PostNoIntercept`) 사용. 또는 `AuthApi.Refresh` 호출 시 `_inRefreshFlow=true` 플래그로 가드.

## 6. 503 점검 인터셉터 (CLIENT_GUIDE 1번/26번)

### ApiClient 내부 처리

```csharp
if (req.responseCode == 503 && IsMaintenanceBody(req.downloadHandler.text))
{
    EventManager.Instance.PublishMaintenance(req.downloadHandler.text);
    onError?.Invoke(ApiError.Maintenance);
    return;
}
```

`IsMaintenanceBody` = JSON 파싱 후 `message` 필드에 "점검" 단어 포함 여부.

### EventManager 확장

`Define.EEventType.MaintenanceDetected` 추가 + EventManager 메서드 1개:
```csharp
public void PublishMaintenance(string rawBody);
```

부트스트랩/메인 등 PopupService를 사용 가능한 시점에 1회 구독:
```csharp
EventManager.Instance.Subscribe(Define.EEventType.MaintenanceDetected,
    rawBody => PopupService.ShowMaintenance("서버 점검 중입니다.", QuitApplication));
```

`PopupService._maintenanceShown` 플래그가 중복 표시 차단.

## 7. 429 Rate Limit (CLIENT_GUIDE 25번)

`Retry-After` 헤더 값(초) 만큼 대기 후 1회 재시도. 헤더 없으면 5초.
2회 이상 누적되면 `onError`로 노출.

```csharp
if (req.responseCode == 429)
{
    int wait = ParseRetryAfter(req) ?? 5;
    await Awaitable.WaitForSecondsAsync(wait);
    return await SendOnce(...);  // 1회 재시도
}
```

## 8. 8개 Api 클래스 신설

모두 `Singleton<T>` 패턴. 콜백 시그니처: `Action<TRes> onSuccess, Action<ApiError> onError = null`.

### 8.1 `AuthApi` (확장)

```csharp
// 기존
void GuestLogin(string deviceId, ...);
void GoogleLogin(string idToken, ...);
void Refresh(string refreshToken, ...);

// 신규
void Logout(string refreshToken, Action<EmptyResponse> onSuccess, Action<ApiError> onError = null);
void LinkGoogle(string idToken, Action<EmptyResponse> onSuccess, Action<ApiError> onError = null);
void ResolveGoogleConflict(string idToken, Action<TokenResponse> onSuccess, Action<ApiError> onError = null);
void Withdraw(Action<EmptyResponse> onSuccess, Action<ApiError> onError = null);
```

### 8.2 `VersionApi`

```csharp
void Check(string currentVersion, Action<VersionCheckResponse> onSuccess, Action<ApiError> onError = null);

[Serializable] public class VersionCheckResponse
{
    public bool   isForceUpdate;
    public string latestVersion;
}
```

### 8.3 `NoticeApi`

```csharp
void GetLatest(Action<NoticeDto> onSuccess, Action<ApiError> onError = null); // 204면 onSuccess(null)

[Serializable] public class NoticeDto
{
    public int    id;
    public string content;
}
```

### 8.4 `MailApi`

```csharp
void GetList(Action<List<MailDto>> onSuccess, Action<ApiError> onError = null);
void Claim(int mailId, Action<EmptyResponse> onSuccess, Action<ApiError> onError = null);

[Serializable] public class MailDto
{
    public int      id;
    public int      playerId;
    public string   title;
    public string   body;
    public int      itemId;
    public string   itemName;
    public int      itemCount;
    public bool     isRead;
    public bool     isClaimed;
    public string   createdAt; // ISO 8601 string
    public string   expiresAt;
    public int      exp;
    public List<MailItemDto> mailItems;
}
[Serializable] public class MailItemDto
{
    public int    itemId;
    public string itemName;
    public int    quantity;
}
```

> JsonUtility는 List<T> 최상위 응답을 직접 못 읽는다. `MailListEnvelope { mails }` 형태로 백엔드와 합의하거나, **클라가 `JsonHelper`(배열 wrapping)를 만들어 처리**한다. 본 라운드는 후자 채택 — `JsonHelper.FromJsonArray<T>(string json)` 유틸 1개를 `Core/`에 둔다.

### 8.5 `StageApi`

```csharp
void GetMasters(Action<List<StageMasterDto>> onSuccess, Action<ApiError> onError = null);
void GetProgress(Action<List<StageProgressDto>> onSuccess, Action<ApiError> onError = null);
void Complete(int stageId, StageClearRequestDto body, Action<StageClearResponseDto> onSuccess, Action<ApiError> onError = null);

[Serializable] public class StageMasterDto
{
    public int    id;
    public string code;
    public string name;
    public string rewardTableCode;
    public string rePlayRewardTableCode;
    public int    rePlayRewardDecayPercent;
    public int    expReward;
    public int?   requiredPrevStageId;  // JsonUtility의 nullable int 한계는 별도 검증 필요
    public bool   isActive;
    public int    sortOrder;
}
[Serializable] public class StageProgressDto
{
    public int    stageId;
    public string code;
    public string name;
    public bool   isCleared;
    public int    clearCount;
    public int    bestScore;
    public int    bestStars;
    public long   bestClearTimeMs;
    public bool   isLocked;
    public int    sortOrder;
}
[Serializable] public class StageClearRequestDto
{
    public int  score;
    public int  stars;
    public long clearTimeMs;
}
[Serializable] public class StageClearResponseDto
{
    public bool   isFirstClear;
    public int    clearCount;
    public int    expGranted;
    public string firstRewardMessage;
    public string replayRewardMessage;
}
```

> `int?`는 JsonUtility가 직접 지원 안 함 → `requiredPrevStageId`를 `int`(0/없음을 0으로 매핑)로 받거나, `Newtonsoft.Json` 도입 검토. **결정**: 1차 안정성 우선 → `int requiredPrevStageId; bool hasRequiredPrev;` 또는 그냥 `int`로 받고 0=없음으로 합의(백엔드 응답에서 null이면 JsonUtility가 0으로 채움). 잠금 판정은 progress.isLocked만 신뢰하므로 이 필드의 정확성은 표시용일 뿐.

### 8.6 `RankingApi`

```csharp
void GetMyRank(Action<MyRankResponse> onSuccess, Action<ApiError> onError = null);

[Serializable] public class MyRankResponse
{
    public int    rank;
    public int    playerId;
    public string nickname;
    public int    bestScore;
}
```

### 8.7 `InquiryApi`

```csharp
void Submit(string content, Action<EmptyResponse> onSuccess, Action<ApiError> onError = null);
void GetList(Action<List<InquiryDto>> onSuccess, Action<ApiError> onError = null);

[Serializable] public class InquiryDto
{
    public int    id;
    public string content;
    public string adminReply;
    public string repliedAt;
    public string createdAt;
}
```

### 8.8 `DailyLoginApi`

```csharp
void Process(Action<DailyLoginResponse> onSuccess, Action<ApiError> onError = null);

[Serializable] public class DailyLoginResponse { public bool rewarded; }
```

### 8.9 `ShoutApi`

```csharp
void GetActive(Action<List<ShoutDto>> onSuccess, Action<ApiError> onError = null);

[Serializable] public class ShoutDto
{
    public int    id;
    public string message;
    public string createdAt;
    public string expiresAt;
}
```

## 9. Auth 확장 (`AuthManager`)

```csharp
public bool IsGoogleLinked { get; private set; }

public void SetGoogleLinked(bool linked)
{
    IsGoogleLinked = linked;
    PlayerPrefs.SetInt("IsGoogleLinked", linked ? 1 : 0);
    PlayerPrefs.Save();
}

// LoadSavedToken 끝에 추가
IsGoogleLinked = PlayerPrefs.GetInt("IsGoogleLinked", 0) == 1;

// Clear 끝에 추가
PlayerPrefs.DeleteKey("IsGoogleLinked");
IsGoogleLinked = false;

// SaveToken에는 추가하지 않음 — 로그인 흐름별로 호출처가 명시적으로 SetGoogleLinked 호출
```

`SaveToken`은 토큰만 저장. 연동 상태는 호출처(GuestLogin → false / GoogleLogin → true / LinkGoogle → true / ResolveConflict → true)가 책임진다.

## 10. 폴더 배치

```
WebFramework/
├── Core/
│   ├── ApiClient.cs         # GET/POST/PUT/DELETE + 401/503/429 인터셉터
│   ├── ApiConfig.cs         # /api prefix 정정 + 8개 정적 클래스
│   ├── ApiError.cs          # 신규
│   ├── JsonHelper.cs        # 신규 (List<T> wrapping)
│   └── RestLogger.cs
├── Auth/
│   ├── AuthManager.cs       # IsGoogleLinked 추가
│   └── GoogleSignInProvider.cs
├── Api/
│   ├── AuthApi.cs           # Logout/Link/Resolve/Withdraw 추가
│   ├── VersionApi.cs        # 신규
│   ├── NoticeApi.cs         # 신규
│   ├── MailApi.cs           # 신규
│   ├── StageApi.cs          # 신규
│   ├── RankingApi.cs        # 신규
│   ├── InquiryApi.cs        # 신규
│   ├── DailyLoginApi.cs     # 신규
│   └── ShoutApi.cs          # 신규
└── Models/
    ├── AuthModels.cs        # GoogleConflictResponse / PlayerSummary / LinkGoogleRequest / ResolveGoogleConflictRequest 추가
    ├── CommonModels.cs      # 신규: EmptyResponse
    ├── VersionModels.cs     # 신규
    ├── NoticeModels.cs      # 신규
    ├── MailModels.cs        # 신규
    ├── StageModels.cs       # 신규
    ├── RankingModels.cs     # 신규
    ├── InquiryModels.cs     # 신규
    ├── DailyLoginModels.cs  # 신규
    └── ShoutModels.cs       # 신규
```

## 11. 백엔드 신설 권고 (사용자 승인 시 별도 라운드)

| 우선순위 | Endpoint | 응답 | 사용처 |
|---|---|---|---|
| 1 | `GET /api/inventory` | `[{itemId, itemName, quantity}]` | 03 인벤토리 버튼 |
| 2 | `GET /api/auth/me` | `{playerId, nickname, level, isGoogleLinked, createdAt}` | 03 문의 메시지 닉네임 / 구글 연동 상태 신뢰 |

분량: 컨트롤러 2 + 서비스 메서드 2 + DTO 2 = 작은 라운드.

## 12. 위험 요소 / 미해결

- **`JsonUtility` 한계**: 최상위 배열, nullable, 동적 키 모두 약함. `Newtonsoft.Json` 도입을 별도 라운드로 박제 권장(CLIENT_GUIDE 사전 준비 표가 권장하지만 본 라운드는 JsonUtility + JsonHelper 패턴으로 우회).
- **`SemaphoreSlim`/Awaitable 호환성**: Unity 6 Awaitable과 `Task` 혼용 시 동기화 컨텍스트 검증 필요. 컴파일 통과 후 실행 검증은 unity-programmer 단계.
- **RefreshToken 보안 저장소**: 현재 PlayerPrefs 평문. 본 라운드 범위 외(연동 검증 단계). AuthManager 메서드 인터페이스만 분리해두어 추후 교체 용이성 확보.
- **`int? requiredPrevStageId` 직렬화**: JsonUtility 한계로 0 매핑 합의. progress.isLocked로 잠금 판정 일원화.
