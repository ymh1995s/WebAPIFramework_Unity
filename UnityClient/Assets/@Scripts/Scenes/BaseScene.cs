using UnityEngine;

public class BaseScene : MonoBehaviour
{
    public Define.EScene SceneType { get; protected set; } = Define.EScene.Unknown;

    protected virtual void Awake()
    {
        // TODO
    }

    // 뒤로가기 입력 시 BackButtonHandler가 호출 — 씬별로 오버라이드
    public virtual void OnBackButton() { }
}
