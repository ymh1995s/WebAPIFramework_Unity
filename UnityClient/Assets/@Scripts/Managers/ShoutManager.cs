using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 활성 외침(Shout) 목록 폴링 매니저 — Singleton.
/// 메인 씬 진입 시 Begin(), 이탈 시 End()를 호출한다.
/// 5분(300초) 간격으로 ShoutApi를 폴링하여 HUD에 전달한다.
/// HUD가 1회 순회를 완료하면 OnHudCompleted를 통해 본 외침 id를 PlayerPrefs에 저장한다.
/// </summary>
public class ShoutManager : Singleton<ShoutManager>
{
    // 폴링 주기 (초) — 5분
    const float POLL_INTERVAL_SEC = 300f;

    // HUD 프리팹 이름 — ResourceManager가 PreLoad 폴더에서 탐색
    const string HUD_PREFAB_NAME = "UI_HUDShout";

    // HUD 전용 루트 Transform 이름 — UIManager의 ToastRoot/PopupRoot와 별도 관리
    const string HUD_ROOT_NAME = "@HUDRoot";

    // 폴링 실행 중 여부
    bool _running;

    // 현재 실행 중인 폴링 코루틴 참조
    Coroutine _pollCoroutine;

    // 마지막으로 표시된 외침 목록 — 동일 id 세트 감지용
    List<ShoutDto> _activeShouts = new List<ShoutDto>();

    // HUD 인스턴스 — 씬 전환으로 파괴될 수 있으므로 null 체크 후 재생성
    UI_HUDShout _hud;

    // HUD 루트 Transform — UIManager Root 하위에 생성
    Transform _hudRoot;

    // 본 외침 id 캐시 — PlayerPrefs로 영속화하여 재폴링 시 제외
    HashSet<int> _seenIds = new HashSet<int>();

    // PlayerPrefs 키 — PlayerId별 분리하여 계정 전환 시 충돌 방지
    private string SeenIdsKey() => $"ShoutSeenIds_{AuthManager.Instance.PlayerId}";

    /// <summary>
    /// 메인 씬 진입 시 호출 — 본 외침 id 로드 → HUD 인스턴스 확보 후 즉시 1회 Fetch + 폴링 코루틴 시작.
    /// 이미 실행 중이면 무시한다.
    /// </summary>
    public void Begin()
    {
        // 중복 호출 방지
        if (_running) return;
        _running = true;

        // 이전 세션의 본 외침 id 로드
        LoadSeenIds();

        // HUD 인스턴스 확보 (없거나 씬 전환으로 파괴된 경우 재생성)
        EnsureHud();

        // 즉시 1회 Fetch 후 폴링 코루틴 시작
        _ = FetchAsync();
        _pollCoroutine = StartCoroutine(PollCoroutine());
    }

    /// <summary>
    /// 메인 씬 이탈 시 호출 — 폴링 코루틴 중단, 콜백 해제 및 HUD 숨김.
    /// </summary>
    public void End()
    {
        _running = false;

        // 폴링 코루틴 중단
        if (_pollCoroutine != null)
        {
            StopCoroutine(_pollCoroutine);
            _pollCoroutine = null;
        }

        // 콜백 해제 후 HUD 숨김 처리 (End() 경로에서는 OnAllShoutsCompleted 발행 불가)
        if (_hud != null)
        {
            _hud.OnAllShoutsCompleted = null;
            _hud.Hide();
        }

        // 상태 초기화
        _activeShouts.Clear();
    }

    /// <summary>
    /// PlayerPrefs에서 본 외침 id CSV를 읽어 _seenIds에 로드한다.
    /// </summary>
    private void LoadSeenIds()
    {
        _seenIds.Clear();
        string csv = PlayerPrefs.GetString(SeenIdsKey(), "");
        if (string.IsNullOrEmpty(csv)) return;

        // CSV 형식 "1,5,8" 파싱
        foreach (var token in csv.Split(','))
        {
            if (int.TryParse(token, out int id))
                _seenIds.Add(id);
        }
    }

