# 04. StageSelectScene

## 1. 요구사항

요구사항.md `## StageSelectScene`:
- 각 스테이지 버튼 → GameScene 전환. **"실제 서비스는 더 복잡할테니 적절한 프레임워크를 설계할 것"**
- 메인 메뉴 버튼 → MainScene 전환

CLIENT_GUIDE 15번(활성 스테이지 목록), 16번(내 진행 현황).

## 2. 1차 대비 변경점

### 2.1 마스터와 진행을 분리 호출

**1차 가정**: `GET /api/stages` 응답에 `isCleared`/`isUnlocked` 포함. **실측: 분리됨**.

| Endpoint | 응답 |
|---|---|
| `GET /api/stages` | `[{id, code, name, rewardTableCode, rePlayRewardTableCode, rePlayRewardDecayPercent, expReward, requiredPrevStageId, isActive, sortOrder}, ...]` |
| `GET /api/stages/progress` | `[{stageId, code, name, isCleared, clearCount, bestScore, bestStars, bestClearTimeMs, isLocked, sortOrder}, ...]` |

**채택 흐름**: 진입 시 두 API를 **병렬 호출** → 응답 둘 다 받으면 `stageId` 키로 머지 → `StageButton` 동적 생성.

코루틴/UniTask가 도입되지 않은 현재 ApiClient 구조에선 두 콜백을 카운트하는 단순 패턴:
```
int pending = 2;
List<StageMasterDto> masters = null;
List<StageProgressDto> progresses = null;
StageApi.Instance.GetMasters(m => { masters = m; if (--pending == 0) Render(); });
StageApi.Instance.GetProgress(p => { progresses = p; if (--pending == 0) Render(); });
```

> 진행이 빈 배열(신규 플레이어)이면 모두 잠금/0 상태로 렌더. 잠금 판정은 `progress.isLocked` 신뢰. 마스터의 `requiredPrevStageId`는 **참고용 표시**만(잠금 판정 권한은 서버).

### 2.2 신규 플레이어 — 진행 응답 비어 있을 때

신규는 `progress` 배열이 비어 있을 가능성. 마스터에는 N개가 있는데 진행에 없으면 클라가 어떻게 잠금/해제를 표시할지가 모호.

**결정**: 진행 응답에 누락된 `stageId`는 **가장 첫 스테이지(sortOrder 1)만 잠금 해제, 나머지는 잠금**으로 가정. 첫 스테이지 클리어 후 progress에 행이 추가되어 다음 스테이지가 자동 해제됨(서버 로직). 클라는 매 진입 시 progress 재조회만 충실히 하면 되며 자체 잠금 판정 로직은 두지 않는다.

> 위 "첫 스테이지만 해제" 가정은 백엔드 정책에 의존. 만약 서버가 첫 진입 시 progress에 모든 스테이지의 `isLocked=true/false`를 채워 보낸다면 클라가 단순화됨. 본 가정은 fallback이며, 실측 검증 필요(로그 확인).

### 2.3 SelectedStage 전달 방식 — `static` 정적 변수 채택

**1차 가정**: PlayerPrefs로 전달.

**재평가**: 씬 간 단일 int 전달은 PlayerPrefs로 가능하지만, 매번 디스크 I/O가 발생 + 외부 스코프(앱 재시작 시 잔존)로 누출. 더 단순한 방식은 **정적 메모리 슬롯**.

```csharp
public static class StageSession
{
    public static int SelectedStageId { get; set; }
    public static StageProgressDto SelectedProgress { get; set; }
}
```

이유:
- GameScene이 클리어 호출 시 `clearTimeMs` 측정 시작점 등도 함께 보관 가능
- 앱 재시작 후엔 자연스럽게 초기화 (PlayerPrefs는 잔존하여 의도와 다름)
- 디스크 I/O 없음

PlayerPrefs는 여전히 영구 보관이 필요한 항목(DeviceId, RefreshToken, IsGoogleLinked, lastSeenNoticeId, TermsAccepted)에만 사용.

### 2.4 StageButton 프리팹 (이전 라운드 승인됨, 유지)

`Resources/PreLoad/Prefabs/UI/StageButton.prefab` — 이전 라운드 사용자 승인. 유지.

구성:
- `Button` (UI)
- 자식 `TMP_Text NameTxt` ("1-1 시작")
- 자식 `Image LockIcon` (잠금 시만 활성)
- 자식 `TMP_Text BestScoreTxt` (클리어 시만 활성, "★3 / 9500")

