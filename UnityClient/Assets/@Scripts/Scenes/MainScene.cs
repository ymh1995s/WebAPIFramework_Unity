using UnityEngine;

// 메인씬 초기화 및 뒤로가기 처리
public class MainScene : BaseScene
{
    protected override void Awake()
    {
        base.Awake();
        SceneType = Define.EScene.MainScene;
    }

    // 뒤로가기 — 종료 확인 팝업 표시
    public override void OnBackButton()
    {
        PopupService.ShowSelect(
            "게임을 종료하시겠습니까?",
            onOk: QuitApplication,
            onCancel: null
        );
    }

    static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
