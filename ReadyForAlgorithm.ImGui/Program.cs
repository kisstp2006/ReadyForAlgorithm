using System.Numerics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ReadyForAlgorithm.Core;

namespace ReadyForAlgorithm.ImGuiFrontend;

internal static class Program
{
    private static void Main(string[] args)
    {
        NativeWindowSettings nativeWindowSettings = new()
        {
            ClientSize = new OpenTK.Mathematics.Vector2i(1440, 900),
            Title = "Mars Rover ImGui"
        };

        string? mapPath = args.ElementAtOrDefault(0);

        int duration = 24;
        if (args.Length > 1 && int.TryParse(args[1], out int parsedDuration))
        {
            duration = Math.Max(24, parsedDuration);
        }

        using RoverImGuiWindow window = new(mapPath, duration/*, args.FirstOrDefault()*/, GameWindowSettings.Default, nativeWindowSettings);
        window.Run();
    }
}

internal sealed class RoverImGuiWindow : GameWindow
{
    private void GameOver()
    {
        isGameOver = true;
        simulation.TogglePause();
    }
    private string? loadedMapPath;
    private RoverSimulation simulation;
    private ImGuiController? controller;

    private int missionDurationHours = 24;
    private bool isSimulationStarted = false;
    private float remainingMissionHours;
    private double timeAccumulator = 0;
    private bool isGameOver = false;

    public RoverImGuiWindow(string? mapPath, int duration, GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        this.loadedMapPath = mapPath;
        this.isSimulationStarted = true;
        this.missionDurationHours = duration;
        this.remainingMissionHours = duration;
        if(duration >= 24)
        {
            this.isSimulationStarted = true;
        }
        else
        {
            this.isSimulationStarted = false;
            this.missionDurationHours = 24;
        }
        simulation = RoverSimulation.CreateFromFile(mapPath);
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        // Modern sötét háttér - mélyebb, elegánsabb árnyalat
        GL.ClearColor(0.06f, 0.07f, 0.09f, 1f);
        controller = new ImGuiController(ClientSize.X, ClientSize.Y);

        TextInput += e => controller.PressChar((char)e.Unicode);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        controller?.Resize(e.Width, e.Height);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        if (controller is null)
        {
            return;
        }

        int elapsedMilliseconds = Math.Max(1, (int)(args.Time * 1000d));
        simulation.Update(elapsedMilliseconds);

        controller.Update(this, (float)args.Time);
        DrawDockSpace();

        if(isSimulationStarted == false)
        {
            DrawLauncherWindow();
        }
        else
        {
            if(isGameOver == false)
            {
                timeAccumulator += args.Time;
                if (timeAccumulator >= 4.0)
                {
                    remainingMissionHours -= 1;
                    timeAccumulator -= 4.0;

                    if (remainingMissionHours < 0)
                    {
                        remainingMissionHours = 0;
                        GameOver();
                    }
                }
            }
            

            elapsedMilliseconds = Math.Max(1, (int)(args.Time * 1000d));
            simulation.Update(elapsedMilliseconds);

            SimulationSnapshot snapshot = simulation.CreateSnapshot();
            DrawMissionControl(snapshot);
            DrawMapWindow(snapshot);
            DrawStatsWindow(snapshot, remainingMissionHours);
            DrawLogWindow(snapshot);

            if(isGameOver == true)
            {
                DrawGameOverWindow();
            }
        }


        GL.Clear(ClearBufferMask.ColorBufferBit);
        controller.Render();
        SwapBuffers();
    }

    private void DrawGameOverWindow()
    {
        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(viewport.Size.X * 0.5f, viewport.Size.Y * 0.5f), ImGuiCond.Always, new System.Numerics.Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(500, 280));

