using System;
using System.Collections.Generic;

// 외침(월드 메시지) API 싱글톤 — 현재 활성화된 외침 목록 조회
public class ShoutApi : Singleton<ShoutApi>
{
    // 현재 유효한(만료 전) 외침 목록 조회 — GET /api/shouts/active (백엔드가 최상위 배열 반환)
    public void GetActive(
        Action<List<ShoutDto>> onSuccess, Action<ApiError> onError = null)
    {
        ApiClient.Instance.GetList<ShoutDto>(
            ApiConfig.Shout.Active, onSuccess, onError);
    }
}
