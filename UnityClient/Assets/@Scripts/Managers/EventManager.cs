using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class EventManager : Singleton<EventManager>
{
    // 페이로드 없는 이벤트 구독 목록
    private Dictionary<EEventType, Action> _events = new Dictionary<EEventType, Action>();

    // 페이로드를 동반하는 이벤트 구독 목록
    private Dictionary<EEventType, Action<object>> _eventsWithPayload = new Dictionary<EEventType, Action<object>>();

    // 페이로드 없는 이벤트 구독 등록
    public void AddEvent(EEventType eventType, Action listener)
    {
        if (_events.ContainsKey(eventType) == false)
            _events.Add(eventType, null);

        _events[eventType] += listener;
    }

    // 페이로드 없는 이벤트 구독 해제
    public void RemoveEvent(EEventType eventType, Action listener)
    {
        if (_events.ContainsKey(eventType))
            _events[eventType] -= listener;
    }

    // 페이로드 없이 이벤트 발행
    public void TriggerEvent(EEventType eventType)
    {
        if (_events.ContainsKey(eventType))
            // 구독자가 모두 RemoveEvent로 해제된 경우 delegate 값이 null이 될 수 있으므로 null-conditional 호출
            _events[eventType]?.Invoke();
    }

    // 페이로드를 받는 이벤트 구독 등록
    public void AddEvent(EEventType eventType, Action<object> listener)
    {
        if (!_eventsWithPayload.ContainsKey(eventType))
            _eventsWithPayload[eventType] = null;
        _eventsWithPayload[eventType] += listener;
    }

    // 페이로드를 받는 이벤트 구독 해제
    public void RemoveEvent(EEventType eventType, Action<object> listener)
    {
        if (_eventsWithPayload.ContainsKey(eventType))
            _eventsWithPayload[eventType] -= listener;
    }

    // 페이로드와 함께 이벤트 발행
    public void TriggerEvent(EEventType eventType, object payload)
    {
        if (_eventsWithPayload.ContainsKey(eventType))
            _eventsWithPayload[eventType]?.Invoke(payload);
    }

    // private이 아닌 protected override로 선언해야 Singleton 베이스의 _instance = null 처리가 실행된다
    protected override void OnDestroy()
    {
        Clear();
        base.OnDestroy();
    }

    // 모든 이벤트 구독 테이블 초기화
    public void Clear()
    {
        _events.Clear();
        _eventsWithPayload.Clear();
    }
}
