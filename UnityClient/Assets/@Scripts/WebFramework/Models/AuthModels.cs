using System;

// 게스트 로그인 요청 모델 (camelCase - ASP.NET Core 기본 JSON 직렬화 정책 대응)
[Serializable]
public class GuestLoginRequest
{
    public string deviceId;
}

// 토큰 응답 모델 - 로그인/재발급 공통 (camelCase 응답 대응)
[Serializable]
public class TokenResponse
{
    public string accessToken;
    public string refreshToken;
    public string playerId;
    public bool   isNewPlayer;
}

// 토큰 재발급 요청 모델
[Serializable]
public class RefreshTokenRequest
{
    public string refreshToken;
}

// 구글 로그인 요청 모델 - Google Sign-In SDK에서 받은 IdToken 전달
[Serializable]
public class GoogleLoginRequest
{
    public string idToken;
}
