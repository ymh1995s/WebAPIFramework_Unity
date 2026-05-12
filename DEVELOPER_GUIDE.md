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

## 신규 도메인 추가 절차 (예: `Friend`)

작업 위치 3 + 옵션 1.

### 1. `WebFramework/Core/ApiConfig.cs` — 엔드포인트 경로 상수 블록

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
| POST | 있음 | `PostAsync<TReq, TRes>(endpoint, body)` |
| POST | 없음 | `PostAsync<TRes>(endpoint)` |
| PUT | 있음 | `PutAsync<TReq, TRes>(endpoint, body)` |
| DELETE | — | `DeleteAsync<TRes>(endpoint)` |

- 응답 본문 없음(204 또는 빈 200): `TRes = EmptyResponse` 사용
- URL 경로 치환: `string.Format(ApiConfig.X.Y, id)` 패턴
- 멱등성 키 필요한 POST(예: `ItemApi.UseAsync`): `clientRequestId = Guid.NewGuid().ToString()` 호출 시점에 생성

---

## 관련 문서

| 문서 | 내용 |
|---|---|
| `../CLIENT_GUIDE.md` | 백엔드 엔드포인트 명세 — 요청/응답/에러 처리 (백엔드 팀 관리) |
| `DEVNOTES.md` `[설계 결정]` | 본 패턴의 채택 근거 박제 (2026-05-12) |
| `CLAUDE.md` | AI 에이전트 행동 규칙 + 폴더 구조 |
