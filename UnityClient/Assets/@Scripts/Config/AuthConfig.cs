using UnityEngine;

// 인증 관련 환경설정 — WEB_CLIENT_ID 등 빌드 환경별 인증 키 관리
// Inspector에서 빌드 환경에 맞는 값을 입력하여 사용
[CreateAssetMenu(fileName = "AuthConfig", menuName = "Config/AuthConfig")]
public class AuthConfig : ScriptableObject
{
    // 구글 로그인 Web Client ID — Google Cloud Console에서 발급한 OAuth2 클라이언트 ID
    // 배포 전 실제 값으로 교체 필요 (Google Cloud Console → 사용자 인증 정보 → OAuth 2.0 클라이언트 ID)
    [SerializeField] private string webClientId = "";

    // 외부에서 Web Client ID를 읽기 전용으로 접근
    public string WebClientId => webClientId;
}