    /// <summary>
    /// _seenIds를 CSV로 직렬화하여 PlayerPrefs에 저장한다.
    /// </summary>
    private void SaveSeenIds()
    {
        // CSV 직렬화 후 저장
        string csv = string.Join(",", _seenIds);
        PlayerPrefs.SetString(SeenIdsKey(), csv);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// UI_HUDShout가 1회 순회를 완료했을 때 호출되는 핸들러.
    /// 표시된 외침 id를 _seenIds에 추가하고 PlayerPrefs에 영속화한다.
    /// </summary>
    private void OnHudCompleted()
    {
        if (_activeShouts == null || _activeShouts.Count == 0) return;

        // 표시 완료된 외침 id를 본 목록에 추가
        foreach (var s in _activeShouts)
            _seenIds.Add(s.id);

        SaveSeenIds();
    }

    /// <summary>
    /// 300초 간격으로 API를 폴링하는 코루틴.
    /// Begin()에서 1회 Fetch 후 이 코루틴이 이어서 주기 대기를 시작한다.
    /// </summary>
    private IEnumerator PollCoroutine()
    {
        while (_running)
        {
            // 5분 대기 후 다음 Fetch
            yield return new WaitForSeconds(POLL_INTERVAL_SEC);

            if (!_running) yield break;

            // fire-and-forget — 폴링 대기는 코루틴이, API 호출은 async Task가 담당
            _ = FetchAsync();
        }
    }

    /// <summary>
    /// ShoutApi를 통해 현재 활성 외침 목록을 조회한다.
    /// 성공 시 HandleResponse(), 실패 시 로그만 출력하고 계속 진행한다.
    /// </summary>
    private async Task FetchAsync()
    {
        var result = await ShoutApi.GetActiveAsync();
        if (!result.IsSuccess)
        {
            // 폴링 실패는 무시 — 다음 주기에 재시도
            Debug.LogWarning($"[ShoutManager] 외침 조회 실패: {result.Error?.UserMessage}");
            return;
        }

        HandleResponse(result.Value);
    }

    /// <summary>
    /// API 응답 처리 — 만료 필터링, 본 외침 제외, 동일 세트 감지, HUD 갱신.
    /// </summary>
    /// <param name="list">서버에서 반환된 외침 목록</param>
    private void HandleResponse(List<ShoutDto> list)
    {
        // 만료되지 않고 아직 보지 않은 외침만 필터링
        var validShouts = list
            .Where(s => ServerTime.UtcNow < ParseUtcSafe(s.expiresAt))
            .Where(s => !_seenIds.Contains(s.id))   // 이미 본 외침 제외
            .ToList();

        // 유효한 외침이 없으면 HUD 숨김
        if (validShouts.Count == 0)
        {
            _activeShouts.Clear();
            EnsureHud();
            _hud.Hide();
            return;
        }

        // 동일 id 세트이면 SetMessages 생략 (불필요한 UI 리셋 방지)
        if (IsSameIdSet(validShouts))
            return;

        // 새 세트로 갱신
        _activeShouts = validShouts;

        // HUD 인스턴스 확보 후 콜백 연결 및 메시지 전달
        EnsureHud();
        _hud.OnAllShoutsCompleted = OnHudCompleted;
        _hud.SetMessages(_activeShouts);
    }

    /// <summary>
    /// HUD 인스턴스가 없거나 파괴된 경우 ResourceManager를 통해 재생성한다.
    /// </summary>
    private void EnsureHud()
    {
        // 이미 유효한 인스턴스가 있으면 스킵
        if (_hud != null) return;

        // HUD 루트 Transform 확보 — UIManager가 초기화된 후 Root에 추가
        if (_hudRoot == null)
        {
            GameObject rootGo = new GameObject(HUD_ROOT_NAME);
            _hudRoot = rootGo.transform;
            // UIManager와 같이 DDOL — ShoutManager는 이미 DDOL(Singleton)
            DontDestroyOnLoad(_hudRoot.gameObject);
        }

        // 프리팹 인스턴스 생성
        GameObject go = ResourceManager.Instance.Instantiate(HUD_PREFAB_NAME, _hudRoot);
        if (go == null)
        {
            Debug.LogError($"[ShoutManager] HUD 프리팹 로드 실패: {HUD_PREFAB_NAME}");
            return;
        }

        _hud = go.GetOrAddComponent<UI_HUDShout>();

        // Canvas sortingOrder = 500 (토스트 999 미만, 일반 씬UI 초과)
        var canvas = go.GetComponent<Canvas>();
        if (canvas != null)
            canvas.sortingOrder = 500;

        // 초기 숨김 상태
        go.SetActive(false);
    }

    /// <summary>
    /// 현재 _activeShouts와 새 목록의 id 세트가 동일한지 비교한다.
    /// 정렬 후 시퀀스 비교 — 동일 시 true 반환.
    /// </summary>
    /// <param name="newList">비교할 새 외침 목록</param>
    private bool IsSameIdSet(List<ShoutDto> newList)
    {
        if (_activeShouts.Count != newList.Count)
            return false;

        // id 기준 정렬 후 순서 무관 비교
        var currentIds = _activeShouts.Select(s => s.id).OrderBy(id => id);
        var newIds     = newList.Select(s => s.id).OrderBy(id => id);

        return currentIds.SequenceEqual(newIds);
    }

    /// <summary>
    /// ISO 8601 문자열을 UTC DateTime으로 안전하게 파싱한다.
    /// 파싱 실패 시 DateTime.MaxValue 반환 — 만료 없음으로 처리.
    /// </summary>
    /// <param name="iso">ISO 8601 형식 날짜 문자열 (expiresAt 필드)</param>
    private static DateTime ParseUtcSafe(string iso)
    {
        if (string.IsNullOrEmpty(iso))
            return DateTime.MaxValue;

        // RoundtripKind — Z(UTC) 접미사 및 오프셋 표기 모두 처리
        if (DateTime.TryParse(iso, null, DateTimeStyles.RoundtripKind, out DateTime dt))
            return dt.ToUniversalTime();

        // 파싱 실패 — 만료 안 됨으로 간주 (안전한 방향)
        Debug.LogWarning($"[ShoutManager] expiresAt 파싱 실패: {iso}");
        return DateTime.MaxValue;
    }
}
