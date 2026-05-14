# /fullreview — GameClient (Unity) 컴팩트 무손실 풀리뷰 (Phase × Chunk)

GameClient Unity 프로젝트를 **Phase × Chunk 2축 분할**로 전사 검토한다. 각 청크는 독립 에이전트 호출로 처리되고 산출물은 디스크에 박제되어, 컨텍스트 컴팩트가 발생해도 직전 청크 산출물만 있으면 다음 청크 재개가 가능하다.

---

## 사용법 (인자 규약)

| 인자 | 동작 |
|---|---|
| `/fullreview` | 신규 라운드 시작. 라운드 디렉토리/PLAN/STATUS 자동 생성 후 P1.1 → ... → P4.1 순차 실행 |
| `/fullreview 1` | Phase 1 전체 청크(P1.1, P1.2, P1.3) 순차 실행 |
| `/fullreview 1.2` | P1.2 청크 단일 실행 |
| `/fullreview resume` | `STATUS.md`의 첫 미완료 청크부터 재개 |
| `/fullreview report` | Phase 4 합본만 재실행 (REVIEW_REPORT.md 재생성) |
| `/fullreview status` | 현재 라운드 진행 상태 출력 |

`$ARGUMENTS`를 위 표 기준으로 파싱한다.

---

## 산출 디렉토리 정책

- 라운드 루트: `review/round_{YYYYMMDD}/`
- 현재 라운드 포인터: `review/CURRENT_ROUND.txt` — 1줄(라운드 폴더명만)
- 청크 산출물: `review/round_{YYYYMMDD}/p{N}_{chunk}_{slug}.md`
- 진행 추적: `review/round_{YYYYMMDD}/STATUS.md`
- PLAN 스냅샷: `review/round_{YYYYMMDD}/REVIEW_PLAN.snapshot.md`
- 직전 보고서 백업: `review/round_{YYYYMMDD}/REVIEW_REPORT.previous.md`
- **결함 추적**: `review/round_{YYYYMMDD}/DEFECT_TRACKING.md` — 직전 라운드 이슈 해결/미해결 현황
- 최종 보고서: 루트 `REVIEW_REPORT.md` 갱신 + 라운드 폴더 사본 저장
- `review/`는 `.gitignore` 등재됨

---

## 신규 라운드 초기화 절차 (`/fullreview` 인자 없음)

1. 오늘 날짜로 `ROUND_ID = round_{YYYYMMDD}` 결정. 같은 날 재시작 시 기존 폴더 재사용 여부 사용자 1회 확인 (기본: 재사용).
2. `review/{ROUND_ID}/` 생성.
3. 기존 루트 `REVIEW_REPORT.md`를 `review/{ROUND_ID}/REVIEW_REPORT.previous.md`로 백업(원본 유지).
4. **직전 라운드 결함 추적** — `REVIEW_REPORT.previous.md`가 존재하면 "식별된 이슈 표" 전체를 추출한다. 각 이슈별로 현재 코드를 검토해 **RESOLVED**(수정 확인) / **OPEN**(미해결)을 판정하고 판정 근거(파일·라인 또는 "코드 변경 없음")를 1줄 기록한다. 결과를 아래 표 형식으로 `review/{ROUND_ID}/DEFECT_TRACKING.md`에 저장한다. 직전 라운드가 없으면 파일에 "이전 라운드 없음" 1줄만 기록 후 계속 진행한다.

   | 심각도 | 점검 ID | 파일·라인 | 이슈 설명 | 상태 | 판정 근거 |
   |---|---|---|---|---|---|
   | CRITICAL | S1 | AuthManager.cs:58 | 토큰 PlayerPrefs 평문 저장 | OPEN | 코드 변경 없음 |

5. 루트 `REVIEW_PLAN.md`를 본 스킬의 청크 카탈로그 기반으로 자동 작성(덮어쓰기) + 동일 본문을 라운드 스냅샷으로 복사.
6. `review/{ROUND_ID}/STATUS.md` 생성 — 모든 청크 `[ ] PENDING`으로 초기화.
7. `review/CURRENT_ROUND.txt`를 `{ROUND_ID}` 한 줄로 갱신.
8. P1.1부터 순차 실행.

---

## 재개 절차 (컴팩트 후 또는 `/fullreview resume`)

다음 파일만 읽으면 재개 가능 (in-memory 변수 사용 금지):

