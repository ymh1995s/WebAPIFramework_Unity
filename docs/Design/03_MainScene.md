# 03. MainScene

## 1. 요구사항

요구사항.md `## MainScene` 9개 버튼:
- 구글 연동 / 메일 박스 / 랭킹 / 인벤토리 / 공지사항 / 문의 / 광고(미구현) / 인앱결제(미구현) / 스테이지 선택 / 로그아웃 / 계정 삭제

부가:
- CLIENT_GUIDE 11번(일일 로그인 트리거), 12번(쇼아웃 폴링)을 메인 진입 시 자동 실행

## 2. 1차 대비 변경점

### 2.1 메인 진입 자동 트리거 추가

요구사항.md 미명시지만 **CLIENT_GUIDE 11/12번**이 메인 로비 진입 직후 호출하라고 명시. 본 라운드에 박제:

| 트리거 | Endpoint | UI |
|---|---|---|
| 일일 로그인 보상 | `POST /api/dailylogin` | `rewarded=true` 시 `UI_AnnouncementPopup`("일일 보상이 우편함에 도착했습니다") — 선택사항이지만 권장 |
| 1회 공지(Shout) | `GET /api/shouts/active` | 응답 배열 비어있지 않으면 첫 메시지를 `UI_AnnouncementPopup`으로 표시. 폴링은 본 라운드 미도입 (1회 호출만) |

`Scene_Main.Start`에서 순차 호출. 둘 다 실패해도 메인 진입 자체는 막지 않음(silent log).

### 2.2 메일 — `claim-all` 폐기, 단건 N회 호출

**1차 박제 오류**: `MailApi.ClaimAll` 가정. **실측: `/api/mails/claim-all` 백엔드 부재**. 수령은 `POST /api/mails/{id}/claim` 단건만.

**결정**: "전체 수령 버튼"은 **클라가 미수령 우편 ID 리스트를 순회하며 단건 claim을 N회 호출**. 실패한 항목은 누적해서 마지막에 `UI_ErrorPopup`(또는 무시) 처리. 백엔드 신설은 **권장하지 않음** — N이 크지 않고(우편함 누적은 보통 수십 건 이내), 멱등(`isClaimed=true` 시 400 반환)이라 부분 실패 회복도 안전.

의사 흐름:
```
ClaimAllBtn 클릭
  → mails.Where(m => !m.isClaimed).Select(m => m.id) 추출
  → 순차로 MailApi.Claim(id) 호출 (병렬은 백엔드 동시성 부담)
  → 모두 끝나면 MailApi.GetList 재호출 → popup 텍스트 갱신
```

병렬 N회 vs 순차 N회: 순차 채택. CLIENT_GUIDE 14번이 동시성 충돌(409/낙관락) 가능성을 언급. 순차가 더 단순 + 안전.

### 2.3 메일 응답 구조 정정

**1차 가정**: `mailItems[]`만 존재. **실측 (`MailDto`)**:
```
{
  id, playerId, title, body,
  itemId?, itemName?, itemCount,         // deprecated 단일 호환
  isRead, isClaimed, createdAt, expiresAt,
  exp,                                   // 우편 첨부 경험치
  mailItems: [ {itemId, itemName, quantity}, ... ]
}
```

**클라 처리**: `mailItems`만 우선 사용(다중 표준). 단일 호환 필드는 무시. `expiresAt`(시각) → `~Nd` 포맷 변환:
```csharp
int days = Math.Max(0, (int)Math.Ceiling((expiresAt - DateTime.UtcNow).TotalDays));
string expire = days > 0 ? $"~{days}일" : "오늘 만료";
```
`exp` > 0이면 별도 줄로 표기: `"경험치, {exp}, ~Nd"` 또는 메일 본문 끝에 추가.

### 2.4 인벤토리 — 결정: 백엔드 부재 (07 문서 결론)

**1차 가정**: `/api/inventory` 호출. **실측 부재**. 결정:

