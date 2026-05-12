using UnityEngine;
using UnityEngine.InputSystem;

// 뒤로가기(Android Escape) 입력을 전역으로 처리하는 싱글톤 매니저
// 팝업이 열려 있으면 최상단 팝업을 닫고, 없으면 현재 씬에 위임한다
public class BackButtonHandler : Singleton<BackButtonHandler>
{
    // 연타 방지 — 처리 후 2프레임 동안 입력 무시
    int _lockUntilFrame;

    void Update()
    {
        // 연타 가드
        if (_lockUntilFrame > Time.frameCount) return;

        // New Input System 사용 — activeInputHandler: 2(New Only) 설정에서 구 Input 클래스 호출 시 런타임 예외 발생
        // Android Back 키는 Escape로 매핑됨. Keyboard 장치가 없는 환경을 null 체크로 안전하게 통과
        if (Keyboard.current == null) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        _lockUntilFrame = Time.frameCount + 2;

        if (UIManager.Instance.PopupCount > 0)
        {
            // 팝업이 있으면 최상단 1개 닫기
            UIManager.Instance.ClosePopupUI();
        }
        else
        {
            // 팝업 없으면 현재 씬에 위임
            var scene = FindFirstObjectByType<BaseScene>();
            scene?.OnBackButton();
        }
    }
}
