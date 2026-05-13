using System;

// 상점 상품 DTO — GET /api/shop 응답 항목
[Serializable]
public class ShopProductDto
{
    // 상품 고유 ID
    public int    id;
    // 상품 이름
    public string name;
    // 상품 설명
    public string description;
    // 가격 아이템 ID (재화 종류)
    public int    priceItemId;
    // 가격 아이템 이름 (표시용)
    public string priceItemName;
    // 가격 수량
    public int    priceAmount;
    // 보상 테이블 ID
    public int    rewardTableId;
    // 일일 구매 한도 — 0 = 무제한
    public int    dailyLimit;
    // 전체 구매 한도 — 0 = 무제한
    public int    totalLimit;
    // 판매 활성화 여부
    public bool   isEnabled;
    // 정렬 순서 (오름차순)
    public int    sortOrder;
    // 생성일시 문자열
    public string createdAt;
    // 갱신일시 문자열
    public string updatedAt;
    // 1회 최대 구매 수량 — 0/음수 = 제한 없음 (JsonUtility int? 미지원 우회)
    public int    maxPerCall;
}

// 상점 구매 요청 — POST /api/shop/{productId}/buy
[Serializable]
public class ShopBuyRequest
{
    // 멱등성 키 — 요청마다 새 Guid 사용
    public string clientRequestId;
    // 구매 수량 — 1 이상
    public int    quantity;
}

// LimitWouldExceed 응답 ProblemDetails 확장필드 재파싱용 보조 DTO
// ApiError.RawBody를 JsonUtility.FromJson 으로 재파싱하여 잔여 수량 추출
[Serializable]
public class ShopLimitProblem
{
    // 구매 가능한 잔여 수량
    public int remainingQuantity;
}
