using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

    protected override async void OnEnable()
    {
        base.OnEnable();
        // 팝업이 활성화될 때마다 최신 문의 목록 로드 (재오픈 시에도 갱신)
        // 팝업 비활성화(OnDisable) 시 EnableToken이 취소되어 비활성 오브젝트 접근을 차단
        try
        {
            await LoadInquiriesAsync(EnableToken);
        }
        catch (OperationCanceledException)
        {
            // 팝업 비활성화로 인한 정상 취소 — 무시
        }
    }

    // 문의 목록 API 호출 및 텍스트 갱신
    // ct: OnEnable~OnDisable 구간 취소 토큰 — 비활성화 시 UI 갱신 코드 진입 전 중단
    private async Task LoadInquiriesAsync(CancellationToken ct = default)
    {
        GetText((int)Texts.Text).text = "문의 내역 불러오는 중...";

        var result = await InquiryApi.GetListAsync();
        // API 응답 후 팝업이 이미 비활성화됐다면 UI 갱신 없이 중단
        ct.ThrowIfCancellationRequested();

        if (!result.IsSuccess)
        {
            GetText((int)Texts.Text).text = "문의 내역 불러오기 실패";
            PopupService.ShowError(result.Error);
            return;
        }

        // 성공 — 목록 화면 반영
        RefreshDisplay(result.Value);
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
    private async void OnClickInquiry()
    {
        const string fixedMessage = "문의합니다. 처리 부탁드립니다.";

        var result = await InquiryApi.SubmitAsync(fixedMessage);
        if (!result.IsSuccess)
        {
            PopupService.ShowError(result.Error);
            return;
        }

        // 제출 성공 — 공지 표시 후 목록 갱신
        // 버튼 클릭 경로는 OnEnable 취소 범위 밖이므로 CancellationToken.None 전달
        PopupService.ShowAnnouncement("문의가 접수되었습니다.");
        await LoadInquiriesAsync(CancellationToken.None);
    }

    private void OnClickExit()
    {
        UIManager.Instance.ClosePopupUI();
    }
}