1. `CLAUDE.md`
2. `DEVNOTES.md` (존재 시)
3. `review/CURRENT_ROUND.txt` → `{ROUND_ID}`
4. `review/{ROUND_ID}/REVIEW_PLAN.snapshot.md`
5. `review/{ROUND_ID}/STATUS.md`
6. `review/{ROUND_ID}/DEFECT_TRACKING.md`
7. **직전 청크 산출물 1개** (다음 청크가 의존)
8. (Phase 4 재개 시) `review/{ROUND_ID}/p*.md` 글롭 전체

알고리즘:
- STATUS.md에서 `[ ]`/`[~]` 첫 청크 X 식별
- X의 직전 산출물을 PLAN/스킬 카탈로그로 조회 → 다음 청크 입력으로 전달
- X 청크 에이전트 호출

---

## 청크 카탈로그 (마스터 정의)

각 청크는 4요소를 가진다: **점검 ID 매핑 / 대상 파일·폴더 / 산출물 경로 / 인계 입력**.

스크립트 루트 경로: `UnityClient/Assets/@Scripts/`

### Phase 1 — unity-architect (아키텍처 검토)

#### P1.1 폴더 구조·의존성 방향·Singleton 패턴
- 점검 ID: A1, A2, A3
- 대상:
  - `UnityClient/Assets/@Scripts/Managers/Singleton.cs` — Singleton<T> 베이스 구현 및 생성 제약
  - `UnityClient/Assets/@Scripts/Managers/GameManager.cs` — 전체 매니저 초기화·참조 허브
  - `UnityClient/Assets/@Scripts/Managers/` 전수 — MonoBehaviour 상속 여부, Singleton<T> 패턴 준수
  - `UnityClient/Assets/@Scripts/Utils/Define.cs` — Enum/상수 위치 원칙
  - `UnityClient/Assets/@Scripts/Config/` — Config 클래스 배치 원칙
  - `UnityClient/Assets/@Scripts/Controllers/` — ObjectBase 계층
  - 의존성 방향: `WebFramework` → `@Scripts` (역방향 금지), `UI` → `Managers` (역방향 금지)
- 산출물: `review/{ROUND_ID}/p1_1_structure_dependency.md`
- 인계 입력: 없음(시작 청크)

#### P1.2 WebFramework 구조·재사용 원칙·Api 매핑
- 점검 ID: A4, A5, A6
- 대상:
  - `UnityClient/Assets/@Scripts/WebFramework/Core/` 전수 — UnityWebRequest 래퍼 구조, ApiClient 책임
  - `UnityClient/Assets/@Scripts/WebFramework/Api/` 전수 — 엔드포인트별 서비스 클래스
  - `UnityClient/Assets/@Scripts/WebFramework/Auth/` — AuthManager, GoogleSignInProvider
  - `UnityClient/Assets/@Scripts/WebFramework/Models/` — DTO 배치 원칙
  - WebFramework 내부 의존 방향: `Api → Core`, `Auth → Core`, `Api/Auth → Models` 만 허용
  - 매니저 중복 구현 여부 (Singleton, Event, UI, Scene 재사용 확인)
  - `../CLIENT_GUIDE.md` 기준 Endpoint 매핑 현황
- 산출물: `review/{ROUND_ID}/p1_2_webframework.md`
- 인계 입력: `p1_1_structure_dependency.md`

#### P1.3 씬 흐름·UI 베이스·라이프사이클·Bootstrap
- 점검 ID: A7, A8, A9
- 대상:
  - `UnityClient/Assets/@Scripts/Scenes/BaseScene.cs` — BaseScene 추상화 및 Enter/Exit 책임
  - `UnityClient/Assets/@Scripts/Scenes/` 전수 — BaseScene 상속 여부, 씬 전환 패턴
  - `UnityClient/Assets/@Scripts/Bootstrap/` — 앱 초기화 흐름(BootstrapFlow, AppResumeFlow)
  - `UnityClient/Assets/@Scripts/UI/UI_Base.cs`, `UI_UGUI.cs`, `UI_Toolkit.cs` — UI 베이스 계층
  - `UnityClient/Assets/@Scripts/UI/` 전수 — 베이스 클래스 상속, UIManager 경유 여부
  - 씬 전환: SceneManager 경유 원칙 (UnityEngine.SceneManagement 직접 호출 금지)
  - DontDestroyOnLoad 오브젝트 관리 전략
