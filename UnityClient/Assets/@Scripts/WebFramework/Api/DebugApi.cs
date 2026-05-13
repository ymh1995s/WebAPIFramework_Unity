#if DEBUG
using System.Threading.Tasks;

// 디버그 API — 개발/QA 전용 보상 즉시 지급 (Release 빌드에서 제외됨)
public static class DebugApi
{
    // 보상 즉시 지급 — POST /api/debug/grant
    // req: 지급할 경험치·아이템 목록 및 출처 키 포함
    public static Task<ApiResult<DebugGrantResponse>> GrantAsync(DebugGrantRequest req)
        => ApiClient.Instance.PostAsync<DebugGrantRequest, DebugGrantResponse>(
            ApiConfig.Debug.Grant, req);
}
#endif