        ImGuiWindowFlags flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize;

        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.12f, 0.08f, 0.08f, 0.98f));
        ImGui.Begin($"{FontAwesome6.Skull} MISSION TERMINATED", flags);
        ImGui.PopStyleColor();

        ImGui.Spacing();
        ImGui.Spacing();
        
        // Nagy piros "GAME OVER" szöveg
        ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[0]);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.2f, 0.2f, 1.0f));
        float textWidth = ImGui.CalcTextSize("GAME OVER").X * 2.5f;
        ImGui.SetCursorPosX((ImGui.GetWindowSize().X - textWidth) * 0.5f);
        ImGui.SetWindowFontScale(2.5f);
        ImGui.Text("GAME OVER");
        ImGui.SetWindowFontScale(1.0f);
        ImGui.PopStyleColor();
        ImGui.PopFont();

        ImGui.Spacing();
        ImGui.Spacing();
        
        ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(0.6f, 0.2f, 0.2f, 1.0f));
        ImGui.Separator();
        ImGui.PopStyleColor();
        
        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.85f, 0.85f, 0.85f, 1.0f));
        ImGui.TextWrapped("The mission has failed because the allocated time has expired. The rover is now outside the communication window and cannot be recovered.");
        ImGui.PopStyleColor();

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.7f, 0.3f, 0.2f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.8f, 0.4f, 0.3f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.1f, 1.0f));
        
        ImGui.SetCursorPosX((ImGui.GetWindowSize().X - 280) * 0.5f);
        if (ImGui.Button($"{FontAwesome6.RotateRight} RESTART MISSION", new System.Numerics.Vector2(280, 45)))
        {
            isGameOver = false;
            isSimulationStarted = false;
            timeAccumulator = 0;

            simulation = RoverSimulation.CreateFromFile(loadedMapPath);
        }
        
        ImGui.PopStyleColor(3);

        ImGui.Spacing();

        ImGui.End();
    }

    protected override void OnUnload()
    {
        controller?.Dispose();
        base.OnUnload();
    }

    private static void DrawDockSpace()
    {
        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos);
        ImGui.SetNextWindowSize(viewport.Size);
        ImGui.SetNextWindowViewport(viewport.ID);

        ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoDocking |
                                       ImGuiWindowFlags.NoTitleBar |
                                       ImGuiWindowFlags.NoCollapse |
                                       ImGuiWindowFlags.NoResize |
                                       ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoBringToFrontOnFocus |
                                       ImGuiWindowFlags.NoNavFocus |
                                       ImGuiWindowFlags.NoBackground;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);

        ImGui.Begin("MainDockHost", windowFlags);
        ImGui.PopStyleVar(2);

        uint dockspaceId = ImGui.GetID("MainDockSpace");
        ImGui.DockSpace(dockspaceId, System.Numerics.Vector2.Zero, ImGuiDockNodeFlags.PassthruCentralNode);
        ImGui.End();
    }

    private void DrawMissionControl(SimulationSnapshot snapshot)
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 200), ImGuiCond.FirstUseEver);
        ImGui.Begin($"{FontAwesome6.Satellite} Mission Control");

        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.85f, 1.0f, 1.0f));
        ImGui.TextUnformatted($"{FontAwesome6.Info} Status");
        ImGui.PopStyleColor();
        ImGui.SameLine();
        ImGui.TextWrapped(snapshot.StatusMessage);
        
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        
        ImGui.TextUnformatted("Speed Control");
        ImGui.Spacing();

        float buttonWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 2) / 3.0f;
        
        if (ImGui.Button($"{FontAwesome6.SpeedSlow} Slow", new Vector2(buttonWidth, 32)))
        {
            simulation.SetSpeed(RoverSpeedMode.Slow);
        }

        ImGui.SameLine();
        if (ImGui.Button($"{FontAwesome6.SpeedNormal} Normal", new Vector2(buttonWidth, 32)))
        {
            simulation.SetSpeed(RoverSpeedMode.Normal);
        }

        ImGui.SameLine();
        if (ImGui.Button($"{FontAwesome6.SpeedFast} Fast", new Vector2(buttonWidth, 32)))
        {
            simulation.SetSpeed(RoverSpeedMode.Fast);
        }

        ImGui.Spacing();
        
        ImGui.PushStyleColor(ImGuiCol.Button, snapshot.IsPaused ? new Vector4(0.2f, 0.6f, 0.3f, 1.0f) : new Vector4(0.8f, 0.5f, 0.2f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, snapshot.IsPaused ? new Vector4(0.3f, 0.7f, 0.4f, 1.0f) : new Vector4(0.9f, 0.6f, 0.3f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, snapshot.IsPaused ? new Vector4(0.15f, 0.5f, 0.25f, 1.0f) : new Vector4(0.7f, 0.4f, 0.1f, 1.0f));
        
        if (ImGui.Button(snapshot.IsPaused ? $"{FontAwesome6.Play} Resume" : $"{FontAwesome6.Pause} Pause", new Vector2(-1, 36)))
        {
            simulation.TogglePause();
        }
        
        ImGui.PopStyleColor(3);

        ImGui.End();
    }

    private static void DrawStatsWindow(SimulationSnapshot snapshot, float remainingHours)
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(380, 300), ImGuiCond.FirstUseEver);
        ImGui.Begin($"{FontAwesome6.ChartLine} Telemetry");
        
        // Mission time header - Prominens kijelzés
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.15f, 0.18f, 0.25f, 1.0f));
        ImGui.BeginChild("MissionTime", new Vector2(0, 50), ImGuiChildFlags.Borders);

        ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[0]);
        float timeTextWidth = ImGui.CalcTextSize($"{FontAwesome6.Clock} MISSION TIME").X;
        ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - timeTextWidth) * 0.5f);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.85f, 1.0f, 1.0f));
        ImGui.TextUnformatted($"{FontAwesome6.Clock} MISSION TIME");
        ImGui.PopStyleColor();
        
        string timeStr = $"{(int)remainingHours} hours";
        float timeValueWidth = ImGui.CalcTextSize(timeStr).X;
        ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - timeValueWidth) * 0.5f);
        ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.0f, 1.0f), timeStr);
        ImGui.PopFont();
        ImGui.EndChild();
        ImGui.PopStyleColor();
        
        ImGui.Spacing();
        
        // Telemetry data in a table-like format
        string batteryIcon = snapshot.Battery switch
        {
            >= 75 => FontAwesome6.BatteryFull,
            >= 50 => FontAwesome6.BatteryThreeQuarters,
            >= 25 => FontAwesome6.BatteryHalf,
            >= 10 => FontAwesome6.BatteryQuarter,
            _ => FontAwesome6.BatteryEmpty
        };
        
        Vector4 batteryColor = snapshot.Battery switch
        {
            >= 75 => new Vector4(0.2f, 0.8f, 0.3f, 1.0f),
            >= 50 => new Vector4(0.5f, 0.8f, 0.3f, 1.0f),
            >= 25 => new Vector4(0.9f, 0.7f, 0.2f, 1.0f),
            >= 10 => new Vector4(0.9f, 0.5f, 0.1f, 1.0f),
            _ => new Vector4(0.9f, 0.2f, 0.2f, 1.0f)
        };
        
        DrawTelemetryRow(batteryIcon, "Battery", $"{snapshot.Battery}%", batteryColor);
        DrawTelemetryRow(FontAwesome6.Clock, "Time", snapshot.TimeLabel);
        DrawTelemetryRow(FontAwesome6.LocationDot, "Position", $"{snapshot.RoverPosition.X}, {snapshot.RoverPosition.Y}");
        DrawTelemetryRow(FontAwesome6.Gauge, "Speed", snapshot.SpeedMode.ToString());
        DrawTelemetryRow(snapshot.IsPaused ? FontAwesome6.Pause : FontAwesome6.Play, "Status", snapshot.IsPaused ? "Paused" : "Running", snapshot.IsPaused ? new Vector4(0.9f, 0.6f, 0.2f, 1.0f) : new Vector4(0.3f, 0.8f, 0.4f, 1.0f));
        
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        
        DrawTelemetryRow(FontAwesome6.ListCheck, "Samples", $"{snapshot.CollectedGoalCount} / {snapshot.TotalGoalCount}");
        DrawTelemetryRow(FontAwesome6.Bullseye, "Remaining", snapshot.RemainingGoals.Count.ToString());
        
        ImGui.End();
    }
    
    private static void DrawTelemetryRow(string icon, string label, string value, Vector4? valueColor = null)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.7f, 0.8f, 1.0f));
        ImGui.TextUnformatted($"{icon} {label}:");
        ImGui.PopStyleColor();
        
        ImGui.SameLine(140);
        
        if (valueColor.HasValue)
        {
            ImGui.TextColored(valueColor.Value, value);
        }
        else
        {
            ImGui.TextUnformatted(value);
        }
    }

    private static void DrawLogWindow(SimulationSnapshot snapshot)
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(700, 380), ImGuiCond.FirstUseEver);
        ImGui.Begin($"{FontAwesome6.Terminal} Console Log");
        
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.05f, 0.06f, 0.08f, 1.0f));
        ImGui.BeginChild("LogScroll", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.Borders);
        
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 2));
        
        foreach (RoverLogEntry log in snapshot.Logs.TakeLast(40))
        {
            string timeStr = $"[{FormatLogTime(log.Tick)}]";
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.6f, 0.7f, 1.0f));
            ImGui.TextUnformatted(timeStr);
            ImGui.PopStyleColor();
            
            ImGui.SameLine();
            
            // Színezés a log típusa alapján
            Vector4 logColor = new Vector4(0.85f, 0.87f, 0.90f, 1.0f);
            if (log.Message.Contains("Battery") || log.Message.Contains("battery"))
            {
                logColor = new Vector4(0.9f, 0.7f, 0.2f, 1.0f);
            }
            else if (log.Message.Contains("detected") || log.Message.Contains("Mining"))
            {
                logColor = new Vector4(0.3f, 0.9f, 0.5f, 1.0f);
            }
            else if (log.Message.Contains("pause") || log.Message.Contains("dead"))
            {
                logColor = new Vector4(0.9f, 0.4f, 0.3f, 1.0f);
            }
            else if (log.Message.Contains("Speed") || log.Message.Contains("Status"))
            {
                logColor = new Vector4(0.5f, 0.7f, 0.9f, 1.0f);
            }
            
            ImGui.TextColored(logColor, log.Message);
        }
        
        ImGui.PopStyleVar();
        
        // Auto-scroll to bottom
        if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
        {
            ImGui.SetScrollHereY(1.0f);
        }
        
        ImGui.EndChild();
        ImGui.PopStyleColor();
        
        ImGui.End();
    }

    private void DrawLauncherWindow()
    {
        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(viewport.Size.X * 0.5f, viewport.Size.Y * 0.5f), ImGuiCond.Always, new System.Numerics.Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(450, 250), ImGuiCond.Always);
        
        ImGui.Begin($"{FontAwesome6.Rocket} Mission Setup", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove);

        ImGui.Spacing();
        ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[0]);
        
        float titleWidth = ImGui.CalcTextSize("Configure Mission Parameters").X;
        ImGui.SetCursorPosX((ImGui.GetWindowSize().X - titleWidth) * 0.5f);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.85f, 1.0f, 1.0f));
        ImGui.TextUnformatted("Configure Mission Parameters");
        ImGui.PopStyleColor();
        
        ImGui.PopFont();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.PushItemWidth(200);
        ImGui.SetCursorPosX((ImGui.GetWindowSize().X - 200) * 0.5f);
        ImGui.InputInt("##MissionHours", ref missionDurationHours);
        ImGui.PopItemWidth();
        
        ImGui.SetCursorPosX((ImGui.GetWindowSize().X - ImGui.CalcTextSize("Mission Duration (hours)").X) * 0.5f);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.75f, 0.8f, 1.0f));
        ImGui.TextUnformatted("Mission Duration (hours)");
        ImGui.PopStyleColor();

        ImGui.Spacing();
        ImGui.Spacing();

        if (missionDurationHours < 24)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.3f, 0.3f, 1.0f));
            float errorWidth = ImGui.CalcTextSize($"{FontAwesome6.TriangleExclamation} Minimum 24 hours required").X;
            ImGui.SetCursorPosX((ImGui.GetWindowSize().X - errorWidth) * 0.5f);
            ImGui.TextUnformatted($"{FontAwesome6.TriangleExclamation} Minimum 24 hours required");
            ImGui.PopStyleColor();
            ImGui.BeginDisabled();
        }

        ImGui.Spacing();
        ImGui.Spacing();
        
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.6f, 0.3f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.7f, 0.4f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.5f, 0.25f, 1.0f));
        
        ImGui.SetCursorPosX((ImGui.GetWindowSize().X - 250) * 0.5f);
        if (ImGui.Button($"{FontAwesome6.Rocket} BEGIN MISSION", new System.Numerics.Vector2(250, 45)))
        {
            isSimulationStarted = true;
            remainingMissionHours = missionDurationHours;
        }
        
        ImGui.PopStyleColor(3);

        if (missionDurationHours < 24)
        {
            ImGui.EndDisabled();
        }

        ImGui.End();
    }

    private static void DrawMapWindow(SimulationSnapshot snapshot)
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(520, 520), ImGuiCond.FirstUseEver);
        ImGui.Begin($"{FontAwesome6.Map} Map Grid");
        ImGui.BeginChild("MapCanvas", new System.Numerics.Vector2(0, 0));

        HashSet<GridPosition> remainingGoals = snapshot.RemainingGoals.ToHashSet();
        int height = snapshot.Terrain.GetLength(0);
        int width = snapshot.Terrain.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GridPosition position = new(x, y);
                char cell = snapshot.Terrain[y, x];
                char displayChar;
                Vector4 color;

                if (position == snapshot.RoverPosition)
                {
                    displayChar = '&';
                    color = new Vector4(1.0f, 0.0f, 0.0f, 1.0f); // Piros
                }
                else if ((cell == 'G' || cell == 'Y' || cell == 'B') && !remainingGoals.Contains(position))
                {
                    displayChar = '.';
                    color = new Vector4(0.5f, 0.5f, 0.5f, 1.0f); // Szürke
                }
                else
                {
                    displayChar = cell;
                    color = GetColorForCell(cell);
                }

                ImGui.SameLine(0, 0);
                ImGui.TextColored(color, displayChar.ToString());
            }
            ImGui.NewLine();
        }

        ImGui.EndChild();
        ImGui.End();
    }

    private static Vector4 GetColorForCell(char cell)
    {
        return cell switch
        {
            'S' => new Vector4(0.0f, 1.0f, 1.0f, 1.0f),      // Cián
            'G' => new Vector4(0.0f, 1.0f, 0.0f, 1.0f),      // Zöld
            'Y' => new Vector4(1.0f, 1.0f, 0.0f, 1.0f),      // Sárga
            'B' => new Vector4(0.0f, 0.0f, 1.0f, 1.0f),      // Kék
            '#' => new Vector4(0.3f, 0.3f, 0.3f, 1.0f),      // Sötétszürke
            '.' => new Vector4(0.7f, 0.7f, 0.7f, 1.0f),      // Világosszürke
            _ => new Vector4(1.0f, 1.0f, 1.0f, 1.0f)         // Fehér (default)
        };
    }

    private static string FormatLogTime(int elapsedMilliseconds)
    {
        TimeSpan time = TimeSpan.FromMilliseconds(elapsedMilliseconds);
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }
}

