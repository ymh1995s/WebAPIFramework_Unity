# GameClient Dev Notes

## [주의] 배포 전 교체 항목

### Google OAuth - Android 서명 키 등록

현재 Google Cloud Console에 **debug.keystore**의 SHA-1 지문으로 Android OAuth 클라이언트 ID가 등록되어 있음.

| 항목 | 현재 값 (개발용) | 출시 시 |
|------|-----------------|---------|
| SHA-1 지문 | `BA:19:19:19:9D:44:5F:0E:79:DE:AF:0A:F7:A6:FC:67:91:75:8C:0B` | release keystore에서 새로 추출 |
| OAuth 클라이언트 이름 | WebFramework Android Debug | release용 별도 생성 필요 |

**출시 시 할 일:**
1. release keystore 생성
2. release keystore SHA-1 추출
3. Google Cloud Console에서 Android OAuth 클라이언트 ID 추가 (release용)

### SHA-1 추출 명령어
```
keytool -list -v -keystore <keystore경로> -alias <alias> -storepass <password>
```

---

## 기능 현황

| 기능 | 상태 | 비고 |
|------|------|------|
| 게스트 로그인 | 완료 | DeviceId 기반 |
| 구글 로그인 | 구현완료/테스트필요 | google-signin-unity SDK, 실기기 빌드 필요 |

---

## [법적 의무] 탈퇴 고지 텍스트 관리

**위치**: `UI_MainGame.cs` `OnClickWithdraw()` 팝업 메시지 문자열 (하드코딩)

**법적 근거**: 개인정보보호법 §22 / Google Play User Data Policy / Apple App Store Review 5.1.1(v)
**상세 기준**: `CLIENT_GUIDE.md` 10번 + 부록 A 의무 동작 체크리스트

**고지 항목 5개** — 서버 `PlayerWithdrawalCleaner.PurgeGameDataAsync` 처리 범위에서 도출:

| # | 고지 내용 | 서버 처리 |
|---|---|---|
| 1 | 인게임 프로필·보유 아이템·스테이지 진행도 영구 삭제 | `PlayerProfile`, `PlayerItem`, `StageClear` hard delete |
| 2 | 우편함 및 미수령 보상 전부 소실 | `Mail`, `MailItem` hard delete |
| 3 | 결제로 획득한 아이템·보상 소실 (결제 이력 자체는 법적 의무로 5년 보관) | `IapPurchase` 보존(전자상거래법), 아이템은 `PlayerItem` 삭제 |
| 4 | 재가입 시 기존 데이터 복구 불가 | 모든 게임 데이터 즉시 hard delete |
| 5 | 개인정보(기기 ID·구글 계정·닉네임) 즉시 익명화 | `DeviceId`/`GoogleId` → null, 닉네임 → `"탈퇴유저-{id}"` |

**변경 트리거**: 서버 `WithdrawAsync` 처리 범위 변경(새 테이블 추가·삭제) 시 이 텍스트도 동반 업데이트 필수.

**하드코딩 의도**: 법적 고지 텍스트를 JSON/설정 파일로 분리하면 실수 수정·누락 위험이 높아짐 — 의도적 코드 변경(코드 리뷰 경유)만 허용.

---

## [미구현] 프레임워크 공통 시스템 (2026-05-09)

> 아래 5개 항목은 모바일 게임 프레임워크로서 모든 프로젝트에 공통 적용되는 필수 기반 시스템이다.
> "다음 게임에도 그대로 쓸 수 있는가"를 기준으로 선별. 구현 순서는 아래 번호 순서를 권장한다.

---

### 1. 씬 전환 로딩 화면 + 페이드 인/아웃

**목적**: 씬 전환 시 빈 화면이 순간 노출되는 문제 해소 + 비동기 로딩 진행률 표시 + 전환 연출 통일.

**현황**: `SceneManager`가 씬 전환을 수행하지만, 전환 중 화면 처리(로딩 UI 표시, 진행률 반영, 페이드 연출)가 전혀 없다. 씬 전환 시 Unity 기본 빈 화면이 그대로 노출됨.

**구현 범위**:

