using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// 서버 원격 설정 캐시 매니저 — 부팅 시 1회 페치 후 인메모리 보관
// 호출처는 GetString/GetBool/GetInt/GetFloat 게터로 타입 안전하게 접근
// 페치 실패 시 빈 사전 유지 — 호출처는 defaultValue를 받음
public class RemoteConfigManager : Singleton<RemoteConfigManager>
{
    // 캐시된 키-값 사전 — FetchAsync 성공 시 교체
    private Dictionary<string, string> _values = new Dictionary<string, string>();

    // 마지막 페치 성공 여부 — 디버그 표시 또는 외부 분기용
    public bool HasData { get; private set; }

    // 서버에서 설정값을 받아 캐시 갱신
    // 실패 시 기존 캐시 유지(최초 실패는 빈 사전) — 반환값 true=성공 false=실패
    public async Task<bool> FetchAsync()
    {
        var result = await RemoteConfigApi.GetAsync();
        if (!result.IsSuccess)
        {
            // 페치 실패 — 기존 캐시 유지, 치명적 오류 아님
            Debug.LogWarning($"[RemoteConfigManager] 원격 설정 페치 실패: {result.Error?.ErrorCode}");
            return false;
        }

        // 응답 values가 null이면 빈 사전으로 안전하게 처리
        _values = result.Value?.values ?? new Dictionary<string, string>();
        HasData = true;
        Debug.Log($"[RemoteConfigManager] 원격 설정 로드 완료 — 키 {_values.Count}개");
        return true;
    }

    // string 게터 — 키 부재 또는 페치 미완료 시 defaultValue 반환
    public string GetString(string key, string defaultValue = "")
    {
        return _values.TryGetValue(key, out var val) ? val : defaultValue;
    }

    // bool 게터 — "true"/"false" 소문자 파싱 (백엔드 기본 형식)
    public bool GetBool(string key, bool defaultValue = false)
    {
        if (!_values.TryGetValue(key, out var val)) return defaultValue;
        return bool.TryParse(val, out var parsed) ? parsed : defaultValue;
    }

    // int 게터 — InvariantCulture로 파싱 (로케일 무관 정수 처리)
    public int GetInt(string key, int defaultValue = 0)
    {
        if (!_values.TryGetValue(key, out var val)) return defaultValue;
        return int.TryParse(val, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : defaultValue;
    }

    // float 게터 — InvariantCulture로 파싱 (한국어 로케일 소수점 오류 방지)
    public float GetFloat(string key, float defaultValue = 0f)
    {
        if (!_values.TryGetValue(key, out var val)) return defaultValue;
        return float.TryParse(val, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : defaultValue;
    }

    // 캐시 초기화 — 세션 격리가 필요한 경우에만 사용 (로그아웃 시 기본 호출 안 함)
    public void Clear()
    {
        _values.Clear();
        HasData = false;
    }
}
