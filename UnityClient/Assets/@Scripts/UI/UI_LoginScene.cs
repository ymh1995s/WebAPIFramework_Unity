using UnityEngine;

// 로그인 씬 UI — 게스트/구글 로그인 버튼 처리 및 409 충돌 해소 흐름 포함
public class UI_LoginScene : UI_UGUI, IUI_Scene
{
    // Canvas 하위 버튼 이름과 일치해야 함
    enum Buttons { GuestLoginBtn, GoogleLoginBtn }

    // 구글 로그인 409 충돌 시 ResolveGoogleConflict 재사용을 위해 보관
    string _pendingGoogleIdToken;

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
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        RestLogger.Info($"게스트 로그인 시도 | DeviceId: {deviceId[..8]}...");
        AuthApi.Instance.GuestLogin(deviceId, onSuccess: OnLoginSuccess, onError: OnLoginError);
    }

    // 구글 로그인 버튼 클릭 처리
    private async void OnClickGoogleLogin()
    {
        RestLogger.Info("구글 로그인 시도...");
        try
        {
            var user = await GoogleSignInProvider.SignIn();

            // 에디터 환경 등에서 null 반환 시 조용히 중단
            if (user == null) return;

            // 409 충돌 해소 시 재사용하기 위해 IdToken 보관
            _pendingGoogleIdToken = user.IdToken;

            // IdToken을 백엔드로 전송하여 JWT 발급 요청
            AuthApi.Instance.GoogleLogin(user.IdToken, OnLoginSuccess, OnGoogleLoginError);
        }
        catch (System.Exception e)
        {
            // 구글 SDK 자체 오류는 ApiError로 래핑하여 공통 오류 처리
            OnLoginError(new ApiError
            {
                Status         = 0,
                Title          = "구글 인증 실패",
                Detail         = e.Message,
                IsNetworkError = false,
            });
        }
    }

    // 구글 로그인 전용 오류 처리 — 409 충돌은 전환 팝업, 그 외는 공통 오류 처리
    private void OnGoogleLoginError(ApiError error)
    {
        if (error.ErrorCode == "GOOGLE_ACCOUNT_CONFLICT")
        {
            // 이미 다른 기기/계정에 연결된 구글 계정 — 전환 여부 확인
            PopupService.ShowSelect(
                "이미 다른 구글 계정으로 가입된 기기입니다.\n해당 구글 계정으로 전환하시겠습니까?",
                onOk:     OnResolveConflict,
                onCancel: null
            );
        }
        else
        {
            OnLoginError(error);
        }
    }

    // 409 충돌 해소 — 기존 구글 계정으로 전환 요청
    private void OnResolveConflict()
    {
        AuthApi.Instance.ResolveGoogleConflict(
            _pendingGoogleIdToken,
            onSuccess: OnLoginSuccess,
            onError:   OnLoginError
        );
    }

    // 로그인 성공 공통 처리
    private void OnLoginSuccess(TokenResponse response)
    {
        AuthManager.Instance.SaveToken(response);
        RestLogger.Info($"로그인 성공 | PlayerId: {response.playerId} | 신규: {response.isNewPlayer}");

        // 로그인 성공 이벤트 발행 후 MainScene 전환
        EventManager.Instance.TriggerEvent(Define.EEventType.LoginSuccess);
        SceneManager.Instance.LoadScene(Define.EScene.MainScene);
    }

    // 로그인 오류 공통 처리 — 팝업 표시
    private void OnLoginError(ApiError error)
    {
        RestLogger.Error($"로그인 실패: {error.UserMessage}");
        PopupService.ShowError(error);
    }
}
