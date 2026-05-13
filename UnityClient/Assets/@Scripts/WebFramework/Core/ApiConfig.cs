// API 서버 설정 - 서버 주소 및 엔드포인트 경로 상수 정의
public static class ApiConfig
{
    // 개발 서버 기본 URL (배포 시 실제 주소로 변경)
    public const string BaseUrl = "http://localhost:5058";

    // 인증 관련 엔드포인트 (/api prefix 포함 — 이전 버전의 prefix 누락 버그 수정)
    public static class Auth
    {
        public const string Guest         = "/api/auth/guest";
        public const string Google        = "/api/auth/google";
        public const string Refresh       = "/api/auth/refresh";
        public const string Logout        = "/api/auth/logout";
        public const string LinkGoogle    = "/api/auth/link/google";       // 게스트 → 구글 연동
        public const string ResolveGoogle = "/api/auth/google/resolve-conflict"; // 계정 충돌 해소
        public const string Withdraw      = "/api/auth/withdraw";          // 회원 탈퇴
    }

    // 버전 체크 엔드포인트
    public static class Version
    {
        public const string Check = "/api/version/check"; // ?version={ver}
    }

    // 공지사항 엔드포인트
    public static class Notice
    {
        public const string Latest = "/api/notices/latest";
    }

    // 메일함 엔드포인트
    public static class Mail
    {
        public const string List  = "/api/mails";
        public const string Claim = "/api/mails/{0}/claim"; // {0} = mailId
    }

    // 스테이지 엔드포인트
    public static class Stage
    {
        public const string List     = "/api/stages";
        public const string Progress = "/api/stages/progress";
        public const string Complete = "/api/stages/{0}/complete"; // {0} = stageId
    }

    // 랭킹 엔드포인트
    public static class Ranking
    {
        public const string Me = "/api/ranking/me";
    }

    // 문의 엔드포인트
    public static class Inquiry
    {
        public const string Submit = "/api/inquiries"; // POST
        public const string List   = "/api/inquiries"; // GET
    }

    // 일일 출석 엔드포인트
    public static class DailyLogin
    {
        public const string Process = "/api/dailylogin"; // POST (본문 없음)
    }

    // 외침(월드 채팅) 엔드포인트
    public static class Shout
    {
        public const string Active = "/api/shouts/active";
    }

    // 아이템 엔드포인트 — 인벤토리 조회 및 아이템 사용 포함
    public static class Item
    {
        // 인벤토리 조회 — GET /api/items/inventory (백엔드가 최상위 배열 반환)
        public const string GetInventory = "/api/items/inventory";

        // 아이템 사용 — POST /api/items/{0}/use ({0} = itemId)
        public const string Use = "/api/items/{0}/use";
    }

    // 상점 엔드포인트 — N개 구매 지원 (백엔드 ShopBuyRequest.Quantity 필드)
    public static class Shop
    {
        // 활성 상품 목록 — GET /api/shop
        public const string List = "/api/shop";
        // 상품 구매 — POST /api/shop/{0}/buy ({0} = productId)
        public const string Buy  = "/api/shop/{0}/buy";
    }

#if DEBUG
    // 디버그 전용 엔드포인트 — 개발/QA 빌드 한정. 릴리즈에서 컴파일 제외됨
    public static class Debug
    {
        // 보상 즉시 지급 — POST /api/debug/grant
        public const string Grant = "/api/debug/grant";
    }
#endif
}