`StageButton.cs` (UI_Base 미상속, 단순 MonoBehaviour):
```csharp
public class StageButton : MonoBehaviour
{
    [SerializeField] TMP_Text nameTxt;
    [SerializeField] TMP_Text bestScoreTxt;
    [SerializeField] GameObject lockIcon;
    [SerializeField] Button button;

    public void Bind(StageMasterDto master, StageProgressDto progress, Action onClick) { ... }
}
```

## 3. 씬 구성

- `StageSelectScene.unity` 루트에 `Scene_StageSelect` 부착
- `Scene_StageSelect`가 `UI_StageSelectScene` 인스턴싱 + 두 API 병렬 호출 트리거

## 4. UI 구조

`UI_StageSelectScene`(UI_UGUI + IUI_Scene):
```
Buttons enum:  { MainMenuBtn }
자식: ScrollView/Viewport/Content (StageButton들 부모)
```
- 진입 시 ScrollView/Content에 `StageButton` N개 동적 인스턴싱(`ResourceManager.Instantiate("StageButton")`)
- MainMenuBtn → `SceneManager.LoadScene(MainScene)`

## 5. WebFramework 매핑

| 기능 | Endpoint (Method) | 인증 | 응답 |
|---|---|---|---|
| 활성 스테이지 마스터 | `GET /api/stages` | O | `List<StageMasterDto>` |
| 내 진행 현황 | `GET /api/stages/progress` | O | `List<StageProgressDto>` |

신규 클래스: `StageApi` (Singleton), `StageMasterDto`, `StageProgressDto`, `StageSession`.

## 6. 데이터 흐름

```
[진입]
Scene_StageSelect.Start
  → UI_StageSelectScene.Init
  → 병렬 호출:
       StageApi.GetMasters(...)
       StageApi.GetProgress(...)
  → 둘 다 도착 → 머지 → StageButton N개 생성

[StageButton 클릭]
StageSession.SelectedStageId = master.id
StageSession.SelectedProgress = progress (또는 null)
SceneManager.LoadScene(GameScene)

[MainMenuBtn]
SceneManager.LoadScene(MainScene)
```

## 7. 영향받는 파일/에셋

**신규**
- `Scenes/Scene_StageSelect.cs`
- `UI/UI_StageSelectScene.cs`
- `UI/StageButton.cs`
- `Resources/PreLoad/Prefabs/UI/StageButton.prefab` (사용자 승인됨)
- `WebFramework/Api/StageApi.cs`
- `WebFramework/Models/StageModels.cs`
- `Game/StageSession.cs` (정적, 신규 — 또는 `Utils/`)

**수정**
- `Utils/Define.cs` — `EScene.StageSelectScene` 추가

## 8. 트레이드오프 — 마스터/진행 분리 vs 통합

| 옵션 | 장점 | 단점 |
|---|---|---|
| A. **분리 호출 (실측)** | 백엔드 변경 없음 / 마스터 캐시 가능 | 클라가 머지 책임 짐. 응답 2회 |
| B. 백엔드에 머지 endpoint 신설 | 클라 단순 | 백엔드 작업 필요 + 책임 분리 무너짐 |

**A 채택**. 머지 비용은 N개 ID 매핑 한 번이라 미미.

## 9. 위험 요소

- ScrollView Content의 RectTransform/Layout Group 설정은 unity-programmer가 prefab 인스펙터에서 수동 매핑(설계 박제 범위 외).
- `StageMasterDto.requiredPrevStageId` 가 null인 첫 스테이지는 무조건 잠금 해제로 그릴지, 서버 progress.isLocked만 따를지 결정 필요. **서버 응답 신뢰 우선**(클라 자체 판정 금지).

## 10. DoD

- [ ] 진입 시 `/api/stages` + `/api/stages/progress` 병렬 호출
- [ ] 응답 머지 후 sortOrder 순으로 StageButton N개 동적 생성
- [ ] 잠긴 스테이지(`isLocked=true`)는 Interactable=false + LockIcon 활성
- [ ] 클리어된 스테이지는 BestScore/Stars 표시
- [ ] 스테이지 클릭 시 `StageSession.SelectedStageId` 설정 + GameScene 전환
- [ ] 메인 메뉴 버튼 → MainScene 전환
- [ ] 4xx/5xx → UI_ErrorPopup
