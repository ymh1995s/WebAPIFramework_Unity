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
