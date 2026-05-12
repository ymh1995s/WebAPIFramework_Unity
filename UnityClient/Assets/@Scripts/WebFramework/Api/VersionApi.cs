using System.Collections.Generic;
using System.Threading.Tasks;

// 버전 체크 API — 앱 시작 시 강제 업데이트 여부 확인에 사용
public static class VersionApi
{
    // 버전 체크 비동기 버전 — 현재 앱 버전을 쿼리 파라미터로 전달해 강제 업데이트 여부 확인
    public static Task<ApiResult<VersionCheckResponse>> CheckAsync(string currentVersion)
        => ApiClient.Instance.GetWithQueryAsync<VersionCheckResponse>(
            ApiConfig.Version.Check, new Dictionary<string, string> { { "version", currentVersion } });
}
