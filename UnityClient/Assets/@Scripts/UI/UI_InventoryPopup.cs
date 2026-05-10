using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 인벤토리 팝업 UI — 보유 아이템 목록 표시 및 아이템 사용 기능을 제공한다
public class UI_InventoryPopup : UI_UGUI, IUI_Popup
{
    // 프리팹 자식 TMP Text 이름
    enum Texts   { Text }

    // 프리팹 자식 Button 이름
    enum Buttons { ExitBtn, UseBtn }

    // 아이템 ID 입력 필드 — UI_UGUI에 InputField 바인딩 헬퍼가 없으므로 직접 탐색
    TMP_InputField _itemIdInput;

    // 현재 표시 중인 인벤토리 목록 (아이템 사용 시 재사용)
    List<PlayerItemDto> _items;

    protected override void Awake()
    {
        base.Awake();

        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

        // InputField를 이름으로 직접 탐색 (enum 바인딩 불가)
        var inputTransform = transform.Find("BackGround/ItemIdInput");
        if (inputTransform != null)
            _itemIdInput = inputTransform.GetComponent<TMP_InputField>();
        else
            Debug.LogWarning("[UI_InventoryPopup] ItemIdInput 을 찾지 못했습니다. 프리팹 자식 이름을 확인하세요.");

        GetButton((int)Buttons.ExitBtn).onClick.AddListener(OnClickExit);
        GetButton((int)Buttons.UseBtn).onClick.AddListener(OnClickUse);
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

        ItemApi.Instance.GetInventory(
            onSuccess: items =>
            {
                _items = items;
                RefreshDisplay();
            },
            onError: err =>
            {
                GetText((int)Texts.Text).text = "인벤토리 불러오기 실패";
                PopupService.ShowError(err);
            }
        );
    }

    // 인벤토리 목록을 텍스트로 포맷하여 표시
    private void RefreshDisplay()
    {
        if (_items == null || _items.Count == 0)
        {
            GetText((int)Texts.Text).text = "보유 아이템이 없습니다.";
            return;
        }

        var sb = new System.Text.StringBuilder();
        foreach (var item in _items)
        {
            // 아이템 종류 레이블 — currency: 재화, 그 외: 소모품
            string typeLabel = item.itemType == "currency" ? "[재화]" : "[소모품]";
            sb.AppendLine($"ID:{item.itemId} {typeLabel} {item.itemName}: {item.quantity}개");
        }

        GetText((int)Texts.Text).text = sb.ToString().TrimEnd();
    }

    // 아이템 사용 버튼 클릭 — InputField에서 itemId 파싱 후 유효성 검사 → API 호출
    private void OnClickUse()
    {
        // InputField가 없으면 안내 후 중단
        if (_itemIdInput == null)
        {
            PopupService.ShowToast("아이템 ID 입력 필드를 찾을 수 없습니다.");
            return;
        }

        // itemId 파싱 실패 시 토스트 안내
        if (!int.TryParse(_itemIdInput.text, out int itemId))
        {
            PopupService.ShowToast("유효한 아이템 ID를 입력해주세요.");
            return;
        }

        // 보유 여부 확인 — _items 리스트에서 해당 itemId 탐색
        var target = _items?.Find(i => i.itemId == itemId);
        if (target == null)
        {
            PopupService.ShowToast("보유하지 않은 아이템입니다.");
            return;
        }

        // 재화(currency)는 사용 불가
        if (target.itemType == "currency")
        {
            PopupService.ShowToast("재화는 사용할 수 없습니다.");
            return;
        }

        // 멱등성 키 생성 — 요청마다 새 UUID 사용
        string clientRequestId = Guid.NewGuid().ToString();

        ItemApi.Instance.Use(
            itemId,
            clientRequestId,
            onSuccess: _ =>
            {
                // 200 성공 — 토스트 안내 후 인벤토리 갱신
                PopupService.ShowToast("아이템을 사용했습니다.");
                LoadInventory();
            },
            onError: err => HandleUseError(err)
        );
    }

    // 아이템 사용 오류 처리 — 상태 코드별 분기
    private void HandleUseError(ApiError err)
    {
        switch (err.Status)
        {
            case 400:
                // 수량 부족 또는 아이템 없음
                PopupService.ShowError(err);
                LoadInventory();
                break;

            case 409:
                // 멱등 충돌 — 사용자 비노출, 로그만 기록 후 인벤토리 갱신
                Debug.LogWarning("[Item] 멱등 충돌 - 중복 요청");
                LoadInventory();
                break;

            case 422:
                // 보상 테이블 오류
                PopupService.ShowError(err);
                LoadInventory();
                break;

            default:
                // 500 및 기타 오류
                PopupService.ShowError(err);
                LoadInventory();
                break;
        }
    }

    private void OnClickExit()
    {
        UIManager.Instance.ClosePopupUI();
    }
}
