using UnityEngine;

// 앱 부팅 씬 진입점 — ResourceManager 프리로드 완료 후 BootstrapFlow에 흐름을 위임한다
public class BootstrapScene : BaseScene
{
    // BaseScene 초기화 후 씬 타입을 BootstrapScene으로 설정
    protected override void Awake()
    {
        base.Awake();
        SceneType = Define.EScene.BootstrapScene;
    }

    // Resources/PreLoad 폴더 에셋을 모두 메모리에 올린 뒤 부팅 흐름 시작
    private void Start()
    {
        // AppLifecycleManager를 앱 시작 시점에 생성 — 포그라운드 복귀 감지 및 세션 만료 처리 담당
        _ = AppLifecycleManager.Instance;

        // BackButtonHandler를 앱 시작 시점에 생성 — DDOL Singleton이므로 이후 씬에서도 유지
        _ = BackButtonHandler.Instance;

        // FadeManager를 앱 시작 시점에 생성 — DDOL Singleton이므로 이후 씬 전환에서도 유지
        _ = FadeManager.Instance;

        ResourceManager.Instance.LoadAll(
            onProgress: null,
            onComplete: () => BootstrapFlow.Run()
        );
    }
}