| 옵션 | 평가 |
|---|---|
| A. 백엔드 신설 (`GET /api/inventory`) | 깔끔. 1개 endpoint면 충분 |
| B. 메일 수령 응답으로 클라가 누적 추적 | 신뢰성 매우 낮음 (앱 재설치/캐시 손실 시 손실) |
| C. 요구사항 축소 — "이 빌드에서는 인벤토리 미지원" 안내 표시 | 간단. 더미 클라이언트 단계라 수용 가능 |

**채택: C → A 권장(별도 라운드)**. 본 라운드는 C로 처리하되 **백엔드 신설을 사용자 의사결정 항목으로 보고**(보고서 섹션 2/3 참조). 클라 우회는 안 함(데이터 손실 위험).

UI 동작:
```
InventoryBtn 클릭
  → PopupService.ShowAnnouncement("인벤토리는 향후 업데이트에서 제공됩니다")
```

이후 백엔드에 `GET /api/inventory` 가 추가되면 `InventoryApi`+`InventoryModels`+버튼 핸들러 본 흐름으로 교체. 이 시점의 변경 분량은 작다.

### 2.5 공지사항 — 단건 표시로 결정 (07 문서 결론)

**1차 가정**: `/api/notices` 리스트. **실측 부재 (단건만)**.

**채택**: 단건만 표시. `UI_ConfirmPopup` 텍스트에 가장 최신 공지의 `content` 1건. 백엔드 신설 권장 안 함(현 빌드 가치 대비 비용 큼).

### 2.6 구글 연동 버튼 — 상태 판정 정정

**1차 가정**: `AuthManager.IsGoogleLinked` 가 항상 정확. 실측: 백엔드 응답에 `isGoogleLinked` 필드 없음.

**결정**: `IsGoogleLinked`는 **클라 추적 플래그(PlayerPrefs)** 로 유지. 갱신 시점:
- 구글 직접 로그인 200 → `true`
- 게스트 → 구글 연동(`/api/auth/link/google`) 200 → `true`
- 게스트 로그인 200 → `false`
- 충돌 해소 200 → `true`

**한계**: 다른 기기에서 같은 계정으로 연동 후, 이 기기에서 동일 RefreshToken으로 자동 로그인 시 클라 캐시는 여전히 `false`일 수 있음. 더미 검증 단계에선 수용. 신뢰 필요 시 `GET /api/auth/me` 같은 프로필 조회 endpoint를 백엔드에 신설(권장 2순위).

연동 버튼 동작 (요구사항: "이미 구글 연동이면, 유니티 디버그 로그에 실패 출력"):
```
if (AuthManager.IsGoogleLinked) {
    Debug.Log("[GoogleLink] 이미 구글 연동된 계정입니다.");
    return;
}
GoogleSignInProvider.SignIn → idToken
AuthApi.LinkGoogle(idToken,
  onSuccess: _ => { AuthManager.SetGoogleLinked(true);
                    PopupService.ShowAnnouncement("구글 연동 완료"); },
  onError: err => {
    if (err.Status == 409 && err.ErrorCode == "GOOGLE_ACCOUNT_CONFLICT")
        ShowConflictResolve(err);  // 02 문서와 동일한 흐름
    else PopupService.ShowError(err.UserMessage);
  });
```

### 2.7 문의 — 응답 구조 정정

**실측 (`InquiryDto`)**:
```
{ id, content, adminReply, repliedAt, createdAt }
```
1차 가정의 `{message, answer, answeredAt}` 폐기.

요구사항: "메세지는 고정 `{닉네임의 문의입니다}` 포멧". 닉네임 출처는 백엔드 응답에 부재 → 임시로 `Player {playerId}의 문의입니다` 사용. 닉네임 필요 시 위 2.6의 `/api/auth/me` 도입.

문의 목록 텍스트 포맷:
```
[YYYY-MM-DD HH:mm] {content}
└ 답변: {adminReply}  (repliedAt 있을 때만)
```
각 문의마다 개행(요구사항).

### 2.8 랭킹 응답 정정

**실측**: `{rank, playerId, nickname, bestScore}`. 1차의 `{rank, score}` 폐기.
요구사항: "유니티 디버그 로그에 출력".
```csharp
RankingApi.Instance.GetMyRank(
  onSuccess: r => Debug.Log($"[Ranking] {r.nickname} - {r.rank}위 / 점수 {r.bestScore}"),
  onError:   err => PopupService.ShowError(err.UserMessage));
```

