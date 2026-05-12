using System.Collections.Generic;
using System.Threading.Tasks;

// 아이템 API — 인벤토리 조회 및 아이템 사용 요청을 처리한다
public static class ItemApi
{
    // 인벤토리 조회 비동기 버전 — 보유 아이템 목록 반환
    public static Task<ApiResult<List<PlayerItemDto>>> GetInventoryAsync()
        => ApiClient.Instance.GetListAsync<PlayerItemDto>(ApiConfig.Item.GetInventory);

    // 아이템 사용 비동기 버전 — itemId 경로 바인딩, clientRequestId로 멱등성 보장
    public static Task<ApiResult<EmptyResponse>> UseAsync(int itemId, string clientRequestId)
        => ApiClient.Instance.PostAsync<UseItemRequest, EmptyResponse>(
            string.Format(ApiConfig.Item.Use, itemId),
            new UseItemRequest { clientRequestId = clientRequestId });
}
