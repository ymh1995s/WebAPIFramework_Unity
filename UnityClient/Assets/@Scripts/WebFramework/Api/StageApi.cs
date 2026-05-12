using System.Collections.Generic;
using System.Threading.Tasks;

// 스테이지 API — 마스터 데이터 조회, 진행 현황 조회, 클리어 결과 전송
public static class StageApi
{
    // 스테이지 마스터 데이터 조회 비동기 버전 — 전체 스테이지 목록 반환
    public static Task<ApiResult<List<StageMasterDto>>> GetMastersAsync()
        => ApiClient.Instance.GetListAsync<StageMasterDto>(ApiConfig.Stage.List);

    // 스테이지 진행 현황 조회 비동기 버전 — 플레이어의 클리어 이력 반환
    public static Task<ApiResult<List<StageProgressDto>>> GetProgressAsync()
        => ApiClient.Instance.GetListAsync<StageProgressDto>(ApiConfig.Stage.Progress);

    // 스테이지 클리어 결과 전송 비동기 버전 — stageId를 경로에 바인딩하여 POST
    public static Task<ApiResult<StageClearResponse>> CompleteAsync(int stageId, StageClearRequest body)
        => ApiClient.Instance.PostAsync<StageClearRequest, StageClearResponse>(
            string.Format(ApiConfig.Stage.Complete, stageId), body);
}
