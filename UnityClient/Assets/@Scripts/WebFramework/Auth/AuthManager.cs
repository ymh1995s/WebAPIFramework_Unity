using System;
using UnityEngine;

// 인증 토큰 및 세션 관리 싱글톤 — ITokenProvider를 구현하여 ApiClient에 자가 등록
public class AuthManager : Singleton<AuthManager>, ITokenProvider
{
    // PlayerPrefs 키 — 단일 파일에서만 사용하므로 클래스 내부 const로 정의
    private const string KEY_REFRESH_TOKEN    = "RefreshToken";
    private const string KEY_PLAYER_ID        = "PlayerId";
    private const string KEY_IS_GOOGLE_LINKED = "IsGoogleLinked";

    // 로그인 성공 이벤트 — SaveToken 호출 시 PlayerId(string)를 전달
    // CrashReportManager 등이 구독하여 플레이어별 컨텍스트를 갱신한다
    public static event Action<string> OnLoginSuccess;

    // 로그아웃 이벤트 — Clear 호출 시 발행
    // 구독자가 플레이어별 상태를 초기화할 수 있도록 알린다
    public static event Action OnLogout;

    // 현재 로그인한 플레이어 PublicId (Guid 문자열 — 내부 int Id 아님)
    public string PlayerId     { get; private set; }

    // JWT Access Token (API 요청 헤더에 자동 첨부)
    public string AccessToken  { get; private set; }

    // Refresh Token (앱 재시작 후 자동 로그인에 사용)
    public string RefreshToken { get; private set; }

    // 신규 플레이어 여부
    public bool   IsNewPlayer  { get; private set; }

    // 구글 계정 연동 여부 — 백엔드 응답에 해당 필드 없어 클라이언트 PlayerPrefs로 직접 추적
    public bool IsGoogleLinked { get; private set; }

    // 로그인 상태 여부 - RefreshToken 존재로 판단
    public bool IsLoggedIn => !string.IsNullOrEmpty(RefreshToken);

    // 초기화 시 ApiClient에 자신을 ITokenProvider로 등록 — 결합을 인터페이스로 역전
    protected virtual void Awake()
    {
        ApiClient.Instance.TokenProvider = this;
    }

    // 앱 시작 시 PlayerPrefs에서 저장된 토큰 및 상태 복원
    public void LoadSavedToken()
    {
        RefreshToken   = PlayerPrefs.GetString(KEY_REFRESH_TOKEN, string.Empty);
        PlayerId       = PlayerPrefs.GetString(KEY_PLAYER_ID, string.Empty);
        IsGoogleLinked = PlayerPrefs.GetInt(KEY_IS_GOOGLE_LINKED, 0) == 1;
    }

    // 구글 계정 연동 상태 갱신 — 연동/해제 흐름에서 호출처가 명시적으로 호출
    public void SetGoogleLinked(bool linked)
    {
        IsGoogleLinked = linked;
        PlayerPrefs.SetInt(KEY_IS_GOOGLE_LINKED, linked ? 1 : 0);
        PlayerPrefs.Save();
    }

    // 로그인 성공 후 토큰 저장
    public void SaveToken(TokenResponse response)
    {
        PlayerId     = response.playerId;
        AccessToken  = response.accessToken;
        RefreshToken = response.refreshToken;
        IsNewPlayer  = response.isNewPlayer;

        // RefreshToken과 PlayerId를 기기에 영구 저장
        PlayerPrefs.SetString(KEY_REFRESH_TOKEN, RefreshToken);
        PlayerPrefs.SetString(KEY_PLAYER_ID, PlayerId);
        PlayerPrefs.Save();

        // 로그인 성공을 구독자에게 알림 — PlayerId 기반 컨텍스트 갱신 트리거
        OnLoginSuccess?.Invoke(response.playerId);
    }

    // 로그아웃 - 모든 토큰 초기화
    public void Clear()
    {
        PlayerId     = string.Empty;
        AccessToken  = string.Empty;
        RefreshToken = string.Empty;
        IsNewPlayer  = false;

        PlayerPrefs.DeleteKey(KEY_REFRESH_TOKEN);
        PlayerPrefs.DeleteKey(KEY_PLAYER_ID);
        PlayerPrefs.DeleteKey(KEY_IS_GOOGLE_LINKED);
        IsGoogleLinked = false;
        PlayerPrefs.Save();

        // 로그아웃을 구독자에게 알림 — 플레이어별 상태 초기화 트리거
        OnLogout?.Invoke();
    }
}
