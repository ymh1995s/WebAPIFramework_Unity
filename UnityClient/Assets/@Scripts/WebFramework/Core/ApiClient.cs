using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// HTTP REST 통신 래퍼 싱글톤
// GET / POST / PUT / DELETE 메서드 제공
// 401 자동 토큰 갱신, 503 점검 인터셉터, 429 자동 백오프 포함
// Task<ApiResult<T>> 반환 오버로드 추가 — 콜백 없이 await로 결과 처리 가능
public class ApiClient : Singleton<ApiClient>
{
    // 요청 타임아웃 — CLIENT_GUIDE 27번 권장 (DB transient retry 최대 50초 고려)
    private const int TimeoutSeconds = 60;

    // 429 Retry-After 헤더 없을 때 사용하는 기본 대기 시간(초)
    private const int DefaultRetryAfterSeconds = 5;

    // ------- 401 자동 갱신 동시성 제어 -------

    // 여러 요청이 동시에 401을 받아도 토큰 갱신을 1회만 수행하도록 직렬화
    private static readonly SemaphoreSlim _refreshSem = new SemaphoreSlim(1, 1);

    // 현재 토큰 갱신 흐름 진행 중 플래그 — refresh 자체가 401 받을 때 무한 루프 차단
    private static bool _inRefreshFlow = false;

    // ------- 503 중복 발행 방지 -------

    // 점검 이벤트를 1회만 발행하는 플래그 (다중 동시 요청 시 이벤트 폭주 방지)
    private static bool _maintenanceShown = false;

    // ====================================================
    // GET (콜백)
    // ====================================================

    // 쿼리 파라미터 없는 GET 요청
    public async void Get<TRes>(string endpoint,
        Action<TRes> onSuccess, Action<ApiError> onError = null)
    {
        await SendAsync<TRes>("GET", ApiConfig.BaseUrl + endpoint, null,
            isRetry: false, customParser: null, onSuccess, onError);
    }

    // 쿼리 파라미터 있는 GET 요청 — 딕셔너리를 URL에 붙여 조립
    public async void GetWithQuery<TRes>(string endpoint, IDictionary<string, string> query,
        Action<TRes> onSuccess, Action<ApiError> onError = null)
    {
        string url = BuildUrl(ApiConfig.BaseUrl + endpoint, query);
        await SendAsync<TRes>("GET", url, null,
            isRetry: false, customParser: null, onSuccess, onError);
    }

    // ====================================================
    // POST (콜백)
    // ====================================================

    // 요청 본문이 있는 POST
    public async void Post<TReq, TRes>(string endpoint, TReq body,
        Action<TRes> onSuccess, Action<ApiError> onError = null)
    {
        string json = JsonUtility.ToJson(body);
        await SendAsync<TRes>("POST", ApiConfig.BaseUrl + endpoint, json,
            isRetry: false, customParser: null, onSuccess, onError);
    }

    // 요청 본문이 없는 POST (예: DailyLogin)
    public async void Post<TRes>(string endpoint,
        Action<TRes> onSuccess, Action<ApiError> onError = null)
    {
        await SendAsync<TRes>("POST", ApiConfig.BaseUrl + endpoint, null,
            isRetry: false, customParser: null, onSuccess, onError);
    }

    // ====================================================
    // PUT (콜백)
    // ====================================================

    // 요청 본문이 있는 PUT
    public async void Put<TReq, TRes>(string endpoint, TReq body,
        Action<TRes> onSuccess, Action<ApiError> onError = null)
    {
        string json = JsonUtility.ToJson(body);
        await SendAsync<TRes>("PUT", ApiConfig.BaseUrl + endpoint, json,
            isRetry: false, customParser: null, onSuccess, onError);
    }

    // ====================================================
    // DELETE (콜백)
    // ====================================================

    // DELETE 요청
    public async void Delete<TRes>(string endpoint,
        Action<TRes> onSuccess, Action<ApiError> onError = null)
    {
        await SendAsync<TRes>("DELETE", ApiConfig.BaseUrl + endpoint, null,
            isRetry: false, customParser: null, onSuccess, onError);
    }