### 2.9 로그아웃 / 탈퇴 정정

- 로그아웃: `POST /api/auth/logout` Body=`{refreshToken}`, 응답 204. **본문 없음** → `EmptyResponse`. 멱등.
- 탈퇴: `DELETE /api/auth/withdraw`, 응답 204. **본문 없음**. 멱등.

탈퇴 후 처리: 메모리/PlayerPrefs 토큰 + IsGoogleLinked + DeviceId 모두 삭제(CLIENT_GUIDE 10번). 그 후 LoginScene.

법적 의무 안내(CLIENT_GUIDE 10번 + 부록 A) — 더미 검증 단계지만 박제: `UI_SelectPopup`의 본문에 캐릭터/아이템/우편 영구 손실 명시 필수.

## 3. 씬 구성

- `MainScene.unity` 루트에 `Scene_Main` 부착
- `Scene_Main.Start`에서 `UIManager.ShowSceneUI<UI_MainScene>` + 자동 트리거 2건(일일로그인/쇼아웃)

## 4. UI 구조

`UI_MainScene` (UI_UGUI + IUI_Scene):
```csharp
enum Buttons
{
    GoogleLinkBtn, MailboxBtn, RankingBtn, InventoryBtn,
    NoticeBtn, InquiryBtn, AdsBtn, IapBtn,
    StageSelectBtn, LogoutBtn, DeleteAccountBtn
}
```
AdsBtn / IapBtn 은 `Interactable=false` (요구사항 "구현하지 않는다").

팝업 클래스 → 프리팹(이미 존재) 매핑:
| 컨트롤러 | 프리팹명 | 용도 |
|---|---|---|
| `UI_MailPopup` | `UI_MailPopup.prefab` | 메일 목록 + Exit + 전체 수령 |
| `UI_ConfirmPopup` | `UI_ConfirmPopup.prefab` | 공지(단건) — 인벤토리 안내 메시지도 동일 |
| `UI_SelectPopup` | `UI_SelectPopup.prefab` | 계정 삭제 확인 + 구글 충돌 해소 |
| `UI_InquiryPopup` | `UI_InquiryPopup.prefab` | 문의 작성 + 목록 |

## 5. 텍스트 포맷 (요구사항 `{이름, 개수, ~Yd}` 패턴)

```csharp
public static string FormatMailItem(MailItemDto i, DateTime? expireAt)
{
    string expire = expireAt.HasValue ? $"~{ToDays(expireAt.Value)}일" : "영구";
    return $"{{{i.itemName}, {i.quantity}개, {expire}}}";
}
```
- 메일은 `expiresAt`이 메일 단위로 있음 → 모든 mailItems가 같은 만료일 공유
- 인벤토리(만약 도입 시): 백엔드 PlayerItem 엔티티에 만료 개념 **없음**(실측). 즉 인벤토리 표기는 항상 `영구`. 보고서에서 백엔드 신설 시 `expiresAt` 필드를 PlayerItem에 추가할지 결정 필요(권장: 추가 안 함 — 시스템 단순화).
- 공통 헬퍼는 `Assets/@Scripts/Utils/ItemFormatter.cs` 신설 권장(이전 라운드와 동일).

## 6. WebFramework 매핑

| 기능 | Endpoint (Method) | 인증 | 응답 |
|---|---|---|---|
| 일일로그인 | `POST /api/dailylogin` | O | `{rewarded:bool}` |
| 1회 공지 | `GET /api/shouts/active` | O | `[{id,message,createdAt,expiresAt},...]` (빈 배열 200) |
| 메일 목록 | `GET /api/mails` | O | `[MailDto, ...]` |
| 메일 수령 | `POST /api/mails/{id}/claim` | O | 200(성공) / 400(이미 수령/없음) |
| 랭킹(나) | `GET /api/ranking/me` | O | `{rank, playerId, nickname, bestScore}` |
| 공지(단건) | `GET /api/notices/latest` | X | `{id, content}` 또는 204 |
| 문의 제출 | `POST /api/inquiries` | O | 201, 본문 없음 |
| 문의 목록 | `GET /api/inquiries` | O | `[{id,content,adminReply,repliedAt,createdAt},...]` |
| 구글 연동 | `POST /api/auth/link/google` | O | 200 본문 없음 / 409 GoogleConflictDto |
| 로그아웃 | `POST /api/auth/logout` | O | 204 |
| 탈퇴 | `DELETE /api/auth/withdraw` | O | 204 |