internal sealed class ImGuiController : IDisposable
{
    private int windowWidth;
    private int windowHeight;

    private int vertexArray;
    private int vertexBuffer;
    private int indexBuffer;
    private int vertexBufferSize;
    private int indexBufferSize;

    private int fontTexture;
    private int shader;
    private int attribLocationTex;
    private int attribLocationProjMtx;

    private bool frameBegun;

    public ImGuiController(int width, int height)
    {
        windowWidth = width;
        windowHeight = height;

        ImGui.CreateContext();
        ImGuiIOPtr io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        
        // Load fonts
        LoadFonts(io);
        
        // Apply modern style
        ApplyModernStyle();

        vertexBufferSize = 10_000;
        indexBufferSize = 2_000;

        CreateDeviceResources();
        SetPerFrameImGuiData(1f / 60f);
        ImGui.NewFrame();
        frameBegun = true;
    }

    private static void ApplyModernStyle()
    {
        ImGuiStylePtr style = ImGui.GetStyle();
        
        // Modern színséma - Dark theme sötét kék-szürke árnyalatokkal
        // Background colors - Sötét, modern háttér
        style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.09f, 0.10f, 0.12f, 0.95f);
        style.Colors[(int)ImGuiCol.ChildBg] = new Vector4(0.11f, 0.12f, 0.14f, 1.00f);
        style.Colors[(int)ImGuiCol.PopupBg] = new Vector4(0.09f, 0.10f, 0.12f, 0.98f);
        style.Colors[(int)ImGuiCol.Border] = new Vector4(0.20f, 0.22f, 0.27f, 0.80f);
        
