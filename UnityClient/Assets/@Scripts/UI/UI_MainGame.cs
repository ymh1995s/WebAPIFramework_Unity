using System;
using UnityEngine;

// 메인 씬 UI — 모든 기능 버튼을 바인딩하고 각 흐름(랭킹/메일/문의/계정/씬 전환)을 처리한다
public class UI_MainGame : UI_UGUI
{
    // 씬 내 TMP Text 자식 오브젝트 이름 — StatusText로 계정 상태를 표시
    enum Texts   { StatusText }

    // 씬 내 Button 자식 오브젝트 이름 — ExtraBtn (3~5)은 특수문자 포함으로 바인딩 제외
    enum Buttons
    {
        RankingBtn, MailBoxBtn, InquiryBtn, NotinceBtn,
        InventoryBtn, GoogleInterlockBtn, WithdrawBtn,
        LogOutBtn, StageSelectBtn, InAppPurchaseBtn, AdsBtn
    }

    protected override void Awake()
    {
        base.Awake();

        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

        // 버튼별 클릭 핸들러 등록
        GetButton((int)Buttons.RankingBtn).onClick.AddListener(OnClickRanking);
        GetButton((int)Buttons.MailBoxBtn).onClick.AddListener(OnClickMailBox);
        GetButton((int)Buttons.InquiryBtn).onClick.AddListener(OnClickInquiry);
        GetButton((int)Buttons.NotinceBtn).onClick.AddListener(OnClickNotice);
        GetButton((int)Buttons.InventoryBtn).onClick.AddListener(OnClickInventory);
        GetButton((int)Buttons.GoogleInterlockBtn).onClick.AddListener(OnClickGoogleInterlock);
        GetButton((int)Buttons.WithdrawBtn).onClick.AddListener(OnClickWithdraw);
        GetButton((int)Buttons.LogOutBtn).onClick.AddListener(OnClickLogout);
        GetButton((int)Buttons.StageSelectBtn).onClick.AddListener(OnClickStageSelect);
        GetButton((int)Buttons.InAppPurchaseBtn).onClick.AddListener(OnClickInAppPurchase);
        GetButton((int)Buttons.AdsBtn).onClick.AddListener(OnClickAds);
    }

    protected override void Start()
    {
        base.Start();
        // 씬 진입 시 계정 상태 표시
        RefreshStatus();
    }

    // 상태 텍스트를 PlayerId + 구글 연동 여부로 갱신
    private void RefreshStatus()
    {
        string googleTag = AuthManager.Instance.IsGoogleLinked ? "[구글 연동]" : "[게스트]";
        GetText((int)Texts.StatusText).text =
            $"{googleTag}  PlayerId: {AuthManager.Instance.PlayerId}";
    }

    // ─── 랭킹 ──────────────────────────────────────────────────────────────

    // 내 랭킹 조회 후 공지 팝업으로 표시
    private void OnClickRanking()
    {
        RankingApi.Instance.GetMyRank(
            onSuccess: rank =>
            {
                string msg = $"현재 순위: {rank.rank}위\n닉네임: {rank.nickname}\n최고 점수: {rank.bestScore}";
                PopupService.ShowAnnouncement(msg);
            },
            onError: err => PopupService.ShowError(err)
        );
    }

    // ─── 메일함 ────────────────────────────────────────────────────────────

    private void OnClickMailBox()
    {
        UIManager.Instance.ShowPopupUI<UI_MailPopup>();
    }

    // ─── 문의 ──────────────────────────────────────────────────────────────

    private void OnClickInquiry()
    {
        UIManager.Instance.ShowPopupUI<UI_InquiryPopup>();
    }

    // ─── 공지 ──────────────────────────────────────────────────────────────

    // 서버에서 최신 공지를 가져와 팝업으로 표시
    private void OnClickNotice()
    {
        NoticeApi.Instance.GetLatest(
            onSuccess: notice =>
            {
                string content = (notice == null || notice.id == 0)
                    ? "현재 공지사항이 없습니다."
                    : notice.content;
                PopupService.ShowAnnouncement(content);
            },
            onError: err => PopupService.ShowError(err)
        );
    }

    // ─── 인벤토리 (미구현) ─────────────────────────────────────────────────

    private void OnClickInventory()
    {
        PopupService.ShowAnnouncement("인벤토리 기능은 준비 중입니다.");
    }

    // ─── 구글 연동 ─────────────────────────────────────────────────────────

    // 미연동 상태이면 구글 로그인 후 계정 연결, 이미 연동이면 안내만 표시
    private async void OnClickGoogleInterlock()
    {
        if (AuthManager.Instance.IsGoogleLinked)
        {
            PopupService.ShowAnnouncement("이미 구글 계정이 연동되어 있습니다.");
            return;
        }

        try
        {
            var user = await GoogleSignInProvider.SignIn();

            // 에디터 환경 등에서 null 반환 시 조용히 중단
            if (user == null) return;

            AuthApi.Instance.LinkGoogle(
                user.IdToken,
                onSuccess: _ =>
                {
                    AuthManager.Instance.SetGoogleLinked(true);
                    RefreshStatus();
                    PopupService.ShowAnnouncement("구글 계정 연동이 완료되었습니다.");
                },
                onError: err => PopupService.ShowError(err)
            );
        }
        catch (Exception e)
        {
            PopupService.ShowError(new ApiError
            {
                Status = 0,
                Title  = "구글 인증 실패",
                Detail = e.Message,
            });
        }
    }

    // ─── 탈퇴 ──────────────────────────────────────────────────────────────

    private void OnClickWithdraw()
    {
        PopupService.ShowSelect(
            "정말 탈퇴하시겠습니까?\n탈퇴 후 계정 복구가 불가능합니다.",
            onOk:     DoWithdraw,
            onCancel: null
        );
    }

    // 탈퇴 API 호출 후 로컬 토큰 초기화 및 LoginScene 전환
    private void DoWithdraw()
    {
        AuthApi.Instance.Withdraw(
            onSuccess: _ =>
            {
                AuthManager.Instance.Clear();
                SceneManager.Instance.LoadScene(Define.EScene.LoginScene);
            },
            onError: err => PopupService.ShowError(err)
        );
    }

    // ─── 로그아웃 ──────────────────────────────────────────────────────────

    private void OnClickLogout()
    {
        PopupService.ShowSelect(
            "로그아웃 하시겠습니까?",
            onOk:     DoLogout,
            onCancel: null
        );
    }

    // 로그아웃 API 호출 — 서버 실패 시에도 로컬 토큰 삭제 후 LoginScene으로 이동
    private void DoLogout()
    {
        AuthApi.Instance.Logout(
            AuthManager.Instance.RefreshToken,
            onSuccess: _ =>
            {
                AuthManager.Instance.Clear();
                SceneManager.Instance.LoadScene(Define.EScene.LoginScene);
            },
            onError: _ =>
            {
                // 서버 오류와 무관하게 로컬 세션 초기화
                AuthManager.Instance.Clear();
                SceneManager.Instance.LoadScene(Define.EScene.LoginScene);
            }
        );
    }

    // ─── 씬 전환 ───────────────────────────────────────────────────────────

    private void OnClickStageSelect()
    {
        SceneManager.Instance.LoadScene(Define.EScene.StageSelectScene);
    }

    // ─── 미구현 버튼 ───────────────────────────────────────────────────────

    private void OnClickInAppPurchase()
    {
        PopupService.ShowAnnouncement("인앱 결제 기능은 준비 중입니다.");
    }

    private void OnClickAds()
    {
        PopupService.ShowAnnouncement("광고 기능은 준비 중입니다.");
    }
}
