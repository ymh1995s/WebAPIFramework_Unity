using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// HTTP REST 통신 래퍼 싱글톤 - POST/GET 공통 처리 및 로그 출력
public class ApiClient : Singleton<ApiClient>
{
    // JSON POST 요청 - async/await 방식 (Unity 6 Awaitable 기반)
    public async void Post<TReq, TRes>(string endpoint, TReq body,
        Action<TRes> onSuccess, Action<string> onError = null)
    {
        string url   = ApiConfig.BaseUrl + endpoint;
        string json  = JsonUtility.ToJson(body);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        RestLogger.Info($"[REQ] POST {endpoint}");

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        // AccessToken이 있으면 Authorization 헤더 자동 첨부
        string token = AuthManager.Instance.AccessToken;
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", $"Bearer {token}");

        // UnityWebRequestAsyncOperation은 Awaitable — 메인 스레드 유지
        await req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            RestLogger.Info($"[{req.responseCode}] POST {endpoint} ← {req.downloadHandler.text}");
            TRes result = JsonUtility.FromJson<TRes>(req.downloadHandler.text);
            onSuccess?.Invoke(result);
        }
        else
        {
            RestLogger.Error($"[{req.responseCode}] POST {endpoint} ← {req.error}");
            onError?.Invoke(req.error);
        }
    }
}
