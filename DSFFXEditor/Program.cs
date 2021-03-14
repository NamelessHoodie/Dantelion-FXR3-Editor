using System;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using ImGuiNET;
using System.Xml;
using System.Collections;

namespace DSFFXEditor
{
    class DSFFXGUIMain
    {
        private static Sdl2Window _window;
        private static GraphicsDevice _gd;
        private static CommandList _cl;
        private static ImGuiRenderer _controller;
        private static MemoryEditor _memoryEditor;

        // UI state
        private static Vector3 _clearColor = new Vector3(0.45f, 0.55f, 0.6f);
        private static bool _showAnotherWindow = false;
        private static byte[] _memoryEditorData;
        private static string _activeTheme = "DarkRedClay"; //Initialized Default Theme
        private static uint MainViewport;

        //colorpicka
        private static Vector3 _CPickerColor = new Vector3(0, 0, 0);

        //checkbox
        private static bool _CPickerCheckbox = false;

        static bool[] s_opened = { true, true, true, true }; // Persistent user state

        //Theme Selector
        private static int _themeSelectorSelectedItem = 0;
        private static String[] _themeSelectorEntriesArray = { "Red Clay", "ImGui Dark", "ImGui Light", "ImGui Classic" };

        //XML
        private static XmlDocument xDoc = new XmlDocument();
        private static bool XMLOpen = false;

        static void SetThing(out float i, float val) { i = val; }

        [STAThread]
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

            DSFFXThemes.ThemesSelector(_activeTheme); //Default Theme

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
            MainViewport = ImGui.GetID("MainViewPort");
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
                        if (ImGui.MenuItem("Open FFX *XML"))
                        {
                            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
                            ofd.Filter = "XML|*.xml";
                            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                xDoc.Load(ofd.FileName);
                                XMLOpen = true;
                            }
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
                        DSFFXThemes.ThemesSelector(_activeTheme);
                        ImGui.EndMenu();
                    }

                    ImGui.EndMainMenuBar();
                }
                ImGui.DockSpace(MainViewport, new Vector2(0, 0));
                ImGui.End();
            }

            {
                ImGui.SetNextWindowDockID(MainViewport, ImGuiCond.Appearing);
                ImGui.Begin("FFXEditor", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
                ImGui.Columns(3);
                ImGui.BeginChild("FFXTreeView");
                ImGui.Checkbox("Another Window", ref _showAnotherWindow);
                ImGui.Checkbox("Button", ref _CPickerCheckbox);
                if (_CPickerCheckbox)
                {
                    ImGui.Separator();
                    ImGui.Indent();
                    ImGui.Text("haha");
                    ImGui.Unindent();
                    ImGui.Separator();

                }
                if (XMLOpen == true)
                {
                    string[] ActionIDs = { "603", "609" };
                    populateTree(xDoc, ActionIDs);
                }
                ImGui.EndChild();
                if (_showFFXEditor)
                {
                    ImGui.NextColumn();
                    FFXEditor("runtime");
                }
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

        public static void populateTree(XmlDocument XMLDoc, string[] ActionIDs)
        {
            XmlNodeList nodeList = XMLDoc.SelectNodes("descendant::FFXEffectCallA/EffectBs");
            if (ImGui.TreeNodeEx("FFX Parts", ImGuiTreeNodeFlags.None))
            {
                int i = 0;
                foreach (XmlNode node in nodeList)
                {
                    ImGui.Indent();
                    if (ImGui.TreeNodeEx($"{node.Name}-ID={i}", ImGuiTreeNodeFlags.Leaf))
                    {
                        foreach (String ActionID in ActionIDs)
                        {
                            foreach (XmlNode node1 in node.SelectNodes($"descendant::*[@*='{ActionID}']"))
                            {
                                ImGui.Indent();
                                if (ImGui.TreeNodeEx($"ActionID={node1.Attributes[0].Value}-ID={i}", ImGuiTreeNodeFlags.Leaf))
                                {
                                    foreach (XmlNode node2 in node1.SelectNodes("descendant::FFXProperty"))
                                    {
                                        if (node2.Attributes[0].Value == "67" & node2.Attributes[1].Value == "19")
                                        {
                                            ImGui.Indent();
                                            ImGui.Indent();
                                            if (ImGui.TreeNodeEx($"A{node2.Attributes[0].Value}B{node2.Attributes[1].Value} ID={i}", ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.Leaf))
                                            {
                                                XmlNodeList NodeListProcessing = node2.SelectNodes("Fields")[0].ChildNodes;
                                                ImGui.SameLine();
                                                if (ImGui.Button("Edit Here")) 
                                                {
                                                    NodeListEditor = NodeListProcessing;
                                                    FFXEditor("init");
                                                }
                                                ImGui.TreePop();
                                                i++;
                                            }
                                            i++;
                                            ImGui.Unindent();
                                            ImGui.Unindent();
                                        }
                                    }
                                    ImGui.TreePop();
                                }
                                i++;
                                ImGui.Unindent();
                            }
                        }
                        ImGui.TreePop();
                    }
                    i++;
                    ImGui.Unindent();
                }
                ImGui.TreePop();
            }
        }

        public static bool _showFFXEditor = false;
        public static int currentitem = 0;
        public static XmlNodeList NodeListEditor;

        public static void FFXEditor(string callFlag)
        {
            if (callFlag == "init")
            {
                _showFFXEditor = true;
                return;
            }
            else if (callFlag == "runtime")
            {
                ImGui.BeginChild("TxtEdit");
                ArrayList arrayList = new ArrayList();
                foreach (XmlNode node in NodeListEditor)
                {
                    arrayList.Add($"{node.Attributes[0].Value} = {node.Attributes[1].Value}");
                }
                string[] Entries = (string[])arrayList.ToArray(typeof(string));
                ImGui.ListBox("Editor Entry's", ref currentitem, Entries, Entries.Length, (int)ImGui.GetWindowSize().Y / 18);
                ImGui.EndChild();
                ImGui.NextColumn();
                ImGui.BeginChild("params");
                ImGui.Text("aaaa");
                ImGui.Text("aaaa");
                ImGui.Text("aaaa");
                ImGui.Text("aaaa");
                ImGui.Text("aaaa");
                ImGui.Text("aaaa");
                ImGui.EndChild();
            }
            if (callFlag == "uninit")
            {
                _showFFXEditor = false;
                return;
            }
        }
    }
}
