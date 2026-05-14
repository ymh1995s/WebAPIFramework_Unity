using UnityEngine;

// 환경별 서버 URL 설정 ScriptableObject
// UNITY_EDITOR / DEVELOPMENT_BUILD → 개발 서버
// STAGING 심볼 정의 시 → 스테이징 서버
// 릴리즈 빌드 → 프로덕션 서버
[CreateAssetMenu(fileName = "NetworkConfig", menuName = "Config/NetworkConfig")]
public class NetworkConfig : ScriptableObject
{
    // 개발 환경 서버 URL (로컬 또는 개발 서버)
    [SerializeField] private string _devBaseUrl;

    // 스테이징 환경 서버 URL (QA 검증용)
    [SerializeField] private string _stagingBaseUrl;

    // 프로덕션 환경 서버 URL (실 서비스)
    [SerializeField] private string _productionBaseUrl;

    // 현재 빌드 환경에 맞는 서버 URL 반환
    // 릴리즈 빌드에서 http:// URL이 설정된 경우 경고 로그 출력 (보안 주의)
    public string BaseUrl
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 에디터 또는 개발 빌드: 개발 서버 사용
            return _devBaseUrl;
#elif STAGING
            // STAGING 심볼이 정의된 빌드: 스테이징 서버 사용
            return _stagingBaseUrl;
#else
            // 릴리즈 빌드: 프로덕션 서버 사용
            if (_productionBaseUrl != null && _productionBaseUrl.StartsWith("http://"))
                Debug.LogError("[NetworkConfig] 릴리즈 빌드에서 http:// URL이 사용되고 있습니다. https://로 변경하세요.");

            return _productionBaseUrl;
#endif
        }
    }
}
