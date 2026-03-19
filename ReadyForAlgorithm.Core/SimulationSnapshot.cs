namespace ReadyForAlgorithm.Core;

public sealed record SimulationSnapshot(
    char[,] Terrain,
    GridPosition Start,
    GridPosition RoverPosition,
    IReadOnlyList<GridPosition> RemainingGoals,
    int CollectedGoalCount,
    int TotalGoalCount,
    int Battery,
    RoverSpeedMode SpeedMode,
    bool IsPaused,
    bool IsNight,
    bool IsComplete,
    bool IsBatteryDepleted,
    int DayClockMilliseconds,
    string TimeLabel,
    IReadOnlyList<RoverLogEntry> Logs,
    string StatusMessage);