        // Title bar - Sötétkék accent
        style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.08f, 0.09f, 0.11f, 1.00f);
        style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.12f, 0.14f, 0.18f, 1.00f);
        style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.08f, 0.09f, 0.11f, 0.75f);
        
        // Menu bar
        style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.10f, 0.11f, 0.13f, 1.00f);
        
        // Scrollbar - Diszkrét, modern
        style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.09f, 0.10f, 0.12f, 0.60f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.25f, 0.27f, 0.32f, 1.00f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.35f, 0.37f, 0.42f, 1.00f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.45f, 0.47f, 0.52f, 1.00f);
        
        // Frame (inputs, etc.) - Letisztult, finom keretekkel
        style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.14f, 0.15f, 0.17f, 1.00f);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.18f, 0.19f, 0.22f, 1.00f);
        style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.22f, 0.23f, 0.26f, 1.00f);
        
        // Buttons - Modern kék accent színnel
        style.Colors[(int)ImGuiCol.Button] = new Vector4(0.20f, 0.25f, 0.35f, 1.00f);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.26f, 0.32f, 0.45f, 1.00f);
        style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.18f, 0.22f, 0.32f, 1.00f);
        
        // Header (collapsing headers, etc.)
        style.Colors[(int)ImGuiCol.Header] = new Vector4(0.20f, 0.25f, 0.35f, 0.80f);
        style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.26f, 0.32f, 0.45f, 0.80f);
        style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.22f, 0.27f, 0.38f, 1.00f);
        
        // Separator - Finom elválasztó vonal
        style.Colors[(int)ImGuiCol.Separator] = new Vector4(0.20f, 0.22f, 0.27f, 1.00f);
        style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.30f, 0.35f, 0.45f, 1.00f);
        style.Colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.35f, 0.40f, 0.52f, 1.00f);
        
        // Tab colors - Modern, letisztult
        style.Colors[(int)ImGuiCol.Tab] = new Vector4(0.14f, 0.16f, 0.20f, 1.00f);
        style.Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.26f, 0.32f, 0.45f, 1.00f);
        
        // Docking
        style.Colors[(int)ImGuiCol.DockingPreview] = new Vector4(0.26f, 0.32f, 0.45f, 0.70f);
        
        // Text colors - Világos, jó kontraszttal
        style.Colors[(int)ImGuiCol.Text] = new Vector4(0.92f, 0.93f, 0.95f, 1.00f);
        style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.50f, 0.52f, 0.55f, 1.00f);
        
        // CheckBox
        style.Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.35f, 0.55f, 0.85f, 1.00f);
        
        // Slider
        style.Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.35f, 0.55f, 0.85f, 1.00f);
        style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.45f, 0.65f, 0.95f, 1.00f);
        
        // Resize grip
        style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.20f, 0.25f, 0.35f, 0.50f);
        style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.26f, 0.32f, 0.45f, 0.75f);
        style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.32f, 0.40f, 0.55f, 1.00f);
        
        // Plot colors
        style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(0.61f, 0.61f, 0.61f, 1.00f);
        style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(1.00f, 0.43f, 0.35f, 1.00f);
        style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
        style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
        
        // Style settings - Modern, lekerekített, tágas
        style.WindowPadding = new Vector2(12, 12);
        style.FramePadding = new Vector2(8, 4);
        style.ItemSpacing = new Vector2(10, 6);
        style.ItemInnerSpacing = new Vector2(6, 6);
        style.IndentSpacing = 22;
        style.ScrollbarSize = 14;
        style.GrabMinSize = 12;
        
        // Rounded corners - Modern, lekerekített design
        style.WindowRounding = 8.0f;
        style.ChildRounding = 6.0f;
        style.FrameRounding = 5.0f;
        style.PopupRounding = 6.0f;
        style.ScrollbarRounding = 9.0f;
        style.GrabRounding = 4.0f;
        style.TabRounding = 5.0f;
        
        // Borders
        style.WindowBorderSize = 1.0f;
        style.ChildBorderSize = 1.0f;
        style.PopupBorderSize = 1.0f;
        style.FrameBorderSize = 0.0f;
        style.TabBorderSize = 0.0f;
        
        // Additional tweaks
        style.WindowTitleAlign = new Vector2(0.02f, 0.50f);
        style.ButtonTextAlign = new Vector2(0.50f, 0.50f);
        style.SelectableTextAlign = new Vector2(0.00f, 0.50f);
        style.DisplaySafeAreaPadding = new Vector2(4, 4);
        
        // Anti-aliasing
        style.AntiAliasedLines = true;
        style.AntiAliasedFill = true;
        
        Console.WriteLine("? Modern UI style applied!");
    }

    private unsafe void LoadFonts(ImGuiIOPtr io)
    {
        try
        {
            // Alapértelmezett font betöltése
            io.Fonts.AddFontDefault();
            
            string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "fa-solid-900.ttf");
            
            if (File.Exists(fontPath))
            {
                // Font Awesome merge-elése az alap fonttal
                ImFontConfigPtr fontConfig = ImGuiNative.ImFontConfig_ImFontConfig();
                fontConfig.MergeMode = true;
                fontConfig.PixelSnapH = true;
                fontConfig.GlyphMinAdvanceX = 13.0f;
                fontConfig.OversampleH = 1;
                fontConfig.OversampleV = 1;

                // Font Awesome 6 Free Solid ikonok TELJES unicode tartománya
                // Kib?vített range, hogy minden ikon megjelenjen
                ushort[] iconRanges = new ushort[] 
                { 
                    0xf000, 0xf8ff,  // Font Awesome összes standard és extended ikonja
                    0 
                };
                
                fixed (ushort* rangePtr = iconRanges)
                {
                    io.Fonts.AddFontFromFileTTF(fontPath, 13.0f, fontConfig, (IntPtr)rangePtr);
                }
                
                Console.WriteLine("???????????????????????????????????????????????????????????");
                Console.WriteLine("? Font Awesome loaded successfully!");
                Console.WriteLine("???????????????????????????????????????????????????????????");
                Console.WriteLine($"  Path: {fontPath}");
                Console.WriteLine($"  Icon Range: U+F000 to U+F8FF ({0xf8ff - 0xf000 + 1} glyphs)");
                Console.WriteLine("???????????????????????????????????????????????????????????");
            }
            else
            {
                Console.WriteLine("???????????????????????????????????????????????????????????");
                Console.WriteLine("?  Font Awesome NOT FOUND - Icons will show as '?'");
                Console.WriteLine("???????????????????????????????????????????????????????????");
                Console.WriteLine($"  Expected path: {fontPath}");
                Console.WriteLine();
                Console.WriteLine("  SETUP INSTRUCTIONS:");
                Console.WriteLine("  1. Download: https://fontawesome.com/download");
                Console.WriteLine("  2. Extract the ZIP file");
                Console.WriteLine("  3. Find 'fa-solid-900.ttf' in webfonts or otfs folder");
                Console.WriteLine("  4. Create folder: ReadyForAlgorithm.ImGui\\Fonts\\");
                Console.WriteLine("  5. Copy fa-solid-900.ttf to the Fonts folder");
                Console.WriteLine("  6. Rebuild and run the application");
                Console.WriteLine("???????????????????????????????????????????????????????????");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error loading fonts: {ex.Message}");
            Console.WriteLine($"  Stack trace: {ex.StackTrace}");
        }
    }

    public void PressChar(char keyChar)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.AddInputCharacter(keyChar);
    }

    public void Resize(int width, int height)
    {
        windowWidth = width;
        windowHeight = height;
    }

    public void Update(GameWindow window, float deltaSeconds)
    {
        if (frameBegun)
        {
            ImGui.Render();
        }

        SetPerFrameImGuiData(Math.Max(deltaSeconds, 1f / 240f));
        UpdateImGuiInput(window);

        frameBegun = true;
        ImGui.NewFrame();
    }

    public void Render()
    {
        if (!frameBegun)
        {
            return;
        }

        frameBegun = false;
        ImGui.Render();
        RenderImDrawData(ImGui.GetDrawData());
    }

    public void Dispose()
    {
        GL.DeleteBuffer(vertexBuffer);
        GL.DeleteBuffer(indexBuffer);
        GL.DeleteVertexArray(vertexArray);
        GL.DeleteTexture(fontTexture);
        GL.DeleteProgram(shader);
    }

    private unsafe void CreateDeviceResources()
    {
        vertexBuffer = GL.GenBuffer();
        indexBuffer = GL.GenBuffer();
        vertexArray = GL.GenVertexArray();

        GL.BindVertexArray(vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        const string vertexSource = @"#version 330 core
layout (location = 0) in vec2 in_position;
layout (location = 1) in vec2 in_texCoord;
layout (location = 2) in vec4 in_color;
uniform mat4 projection_matrix;
out vec2 frag_uv;
out vec4 frag_color;
void main()
{
    frag_uv = in_texCoord;
    frag_color = in_color;
    gl_Position = projection_matrix * vec4(in_position, 0, 1);
}";

        const string fragmentSource = @"#version 330 core
in vec2 frag_uv;
in vec4 frag_color;
uniform sampler2D in_fontTexture;
out vec4 output_color;
void main()
{
    output_color = frag_color * texture(in_fontTexture, frag_uv.st);
}";

        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexSource);
        GL.CompileShader(vertexShader);

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentSource);
        GL.CompileShader(fragmentShader);

        shader = GL.CreateProgram();
        GL.AttachShader(shader, vertexShader);
        GL.AttachShader(shader, fragmentShader);
        GL.LinkProgram(shader);

        GL.DetachShader(shader, vertexShader);
        GL.DetachShader(shader, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        attribLocationTex = GL.GetUniformLocation(shader, "in_fontTexture");
        attribLocationProjMtx = GL.GetUniformLocation(shader, "projection_matrix");

        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        int stride = sizeof(ImDrawVert);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

        CreateFontTexture();
    }

    private unsafe void CreateFontTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out _);

        fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, fontTexture);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
        GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)pixels);

        io.Fonts.SetTexID((IntPtr)fontTexture);
        io.Fonts.ClearTexData();
    }

    private static void UpdateImGuiInput(GameWindow window)
    {
        ImGuiIOPtr io = ImGui.GetIO();

        KeyboardState keyboard = window.KeyboardState;
        MouseState mouse = window.MouseState;

        io.AddMousePosEvent(mouse.X, mouse.Y);
        io.AddMouseButtonEvent(0, mouse.IsButtonDown(MouseButton.Left));
        io.AddMouseButtonEvent(1, mouse.IsButtonDown(MouseButton.Right));
        io.AddMouseButtonEvent(2, mouse.IsButtonDown(MouseButton.Middle));
        io.AddMouseWheelEvent(mouse.ScrollDelta.X, mouse.ScrollDelta.Y);

        io.AddKeyEvent(ImGuiKey.Tab, keyboard.IsKeyDown(Keys.Tab));
        io.AddKeyEvent(ImGuiKey.LeftArrow, keyboard.IsKeyDown(Keys.Left));
        io.AddKeyEvent(ImGuiKey.RightArrow, keyboard.IsKeyDown(Keys.Right));
        io.AddKeyEvent(ImGuiKey.UpArrow, keyboard.IsKeyDown(Keys.Up));
        io.AddKeyEvent(ImGuiKey.DownArrow, keyboard.IsKeyDown(Keys.Down));
        io.AddKeyEvent(ImGuiKey.PageUp, keyboard.IsKeyDown(Keys.PageUp));
        io.AddKeyEvent(ImGuiKey.PageDown, keyboard.IsKeyDown(Keys.PageDown));
        io.AddKeyEvent(ImGuiKey.Home, keyboard.IsKeyDown(Keys.Home));
        io.AddKeyEvent(ImGuiKey.End, keyboard.IsKeyDown(Keys.End));
        io.AddKeyEvent(ImGuiKey.Insert, keyboard.IsKeyDown(Keys.Insert));
        io.AddKeyEvent(ImGuiKey.Delete, keyboard.IsKeyDown(Keys.Delete));
        io.AddKeyEvent(ImGuiKey.Backspace, keyboard.IsKeyDown(Keys.Backspace));
        io.AddKeyEvent(ImGuiKey.Space, keyboard.IsKeyDown(Keys.Space));
        io.AddKeyEvent(ImGuiKey.Enter, keyboard.IsKeyDown(Keys.Enter));
        io.AddKeyEvent(ImGuiKey.Escape, keyboard.IsKeyDown(Keys.Escape));

        io.AddKeyEvent(ImGuiKey.A, keyboard.IsKeyDown(Keys.A));
        io.AddKeyEvent(ImGuiKey.C, keyboard.IsKeyDown(Keys.C));
        io.AddKeyEvent(ImGuiKey.V, keyboard.IsKeyDown(Keys.V));
        io.AddKeyEvent(ImGuiKey.X, keyboard.IsKeyDown(Keys.X));
        io.AddKeyEvent(ImGuiKey.Y, keyboard.IsKeyDown(Keys.Y));
        io.AddKeyEvent(ImGuiKey.Z, keyboard.IsKeyDown(Keys.Z));

        io.AddKeyEvent(ImGuiKey.LeftCtrl, keyboard.IsKeyDown(Keys.LeftControl));
        io.AddKeyEvent(ImGuiKey.RightCtrl, keyboard.IsKeyDown(Keys.RightControl));
        io.AddKeyEvent(ImGuiKey.LeftShift, keyboard.IsKeyDown(Keys.LeftShift));
        io.AddKeyEvent(ImGuiKey.RightShift, keyboard.IsKeyDown(Keys.RightShift));
        io.AddKeyEvent(ImGuiKey.LeftAlt, keyboard.IsKeyDown(Keys.LeftAlt));
        io.AddKeyEvent(ImGuiKey.RightAlt, keyboard.IsKeyDown(Keys.RightAlt));
        io.AddKeyEvent(ImGuiKey.LeftSuper, keyboard.IsKeyDown(Keys.LeftSuper));
        io.AddKeyEvent(ImGuiKey.RightSuper, keyboard.IsKeyDown(Keys.RightSuper));
    }

    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(windowWidth, windowHeight);
        io.DisplayFramebufferScale = System.Numerics.Vector2.One;
        io.DeltaTime = deltaSeconds;
    }

    private unsafe void RenderImDrawData(ImDrawDataPtr drawData)
    {
        int fbWidth = (int)(drawData.DisplaySize.X * drawData.FramebufferScale.X);
        int fbHeight = (int)(drawData.DisplaySize.Y * drawData.FramebufferScale.Y);

        if (fbWidth <= 0 || fbHeight <= 0)
        {
            return;
        }

        drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.ScissorTest);
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

        GL.Viewport(0, 0, fbWidth, fbHeight);
        OpenTK.Mathematics.Matrix4 projection = OpenTK.Mathematics.Matrix4.CreateOrthographicOffCenter(0.0f, ImGui.GetIO().DisplaySize.X, ImGui.GetIO().DisplaySize.Y, 0.0f, -1.0f, 1.0f);

        GL.UseProgram(shader);
        GL.Uniform1(attribLocationTex, 0);
        GL.UniformMatrix4(attribLocationProjMtx, false, ref projection);

        GL.BindVertexArray(vertexArray);

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            int vertexSize = cmdList.VtxBuffer.Size * sizeof(ImDrawVert);
            if (vertexSize > vertexBufferSize)
            {
                while (vertexSize > vertexBufferSize)
                {
                    vertexBufferSize *= 2;
                }

                GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
                GL.BufferData(BufferTarget.ArrayBuffer, vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            }

            int indexSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            if (indexSize > indexBufferSize)
            {
                while (indexSize > indexBufferSize)
                {
                    indexBufferSize *= 2;
                }

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
                GL.BufferData(BufferTarget.ElementArrayBuffer, indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertexSize, (IntPtr)cmdList.VtxBuffer.Data);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, indexSize, (IntPtr)cmdList.IdxBuffer.Data);

            int idxOffset = 0;
            for (int cmdIndex = 0; cmdIndex < cmdList.CmdBuffer.Size; cmdIndex++)
            {
                ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmdIndex];

                GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);
                GL.Scissor(
                    (int)pcmd.ClipRect.X,
                    (int)(fbHeight - pcmd.ClipRect.W),
                    (int)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                    (int)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                if ((ImGui.GetIO().BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                {
                    GL.DrawElementsBaseVertex(
                        PrimitiveType.Triangles,
                        (int)pcmd.ElemCount,
                        DrawElementsType.UnsignedShort,
                        (IntPtr)(idxOffset * sizeof(ushort)),
                        (int)pcmd.VtxOffset);
                }
                else
                {
                    GL.DrawElements(
                        PrimitiveType.Triangles,
                        (int)pcmd.ElemCount,
                        DrawElementsType.UnsignedShort,
                        (IntPtr)(idxOffset * sizeof(ushort)));
                }

                idxOffset += (int)pcmd.ElemCount;
            }
        }

        GL.Disable(EnableCap.ScissorTest);
    }
}
