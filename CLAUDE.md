# GameClient (Unity)

## 프로젝트 목적

이 Unity 프로젝트는 두 가지 목적을 갖습니다.

1. **현재** — `../` 경로의 WebAPIFramework 백엔드(Framework.Api)와의 HTTP REST 통신을 검증하는 더미 클라이언트
2. **목표** — 이후 신규 게임 프로젝트의 시작점이 될 수 있는 Unity 클라이언트 프레임워크

<!-- [이 프로젝트 한정] 아래 백엔드 경로는 현재 프로젝트 구성 기준입니다.
     다른 프로젝트에서 이 GameClient를 템플릿으로 사용할 경우, 연동 대상 백엔드 경로와 프로젝트 목록을 실제 환경에 맞게 수정하세요. -->

### 연동 백엔드 (현재 프로젝트)

`../` 경로의 **WebAPIFramework**는 다음 프로젝트로 구성됩니다:

- `Framework.Api` — ASP.NET Core Web API 서버 (실제 연동 대상)
- `Framework.Application` — 애플리케이션 레이어 (유스케이스)
- `Framework.Domain` — 도메인 모델 및 비즈니스 로직
- `Framework.Infrastructure` — EF Core 기반 데이터 접근
- `Framework.Admin` — Blazor Server 관리 도구

> **⚠ READ-ONLY** — `../Framework/*` 백엔드는 **읽기 전용**이다. 컨트롤러/엔티티/DTO/마이그레이션 등 어떠한 서버 측 코드도 GameClient 작업 중에는 수정·추가·삭제하지 않는다. 백엔드 부재 항목은 클라이언트 측 우회 또는 요구사항 축소로 해결한다.

## 프레임워크 방향

- 씬 관리, UI, 리소스, 사운드, 네트워크 등 공통 시스템을 재사용 가능한 구조로 구축
- 백엔드 연동(HTTP REST) 패턴을 표준화하여 다른 프로젝트에서도 그대로 활용 가능하도록 설계
- `@Scripts` 하위 구조를 유지하며 점진적으로 확장

## 씬 구성

- `LoginScene` — 인증 API 테스트 및 로그인 흐름
- `GameScene` — 게임플레이 관련 API 테스트

## 스크립트 구조

```
@Scripts/
├── Config/         공통 설정 (게임, 광고, IAP, 로컬라이제이션)
├── Controllers/    오브젝트 제어 (ObjectBase, Player)
├── Data/           데이터 모델 (ItemData, TextData)
├── Editor/         에디터 전용 도구
├── Managers/       공통 매니저 (Singleton, Event, UI, Resource, Scene, Sound 등)
├── Scenes/         씬 베이스 클래스
├── UI/             UI 베이스 클래스 및 Prefab
├── Utils/          유틸리티 (Define, Extension, Utils, PriorityQueue)
└── WebFramework/   백엔드 연동 전용 (하단 참고)
```

## WebFramework 폴더 구조 및 원칙

```
@Scripts/WebFramework/
├── Core/    UnityWebRequest 래퍼, 공통 요청/응답 처리, 에러 핸들링
├── Api/     엔드포인트별 서비스 클래스 (예: AuthApi, PlayerApi)
├── Models/  DTO — 요청/응답 데이터 모델
└── Auth/    토큰·세션 관리
```

### 재사용 원칙

- WebFramework는 기존 `@Scripts` 코드를 **재사용**하며, 중복 구현을 금지한다.
- 아래 모듈은 WebFramework에서 그대로 의존해도 된다:

| 모듈 | 용도 |
|------|------|
| `Managers/Singleton` | 싱글톤 베이스 클래스 |
| `Managers/EventManager` | API 결과 브로드캐스트 |
| `Managers/UIManager` | 로딩/에러 UI 표시 |
| `Managers/SceneManager` | API 완료 후 씬 전환 |
| `Utils/Define` | Enum, 상수 정의 추가 |
| `Utils/Extension` | 공통 확장 메서드 활용 |

- WebFramework 내에서 새 매니저가 필요하면 `Singleton<T>`를 상속하여 기존 패턴을 따른다.

> 개발 가이드:
> - Api 호출 패턴 / 신규 도메인 추가 절차 → `DEVELOPER_GUIDE.md`
> - 패턴 채택 근거 박제 → `DEVNOTES.md` `[설계 결정]` 섹션
> - 백엔드 엔드포인트 명세 → `../CLIENT_GUIDE.md`

---

# Unity Agents
You are UnityAgent, an AI assistant that controls the Unity Editor.

You are an interactive agent that helps users with Unity editor automation,
scene editing, GameObject and component management, prefab workflows,
asset management, and script generation or modification tasks.
Use the instructions below and the tools available to you to assist the user.

IMPORTANT: You must NEVER guess object names, asset paths, scene paths,
component values, or script contents. Always query the current state first.
IMPORTANT: Before deleting assets, removing objects, overwriting scripts,
or making bulk destructive changes (100+ objects, project settings,
scenes, or prefabs), always confirm with the user first.
IMPORTANT: When modifying scripts or other assets, consider Unity's
compilation and refresh flow, and verify the result after changes.