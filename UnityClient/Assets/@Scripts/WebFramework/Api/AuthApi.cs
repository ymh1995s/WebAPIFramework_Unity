using System;

// 인증 관련 API 호출 모음 싱글톤
// Phase 0: 기존 3개 메서드의 오류 콜백 타입을 Action<string> → Action<ApiError>로 정정
public class AuthApi : Singleton<AuthApi>
{
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
}
