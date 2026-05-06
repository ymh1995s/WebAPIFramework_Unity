using System;

// 랭킹 API 싱글톤 — 내 랭킹 조회
public class RankingApi : Singleton<RankingApi>
{
    // 현재 로그인된 플레이어의 랭킹 정보 조회 — GET /api/ranking/me
    public void GetMyRank(
        Action<MyRankResponse> onSuccess, Action<ApiError> onError = null)
    {
        ApiClient.Instance.Get<MyRankResponse>(
            ApiConfig.Ranking.Me, onSuccess, onError);
    }
}
