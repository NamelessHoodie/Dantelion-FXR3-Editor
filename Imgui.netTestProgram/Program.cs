using System;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using ImGuiNET;

namespace DSFFXEditor
{
    class Program
    {
        private static Sdl2Window _window;
        private static GraphicsDevice _gd;
        private static CommandList _cl;
        private static ImGuiRenderer _controller;
        private static MemoryEditor _memoryEditor;

        // UI state
        private static float _f = 0.0f;
        private static int _counter = 0;
        private static int _dragInt = 0;
        private static Vector3 _clearColor = new Vector3(0.45f, 0.55f, 0.6f);
        private static bool _showDemoWindow = true;
        private static bool _showAnotherWindow = false;
        private static bool _showMemoryEditor = false;
        private static byte[] _memoryEditorData;
        private static uint s_tab_bar_flags = (uint)ImGuiTabBarFlags.Reorderable;
        private static string _activeTheme = "DarkRedClay"; //Initialized Default Theme

        //Listboxmeme
        private static int _selectedItem = 0;
        private static String[] _items = { "meme", "meme1", "meme2" };

        //colorpicka
        private static Vector3 _CPickerColor = new Vector3(0, 0, 0);

        //checkbox
        private static bool _CPickerCheckbox = false;

        static bool[] s_opened = { true, true, true, true }; // Persistent user state

        //Theme Selector
        private static int _themeSelectorSelectedItem = 0;
        private static String[] _themeSelectorEntriesArray = { "Red Clay", "ImGui Dark", "ImGui Light", "ImGui Classic" };

        static void SetThing(out float i, float val) { i = val; }

        static void Main(string[] args)
        {
            // Create window, GraphicsDevice, and all resources necessary for the demo.
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "Dark Souls FFX Studio"),
                new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
                out _window,
                out _gd);
            _window.Resized += () =>
            {
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _controller.WindowResized(_window.Width, _window.Height);
            };
            _cl = _gd.ResourceFactory.CreateCommandList();
            _controller = new ImGuiRenderer(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);
            _memoryEditor = new MemoryEditor();
            Random random = new Random();
            _memoryEditorData = Enumerable.Range(0, 1024).Select(i => (byte)random.Next(255)).ToArray();

            ImGuiIOPtr io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            ThemesSelector(_activeTheme); //Default Theme

