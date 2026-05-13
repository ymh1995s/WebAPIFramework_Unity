using System;
using System.Globalization;
using System.Threading;
using UnityEngine;

// 서버 응답 Date 헤더를 기반으로 클라이언트 시계와의 오프셋을 보정하는 정적 유틸리티
// 모든 API 응답마다 UpdateFromHeader가 호출되어 오프셋을 갱신
// 멀티스레드 안전: _offsetTicks는 Interlocked으로 보호
public static class ServerTime
{
    // 서버-클라이언트 시간 차이(틱 단위) — Interlocked으로 원자적 읽기/쓰기
    private static long _offsetTicks;

    // 최소 1회 동기화 완료 여부 — volatile로 읽기 순서 보장
    private static volatile bool _isSynced;

    // 미동기화 경고 1회 출력 제어 (매 호출마다 로그 폭주 방지) — volatile로 멀티스레드 가시성 보장
    private static volatile bool _unsyncedWarned;

    // Date 헤더 파싱 실패 경고 1회 출력 제어 — volatile로 멀티스레드 가시성 보장
    private static volatile bool _parseFailWarnedOnce;

    // ====================================================
    // 공개 속성
    // ====================================================

    // 서버 오프셋이 보정된 UTC 시각
    // 미동기화 상태이면 로컬 UTC를 반환하되, 최초 1회 경고 로그 출력
    public static DateTime UtcNow
    {
        get
        {
            if (!_isSynced && !_unsyncedWarned)
            {
                RestLogger.Warn("[ServerTime] 미동기화 상태 — 로컬 UTC 사용");
                _unsyncedWarned = true;
            }

            // 오프셋이 0이면 로컬 UTC와 동일, 비용 거의 없음
            return DateTime.UtcNow.AddTicks(Interlocked.Read(ref _offsetTicks));
        }
    }

    // KST(UTC+9) 기준 현재 시각 — Kind = Unspecified 허용
    public static DateTime Now => UtcNow.AddHours(9);

    // Unix 밀리초 타임스탬프 — 서버 측 기록값과 비교할 때 사용
    public static long UnixMs
        => (long)(UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

    // 동기화 완료 여부
    public static bool IsSynced => _isSynced;

    // 현재 적용 중인 오프셋 — 디버그/모니터링 목적으로만 사용
    public static TimeSpan Offset => new TimeSpan(Interlocked.Read(ref _offsetTicks));

    // ====================================================
    // 공개 메서드
    // ====================================================

    // HTTP 응답 Date 헤더를 파싱하여 오프셋을 갱신
    // 헤더가 없거나 비어 있으면 즉시 반환 (정상적으로 발생 가능한 경우이므로 경고 없음)
    public static void UpdateFromHeader(string dateHeader)
    {
        if (string.IsNullOrEmpty(dateHeader))
            return;

        // RFC 1123 형식("r") 파싱 — HTTP Date 헤더 표준 포맷
        if (!DateTimeOffset.TryParseExact(
                dateHeader,
                "r",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var dto))
        {
            // 파싱 실패는 헤더 포맷 이상이므로 1회만 경고
            if (!_parseFailWarnedOnce)
            {
                RestLogger.Warn($"[ServerTime] Date 헤더 파싱 실패: {dateHeader}");
                _parseFailWarnedOnce = true;
            }
            return;
        }

        // 서버 UTC와 로컬 UTC의 차이를 틱으로 환산하여 저장
        long newOffsetTicks = (dto.UtcDateTime - DateTime.UtcNow).Ticks;
        Interlocked.Exchange(ref _offsetTicks, newOffsetTicks);
        _isSynced = true;
    }

    // 에디터에서 Play 재진입 시 static 필드가 이전 상태를 유지하는 문제 방지
    // SubsystemRegistration 단계에서 호출 — 모든 static 필드를 초기값으로 리셋
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        _offsetTicks        = 0L;
        _isSynced           = false;
        _unsyncedWarned     = false;
        _parseFailWarnedOnce = false;
    }

    // 동기화 여부를 확인하고 미동기화 시 경고 로그 출력
    // 예외를 던지지 않으므로 호출부에서 별도 try-catch 불필요
    public static void AssertSynced()
    {
        if (!IsSynced)
            RestLogger.Warn("[ServerTime] AssertSynced: 미동기화 상태입니다.");
    }
}
