# 08. 구현 순서 (Phase 분할 + DoD)

## 의존 그래프

```
[Phase 0: 기반 정정·확장]
   ApiConfig /api prefix 정정 + 8개 정적 클래스
   Define.EScene/EEventType 확장
   ApiError + EmptyResponse + JsonHelper
   ApiClient GET/POST/PUT/DELETE + 401/503/429 인터셉터
       │
       ▼
[Phase 1: WebFramework Api·Models 신설]
   AuthApi 확장 (Logout/Link/Resolve/Withdraw)
   8개 신규 Api (Version/Notice/Mail/Stage/Ranking/Inquiry/DailyLogin/Shout)
   대응 Models 9개 파일
   AuthManager.IsGoogleLinked
       │
       ▼
[Phase 2: 공통 팝업 디스패처]
   PopupService + 5종 일반 팝업 컨트롤러 (Error/Announcement/Maintenance/Update/Terms)
   UI_SelectPopup (충돌 해소/탈퇴 공유)
       │
       ▼
[Phase 3: BootstrapScene]
   Scene_Bootstrap + BootstrapFlow + BootstrapScene.unity + EScene 등록
   503 EventManager 구독 → PopupService.ShowMaintenance
       │
       ▼
[Phase 4: LoginScene 보완]
   Scene_Login + UI_LoginScene 에러 팝업 + 409 충돌 처리
   ApiConfig.Auth 정정 적용 (실제 호출 검증)
       │
       ▼
[Phase 5: MainScene]
   Scene_Main + UI_MainScene + 4개 도메인 팝업 컨트롤러
   진입 시 DailyLogin/Shout 자동 호출
   메일·공지·문의·랭킹·구글 연동·로그아웃·탈퇴 흐름 모두
       │
       ▼
[Phase 6: StageSelectScene]
   Scene_StageSelect + UI_StageSelectScene + StageButton 프리팹·스크립트
   StageSession 정적 슬롯
   /api/stages + /api/stages/progress 병렬 호출 + 머지
       │
       ▼
[Phase 7: GameScene]
   Scene_Game + UI_GameScene + UI_RewardPopup
   /api/stages/{id}/complete 본문 3필드 송신
   StageClearResponseDto 텍스트 빌드
       │
       ▼
[Phase 8: 정리]
   stub 3종 (UI_MainGame / UI_StageSelect / UI_InGame) 폐기
   Build Settings 5개 씬 정렬
   smoke test 시나리오 5종
```

## Phase별 DoD

### Phase 0 — 기반 정정·확장

- [ ] `ApiConfig.Auth`의 7개 경로를 `/api/auth/...` 로 정정 (기존 4개 + 신규 3개)
- [ ] `ApiConfig`에 8개 정적 클래스 추가 (Version/Notice/Mail/Stage/Ranking/Inquiry/DailyLogin/Shout)
- [ ] `Define.EScene`에 `BootstrapScene`, `StageSelectScene` 추가
- [ ] `Define.EEventType`에 `MaintenanceDetected` 추가 (`SessionExpired`도 권장)
- [ ] `WebFramework/Core/ApiError.cs` 신규
- [ ] `WebFramework/Core/JsonHelper.cs` 신규 (List wrapping)
- [ ] `WebFramework/Models/CommonModels.cs` — `EmptyResponse`
- [ ] `ApiClient`에 GET / POST(본문/무본문) / PUT / DELETE 메서드 추가, **기존 `Post<TReq,TRes>` 시그니처 보존**
- [ ] `ApiClient` 401 자동 회전 (SemaphoreSlim 직렬화, refresh 자체 가드)
- [ ] `ApiClient` 503 점검 인터셉터 → `EventManager.PublishMaintenance`
- [ ] `ApiClient` 429 자동 1회 백오프
- [ ] 컴파일 통과
- [ ] 기존 `AuthApi.GuestLogin` → 정상 200 응답 검증 (URL 정정 후)

### Phase 1 — WebFramework Api·Models 신설

- [ ] `AuthApi`에 `Logout`/`LinkGoogle`/`ResolveGoogleConflict`/`Withdraw` 추가
- [ ] 신규 8개 Api 클래스 (`Singleton<T>` 패턴)
- [ ] `AuthModels` 확장: `LinkGoogleRequest`, `ResolveGoogleConflictRequest`, `GoogleConflictResponse`, `PlayerSummary`
- [ ] 신규 9개 Models 파일
- [ ] `AuthManager.IsGoogleLinked` + `SetGoogleLinked` + PlayerPrefs 직렬화
- [ ] `AuthManager.LoadSavedToken/Clear` 가 IsGoogleLinked도 처리
- [ ] 임시 호출 테스트: 각 Api 메서드를 LoginScene 진입 후 한 번씩 호출하고 RestLogger 출력 확인

