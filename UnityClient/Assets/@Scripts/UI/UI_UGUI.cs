using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class UI_UGUI : UI_Base
{
    // EventSystem 프리팹 이름 — ResourceManager가 PreLoad 폴더에서 탐색
    private const string EVENT_SYSTEM_PREFAB_NAME = "EventSystem";

    protected Dictionary<Type, Object[]> _objects = new Dictionary<Type, Object[]>();

    protected override void Awake()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
            ResourceManager.Instance.Instantiate(EVENT_SYSTEM_PREFAB_NAME);
    }

    protected void BindObjects(Type type) { Bind<GameObject>(type); }
    protected void BindImages(Type type) { Bind<Image>(type); }
    protected void BindTexts(Type type) { Bind<TMP_Text>(type); }
    protected void BindButtons(Type type) { Bind<Button>(type); }

    protected GameObject GetObject(int idx) { return Get<GameObject>(idx); }
    protected TMP_Text GetText(int idx) { return Get<TMP_Text>(idx); }
    protected Button GetButton(int idx) { return Get<Button>(idx); }
    protected Image GetImage(int idx) { return Get<Image>(idx); }

    protected void Bind<T>(Type type) where T : Object
    {
        string[] names = Enum.GetNames(type);
        Object[] objects = new Object[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            if (typeof(T) == typeof(GameObject))
                objects[i] = Utils.FindChildGameObject(gameObject, names[i], true);
            else
                objects[i] = Utils.FindChildComponent<T>(gameObject, names[i], true);

            if (objects[i] == null)
                Debug.Log($"Failed to bind({names[i]})");
        }

        _objects.Add(typeof(T), objects);
    }

    protected T Get<T>(int idx) where T : Object
    {
        if (_objects.TryGetValue(typeof(T), out Object[] objects) == false)
            return null;

        return objects[idx] as T;
    }
}
