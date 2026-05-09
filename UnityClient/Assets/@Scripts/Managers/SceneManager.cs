using System.Collections;
using UnityEngine;

// 씬 전환을 총괄하는 매니저 — 페이드 + 로딩씬 경유 전환과 즉시 전환 두 가지 제공
public class SceneManager : Singleton<SceneManager>
{
    // 현재 씬의 BaseScene 컴포넌트 캐시 — null 시 FindFirstObjectByType으로 재탐색
    private BaseScene _currentScene;
    public BaseScene CurrentScene
    {
        get
        {
            if (_currentScene == null)
                _currentScene = FindFirstObjectByType<BaseScene>();

            return _currentScene;
        }
    }

    public Define.EScene CurrentSceneType
    {
        get
        {
            if (CurrentScene == null)
                return Define.EScene.Unknown;

            return CurrentScene.SceneType;
        }
    }

    // 로딩씬이 읽어갈 전환 대상 씬 — LoadScene 호출 시 설정, LoadingScene.cs에서 참조
    public static Define.EScene PendingScene { get; private set; }

    // 씬 전환 공개 메서드 — FadeOut → 로딩씬 경유 → FadeIn 흐름으로 전환
    // 일반 씬 전환 시 이 메서드만 사용할 것
    public void LoadScene(Define.EScene scene)
    {
        PendingScene = scene;
        StartCoroutine(TransitionToLoading());
    }

    // FadeOut 완료 후 로딩씬으로 즉시 전환하는 코루틴
    private IEnumerator TransitionToLoading()
    {
        // [1] 화면을 검정으로 가리기
        yield return StartCoroutine(FadeManager.Instance.FadeOut(0.15f));

        // [2] 로딩씬으로 즉시 전환 — 이후 로딩씬이 진행률 표시 및 대상 씬 로드 담당
        LoadSceneImmediate(Define.EScene.LoadingScene);
    }

    // 페이드 없이 즉시 씬 전환 — Bootstrap 부팅, 로딩씬 내부 전환 등에서 사용
    public void LoadSceneImmediate(Define.EScene sceneType)
    {
        // 씬 전환 직전 UI 스택·캐시 정리 — 파괴된 팝업 참조 잔류 방지
        UIManager.Instance.Clear();
        string sceneName = sceneType.ToString();
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        _currentScene = null;
    }
}
