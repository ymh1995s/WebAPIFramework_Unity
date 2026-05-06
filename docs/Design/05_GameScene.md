# 05. GameScene

## 1. 요구사항

요구사항.md `## GameScene`:
- 텍스트: 현재 몇 스테이지인지 표기
- 성공 버튼: 클리어 요청 → `UI_RewardPopup` → 보상 텍스트 → OK 시 MainScene
- 실패 버튼: MainScene 전환

CLIENT_GUIDE 17번(스테이지 클리어).

## 2. 1차 대비 변경점

### 2.1 클리어 요청 본문 정정 (필수)

**1차 가정**: `Complete(stageId)` 본문 없음. **실측 (`StageClearRequestDto`)**:
```json
{ "score": 9500, "stars": 3, "clearTimeMs": 45000 }
```
- score ≥ 0
- stars 0~3
- clearTimeMs ≥ 0

요구사항이 "성공 버튼만 누르면 클리어" 더미 흐름이므로 **고정값**으로 송신:
```csharp
new StageClearRequestDto { score = 1000, stars = 3, clearTimeMs = 30_000 }
```
이 값은 `Define.cs` 또는 `StageSession`에 상수로 박제 권장. 추후 실제 게임 로직에서 측정값으로 교체.

### 2.2 응답 구조 정정 (`StageClearResponseDto`)

**실측**:
```json
{
  "isFirstClear": true,
  "clearCount": 1,
  "expGranted": 100,
  "firstRewardMessage": "최초 클리어 보상이 우편함에 발송되었습니다",
  "replayRewardMessage": null
}
```

**핵심**: 응답에 **보상 아이템 배열이 직접 들어오지 않음**. 보상은 **우편함을 통해 발송됨**(`firstRewardMessage`/`replayRewardMessage`가 안내 텍스트).

**1차 설계 오류**: `result.rewards` 배열 가정. 폐기.

**정정**: `UI_RewardPopup`에 표시할 텍스트는 다음을 조합:
```
isFirstClear=true → "최초 클리어!"
isFirstClear=false → $"클리어 {clearCount}회"
"경험치 +{expGranted}"
firstRewardMessage / replayRewardMessage 중 비어있지 않은 메시지 출력
```

실제 보상 아이템 확인은 사용자가 OK 후 메인의 메일 박스 버튼으로 확인. UX는 `UI_RewardPopup` 텍스트 아래에 "보상은 우편함에서 확인하세요" 안내 추가.

### 2.3 에러 처리 매트릭스 (CLIENT_GUIDE 17번)

| Status | 처리 |
|---|---|
| 200 | UI_RewardPopup 표시 |
| 401 | 토큰 자동 회전 (ApiClient 인터셉터) |
| 404 | 스테이지 마스터 없음/비활성 → "스테이지가 변경되었습니다" 안내 후 MainScene |
| 409 | 선행 스테이지 미클리어 → "잠금 상태" 안내 후 MainScene |

`UI_ErrorPopup` 또는 `UI_AnnouncementPopup`로 분기. 단순화 위해 모두 `UI_ErrorPopup` (OK → 게임 재시작)으로 통일해도 무방하지만, 4xx 비즈니스 응답은 게임 재시작이 과함 → `UI_AnnouncementPopup` + OK 시 MainScene 권장.

### 2.4 SelectedStageId 출처 — `StageSession` 정적

04 문서 결정 반영. `PlayerPrefs` 폐기.
```csharp
int stageId = StageSession.SelectedStageId;
if (stageId == 0) { /* 잘못된 진입 — MainScene로 fallback */ }
```

## 3. 씬 구성

- `GameScene.unity` 루트에 `Scene_Game` 부착
- `Scene_Game`이 `UI_GameScene` 인스턴싱

## 4. UI 구조

`UI_GameScene` (UI_UGUI + IUI_Scene):
```
Texts enum:    { StageLabelTxt }
Buttons enum:  { SuccessBtn, FailBtn }
```

