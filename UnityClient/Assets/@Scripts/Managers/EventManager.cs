using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class EventManager : Singleton<EventManager>
{
    private Dictionary<EEventType, Action> _events = new Dictionary<EEventType, Action>();

    public void AddEvent(EEventType eventType, Action listener)
    {
        if (_events.ContainsKey(eventType) == false)
            _events.Add(eventType, null);

        _events[eventType] += listener;
    }

    public void RemoveEvent(EEventType eventType, Action listener)
    {
        if (_events.ContainsKey(eventType))
            _events[eventType] -= listener;
    }

    public void TriggerEvent(EEventType eventType)
    {
        if (_events.ContainsKey(eventType))
            _events[eventType].Invoke();
    }

    // private이 아닌 protected override로 선언해야 Singleton 베이스의 _instance = null 처리가 실행된다
    protected override void OnDestroy()
    {
        Clear();
        base.OnDestroy();
    }

    public void Clear()
    {
        _events.Clear();
    }
}