- 산출물: `review/{ROUND_ID}/p1_3_scene_ui_bootstrap.md`
- 인계 입력: `p1_2_webframework.md`

---

### Phase 2 — unity-qa (구현 품질 + 컴파일)

#### P2.1 Managers + Bootstrap + Utils + 컴파일 검증
- 점검 ID: Q1, Q2, Q3
- 대상:
  - `UnityClient/Assets/@Scripts/Managers/` 전수
  - `UnityClient/Assets/@Scripts/Bootstrap/` 전수
  - `UnityClient/Assets/@Scripts/Utils/` 전수
  - `UnityClient/Assets/@Scripts/Config/` 전수
  - `UnityClient/Assets/@Scripts/Controllers/` 전수
  - `UnityClient/Assets/@Scripts/Data/` 전수
- 컴파일 검증:
  ```powershell
  dotnet build UnityClient/Assembly-CSharp.csproj /nologo /p:WarningLevel=4
  ```
  - 오류 0개 확인. `error CS*` 항목 전수 보고
  - 신규 warning은 보고; 기존 warning 수 대비 증가분만 언급
  - Unity Editor 재컴파일이 필요한 경우 "Unity Editor 컴파일 결과는 미검증" 명시
- 산출물: `review/{ROUND_ID}/p2_1_managers_compile.md`
- 인계 입력: `p1_3_scene_ui_bootstrap.md`

#### P2.2 WebFramework Core + Api + Models + Auth 구현 품질
- 점검 ID: Q4, Q5, Q6, Q7
- 대상:
  - `UnityClient/Assets/@Scripts/WebFramework/Core/` 전수
    - ApiClient: 요청/응답 처리, 에러 핸들링, 타임아웃, 429 재시도(지수 백오프) 로직
    - ServerTime: 서버 시간 동기화 흐름
    - ApiResult/ApiError: 결과 래퍼 일관성
  - `UnityClient/Assets/@Scripts/WebFramework/Api/` 전수
    - 각 Api 클래스: ApiClient 재사용 여부, 중복 HTTP 로직 여부
    - 응답 DTO 역직렬화 안전성
  - `UnityClient/Assets/@Scripts/WebFramework/Auth/` 전수
    - AuthManager: 토큰 저장/갱신/만료 처리
    - GoogleSignInProvider: OAuth 흐름
  - `UnityClient/Assets/@Scripts/WebFramework/Models/` 전수
    - DTO 필드: nullable 처리, 이름 일치 (`[JsonProperty]` 또는 camelCase)
- 산출물: `review/{ROUND_ID}/p2_2_webframework_quality.md`
- 인계 입력: `p2_1_managers_compile.md`

#### P2.3 Scenes + UI + 한국어 주석 전수
- 점검 ID: Q8, Q9, Q10
- 대상:
  - `UnityClient/Assets/@Scripts/Scenes/` 전수
    - BaseScene 상속 구조, Awake/Start/OnDestroy 이벤트 구독·해제 짝
    - 씬 초기화 중 API 호출 패턴
  - `UnityClient/Assets/@Scripts/UI/` 전수
    - `[SerializeField]` 누락 가능성, NullReference 경로
    - UGUI/UIToolkit 베이스 상속 일관성
    - UIManager.ShowUI / CloseUI 패턴 준수
  - 한국어 주석 전수 점검: `@Scripts/` 전체 `.cs` 파일 대상
    - 주석 없는 public/private 메서드 비율 보고
    - 영문 주석 혼용 파일 목록화
- 산출물: `review/{ROUND_ID}/p2_3_scenes_ui_comments.md`
- 인계 입력: `p2_2_webframework_quality.md`

---

### Phase 3 — unity-qa (안정성·보안)

#### P3.1 인증·토큰·세션 보안
- 점검 ID: S1, S2, S3
- 대상:
  - `UnityClient/Assets/@Scripts/WebFramework/Auth/AuthManager.cs` — JWT 저장 방식 (PlayerPrefs 평문 여부)
  - `UnityClient/Assets/@Scripts/WebFramework/Core/ApiConfig.cs` — 서버 URL·키 하드코딩 여부
  - `UnityClient/Assets/@Scripts/WebFramework/Auth/GoogleSignInProvider.cs` — OAuth 흐름 보안
  - `UnityClient/Assets/@Scripts/WebFramework/Core/ApiClient.cs` — Authorization 헤더 조립 방식
  - `UnityClient/Assets/Settings/GoogleSignIn/` — Google Sign-In SDK 설정
  - PlayerPrefs 저장 키 전수 grep (`PlayerPrefs.Set*`) — 민감정보 노출 여부
  - HTTPS 강제 여부 (`http://` 하드코딩 grep)
