using System;

// IAP 영수증 검증 요청 DTO — POST /api/iap/google/verify
[Serializable]
public class IapVerifyRequest
{
    // 구글 플레이 상품 ID (예: com.rookiss.s2.noads)
    public string productId;
    // 구글 결제 토큰 — 서버 멱등 키로 사용 (동일 토큰 재호출 안전)
    public string purchaseToken;
    // 구글 주문 ID (GPA.xxx 형식, 없으면 빈 문자열)
    public string orderId;
}

// IAP 영수증 검증 응답 DTO
[Serializable]
public class IapVerifyResponse
{
    // 검증 성공 여부 — false이면 서버가 영수증을 유효하지 않다고 판단
    public bool ok;
    // 이미 지급된 결제 여부 — 재시도/복원 시 true, 이 경우도 ok=true
    public bool alreadyGranted;
    // 서버 측 구매 레코드 ID — 고객센터 문의 시 참조용
    public long purchaseId;
}
