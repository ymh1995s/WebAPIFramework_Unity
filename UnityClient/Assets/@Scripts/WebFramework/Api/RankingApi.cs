using System.Threading.Tasks;

// 랭킹 API — 내 랭킹 조회
public static class RankingApi
{
    // 내 랭킹 조회 비동기 버전 — 현재 로그인된 플레이어의 랭킹 정보 반환
    public static Task<ApiResult<MyRankResponse>> GetMyRankAsync()
        => ApiClient.Instance.GetAsync<MyRankResponse>(ApiConfig.Ranking.Me);
}
