// API 호출 결과를 담는 값 타입 — 성공/실패 양쪽을 하나의 반환값으로 표현
// Task 반환 메서드에서 onSuccess/onError 콜백 없이 결과를 직접 처리할 때 사용
public readonly struct ApiResult<T>
{
    // 성공 여부 — true이면 Value에 유효한 데이터, false이면 Error에 오류 정보
    public bool IsSuccess { get; }

    // 성공 시 역직렬화된 응답 데이터 (실패 시 default(T))
    public T Value { get; }

    // 실패 시 오류 정보 (성공 시 null)
    public ApiError Error { get; }

    // 성공 결과 생성자
    private ApiResult(T value)
    {
        IsSuccess = true;
        Value     = value;
        Error     = null;
    }

    // 실패 결과 생성자
    private ApiResult(ApiError error)
    {
        IsSuccess = false;
        Value     = default;
        Error     = error;
    }

    // 성공 인스턴스 팩토리
    public static ApiResult<T> Ok(T value)       => new ApiResult<T>(value);

    // 실패 인스턴스 팩토리
    public static ApiResult<T> Fail(ApiError err) => new ApiResult<T>(err);
}
