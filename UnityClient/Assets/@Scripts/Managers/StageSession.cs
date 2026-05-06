// 씬 간 스테이지 선택 상태를 보관하는 정적 세션 클래스
// StageSelectScene에서 선택된 stageId를 GameScene이 읽는 용도로 사용한다
public static class StageSession
{
    // 현재 선택된 스테이지 ID — StageSelectScene에서 설정, GameScene에서 소비
    public static int StageId { get; set; }
}
