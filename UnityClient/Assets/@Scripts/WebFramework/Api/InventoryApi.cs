using System;
using System.Collections.Generic;

// 인벤토리 API 싱글톤 — 플레이어 보유 아이템 목록 조회
public class InventoryApi : Singleton<InventoryApi>
{
    // 인벤토리 목록 조회 — GET /api/items/inventory (백엔드가 최상위 배열 반환)
    public void GetInventory(
        Action<List<PlayerItemDto>> onSuccess, Action<ApiError> onError = null)
    {
        ApiClient.Instance.GetList<PlayerItemDto>(
            ApiConfig.Inventory.GetInventory, onSuccess, onError);
    }
}
