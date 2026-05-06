using System;

// 일일 출석 처리 응답 DTO — POST /api/dailylogin 응답
[Serializable]
public class DailyLoginResponse
{
    // 보상 지급 여부 — false이면 오늘 이미 출석 처리된 상태
    public bool rewarded;
}
