using System.Threading.Tasks;

// 인증 관련 API 호출 모음
// Phase 0: 기존 3개 메서드의 오류 콜백 타입을 Action<string> → Action<ApiError>로 정정
public static class AuthApi
{
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
}
