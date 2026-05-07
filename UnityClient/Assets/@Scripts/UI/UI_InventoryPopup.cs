using System.Collections.Generic;
using UnityEngine;

// 인벤토리 팝업 UI — 보유 아이템 목록을 문자열로 표시한다
public class UI_InventoryPopup : UI_UGUI, IUI_Popup
{
    // 프리팹 자식 TMP Text 이름
    enum Texts   { Text }

    // 프리팹 자식 Button 이름
    enum Buttons { ConfirmBtn }

    protected override void Awake()
    {
        base.Awake();

        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

        GetButton((int)Buttons.ConfirmBtn).onClick.AddListener(OnClickConfirm);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // 팝업이 활성화될 때마다 최신 인벤토리 로드 (재오픈 시에도 갱신)
        LoadInventory();
    }

    // 인벤토리 API 호출 및 텍스트 갱신
    private void LoadInventory()
    {
        GetText((int)Texts.Text).text = "인벤토리 불러오는 중...";

        InventoryApi.Instance.GetInventory(
            onSuccess: items => RefreshDisplay(items),
            onError: err =>
            {
                GetText((int)Texts.Text).text = "인벤토리 불러오기 실패";
                PopupService.ShowError(err);
            }
        );
    }

    // 아이템 목록을 텍스트로 포맷하여 표시
    private void RefreshDisplay(List<PlayerItemDto> items)
    {
        if (items == null || items.Count == 0)
        {
            GetText((int)Texts.Text).text = "보유 아이템이 없습니다.";
            return;
        }

        var sb = new System.Text.StringBuilder();
        foreach (var item in items)
        {
            // itemType "currency" = 재화, "consumable" = 소모품
            string typeLabel = item.itemType == "currency" ? "[재화]" : "[소모품]";
            sb.AppendLine($"{typeLabel} {item.itemName}: {item.quantity}개");
        }

        GetText((int)Texts.Text).text = sb.ToString().TrimEnd();
    }

    private void OnClickConfirm()
    {
        UIManager.Instance.ClosePopupUI();
    }
}
