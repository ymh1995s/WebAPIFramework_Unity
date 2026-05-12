using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로딩씬 UI — Slider로 진행률 표시, TMP_Text로 퍼센트 숫자 표시
// 자식 GameObject 이름이 enum 이름과 매칭되어 자동 바인딩됨
public class UI_LoadingScene : UI_UGUI
{
    // 자식 Slider GameObject 이름과 매칭
    enum Sliders { ProgressBar }

    // 자식 TMP_Text GameObject 이름과 매칭
    enum Texts { ProgressText }

    // 진행률 슬라이더 캐시 — Awake에서 바인딩
    Slider _slider;

    protected override void Awake()
    {
        base.Awake();

        // 자식 자동 바인딩
        Bind<Slider>(typeof(Sliders));
        BindTexts(typeof(Texts));

        _slider = Get<Slider>((int)Sliders.ProgressBar);

        // 초기 상태: 0%
        SetProgress(0f);
    }

    // 로딩 진행률 갱신 — value는 0~1 범위, 외부에서 매 프레임 호출
    public void SetProgress(float value)
    {
        if (_slider != null)
            _slider.value = value;

        GetText((int)Texts.ProgressText).text = $"{Mathf.RoundToInt(value * 100)}%";
    }
}
