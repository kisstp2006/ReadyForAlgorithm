namespace ReadyForAlgorithm.Core;

public sealed class RoverSimulation
{
    private const int FullDayMilliseconds = 96_000;
    private const int DaylightMilliseconds = 64_000;
    private const int TickLogIntervalMilliseconds = 2_000;

    private readonly char[,] terrain;
    private readonly GridPosition start;
    private readonly List<GridPosition> initialGoals;
    private readonly List<GridPosition> route;
    private readonly List<RoverLogEntry> logs = new();

    private int routeIndex;
    private int batteryDrainAccumulator;
    private int batteryChargeAccumulator;
    private int standbyDrainAccumulator;
    private int logAccumulator;
    private int dayClockMilliseconds;
    private int movementAccumulator;
    private int tickCounter;
    private bool lowBatteryLogged;
    private bool completionLogged;
    private bool batteryStopLogged;
    private bool lastNightState;

    private int miningTimeAccumulator = 0;
    private const int MiningDuration = 2000;

    public RoverSimulation(char[,] sourceTerrain)
    {
        terrain = (char[,])sourceTerrain.Clone();
        char[,] plannerGrid = (char[,])sourceTerrain.Clone();
        (start, initialGoals) = PathPlanner.FindStartAndGoals(plannerGrid);
        route = PathPlanner.BuildMissionPath(plannerGrid);

        RoverPosition = route.Count > 0 ? route[0] : start;
        Battery = 100;
        SpeedMode = RoverSpeedMode.Normal;
        StatusMessage = "The Simulation is ready for start";
        lastNightState = IsNight;
        AppendLog("Simulation initialized");
    }

    public GridPosition RoverPosition { get; private set; }

    public int Battery { get; private set; }

    public RoverSpeedMode SpeedMode { get; private set; }

    public bool IsPaused { get; private set; }

    public bool IsComplete { get; private set; }

    public int DayClockMilliseconds => dayClockMilliseconds;

    public bool IsNight => dayClockMilliseconds >= DaylightMilliseconds;

    public string StatusMessage { get; private set; }

    public IReadOnlyList<RoverLogEntry> Logs => logs;

    public IReadOnlyList<GridPosition> RemainingGoals
    {
        get
        {
            List<GridPosition> collected = route
                .Take(routeIndex + 1)
                .Where(position => IsGoal(position))
                .Distinct()
                .ToList();

            return initialGoals.Where(goal => !collected.Contains(goal)).ToList();
        }
    }

    public static RoverSimulation CreateFromFile(string? path = null)
    {
        string[] lines = MapLoader.LoadLines(path);
        return new RoverSimulation(MapLoader.LoadGrid(lines));
    }

    public void SetSpeed(RoverSpeedMode speedMode)
    {
        if (SpeedMode == speedMode)
        {
            return;
        }

        SpeedMode = speedMode;
        StatusMessage = $"Speed changed: {GetSpeedLabel(speedMode)}.";
        AppendLog(StatusMessage);
    }

    public void Pause(string reason = "Paused? Yes")
    {
        if (IsPaused)
        {
            return;
        }

        IsPaused = true;
        StatusMessage = reason;
        AppendLog(reason);
    }

    public void Resume(string reason = "Paused? No")
    {
        if (!IsPaused)
        {
            return;
        }

        if (Battery <= 0)
        {
            StatusMessage = "Battery is dead, charging is necessary";
            AppendLog(StatusMessage);
            return;
        }

        IsPaused = false;
        StatusMessage = reason;
        AppendLog(reason);
    }

    public void TogglePause()
    {
        if (IsPaused)
        {
            Resume();
            return;
        }

        Pause();
    }

    public void Update(int elapsedMilliseconds)
    {
        if (elapsedMilliseconds <= 0)
        {
            return;
        }

        tickCounter += elapsedMilliseconds;
        UpdateDayNight(elapsedMilliseconds);

        if (IsComplete)
        {
            UpdatePassiveSystems(elapsedMilliseconds, allowCharging: false);
            return;
        }

        if (IsPaused)
        {
            UpdatePassiveSystems(elapsedMilliseconds, allowCharging: true);
            return;
        }

        UpdateMovement(elapsedMilliseconds);
    }

