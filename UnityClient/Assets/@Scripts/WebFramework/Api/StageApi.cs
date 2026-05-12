using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// 스테이지 API 싱글톤 — 마스터 데이터 조회, 진행 현황 조회, 클리어 결과 전송
public class StageApi : Singleton<StageApi>
{
    // ── Task 기반 비동기 정적 메서드 (await 호출용) ──────────────────────────

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


    // 스테이지 마스터 데이터 전체 조회 — GET /api/stages (백엔드가 최상위 배열 반환)
    public void GetMasters(
        Action<List<StageMasterDto>> onSuccess, Action<ApiError> onError = null)
    {
        ApiClient.Instance.GetList<StageMasterDto>(
            ApiConfig.Stage.List, onSuccess, onError);
    }

    // 플레이어의 스테이지 진행 현황 조회 — GET /api/stages/progress (백엔드가 최상위 배열 반환)
    public void GetProgress(
        Action<List<StageProgressDto>> onSuccess, Action<ApiError> onError = null)
    {
        ApiClient.Instance.GetList<StageProgressDto>(
            ApiConfig.Stage.Progress, onSuccess, onError);
    }

    // 스테이지 클리어 결과 전송 — POST /api/stages/{stageId}/complete
    public void Complete(int stageId, StageClearRequest body,
        Action<StageClearResponse> onSuccess, Action<ApiError> onError = null)
    {
        // stageId를 URL 경로에 바인딩
        string endpoint = string.Format(ApiConfig.Stage.Complete, stageId);
        ApiClient.Instance.Post<StageClearRequest, StageClearResponse>(
            endpoint, body, onSuccess, onError);
    }
}
