using System;
using System.Collections.Generic;

// 서버 원격 설정 응답 DTO — GET /api/remoteconfig 응답 본문
// values: 키-값 사전 (값은 항상 string — 타입 변환은 RemoteConfigManager 게터 책임)
// JsonUtility는 Dictionary를 지원하지 않으므로 Newtonsoft.Json으로 역직렬화
[Serializable]
public class RemoteConfigResponse
{
    // 백엔드 camelCase 정책 대응 — 와이어상 "values" 키와 일치
    public Dictionary<string, string> values;
}
