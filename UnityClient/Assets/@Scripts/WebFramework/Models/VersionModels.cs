using System;

// 버전 체크 응답 모델 - 강제 업데이트 여부 및 최신 버전 정보
[Serializable]
public class VersionCheckResponse
{
    // 강제 업데이트 필요 여부 - true이면 앱 업데이트 없이 진행 불가
    public bool   isForceUpdate;

    // 서버가 인식하는 최신 앱 버전 문자열
    public string latestVersion;
}
