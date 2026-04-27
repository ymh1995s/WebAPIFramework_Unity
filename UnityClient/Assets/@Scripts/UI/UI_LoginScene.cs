using UnityEngine;

// 로그인 씬 UI - 버튼 바인딩 및 로그인 흐름 처리
public class UI_LoginScene : UI_UGUI, IUI_Scene
{
    // Canvas 하위 버튼 이름과 일치해야 함
    enum Buttons { GuestLoginBtn, GoogleLoginBtn }

    protected override void Awake()
    {
        base.Awake();

        // 버튼 바인딩
        BindButtons(typeof(Buttons));
        GetButton((int)Buttons.GuestLoginBtn).onClick.AddListener(OnClickGuestLogin);
        GetButton((int)Buttons.GoogleLoginBtn).onClick.AddListener(OnClickGoogleLogin);
    }

    // 게스트 로그인 버튼 클릭 처리
    private void OnClickGuestLogin()
    {
        // 기기 고유 ID를 DeviceId로 사용
        string deviceId = SystemInfo.deviceUniqueIdentifier;

        RestLogger.Info($"게스트 로그인 시도 | DeviceId: {deviceId[..8]}...");

        AuthApi.Instance.GuestLogin(
            deviceId,
            onSuccess: OnLoginSuccess,
            onError:   OnLoginError
        );
    }

    // 구글 로그인 버튼 클릭 처리
    private async void OnClickGoogleLogin()
    {
        RestLogger.Info("구글 로그인 시도...");

        try
        {
            var user = await GoogleSignInProvider.SignIn();

            // IdToken을 백엔드로 전송하여 JWT 발급 요청
            AuthApi.Instance.GoogleLogin(user.IdToken, OnLoginSuccess, OnLoginError);
        }
        catch (System.Exception e)
        {
            OnLoginError($"구글 인증 실패: {e.Message}");
        }
    }

    // 로그인 성공 콜백
    private void OnLoginSuccess(TokenResponse response)
    {
        AuthManager.Instance.SaveToken(response);
        RestLogger.Info($"로그인 성공 | PlayerId: {response.playerId} | 신규: {response.isNewPlayer}");

        // 로그인 성공 이벤트 발행 후 MainScene 전환
        EventManager.Instance.TriggerEvent(Define.EEventType.LoginSuccess);
        SceneManager.Instance.LoadScene(Define.EScene.MainScene);
    }

    // 로그인 실패 콜백
    private void OnLoginError(string error)
    {
        RestLogger.Error($"로그인 실패: {error}");
    }
}
