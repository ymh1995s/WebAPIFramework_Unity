#if DEBUG
using System;

// 디버그 보상 — 아이템 단건 지급 정보
[Serializable]
public class DebugGrantItem
{
    // 지급할 아이템 ID
    public int itemId;
    // 지급 수량
    public int quantity;
}

// 디버그 보상 요청 — POST /api/debug/grant
// playerId 필드는 의도적으로 생략 (백엔드가 키 누락=null=본인 처리)
[Serializable]
public class DebugGrantRequest
{
    // 지급할 경험치 — 0 = 미지급
    public int             exp;
    // 지급할 아이템 목록
    public DebugGrantItem[] items;
    // 보상 출처 식별 키 (로그/추적용)
    public string          sourceKey;
}

// 디버그 보상 응답
[Serializable]
public class DebugGrantResponse
{
    // 결과 메시지
    public string message;
    // 요청 시 전달한 출처 키 (에코)
    public string sourceKey;
}
#endif
