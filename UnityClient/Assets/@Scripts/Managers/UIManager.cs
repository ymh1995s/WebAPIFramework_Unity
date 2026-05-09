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

    public void Clear()
    {
        CloseAllPopupUI();
        _popupStack.Clear();   // CloseAllPopupUI 후 잔류 참조 완전 정리

        _popups.Clear();

        Root.DestroyChildren();

        _popupRoot = null;     // 팝업 루트 재생성 강제
        _toastRoot = null;     // 토스트 루트 재생성 강제
        _toast = null;         // 토스트 인스턴스 참조 초기화
        _popupOrder = 100;     // 정렬 순서 초기화
        _sceneUI = null;
    }
}
