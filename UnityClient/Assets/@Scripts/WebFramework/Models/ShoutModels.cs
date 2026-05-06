using System;

// 외침(월드 메시지) 단건 DTO — 활성 외침 목록의 항목 단위
[Serializable]
public class ShoutDto
{
    // 외침 고유 ID
    public int    id;

    // 외침 메시지 내용
    public string message;

    // 생성 일시 (ISO 8601 문자열)
    public string createdAt;

    // 만료 일시 (ISO 8601 문자열)
    public string expiresAt;
}

