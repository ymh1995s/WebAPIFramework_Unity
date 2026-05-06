using System.Collections.Generic;
using UnityEngine;

// 스테이지 선택 씬 UI — 서버에서 진행 현황을 받아 각 스테이지 버튼의 활성 상태를 설정한다
public class UI_StageSelect : UI_UGUI
{
    // 씬 내 Button 자식 오브젝트 이름 — sortOrder 순으로 Stage1~3에 매핑
    enum Buttons { Stage1Btn, Stage2Btn, Stage3Btn, MainMenuBtn }

    // sortOrder 기준 정렬된 진행 현황 캐시
    List<StageProgressDto> _progressList;

    protected override void Awake()
    {
        base.Awake();
        BindButtons(typeof(Buttons));

        // 메인메뉴 버튼은 항상 동일 — Awake에서 고정 등록
        GetButton((int)Buttons.MainMenuBtn).onClick.AddListener(OnClickMainMenu);
    }

    protected override void Start()
    {
        base.Start();
        LoadProgress();
    }

    // 진행 현황 API 호출 후 버튼 상태 갱신
    private void LoadProgress()
    {
        StageApi.Instance.GetProgress(
            onSuccess: progress =>
            {
                _progressList = progress;
                // 정렬 순서 기준으로 Stage1~3에 순차 매핑
                _progressList.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
                RefreshButtons();
            },
            onError: err => PopupService.ShowError(err)
        );
    }

    // 진행 현황에 따라 Stage1~3 버튼 활성 여부 및 클릭 핸들러 설정
    private void RefreshButtons()
    {
        int[] stageButtonIndices = { (int)Buttons.Stage1Btn, (int)Buttons.Stage2Btn, (int)Buttons.Stage3Btn };

        for (int i = 0; i < stageButtonIndices.Length; i++)
        {
            var btn = GetButton(stageButtonIndices[i]);

            if (i >= _progressList.Count)
            {
                // 서버에 등록된 스테이지가 버튼 수보다 적으면 비활성
                btn.interactable = false;
                continue;
            }

            var stage = _progressList[i];
            // 잠금 상태이면 버튼 비활성
            btn.interactable = !stage.isLocked;

            // 이전 리스너 제거 후 재등록 — LoadProgress 재호출 시 중복 방지
            btn.onClick.RemoveAllListeners();
            int stageId = stage.stageId; // 클로저 캡처용 로컬 변수
            btn.onClick.AddListener(() => OnClickStage(stageId));
        }
    }

    // 스테이지 버튼 클릭 — StageSession에 선택 stageId를 저장하고 GameScene으로 전환
    private void OnClickStage(int stageId)
    {
        StageSession.StageId = stageId;
        SceneManager.Instance.LoadScene(Define.EScene.GameScene);
    }

    // 메인메뉴 버튼 클릭 — MainScene으로 복귀
    private void OnClickMainMenu()
    {
        SceneManager.Instance.LoadScene(Define.EScene.MainScene);
    }
}
