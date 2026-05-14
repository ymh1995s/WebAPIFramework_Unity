using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

// 상점 팝업 UI — 상점 상품 목록 + 가격 재화 보유량 표시, N개 구매, 디버그 아이템 충전 기능을 제공한다
public class UI_ShopPopup : UI_UGUI, IUI_Popup
{
    // 프리팹 자식 TMP Text 이름
    enum Texts   { Text }

    // 프리팹 자식 Button 이름
    enum Buttons { ExitBtn, BuyBtn, ItemAmendDebugBtn }

    // 상품 ID 입력 필드 — 이름 비교로 탐색
    TMP_InputField _itemIdInput;

    // 구매 수량 입력 필드 — 이름 비교로 탐색
    TMP_InputField _itemCountInput;

    // 상점 상품 목록 캐시 — GET /api/shop 응답
    private List<ShopProductDto> _products;

    // 내 인벤토리 캐시 — GET /api/items/inventory 응답. 가격 재화 보유량 매칭용
    private List<PlayerItemDto> _items;

    protected override void Awake()
    {
        base.Awake();

        BindTexts(typeof(Texts));
        BindButtons(typeof(Buttons));

        // TMP_InputField를 이름 기반으로 탐색 — 2개 모두 찾아야 함
        // UI_UGUI 베이스에 InputField 바인딩 헬퍼가 없으므로 직접 순회
        foreach (var f in GetComponentsInChildren<TMP_InputField>(true))
        {
            if (f.gameObject.name == "ItemIdInput")    _itemIdInput    = f;
            if (f.gameObject.name == "ItemCountInput") _itemCountInput = f;
        }

        if (_itemIdInput    == null) Debug.LogWarning("[UI_ShopPopup] ItemIdInput 미발견");
        if (_itemCountInput == null) Debug.LogWarning("[UI_ShopPopup] ItemCountInput 미발견");

        GetButton((int)Buttons.ExitBtn).onClick.AddListener(OnClickExit);
        GetButton((int)Buttons.BuyBtn).onClick.AddListener(OnClickUse);

#if DEBUG
        // 디버그 빌드 — 아이템 충전 버튼 활성화
        GetButton((int)Buttons.ItemAmendDebugBtn).onClick.AddListener(OnClickItemAmendDebug);
#else
        // 릴리즈 빌드 — 디버그 충전 버튼 숨김 (enum 멤버는 컴파일에 필요하므로 유지)
        GetButton((int)Buttons.ItemAmendDebugBtn).gameObject.SetActive(false);
#endif
    }

    protected override async void OnEnable()
    {
        base.OnEnable();
        // 팝업 활성화 시마다 상점 정보 로드 (재오픈 시에도 갱신)
        // 팝업 비활성화(OnDisable) 시 EnableToken이 취소되어 비활성 오브젝트 접근을 차단
        try
        {
            await LoadShopAsync(EnableToken);
        }
        catch (OperationCanceledException)
        {
            // 팝업 비활성화로 인한 정상 취소 — 무시
        }
    }

    // 상점 상품 목록 + 인벤토리를 병렬 조회 후 화면 갱신
    // ct: OnEnable~OnDisable 구간 취소 토큰 — WhenAll 완료 후 팝업이 비활성이면 UI 갱신 없이 중단
    private async Task LoadShopAsync(CancellationToken ct = default)
    {
        GetText((int)Texts.Text).text = "상점 정보 불러오는 중...";

        // 상품 목록과 인벤토리를 병렬 조회 — 응답 도착 순서 무관
        var productsTask  = ShopApi.GetListAsync();
        var inventoryTask = ItemApi.GetInventoryAsync();
        await Task.WhenAll(productsTask, inventoryTask);
        // 병렬 조회 완료 후 팝업이 이미 비활성화됐다면 UI 갱신 없이 중단
        ct.ThrowIfCancellationRequested();

        var productsResult  = productsTask.Result;
        var inventoryResult = inventoryTask.Result;

        if (!productsResult.IsSuccess)
        {
            GetText((int)Texts.Text).text = "상점 목록 불러오기 실패";
            PopupService.ShowError(productsResult.Error);
            return;
        }
        if (!inventoryResult.IsSuccess)
        {
            GetText((int)Texts.Text).text = "인벤토리 불러오기 실패";
            PopupService.ShowError(inventoryResult.Error);
            return;
        }

        // 두 요청 모두 성공 — 캐시 갱신 후 화면 반영
        _products = productsResult.Value;
        _items    = inventoryResult.Value;
        RefreshDisplay();
    }

