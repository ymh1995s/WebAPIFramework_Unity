using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로딩씬 UI 컴포넌트 — Slider로 진행률 표시, TMP_Text로 퍼센트 숫자 표시
// 자식 오브젝트에서 Slider와 TMP_Text를 자동 탐색하므로 Inspector 연결 불필요
public class UI_LoadingScene : UI_UGUI
{
    // 진행률 바 — 0(비어있음) ~ 1(가득참) 범위로 표시
    private Slider _slider;

    // 퍼센트 텍스트 — "0%" ~ "100%" 형태로 표시
    private TMP_Text _text;

    protected override void Awake()
    {
        base.Awake();

        // 자식 오브젝트에서 Slider와 TMP_Text 자동 바인딩
        _slider = GetComponentInChildren<Slider>();
        _text = GetComponentInChildren<TMP_Text>();

        // 초기 상태: 0%
        SetProgress(0f);
    }

    // 로딩 진행률 갱신 — value는 0~1 범위, 외부에서 매 프레임 호출
    public void SetProgress(float value)
    {
        if (_slider != null)
            _slider.value = value;

        if (_text != null)
            _text.text = $"{Mathf.RoundToInt(value * 100)}%";
    }
}
