using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;

// IAP(인앱 결제) 매니저 — Unity IAP v5 기반 구글 플레이 결제 처리
// 핵심 흐름: OnPurchasePending → 서버 영수증 검증(IapApi) → 성공 시 ConfirmPurchase
// 실패 유형별 재시도 정책(백오프/즉시종료/강제로그아웃)을 분기하여 처리한다
public class IAPManager : Singleton<IAPManager>
{
    // 광고 제거 상품 ID — IAPConfig에도 등록되어 있어야 하며 Entitlement 체크에서 사용
    private const string NOADS_PRODUCT_ID = "com.rookiss.s2.noads";

    // IAP 검증 최대 재시도 횟수
    private const int IAP_MAX_RETRY = 5;

    // 재시도 백오프 대기 시간(초) — 30 / 60 / 120 / 240 / 300
    private static readonly int[] IAP_BACKOFF_SECONDS = { 30, 60, 120, 240, 300 };

    StoreController _storeController;

    // 구매 완료 콜백 — true=성공, false=실패
    Action<bool> _onPurchaseCallback;

    // 중복 초기화 방지 플래그
    bool _initialized;

    // 동일 purchaseToken 중복 진입 방지 — 동시에 같은 토큰으로 재진입 시 무시
    readonly HashSet<string> _inflightTokens = new HashSet<string>();

    #region 아이템 구매

    // 상품 구매 요청 — 콜백은 검증 완료(성공/실패) 후 호출된다
    public void Purchase(string productId, Action<bool> onPurchaseCallback)
    {
        Product product = _storeController.GetProducts()
            .FirstOrDefault(p => p.definition.id == productId);

        if (product != null)
        {
            _onPurchaseCallback = onPurchaseCallback;
            _storeController.PurchaseProduct(product);
        }
        else
        {
            Debug.LogError($"[IAPManager] 상품을 찾을 수 없음: {productId}");
        }
    }

    // 구매 복원 — iOS 전용. Android는 앱 실행 시 Entitlement 자동 체크
    public void RestorePurchases(Action<bool, string> onRestoreCallback)
    {
        _storeController.RestoreTransactions(onRestoreCallback);
    }

    #endregion

    public void Init()
    {
        // 초기화는 Start()에서 비동기로 처리.
    }