`UI_RewardPopup` (UI_UGUI + IUI_Popup):
```
Texts enum:    { RewardLabelTxt }
Buttons enum:  { OkBtn }
public void SetText(string)
public Action OnOk;
```

## 5. WebFramework 매핑

| 기능 | Endpoint | Body | 응답 | 인증 |
|---|---|---|---|---|
| 스테이지 클리어 | `POST /api/stages/{id}/complete` | `{score, stars, clearTimeMs}` | `StageClearResponseDto` | O |

## 6. 데이터 흐름

```
[진입]
Scene_Game.Start
  → int stageId = StageSession.SelectedStageId
  → UI_GameScene.SetStageLabel($"Stage {stageId}")

[성공]
SuccessBtn → StageApi.Complete(stageId, fixedDummyBody, ...)
           → 200 StageClearResponseDto
           → UI_RewardPopup.SetText(BuildRewardText(res)) + show
           → OkBtn → SceneManager.LoadScene(MainScene)
           → 4xx/5xx → PopupService.ShowAnnouncement → MainScene

[실패]
FailBtn → SceneManager.LoadScene(MainScene)
```

`BuildRewardText` 헬퍼:
```csharp
public static string BuildRewardText(StageClearResponseDto r)
{
    var lines = new List<string>();
    lines.Add(r.isFirstClear ? "최초 클리어!" : $"클리어 {r.clearCount}회");
    if (r.expGranted > 0) lines.Add($"경험치 +{r.expGranted}");
    if (!string.IsNullOrEmpty(r.firstRewardMessage))  lines.Add(r.firstRewardMessage);
    if (!string.IsNullOrEmpty(r.replayRewardMessage)) lines.Add(r.replayRewardMessage);
    lines.Add("보상은 우편함에서 확인하세요.");
    return string.Join("\n", lines);
}
```

## 7. 영향받는 파일

**신규**
- `Scenes/Scene_Game.cs`
- `UI/UI_GameScene.cs`
- `UI/UI_RewardPopup.cs`
- (StageApi/StageModels는 04 문서에서 이미 정의 — `Complete` 메서드만 추가)

**수정 없음** (StageApi에 Complete 메서드만 합쳐지면 됨)

## 8. 트레이드오프 — 더미 클리어 값

| 옵션 | 장점 | 단점 |
|---|---|---|
| A. 고정값 송신 (채택) | 단순, 백엔드 검증 통과 보장 | 점수 분산 부족 — 랭킹 검증이 무의미 |
| B. 랜덤값 (예: score=Random(1000~10000)) | 랭킹·통계 다양성 확보 | 더미 단계 의도 흐려짐 |

**A 채택, 단 `StageSession`에 `LastClearScore` 같은 슬롯을 두어 추후 측정값 교체 지점만 명확히 박제**.

## 9. 위험 요소

- `StageSession.SelectedStageId == 0` 으로 GameScene이 직접 진입되는 비정상 흐름 방어 필요. `Awake`에서 검증 후 MainScene으로 fallback.
- `clearTimeMs`는 long이 아니라 int. 백엔드 DTO 확인 시 long이면 모델 정정 필요(실측 컨트롤러는 `[FromBody]` 위임이라 DTO 정의가 결정). **확인 필요**: `Framework.Application/Content/Stage/StageClearRequestDto`의 `ClearTimeMs` 타입.

## 10. DoD

- [ ] StageLabelTxt에 `Stage {N}` 표기
- [ ] 성공 버튼 → `/api/stages/{id}/complete` 호출 + 본문 3필드 송신
- [ ] 200 응답 → UI_RewardPopup에 BuildRewardText 결과 표시
- [ ] OK → MainScene 전환
- [ ] 실패 버튼 → 즉시 MainScene
- [ ] 404/409 → 안내 팝업 후 MainScene
- [ ] 401 → ApiClient 자동 회전 (호출처 코드 변경 없음)
