using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public interface IUI_Popup { }
public interface IUI_Scene { }

public class UI_Base : MonoBehaviour
{
    // 핸들러 이름 기준 진행 중 요청 집합 — RunWithBusyAsync(Layer C) 및 GuardReentry(동기)가 공유
    // CallerMemberName으로 자동 추출된 메서드 이름을 키로 사용
    private readonly HashSet<string> _inflight = new HashSet<string>(StringComparer.Ordinal);

    // 팝업 활성(OnEnable) ~ 비활성(OnDisable) 구간 취소 토큰 소스
    // OnDisable 시 Cancel → 비활성 오브젝트에 대한 비동기 접근을 중단시킨다
    private CancellationTokenSource _enableCts;

    // OnEnable~OnDisable 구간 동안 유효한 CancellationToken
    // 팝업이 비활성화되면 자동 취소되어 이후 await 지점에서 OperationCanceledException 발생
    protected CancellationToken EnableToken => _enableCts?.Token ?? CancellationToken.None;

    protected virtual void Awake() { }

    protected virtual void Start()
    {
        RefreshUI();
    }

    protected virtual void OnEnable()
    {
        // 팝업이 활성화될 때마다 새 취소 토큰 소스를 발급 — 이전 소스는 OnDisable에서 정리됨
        _enableCts = new CancellationTokenSource();
        // 팝업 풀링(SetActive 재사용) 시 이전 진행 상태 초기화
        _inflight.Clear();
        // 앱 종료 중 Instance가 null을 반환할 수 있으므로 null-conditional 사용
        EventManager.Instance?.AddEvent(Define.EEventType.LanguageChanged, RefreshUI);
    }

    protected virtual void OnDisable()
    {
        // 팝업 비활성화 시 진행 중인 비동기 작업을 취소하여 비활성 오브젝트 접근 방지
        _enableCts?.Cancel();
        _enableCts?.Dispose();
        _enableCts = null;
        // 앱 종료 시 _applicationIsQuitting=true → Instance null 반환 → NPE 방지
        EventManager.Instance?.RemoveEvent(Define.EEventType.LanguageChanged, RefreshUI);
    }

    public virtual void RefreshUI() { }

    // 동기 핸들러 재진입 방지 — 동일 핸들러 이름이 이미 실행 중이면 false 반환 (Layer C 최후 보루)
    // CallerMemberName으로 호출 메서드 이름을 자동 캡처하므로 key 인자는 생략 가능
    protected bool GuardReentry(Action action, [CallerMemberName] string key = null)
    {
        if (!_inflight.Add(key)) return false;
        try   { action(); }
        finally { _inflight.Remove(key); }
        return true;
    }

    // BusyMask 연동 async void 핸들러 래퍼 — Phase 1 이후 표준 진입점
    // reentryKey(CallerMemberName)로 중복 실행을 막은 뒤 scope에 따라 UI 잠금을 적용한다
    //   Global : UIManager.BeginBusy → 전화면 반투명 마스크 + 모든 입력 차단
    //   Button : gateButton.interactable = false → 해당 버튼만 비활성화
    //   None   : 가시 처리 없이 재진입 방지만 수행
    // action에서 발생하는 예외는 Debug.LogException으로 전파하며 삼키지 않는다
    protected async void RunWithBusyAsync(
        Func<Task> action,
        Define.EBusyScope scope = Define.EBusyScope.Global,
        Button gateButton = null,
        string busyLabel = null,
        [CallerMemberName] string reentryKey = null)
    {
        // 동일 핸들러가 이미 실행 중이면 즉시 무시
        if (!_inflight.Add(reentryKey)) return;

        // scope에 따라 UI 잠금 처리
        IDisposable busyHandle = null;
        if (scope == Define.EBusyScope.Global)
        {
            // 전화면 마스크를 BeginBusy → using 블록으로 자동 해제
            busyHandle = UIManager.Instance?.BeginBusy(busyLabel);
        }
        else if (scope == Define.EBusyScope.Button && gateButton != null)
        {
            // 버튼 단독 차단 — 처리 완료 후 finally에서 복원
            gateButton.interactable = false;
        }

        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            // 팝업 비활성(OnDisable)으로 인한 정상 취소 — 오류가 아니므로 Log 레벨 출력 후 조용히 종료
            Debug.Log($"[UI_Base] {reentryKey} 작업이 팝업 비활성화로 취소되었습니다.");
        }
        catch (Exception e)
        {
            // 예외를 삼키지 않고 Unity 콘솔에 전체 스택 출력
            Debug.LogException(e);
        }
        finally
        {
            // _inflight에서 키 제거 — 다음 클릭부터 다시 실행 허용
            _inflight.Remove(reentryKey);

            // UI 잠금 해제
            busyHandle?.Dispose();
            if (scope == Define.EBusyScope.Button && gateButton != null)
                gateButton.interactable = true;
        }
    }
}