1. **FadeManager (신규 싱글톤)** — `Managers/FadeManager.cs`
   - `FadeIn(float duration)` / `FadeOut(float duration)` 코루틴 제공
   - DontDestroyOnLoad Canvas 위에 전체화면 검정 Image(`CanvasGroup`) 배치
   - alpha 0→1(FadeOut), 1→0(FadeIn) 코루틴으로 처리
   - `FadeAndLoadScene(EScene scene, float fadeTime)` 통합 메서드 — FadeOut → 씬 로드 → FadeIn 순서 보장
   - 중복 호출 방지: 페이드 진행 중 재호출 무시 (bool `_isFading` 플래그)

2. **LoadingScene 신규 씬 + UI_LoadingScene (신규)**
   - `EScene.Loading` 항목을 `Utils/Define.cs`에 추가
   - `LoadingScene`은 DontDestroyOnLoad 오브젝트들이 이미 존재하는 상태로 진입하는 경유 씬
   - `UI_LoadingScene`에 진행률 바(Slider 또는 Image fillAmount)와 로딩 텍스트 표시
   - `SceneManager`의 씬 전환 메서드가 LoadingScene을 경유하도록 수정:
     - FadeOut → LoadingScene 진입 → 실제 씬 비동기 로드(`LoadSceneAsync`, `allowSceneActivation=false`) → 진행률 90% 도달 시 `allowSceneActivation=true` → FadeIn
   - 로딩 중 최소 표시 시간(0.5초 등) 보장 옵션 — 너무 빠른 씬 전환 시 화면 깜빡임 방지

3. **SceneManager 수정** — `Managers/SceneManager.cs`
   - 기존 `LoadScene(EScene)` 내부에서 FadeManager 호출로 교체
   - 로딩 화면 없이 즉시 전환하는 `LoadSceneImmediate(EScene)` 오버로드 유지 (Bootstrap 부팅 흐름 등 내부용)

**주의사항**:
- FadeManager의 Canvas sortOrder를 가장 높게 설정해야 다른 UI 위에 페이드 레이어가 올라옴
- DontDestroyOnLoad 씬에서 FadeManager Canvas가 이미 존재하는데 LoadingScene의 UI와 겹치지 않도록 레이어 순서 관리 필요
- `allowSceneActivation=false` 상태의 AsyncOperation은 진행률이 0.9에서 멈춤 — 90%를 100%로 보정해서 UI에 표시할 것

---

### 2. Android 뒤로 가기 스택 (Back Button)

**목적**: Android 물리/제스처 뒤로가기 버튼 처리. 미구현 시 OS가 앱을 강제 종료하거나 예기치 않은 씬 이탈 발생.

**현황**: 처리 코드 전무. `Input.GetKeyDown(KeyCode.Escape)`에 대한 대응 없음.

**구현 범위**:

1. **UIManager 팝업 스택 관리** — `Managers/UIManager.cs`
   - 현재 UIManager가 팝업을 관리하고 있으나 스택(순서) 개념이 있는지 확인 필요
   - `_popupStack (Stack<UI_Popup>)` 자료구조 유지 — 팝업 열릴 때 Push, 닫힐 때 Pop
   - `CloseTopPopup()` 메서드 추가 — 스택 최상단 팝업 닫기

2. **BackButtonHandler (BaseScene에 통합 권장)** — 씬마다 동작이 다르므로 BaseScene에 가상 메서드로 처리
   - `BaseScene`에 `protected virtual void OnBackButton()` 추가
   - 매 프레임 `Input.GetKeyDown(KeyCode.Escape)` 감지 → UIManager에 열린 팝업 있으면 `UIManager.Instance.CloseTopPopup()` 호출 → 팝업 없으면 `OnBackButton()` 위임
   - `LoginScene` 오버라이드: "게임을 종료하시겠습니까?" 확인 팝업 → 확인 시 `Application.Quit()`
   - `GameScene` 오버라이드: StageSelect 씬으로 이동 (또는 인게임 중 일시정지 메뉴)
   - `BootstrapScene`은 뒤로가기 무시 (부팅 중 종료 방지)

