using System;
using System.Threading.Tasks;

// 인증 관련 API 호출 모음 싱글톤
// Phase 0: 기존 3개 메서드의 오류 콜백 타입을 Action<string> → Action<ApiError>로 정정
public class AuthApi : Singleton<AuthApi>
{
    // ── Task 기반 비동기 정적 메서드 (await 호출용) ──────────────────────────

    // 게스트 로그인 비동기 버전 — DeviceId로 JWT 발급 요청
    public static Task<ApiResult<TokenResponse>> GuestLoginAsync(string deviceId)
        => ApiClient.Instance.PostAsync<GuestLoginRequest, TokenResponse>(
            ApiConfig.Auth.Guest, new GuestLoginRequest { deviceId = deviceId });

    // 구글 로그인 비동기 버전 — Google Sign-In SDK에서 받은 IdToken으로 JWT 발급
    public static Task<ApiResult<TokenResponse>> GoogleLoginAsync(string idToken)
        => ApiClient.Instance.PostAsync<GoogleLoginRequest, TokenResponse>(
            ApiConfig.Auth.Google, new GoogleLoginRequest { idToken = idToken });

    // Access Token 재발급 비동기 버전 — RefreshToken으로 무중단 토큰 갱신
    public static Task<ApiResult<TokenResponse>> RefreshAsync(string refreshToken)
        => ApiClient.Instance.PostAsync<RefreshTokenRequest, TokenResponse>(
            ApiConfig.Auth.Refresh, new RefreshTokenRequest { refreshToken = refreshToken });

    // 로그아웃 비동기 버전 — 서버 측 RefreshToken 무효화
    public static Task<ApiResult<EmptyResponse>> LogoutAsync(string refreshToken)
        => ApiClient.Instance.PostAsync<RefreshTokenRequest, EmptyResponse>(
            ApiConfig.Auth.Logout, new RefreshTokenRequest { refreshToken = refreshToken });

    // 구글 계정 연동 비동기 버전 — 게스트 계정에 구글 IdToken으로 계정 연결
    public static Task<ApiResult<EmptyResponse>> LinkGoogleAsync(string idToken)
        => ApiClient.Instance.PostAsync<LinkGoogleRequest, EmptyResponse>(
            ApiConfig.Auth.LinkGoogle, new LinkGoogleRequest { idToken = idToken });

    // 구글 충돌 해소 비동기 버전 — 409 GOOGLE_ACCOUNT_CONFLICT 발생 시 기존 구글 계정으로 전환
    public static Task<ApiResult<TokenResponse>> ResolveGoogleConflictAsync(string idToken)
        => ApiClient.Instance.PostAsync<ResolveGoogleConflictRequest, TokenResponse>(
            ApiConfig.Auth.ResolveGoogle, new ResolveGoogleConflictRequest { idToken = idToken });

    // 회원 탈퇴 비동기 버전 — 계정 삭제 요청 (복구 불가)
    public static Task<ApiResult<EmptyResponse>> WithdrawAsync()
        => ApiClient.Instance.DeleteAsync<EmptyResponse>(ApiConfig.Auth.Withdraw);


    // 게스트 로그인 - DeviceId로 JWT 발급 요청
    public void GuestLogin(string deviceId,
        Action<TokenResponse> onSuccess, Action<ApiError> onError = null)
    {
        var request = new GuestLoginRequest { deviceId = deviceId };
        ApiClient.Instance.Post<GuestLoginRequest, TokenResponse>(
            ApiConfig.Auth.Guest, request, onSuccess, onError);
    }

    // 구글 로그인 - Google Sign-In SDK에서 받은 IdToken으로 JWT 발급 요청
    public void GoogleLogin(string idToken,
        Action<TokenResponse> onSuccess, Action<ApiError> onError = null)
    {
        var request = new GoogleLoginRequest { idToken = idToken };
        ApiClient.Instance.Post<GoogleLoginRequest, TokenResponse>(
            ApiConfig.Auth.Google, request, onSuccess, onError);
    }

    // Access Token 재발급 - RefreshToken을 이용한 무중단 토큰 갱신
    public void Refresh(string refreshToken,
        Action<TokenResponse> onSuccess, Action<ApiError> onError = null)
    {
        var request = new RefreshTokenRequest { refreshToken = refreshToken };
        ApiClient.Instance.Post<RefreshTokenRequest, TokenResponse>(
            ApiConfig.Auth.Refresh, request, onSuccess, onError);
    }

    // 로그아웃 - 서버 측 RefreshToken 무효화 요청
    public void Logout(string refreshToken,
        Action<EmptyResponse> onSuccess, Action<ApiError> onError = null)
    {
        var request = new RefreshTokenRequest { refreshToken = refreshToken };
        ApiClient.Instance.Post<RefreshTokenRequest, EmptyResponse>(
            ApiConfig.Auth.Logout, request, onSuccess, onError);
    }

    // 구글 계정 연동 - 게스트 계정에 구글 IdToken으로 계정 연결
    public void LinkGoogle(string idToken,
        Action<EmptyResponse> onSuccess, Action<ApiError> onError = null)
    {
        var request = new LinkGoogleRequest { idToken = idToken };
        ApiClient.Instance.Post<LinkGoogleRequest, EmptyResponse>(
            ApiConfig.Auth.LinkGoogle, request, onSuccess, onError);
    }

    // 구글 계정 충돌 해소 - 409 GOOGLE_ACCOUNT_CONFLICT 발생 시 기존 구글 계정으로 전환
    public void ResolveGoogleConflict(string idToken,
        Action<TokenResponse> onSuccess, Action<ApiError> onError = null)
    {
        var request = new ResolveGoogleConflictRequest { idToken = idToken };
        ApiClient.Instance.Post<ResolveGoogleConflictRequest, TokenResponse>(
            ApiConfig.Auth.ResolveGoogle, request, onSuccess, onError);
    }

    // 회원 탈퇴 - 계정 삭제 요청 (복구 불가)
    public void Withdraw(
        Action<EmptyResponse> onSuccess, Action<ApiError> onError = null)
    {
        ApiClient.Instance.Delete<EmptyResponse>(
            ApiConfig.Auth.Withdraw, onSuccess, onError);
    }
}
