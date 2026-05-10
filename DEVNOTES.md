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
| 인벤토리 조회 / 아이템 사용 | 완료 | `ItemApi` 통합 (GET /api/items/inventory, POST /api/items/{id}/use), `UI_InventoryPopup` 프리팹 |
| 씬 전환 로딩 + 페이드 | 완료 | `FadeManager`, `LoadingScene`, `UI_LoadingScene` |
| Android 뒤로가기 | 완료 | `BackButtonHandler`, `BaseScene.OnBackButton` 가상 메서드 |
| Safe Area | 완료 | `SafeAreaFitter` 컴포넌트 — 모든 씬/팝업 프리팹 적용 |
| 앱 생명주기 (포그라운드 복귀) | 완료 | `AppLifecycleManager` — 토큰 갱신·점검 재확인 |
| 토스트 UI | 완료 | `UI_Toast`, `PopupService.ShowToast` |

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
