using System.Collections.Generic;
using UnityEngine;

// 메일함 팝업 UI — 메일 목록 조회 및 전체 보상 수령 기능을 제공한다
public class UI_MailPopup : UI_UGUI, IUI_Popup
{
    // 프리팹 자식 TMP Text 이름
    enum Texts   { Text }

    // 프리팹 자식 Button 이름
    enum Buttons { ExitBtn, ReceiveAllBtn }

    // 현재 표시 중인 메일 목록 (전체 수령 시 재사용)
    List<MailDto> _mails;

    protected override void Awake()
    {
        base.Awake();

        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

        GetButton((int)Buttons.ExitBtn).onClick.AddListener(OnClickExit);
        GetButton((int)Buttons.ReceiveAllBtn).onClick.AddListener(OnClickReceiveAll);
    }

    protected override void Start()
    {
        base.Start();
        // 팝업 열릴 때 메일 목록 로드
        LoadMails();
    }

    // 메일 목록 API 호출 및 텍스트 갱신
    private void LoadMails()
    {
        GetText((int)Texts.Text).text = "메일 불러오는 중...";

        MailApi.Instance.GetList(
            onSuccess: mails =>
            {
                _mails = mails;
                RefreshDisplay();
            },
            onError: err =>
            {
                GetText((int)Texts.Text).text = "메일 불러오기 실패";
                PopupService.ShowError(err);
            }
        );
    }

    // 메일 목록을 텍스트로 포맷하여 표시
    private void RefreshDisplay()
    {
        if (_mails == null || _mails.Count == 0)
        {
            GetText((int)Texts.Text).text = "메일이 없습니다.";
            return;
        }

        var sb = new System.Text.StringBuilder();
        foreach (var mail in _mails)
        {
            // 수령 여부 태그
            string tag = mail.isClaimed ? "[수령완료]" : "[미수령]";
            sb.AppendLine($"{tag} {mail.title}");
            sb.AppendLine($"  {mail.body}");
            sb.AppendLine($"  만료: {mail.expiresAt}");
            sb.AppendLine();
        }

        GetText((int)Texts.Text).text = sb.ToString().TrimEnd();
    }

    // 미수령 메일 전부 순차 수령 — 마지막 요청 완료 후 목록 갱신
    private void OnClickReceiveAll()
    {
        if (_mails == null) return;

        var unclaimed = _mails.FindAll(m => !m.isClaimed);
        if (unclaimed.Count == 0)
        {
            GetText((int)Texts.Text).text = "수령할 메일이 없습니다.";
            return;
        }

        // 남은 요청 수를 추적하여 모두 완료된 시점에 목록 갱신
        int remaining = unclaimed.Count;

        foreach (var mail in unclaimed)
        {
            int mailId = mail.id;
            MailApi.Instance.Claim(
                mailId,
                onSuccess: _ =>
                {
                    remaining--;
                    if (remaining == 0) LoadMails();
                },
                onError: err =>
                {
                    remaining--;
                    if (remaining == 0) LoadMails();
                    PopupService.ShowError(err);
                }
            );
        }
    }

    private void OnClickExit()
    {
        UIManager.Instance.ClosePopupUI();
    }
}
