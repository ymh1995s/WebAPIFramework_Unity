using System.Collections.Generic;
using UnityEngine;

// JsonUtility의 최상위 배열 미지원 한계를 우회하는 보조 유틸리티
// 백엔드가 배열을 직접 반환하는 경우 {"items":[...]} 형태로 감싸 파싱한다
public static class JsonHelper
{
    // 최상위 JSON 배열을 List<T>로 변환
    // 예: "[{\"id\":1},{\"id\":2}]" → List<T> {2개}
    public static List<T> FromJsonList<T>(string json)
    {
        // JsonUtility가 최상위 배열을 파싱하지 못하므로 래퍼 객체로 감싼다
        string wrapped = $"{{\"items\":{json}}}";
        var envelope   = JsonUtility.FromJson<ListEnvelope<T>>(wrapped);
        return envelope?.items ?? new List<T>();
    }

    // FromJsonList 내부에서 사용하는 배열 래퍼 클래스
    [System.Serializable]
    private class ListEnvelope<T>
    {
        // "items" 키로 감싼 배열 — FromJsonList의 wrapped 문자열과 키 이름 일치해야 함
        public List<T> items;
    }
}
