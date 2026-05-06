using System;

// 문의 제출 요청 DTO — POST /api/inquiries 요청 본문
[Serializable]
public class InquirySubmitRequest
{
    // 문의 내용 본문
    public string content;
}

// 문의 단건 DTO — 문의 목록 응답의 항목 단위
[Serializable]
public class InquiryDto
{
    // 문의 고유 ID
    public int    id;

    // 문의 내용 본문
    public string content;

    // 관리자 답변 (미답변이면 null → JsonUtility가 빈 문자열로 처리)
    public string adminReply;

    // 답변 일시 (ISO 8601 문자열, 미답변이면 빈 문자열)
    public string repliedAt;

    // 문의 생성 일시 (ISO 8601 문자열)
    public string createdAt;
}