    // 상점 상품 목록을 sortOrder 오름차순으로 표시하고 가격 재화 보유량을 함께 출력
    private void RefreshDisplay()
    {
        if (_products == null || _products.Count == 0)
        {
            GetText((int)Texts.Text).text = "판매 중인 상품이 없습니다.";
            return;
        }

        // 정렬은 원본을 손대지 않도록 복사본 사용
        var sorted = new List<ShopProductDto>(_products);
        sorted.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));

        var sb = new System.Text.StringBuilder();
        foreach (var p in sorted)
        {
            // 가격 재화 보유량 매칭 — 인벤토리에 없으면 0
            int owned = 0;
            var held = _items?.Find(i => i.itemId == p.priceItemId);
            if (held != null) owned = held.quantity;

            string dailyLabel   = p.dailyLimit == 0  ? "무제한" : p.dailyLimit.ToString();
            string totalLabel   = p.totalLimit == 0  ? "무제한" : p.totalLimit.ToString();
            string maxCallLabel = p.maxPerCall <= 0  ? "제한 없음" : p.maxPerCall.ToString();
            // 판매 중지 상품에 태그 표시
            string disabledTag  = p.isEnabled ? "" : " [판매중지]";

            sb.AppendLine($"[ID:{p.id}] {p.name}{disabledTag}");
            if (!string.IsNullOrEmpty(p.description))
                sb.AppendLine($"  - {p.description}");
            sb.AppendLine($"  가격: {p.priceItemName} {p.priceAmount} (보유: {owned})");
            sb.AppendLine($"  한도: 일일 {dailyLabel} / 전체 {totalLabel} / 1회 {maxCallLabel}");
            sb.AppendLine("───");
        }
        GetText((int)Texts.Text).text = sb.ToString().TrimEnd();
    }

    // 구매 버튼 클릭 — 구매 버튼 단위 비활성(Button scope)으로 이중 클릭 방지
    // ItemIdInput/ItemCountInput 파싱 후 API 호출, 응답 분기 처리
    private void OnClickUse() => RunWithBusyAsync(async () =>
    {
        // 입력 필드 유효성 확인
        if (_itemIdInput == null)
        {
            PopupService.ShowToast("상품 ID 입력 필드를 찾을 수 없습니다.");
            return;
        }
        if (_itemCountInput == null)
        {
            PopupService.ShowToast("수량 입력 필드를 찾을 수 없습니다.");
            return;
        }

        // 상품 ID 파싱
        if (!int.TryParse(_itemIdInput.text, out int productId))
        {
            PopupService.ShowToast("유효한 상품 ID를 입력해주세요.");
            return;
        }

        // 수량 파싱 — 실패 또는 0 이하이면 안내
        if (!int.TryParse(_itemCountInput.text, out int quantity) || quantity <= 0)
        {
            PopupService.ShowToast("1 이상의 수량을 입력해주세요.");
            return;
        }

        // 멱등성 키 생성 — 요청마다 새 UUID 사용
        string clientRequestId = Guid.NewGuid().ToString();

        var result = await ShopApi.BuyAsync(productId, clientRequestId, quantity);
        if (!result.IsSuccess)
        {
            // 오류 처리 — errorCode 기준 분기
            await HandleBuyErrorAsync(result.Error);
            return;
        }

        // 200 성공 — 구매 완료 안내 후 상점 정보 갱신
        // 버튼 클릭 경로는 OnEnable 취소 범위 밖이므로 CancellationToken.None 전달
        PopupService.ShowToast("구매가 완료되었습니다.");
        await LoadShopAsync(CancellationToken.None);
    }, scope: Define.EBusyScope.Button, gateButton: GetButton((int)Buttons.BuyBtn));

    // 구매 오류 처리 — errorCode 기준 분기 (HTTP 상태 코드만으로는 구분 불가능한 경우 포함)
    // 버튼 클릭 경로에서 호출되므로 CancellationToken.None으로 상점 갱신 (취소 불필요)
    private async Task HandleBuyErrorAsync(ApiError err)
    {
        switch (err.ErrorCode)
        {
            case "SHOP_PRODUCT_NOT_FOUND":
                // 404 — 존재하지 않는 상품
                PopupService.ShowToast("존재하지 않는 상품입니다.");
                await LoadShopAsync(CancellationToken.None);
                break;

            case "SHOP_NOT_ENOUGH_CURRENCY":
                // 400 — 잔액 부족
                PopupService.ShowError(err);
                break;

            case "SHOP_LIMIT_WOULD_EXCEED":
                // 400 — 구매 한도 초과 — RawBody 재파싱으로 잔여 수량 추출
                int remaining = ParseRemainingQuantity(err.RawBody);
                // 잔여 수량이 0이면 "(잔여: 0개)" 부분을 숨겨 사용자 혼란 방지
                string suffix = remaining > 0 ? $" (잔여: {remaining}개)" : "";
                PopupService.ShowToast($"구매 한도 초과{suffix}");
                await LoadShopAsync(CancellationToken.None);
                break;

            case "SHOP_MAX_PER_CALL_EXCEEDED":
                // 400 — 1회 최대 구매 수량 초과
                PopupService.ShowToast("1회 최대 구매 수량을 초과했습니다.");
                break;

            case "SHOP_DUPLICATE_REQUEST":
                // 409 — 멱등 충돌 — 사용자 비노출, 로그만 기록 후 상점 갱신
                Debug.LogWarning("[Shop] 멱등 충돌");
                await LoadShopAsync(CancellationToken.None);
                break;

            default:
                // 기타 오류 (500 등)
                PopupService.ShowError(err);
                break;
        }
    }

    // ShopLimitProblem RawBody 파싱 — 실패 시 0 반환
    private int ParseRemainingQuantity(string rawBody)
    {
        if (string.IsNullOrEmpty(rawBody)) return 0;

        try
        {
            var problem = JsonUtility.FromJson<ShopLimitProblem>(rawBody);
            return problem?.remainingQuantity ?? 0;
        }
        catch
        {
            // 파싱 실패 시 0 반환 (잔여 수량 미표시 허용)
            return 0;
        }
    }