- 산출물: `review/{ROUND_ID}/p3_1_auth_security.md`
- 인계 입력: `p2_3_scenes_ui_comments.md`

#### P3.2 네트워크 안정성·에러 처리·429 재시도
- 점검 ID: S4, S5, S6
- 대상:
  - `UnityClient/Assets/@Scripts/WebFramework/Core/ApiClient.cs` — 타임아웃 설정, 재시도 로직
  - `UnityClient/Assets/@Scripts/WebFramework/Core/ApiError.cs` — 에러 코드 분기 완결성
  - `UnityClient/Assets/@Scripts/WebFramework/Api/` 전수 — 응답 실패 시 UI 피드백 경로
  - 씬 전환 도중 진행 중인 요청 취소 처리 여부 (CancellationToken 또는 `isCancelled` 확인)
  - 중복 요청 방지 패턴 (동시 로그인 버튼 다중 탭 등)
  - CrashReportManager: 크래시 수집 흐름 점검
- 산출물: `review/{ROUND_ID}/p3_2_network_stability.md`
- 인계 입력: `p3_1_auth_security.md`

#### P3.3 메모리·이벤트·리소스·Unity 특이 이슈
- 점검 ID: S7, S8, S9
- 대상: `UnityClient/Assets/@Scripts/` 전수 (Grep 기반)
  - 이벤트 구독·해제 짝 검사: `AddListener` / `RemoveListener`, `+=` / `-=`
  - `DontDestroyOnLoad` 오브젝트 중복 생성 경로
  - `FindObjectOfType` / `FindObjectsOfType` 남용 (캐싱 누락)
  - `Resources.Load` 경로 오류 가능성 및 빌드 누락 위험
  - 코루틴이 오브젝트 비활성화로 중단되는 케이스
  - `Update()` 내 무거운 연산 (로직 분리 필요 여부)
  - `AppLifecycleManager` 앱 포그라운드/백그라운드 전환 처리
  - `SaveManager`: 저장 데이터 직렬화 안전성, 손상 시 복구 경로
- 산출물: `review/{ROUND_ID}/p3_3_memory_events_resources.md`
- 인계 입력: `p3_2_network_stability.md`

---

### Phase 4 — orchestrator (합본)

#### P4.1 최종 보고서 합본
- 입력: `review/{ROUND_ID}/p*.md` 글롭 전체
- 산출물: 루트 `REVIEW_REPORT.md` 덮어쓰기 + `review/{ROUND_ID}/REVIEW_REPORT.md` 사본
- 에이전트: orchestrator 직접 처리(별도 에이전트 미호출). 컴팩트 시 `/fullreview report`로 재실행

---

## 청크별 산출물 표준 형식

각 `p{N}_{chunk}_{slug}.md`는 다음 섹션을 반드시 포함:

- 메타: ROUND_ID / 에이전트 / 점검 ID / 입력 인계 경로
- 점검 항목별 결과: 항목별 PASS/WARN/FAIL + 위반(파일:라인 — 심각도 — 설명) + 근거
- 식별된 이슈 표: 심각도/파일·라인/점검 ID/설명/권고 조치 컬럼
- 인계 노트: 검토 완료 모듈 / 핫스팟 / 의문점·미해결 질문
- **다음 청크 입력 요약 (200단어 이내)** — 다음 청크 프롬프트에 그대로 붙여넣을 압축본

마지막 섹션이 핵심: 다음 청크는 이 200단어 + 점검 ID + STATUS만으로도 정상 동작해야 함.

---

## STATUS.md 형식

| 청크 | 상태 | 산출물 | 시작 | 완료 |
|---|---|---|---|---|

상태 코드: `[ ] PENDING` / `[~] IN_PROGRESS` / `[x] DONE` / `[!] FAILED`.

마지막에 "최근 인계 노트 요약" 섹션으로 직전 완료 청크의 200단어 요약을 보존.

---

## 청크 실행 표준 절차 (모든 청크 공통)

