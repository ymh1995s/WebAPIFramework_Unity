using System;
using UnityEngine;

// API 오류 정보를 담는 데이터 클래스 - 백엔드 ProblemDetails 구조에 대응
[Serializable]
public class ApiError
{
    // HTTP 상태 코드 (0 = 네트워크 오류)
    public long   Status;

    // 백엔드가 반환하는 에러 식별자 (예: "GOOGLE_ACCOUNT_CONFLICT")
    public string ErrorCode;

    // 오류 제목 (ProblemDetails.title)
    public string Title;

    // 오류 상세 메시지 (ProblemDetails.detail)
    public string Detail;

    // 서버 추적 ID (ProblemDetails.traceId)
    public string TraceId;

    // 파싱 실패 시 보관하는 원본 응답 본문
    public string RawBody;

    // 네트워크 연결 실패 여부 (DNS/타임아웃 등)
    public bool   IsNetworkError;

    // 사용자에게 표시할 메시지 - Detail → Title → 상황별 기본값 순으로 선택
    public string UserMessage
    {
        get
        {
            if (!string.IsNullOrEmpty(Detail))    return Detail;
            if (!string.IsNullOrEmpty(Title))     return Title;
            if (IsNetworkError)                   return "네트워크 오류가 발생했습니다.";
            return $"요청 처리 중 오류 ({Status})";
        }
    }

    // HTTP 응답에서 ApiError 생성 - ProblemDetails JSON 파싱 시도, 실패 시 RawBody만 채움
    public static ApiError FromHttp(long statusCode, string body, bool isNetwork)
    {
        var error = new ApiError
        {
            Status        = statusCode,
            IsNetworkError = isNetwork,
            RawBody       = body,
        };

        // 본문이 없거나 네트워크 오류이면 파싱 생략
        if (string.IsNullOrEmpty(body))
            return error;

        // ProblemDetails 형식 파싱 시도 (JsonUtility 사용)
        try
        {
            var parsed = JsonUtility.FromJson<ProblemDetailsDto>(body);
            if (parsed != null && !string.IsNullOrEmpty(parsed.title))
            {
                error.ErrorCode = parsed.errorCode;
                error.Title     = parsed.title;
                error.Detail    = parsed.detail;
                error.TraceId   = parsed.traceId;
            }
        }
        catch
        {
            // 파싱 실패는 무시 - RawBody만 보관
        }

        // ProblemDetails 파싱 후 title이 없으면 JSON 문자열 응답 추출 시도
        // 예: StatusCode(403, "정지된 계정입니다.") 처럼 단순 문자열을 반환하는 경우
        if (string.IsNullOrEmpty(error.Title) && string.IsNullOrEmpty(error.Detail))
        {
            string trimmed = body.Trim();
            if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"')
                error.Detail = trimmed.Substring(1, trimmed.Length - 2);
        }

        return error;
    }

    // 503 점검 모드 전용 싱글턴 오류 객체
    public static readonly ApiError Maintenance = new ApiError
    {
        Status = 503,
        Title  = "서버 점검 중",
        Detail = "서버 점검 중입니다. 잠시 후 다시 시도해주세요.",
    };

    // ProblemDetails JSON 파싱용 내부 DTO (JsonUtility 대응 camelCase 필드)
    [Serializable]
    private class ProblemDetailsDto
    {
        public string errorCode;
        public string title;
        public string detail;
        public string traceId;
    }
}
