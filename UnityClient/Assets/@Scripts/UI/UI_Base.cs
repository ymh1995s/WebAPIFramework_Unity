using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public interface IUI_Popup
{

}

public interface IUI_Scene
{

}

public class UI_Base : MonoBehaviour
{
    protected virtual void Awake()
    {

    }

    protected virtual void Start()
    {
        RefreshUI();
    }

    protected virtual void OnEnable()
    {
        // 앱 종료 중 Instance가 null을 반환할 수 있으므로 null-conditional 사용
        EventManager.Instance?.AddEvent(Define.EEventType.LanguageChanged, RefreshUI);
    }

    protected virtual void OnDisable()
    {
        // 앱 종료 시 _applicationIsQuitting=true → Instance null 반환 → NPE 방지
        EventManager.Instance?.RemoveEvent(Define.EEventType.LanguageChanged, RefreshUI);
    }

    public virtual void RefreshUI()
    {

    }
}