1. STATUS.md의 해당 청크 행을 `[~] IN_PROGRESS` + 시작 시각으로 갱신
2. 직전 청크 산출물 경로를 카탈로그에서 조회 → 읽기
3. 해당 Phase 에이전트(unity-architect / unity-qa)를 **백그라운드**로 호출. 프롬프트에 다음 포함:
   - 청크 코드 + 점검 ID + 합격 기준
   - 대상 파일·폴더 목록
   - 직전 청크 200단어 요약
   - 산출물 경로(쓰기 권한): `review/{ROUND_ID}/p{N}_{chunk}_{slug}.md`
   - 산출물 표준 형식 준수 의무
   - **코드 수정 절대 금지** (산출물 .md만 쓰기 허용)
4. 에이전트 종료 후 산출물 파일 존재 검증 → STATUS.md를 `[x] DONE` + 완료 시각 + 200단어 요약 갱신
5. 다음 청크 진행. 단일 청크 인자였다면 종료
6. 청크 실패 시 `[!] FAILED` + 사유 1줄 기록, 사용자에게 보고. 자동 재시도 금지

---

## Phase 4 합본 절차

1. `review/{ROUND_ID}/p*.md` 글롭 전체 수집
2. 각 산출물 "식별된 이슈 표"를 심각도별 통합
3. 다음 구조로 루트 `REVIEW_REPORT.md` 작성(덮어쓰기 전 사용자 1회 확인):
   - Executive Summary (라운드 정보, 청크 완료 현황, 심각도별 합계, Top 5 즉시 조치)
   - **0장 직전 라운드 결함 추적** — `DEFECT_TRACKING.md` 기반. RESOLVED/OPEN 건수 요약 + OPEN 이슈 전체 표. 직전 라운드 없으면 "해당 없음" 1줄
   - 1장 Phase 1 결과 (1.1/1.2/1.3) — 아키텍처
   - 2장 Phase 2 결과 (2.1/2.2/2.3) — 구현 품질
   - 3장 Phase 3 결과 (3.1/3.2/3.3) — 안정성·보안
   - 4장 Critical Issues
   - 5장 High Issues
   - 6장 Medium Issues
   - 7장 Low / 추적 항목
   - 8장 DEVNOTES.md 갱신 권고
   - 부록 A. 청크 산출물 인덱스
4. 동일 본문 `review/{ROUND_ID}/REVIEW_REPORT.md`에 사본 저장
5. STATUS.md P4.1을 `[x] DONE`으로 마감

---

## 에이전트 배정

| Phase | 에이전트 | 역할 |
|---|---|---|
| P1.x | unity-architect | 폴더 구조·의존성·설계 정합성 |
| P2.x | unity-qa | 구현 품질·컴파일·한국어 주석 |
| P3.x | unity-qa | 안정성·보안·메모리·Unity 특이 이슈 |
| P4.1 | orchestrator | 합본 (직접 처리) |

---

## 컴파일 검증 경로

```powershell
# GameClient 루트 기준
dotnet build UnityClient/Assembly-CSharp.csproj /nologo /p:WarningLevel=4
```

- Unity Editor가 csproj를 백그라운드에서 재생성하므로 일시 실패 시: 사용자에게 Unity Editor 포커스 후 재시도 안내
- `error CS0246`(타입 못 찾음), `CS0103`, `CS0104` 등은 반드시 보고
- Editor 컴파일 결과 직접 확인이 불가능하면 "Unity Editor 컴파일 결과 미검증" 명시

---

## 주의사항

- 모든 에이전트는 **읽기 전용** + 산출물 `.md` 1개 쓰기만 허용
- 에이전트 실행은 `run_in_background: true` (unity-programmer 미사용 — 리뷰 전용)
- in-memory 변수 사용 금지. 모든 인계는 산출물 파일을 통해
- 중간 청크 완료는 사용자에게 보고하지 않음. 단 **Phase 4 합본 직전 확인** + **최종 보고**는 필수
- 컴팩트 발생 시 `/fullreview resume`만으로 마지막 미완료 청크부터 재개 가능해야 함 — 본 스킬의 핵심 설계 목표
- **씬/프리팹/에셋 수정 절대 금지** — CLAUDE.md 직렬화 파일 직접 작성 금지 원칙 적용
- `../Framework/*` 백엔드 파일은 READ-ONLY — 리뷰 중 수정·제안 금지
