using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HTTP REST 로그 패널 - 씬 전환 시에도 유지되는 전역 로그 출력 UI
public class UI_Log : UI_UGUI
{
    // 씬 전환 후 중복 생성 방지용 정적 인스턴스
    private static UI_Log _instance;

    enum Buttons { ClearBtn }
    enum GameObjects { ScrollRect, Content, LogEntryTemplate }

    private ScrollRect _scrollRect;
    private Transform  _content;
    private GameObject _template;

    // 최대 로그 항목 수 - 초과 시 오래된 항목부터 제거
    private const int MaxEntries = 100;
    private readonly List<GameObject> _entries = new();

    protected override void Awake()
    {
        // 이미 인스턴스가 존재하면 중복 제거
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        base.Awake();
        BindButtons(typeof(Buttons));
        BindObjects(typeof(GameObjects));

        _scrollRect = GetObject((int)GameObjects.ScrollRect).GetComponent<ScrollRect>();
        _content    = GetObject((int)GameObjects.Content).transform;
        _template   = GetObject((int)GameObjects.LogEntryTemplate);
        _template.SetActive(false); // 템플릿은 항상 비활성 상태 유지

        GetButton((int)Buttons.ClearBtn).onClick.AddListener(OnClickClear);
        RestLogger.OnLog += HandleLog;
    }

    private void OnDestroy()
    {
        RestLogger.OnLog -= HandleLog;
        if (_instance == this) _instance = null;
    }

    // 로그 수신 - 레벨별 색상 적용 후 스크롤 최하단 이동
    private void HandleLog(RestLogger.LogEntry entry)
    {
        if (_entries.Count >= MaxEntries)
        {
            Destroy(_entries[0]);
            _entries.RemoveAt(0);
        }

        GameObject go = Instantiate(_template, _content);
        go.SetActive(true);

        string color = entry.Level switch
        {
            RestLogger.LogLevel.Warn  => "#FFD700",
            RestLogger.LogLevel.Error => "#FF6B6B",
            _                         => "#FFFFFF"
        };
        go.GetComponentInChildren<TMP_Text>().text =
            $"<color={color}>[{entry.Level}]</color> {entry.Message}";

        _entries.Add(go);
        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 0f;
    }

    // 로그 전체 초기화
    private void OnClickClear()
    {
        foreach (var go in _entries) Destroy(go);
        _entries.Clear();
    }
}
