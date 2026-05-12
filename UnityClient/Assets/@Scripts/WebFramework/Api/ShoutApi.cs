using System.Collections.Generic;
using System.Threading.Tasks;

// 외침(월드 메시지) API — 현재 활성화된 외침 목록 조회
public static class ShoutApi
{
    // 활성 외침 목록 조회 비동기 버전 — 만료 전 외침 전체 반환
    public static Task<ApiResult<List<ShoutDto>>> GetActiveAsync()
        => ApiClient.Instance.GetListAsync<ShoutDto>(ApiConfig.Shout.Active);
}
