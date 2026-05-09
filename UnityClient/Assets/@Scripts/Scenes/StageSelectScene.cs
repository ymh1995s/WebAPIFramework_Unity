using UnityEngine;

// 스테이지 선택씬 초기화 및 뒤로가기 처리
public class StageSelectScene : BaseScene
{
    protected override void Awake()
    {
        base.Awake();
        SceneType = Define.EScene.StageSelectScene;
    }

    // 뒤로가기 — 메인씬으로 이동
    public override void OnBackButton()
    {
        SceneManager.Instance.LoadScene(Define.EScene.MainScene);
    }
}
