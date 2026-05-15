using System.Threading.Tasks;

// IAP 영수증 검증 API — 구글 플레이 영수증을 서버에 전달하여 유효성 검증 및 보상 지급
// 멱등 설계: 동일 purchaseToken 재호출 시 서버가 alreadyGranted=true로 안전하게 응답
public static class IapApi
{
    // 구글 플레이 영수증 서버 검증
    // productId      : 구글 플레이 상품 ID (예: com.rookiss.s2.noads)
    // purchaseToken  : 구글 결제 토큰 — 서버 멱등 키 (동일 토큰 재호출 안전)
    // orderId        : 구글 주문 ID (GPA.xxx 형식, 없으면 빈 문자열 전달)
    public static Task<ApiResult<IapVerifyResponse>> VerifyGoogleAsync(
        string productId, string purchaseToken, string orderId)
    {
        // 검증 요청 DTO 생성 후 POST 전송
        var request = new IapVerifyRequest
        {
            productId     = productId,
            purchaseToken = purchaseToken,
            orderId       = orderId ?? string.Empty,
        };
        return ApiClient.Instance.PostAsync<IapVerifyRequest, IapVerifyResponse>(
            ApiConfig.Iap.VerifyGoogle, request);
    }
}
