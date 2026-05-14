using System;

// 스테이지 마스터 데이터 DTO — 스테이지 정의 정보 (잠금 여부는 progress 참조)
[Serializable]
public class StageMasterDto
{
    // 스테이지 고유 ID
    public int    id;

    // 스테이지 코드 (예: "STAGE_001")
    public string code;

    // 스테이지 표시 이름
    public string name;

    // 클리어 보상 테이블 코드 — 서버 string?(null=보상 없음)
    // JsonUtility는 null을 ""로 디시리얼라이즈하므로 "보상 없음" 판정은 string.IsNullOrEmpty() 사용
    public string rewardTableCode;

    // 재도전 보상 테이블 코드 — 서버 string?(null=재도전 보상 없음)
    public string rePlayRewardTableCode;

    // 재도전 보상 감쇠율 (퍼센트, 0~100) — 누적 클리어마다 보상 감소
    public int    rePlayRewardDecayPercent;

    // 클리어 시 획득 경험치
    public int    expReward;

    // 선행 스테이지 ID — null이면 JsonUtility가 0으로 채움. 0=선행 없음으로 합의
    // 잠금 여부 판정은 StageProgressDto.isLocked를 신뢰할 것
    public int    requiredPrevStageId;

    // 활성화 여부 — false이면 클라이언트에서 숨김 처리
    public bool   isActive;

    // 정렬 순서
    public int    sortOrder;
}

// 스테이지 진행 현황 DTO — 플레이어별 클리어 이력 및 잠금 상태
[Serializable]
public class StageProgressDto
{
    // 스테이지 고유 ID
    public int    stageId;

    // 스테이지 코드
    public string code;

    // 스테이지 표시 이름
    public string name;

    // 클리어 여부
    public bool   isCleared;

    // 누적 클리어 횟수
    public int    clearCount;

    // 최고 점수
    public int    bestScore;

    // 최고 별 개수 (0~3)
    public int    bestStars;

    // 최고 클리어 시간 (밀리초)
    public long   bestClearTimeMs;

    // 잠금 여부 — 선행 스테이지 미클리어 시 true
    public bool   isLocked;

    // 정렬 순서
    public int    sortOrder;
}

// 스테이지 클리어 요청 DTO — 클리어 결과 데이터 전송
[Serializable]
public class StageClearRequest
{
    // 획득 점수
    public int  score;

    // 획득 별 개수 (0~3)
    public int  stars;

    // 클리어 소요 시간 (밀리초)
    public long clearTimeMs;
}

// 스테이지 클리어 응답 DTO — 서버가 산정한 보상 정보
[Serializable]
public class StageClearResponse
{
    // 최초 클리어 여부 — true이면 firstRewardMessage 표시
    public bool   isFirstClear;

    // 누적 클리어 횟수 (갱신 후 값)
    public int    clearCount;

    // 이번 클리어로 지급된 경험치
    public int    expGranted;

    // 최초 클리어 보상 안내 메시지 (isFirstClear=true 시 유효)
    public string firstRewardMessage;

    // 재도전 보상 안내 메시지 (isFirstClear=false 시 유효)
    public string replayRewardMessage;
}
