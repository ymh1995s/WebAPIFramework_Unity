using System.Collections.Generic;
using System.Threading.Tasks;

// 상점 API — 상품 목록 조회 및 N개 구매 요청을 처리한다
public static class ShopApi
{
    // 활성 상품 목록 조회 — GET /api/shop, 백엔드가 최상위 배열 반환
    public static Task<ApiResult<List<ShopProductDto>>> GetListAsync()
        => ApiClient.Instance.GetListAsync<ShopProductDto>(ApiConfig.Shop.List);

    // 상품 구매 — POST /api/shop/{productId}/buy
    // clientRequestId: 멱등성 키, quantity: 1 이상
    public static Task<ApiResult<EmptyResponse>> BuyAsync(int productId, string clientRequestId, int quantity)
        => ApiClient.Instance.PostAsync<ShopBuyRequest, EmptyResponse>(
            string.Format(ApiConfig.Shop.Buy, productId),
            new ShopBuyRequest { clientRequestId = clientRequestId, quantity = quantity });
}
