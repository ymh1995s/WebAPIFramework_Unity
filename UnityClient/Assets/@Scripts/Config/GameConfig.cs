using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Config/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Store URLs")]
    // 안드로이드 스토어 URL — 강제 업데이트 시 Application.OpenURL에 전달
    // Inspector에서 Google Play 스토어 링크 입력 필요
    [SerializeField] private string androidStoreUrl = "";
    // iOS 스토어 URL — 강제 업데이트 시 Application.OpenURL에 전달
    // Inspector에서 App Store 링크 입력 필요
    [SerializeField] private string iosStoreUrl     = "";

    [Header("App Lifecycle")]
    // 백그라운드 복귀 시 토큰 갱신을 수행할 최소 경과 시간(초)
    // 이 임계값 미만으로 백그라운드에 있었으면 갱신을 생략한다
    [SerializeField] private int resumeThresholdSec = 60;

    [Header("Game Settings")]

    [Min(500)]
    [SerializeField]
    private int initialGold = 1000;

    [Range(1, 20)]
    [SerializeField]
    private int initialLevel = 1;

    public string AndroidStoreUrl    => androidStoreUrl;
    public string IosStoreUrl        => iosStoreUrl;
    public int    ResumeThresholdSec => resumeThresholdSec;
    public int    InitialGold        => initialGold;
    public int    InitialLevel       => initialLevel;
}
