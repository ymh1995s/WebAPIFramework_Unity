using UnityEngine;

/// <summary>
/// SafeArea(노치·홈 인디케이터 등 기기 UI 요소 제외 영역)에 맞춰
/// RectTransform 앵커를 동적으로 조정하는 컴포넌트.
/// 세로/가로 화면 전환, 기기 다양성 대응을 위해 Panel에 부착하여 사용한다.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    // 마지막으로 적용한 SafeArea — 변경 감지용
    private Rect _lastSafeArea = Rect.zero;

    // 마지막으로 적용한 화면 크기 — 회전/해상도 변경 감지용
    private Vector2Int _lastScreenSize = Vector2Int.zero;

    // 대상 RectTransform (자기 자신)
    private RectTransform _rectTransform;

    // 방향별 SafeArea 적용 여부 — false이면 해당 방향 앵커를 화면 끝까지 유지
    [SerializeField] private bool _applyTop    = true;
    [SerializeField] private bool _applyBottom = true;
    [SerializeField] private bool _applyLeft   = true;
    [SerializeField] private bool _applyRight  = true;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        // 재활성화 시 즉시 적용 (Start 대기로 인한 1프레임 지연 방지)
        ApplySafeArea();
    }

    private void Start()
    {
        // 시작 시 1회 적용
        ApplySafeArea();
    }

    private void Update()
    {
        // 화면 크기 또는 SafeArea가 변경되었을 때만 재계산 (매 프레임 연산 최소화)
        Vector2Int currentSize = new Vector2Int(Screen.width, Screen.height);
        Rect currentSafeArea = Screen.safeArea;

        if (_lastSafeArea == currentSafeArea && _lastScreenSize == currentSize)
            return;

        ApplySafeArea();
    }

    /// <summary>
    /// Screen.safeArea 기준으로 RectTransform 앵커를 재설정한다.
    /// 앵커 비율로 변환하여 부모 크기 변경에도 자동 대응한다.
    /// </summary>
    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

        // 화면 크기가 0이면 처리 불가 (에디터 최소화 등 예외 상황 방어)
        if (screenSize.x == 0 || screenSize.y == 0)
            return;

        // SafeArea 픽셀 좌표를 앵커 비율(0~1)로 변환
        Vector2 anchorMin = new Vector2(
            safeArea.xMin / screenSize.x,
            safeArea.yMin / screenSize.y
        );
        Vector2 anchorMax = new Vector2(
            safeArea.xMax / screenSize.x,
            safeArea.yMax / screenSize.y
        );

        // 적용 비활성화된 방향은 앵커를 화면 끝으로 강제 고정
        if (!_applyBottom) anchorMin.y = 0f;
        if (!_applyLeft)   anchorMin.x = 0f;
        if (!_applyTop)    anchorMax.y = 1f;
        if (!_applyRight)  anchorMax.x = 1f;

        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;

        // anchorOffset을 0으로 맞춰 앵커와 완전히 일치
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;

        // 상태 캐시 갱신
        _lastSafeArea = safeArea;
        _lastScreenSize = screenSize;
    }
}
