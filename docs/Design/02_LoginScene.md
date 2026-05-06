# 02. LoginScene

## 1. 요구사항

요구사항.md `## LoginScene`:
- GuestLogin Btn → 게스트 로그인
- GoogleLogin Btn → 구글 로그인

CLIENT_GUIDE 4번(게스트), 5번(구글), 7번(충돌 해소).

## 2. 1차 대비 변경점

### 2.1 ApiConfig 경로 정정 (필수)

**1차 박제 오류**: 현재 `ApiConfig.Auth`가 `/auth/guest`, `/auth/refresh` 등 **`/api` prefix 누락**.
실측: 백엔드 `[Route("api/auth")]`. BaseUrl `http://192.168.219.101:5058` 기준이면 클라이언트 호출 URL이 `http://...:5058/auth/guest` 로 만들어져 **404가 정상**. 그동안 동작했다는 것은 환경 우회(리버스 프록시)일 가능성. 이를 정정.

```csharp
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
```

### 2.2 구글 로그인 — 409 충돌 해소 흐름 박제 (1차 누락)

CLIENT_GUIDE 5/7번. 구글 로그인 응답이 409 + `errorCode=GOOGLE_ACCOUNT_CONFLICT`이면 `existingPlayer`/`currentGuestPlayer` 프로필을 보여주고 사용자 선택을 받는다.

흐름:
```
GoogleLogin 200 → MainScene
GoogleLogin 409 (게스트 상태에서 호출됨)
  → UI_SelectPopup ("기존 구글 계정으로 전환?  진행도가 사라집니다")
        OK → AuthApi.ResolveGoogleConflict(idToken) → MainScene
        Cancel → 게스트 상태 유지 (LoginScene 머무름)
GoogleLogin 401 → "유효하지 않은 IdToken" → PopupService.ShowError
GoogleLogin 503 → 점검 인터셉터(자동)
```

**핵심**: 409는 **게스트 토큰 보유 + GoogleId가 다른 계정에 이미 묶여 있을 때**만 발생. 비게스트(미로그인) 호출은 그냥 매칭/신규 처리되어 200 반환. ApiClient는 인증 토큰이 있으면 자동 첨부 → 자연스럽게 게스트 상태에서 호출됨.

### 2.3 에러 표시 일원화

`UI_LoginScene.OnLoginError`는 `RestLogger.Error` + `PopupService.ShowError(error.UserMessage)` 둘 다.
1차 설계의 `Action<string> onError`는 ProblemDetails(`status`, `errorCode`, `detail`)을 표현 못 하므로 `ApiError` 객체로 교체(07 문서).

### 2.4 게스트 로그인 — DeviceId 정책 강화

CLIENT_GUIDE 4번 + 부록 C. 형식: 8~64자, 영숫자/하이픈/언더스코어. PlayerPrefs 키 `"DeviceId"`에 영구 저장. 현재 `SystemInfo.deviceUniqueIdentifier`만 쓰는데, **에디터/일부 디바이스에서 빈 문자열·64자 초과**가 가능하므로 검증·재발급(GUID) 폴백을 추가.

의사코드:
```csharp
static string GetOrCreateDeviceId()
{
    var saved = PlayerPrefs.GetString("DeviceId", null);
    if (IsValid(saved)) return saved;

    string id = SystemInfo.deviceUniqueIdentifier;
    if (!IsValid(id)) id = Guid.NewGuid().ToString("N");
    if (id.Length > 64) id = id.Substring(0, 64);

    PlayerPrefs.SetString("DeviceId", id);
    PlayerPrefs.Save();
    return id;
}
```

### 2.5 RefreshToken 저장소 — 권고 박제 (현 라운드 미구현)

CLIENT_GUIDE 부록 C. RefreshToken은 Android Keystore / iOS Keychain 권장이나 현재는 PlayerPrefs 평문. **현 라운드 범위 외**(연동 검증용 더미 단계). `AuthManager` 주석에 "프로덕션 진입 시 보안 저장소로 교체"라고 박제하고 인터페이스만 캡슐화 (`AuthManager.SaveRefreshToken/LoadRefreshToken` 별도 메서드).

## 3. 씬 구성

- `LoginScene.unity` 루트에 `Scene_Login` (BaseScene 상속) 부착
- `Scene_Login`은 `SceneType = LoginScene` 지정 + `UIManager.ShowSceneUI<UI_LoginScene>()` 호출

## 4. 매니저 책임