#if DEBUG
    // 디버그 빌드 전용 — ItemIdInput / ItemCountInput 입력값으로 임의 아이템 N개 충전
    private async void OnClickItemAmendDebug()
    {
        // 입력 파싱 — ItemIdInput
        if (_itemIdInput == null ||
            !int.TryParse(_itemIdInput.text, out int itemId) ||
            itemId <= 0)
        {
            PopupService.ShowToast("유효한 아이템 ID를 입력해주세요.");
            return;
        }

        // 입력 파싱 — ItemCountInput
        if (_itemCountInput == null ||
            !int.TryParse(_itemCountInput.text, out int qty) ||
            qty <= 0)
        {
            PopupService.ShowToast("유효한 수량을 입력해주세요.");
            return;
        }

        // 디버그 보상 호출 — playerId 생략(JWT 본인)
        var req = new DebugGrantRequest
        {
            exp       = 0,
            items     = new[] { new DebugGrantItem { itemId = itemId, quantity = qty } },
            sourceKey = null  // 서버가 UUID 자동 생성 — 고정 키는 중복 차단됨
        };

        var result = await DebugApi.GrantAsync(req);
        if (!result.IsSuccess)
        {
            PopupService.ShowError(result.Error);
            return;
        }

        // _items, _products 모두 갱신 후 itemName 동적 추출 — 추출 실패 시 itemId 폴백
        // 디버그 핸들러 경로는 OnEnable 취소 범위 밖이므로 CancellationToken.None 전달
        await LoadShopAsync(CancellationToken.None);
        string label = $"itemId={itemId}";
        var added = _items?.Find(i => i.itemId == itemId);
        if (added != null && !string.IsNullOrEmpty(added.itemName))
            label = added.itemName;
        PopupService.ShowToast($"{label} {qty}개 추가되었습니다");

        // 인벤토리 변경 — StatusText 골드 수치 갱신 트리거 (itemId 무관하게 발행)
        EventManager.Instance.TriggerEvent(Define.EEventType.GoldChanged);
    }
#endif

    private void OnClickExit()
    {
        UIManager.Instance.ClosePopupUI();
    }
}