    // ====================================================
    // GET 최상위 배열 응답 전용 (콜백)
    // ====================================================

    // 백엔드가 최상위 JSON 배열([...])을 반환하는 GET 요청 — JsonHelper로 파싱
    public async void GetList<T>(string endpoint,
        Action<List<T>> onSuccess, Action<ApiError> onError = null)
    {
        await SendAsync<List<T>>("GET", ApiConfig.BaseUrl + endpoint, null,
            isRetry: false,
            customParser: body => JsonHelper.FromJsonList<T>(body),
            onSuccess, onError);
    }

    // ====================================================
    // GET (Task 반환)
    // ====================================================

    // 쿼리 파라미터 없는 GET — await로 결과 직접 수신
    public Task<ApiResult<TRes>> GetAsync<TRes>(string endpoint)
        => SendAsync<TRes>("GET", ApiConfig.BaseUrl + endpoint, null,
            isRetry: false, customParser: null, onSuccess: null, onError: null);

    // 쿼리 파라미터 있는 GET — await로 결과 직접 수신
    public Task<ApiResult<TRes>> GetWithQueryAsync<TRes>(string endpoint, IDictionary<string, string> query)
    {
        string url = BuildUrl(ApiConfig.BaseUrl + endpoint, query);
        return SendAsync<TRes>("GET", url, null,
            isRetry: false, customParser: null, onSuccess: null, onError: null);
    }

    // ====================================================
    // POST (Task 반환)
    // ====================================================

    // 요청 본문이 있는 POST — await로 결과 직접 수신
    public Task<ApiResult<TRes>> PostAsync<TReq, TRes>(string endpoint, TReq body)
    {
        string json = JsonUtility.ToJson(body);
        return SendAsync<TRes>("POST", ApiConfig.BaseUrl + endpoint, json,
            isRetry: false, customParser: null, onSuccess: null, onError: null);
    }

    // 요청 본문이 없는 POST — await로 결과 직접 수신
    public Task<ApiResult<TRes>> PostAsync<TRes>(string endpoint)
        => SendAsync<TRes>("POST", ApiConfig.BaseUrl + endpoint, null,
            isRetry: false, customParser: null, onSuccess: null, onError: null);

    // ====================================================
    // PUT (Task 반환)
    // ====================================================

    // 요청 본문이 있는 PUT — await로 결과 직접 수신
    public Task<ApiResult<TRes>> PutAsync<TReq, TRes>(string endpoint, TReq body)
    {
        string json = JsonUtility.ToJson(body);
        return SendAsync<TRes>("PUT", ApiConfig.BaseUrl + endpoint, json,
            isRetry: false, customParser: null, onSuccess: null, onError: null);
    }

    // ====================================================
    // DELETE (Task 반환)
    // ====================================================

    // DELETE — await로 결과 직접 수신
    public Task<ApiResult<TRes>> DeleteAsync<TRes>(string endpoint)
        => SendAsync<TRes>("DELETE", ApiConfig.BaseUrl + endpoint, null,
            isRetry: false, customParser: null, onSuccess: null, onError: null);

    // ====================================================
    // GET 최상위 배열 응답 전용 (Task 반환)
    // ====================================================

    // 최상위 JSON 배열 GET — await로 결과 직접 수신
    public Task<ApiResult<List<T>>> GetListAsync<T>(string endpoint)
        => SendAsync<List<T>>("GET", ApiConfig.BaseUrl + endpoint, null,
            isRetry: false,
            customParser: body => JsonHelper.FromJsonList<T>(body),
            onSuccess: null, onError: null);

    // ====================================================
    // 내부 공통 전송 로직
    // ====================================================

