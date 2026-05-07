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

    public void Clear()
    {
        CloseAllPopupUI();
        _popupStack.Clear();   // CloseAllPopupUI 후 잔류 참조 완전 정리

        _popups.Clear();

        Root.DestroyChildren();

        _popupRoot = null;     // 팝업 루트 재생성 강제
        _popupOrder = 100;     // 정렬 순서 초기화
        _sceneUI = null;
    }
}
