using System;

// 인증 관련 API 호출 모음 싱글톤
public class AuthApi : Singleton<AuthApi>
{
    // 게스트 로그인 - DeviceId로 JWT 발급 요청
    public void GuestLogin(string deviceId,
        Action<TokenResponse> onSuccess, Action<string> onError = null)
    {
        var request = new GuestLoginRequest { deviceId = deviceId };
        ApiClient.Instance.Post<GuestLoginRequest, TokenResponse>(
            ApiConfig.Auth.Guest, request, onSuccess, onError);
    }

    // Access Token 재발급
    public void Refresh(string refreshToken,
        Action<TokenResponse> onSuccess, Action<string> onError = null)
    {
        var request = new RefreshTokenRequest { refreshToken = refreshToken };
        ApiClient.Instance.Post<RefreshTokenRequest, TokenResponse>(
            ApiConfig.Auth.Refresh, request, onSuccess, onError);
    }
}