    // 콜백(onSuccess/onError)과 Task<ApiResult<T>> 반환을 하나의 메서드로 통합
    // isRetry: 401 갱신 후 재시도 여부 — true이면 401 발생 시 즉시 실패 반환 (무한 루프 방지)
    // customParser: null이면 JsonUtility.FromJson<TRes> 사용, 지정 시 커스텀 파서로 응답 파싱
    // onSuccess/onError가 모두 null이면 Task 반환 모드로 동작
    private async Task<ApiResult<TRes>> SendAsync<TRes>(
        string method, string url, string jsonBody,
        bool isRetry,
        Func<string, TRes> customParser,
        Action<TRes> onSuccess,
        Action<ApiError> onError)
    {
        RestLogger.Info($"[REQ] {method} {url}");

        using var req = BuildRequest(method, url, jsonBody);
        await req.SendWebRequest();

        long statusCode = req.responseCode;
        string body     = req.downloadHandler?.text ?? string.Empty;

        // 네트워크 오류 (DNS 실패, 타임아웃 등 — HTTP 레벨 이전 실패)
        if (req.result == UnityWebRequest.Result.ConnectionError ||
            req.result == UnityWebRequest.Result.DataProcessingError)
        {
            RestLogger.Error($"[네트워크 오류] {method} {url} ← {req.error}");
            var netErr = ApiError.FromHttp(0, null, isNetwork: true);
            onError?.Invoke(netErr);
            return ApiResult<TRes>.Fail(netErr);
        }

        RestLogger.Info($"[{statusCode}] {method} {url} ← {body}");

        // ---- 상태 코드별 분기 ----

        // 503 점검 인터셉터
        if (statusCode == 503)
        {
            var maintErr = Handle503(onError);
            return ApiResult<TRes>.Fail(maintErr);
        }

        // 429 Rate Limit — 1회 자동 백오프 재시도
        if (statusCode == 429 && !isRetry)
        {
            int waitSec = ParseRetryAfter(req);
            RestLogger.Warn($"[429] Rate Limit — {waitSec}초 대기 후 재시도");
            await Task.Delay(waitSec * 1000);
            // isRetry=true로 재시도 → 429 반복 시 아래 4xx/5xx 분기로 처리됨
            return await SendAsync<TRes>(method, url, jsonBody,
                isRetry: true, customParser, onSuccess, onError);
        }

        // 403 — 밴 계정은 토큰 갱신 대상 아님. ErrorCode 정규화 후 즉시 onError
        if (statusCode == 403)
        {
            var err403 = ApiError.FromHttp(statusCode, body, isNetwork: false);
            // 백엔드가 plain text로 응답해 ErrorCode가 비어있는 케이스 정규화 (GuestLogin 호환)
            if (err403.IsBanned && string.IsNullOrEmpty(err403.ErrorCode))
                err403.ErrorCode = "AUTH_BANNED";

            if (err403.IsBanned)
                RestLogger.Warn($"[403] 밴 계정 감지 — {err403.UserMessage}");

            onError?.Invoke(err403);
            return ApiResult<TRes>.Fail(err403);
        }

        // 401 미인증 — 밴 계정이면 갱신 없이 즉시 에러 반환, 그 외는 토큰 갱신 후 재시도
        if (statusCode == 401)
        {
            var err401 = ApiError.FromHttp(statusCode, body, isNetwork: false);
            if (err401.ErrorCode == "AUTH_BANNED")
            {
                // 밴 계정은 토큰 갱신 루프 진입 없이 onError 직행
                onError?.Invoke(err401);
                return ApiResult<TRes>.Fail(err401);
            }
            return await Handle401<TRes>(method, url, jsonBody, isRetry, customParser, onSuccess, onError);
        }

        // 200~299 성공 범위
        if (statusCode >= 200 && statusCode < 300)
        {
            // 본문이 비어있으면 default(TRes) 반환 (EmptyResponse 용도)
            if (string.IsNullOrEmpty(body))
            {
                onSuccess?.Invoke(default(TRes));
                return ApiResult<TRes>.Ok(default(TRes));
            }

            try
            {
                // customParser가 지정된 경우 사용 (예: 최상위 배열 응답), 없으면 기본 JsonUtility 사용
                TRes result = customParser != null ? customParser(body) : JsonUtility.FromJson<TRes>(body);
                onSuccess?.Invoke(result);
                return ApiResult<TRes>.Ok(result);
            }
            catch (Exception ex)
            {
                RestLogger.Error($"JSON 파싱 실패: {ex.Message} | body: {body}");
                var parseErr = ApiError.FromHttp(statusCode, body, isNetwork: false);
                onError?.Invoke(parseErr);
                return ApiResult<TRes>.Fail(parseErr);
            }
        }

        // 그 외 4xx / 5xx 오류
        var httpErr = ApiError.FromHttp(statusCode, body, isNetwork: false);
        onError?.Invoke(httpErr);
        return ApiResult<TRes>.Fail(httpErr);
    }

