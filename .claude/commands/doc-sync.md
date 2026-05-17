# /doc-sync — GameClient(Unity) 전수 스캔 → 문서 최신화 (Phase × Chunk, 컴팩트 내성)

`UnityClient/Assets/@Scripts/` 전체 코드를 청크별로 빠짐없이 읽고, 지정된 .md 문서를 코드 실제 상태와 일치하도록 추가/삭제/수정한다.
컨텍스트 컴팩트가 발생해도 STATUS.md 하나만 있으면 마지막 미완료 청크부터 재개 가능하다.

> 백엔드(`../.claude/commands/doc-sync.md`)의 doc-sync를 GameClient(Unity) 구조에 맞게 이식한 버전이다. 스캔 대상은 Unity 클라이언트 코드, 동기화 대상은 클라이언트 문서다.

---

## 사용법

| 인자 | 동작 |
|---|---|
| `/doc-sync` | 기본 대상 4종으로 신규 라운드 시작: `DEVELOPER_GUIDE.md` + `DEVNOTES.md` + `CLAUDE.md` + `MANUAL_TEST.md` |
| `/doc-sync devguide` | `DEVELOPER_GUIDE.md`만 동기화 |
| `/doc-sync devnotes` | `DEVNOTES.md`만 동기화 |
| `/doc-sync claude` | `CLAUDE.md` 카탈로그 표만 동기화 |
| `/doc-sync manualtest` | `MANUAL_TEST.md`만 동기화 |
| `/doc-sync devguide devnotes` | 공백 구분 복수 지정 |
| `/doc-sync resume` | STATUS.md의 첫 미완료 청크부터 재개 |
| `/doc-sync status` | 현재 라운드 진행 상태 출력 |

`$ARGUMENTS`를 위 표 기준으로 파싱한다. 인자 없으면 4종 전체.

---

## 산출 디렉토리 정책

- 라운드 루트: `doc-sync/round_{YYYYMMDD}/`
- 현재 라운드 포인터: `doc-sync/CURRENT_ROUND.txt` — 1줄(라운드 폴더명만)
- 스캔 청크 산출물: `doc-sync/round_{YYYYMMDD}/c{N}_{slug}.md`
- 동기화 청크 산출물: `doc-sync/round_{YYYYMMDD}/s{N}_{slug}_result.md`
- 진행 추적: `doc-sync/round_{YYYYMMDD}/STATUS.md`
- `doc-sync/`는 `.gitignore`에 등재됨 (로컬 전용 산출물)

---

## 대상 문서 경로 (GameClient 루트 기준)

| 키 | 경로 | 코드 유도 범위 |
|---|---|---|
| `devguide` | `DEVELOPER_GUIDE.md` | Api 호출 패턴·NetworkConfig·AuthConfig·신규 도메인 절차·ApiClient 메서드 매핑·씬 구성 전부 |
| `devnotes` | `DEVNOTES.md` | `[주의] 배포 전 교체`·`[TODO]`·`기능 현황` 섹션만. `[설계 결정]`·`[법적 의무]`는 유지 |
| `claude` | `CLAUDE.md` | Managers/Config/Utils/UI/WebFramework/Scenes **카탈로그 표만**. 설계 원칙·규칙·Why 문단은 유지 |
| `manualtest` | `MANUAL_TEST.md` | 시나리오 단계의 §참조·호출 API·UI 전환이 코드 흐름과 어긋난 경우만 |

> **제외 (동기화 대상 아님)**: `REVIEW_REPORT.md`(=/fullreview 산출물), `../CLIENT_GUIDE.md`·`../Framework/*`(백엔드 소유 READ-ONLY). 어떤 청크도 이 파일들을 수정하지 않는다.

---

## 신규 라운드 초기화 절차 (`/doc-sync [대상]`)

1. 오늘 날짜로 `ROUND_ID = round_{YYYYMMDD}` 결정. 같은 날 재시작 시 기존 폴더 재사용 여부 1회 확인(기본: 재사용).
2. `doc-sync/{ROUND_ID}/` 생성.
3. 선택된 대상에 필요한 스캔/동기화 청크만 산정(아래 "대상↔청크 매핑").
4. `doc-sync/{ROUND_ID}/STATUS.md` 생성 — 필요한 청크 전체를 `[ ] PENDING`으로 초기화.
5. `doc-sync/CURRENT_ROUND.txt`를 `{ROUND_ID}` 한 줄로 갱신.
6. C1부터 순차 실행 → 스캔 전부 완료 후 S 청크 실행.