| 매니저 | 책임 |
|---|---|
| `Scene_Login` | 씬 진입 시 UI_LoginScene 인스턴싱 |
| `UI_LoginScene` (기존) | 두 버튼 onClick → AuthApi 호출 → 결과 처리 |
| `AuthApi` (기존, 확장) | GuestLogin / GoogleLogin / **ResolveGoogleConflict (신설)** |
| `AuthManager` (기존) | SaveToken / IsLoggedIn / **IsGoogleLinked 갱신** |
| `GoogleSignInProvider` (기존) | IdToken 발급 |
| `PopupService` | 충돌 다이얼로그(`UI_SelectPopup`) + 에러(`UI_ErrorPopup`) |
| `SceneManager` | MainScene 전환 |
| `EventManager` | `LoginSuccess` 발행 (기존) |

## 5. UI 구조

`UI_LoginScene` (UI_UGUI):
```
Buttons enum: { GuestLoginBtn, GoogleLoginBtn }
```
변경 없음. 기존 코드 유지.

충돌 처리는 `UI_SelectPopup`(06 문서) 재사용. 텍스트 포맷:
```
이 구글 계정은 다른 캐릭터에 이미 연동되어 있습니다.

기존 캐릭터: {existingPlayer.nickname} (Lv.{existingPlayer.level})
현재 게스트: {currentGuestPlayer.nickname} (Lv.{currentGuestPlayer.level})

기존 캐릭터로 전환하시겠습니까?
※ 현재 게스트 진행도는 영구적으로 사라집니다.
```

## 6. WebFramework 매핑

| 기능 | Endpoint (Method) | Body | 응답 | Auth |
|---|---|---|---|---|
| 게스트 로그인 | `POST /api/auth/guest` | `{deviceId}` | TokenResponse | X |
| 구글 로그인 | `POST /api/auth/google` | `{idToken}` | TokenResponse / 409 GoogleConflictDto / 401 | (선택) 게스트 토큰 첨부 시 충돌 감지 |
| 구글 충돌 해소 | `POST /api/auth/google/resolve-conflict` | `{idToken}` | TokenResponse | O (게스트 토큰) |

`TokenResponse = {accessToken, refreshToken, playerId, isNewPlayer}`. **`isGoogleLinked` 필드 없음** — 클라가 추적.

`GoogleConflictDto`:
```json
{
  "errorCode": "GOOGLE_ACCOUNT_CONFLICT",
  "existingPlayer": { "playerId":"...", "nickname":"...", "level":5, "createdAt":"...", "lastLoginAt":"..." },
  "currentGuestPlayer": { ... }
}
```

## 7. 데이터 흐름

```
[GuestLoginBtn]
UI_LoginScene → AuthApi.GuestLogin(deviceId)
              → ApiClient.Post /api/auth/guest
              → 200 TokenResponse → AuthManager.SaveToken (IsGoogleLinked=false)
              → EventManager.LoginSuccess → SceneManager.LoadScene(MainScene)

[GoogleLoginBtn]
UI_LoginScene → GoogleSignInProvider.SignIn → idToken
              → AuthApi.GoogleLogin(idToken)
              → ApiClient.Post /api/auth/google (게스트 토큰 자동 첨부)
              ├─ 200 → AuthManager.SaveToken + SetGoogleLinked(true) → MainScene
              ├─ 409 → PopupService.ShowSelect(...) → OK → ResolveGoogleConflict
              │       → 200 → SaveToken + SetGoogleLinked(true) → MainScene
              │       → Cancel → 닫기 (게스트 유지)
              └─ 4xx/5xx → PopupService.ShowError
```

## 8. 영향받는 파일

**수정**
- `WebFramework/Core/ApiConfig.cs` — `Auth` 정적 클래스 경로 7개 정정/추가
- `WebFramework/Auth/AuthManager.cs` — `IsGoogleLinked` 프로퍼티 + PlayerPrefs 키
- `WebFramework/Api/AuthApi.cs` — `Logout`, `LinkGoogle`, `ResolveGoogleConflict`, `Withdraw` 추가
- `WebFramework/Models/AuthModels.cs` — `LinkGoogleRequest`, `ResolveGoogleConflictRequest`, `GoogleConflictResponse`, `PlayerSummary` 추가
- `UI/UI_LoginScene.cs` — `OnLoginError`에 `PopupService.ShowError` 한 줄 추가, 409 응답 분기

**신규**
- `Scenes/Scene_Login.cs`

## 9. DoD

- [ ] `Scene_Login` 부착된 LoginScene 진입 시 UI_LoginScene 자동 인스턴싱
- [ ] 게스트 로그인 200 → MainScene 전환 + AuthManager.IsGoogleLinked=false
- [ ] 구글 로그인 200 → MainScene 전환 + IsGoogleLinked=true
- [ ] 구글 로그인 409 → UI_SelectPopup → OK → resolve-conflict 호출 → MainScene
- [ ] 구글 로그인 409 → Cancel → LoginScene 유지
- [ ] 모든 4xx/5xx → UI_ErrorPopup 노출