    public SimulationSnapshot CreateSnapshot()
    {
        IReadOnlyList<GridPosition> remainingGoals = RemainingGoals;
        return new SimulationSnapshot(
            Terrain: terrain,
            Start: start,
            RoverPosition: RoverPosition,
            RemainingGoals: remainingGoals,
            CollectedGoalCount: initialGoals.Count - remainingGoals.Count,
            TotalGoalCount: initialGoals.Count,
            Battery: Battery,
            SpeedMode: SpeedMode,
            IsPaused: IsPaused,
            IsNight: IsNight,
            IsComplete: IsComplete,
            IsBatteryDepleted: Battery <= 0,
            DayClockMilliseconds: dayClockMilliseconds,
            TimeLabel: GetTimeLabel(),
            Logs: logs.ToArray(),
            StatusMessage: StatusMessage);
    }

    private void UpdateDayNight(int elapsedMilliseconds)
    {
        dayClockMilliseconds = (dayClockMilliseconds + elapsedMilliseconds) % FullDayMilliseconds;
        if (lastNightState != IsNight)
        {
            lastNightState = IsNight;
            AppendLog(IsNight ? "Evening time on Mars" : "Morning time, the battery is charging");
        }
    }

    private void UpdatePassiveSystems(int elapsedMilliseconds, bool allowCharging)
    {
        standbyDrainAccumulator += elapsedMilliseconds;
        while (standbyDrainAccumulator >= TickLogIntervalMilliseconds)
        {
            standbyDrainAccumulator -= TickLogIntervalMilliseconds;
            ChangeBattery(-1);
        }

        if (allowCharging && !IsNight)
        {
            batteryChargeAccumulator += elapsedMilliseconds;
            while (batteryChargeAccumulator >= TickLogIntervalMilliseconds)
            {
                batteryChargeAccumulator -= TickLogIntervalMilliseconds;
                ChangeBattery(+10);
            }
        }

        EmitPeriodicStatus(elapsedMilliseconds);

        if (Battery > 0 && StatusMessage.Contains("charging", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "The battery can be used again, the simulation can continue.";
        }
    }

    private void UpdateMovement(int elapsedMilliseconds)
    {
        logAccumulator += elapsedMilliseconds;
        batteryDrainAccumulator += elapsedMilliseconds;

        // 1. Napelemes töltés (ha nappal van)
        if (!IsNight)
        {
            batteryChargeAccumulator += elapsedMilliseconds;
            while (batteryChargeAccumulator >= TickLogIntervalMilliseconds)
            {
                batteryChargeAccumulator -= TickLogIntervalMilliseconds;
                ChangeBattery(+10);
            }
        }

        // 2. Energiafogyasztás kezelése (Bányászat vagy Mozgás alapján)
        while (batteryDrainAccumulator >= TickLogIntervalMilliseconds)
        {
            batteryDrainAccumulator -= TickLogIntervalMilliseconds;
            if (miningTimeAccumulator > 0)
            {
                ChangeBattery(-2); // Bányászati fogyasztás (1 egység / mp)
            }
            else
            {
                int speedValue = GetSpeedValue(SpeedMode);
                ChangeBattery(-(2 * speedValue * speedValue)); // Mozgási fogyasztás
            }
        }

        // 3. Cselekvés kezelése
        if (miningTimeAccumulator > 0)
        {
            // Éppen bányászunk, nem mozdulunk, csak az idõ telik
            miningTimeAccumulator -= elapsedMilliseconds;
            if (miningTimeAccumulator <= 0)
            {
                miningTimeAccumulator = 0;
                StatusMessage = "Mining is finished!";
            }
        }
        else if (IsPaused == false && IsComplete == false)
        {
            // Ha nincs megállítva és nem bányászunk, akkor haladunk a következõ mezõre
            movementAccumulator += elapsedMilliseconds;
            int stepDuration = GetStepDuration(SpeedMode);
            while (movementAccumulator >= stepDuration)
            {
                movementAccumulator -= stepDuration;
                AdvanceRouteStep();
            }
        }

        // 4. Állapotnaplózás (periodic log)
        EmitPeriodicStatus(0);
    }

    private void AdvanceRouteStep()
    {
        if (Battery <= 0)
        {
            PauseForBattery();
            return;
        }

        if (routeIndex >= route.Count - 1)
        {
            MarkCompleted();
            return;
        }

        routeIndex++;
        RoverPosition = route[routeIndex];
        StatusMessage = $"Rover position: {RoverPosition.X}, {RoverPosition.Y}";

        if (IsGoal(RoverPosition))
        {
            AppendLog($"Precious stone detected! Mining begins: {RoverPosition.X}, {RoverPosition.Y}");
            miningTimeAccumulator = MiningDuration;
        }

        if (routeIndex >= route.Count - 1)
        {
            MarkCompleted();
        }
    }

    private void EmitPeriodicStatus(int extraMilliseconds)
    {
        logAccumulator += extraMilliseconds;
        while (logAccumulator >= TickLogIntervalMilliseconds)
        {
            logAccumulator -= TickLogIntervalMilliseconds;
            AppendLog($"Status | Battery: {Battery}% | Position: {RoverPosition.X},{RoverPosition.Y} | Time: {GetTimeLabel()} | Speed: {GetSpeedLabel(SpeedMode)} | Paused?: {(IsPaused ? "Yes" : "No")}");
        }
    }

    private void ChangeBattery(int delta)
    {
        int previous = Battery;
        Battery = Math.Clamp(Battery + delta, 0, 100);

        if (Battery <= 20 && !lowBatteryLogged)
        {
            lowBatteryLogged = true;
            AppendLog("20% - Low battery level.");
        }
        else if (Battery > 20)
        {
            lowBatteryLogged = false;
        }

        if (previous > 0 && Battery <= 0)
        {
            PauseForBattery();
        }
    }

    private void PauseForBattery()
    {
        IsPaused = true;
        StatusMessage = !IsNight
            ? "The battery is dead, morning charging is in process. Press Resume when ready."
            : "The battery is dead, you must wait till morning.";

        if (!batteryStopLogged)
        {
            batteryStopLogged = true;
            AppendLog("Auto-pause due to the battery being dead.");
        }
    }

    private void MarkCompleted()
    {
        IsComplete = true;
        StatusMessage = "Goal Reached!";
        if (!completionLogged)
        {
            completionLogged = true;
            AppendLog("The mission is accomplished.");
        }
    }

    private bool IsGoal(GridPosition position)
    {
        return terrain[position.Y, position.X] is 'G' or 'Y' or 'B';
    }

    private void AppendLog(string message)
    {
        logs.Add(new RoverLogEntry(tickCounter, message));
        if (logs.Count > 500)
        {
            logs.RemoveAt(0);
        }
    }

    private static int GetStepDuration(RoverSpeedMode speedMode)
    {
        return speedMode switch
        {
            RoverSpeedMode.Slow => 2000,
            RoverSpeedMode.Fast => 667,
            _ => 1000
        };
    }

    private static int GetSpeedValue(RoverSpeedMode speedMode)
    {
        return speedMode switch
        {
            RoverSpeedMode.Slow => 1,
            RoverSpeedMode.Fast => 3,
            _ => 2
        };
    }

    private static string GetSpeedLabel(RoverSpeedMode speedMode)
    {
        return speedMode switch
        {
            RoverSpeedMode.Slow => "Slow",
            RoverSpeedMode.Fast => "Fast",
            _ => "Normal"
        };
    }

    private string GetTimeLabel()
    {
        int totalMinutes = (int)Math.Round(dayClockMilliseconds / 1000d * 15d, MidpointRounding.AwayFromZero);
        int hours = (totalMinutes / 60) % 24;
        int minutes = totalMinutes % 60;
        return $"{hours:00}:{minutes:00} {(IsNight ? "Evening" : "Morning")}";
    }
}