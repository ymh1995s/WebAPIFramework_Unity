using System.Collections.Generic;
using System.Threading.Tasks;
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

    protected override async void OnEnable()
    {
        base.OnEnable();
        // 팝업이 활성화될 때마다 최신 메일 목록 로드 (재오픈 시에도 갱신)
        await LoadMailsAsync();
    }

    // 메일 목록 API 호출 및 텍스트 갱신
    private async Task LoadMailsAsync()
    {
        GetText((int)Texts.Text).text = "메일 불러오는 중...";

        var result = await MailApi.GetListAsync();
        if (!result.IsSuccess)
        {
            GetText((int)Texts.Text).text = "메일 불러오기 실패";
            PopupService.ShowError(result.Error);
            return;
        }

        // 성공 — 목록 캐시 갱신 후 화면 반영
        _mails = result.Value;
        RefreshDisplay();
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
            // 수령 완료된 메일은 목록에 표시하지 않음
            if (mail.isClaimed) continue;

            // 미수령 태그
            string tag = "[미수령]";
            sb.AppendLine($"{tag} {mail.title}");
            sb.AppendLine($"  {mail.body}");
            sb.AppendLine($"  만료: {mail.expiresAt}");

            // 보상 정보 줄 구성 — 아이템·경험치 유무에 따라 분기
            sb.AppendLine($"  {BuildRewardText(mail)}");
            sb.AppendLine();
        }

        // 미수령 메일이 하나도 없으면 빈 화면 대신 안내 문구 표시
        string result = sb.ToString().TrimEnd();
        GetText((int)Texts.Text).text = string.IsNullOrEmpty(result) ? "메일이 없습니다." : result;
    }

    // 단일 메일의 보상 텍스트를 생성한다
    // 아이템이 있으면 "보상: Gold×100, Gems×5" 형태,
    // 경험치만 있으면 "경험치: N", 둘 다 없으면 "보상 없음"
    private string BuildRewardText(MailDto mail)
    {
        bool hasItems = mail.mailItems != null && mail.mailItems.Length > 0;
        bool hasExp   = mail.exp > 0;

        if (!hasItems && !hasExp)
            return "보상 없음";

        var parts = new System.Collections.Generic.List<string>();

        if (hasItems)
        {
            var itemParts = new System.Collections.Generic.List<string>();
            foreach (var item in mail.mailItems)
            {
                // itemName이 null이거나 비어 있으면 ID로 대체
                string name = string.IsNullOrEmpty(item.itemName) ? $"아이템({item.itemId})" : item.itemName;
                itemParts.Add($"{name}×{item.quantity}");
            }
            parts.Add($"보상: {string.Join(", ", itemParts)}");
        }

        if (hasExp)
            parts.Add($"경험치: {mail.exp}");

        return string.Join(" / ", parts);
    }

    // 미수령 메일 전부 순차 수령 — 수령 성공 시 즉시 목록에서 제거하고 화면 갱신
    private async void OnClickReceiveAll()
    {
        if (_mails == null) return;

        var unclaimed = _mails.FindAll(m => !m.isClaimed);
        if (unclaimed.Count == 0)
        {
            GetText((int)Texts.Text).text = "수령할 메일이 없습니다.";
            return;
        }

        // 미수령 메일을 순차적으로 수령 — 병렬 대신 순차 처리로 서버 부하 방지
        foreach (var mail in unclaimed)
        {
            var result = await MailApi.ClaimAsync(mail.id);
            if (!result.IsSuccess)
            {
                // 실패한 메일은 리스트에 그대로 유지하고 오류만 표시
                PopupService.ShowError(result.Error);
                continue;
            }

            // 수령 완료된 메일을 리스트에서 즉시 제거하고 화면 반영
            _mails.Remove(mail);
            RefreshDisplay();
        }
    }

    private void OnClickExit()
    {
        UIManager.Instance.ClosePopupUI();
    }
}
