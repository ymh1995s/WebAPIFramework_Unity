using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Config/GameConfig")]
public class GameConfig : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────────────
    // 스토어 URL — 강제 업데이트 시 Application.OpenURL 에 전달
    // 배포 전 실제 스토어 링크로 교체 필요
    // ─────────────────────────────────────────────────────────────────────
    public const string ANDROID_STORE_URL = "";
    public const string IOS_STORE_URL     = "";

    // 백그라운드 복귀 시 토큰 갱신을 수행할 최소 경과 시간(초)
    // 이 임계값 미만으로 백그라운드에 있었으면 갱신을 생략한다
    public const int ResumeThresholdSec = 60;


    [Header("Game Settings")]
    
    [Min(500)]
    [SerializeField]
    private int initialGold = 1000;

    [Range(1, 20)]
    [SerializeField]
    private int initialLevel = 1;

    public int InitialGold => initialGold;
    public int InitialLevel => initialLevel;
}
