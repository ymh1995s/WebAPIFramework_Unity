using System;
using System.Collections.Generic;

// 아이템 API 싱글톤 — 인벤토리 조회 및 아이템 사용 요청을 처리한다
public class ItemApi : Singleton<ItemApi>
{
    // 인벤토리 조회 — GET /api/items/inventory (백엔드가 최상위 배열 반환)
    // onSuccess: 보유 아이템 목록 반환
    // onError  : 4xx/5xx 오류 시 호출
    public void GetInventory(Action<List<PlayerItemDto>> onSuccess, Action<ApiError> onError = null)
    {
        ApiClient.Instance.GetList<PlayerItemDto>(ApiConfig.Item.GetInventory, onSuccess, onError);
    }

    // 아이템 사용 — POST /api/items/{itemId}/use
    // itemId         : 사용할 아이템 ID
    // clientRequestId: 멱등성 키 (Guid.NewGuid().ToString() 으로 생성)
    // onSuccess      : 200 성공 시 호출 (응답 본문 없음 — EmptyResponse)
    // onError        : 4xx/5xx 오류 시 호출
    public void Use(int itemId, string clientRequestId,
        Action<EmptyResponse> onSuccess, Action<ApiError> onError = null)
    {
        // 엔드포인트에 itemId 삽입 — string.Format 으로 {0} 치환
        string endpoint = string.Format(ApiConfig.Item.Use, itemId);

        ApiClient.Instance.Post<UseItemRequest, EmptyResponse>(
            endpoint,
            new UseItemRequest { clientRequestId = clientRequestId },
            onSuccess,
            onError
        );
    }
}