3. **공통 확인 팝업 — 구현 완료** (5b94092)
   - `UI_ConfirmPopup` (단일 확인 버튼) + `UI_WithdrawPopup` (확인/취소 두 버튼) 두 컴포넌트로 분리 구현됨
   - `PopupService`에서 타입별 메서드로 접근: `ShowError` / `ShowAnnouncement` / `ShowMaintenance` / `ShowUpdate` / `ShowReward` / `ShowBanned` / `ShowSelect`
   - 뒤로가기 종료 확인은 `PopupService.ShowSelect("게임을 종료하시겠습니까?", onOk, onCancel)` 사용

**주의사항**:
- New Input System을 사용 중이라면 `Keyboard.current.escapeKey.wasPressedThisFrame` 사용
- iOS는 뒤로가기 버튼이 없으므로 `#if UNITY_ANDROID` 감싸기 불필요 — iOS에서는 Escape 키 자체가 발생하지 않음
- `Application.Quit()`은 에디터에서 동작하지 않지만 그냥 호출해도 무방 (에디터에서 무시됨)

---

### 3. Safe Area 처리

**목적**: 아이폰 노치/Dynamic Island, Android 펀치홀/카메라 컷아웃, 홈 인디케이터 영역과 UI가 겹치는 문제 방지. 미처리 시 마켓 심사 거절 사유가 됨.

**현황**: UI가 전체 화면을 기준으로 배치되어 있음. `Screen.safeArea` 기반 조정 코드 없음.

**구현 범위**:

1. **SafeAreaFitter (신규 MonoBehaviour)** — `UI/SafeAreaFitter.cs`
   - `Awake()`에서 `Screen.safeArea`를 읽어 부착된 `RectTransform`의 `offsetMin`/`offsetMax`를 조정
   - 계산식: `safeArea.x / Screen.width` 등 정규화 비율로 앵커 또는 오프셋 적용
   - 씬 전환 후에도 작동하도록 `Awake`에서 처리
   - 화면 회전 감지: `Screen.orientation` 변경 시 재계산 (`Update`에서 이전 값과 비교)

2. **적용 방법**:
   - 각 씬의 최상위 Canvas 바로 아래에 "SafeArea" 빈 RectTransform 오브젝트 생성
   - `SafeAreaFitter` 컴포넌트 부착
   - 모든 씬 UI 요소를 SafeArea 오브젝트 하위로 배치
   - 배경 이미지 등 전체 화면을 꽉 채워야 하는 요소는 SafeArea 바깥(Canvas 직속)에 배치

3. **에디터 시뮬레이션**:
   - Unity Device Simulator 패키지로 노치 기기 시뮬레이션 가능
   - 개발용 `SafeAreaSimulator` 에디터 컴포넌트 고려 (특정 safe area를 강제 설정해 에디터에서 테스트)

**주의사항**:
- `Screen.safeArea`는 런타임에만 정확한 값을 반환함 — 에디터에서는 항상 전체 화면 반환
- 팝업은 DontDestroyOnLoad Canvas에 올라가므로 해당 Canvas에도 SafeArea 오브젝트 적용 필요
- 최소 iPhone 14 Pro(Dynamic Island), Galaxy S 시리즈(펀치홀) 시뮬레이션으로 확인

---

### 4. 앱 생명주기 처리 (OnApplicationPause)

**목적**: 백그라운드 진입/복귀 시 저장, 오디오, 세션 관리. 미처리 시 세션 만료 후 복귀 시 API 오류 + 자동 저장 미실행으로 데이터 유실 가능.

**현황**: `SaveManager`에 자동 저장 기능이 있으나 앱 생명주기 이벤트와 연동되지 않음. `AuthManager`의 토큰은 포그라운드 복귀 시 유효성 재확인 없음.

**구현 범위**:

