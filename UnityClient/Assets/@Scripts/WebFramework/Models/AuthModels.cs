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

// 구글 계정 연동 요청 모델 - 게스트 계정에 구글 계정을 연결할 때 사용
[Serializable]
public class LinkGoogleRequest
{
    public string idToken;
}

// 구글 계정 충돌 해소 요청 모델 - 409 GOOGLE_ACCOUNT_CONFLICT 발생 시 기존 계정으로 전환
[Serializable]
public class ResolveGoogleConflictRequest
{
    public string idToken;
}
