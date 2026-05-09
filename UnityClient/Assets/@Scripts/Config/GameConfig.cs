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