## 7. 데이터 흐름 (대표 — 메일 박스)

```
MailboxBtn → MailApi.GetList()
          → ApiClient.Get /api/mails (Bearer 자동)
          → 200 List<MailDto>
          → UI_MailPopup.SetItems(list, ItemFormatter)
            ├─ ExitBtn → UIManager.ClosePopupUI
            └─ ClaimAllBtn → 미수령 ID 순차 Claim → GetList 재호출 → SetItems
```

## 8. 영향받는 파일/에셋

**신규**
- `Scenes/Scene_Main.cs`
- `UI/UI_MainScene.cs`
- 팝업 컨트롤러 4개: `UI_MailPopup`, `UI_ConfirmPopup`, `UI_SelectPopup`, `UI_InquiryPopup`
- API 클래스 7개: `MailApi`, `RankingApi`, `InquiryApi`, `DailyLoginApi`, `ShoutApi`, (NoticeApi/AccountApi=AuthApi 확장)
- Models 7개 파일
- `Utils/ItemFormatter.cs`

**수정 없음** — 프리팹 4종은 이미 `Resources/PreLoad/Prefabs/UI/` 에 존재 (00 문서 인덱스에 따름).

## 9. 트레이드오프 — `claim-all` 백엔드 신설?

| 옵션 | 장점 | 단점 |
|---|---|---|
| A. 클라 N회 단건 호출 (채택) | 백엔드 변경 0. 멱등이라 부분 실패 안전. Rate Limit `game` 분당 120회 여유 충분 | N개 만큼 네트워크 라운드 트립. 우편 100개일 때 체감 가능 |
| B. 백엔드 `/api/mails/claim-all` 신설 | 1회 호출 | 동시성 처리(여러 우편을 한 트랜잭션) 복잡도 증가. 부분 성공/실패 응답 정의 부담 |

**A 채택**. 더미 검증 단계에서 일반적인 우편 누적량(수십 건 이내)이면 N회 단건 호출이 단순함·일관성에서 우위.

## 10. 위험 요소

- 일일 로그인이 우편을 발송 → 메일 박스 진입 시점에 그 우편을 보임. 메인 진입 직후 `MailApi.GetList`까지 자동 호출할지 vs 사용자가 메일박스 버튼 누를 때만 호출할지 → 후자(요구사항대로). 일일 보상 알림은 텍스트만.
- 구글 충돌 해소 흐름은 LoginScene과 MainScene 양쪽에서 발생 가능. 충돌 처리 로직을 `Utils/GoogleConflictResolver.cs` 정적 헬퍼로 추출해 재사용 권장.
- `DELETE /api/auth/withdraw`는 본문 없음. `ApiClient.Delete<TRes>`가 빈 응답을 어떻게 다룰지 명세(`EmptyResponse` + 204 처리, 07 문서).

## 11. DoD

- [ ] MainScene 진입 시 UI_MainScene 인스턴싱
- [ ] 진입 직후 `POST /api/dailylogin` + `GET /api/shouts/active` 자동 호출
- [ ] 11개 버튼 onClick 정상 바인딩 (Ads/Iap는 비활성)
- [ ] 메일 박스: 목록 표시 + 단건/전체 수령(N회) + 갱신
- [ ] 인벤토리: "향후 제공" 안내(백엔드 부재 결정)
- [ ] 공지: 단건만 표시
- [ ] 문의: 작성/목록 + 답변 표시
- [ ] 구글 연동: 미연동 시 LinkGoogle, 연동 시 Debug.Log 출력
- [ ] 로그아웃/탈퇴: 토큰·DeviceId 정리 후 LoginScene
- [ ] 모든 API 에러 → UI_ErrorPopup
