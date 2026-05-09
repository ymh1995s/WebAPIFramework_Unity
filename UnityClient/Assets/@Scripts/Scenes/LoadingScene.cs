using System.Collections;
using UnityEngine;

// 씬 전환 로딩 화면 — FadeIn으로 드러낸 뒤 대상 씬을 비동기 로드하고 FadeOut 후 전환
// SceneManager.LoadScene → TransitionToLoading → 이 씬 → PendingScene 순서로 진행
public class LoadingScene : BaseScene
{
    protected override void Awake()
    {
        base.Awake();
        // 로딩 씬 타입 등록 — SceneManager가 현재 씬을 식별할 때 사용
        SceneType = Define.EScene.LoadingScene;
    }

    // 씬 내 UI_LoadingScene 컴포넌트 — 진행률 표시 담당
    private UI_LoadingScene _ui;

    // IEnumerator Start() — Unity가 코루틴으로 자동 실행, 전체 로딩 흐름 담당
    private IEnumerator Start()
    {
        // [1] UI_LoadingScene 컴포넌트 탐색 (씬 내 Hierarchy에서 자동 탐색)
        _ui = FindFirstObjectByType<UI_LoadingScene>();

        // [2] 로딩 화면 드러내기 — 검정 → 로딩 UI 보임
        yield return StartCoroutine(FadeManager.Instance.FadeIn(0.15f));

        // [3] 대상 씬 비동기 로드 (진행률 표시 포함)
        yield return StartCoroutine(LoadAsync());
    }

    // 대상 씬을 비동기로 로드하고 진행률을 UI에 반영하는 코루틴
    private IEnumerator LoadAsync()
    {
        // SceneManager.PendingScene에서 전환 대상 씬 이름 획득
        Define.EScene target = SceneManager.PendingScene;
        string sceneName = target.ToString();

        // 비동기 씬 로드 시작 — 활성화는 수동으로 제어 (allowSceneActivation = false)
        // UnityEngine.SceneManagement.SceneManager와 프로젝트 SceneManager 이름 충돌 방지를 위해 전체 경로 사용
        AsyncOperation op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // [4] 진행률 0 ~ 90% 구간 표시 (Unity는 allowSceneActivation=false일 때 최대 0.9까지만 진행)
        while (op.progress < 0.9f)
        {
            _ui?.SetProgress(op.progress / 0.9f);
            yield return null;
        }

        // [5] 로딩 완료 — 진행률 100% 표시
        _ui?.SetProgress(1f);

        // [6] 최소 표시 시간 확보 — 로딩이 너무 빠를 때 로딩바가 깜빡이는 현상 방지
        yield return new WaitForSeconds(0.5f);

        // [7] 로딩 화면 가리기 — 로딩 UI → 검정
        yield return StartCoroutine(FadeManager.Instance.FadeOut(0.15f));

        // [8] 대상 씬 활성화 후 FadeIn 예약 — DDOL FadeManager 코루틴이 씬 전환 후에도 생존하여 실행
        FadeManager.Instance.ScheduleFadeIn(0.15f);

        // [9] 씬 활성화 — LoadingScene 파괴, 대상 씬 시작
        op.allowSceneActivation = true;
    }
}