    // ====================================================
    // 401 자동 갱신 흐름
    // ====================================================

    // refresh 요청 자체가 401을 받은 경우 무한 루프 방지 — 즉시 onError + 세션 만료 이벤트
    private async Task<ApiResult<TRes>> Handle401<TRes>(
        string method, string url, string jsonBody,
        bool isRetry,
        Func<string, TRes> customParser,
        Action<TRes> onSuccess,
        Action<ApiError> onError)
    {
        // 재시도 요청이 또 401을 받았거나, refresh 흐름 중에 401이 발생한 경우
        if (isRetry || _inRefreshFlow)
        {
            RestLogger.Error("[401] 토큰 갱신 실패 — 세션 만료");
            AuthManager.Instance.Clear();
            EventManager.Instance.TriggerEvent(Define.EEventType.SessionExpired);
            var expiredErr = ApiError.FromHttp(401, null, isNetwork: false);
            onError?.Invoke(expiredErr);
            return ApiResult<TRes>.Fail(expiredErr);
        }

        // SemaphoreSlim으로 동시 갱신 요청 직렬화 — 첫 번째 요청만 실제 갱신 수행
        await _refreshSem.WaitAsync();
        bool refreshSucceeded = true;
        try
        {
            // 세마포어 획득 시점에 _inRefreshFlow가 false이면 이 요청이 갱신 담당
            // true이면 다른 요청이 이미 갱신 중 → 대기 후 새 토큰으로 재시도만 수행
            if (!_inRefreshFlow)
            {
                // 최초 진입자: 실제 RefreshToken 갱신 수행
                _inRefreshFlow = true;
                refreshSucceeded = await DoRefresh();
                _inRefreshFlow = false;
            }
            // 후속 진입자: 갱신 완료 후 세마포어 획득 → 새 토큰으로 재시도 (갱신 재수행 안 함)
        }
        finally
        {
            _refreshSem.Release();
        }

        if (!refreshSucceeded)
        {
            // 갱신 실패 — 세션 만료 처리
            AuthManager.Instance.Clear();
            EventManager.Instance.TriggerEvent(Define.EEventType.SessionExpired);
            var failErr = ApiError.FromHttp(401, null, isNetwork: false);
            onError?.Invoke(failErr);
            return ApiResult<TRes>.Fail(failErr);
        }

        // 새 토큰으로 원 요청 1회 재시도 (isRetry=true — 재시도 중 401이면 즉시 종료)
        // customParser를 그대로 전달하여 재시도 시에도 동일한 파싱 방식 유지
        return await SendAsync<TRes>(method, url, jsonBody,
            isRetry: true, customParser, onSuccess, onError);
    }

