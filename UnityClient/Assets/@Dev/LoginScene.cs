using UnityEngine;

// 로그인 씬 초기화 - 리소스 로드 완료 후 로그인 UI 대기
public class LoginScene : BaseScene
{
    protected override void Awake()
    {
        base.Awake();

        SceneType = Define.EScene.LoginScene;

        // 리소스 및 데이터 선행 로드
        ResourceManager.Instance.LoadAll(OnProgress, OnComplete);
    }

    // 로드 진행률 로그
    void OnProgress(float value)
    {
        Debug.Log($"Loading Progress: {value * 100}%");
    }

    // 로드 완료 - 씬 전환 없이 로그인 UI 대기
    void OnComplete()
    {
        Debug.Log($"Loading Complete");
        DataManager.Instance.LoadData();
        // 씬 전환은 UI_LoginScene에서 로그인 성공 후 처리
    }
}
