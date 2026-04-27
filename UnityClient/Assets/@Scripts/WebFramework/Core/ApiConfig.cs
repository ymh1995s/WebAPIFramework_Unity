// API 서버 설정 - 서버 주소 및 엔드포인트 경로 상수 정의
public static class ApiConfig
{
    // 개발 서버 기본 URL (배포 시 실제 주소로 변경)
    public const string BaseUrl = "http://localhost:5058";

    // 인증 관련 엔드포인트
    public static class Auth
    {
        public const string Guest   = "/auth/guest";
        public const string Refresh = "/auth/refresh";
        public const string Logout  = "/auth/logout";
        public const string Google  = "/auth/google";
    }
}