1. **AppLifecycleManager (신규 싱글톤)** — `Managers/AppLifecycleManager.cs`
   - `Singleton<AppLifecycleManager>` 상속, DontDestroyOnLoad
   - `OnApplicationPause(bool pauseStatus)` 구현:
     - `pauseStatus == true` (백그라운드 진입):
       - `SoundManager.Instance.PauseBGM()` 호출 (BGM 정지)
       - `SaveManager.Instance.SaveGame()` 호출 (즉시 저장)
       - `_backgroundEnteredAt = Time.realtimeSinceStartup` (복귀 시 경과 시간 계산용)
     - `pauseStatus == false` (포그라운드 복귀):
       - `SoundManager.Instance.ResumeBGM()` 호출 (BGM 재개)
       - 경과 시간 계산: `float elapsed = Time.realtimeSinceStartup - _backgroundEnteredAt`
       - 임계값(예: 30분 = 1800f) 초과 시 → `AuthManager.Instance.TryRefreshTokenAsync()` 호출
       - 갱신 실패 시 → LoginScene으로 강제 이동 + "세션이 만료되었습니다" 토스트

2. **SoundManager 연동** — `Managers/SoundManager.cs`
   - `PauseBGM()` / `ResumeBGM()` 메서드 없으면 추가 (`AudioSource.Pause()` / `AudioSource.UnPause()`)

3. **임계값 설정**: `GameConfig` 또는 `AppLifecycleManager` 상수로 관리 (`const float SESSION_TIMEOUT_SEC = 1800f`)

**주의사항**:
- `OnApplicationFocus`와 `OnApplicationPause`는 플랫폼별로 호출 순서/타이밍이 다름 — `OnApplicationPause`만으로 처리하는 것이 크로스플랫폼에서 더 일관성 있음
- `Time.realtimeSinceStartup`은 앱이 백그라운드에 있어도 계속 증가하므로 경과 시간 계산에 적합
- `TryRefreshTokenAsync`가 이미 `ApiClient`의 401 자동 갱신 로직과 중복될 수 있음 — 갱신 로직을 `AuthManager`에 집중시키고 여기서 호출만 할 것

---

### 5. 토스트 메시지 UI

**목적**: 팝업을 띄우기엔 가벼운 피드백(예: "보상 수령 완료", "복사되었습니다")을 일시적으로 표시. 현재 모든 알림이 팝업 → UX 과중 및 사용자 확인 동작 강요.

**현황**: 가벼운 알림 수단 없음. 모든 피드백이 `UI_ErrorPopup` 또는 별도 팝업으로 처리됨.

**구현 범위**:

1. **UI_Toast (신규)** — `UI/UI_Toast.cs`
   - TextMeshPro 1개 + 배경 Image로 구성된 단순 프리팹
   - 화면 하단 중앙에 고정 위치 (SafeArea 안쪽 기준)
   - 등장: 아래에서 위로 슬라이드 + FadeIn (코루틴, DOTween 없이 구현)
   - 퇴장: FadeOut 후 비활성화
   - 기본 표시 시간: 2초 (호출 시 오버라이드 가능)

2. **UIManager 확장** — `Managers/UIManager.cs`
   - `ShowToast(string message, float duration = 2f)` 메서드 추가
   - 토스트 프리팹을 DontDestroyOnLoad Canvas에 미리 생성해두고 재사용 (단순 SetActive)
   - 동시 다중 토스트 정책: **덮어쓰기 방식** — 새 토스트가 오면 기존 것 즉시 교체 (1인 개발 기준 가장 단순)

3. **사용 예시** (구현 후 기존 코드에서 치환 가능한 곳):
   - `MailPopup`에서 보상 수령 완료 후 토스트 표시
   - `InquiryPopup` 제출 완료 메시지
   - 클립보드 복사, 설정 저장 등 즉각적 피드백

**주의사항**:
- 토스트 Canvas의 sortOrder를 FadeManager 레이어 아래, 일반 팝업 위로 설정
- 토스트는 사용자 확인이 필요 없는 정보성 메시지에만 사용. 오류/경고는 기존 `UI_ErrorPopup` 유지

---

### 구현 우선순위

| 순서 | 항목 | 이유 |
|------|------|------|
| 1 | 씬 전환 로딩 + 페이드 | 가장 체감 크고 SceneManager에 묶어 한 번에 처리 가능 |
| 2 | Android 뒤로가기 | 없으면 OS 강제 종료 — 기능 결함 수준 |
| 3 | Safe Area | 마켓 심사 통과 요건 |
| 4 | 앱 생명주기 | 세션/저장 안정성 |
| 5 | 토스트 메시지 | 1~4 완료 후 UX 마감 작업 |
