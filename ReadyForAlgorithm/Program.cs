using System.Diagnostics;
using System.Text;
using ReadyForAlgorithm.Core;

namespace Program;

internal static class Program
{
    private static int previousRenderLength;

    private static void Main(string[] args)
    {
        RoverSimulation simulation = RoverSimulation.CreateFromFile(args.FirstOrDefault());

        Console.CursorVisible = false;
        Stopwatch stopwatch = Stopwatch.StartNew();
        long lastTick = stopwatch.ElapsedMilliseconds;
        bool exitRequested = false;

        while (!exitRequested)
        {
            HandleInput(simulation, ref exitRequested);

            long currentTick = stopwatch.ElapsedMilliseconds;
            int elapsedMilliseconds = (int)(currentTick - lastTick);
            lastTick = currentTick;

            simulation.Update(elapsedMilliseconds);
            SimulationSnapshot snapshot = simulation.CreateSnapshot();
            Render(snapshot);

            if (snapshot.IsComplete)
            {
                break;
            }

            Thread.Sleep(50);
        }

        SimulationSnapshot finalSnapshot = simulation.CreateSnapshot();
        Render(finalSnapshot, showExitPrompt: true);
        Console.ReadKey(true);
    }

    private static void HandleInput(RoverSimulation simulation, ref bool exitRequested)
    {
        while (Console.KeyAvailable)
        {
            ConsoleKey key = Console.ReadKey(true).Key;
            switch (key)
            {
                case ConsoleKey.S:
                    simulation.SetSpeed(RoverSpeedMode.Slow);
                    break;
                case ConsoleKey.N:
                    simulation.SetSpeed(RoverSpeedMode.Normal);
                    break;
                case ConsoleKey.F:
                    simulation.SetSpeed(RoverSpeedMode.Fast);
                    break;
                case ConsoleKey.Escape:
                    simulation.TogglePause();
                    break;
                case ConsoleKey.Q:
                    exitRequested = true;
                    break;
            }
        }
    }

    private static void Render(SimulationSnapshot snapshot, bool showExitPrompt = false)
    {
        StringBuilder builder = new();
        builder.AppendLine("Mars Rover Console Frontend");
        builder.AppendLine("Controls: S slow | N normal | F fast | Esc pause/resume | Q quit");
        builder.AppendLine($"Status: {snapshot.StatusMessage}");
        builder.AppendLine();

        AppendMap(builder, snapshot);
        builder.AppendLine();
        builder.AppendLine($"Battery: {snapshot.Battery}%");
        builder.AppendLine($"Speed: {GetSpeedLabel(snapshot.SpeedMode)}");
        builder.AppendLine($"Paused: {(snapshot.IsPaused ? "Yes" : "No")}");
        builder.AppendLine($"Time: {snapshot.TimeLabel}");
        builder.AppendLine($"Position: {snapshot.RoverPosition.X}, {snapshot.RoverPosition.Y}");
        builder.AppendLine($"Samples: {snapshot.CollectedGoalCount}/{snapshot.TotalGoalCount}");
        builder.AppendLine($"Remaining goals: {snapshot.RemainingGoals.Count}");
        builder.AppendLine();
        builder.AppendLine("Recent log:");

        foreach (RoverLogEntry log in snapshot.Logs.TakeLast(10))
        {
            builder.AppendLine($"[{FormatLogTime(log.Tick)}] {log.Message}");
        }

        if (showExitPrompt)
        {
            builder.AppendLine();
            builder.AppendLine("Press any key to exit...");
        }

        string output = builder.ToString();
        Console.SetCursorPosition(0, 0);
        Console.Write(output);

        if (output.Length < previousRenderLength)
        {
            Console.Write(new string(' ', previousRenderLength - output.Length));
        }

        previousRenderLength = output.Length;
    }

    private static void AppendMap(StringBuilder builder, SimulationSnapshot snapshot)
    {
        int height = snapshot.Terrain.GetLength(0);
        int width = snapshot.Terrain.GetLength(1);
        HashSet<GridPosition> remainingGoals = snapshot.RemainingGoals.ToHashSet();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GridPosition current = new(x, y);
                char cell = snapshot.Terrain[y, x];

                if (current == snapshot.RoverPosition)
                {
                    builder.Append('&');
                    continue;
                }

                if ((cell == 'G' || cell == 'Y' || cell == 'B') && !remainingGoals.Contains(current))
                {
                    builder.Append('.');
                    continue;
                }

                builder.Append(cell);
            }

            if (width > 0)
            {
                builder.AppendLine();
            }
        }
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

    private static string FormatLogTime(int elapsedMilliseconds)
    {
        TimeSpan time = TimeSpan.FromMilliseconds(elapsedMilliseconds);
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }
}