---

## 재개 절차 (컴팩트 후 또는 `/doc-sync resume`)

다음 파일만 읽으면 재개 가능 (in-memory 변수 사용 금지):

1. `doc-sync/CURRENT_ROUND.txt` → `{ROUND_ID}`
2. `doc-sync/{ROUND_ID}/STATUS.md` → 첫 `[ ]`/`[~]` 청크 식별
3. 해당 청크의 인계 입력 산출물(스킬 카탈로그로 조회)

알고리즘:
- STATUS.md에서 미완료 첫 청크 X 식별
- X의 인계 입력 산출물을 읽어 컨텍스트 복구
- X 청크 에이전트 호출

---

## 대상↔청크 매핑

| 대상 | 필요 스캔 청크 | 동기화 청크 |
|---|---|---|
| `devguide` | C1, C2, C3 | S1 |
| `devnotes` | C1, C4 | S2 |
| `claude` | C1, C2, C3 | S3 |
| `manualtest` | C3, C5 | S4 |

선택된 대상들의 스캔 청크를 **합집합**으로 1회만 실행하고, 동기화 청크는 대상별로 실행한다.

---

## 청크 카탈로그

스크립트 루트: `UnityClient/Assets/@Scripts/`

### 스캔 Phase — 코드 읽기 (general-purpose 에이전트)

각 스캔 청크는 파일을 읽고 구조화된 정보를 산출물에 기록한다.
**Glob으로 파일 목록을 얻은 뒤 목록의 모든 파일을 Read로 읽는다. 샘플링·추측 금지.** `*.meta`·`Library/`·`PackageCache`는 제외.

#### C1 — WebFramework 전수 (Core / Api / Models / Auth)
- **스캔 대상** (전수 읽기):
  - `UnityClient/Assets/@Scripts/WebFramework/Core/` 하위 모든 `.cs`
  - `UnityClient/Assets/@Scripts/WebFramework/Api/` 하위 모든 `.cs`
  - `UnityClient/Assets/@Scripts/WebFramework/Models/` 하위 모든 `.cs`
  - `UnityClient/Assets/@Scripts/WebFramework/Auth/` 하위 모든 `.cs`
- **산출 정보**: `ApiClient` public 메서드 시그니처(GetAsync/PostAsync 등), `ApiResult<T>`/`ApiError` 구조, 각 `*Api` 클래스의 메서드명·호출 HTTP 메서드·엔드포인트 경로·요청/응답 DTO명, 각 Model DTO의 필드명·타입·`[JsonProperty]`/camelCase 매핑, `AuthManager` 토큰 저장/갱신/만료 흐름, `GoogleSignInProvider` OAuth 흐름
- **산출물**: `doc-sync/{ROUND_ID}/c1_webframework.md`
- **관련 대상**: devguide, devnotes, claude

#### C2 — Managers · Config · Utils 카탈로그 전수
- **스캔 대상** (전수 읽기):
  - `UnityClient/Assets/@Scripts/Managers/` 하위 모든 `.cs`
  - `UnityClient/Assets/@Scripts/Config/` 하위 모든 `.cs`
  - `UnityClient/Assets/@Scripts/Utils/` 하위 모든 `.cs` (Define / Extension / Utils / PriorityQueue)
- **산출 정보**: 각 매니저 클래스명·상속(`Singleton<T>`/MonoBehaviour/static 여부)·핵심 책임·주요 public 메서드, 각 Config ScriptableObject의 필드·역할, `Define.cs`의 전 enum 목록·멤버, `PlayerPrefsKey` 상수 목록, `Extension`/`Utils` 정적 메서드 시그니처 목록, `PriorityQueue` 공개 API
- **산출물**: `doc-sync/{ROUND_ID}/c2_managers_config_utils.md`
- **관련 대상**: claude, devguide

#### C3 — Scenes · UI · Bootstrap · Controllers · Data 전수
- **스캔 대상** (전수 읽기):
  - `UnityClient/Assets/@Scripts/Scenes/` 하위 모든 `.cs`
  - `UnityClient/Assets/@Scripts/UI/` 하위 모든 `.cs`
  - `UnityClient/Assets/@Scripts/Bootstrap/` 하위 모든 `.cs`
  - `UnityClient/Assets/@Scripts/Controllers/` 하위 모든 `.cs`
  - `UnityClient/Assets/@Scripts/Data/` 하위 모든 `.cs`
