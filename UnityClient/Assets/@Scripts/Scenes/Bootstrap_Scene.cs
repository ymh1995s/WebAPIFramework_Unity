using UnityEngine;

// 앱 부팅 씬 진입점 — ResourceManager 프리로드 완료 후 BootstrapFlow에 흐름을 위임한다
public class Bootstrap_Scene : BaseScene
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
        ResourceManager.Instance.LoadAll(
            onProgress: null,
            onComplete: () => BootstrapFlow.Run()
        );
    }
}
