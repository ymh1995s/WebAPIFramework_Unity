using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// 외침(월드 메시지) API 싱글톤 — 현재 활성화된 외침 목록 조회
public class ShoutApi : Singleton<ShoutApi>
{
    // ── Task 기반 비동기 정적 메서드 (await 호출용) ──────────────────────────

    // 활성 외침 목록 조회 비동기 버전 — 만료 전 외침 전체 반환
    public static Task<ApiResult<List<ShoutDto>>> GetActiveAsync()
        => ApiClient.Instance.GetListAsync<ShoutDto>(ApiConfig.Shout.Active);


    // 현재 유효한(만료 전) 외침 목록 조회 — GET /api/shouts/active (백엔드가 최상위 배열 반환)
    public void GetActive(
        Action<List<ShoutDto>> onSuccess, Action<ApiError> onError = null)
    {
        ApiClient.Instance.GetList<ShoutDto>(
            ApiConfig.Shout.Active, onSuccess, onError);
    }
}
