using System;
using System.Collections.Generic;

// 버전 체크 API 싱글톤 — 앱 시작 시 강제 업데이트 여부 확인에 사용
public class VersionApi : Singleton<VersionApi>
{
    // 현재 앱 버전을 서버에 전달해 강제 업데이트 여부와 최신 버전 정보를 반환받음
    public void Check(string currentVersion,
        Action<VersionCheckResponse> onSuccess, Action<ApiError> onError = null)
    {
        // 쿼리 파라미터로 현재 버전 전달 — GET /api/version/check?version={currentVersion}
        var query = new Dictionary<string, string>
        {
            { "version", currentVersion }
        };

        ApiClient.Instance.GetWithQuery<VersionCheckResponse>(
            ApiConfig.Version.Check, query, onSuccess, onError);
    }
}
