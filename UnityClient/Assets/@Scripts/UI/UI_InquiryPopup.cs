using System.Collections.Generic;
using UnityEngine;

// 문의 팝업 UI — 문의 목록 조회 및 새 문의 제출 기능을 제공한다
// 현재 프리팹에 InputField가 없어 제출 기능은 미구현 (준비 중 안내만 표시)
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

    protected override void Start()
    {
        base.Start();
        // 팝업 열릴 때 문의 목록 로드
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

    // 문의 제출 — 현재 프리팹에 InputField가 없어 준비 중 안내
    private void OnClickInquiry()
    {
        PopupService.ShowAnnouncement("문의 입력 UI는 준비 중입니다.");
    }

    private void OnClickExit()
    {
        UIManager.Instance.ClosePopupUI();
    }
}