### Phase 2 — 공통 팝업 디스패처

- [ ] `PopupService.cs` 정적 클래스 (7개 메서드)
- [ ] 5개 일반 팝업 컨트롤러 (`UI_ErrorPopup`, `UI_AnnouncementPopup`, `UI_ServerMaintenancePopup`, `UI_UpdatePopup`, `UI_TermsPopup`)
- [ ] `UI_SelectPopup` 컨트롤러 (충돌 해소/탈퇴 공유)
- [ ] 각 컨트롤러가 자신의 프리팹 자식 이름과 enum 정합 (프리팹 자식 이름 매핑은 unity-programmer 수행)
- [ ] 임시 테스트: LoginScene에서 `PopupService.ShowError("test")` 호출 시 팝업 노출 + OK 시 BootstrapScene 진입

### Phase 3 — BootstrapScene

- [ ] `Scene_Bootstrap.cs` (BaseScene 상속)
- [ ] `Bootstrap/BootstrapFlow.cs` 정적 헬퍼
- [ ] `BootstrapScene.unity` 신설 + Scene_Bootstrap 부착
- [ ] Build Settings index 0 등록
- [ ] EventManager에 MaintenanceDetected 구독 등록 (BootstrapFlow 진입 시)
- [ ] 동작 검증:
  - [ ] 정상 응답 → 자동 로그인 또는 LoginScene 진입
  - [ ] 503 응답 → ServerMaintenancePopup → Confirm → Quit
  - [ ] 200 isForceUpdate=true → UpdatePopup → Confirm → 디버그 로그 + 진행
  - [ ] 200 + 신규 noticeId → AnnouncementPopup → 닫으면 진행
  - [ ] RefreshToken 보유 + Refresh 200 → MainScene
  - [ ] RefreshToken 보유 + Refresh 401 → AuthManager.Clear → LoginScene
  - [ ] 신규 + TermsAccepted=0 → TermsPopup → 동의 → LoginScene

### Phase 4 — LoginScene 보완

- [ ] `Scene_Login.cs` 신설 + LoginScene.unity 에 부착
- [ ] `UI_LoginScene.OnLoginError` → `PopupService.ShowError(ApiError)` 한 줄 추가
- [ ] 게스트 로그인 200 → `AuthManager.SetGoogleLinked(false)` → MainScene
- [ ] 구글 로그인 200 → `SetGoogleLinked(true)` → MainScene
- [ ] 구글 로그인 409 → UI_SelectPopup 충돌 본문 표시 → OK → ResolveGoogleConflict → MainScene
- [ ] DeviceId 영구 저장 + 폴백 검증

### Phase 5 — MainScene

- [ ] `Scene_Main.cs`
- [ ] `UI_MainScene.cs` + 11개 버튼 바인딩 (광고/인앱결제 비활성)
- [ ] 진입 직후 `DailyLoginApi.Process` + `ShoutApi.GetActive` 자동 호출
- [ ] `UI_MailPopup.cs` — 목록 + Exit + 전체 수령(N회 단건 호출)
- [ ] `UI_ConfirmPopup.cs` — 공지(단건) / 인벤토리("향후 제공" 안내) 공유
- [ ] `UI_InquiryPopup.cs` — 작성 + 목록
- [ ] `UI_SelectPopup.cs` 재사용 — 계정 삭제 확인
- [ ] `Utils/ItemFormatter.cs` 신설 — `FormatMailItem` / `FormatItems`
- [ ] 구글 연동 버튼 — `IsGoogleLinked=true`면 Debug.Log, false면 LinkGoogle (409 시 충돌 해소 흐름 재사용)
- [ ] 로그아웃 → 토큰 정리 + LoginScene
- [ ] 탈퇴 → 토큰·DeviceId·IsGoogleLinked 모두 정리 + LoginScene
- [ ] 모든 API 에러 → UI_ErrorPopup
- [ ] 9개 버튼 흐름 모두 동작 (수동 smoke test)

### Phase 6 — StageSelectScene

- [ ] `Scene_StageSelect.cs`
- [ ] `UI_StageSelectScene.cs` (병렬 호출 → 머지 → 동적 생성)
- [ ] `StageButton.prefab` + `StageButton.cs` (사용자 승인 항목)
- [ ] `Game/StageSession.cs` 정적 클래스
- [ ] 메인 메뉴 버튼
- [ ] 잠긴 스테이지(`isLocked=true`) 비활성 + LockIcon
- [ ] 클리어된 스테이지 BestScore/Stars 표시
- [ ] 선택 시 `StageSession.SelectedStageId` 설정 + GameScene 전환

