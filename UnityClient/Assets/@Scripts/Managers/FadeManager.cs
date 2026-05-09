using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 씬 전환 시 화면 페이드 인/아웃을 담당하는 매니저 — DDOL Canvas를 코드로 생성하여 관리
public class FadeManager : Singleton<FadeManager>
{
    // 페이드용 CanvasGroup — alpha 값으로 화면 가리기/드러내기 제어
    private CanvasGroup _canvasGroup;

    // 페이드 진행 중 중복 호출 방지 플래그
    private bool _isFading = false;

    protected void Awake()
    {
        // 페이드 Canvas 및 전체화면 검정 이미지 생성
        CreateFadeCanvas();
    }

    // DDOL 전용 페이드 Canvas를 코드로 생성 — Inspector 설정 불필요
    private void CreateFadeCanvas()
    {
        // 페이드 Canvas 오브젝트 생성 및 DDOL 등록
        GameObject canvasGo = new GameObject("FadeCanvas");
        DontDestroyOnLoad(canvasGo);

        // Canvas 설정 — 최상위 오버레이, sortingOrder 999로 모든 UI 위에 렌더링
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        // CanvasScaler — 다양한 해상도에서 일관된 비율 유지
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);

        // GraphicRaycaster — 페이드 중 입력 차단을 위해 추가
        canvasGo.AddComponent<GraphicRaycaster>();

        // 전체화면 검정 Image 오브젝트 생성
        GameObject imageGo = new GameObject("FadeImage");
        imageGo.transform.SetParent(canvasGo.transform, false);

        // 전체 화면을 덮도록 RectTransform stretch 설정
        RectTransform rect = imageGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 검정 이미지 — alpha는 CanvasGroup으로 제어하므로 color.a = 1로 고정
        Image image = imageGo.AddComponent<Image>();
        image.color = Color.black;

        // CanvasGroup — alpha 제어 및 Raycast 차단 담당
        _canvasGroup = imageGo.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;               // 초기 상태: 완전 투명 (화면 드러남)
        _canvasGroup.blocksRaycasts = false;   // 투명 상태에서는 입력 통과
        _canvasGroup.interactable = false;
    }

    // 화면을 검정으로 가리는 페이드 아웃 — alpha 0 → 1, duration초 소요
    public IEnumerator FadeOut(float duration = 0.3f)
    {
        _canvasGroup.blocksRaycasts = true; // 페이드 중 입력 차단 시작
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        _canvasGroup.alpha = 1f;
    }

    // 화면을 드러내는 페이드 인 — alpha 1 → 0, duration초 소요
    public IEnumerator FadeIn(float duration = 0.3f)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(1f - elapsed / duration);
            yield return null;
        }
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false; // 페이드 완료 후 입력 통과 허용
    }

    // 씬 활성화 후 FadeIn을 예약하는 메서드 — DDOL 오브젝트에서 실행되므로 씬 전환 후에도 코루틴 생존
    // LoadingScene에서 op.allowSceneActivation = true 직전에 호출하여 대상 씬이 드러날 때 페이드 인
    public void ScheduleFadeIn(float duration = 0.15f)
    {
        StartCoroutine(FadeInAfterFrame(duration));
    }

    // 한 프레임 대기 후 FadeIn 실행 — 대상 씬 활성화(Awake/Start) 완료를 기다린 뒤 드러냄
    private IEnumerator FadeInAfterFrame(float duration)
    {
        yield return null; // 대상 씬 활성화 완료 대기 (1프레임)
        yield return FadeIn(duration);
    }
}
