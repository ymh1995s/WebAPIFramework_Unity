using Google;
using System;
using System.Threading.Tasks;
using UnityEngine;

// Google Sign-In SDK 래퍼 - IdToken 획득 담당
public static class GoogleSignInProvider
{
    // 구글 SDK 에러 코드 → 사용자 친화적 메시지 변환
    // GoogleSignInStatusCode 기준: Canceled=2, InvalidAccount=4, InternalError=7, NetworkError=8
    private static string NormalizeError(Exception e)
    {
        // AggregateException 언래핑 — Task.FromException 경로에서 래핑될 수 있음
        var inner = (e is AggregateException agg) ? agg.InnerException ?? e : e;

        return inner switch
        {
            GoogleSignIn.SignInException sie when sie.Status == GoogleSignInStatusCode.Canceled
                => "구글 로그인이 취소되었습니다.",
            GoogleSignIn.SignInException sie when sie.Status == GoogleSignInStatusCode.NetworkError
                => "네트워크 오류로 구글 로그인에 실패했습니다. 인터넷 연결을 확인해 주세요.",
            GoogleSignIn.SignInException sie when sie.Status == GoogleSignInStatusCode.InvalidAccount
                => "유효하지 않은 구글 계정입니다. 다른 계정으로 시도해 주세요.",
            GoogleSignIn.SignInException sie when sie.Status == GoogleSignInStatusCode.ApiNotConnected
                => "구글 로그인 서비스에 연결할 수 없습니다.",
            GoogleSignIn.SignInException sie when sie.Status == GoogleSignInStatusCode.Timeout
                => "구글 로그인이 시간 초과되었습니다. 다시 시도해 주세요.",
            GoogleSignIn.SignInException sie when sie.Status == GoogleSignInStatusCode.InternalError
                => "구글 내부 오류가 발생했습니다. 잠시 후 다시 시도해 주세요.",
            NotSupportedException
                => inner.Message,
            _
                => $"구글 인증 중 오류가 발생했습니다: {inner.Message}"
        };
    }

    // 구성 초기화 — Platform 빌드 전용, 중복 설정 방지
    // Web Client ID는 AuthConfig ScriptableObject에서 읽어 하드코딩을 회피한다
    private static void Configure()
    {
        // AuthConfig.asset 미생성 시 NullReferenceException 방지 — 명확한 에러 로그 후 중단
        if (DataManager.Instance.AuthConfig == null)
        {
            Debug.LogError("[GoogleSignInProvider] AuthConfig.asset이 없습니다. " +
                           "Unity Editor에서 Resources/PreLoad/Config/AuthConfig.asset을 생성하고 WebClientId를 입력하세요.");
            return;
        }
        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            WebClientId    = DataManager.Instance.AuthConfig.WebClientId,
            RequestIdToken = true
        };
    }

    // 구글 로그인 시도 → GoogleSignInUser 반환 (IdToken 포함)
    // 실패 시 NormalizeError()로 정규화된 메시지를 포함한 Exception throw
    // 에디터/스탠드얼론 분기에서 await 경로가 제거되어 CS1998이 발생 — 의도된 플랫폼 분기이므로 억제
#pragma warning disable CS1998
    public static async Task<GoogleSignInUser> SignIn()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        // 에디터/PC 환경에서는 네이티브 SDK 바인딩이 없어 크래시 발생 — null 반환
        RestLogger.Warn("에디터/PC 환경에서는 구글 로그인을 사용할 수 없습니다. Android/iOS 빌드에서 시도해 주세요.");
        return null;
#else
        try
        {
            Configure();
            return await GoogleSignIn.DefaultInstance.SignIn();
        }
        catch (OperationCanceledException)
        {
            // 사용자가 구글 로그인 팝업을 취소한 경우 — 조용히 null 반환
            return null;
        }
        catch (Exception e)
        {
            // SDK 에러를 정규화된 메시지로 변환하여 상위로 전파
            throw new Exception(NormalizeError(e), e);
        }
#endif
    }
#pragma warning restore CS1998

    // Silent 로그인 (이전 로그인 세션이 있는 경우 UI 없이 자동 인증)
    // 실패 시 NormalizeError()로 정규화된 메시지를 포함한 Exception throw
    // 에디터/스탠드얼론 분기에서 await 경로가 제거되어 CS1998이 발생 — 의도된 플랫폼 분기이므로 억제
#pragma warning disable CS1998
    public static async Task<GoogleSignInUser> SignInSilently()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        // 에디터/PC 환경에서는 네이티브 SDK 바인딩이 없어 크래시 발생 — null 반환
        RestLogger.Warn("에디터/PC 환경에서는 구글 사일런트 로그인을 사용할 수 없습니다.");
        return null;
#else
        try
        {
            Configure();
            return await GoogleSignIn.DefaultInstance.SignInSilently();
        }
        catch (OperationCanceledException)
        {
            // 사용자가 구글 로그인 팝업을 취소한 경우 — 조용히 null 반환
            return null;
        }
        catch (Exception e)
        {
            // SDK 에러를 정규화된 메시지로 변환하여 상위로 전파
            throw new Exception(NormalizeError(e), e);
        }
#endif
    }
#pragma warning restore CS1998

    // 로그아웃
    public static void SignOut()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        // 에디터/PC 환경에서는 호출을 무시
        return;
#else
        GoogleSignIn.DefaultInstance.SignOut();
#endif
    }
}
