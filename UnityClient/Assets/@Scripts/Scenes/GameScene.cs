using UnityEngine;

// 게임씬 초기화 및 뒤로가기 처리
public class GameScene : BaseScene
{
    protected override void Awake()
    {
        base.Awake();
        SceneType = Define.EScene.GameScene;
    }

    // 뒤로가기 — 스테이지 이탈 확인 팝업 표시
    public override void OnBackButton()
    {
        PopupService.ShowSelect(
            "스테이지에서 나가시겠습니까?\n진행 상황이 저장되지 않습니다.",
            onOk: () => SceneManager.Instance.LoadScene(Define.EScene.StageSelectScene),
            onCancel: null
        );
    }
}
