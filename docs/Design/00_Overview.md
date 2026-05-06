# 00. 전체 개요

> 본 문서들은 `요구사항.md` 충족을 위한 클라이언트 설계 박제다. 코드는 unity-programmer 단계에서 작성한다. 본 라운드는 1차 설계의 백엔드 추측 오류를 **`CLIENT_GUIDE.md` (정답지)** 와 **`Framework.Api/Controllers/Player/*` 컨트롤러 실측** 기준으로 전면 정정한다.

## 1차 설계 대비 핵심 변경 (요지)

| 영역 | 1차(추정) | 본 라운드(실측) |
|---|---|---|
| 점검 모드 endpoint | `/api/system/maintenance` 별도 호출 가정 | **별도 endpoint 없음**. 모든 응답의 503+`{"message":"서버 점검 중..."}` **인터셉터** 패턴 |
| 버전 체크 응답 필드 | `requiresUpdate` | **`isForceUpdate`**, `latestVersion` |
| Auth prefix | `/auth/...` (현재 ApiConfig 박제) | **`/api/auth/...`** — 현재 코드 버그 (Phase 0 정정) |
| 공지 리스트 | `/api/notices` 가정 | 백엔드 **단건만** 존재 (`/api/notices/latest`). 리스트 API 없음 → 본 라운드 결정 |
| 인벤토리 | `/api/inventory` 가정 | **Player용 endpoint 부재** (Admin만). 본 라운드 결정 |
| 우편 | `/api/mail`, `claim-all` | **`/api/mails`**, `claim-all` 없음 — `POST /api/mails/{id}/claim` N회 |
| 스테이지 | `Complete(stageId)` 본문 없음 | **`{score, stars, clearTimeMs}` 본문 필수** |
| 스테이지 진행 | 마스터 응답에 `isCleared` 포함 가정 | 마스터(`/stages`) ≠ 진행(`/stages/progress`) **분리 호출** |
| 랭킹 | `{rank, score}` | `{rank, playerId, nickname, bestScore}` |
| 구글 충돌 | 1차 설계 누락 | 5/6번에서 **409 GOOGLE_ACCOUNT_CONFLICT → resolve-conflict** 흐름 박제 |
| `isGoogleLinked` | TokenResponse에 추가 협의 가정 | 백엔드 응답에 **없음**. 본 라운드 결정 |
| 401 자동 회전 | 별도 라운드로 미루기 | **본 라운드 포함** (이유 02 본문) |
| 점검+버전 통합 endpoint | 권장 협의 | 폐기 (인터셉터 패턴이 정답) |
| AccountApi.LinkGoogle URL | `/auth/google/link` 추정 | **`/api/auth/link/google`** |
| 탈퇴 | `POST` 가정 | **`DELETE /api/auth/withdraw`**, 204 |

## 본 라운드 결정 (백엔드 부재 항목)

> 백엔드 READ-ONLY 정책에 따라 **모두 클라이언트 측 처리**로 결정.

상세는 `07_WebFramework_Plan.md`의 "백엔드 부재 항목 결정" 섹션. 결론만:

1. **인벤토리 조회** — **요구사항 축소**. `InventoryBtn` 클릭 시 `UI_AnnouncementPopup`으로 "보유 아이템 정보는 우편함의 수령 내역으로 확인해주세요" 안내. 별도 호출 없음.
2. **공지 리스트** — **단건 표시**. 백엔드의 `GET /api/notices/latest`만 호출, `UI_ConfirmPopup`에 1개만 표시.
3. **계정 상태(`isGoogleLinked`)** — **클라 추적**. 게스트 로그인 직후엔 `false`, `LinkGoogle` 200 또는 구글 직접 로그인 200 시 `true`. PlayerPrefs 로컬 캐시.

## 씬 전환 그래프

