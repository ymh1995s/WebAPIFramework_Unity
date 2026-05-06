using System;

// 공지사항 단건 DTO — GET /api/notices/latest 응답
[Serializable]
public class NoticeDto
{
    // 공지사항 고유 ID
    public int    id;

    // 공지사항 본문 내용 — 백엔드 NoticeDto는 { id, content } 만 반환 (title 없음)
    public string content;
}
