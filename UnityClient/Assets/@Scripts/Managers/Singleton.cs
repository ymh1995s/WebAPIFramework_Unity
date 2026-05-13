using UnityEngine;

/// <summary>
/// 앱 종료 가드가 포함된 싱글톤 베이스 클래스.
/// 파괴 순서가 비결정적인 DDOL 오브젝트 간 MissingReferenceException을 방지한다.
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    static T _instance;

    /// <summary>
    /// 앱 종료(에디터 플레이 중지 포함) 여부 플래그.
    /// true 상태에서 Instance 접근 시 새 인스턴스 생성을 차단한다.
    /// </summary>
    static bool _applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            // 앱 종료 중에는 새 인스턴스 생성을 허용하지 않는다.
            if (_applicationIsQuitting)
                return null;

            // Unity == 오버로드가 destroyed object를 null로 판정하므로
            // _instance == null 조건만으로 파괴된 인스턴스도 올바르게 감지한다.
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();

                if (_instance == null)
                {
                    GameObject go = new GameObject($"@{typeof(T).Name}");
                    _instance = go.AddComponent<T>();
                }

                DontDestroyOnLoad(_instance.gameObject);
            }

            return _instance;
        }
    }

    /// <summary>
    /// 앱 종료 또는 에디터 플레이 중지 시 호출된다.
    /// 이후 Instance 접근에서 새 인스턴스가 생성되지 않도록 플래그를 세팅한다.
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    /// <summary>
    /// 오브젝트가 파괴될 때 호출된다.
    /// 자신이 현재 인스턴스인 경우에만 null 처리하여 다음 접근 시 재생성을 허용한다.
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    /// <summary>
    /// Enter Play Mode에서 Domain Reload가 비활성화된 경우,
    /// 정적 필드가 자동으로 초기화되지 않아 두 번째 Play 진입 시
    /// _applicationIsQuitting = true 상태가 유지되어 Instance가 null을 반환하는 문제를 방지한다.
    /// SubsystemRegistration 타이밍은 Domain Reload 없이도 항상 호출되므로 안전하게 리셋할 수 있다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _applicationIsQuitting = false;
        _instance = null;
    }
}
