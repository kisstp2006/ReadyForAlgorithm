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
        GL.ClearColor(0.08f, 0.1f, 0.13f, 1f);
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
        // Az ablakot a képernyõ közepére pozicionáljuk
        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(viewport.Size.X * 0.5f, viewport.Size.Y * 0.5f), ImGuiCond.Always, new System.Numerics.Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 200));

        ImGuiWindowFlags flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize;

        ImGui.Begin("MISSION TERMINATED", flags);

        // Nagy piros "GAME OVER" szöveg
        ImGui.SetWindowFontScale(2.5f); // Megnöveljük a betûméretet
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1)); // Piros szín
        float textWidth = ImGui.CalcTextSize("GAME OVER").X;
        ImGui.SetCursorPosX((ImGui.GetWindowSize().X - textWidth) * 0.5f);
        ImGui.Text("GAME OVER");
        ImGui.PopStyleColor();
        ImGui.SetWindowFontScale(1.0f);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextWrapped("The mission has failed, because the allocated time has expired. The rover is now out of communication window.");

        ImGui.Spacing();
        if (ImGui.Button("RESTART MISSION", new System.Numerics.Vector2(-1, 40)))
        {
            isGameOver = false;
            isSimulationStarted = false;
            timeAccumulator = 0;

            simulation = RoverSimulation.CreateFromFile(loadedMapPath);
        }

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
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(360, 170), ImGuiCond.FirstUseEver);
        ImGui.Begin("Mission Control");

        ImGui.TextUnformatted($"Status: {snapshot.StatusMessage}");

        if (ImGui.Button("Slow"))
        {
            simulation.SetSpeed(RoverSpeedMode.Slow);
        }

        ImGui.SameLine();
        if (ImGui.Button("Normal"))
        {
            simulation.SetSpeed(RoverSpeedMode.Normal);
        }

        ImGui.SameLine();
        if (ImGui.Button("Fast"))
        {
            simulation.SetSpeed(RoverSpeedMode.Fast);
        }

        if (ImGui.Button(snapshot.IsPaused ? "Resume" : "Pause"))
        {
            simulation.TogglePause();
        }

        ImGui.End();
    }

    private static void DrawStatsWindow(SimulationSnapshot snapshot, float remainingHours)
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(320, 240), ImGuiCond.FirstUseEver);
        ImGui.Begin("Telemetry");
        ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.0f, 1.0f), $"MISSION TIME REMAINING: {(int)remainingHours} h");
        ImGui.Separator();
        ImGui.TextUnformatted($"Battery: {snapshot.Battery}%");
        ImGui.TextUnformatted($"Time: {snapshot.TimeLabel}");
        ImGui.TextUnformatted($"Position: {snapshot.RoverPosition.X}, {snapshot.RoverPosition.Y}");
        ImGui.TextUnformatted($"Speed: {snapshot.SpeedMode}");
        ImGui.TextUnformatted($"Paused: {(snapshot.IsPaused ? "Yes" : "No")}");
        ImGui.TextUnformatted($"Samples: {snapshot.CollectedGoalCount}/{snapshot.TotalGoalCount}");
        ImGui.TextUnformatted($"Remaining goals: {snapshot.RemainingGoals.Count}");
        ImGui.End();
    }

    private static void DrawLogWindow(SimulationSnapshot snapshot)
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(660, 340), ImGuiCond.FirstUseEver);
        ImGui.Begin("Console Log");
        ImGui.BeginChild("LogScroll", new System.Numerics.Vector2(0, 0));
        foreach (RoverLogEntry log in snapshot.Logs.TakeLast(40))
        {
            ImGui.TextUnformatted($"[{FormatLogTime(log.Tick)}] {log.Message}");
        }
        ImGui.EndChild();
        ImGui.End();
    }

    private void DrawLauncherWindow()
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 200), ImGuiCond.Always);
        ImGui.Begin("Mission Setup", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

        ImGui.Text("Please input the time for the mission!");
        ImGui.Separator();

        ImGui.InputInt("Time (hours)", ref missionDurationHours);

        if (missionDurationHours < 24)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "Error! 24 hours is the minimum");
            ImGui.BeginDisabled();
        }

        ImGui.Spacing();
        if (ImGui.Button("MISSION BEGIN", new System.Numerics.Vector2(-1, 40)))
        {
            isSimulationStarted = true;
            remainingMissionHours = missionDurationHours;
        }

        if (missionDurationHours < 24)
        {
            ImGui.EndDisabled();
        }

        ImGui.End();
    }

    private static void DrawMapWindow(SimulationSnapshot snapshot)
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(520, 520), ImGuiCond.FirstUseEver);
        ImGui.Begin("Map Grid");
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
        io.Fonts.AddFontDefault();

        vertexBufferSize = 10_000;
        indexBufferSize = 2_000;

        CreateDeviceResources();
        SetPerFrameImGuiData(1f / 60f);
        ImGui.NewFrame();
        frameBegun = true;
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
