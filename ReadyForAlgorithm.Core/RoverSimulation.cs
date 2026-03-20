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

    public RoverSimulation(char[,] sourceTerrain)
    {
        terrain = (char[,])sourceTerrain.Clone();
        char[,] plannerGrid = (char[,])sourceTerrain.Clone();
        (start, initialGoals) = PathPlanner.FindStartAndGoals(plannerGrid);
        route = PathPlanner.BuildMissionPath(plannerGrid);

        RoverPosition = route.Count > 0 ? route[0] : start;
        Battery = 100;
        SpeedMode = RoverSpeedMode.Normal;
        StatusMessage = "Szimulacio keszen all.";
        lastNightState = IsNight;
        AppendLog("Szimulacio inicializalva.");
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
        StatusMessage = $"Sebesseg modositva: {GetSpeedLabel(speedMode)}.";
        AppendLog(StatusMessage);
    }

    public void Pause(string reason = "Szimulacio szuneteltetve.")
    {
        if (IsPaused)
        {
            return;
        }

        IsPaused = true;
        StatusMessage = reason;
        AppendLog(reason);
    }

    public void Resume(string reason = "Szimulacio folytatva.")
    {
        if (!IsPaused)
        {
            return;
        }

        if (Battery <= 0)
        {
            StatusMessage = "Az akku ures, toltodes szukseges a folytatashoz.";
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
            AppendLog(IsNight ? "Ejjel lett a Marson." : "Hajnalodott, ujra tolt a napelem.");
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

        if (Battery > 0 && StatusMessage.Contains("toltodes", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Az akku ujra hasznalhato, a szimulacio folytathato.";
        }
    }

    private void UpdateMovement(int elapsedMilliseconds)
    {
        logAccumulator += elapsedMilliseconds;
        batteryDrainAccumulator += elapsedMilliseconds;
        movementAccumulator += elapsedMilliseconds;

        if (!IsNight)
        {
            batteryChargeAccumulator += elapsedMilliseconds;
            while (batteryChargeAccumulator >= TickLogIntervalMilliseconds)
            {
                batteryChargeAccumulator -= TickLogIntervalMilliseconds;
                ChangeBattery(+10);
            }
        }

        while (batteryDrainAccumulator >= TickLogIntervalMilliseconds)
        {
            batteryDrainAccumulator -= TickLogIntervalMilliseconds;
            int speedValue = GetSpeedValue(SpeedMode);
            ChangeBattery(-(2 * speedValue * speedValue));
        }

        int stepDuration = GetStepDuration(SpeedMode);
        while (!IsPaused && !IsComplete && movementAccumulator >= stepDuration)
        {
            movementAccumulator -= stepDuration;
            AdvanceRouteStep();
        }

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
        StatusMessage = $"Rover pozicio: {RoverPosition.X}, {RoverPosition.Y}";

        if (IsGoal(RoverPosition))
        {
            AppendLog($"Minta begyujtve: {RoverPosition.X}, {RoverPosition.Y}");
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
            AppendLog($"Statusz | Akku: {Battery}% | Pozicio: {RoverPosition.X},{RoverPosition.Y} | Ido: {GetTimeLabel()} | Sebesseg: {GetSpeedLabel(SpeedMode)} | Pause: {(IsPaused ? "Igen" : "Nem")}");
        }
    }

    private void ChangeBattery(int delta)
    {
        int previous = Battery;
        Battery = Math.Clamp(Battery + delta, 0, 100);

        if (Battery <= 20 && !lowBatteryLogged)
        {
            lowBatteryLogged = true;
            AppendLog("20% - Alacsony toltottsegi szint.");
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
            ? "Az akku lemerult, nappali toltodes folyamatban. Resume amikor megfelelo."
            : "Az akku lemerult, varni kell nappalig.";

        if (!batteryStopLogged)
        {
            batteryStopLogged = true;
            AppendLog("Auto-megallas lemerules miatt.");
        }
    }

    private void MarkCompleted()
    {
        IsComplete = true;
        StatusMessage = "Cel elerve.";
        if (!completionLogged)
        {
            completionLogged = true;
            AppendLog("A kuldetes befejezodott.");
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
            RoverSpeedMode.Slow => "Lassu",
            RoverSpeedMode.Fast => "Gyors",
            _ => "Normal"
        };
    }

    private string GetTimeLabel()
    {
        int totalMinutes = (int)Math.Round(dayClockMilliseconds / 1000d * 15d, MidpointRounding.AwayFromZero);
        int hours = (totalMinutes / 60) % 24;
        int minutes = totalMinutes % 60;
        return $"{hours:00}:{minutes:00} {(IsNight ? "Ejjel" : "Nappal")}";
    }
}