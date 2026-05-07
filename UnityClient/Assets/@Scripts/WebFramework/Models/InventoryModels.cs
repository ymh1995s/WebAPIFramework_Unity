using System;

// 플레이어 보유 아이템 단건 DTO — GET /api/items/inventory 응답 항목 단위
[Serializable]
public class PlayerItemDto
{
    // 아이템 고유 ID
    public int    itemId;

    // 아이템 이름 (표시용)
    public string itemName;

    // 아이템 종류 — "currency": 재화, "consumable": 소모품 (백엔드 camelCase 문자열 직렬화)
    public string itemType;

    // 보유 수량
    public int    quantity;
}