- **산출 정보**: 씬 클래스 목록·`BaseScene` 상속·대응 `Define.EScene` 멤버·`OnBackButton` 오버라이드 여부, `BootstrapFlow`/`AppResumeFlow` 단계 흐름, UI 베이스 계층(`UI_Base`/`UI_UGUI`/`UI_Toolkit`)과 `PopupService` 정적 헬퍼 목록·`UI_Base.RunWithBusyAsync`/`GuardReentry` 시그니처, `Controllers`/`Data` 모델 목록
- **산출물**: `doc-sync/{ROUND_ID}/c3_scene_ui_bootstrap.md`
- **관련 대상**: claude, devguide, manualtest

#### C4 — 기능 구현 현황 · 배포 전 마커 전수 (devnotes 대상 시에만)
- **스캔 대상**:
  - `UnityClient/Assets/@Scripts/` 전수 Grep — `#if DEBUG`, `TODO`, `FIXME`, `PLACEHOLDER`, `HACK`, `http://`(평문 URL), 하드코딩 SDK 키 후보(`AppKey`, `ClientId` 리터럴)
  - 기능 매니저 구현 상태 Read: `IAPManager`, `AdsManager`, `CrashReportManager`, `LocalizationManager`, `SaveManager`, `AppLifecycleManager`
  - `Config/` ScriptableObject 기본값(placeholder vs 실값) 추정
- **산출 정보**: 기능별 구현/부분구현/미구현 상태(근거 파일:라인), 배포 전 교체 필요 항목(placeholder·디버그 우회·평문 키 위치), TODO/FIXME 마커 위치 목록
- **산출물**: `doc-sync/{ROUND_ID}/c4_feature_status.md`
- **관련 대상**: devnotes

#### C5 — 사용자 흐름 시퀀스 스캔 (manualtest 대상 시에만)
- **스캔 대상** (전수 읽기):
  - `UnityClient/Assets/@Scripts/Bootstrap/` 전수
  - `BootstrapScene` / `LoginScene` / `MainScene` / `StageSelectScene` / `GameScene` 씬 스크립트 및 직접 연결 UI 스크립트
  - 위 흐름에서 호출되는 `WebFramework/Api/*` 호출부
- **산출 정보**: 앱 진입 → 버전체크/점검 → 자동/게스트/구글 로그인 → 메인 → 기능 탐색 각 단계의 실제 코드 시퀀스, 단계별 호출 API·전환 UI·분기 조건. `MANUAL_TEST.md` 시나리오 §번호와 대조 가능한 표 형식
- **산출물**: `doc-sync/{ROUND_ID}/c5_user_flow.md`
- **관련 대상**: manualtest

---

### 동기화 Phase — 문서 수정 (doc-sync 에이전트, run_in_background: true)

해당 대상에 필요한 스캔 청크가 모두 완료된 후 대상 문서별로 호출한다.

#### S1 — DEVELOPER_GUIDE.md 동기화 (devguide 대상 시)
- **입력 청크**: c1, c2, c3 산출물
- **대상 파일**: `DEVELOPER_GUIDE.md`
- **산출물**: `doc-sync/{ROUND_ID}/s1_devguide_result.md`

#### S2 — DEVNOTES.md 동기화 (devnotes 대상 시)
- **입력 청크**: c1, c4 산출물
- **대상 파일**: `DEVNOTES.md`
- **비고**: `[설계 결정]`·`[법적 의무]` 섹션은 코드로 검증 불가 → 유지. `[TODO]`/`기능 현황`에서 코드에 이미 구현된 항목 → 삭제/상태 갱신
- **산출물**: `doc-sync/{ROUND_ID}/s2_devnotes_result.md`

#### S3 — CLAUDE.md 카탈로그 동기화 (claude 대상 시)
- **입력 청크**: c1, c2, c3 산출물
- **대상 파일**: `CLAUDE.md`
- **비고**: **카탈로그 표만 수정** — `Managers/ 카탈로그`, `Config/ 카탈로그`, `Utils/ 카탈로그`, `UI/ 헬퍼`, `WebFramework 폴더 구조`, `씬 구성`. 재사용 우선 원칙·코딩 컨벤션·Why 문단·Behavioral Guidelines는 **절대 수정 금지**
- **산출물**: `doc-sync/{ROUND_ID}/s3_claude_result.md`

