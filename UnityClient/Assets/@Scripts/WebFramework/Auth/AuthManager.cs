using UnityEngine;

// 인증 토큰 및 세션 관리 싱글톤
public class AuthManager : Singleton<AuthManager>
{
    // 현재 로그인한 플레이어 PublicId (Guid 문자열 — 내부 int Id 아님)
    public string PlayerId     { get; private set; }

    // JWT Access Token (API 요청 헤더에 자동 첨부)
    public string AccessToken  { get; private set; }

    // Refresh Token (앱 재시작 후 자동 로그인에 사용)
    public string RefreshToken { get; private set; }

    // 신규 플레이어 여부
    public bool   IsNewPlayer  { get; private set; }

    // 로그인 상태 여부 - RefreshToken 존재로 판단
    public bool IsLoggedIn => !string.IsNullOrEmpty(RefreshToken);

    // 앱 시작 시 PlayerPrefs에서 저장된 토큰 복원
    public void LoadSavedToken()
    {
        RefreshToken = PlayerPrefs.GetString("RefreshToken", string.Empty);
        PlayerId     = PlayerPrefs.GetString("PlayerId", string.Empty);
    }

    // 로그인 성공 후 토큰 저장
    public void SaveToken(TokenResponse response)
    {
        PlayerId     = response.playerId;
        AccessToken  = response.accessToken;
        RefreshToken = response.refreshToken;
        IsNewPlayer  = response.isNewPlayer;

        // RefreshToken과 PlayerId를 기기에 영구 저장
        PlayerPrefs.SetString("RefreshToken", RefreshToken);
        PlayerPrefs.SetString("PlayerId", PlayerId);
        PlayerPrefs.Save();
    }

    // 로그아웃 - 모든 토큰 초기화
    public void Clear()
    {
        PlayerId     = string.Empty;
        AccessToken  = string.Empty;
        RefreshToken = string.Empty;
        IsNewPlayer  = false;

        PlayerPrefs.DeleteKey("RefreshToken");
        PlayerPrefs.DeleteKey("PlayerId");
        PlayerPrefs.Save();
    }
}