    // 실제 RefreshToken 갱신 HTTP 요청 수행 — ApiClient.Post를 우회하여 인터셉터 중복 방지
    private async Task<bool> DoRefresh()
    {
        string refreshToken = AuthManager.Instance.RefreshToken;
        if (string.IsNullOrEmpty(refreshToken))
        {
            RestLogger.Warn("[Refresh] RefreshToken 없음 — 갱신 생략");
            return false;
        }

        string url      = ApiConfig.BaseUrl + ApiConfig.Auth.Refresh;
        var    body     = new RefreshTokenRequest { refreshToken = refreshToken };
        string jsonBody = JsonUtility.ToJson(body);

        RestLogger.Info($"[REQ] POST {ApiConfig.Auth.Refresh} (토큰 갱신)");

        using var req = BuildRequest("POST", url, jsonBody);
        await req.SendWebRequest();

        long statusCode = req.responseCode;
        string respBody = req.downloadHandler?.text ?? string.Empty;

        RestLogger.Info($"[{statusCode}] POST {ApiConfig.Auth.Refresh} ← {respBody}");

        if (statusCode >= 200 && statusCode < 300 && !string.IsNullOrEmpty(respBody))
        {
            try
            {
                var tokenResp = JsonUtility.FromJson<TokenResponse>(respBody);
                AuthManager.Instance.SaveToken(tokenResp);
                RestLogger.Info("[Refresh] 토큰 갱신 성공");
                return true;
            }
            catch (Exception ex)
            {
                RestLogger.Error($"[Refresh] 응답 파싱 실패: {ex.Message}");
                return false;
            }
        }

        RestLogger.Warn($"[Refresh] 갱신 실패 — 상태: {statusCode}");
        return false;
    }

    // ====================================================
    // 503 점검 인터셉터
    // ====================================================

    // 점검 응답 처리 — 최초 1회만 이벤트 발행하여 팝업 폭주 방지, ApiError 반환
    private ApiError Handle503(Action<ApiError> onError)
    {
        RestLogger.Error("[503] 서버 점검 감지");

        if (!_maintenanceShown)
        {
            _maintenanceShown = true;
            EventManager.Instance.TriggerEvent(Define.EEventType.MaintenanceDetected);
        }

        onError?.Invoke(ApiError.Maintenance);
        return ApiError.Maintenance;
    }

    // 점검 플래그 초기화 — 씬 재진입 또는 부팅 흐름 재실행 시 호출
    // 점검 해제 후 재점검 시 MaintenanceDetected 이벤트가 재발행될 수 있도록 리셋
    public static void ResetMaintenanceFlag()
    {
        _maintenanceShown = false;
    }

    // ====================================================
    // 유틸리티
    // ====================================================

    // UnityWebRequest 생성 — 공통 헤더(Authorization, Content-Type) 설정
    private UnityWebRequest BuildRequest(string method, string url, string jsonBody)
    {
        var req = new UnityWebRequest(url, method);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout         = TimeoutSeconds;

        // JWT 자동 첨부 — AccessToken이 있으면 Authorization 헤더 추가
        string token = AuthManager.Instance.AccessToken;
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", $"Bearer {token}");

        // 요청 본문이 있는 경우 JSON 본문과 Content-Type 설정
        if (!string.IsNullOrEmpty(jsonBody))
        {
            byte[] bytes         = Encoding.UTF8.GetBytes(jsonBody);
            req.uploadHandler    = new UploadHandlerRaw(bytes);
            req.SetRequestHeader("Content-Type", "application/json");
        }

        return req;
    }

    // 쿼리 파라미터 딕셔너리를 URL에 붙여 반환
    private string BuildUrl(string baseUrl, IDictionary<string, string> query)
    {
        if (query == null || query.Count == 0)
            return baseUrl;

        var sb = new StringBuilder(baseUrl);
        sb.Append('?');

        bool first = true;
        foreach (var kv in query)
        {
            if (!first) sb.Append('&');
            sb.Append(Uri.EscapeDataString(kv.Key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(kv.Value));
            first = false;
        }

        return sb.ToString();
    }

    // Retry-After 헤더를 파싱하여 대기 초 반환 — 없거나 파싱 실패 시 기본값 반환
    private int ParseRetryAfter(UnityWebRequest req)
    {
        string header = req.GetResponseHeader("Retry-After");
        if (!string.IsNullOrEmpty(header) && int.TryParse(header, out int seconds))
            return seconds;

        return DefaultRetryAfterSeconds;
    }
}
