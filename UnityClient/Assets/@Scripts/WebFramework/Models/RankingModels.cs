using System;

// 내 랭킹 응답 DTO — GET /api/ranking/me 응답
[Serializable]
public class MyRankResponse
{
    // 현재 순위
    public int    rank;

    // 플레이어 PublicId (Guid 문자열 — 내부 int Id 아님)
    public string playerId;

    // 플레이어 닉네임
    public string nickname;

    // 최고 점수
    public int    bestScore;
}