### Phase 7 — GameScene

- [ ] `Scene_Game.cs`
- [ ] `UI_GameScene.cs` (StageLabelTxt, SuccessBtn, FailBtn)
- [ ] `UI_RewardPopup.cs` (보상 텍스트, OK)
- [ ] 성공: `StageApi.Complete(stageId, {score=1000, stars=3, clearTimeMs=30000}, ...)` → UI_RewardPopup → MainScene
- [ ] 실패: 즉시 MainScene
- [ ] 404/409 응답 → UI_AnnouncementPopup → MainScene
- [ ] `BuildRewardText` 헬퍼 텍스트 검증

### Phase 8 — 정리

- [ ] stub 3개 (`UI_MainGame.cs`, `UI_StageSelect.cs`, `UI_InGame.cs`) 폐기 — 새 컨트롤러 미참조 확인 후 삭제
- [ ] Build Settings 씬 5개 정렬: BootstrapScene → LoginScene → MainScene → StageSelectScene → GameScene
- [ ] smoke test 시나리오:
  - [ ] BootstrapScene → LoginScene → 게스트 로그인 → MainScene → 메일/공지/문의/랭킹/구글연동 → StageSelect → Game → 성공 → MainScene
  - [ ] BootstrapScene → 자동 로그인 → MainScene
  - [ ] BootstrapScene → 점검 응답 → 종료
  - [ ] BootstrapScene → 강제 업데이트 → 진행
  - [ ] 임의 위치에서 401 발생 → ApiClient 자동 회전 → 호출 성공 (수동 토큰 만료 트리거)
  - [ ] 임의 위치에서 503 발생 → MaintenancePopup → 종료

## unity-programmer 호출 단위 권장

위 9개 Phase를 각각 **개별 programmer 호출 + qa-reviewer 자동 루프** 단위로 분할. CLAUDE.md programmer 양식 준수, 변경 범위 최소화.

## 사용자 승인 필요 항목 (라운드 진입 전 일괄 확인)

orchestrator(메인 컨텍스트)가 사용자에게 받을 항목:

1. **백엔드 부재 3건 결정 (07 §3 결론)**
   - 인벤토리 — 본 라운드는 안내 표시만, 별도 백엔드 라운드(`GET /api/inventory`)를 제안할지
   - 공지 리스트 — 단건 표시로 진행 (백엔드 신설 불필요)
   - 계정 상태(`isGoogleLinked`) — 클라 캐시로 진행, `GET /api/auth/me` 신설을 별도 라운드로 제안할지
2. **신설 백엔드 endpoint 2개**(우선순위 1·2) 별도 라운드 진행 여부
3. **401 자동 회전 본 라운드 포함** (이전 라운드는 별도로 미루기로 했으나 본 라운드 결정으로 포함)
4. **`StageSession` 정적 슬롯** 채택 (PlayerPrefs 폐기)
5. **`ApiClient` 콜백 시그니처 변경**: `Action<string>` → `Action<ApiError>` (기존 AuthApi 호출처도 영향 — `UI_LoginScene` 1곳)
6. **JsonUtility 유지** (Newtonsoft 도입은 별도 라운드 박제)
7. 이전 라운드 승인 항목 재확인:
   - BootstrapScene 신설
   - 약관 팝업 = 자동 로그인 분기 종료 후 LoginScene 직전
   - StageButton 프리팹 신설
   - UI_MainGame / UI_StageSelect / UI_InGame stub 3개 폐기

## 신설 백엔드 endpoint 권고 (사용자 작업 분량)

| 우선순위 | Method | Path | 응답 | 분량 |
|---|---|---|---|---|
| 1 | GET | `/api/inventory` | `[{itemId, itemName, quantity}]` | 컨트롤러 1 + 서비스 메서드 1 + DTO 1 |
| 2 | GET | `/api/auth/me` | `{playerId, nickname, level, isGoogleLinked, createdAt}` | 컨트롤러 1 + 서비스 메서드 1 + DTO 1 |

도입 시 클라 영향:
- 우선순위 1 → 03 MainScene §2.4 폐기, `InventoryApi`/`InventoryModels` 신규, 인벤토리 버튼 흐름 정상화 (작은 변경)
- 우선순위 2 → 03 MainScene §2.6/2.7 단순화, 닉네임 정확 표기, IsGoogleLinked 신뢰 가능 (작은 변경)
