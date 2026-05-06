using System;

// 메일 단건 DTO — 메일함 목록 응답의 항목 단위
[Serializable]
public class MailDto
{
    // 메일 고유 ID
    public int    id;

    // 수신자 플레이어 내부 정수 ID (백엔드 MailDto.PlayerId는 int)
    public int    playerId;

    // 메일 제목
    public string title;

    // 메일 본문
    public string body;

    // 읽음 여부
    public bool   isRead;

    // 보상 수령 여부
    public bool   isClaimed;

    // 생성 일시 (ISO 8601 문자열)
    public string createdAt;

    // 만료 일시 (ISO 8601 문자열)
    public string expiresAt;
}