```
[App 부팅 — BootstrapScene index 0]
   │
   │ ① 버전 체크 (인증 불필요) → 503이면 점검 / 200 isForceUpdate=true 면 업데이트
   │ ② 최신 공지 1건 (인증 불필요)
   │ ③ 자동 로그인 (RefreshToken 보유) → MainScene 또는 LoginScene
   │ ④ 약관 동의(첫 실행) → LoginScene
   ▼
[LoginScene] ──(게스트/구글 로그인)──▶ [MainScene]
                                          │
                                          │ (메인 진입 직후) 일일로그인 트리거 + 1회공지 폴링
                                          │
                                          ├──(스테이지 선택)──▶ [StageSelectScene]
                                          │                       │
                                          │                       └──(진입)──▶ [GameScene]
                                          │                                       │
                                          ├──(로그아웃 / 계정 삭제)──▶ [LoginScene] │
                                          │                                       │
                                          └◀──(메인메뉴 / 클리어 OK / 실패)─────────┘
```

## 책임 분배 요약

| 영역 | 담당 | 신설 / 기존 |
|---|---|---|
| 씬 베이스 | `BaseScene`(기존), `Scene_Bootstrap`/`Scene_Login`/`Scene_Main`/`Scene_StageSelect`/`Scene_Game` | Scene_* 5개 신설 |
| 씬 UI 컨트롤러 | `UI_LoginScene`(기존), `UI_MainScene`/`UI_StageSelectScene`/`UI_GameScene`(신설) | stub 3개(UI_MainGame/UI_StageSelect/UI_InGame) **폐기** |
| 부트스트랩 흐름 | `BootstrapFlow` (정적 헬퍼) | 신설 |
| 공통 팝업 디스패처 | `PopupService` 정적 클래스 | 신설 |
| WebFramework Core | `ApiClient` GET/POST/PUT/DELETE + **401 자동 회전** + **503 인터셉터** + JWT 헤더 자동 첨부 | **확장** |
| WebFramework Api | `AuthApi`(확장), `VersionApi`/`NoticeApi`/`MailApi`/`StageApi`/`RankingApi`/`InquiryApi`/`DailyLoginApi`/`ShoutApi` | 8개 신설 |
| WebFramework Auth | `AuthManager.IsGoogleLinked` 추가 (클라 추적) | 확장 |
| WebFramework Models | 위 8개 Api 1:1 DTO + ProblemDetails | 신설 |

## 설계 원칙

1. **재사용 우선**. UIManager/SceneManager/EventManager/Singleton/ResourceManager 그대로. 새 매니저 도입은 정당화 후만.
2. **기존 클래스 시그니처 보존**. `Define.EScene`/`ApiConfig`/`ApiClient`/`AuthManager`는 **추가 위주**. 단 `ApiConfig.Auth` 경로의 `/api` prefix 누락은 버그라 정정한다.
3. **stub UI 컨트롤러 3종 폐기**. 새 컨트롤러 클래스명은 프리팹 이름과 1:1 (`UIManager.ShowPopupUI<T>` 가 `typeof(T).Name` 으로 검색).
4. **모든 인증 필요 API는 ApiClient 단에서 401 자동 회전 1회**. 호출처 코드 단순화.
5. **응답 콜백 시그니처 통일**: `Action<TRes> onSuccess, Action<ApiError> onError = null`. 1차 설계의 `Action<string>`은 ProblemDetails(에러코드/HTTP/메시지)를 못 담아 부족 → `ApiError` 도입.

## 산출물 인덱스

- `01_Bootstrap.md` — 부팅~LoginScene 진입 흐름. BootstrapScene 신설(승인됨), 점검은 인터셉터 패턴
- `02_LoginScene.md` — 게스트/구글 로그인 + 충돌 해소(409) 흐름 박제
- `03_MainScene.md` — 9개 버튼 흐름 + 메일/공지/문의 텍스트 포맷 + 일일로그인/쇼아웃 트리거
- `04_StageSelectScene.md` — 스테이지 마스터+진행 분리 호출, StageButton 동적 생성
- `05_GameScene.md` — `{score, stars, clearTimeMs}` 송신, RewardPopup 보상 출처 정정
- `06_CommonPopups.md` — Error/Announcement/Maintenance/Update/Terms 5종 + PopupService
- `07_WebFramework_Plan.md` — 8개 Api/Models 명세 + ApiClient 401/503 인터셉터 + 백엔드 부재 결정
- `08_Implementation_Order.md` — 8 Phase + DoD + 신설 백엔드 endpoint 요청 목록