    async void Start()
    {
        try
        {
            await InitializeIAP();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    async Task InitializeIAP()
    {
        // 중복 초기화 방지 — Start()가 여러 번 호출되더라도 이벤트가 중복 등록되지 않도록
        if (_initialized) return;
        _initialized = true;

        _storeController = UnityIAPServices.StoreController();

        // 모든 이벤트 핸들러 연결
        _storeController.OnStoreDisconnected += OnStoreDisconnected;
        _storeController.OnPurchasePending   += OnPurchasePending;
        _storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
        _storeController.OnPurchaseFailed    += OnPurchaseFailed;
        _storeController.OnPurchaseDeferred  += OnPurchaseDeferred;
        _storeController.OnCheckEntitlement  += OnCheckEntitlement;
        _storeController.OnProductsFetched   += OnProductsFetched;
        _storeController.OnProductsFetchFailed += OnProductsFetchedFailed;

        await _storeController.Connect();

        // Connect() 완료 전 GameObject가 파괴된 경쟁 상태 방지
        if (this == null) return;

        FetchProducts();
    }

    private void FetchProducts()
    {
        IAPConfig iapConfig = DataManager.Instance.IAPConfig;
        if (iapConfig == null)
        {
            Debug.LogError("[IAPManager] IAPConfig이 DataManager에 로드되지 않았습니다!");
            return;
        }

        List<ProductDefinition> products = iapConfig.GetProductDefinitions();
        _storeController.FetchProducts(products);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // InitializeIAP에서 등록한 UnityIAP StoreController 이벤트 전량 해제
        if (_storeController != null)
        {
            _storeController.OnStoreDisconnected   -= OnStoreDisconnected;
            _storeController.OnPurchasePending     -= OnPurchasePending;
            _storeController.OnPurchaseConfirmed   -= OnPurchaseConfirmed;
            _storeController.OnPurchaseFailed      -= OnPurchaseFailed;
            _storeController.OnPurchaseDeferred    -= OnPurchaseDeferred;
            _storeController.OnCheckEntitlement    -= OnCheckEntitlement;
            _storeController.OnProductsFetched     -= OnProductsFetched;
            _storeController.OnProductsFetchFailed -= OnProductsFetchedFailed;
        }
    }

    #region Event Handlers

    private void OnStoreDisconnected(StoreConnectionFailureDescription obj)
    {
        Debug.LogWarning($"[IAPManager] 스토어 연결 끊김: {obj.Message}");
    }

    // 결제 대기 이벤트 — 구글로부터 결제 승인 후 서버 검증을 수행한다
    // 검증 성공 시에만 ConfirmPurchase 호출 (즉시 Confirm하면 영수증 미검증 지급 버그 발생)
    async void OnPurchasePending(PendingOrder order)
    {
        string productId = order.CartOrdered.Items().First().Product.definition.id;
        // TransactionID = 구글 주문 ID (GPA.xxx 형식)
        string transactionId = order.Info.TransactionID;

        // 구글 플레이 영수증에서 purchaseToken 추출
        if (!TryExtractGooglePurchaseToken(order.Info.Receipt, out string purchaseToken))
        {
            Debug.LogError($"[IAPManager] purchaseToken 추출 실패 — 영수증: {order.Info.Receipt}");
            // 토큰 파싱 실패는 재시도 불가 — 구매 소비 없이 종료 (다음 부팅에서 재시도)
            PopupService.ShowError("결제 처리에 실패했습니다.");
            _onPurchaseCallback?.Invoke(false);
            _onPurchaseCallback = null;
            return;
        }

        // 동일 purchaseToken 중복 진입 방지
        if (!_inflightTokens.Add(purchaseToken))
        {
            Debug.LogWarning($"[IAPManager] purchaseToken 중복 진입 무시: {purchaseToken}");
            return;
        }

        try
        {
            await VerifyAndConfirmAsync(order, productId, purchaseToken, transactionId);
        }
        finally
        {
            // 성공/실패 무관하게 인플라이트 세트에서 제거
            _inflightTokens.Remove(purchaseToken);
            _onPurchaseCallback = null;
        }
    }

    // 서버 영수증 검증 + 재시도 정책 처리
    // 백오프 재시도: 503/502 + IAP_* errorCode → 최대 IAP_MAX_RETRY회
    // 즉시 종료: 403/422/404/409 및 그 외 — 각 케이스별 안내 메시지 출력
    private async Task VerifyAndConfirmAsync(
        PendingOrder order, string productId, string purchaseToken, string transactionId)
    {
        for (int attempt = 0; attempt < IAP_MAX_RETRY; attempt++)
        {
            var result = await IapApi.VerifyGoogleAsync(productId, purchaseToken, transactionId);

            if (result.IsSuccess)
            {
                var payload = result.Value;

                if (!payload.ok)
                {
                    // 서버가 200이지만 ok=false — 영수증 유효하지 않음, 소비 안 함
                    PopupService.ShowError("결제 검증에 실패했습니다. 고객센터에 문의해주세요.");
                    _onPurchaseCallback?.Invoke(false);
                    return;
                }

                // 검증 성공 — ConfirmPurchase로 구글에 소비 완료 전송
                _storeController.ConfirmPurchase(order);

                if (payload.alreadyGranted)
                    PopupService.ShowToast("이미 처리된 결제입니다. 보상은 우편함을 확인해주세요.");

                _onPurchaseCallback?.Invoke(true);
                return;
            }

            var err = result.Error;

            // 네트워크 오류(Status==0) 또는 502/503 + IAP 전용 errorCode → 백오프 재시도
            // CLIENT_GUIDE 명세: "502/503/네트워크 오류: 지수 백오프 재시도"
            bool isIapServerError = err.IsNetworkError
                || ((err.Status == 503 || err.Status == 502)
                    && !string.IsNullOrEmpty(err.ErrorCode)
                    && err.ErrorCode.StartsWith("IAP_"));

            if (isIapServerError)
            {
                if (attempt < IAP_MAX_RETRY - 1)
                {
                    int waitSec = IAP_BACKOFF_SECONDS[attempt];
                    Debug.LogWarning($"[IAPManager] IAP 서버 오류({err.ErrorCode}) — {waitSec}초 후 재시도 ({attempt + 1}/{IAP_MAX_RETRY})");
                    await Task.Delay(waitSec * 1000);
                    continue;
                }

                // 최대 재시도 초과 — 다음 부팅에서 미완료 영수증 재시도 (ConfirmPurchase 안 함)
                Debug.LogError($"[IAPManager] IAP 검증 최대 재시도 초과 — purchaseToken: {purchaseToken}");
                PopupService.ShowToast("결제 처리 중입니다. 잠시 후 자동으로 완료됩니다.");
                return;
            }

            // 403 RequireLinkedAccount — 구글 계정 연동 필요 (ConfirmPurchase 안 함)
            if (err.Status == 403)
            {
                Debug.LogWarning($"[IAPManager] 403 계정 연동 필요 — {err.ErrorCode}");
                PopupService.ShowError("구글 계정 연동이 필요합니다. 로그인 화면에서 구글 계정을 연동해주세요.");
                _onPurchaseCallback?.Invoke(false);
                return;
            }

            // 422 IAP_TOKEN_OWNERSHIP_MISMATCH — 다른 계정의 영수증
            // 무한루프 방지를 위해 소비 처리 후 강제 로그아웃
            if (err.Status == 422 && err.ErrorCode == "IAP_TOKEN_OWNERSHIP_MISMATCH")
            {
                Debug.LogError($"[IAPManager] 422 영수증 소유권 불일치 — purchaseToken: {purchaseToken}");
                _storeController.ConfirmPurchase(order);
                PopupService.ShowError("결제 정보가 일치하지 않습니다. 고객센터에 문의해주세요.");
                EventManager.Instance.TriggerEvent(Define.EEventType.SessionExpired);
                _onPurchaseCallback?.Invoke(false);
                return;
            }

            // 404 / 409 — 재시도 불가 (존재하지 않는 영수증 또는 이미 처리된 중복)
            if (err.Status == 404 || err.Status == 409)
            {
                Debug.LogError($"[IAPManager] {err.Status} 재시도 불가 오류 — {err.ErrorCode}");
                _storeController.ConfirmPurchase(order);
                PopupService.ShowError("결제 처리에 실패했습니다. 고객센터에 문의해주세요.");
                _onPurchaseCallback?.Invoke(false);
                return;
            }

            // 그 외 오류 — 미정의 케이스, ConfirmPurchase 안 함
            Debug.LogError($"[IAPManager] 미정의 오류 — status:{err.Status}, code:{err.ErrorCode}");
            PopupService.ShowError("결제 처리에 실패했습니다.");
            _onPurchaseCallback?.Invoke(false);
            return;
        }
    }

    void OnPurchaseConfirmed(Order order)
    {
        switch (order)
        {
            case FailedOrder failedOrder:
                Debug.LogWarning($"[IAPManager] 구매 확인 실패: {failedOrder.CartOrdered.Items().First().Product.definition.id}, {failedOrder.FailureReason}, {failedOrder.Details}");
                break;
            case ConfirmedOrder:
                Debug.Log($"[IAPManager] 구매 완료: {order.CartOrdered.Items().First().Product.definition.id}");
                break;
        }
    }

    private void OnPurchaseFailed(FailedOrder order)
    {
        Debug.LogWarning($"[IAPManager] 구매 실패: {order.CartOrdered.Items().First().Product.definition.id}, {order.FailureReason}, {order.Details}");
    }

    private void OnPurchaseDeferred(DeferredOrder order)
    {
        Debug.LogWarning($"[IAPManager] 구매 보류(부모 결제 승인 대기): {order.CartOrdered.Items().First().Product.definition.id}");
    }

    // 기존 구매 자격 확인 — 앱 재시작 시 이미 구매한 비소모품 상태를 복원
    private void OnCheckEntitlement(Entitlement entitlement)
    {
        // 완전 보유 상태가 아니면 무시
        if (entitlement.Status != EntitlementStatus.FullyEntitled) return;

        string pid = entitlement.Product?.definition.id;
        Debug.Log($"[IAPManager] Entitlement 확인됨: {pid}");

        // 광고 제거 상품 보유 확인 시 즉시 적용
        if (pid == NOADS_PRODUCT_ID)
            ApplyNoAdsEntitlement();
    }

    // 광고 제거 상품 보유 적용 — PlayerPrefs 캐시 갱신 + AdsManager 상태 반영
    private void ApplyNoAdsEntitlement()
    {
        // 이미 적용되어 있으면 중복 처리 건너뜀
        if (PlayerPrefs.GetInt(PlayerPrefsKey.AdsRemoved, 0) == 1) return;

        PlayerPrefs.SetInt(PlayerPrefsKey.AdsRemoved, 1);
        PlayerPrefs.Save();

        // AdsManager에 즉시 반영 — 이번 세션부터 전면 광고 차단
        AdsManager.Instance.DisableInterstitial();
        Debug.Log("[IAPManager] 광고 제거 적용 완료");
    }

    private void OnProductsFetched(List<Product> products)
    {
        foreach (Product product in products)
        {
            _storeController.CheckEntitlement(product);
        }
    }

    private void OnProductsFetchedFailed(ProductFetchFailed obj)
    {
        Debug.LogWarning($"[IAPManager] 상품 목록 가져오기 실패: {obj.FailureReason}");
    }

    #endregion

    #region 영수증 파싱

    // 구글 플레이 영수증에서 purchaseToken 추출
    // Unity IAP v5 구글 영수증 구조 (중첩 JSON):
    //   외부: {"Store":"GooglePlay","TransactionID":"GPA.xxx","Payload":"{\"json\":\"{...}\",\"purchaseToken\":\"...\"}"}
    //   Payload를 한 번 더 파싱하면 purchaseToken 필드가 있음
    private bool TryExtractGooglePurchaseToken(string receipt, out string purchaseToken)
    {
        purchaseToken = null;

        if (string.IsNullOrEmpty(receipt))
            return false;

        try
        {
            // 외부 래퍼 파싱 — Payload 문자열 추출
            var outer = JsonUtility.FromJson<GoogleReceiptOuter>(receipt);
            if (outer == null || string.IsNullOrEmpty(outer.Payload))
                return false;

            // Payload는 이스케이프된 JSON 문자열 — 한 번 더 파싱
            var payload = JsonUtility.FromJson<GoogleReceiptPayload>(outer.Payload);
            if (payload == null || string.IsNullOrEmpty(payload.purchaseToken))
                return false;

            purchaseToken = payload.purchaseToken;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[IAPManager] 영수증 파싱 중 예외: {ex.Message}");
            return false;
        }
    }

    // 구글 영수증 외부 래퍼 파싱용 내부 DTO
    [Serializable]
    private class GoogleReceiptOuter
    {
        // 스토어 이름 (예: "GooglePlay")
        public string Store;
        // 구글 주문 ID
        public string TransactionID;
        // 내부 결제 정보 JSON 문자열 (이스케이프됨)
        public string Payload;
    }

    // 구글 영수증 Payload 파싱용 내부 DTO
    [Serializable]
    private class GoogleReceiptPayload
    {
        // 구글 결제 토큰 — 서버 검증 및 멱등 키로 사용
        public string purchaseToken;
    }

    #endregion
}
