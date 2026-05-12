using System;
using System.Threading.Tasks;

// 랭킹 API 싱글톤 — 내 랭킹 조회
public class RankingApi : Singleton<RankingApi>
{
    // ── Task 기반 비동기 정적 메서드 (await 호출용) ──────────────────────────

    // 내 랭킹 조회 비동기 버전 — 현재 로그인된 플레이어의 랭킹 정보 반환
    public static Task<ApiResult<MyRankResponse>> GetMyRankAsync()
        => ApiClient.Instance.GetAsync<MyRankResponse>(ApiConfig.Ranking.Me);


    // 현재 로그인된 플레이어의 랭킹 정보 조회 — GET /api/ranking/me
    public void GetMyRank(
        Action<MyRankResponse> onSuccess, Action<ApiError> onError = null)
    {
        ApiClient.Instance.Get<MyRankResponse>(
            ApiConfig.Ranking.Me, onSuccess, onError);
    }
}
