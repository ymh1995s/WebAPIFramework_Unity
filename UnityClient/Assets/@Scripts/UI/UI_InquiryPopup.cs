using System.Collections.Generic;
using UnityEngine;

// 문의 팝업 UI — 문의 목록 조회 및 고정 메시지로 문의 제출 기능을 제공한다
public class UI_InquiryPopup : UI_UGUI, IUI_Popup
{
    // 프리팹 자식 TMP Text 이름
    enum Texts   { Text }

    // 프리팹 자식 Button 이름
    enum Buttons { ExitBtn, InquiryBtn }

    protected override void Awake()
    {
        base.Awake();

        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

        GetButton((int)Buttons.ExitBtn).onClick.AddListener(OnClickExit);
        GetButton((int)Buttons.InquiryBtn).onClick.AddListener(OnClickInquiry);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // 팝업이 활성화될 때마다 최신 문의 목록 로드 (재오픈 시에도 갱신)
        LoadInquiries();
    }

    // 문의 목록 API 호출 및 텍스트 갱신
    private void LoadInquiries()
    {
        GetText((int)Texts.Text).text = "문의 내역 불러오는 중...";

        InquiryApi.Instance.GetList(
            onSuccess: inquiries =>
            {
                RefreshDisplay(inquiries);
            },
            onError: err =>
            {
                GetText((int)Texts.Text).text = "문의 내역 불러오기 실패";
                PopupService.ShowError(err);
            }
        );
    }

    // 문의 목록을 텍스트로 포맷하여 표시 — 각 문의마다 개행
    private void RefreshDisplay(List<InquiryDto> inquiries)
    {
        if (inquiries == null || inquiries.Count == 0)
        {
            GetText((int)Texts.Text).text = "문의 내역이 없습니다.";
            return;
        }

        var sb = new System.Text.StringBuilder();
        foreach (var inquiry in inquiries)
        {
            sb.AppendLine($"[{inquiry.createdAt}] {inquiry.content}");

            if (!string.IsNullOrEmpty(inquiry.adminReply))
                sb.AppendLine($"  답변: {inquiry.adminReply} ({inquiry.repliedAt})");
            else
                sb.AppendLine("  답변 대기 중");

            sb.AppendLine();
        }

        GetText((int)Texts.Text).text = sb.ToString().TrimEnd();
    }

    // 고정 메시지로 문의를 제출한다 — 프리팹에 InputField가 없으므로 고정 문구 사용
    private void OnClickInquiry()
    {
        const string fixedMessage = "문의합니다. 처리 부탁드립니다.";
        InquiryApi.Instance.Submit(
            fixedMessage,
            onSuccess: _ =>
            {
                PopupService.ShowAnnouncement("문의가 접수되었습니다.");
                LoadInquiries();
            },
            onError: err => PopupService.ShowError(err)
        );
    }

    private void OnClickExit()
    {
        UIManager.Instance.ClosePopupUI();
    }
}
