// 토큰 제공자 인터페이스 — ApiClient가 AuthManager를 직접 참조하지 않도록 추상화
// ApiClient는 이 인터페이스에만 의존하며, 실제 구현체(AuthManager)는 런타임에 주입된다
public interface ITokenProvider
{
    // 현재 유효한 JWT Access Token — API 요청 헤더에 첨부
    string AccessToken { get; }

    // 자동 로그인 및 토큰 갱신에 사용하는 Refresh Token
    string RefreshToken { get; }

    // 로그인/갱신 성공 시 서버 응답 토큰을 저장
    void SaveToken(TokenResponse response);

    // 로그아웃 또는 세션 만료 시 모든 토큰 초기화
    void Clear();
}