#### S4 — MANUAL_TEST.md 동기화 (manualtest 대상 시)
- **입력 청크**: c3, c5 산출물
- **대상 파일**: `MANUAL_TEST.md`
- **비고**: 시나리오 단계의 §참조·호출 API·UI 전환·분기 조건이 코드 흐름과 다르면 수정. 테스트 의도·기대 결과 서술은 유지
- **산출물**: `doc-sync/{ROUND_ID}/s4_manualtest_result.md`

---

## 청크 실행 표준 절차

1. STATUS.md 해당 청크 행을 `[~] IN_PROGRESS` + 시작 시각으로 갱신
2. 스캔 청크: **general-purpose** 에이전트를 백그라운드로 호출
   동기화 청크: **doc-sync** 에이전트를 백그라운드로 호출
3. 에이전트 종료 후 산출물 파일 존재 검증
4. STATUS.md를 `[x] DONE` + 완료 시각으로 갱신
5. 다음 청크 진행 (단일 대상 인자였고 해당 대상 청크가 끝났으면 종료)
6. 실패 시 `[!] FAILED` + 사유 1줄 기록, 사용자에게 보고. 자동 재시도 금지

---

## STATUS.md 형식

```
대상 문서: devguide, devnotes, claude, manualtest
ROUND_ID: round_{YYYYMMDD}

| 청크 | 설명 | 상태 | 산출물 | 시작 | 완료 |
|---|---|---|---|---|---|
| C1 | WebFramework 전수 | [ ] PENDING | c1_webframework.md | | |
| C2 | Managers/Config/Utils | [ ] PENDING | c2_managers_config_utils.md | | |
...
| S1 | DEVELOPER_GUIDE 동기화 | [ ] PENDING | s1_devguide_result.md | | |
```

상태 코드: `[ ] PENDING` / `[~] IN_PROGRESS` / `[x] DONE` / `[!] FAILED`

---

## 스캔 청크 에이전트 프롬프트 필수 포함 항목

- 스캔 대상 파일/폴더 경로 (카탈로그 그대로)
- "Glob으로 파일 목록 확보 후 **목록의 모든 파일**을 Read로 읽을 것. `*.meta`·`Library/`·`PackageCache` 제외. 샘플링·추측 금지"
- 산출물 경로와 형식 (마크다운 표/목록)
- "코드/씬/프리팹/에셋 파일(`.cs`·`.unity`·`.prefab`·`.asset`·`.meta`) 수정 절대 금지. 산출물 `.md` 1개만 쓰기 허용"
- ROUND_ID

## 동기화 청크 에이전트 프롬프트 필수 포함 항목

- 입력 청크 산출물 경로 목록
- 대상 .md 파일 경로
- 비교 기준(추가/삭제/수정 판단 기준) + 대상별 보존 섹션 지정(위 S1~S4 비고 그대로)
- 결과 산출물 경로
- "문서 전체 구조(목차·섹션 순서) 유지, 내용만 수정"
- "`.md` 외 모든 파일 수정 절대 금지. `../Framework/*`·`../CLIENT_GUIDE.md`·`REVIEW_REPORT.md` 건드리지 말 것"

---

## 비교 기준 (동기화 청크 공통)

- 문서 기술(엔드포인트 경로·메서드 시그니처·DTO 필드·매니저 책임·enum 멤버·씬 흐름 등)이 코드와 다르면 → **수정**
- 문서에 있으나 코드에 없으면 → **삭제**
- 코드에 있으나 문서에 없으면 → **추가**
- DEVNOTES `[TODO]`/`기능 현황`: 코드에 이미 구현된 항목 → **삭제 또는 상태 갱신** (신규 미구현 항목 발굴은 이 스킬 범위 밖)
- 설계 의도·운영 정책·법적 고지·주의사항·코딩 컨벤션 등 코드로 검증 불가한 내용 → **건드리지 않음**

---

## 주의사항

- in-memory 변수 사용 금지. 모든 인계는 산출물 파일을 통해
- 중간 청크 완료는 사용자에게 보고하지 않음. **최종 완료 보고만 필수**
- 컴팩트 발생 시 `/doc-sync resume`만으로 재개 가능해야 함 — 본 스킬의 핵심 설계 목표
- 대상 문서에 불필요한 청크는 실행하지 않음 (예: manualtest 미대상 시 C5 건너뜀)
- **씬/프리팹/에셋/.meta 직접 수정 절대 금지** — CLAUDE.md Unity 직렬화 파일 작성 금지 원칙 적용
- `../Framework/*` 백엔드 + `../CLIENT_GUIDE.md`는 READ-ONLY — 스캔 대상도 아님(클라이언트 코드만 스캔)