            // Main application loop
            while (_window.Exists)
            {
                InputSnapshot snapshot = _window.PumpEvents();
                if (!_window.Exists) { break; }
                _controller.Update(1f / 60f, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

                SubmitUI();

                _cl.Begin();
                _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
                _cl.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 1f));
                _controller.Render(_gd, _cl);
                _cl.End();
                _gd.SubmitCommands(_cl);
                _gd.SwapBuffers(_gd.MainSwapchain);
            }

            // Clean up Veldrid resources
            _gd.WaitForIdle();
            _controller.Dispose();
            _cl.Dispose();
            _gd.Dispose();
        }

        private static unsafe void SubmitUI()
        {
            // Demo code adapted from the official Dear ImGui demo program:
            // https://github.com/ocornut/imgui/blob/master/examples/example_win32_directx11/main.cpp#L172

            // 1. Show a simple window.
            // Tip: if we don't call ImGui.BeginWindow()/ImGui.EndWindow() the widgets automatically appears in a window called "Debug".
            ImGuiViewport* viewport = ImGui.GetMainViewport();
            var MainViewport = ImGui.GetID("MainViewPort");
            {
                // Docking setup
                ImGui.SetNextWindowPos(new Vector2(viewport->Pos.X, viewport->Pos.Y + 18.0f));
                ImGui.SetNextWindowSize(new Vector2(viewport->Size.X, viewport->Size.Y - 18.0f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0.0f, 0.0f));
                ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 0.0f);
                ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
                flags |= ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;
                flags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.DockNodeHost;
                ImGui.Begin("Main Viewport", flags);
                ImGui.PopStyleVar(4);
                if (ImGui.BeginMainMenuBar())
                {
                    if (ImGui.BeginMenu("File"))
                    {
                        if (ImGui.MenuItem("New"))
                        {
                            //Do something
                        }
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("File2"))
                    {
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("File3"))
                    {
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("Themes"))
                    {
                        ImGui.Combo("Theme Selector", ref _themeSelectorSelectedItem, _themeSelectorEntriesArray, _themeSelectorEntriesArray.Length);
                        switch (_themeSelectorSelectedItem)
                        {
                            case 0:
                                _activeTheme = "DarkRedClay";
                                break;
                            case 1:
                                _activeTheme = "ImGuiDark";
                                break;
                            case 2:
                                _activeTheme = "ImGuiLight";
                                break;
                            case 3:
                                _activeTheme = "ImGuiClassic";
                                break;
                            default:
                                break;
                        }
                        ThemesSelector(_activeTheme);
                        ImGui.EndMenu();
                    }

                    ImGui.EndMainMenuBar();
                }
                ImGui.DockSpace(MainViewport, new Vector2(0, 0));
                ImGui.End();
            }

            {
                ImGui.SetNextWindowDockID(MainViewport, ImGuiCond.Appearing);
                ImGui.Begin("Window1", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
                ImGui.Button("haha");
                ImGui.Text("Hello, world!");                                        // Display some text (you can use a format string too)
                ImGui.SliderFloat("float", ref _f, 0, 1, _f.ToString("0.000"));  // Edit 1 float using a slider from 0.0f to 1.0f    
                ImGui.Text($"Mouse position: {ImGui.GetMousePos()}");
                ImGui.Checkbox("Another Window", ref _showAnotherWindow);
                //ImGui.SameLine(0, -1);
                ImGui.Text($"counter = {_counter}");

                ImGui.DragInt("Draggable Int", ref _dragInt);

                float framerate = ImGui.GetIO().Framerate;
                ImGui.Text($"Application average {1000.0f / framerate:0.##} ms/frame ({framerate:0.#} FPS)");
                ImGui.Separator();
                ImGui.Checkbox("Button", ref _CPickerCheckbox);
                ImGui.ColorButton("Stored Color", new Vector4(_CPickerColor, 1.0f));
                ImGui.End();
            }

            // 2. Show another simple window. In most cases you will use an explicit Begin/End pair to name your windows.
            if (_showAnotherWindow)
            {
                ImGui.SetNextWindowDockID(MainViewport, ImGuiCond.Appearing);
                ImGui.Begin("Another Window", ref _showAnotherWindow);
                ImGui.Text("Hello from another window!");
                if (ImGui.Button("Close Me"))
                    _showAnotherWindow = false;
                if (_CPickerCheckbox)
                {
                    ImGui.ColorPicker3("dork", ref _CPickerColor, ImGuiColorEditFlags.DisplayRGB);
                    float[] meme = { 0, 0, 0 };
                    _CPickerColor.CopyTo(meme);
                    ImGui.TextColored(new Vector4(_CPickerColor, 1f), $"R:{Math.Round(meme[0], 2)} G:{Math.Round(meme[1], 2)} B:{Math.Round(meme[2], 2)}");
                }
                ImGui.End();
            }
        }
        public static void ThemesSelector(String themeName)
        {
            if (themeName == "DarkRedClay")
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.00f, 1.00f)); //Pretty Self explanatory
                //ImGui.PushStyleColor(ImGuiCol.TextDisabled, new Vector4(0.60f, 1.0f, 0.60f, 1.00f)); //idk yet
                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.17f, 0.16f, 0.16f, 1.0f)); //Window Background
                ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.35f, 0.24f, 0.24f, 1.00f)); //Popup Background
                ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.61f, 0.50f, 0.50f, 0.90f)); //Context Menu Border
                ImGui.PushStyleColor(ImGuiCol.BorderShadow, new Vector4(0.00f, 0.00f, 0.00f, 0.39f)); //idk
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.37f, 0.30f, 0.30f, 0.50f)); // Not clicked/hovered Control Background
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.42f, 0.30f, 0.30f, 0.60f)); // Hovered Control Background
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.46f, 0.30f, 0.30f, 0.70f)); // Clicked Control Background
                ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.5f, 0.39f, 0.39f, 0.50f)); // Unselected window title color
                ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, new Vector4(1.00f, 1.00f, 1.00f, 0.51f)); // Collapsed window title color
                ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.5f, 0.39f, 0.39f, 0.85f)); // Selected window title color
                ImGui.PushStyleColor(ImGuiCol.MenuBarBg, new Vector4(0.5f, 0.39f, 0.39f, 0.50f)); // MenuBar color
                ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new Vector4(0.5f, 0.39f, 0.39f, 0.50f)); // Scroll bar Background
                ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, new Vector4(0.5f, 0.39f, 0.39f, 1.00f)); // Scroll bar Grabby bit color
                ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, new Vector4(0.55f, 0.39f, 0.39f, 1.00f)); // Scroll bar grabby bit Hover
                ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, new Vector4(0.60f, 0.39f, 0.39f, 1.00f)); // Scroll bar color when clicked
                ImGui.PushStyleColor(ImGuiCol.CheckMark, new Vector4(0.5f, 0.39f, 0.39f, 1.00f)); // Checkbox Sign Color
                ImGui.PushStyleColor(ImGuiCol.SliderGrab, new Vector4(0.5f, 0.39f, 0.39f, 1.00f)); // Sliders grabby bit color
                ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, new Vector4(0.60f, 0.39f, 0.39f, 1.00f)); // Sliders grabby bit color when clicked
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.37f, 0.30f, 0.30f, 1.00f)); //Button Control Color Overrides
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.42f, 0.30f, 0.30f, 0.94f)); //Button Control Color Overrides
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.46f, 0.30f, 0.30f, 0.94f)); //Button Control Color Overrides
                ImGui.PushStyleColor(ImGuiCol.DockingPreview, new Vector4(0.00f, 0.00f, 0.00f, 0.67f)); //Docking screen preview color
                //ImGui.PushStyleColor(ImGuiCol.DockingEmptyBg, new Vector4(0.00f, 0.00f, 0.00f, 0.39f));
                ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.35f, 0.30f, 0.30f, 0.90f)); //Unfocused Tab Color?
                ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.00f, 0.00f, 0.00f, 0.39f)); //Window Tab Color when hovered
                ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(0.00f, 0.00f, 0.00f, 0.39f)); //Focused Window Tab color
                ImGui.PushStyleColor(ImGuiCol.TabUnfocused, new Vector4(0.00f, 0.00f, 0.00f, 0.39f));
                ImGui.PushStyleColor(ImGuiCol.TabUnfocusedActive, new Vector4(0.35f, 0.30f, 0.30f, 0.50f)); //Unfocused Window Tab color
                ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.24f, 0.15f, 0.15f, 0.60f)); //Menubar/context bar clicked tab?
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.45f, 0.2f, 0.2f, 0.80f)); //Menubar/context bar Hovered Tab
                //ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.26f, 0.59f, 0.98f, 1.00f));
                ImGui.PushStyleColor(ImGuiCol.ResizeGrip, new Vector4(0.5f, 0.39f, 0.39f, 1.00f));
                ImGui.PushStyleColor(ImGuiCol.ResizeGripHovered, new Vector4(0.55f, 0.39f, 0.39f, 1.00f));
                ImGui.PushStyleColor(ImGuiCol.ResizeGripActive, new Vector4(0.60f, 0.39f, 0.39f, 1.00f));
                //ImGui.PushStyleColor(ImGuiCol.PlotLines, new Vector4(0.39f, 0.39f, 0.39f, 1.00f));
                //ImGui.PushStyleColor(ImGuiCol.PlotLinesHovered, new Vector4(1.00f, 0.43f, 0.35f, 1.00f));
                //ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.90f, 0.70f, 0.00f, 1.00f));
                //ImGui.PushStyleColor(ImGuiCol.PlotHistogramHovered, new Vector4(1.00f, 0.60f, 0.00f, 1.00f));
                //ImGui.PushStyleColor(ImGuiCol.TextSelectedBg, new Vector4(0.26f, 0.59f, 0.98f, 0.35f)); // Most Likely Selected Text
            }
            else if (themeName == "ImGuiDark")
            {
                ImGui.StyleColorsDark();
            }
            else if (themeName == "ImGuiLight")
            {
                ImGui.StyleColorsLight();
            }
            else if (themeName == "ImGuiClassic")
            {
                ImGui.StyleColorsClassic();
            }
        }
    }
}
