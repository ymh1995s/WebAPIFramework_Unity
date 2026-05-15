using System.Threading.Tasks;

// 원격 설정 API — 서버에서 키-값 설정값 다운로드
// 인증 불필요(AllowAnonymous) — 부팅 초기 단계에서도 호출 가능
public static class RemoteConfigApi
{
    // 원격 설정 전체 조회 — GET /api/remoteconfig
    // Dictionary<string,string> 응답이므로 Newtonsoft 기반 파서 경유
    public static Task<ApiResult<RemoteConfigResponse>> GetAsync()
        => ApiClient.Instance.GetAsyncNewtonsoft<RemoteConfigResponse>(ApiConfig.RemoteConfig.Get);
}
