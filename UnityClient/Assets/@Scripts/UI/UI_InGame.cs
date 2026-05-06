using UnityEngine;

// 게임 씬 UI — 클리어/실패 버튼으로 스테이지 결과를 서버에 전송하고 보상을 표시한다
// StageSession.StageId에서 선택된 스테이지 ID를 읽는다
public class UI_InGame : UI_UGUI
{
    // 씬 내 TMP Text 자식 오브젝트 이름
    enum Texts   { Text }

    // 씬 내 Button 자식 오브젝트 이름
    enum Buttons { StageClearBtn, StageFailBtn }

    protected override void Awake()
    {
        base.Awake();
        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));
        GetButton((int)Buttons.StageClearBtn).onClick.AddListener(OnClickClear);
        GetButton((int)Buttons.StageFailBtn).onClick.AddListener(OnClickFail);
    }

    protected override void Start()
    {
        base.Start();
        // 현재 선택된 스테이지 ID 표시
        GetText((int)Texts.Text).text = $"Stage : {StageSession.StageId:00}";
    }

    // 클리어 버튼 — 더미 결과 데이터로 서버에 클리어 전송 후 보상 팝업 표시
    private void OnClickClear()
    {
        var body = new StageClearRequest
        {
            score       = 100,
            stars       = 3,
            clearTimeMs = 30000L,
        };

        StageApi.Instance.Complete(
            StageSession.StageId,
            body,
            onSuccess: res =>
            {
                string msg = res.isFirstClear
                    ? $"최초 클리어!\n{res.firstRewardMessage}\n경험치 +{res.expGranted}"
                    : $"재도전 클리어!\n{res.replayRewardMessage}\n경험치 +{res.expGranted}";

                // 보상 팝업 확인 후 StageSelectScene으로 복귀
                PopupService.ShowReward(msg, () =>
                    SceneManager.Instance.LoadScene(Define.EScene.StageSelectScene));
            },
            onError: err => PopupService.ShowError(err)
        );
    }

    // 실패 버튼 — StageSelectScene으로 즉시 복귀
    private void OnClickFail()
    {
        SceneManager.Instance.LoadScene(Define.EScene.StageSelectScene);
    }
}
