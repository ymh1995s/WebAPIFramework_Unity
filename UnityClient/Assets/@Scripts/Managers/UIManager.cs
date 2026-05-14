using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : Singleton<UIManager>
{
    Transform _root;
    Transform Root
    {
        get
        {
            // UIManager 자식으로 생성 → UIManager가 DDOL이므로 @UI_Root도 DDOL 상속
            return Utils.GetRootTransform(ref _root, "@UI_Root", this.transform);
        }
    }

    #region Scene UI
    private UI_Base _sceneUI;
    public UI_Base SceneUI
    {
        get
        {
			foreach (UI_Base ui in FindObjectsByType<UI_Base>(FindObjectsSortMode.None))
			{
				if (ui is IUI_Scene)
				{
					_sceneUI = ui;
					break;
				}
			}
			
			return _sceneUI;
        }
    }

    public T ShowSceneUI<T>(string name = null) where T : UI_Base, IUI_Scene
	{
        if (_sceneUI != null)
            return _sceneUI as T;

        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        T sceneUI = FindFirstObjectByType<T>();
        if (sceneUI == null)
        {
            GameObject go = ResourceManager.Instance.Instantiate(name);
            sceneUI = Utils.GetOrAddComponent<T>(go);
        }

        sceneUI.transform.SetParent(Root);
        _sceneUI = sceneUI;

        return sceneUI;
    }
    #endregion

    #region Popup UI
    Transform _popupRoot;
    Transform PopupRoot
    {
        get
        {
            return Utils.GetRootTransform(ref _popupRoot, "@PopupRoot", Root);
        }
    }

    private int _popupOrder = 100;
    private Stack<UI_Base> _popupStack = new Stack<UI_Base>();
    private Dictionary<string, UI_Base> _popups = new Dictionary<string, UI_Base>();

    // BackButtonHandler가 팝업 존재 여부를 판단하는 데 사용
    public int PopupCount => _popupStack.Count;

    public T ShowPopupUI<T>(string name = null) where T : UI_Base, IUI_Popup
	{
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        // 씬 전환으로 파괴된 캐시 참조를 감지하여 재생성
        if (_popups.TryGetValue(name, out UI_Base popup) == false || popup == null)
        {
            if (popup == null) _popups.Remove(name);
            GameObject go = ResourceManager.Instance.Instantiate(name);
            popup = Utils.GetOrAddComponent<T>(go);
            _popups[name] = popup;
        }

        _popupStack.Push(popup);

        popup.transform.SetParent(PopupRoot);
        popup.gameObject.SetActive(true);
        _popupOrder++;

		if (popup is UI_Toolkit toolkitUI)
        {
            toolkitUI.GetComponent<UIDocument>().sortingOrder = _popupOrder;
			toolkitUI.GetComponent<UIDocument>().rootVisualElement.visible = true;
		}
        else
        {
            popup.GetComponent<Canvas>().sortingOrder = _popupOrder;
        }

        return popup as T;
    }

    public T GetLastPopupUI<T>() where T : UI_Base
	{
        if (_popupStack.Count == 0)
            return null;

        return _popupStack.Peek() as T;
    }

    public void ClosePopupUI()
    {
        if (_popupStack.Count == 0)
            return;

        UI_Base popup = _popupStack.Pop();
        if (popup == null)
        {
            // 외부 Destroy 등으로 파괴된 팝업 참조 — 스택 제거 후 종료
            Debug.LogWarning("[UIManager] ClosePopupUI: 파괴된 팝업 참조가 스택에서 발견됨");
            _popupOrder--;
            return;
        }
        if (popup is UI_Toolkit toolkitUI)
			toolkitUI.GetComponent<UIDocument>().rootVisualElement.visible = false;
        else
            popup.gameObject.SetActive(false);

        _popupOrder--;
    }

    public void CloseAllPopupUI()
    {
        while (_popupStack.Count > 0)
            ClosePopupUI();
    }
    #endregion

    public T ShowUI<T>(string name = null) where T : UI_Base
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = ResourceManager.Instance.Instantiate(name);

        return go.GetOrAddComponent<T>();
    }

    #region Toast UI
    // 토스트 전용 루트 Transform — PopupRoot와 분리하여 팝업 스택에 영향을 주지 않음
    Transform _toastRoot;
    Transform ToastRoot
    {
        get
        {
            return Utils.GetRootTransform(ref _toastRoot, "@ToastRoot", Root);
        }
    }

    // 토스트 인스턴스 캐시 — 매번 새로 생성하지 않고 재사용
    UI_Toast _toast;

    /// <summary>
    /// 화면에 토스트 메시지를 표시한다.
    /// 이미 표시 중인 토스트가 있으면 메시지를 교체하고 타이머를 리셋한다.
    /// </summary>
    /// <param name="message">표시할 메시지</param>
    /// <param name="duration">표시 지속 시간(초). 0 이하이면 기본값(2초) 사용</param>
    public void ShowToast(string message, float duration = 0f)
    {
        // 토스트 인스턴스 최초 생성 또는 씬 전환으로 파괴된 경우 재생성
        if (_toast == null)
        {
            GameObject go = ResourceManager.Instance.Instantiate("UI_Toast");
            _toast = go.GetOrAddComponent<UI_Toast>();
            _toast.transform.SetParent(ToastRoot, false);
            // 팝업보다 항상 위에 표시되도록 높은 sortingOrder 설정
            var canvas = _toast.GetComponent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = 999;
        }

        _toast.Show(message, duration);
    }
    #endregion

    #region BusyMask UI
    // BusyMask 프리팹 이름 — ResourceManager가 PreLoad 폴더에서 탐색
    private const string BUSY_MASK_PREFAB_NAME = "UI_BusyMask";

    // BusyMask 전용 루트 Transform — 팝업·토스트보다 높은 sortingOrder로 항상 최상단 유지
    Transform _busyRoot;
    Transform BusyRoot
    {
        get
        {
            return Utils.GetRootTransform(ref _busyRoot, "@BusyRoot", Root);
        }
    }

    // BusyMask 인스턴스 캐시 — 매 BeginBusy 호출마다 새로 생성하지 않고 재사용
    UI_BusyMask _busyMask;

    // 현재 중첩된 BeginBusy 호출 횟수 — 0이 되는 순간 마스크를 숨긴다
    int _busyCount;

    /// <summary>
    /// BusyMask 표시를 시작하고 IDisposable을 반환한다.
    /// using 블록 또는 Dispose() 호출로 카운트를 자동 감소시킬 수 있다.
    /// 중첩 호출 시 카운트가 쌓이며, 마지막 Dispose에서만 마스크가 사라진다.
    /// </summary>
    /// <param name="label">마스크 위에 표시할 안내 문구 (null이면 텍스트 미표시)</param>
    /// <returns>Dispose 시 카운트를 감소시키는 핸들 객체</returns>
    public IDisposable BeginBusy(string label = null)
    {
        _busyCount++;

        // 카운트가 0에서 1로 오를 때만 마스크를 생성·표시
        if (_busyCount == 1)
        {
            EnsureBusyMask();
            _busyMask?.Show(label); // 프리팹 미발급 상태 NRE 방지 (EnsureBusyMask에서 이미 LogError 출력)
        }

        return new BusyHandle(this);
    }

    /// <summary>
    /// BusyMask 카운트를 강제로 0으로 리셋하고 마스크를 숨긴다.
    /// 씬 전환 직전 SceneManager가 호출하여 잔존 마스크를 정리한다.
    /// </summary>
    public void ForceClearBusy()
    {
        _busyCount = 0;
        _busyMask?.Hide();
    }

    /// <summary>
    /// BusyMask 인스턴스가 없거나 씬 전환으로 파괴된 경우 재생성한다.
    /// ResourceManager 캐시에서 UI_BusyMask 프리팹을 로드하여 BusyRoot에 배치한다.
    /// </summary>
    private void EnsureBusyMask()
    {
        if (_busyMask != null)
            return;

        // ResourceManager를 통해 프리팹 인스턴스 생성
        GameObject go = ResourceManager.Instance.Instantiate(BUSY_MASK_PREFAB_NAME);
        if (go == null)
        {
            Debug.LogError($"[UIManager] 프리팹 키 '{BUSY_MASK_PREFAB_NAME}'를 ResourceManager에서 찾을 수 없습니다. PreLoad 캐시에 등록되어 있는지 확인하세요.");
            return;
        }

        _busyMask = go.GetOrAddComponent<UI_BusyMask>();
        _busyMask.transform.SetParent(BusyRoot, false);

        // 팝업(100+)·토스트(999)보다 높은 sortingOrder로 최상단 보장
        var canvas = _busyMask.GetComponent<Canvas>();
        if (canvas != null)
            canvas.sortingOrder = 1000;

        // 초기 상태는 비활성 — BeginBusy 이후 Show()에서 활성화됨
        _busyMask.gameObject.SetActive(false);
    }

    /// <summary>
    /// BeginBusy의 반환 핸들 — Dispose 시 BusyCount를 감소시킨다.
    /// using 패턴 또는 try/finally에서 사용한다.
    /// </summary>
    private sealed class BusyHandle : IDisposable
    {
        readonly UIManager _manager;
        // 중복 Dispose 방지 플래그
        bool _disposed;

        internal BusyHandle(UIManager manager)
        {
            _manager = manager;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _manager._busyCount--;

            // 카운트가 0 이하로 떨어지면 안전하게 클램프 후 마스크 숨김
            if (_manager._busyCount <= 0)
            {
                _manager._busyCount = 0;
                _manager._busyMask?.Hide();
            }
        }
    }
    #endregion

    public void Clear()
    {
        CloseAllPopupUI();
        _popupStack.Clear();   // CloseAllPopupUI 후 잔류 참조 완전 정리

        _popups.Clear();

        Root.DestroyChildren();

        _popupRoot = null;     // 팝업 루트 재생성 강제
        _toastRoot = null;     // 토스트 루트 재생성 강제
        _busyRoot  = null;     // BusyMask 루트 재생성 강제
        _toast     = null;     // 토스트 인스턴스 참조 초기화
        _busyMask  = null;     // BusyMask 인스턴스 참조 초기화
        _busyCount = 0;        // BusyMask 카운트 초기화
        _popupOrder = 100;     // 정렬 순서 초기화
        _sceneUI = null;
    }
}
