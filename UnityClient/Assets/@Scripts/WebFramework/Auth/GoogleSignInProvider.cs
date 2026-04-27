using Google;
using System.Threading.Tasks;
using UnityEngine;

// Google Sign-In SDK 래퍼 - IdToken 획득 담당
public static class GoogleSignInProvider
{
    // Google Cloud Console에서 발급받은 Web Client ID
    private const string WEB_CLIENT_ID = "1080787057808-g3elodns8j77qk67hoh67hm8nbess0r9.apps.googleusercontent.com";

    // 구글 로그인 시도 → GoogleSignInUser 반환 (IdToken 포함)
    public static Task<GoogleSignInUser> SignIn()
    {
        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            WebClientId = WEB_CLIENT_ID,
            RequestIdToken = true
        };
        return GoogleSignIn.DefaultInstance.SignIn();
    }

    // Silent 로그인 (이전 로그인 세션이 있는 경우 UI 없이 자동 인증)
    public static Task<GoogleSignInUser> SignInSilently()
    {
        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            WebClientId = WEB_CLIENT_ID,
            RequestIdToken = true
        };
        return GoogleSignIn.DefaultInstance.SignInSilently();
    }

    // 로그아웃
    public static void SignOut()
    {
        GoogleSignIn.DefaultInstance.SignOut();
    }
}
