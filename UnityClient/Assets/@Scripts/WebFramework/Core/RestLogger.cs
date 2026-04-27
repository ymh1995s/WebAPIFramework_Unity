using System;

// REST 통신 로그를 UI 패널로 전달하는 정적 로거
public static class RestLogger
{
    // 로그 레벨 구분
    public enum LogLevel { Info, Warn, Error }

    // 로그 항목 구조체
    public struct LogEntry
    {
        public LogLevel Level;
        public string   Message;
        public DateTime Time;
    }

    // RestLogPanel이 구독하는 이벤트
    public static event Action<LogEntry> OnLog;

    // 외부에서 사용하는 로그 메서드
    public static void Info(string msg)  => Log(LogLevel.Info,  msg);
    public static void Warn(string msg)  => Log(LogLevel.Warn,  msg);
    public static void Error(string msg) => Log(LogLevel.Error, msg);

    private static void Log(LogLevel level, string msg)
    {
        OnLog?.Invoke(new LogEntry { Level = level, Message = msg, Time = DateTime.Now });
    }
}
