using System;

// 응답 본문이 없는 API 호출(204 / 빈 200)에 사용하는 빈 응답 모델
// ApiClient의 제네릭 메서드가 void를 반환할 수 없으므로 자리 표시자로 활용
[Serializable]
public class EmptyResponse
{
    // 의도적으로 필드 없음 — 본문 없는 응답을 타입 안전하게 처리하기 위한 마커 클래스
}
