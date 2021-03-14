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
                    if (ImGui.BeginMenu("This is a Meme"))
                    {
                        ImGui.Checkbox("This opens a WindowMeme", ref _showAnotherWindow);
                        ImGui.Checkbox("Color Picker in WindowMeme", ref _CPickerCheckbox);
                        if (_CPickerCheckbox)
                        {
                            ImGui.Separator();
                            ImGui.Indent();
                            ImGui.Text("haha");
                            ImGui.Unindent();
                            ImGui.Separator();

                        }
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
                ImGui.Columns(2);
                ImGui.BeginChild("FFXTreeView");
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
                if (_cPickerIsEnable)
                {
                    ImGui.Begin("FFX Color Picker");
                    ImGui.Text(_cPickerRed.Attributes[1].Value);
                    ImGui.Text(_cPickerGreen.Attributes[1].Value);
                    ImGui.Text(_cPickerBlue.Attributes[1].Value);
                    ImGui.Text(_cPickerAlpha.Attributes[1].Value);
                    if (ImGui.Button("Close Color Picker"))
                        _cPickerIsEnable = false;
                    ImGui.SameLine();
                    if (ImGui.Button("Commit Color Change"))
                    {
                        _cPickerRed.Attributes[1].Value = _cPicker.X.ToString();
                        _cPickerGreen.Attributes[1].Value = _cPicker.Y.ToString();
                        _cPickerBlue.Attributes[1].Value = _cPicker.Z.ToString();
                        _cPickerAlpha.Attributes[1].Value = _cPicker.W.ToString();
                    }
                    ImGui.ColorPicker4("CPicker", ref _cPicker);
                    ImGui.End();
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
                                                    AXBX = $"A{node2.Attributes[0].Value}B{node2.Attributes[1].Value}";
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
        public static string AXBX;
        public static bool pselected = false;

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
                if (AXBX == "A67B19")
                {
                    int Pos = 0;
                    int StopsCount = Int32.Parse(NodeListEditor.Item(0).Attributes[1].Value);
                    if (ImGui.Selectable($"Number of Stops = {StopsCount}"))
                    {
                        pselected = true;
                        ImGui.Text("aaaaaaaa");
                    }
                    Pos++;
                    for (int i = 0; i < 8; i++)
                    {
                        arrayList.Add("");
                        Pos++;
                    }
                    for (int i = 0; i != StopsCount; i++)
                    {
                        arrayList.Add($"Stop {i} position = {NodeListEditor.Item(i + 9).Attributes[1].Value}");
                        Pos++;
                    }
                    for (int i = 0; i != StopsCount * 4; i += 4)
                    {
                        if (ImGui.Selectable($"Stop Position {i / 4}: Color"))
                        {
                            _cPickerRed = NodeListEditor.Item(Pos);
                            _cPickerGreen = NodeListEditor.Item(Pos + 1);
                            _cPickerBlue = NodeListEditor.Item(Pos + 2);
                            _cPickerAlpha = NodeListEditor.Item(Pos + 3);
                            _cPicker = new Vector4(float.Parse(_cPickerRed.Attributes[1].Value), float.Parse(_cPickerGreen.Attributes[1].Value), float.Parse(_cPickerBlue.Attributes[1].Value), float.Parse(_cPickerAlpha.Attributes[1].Value));
                            _cPickerIsEnable = true;
                        }
                        ImGui.SameLine();
                        ImGui.ColorButton($"Stop Position {i / 4}: Color Color Button", new Vector4(float.Parse(NodeListEditor.Item(Pos).Attributes[1].Value), float.Parse(NodeListEditor.Item(Pos + 1).Attributes[1].Value), float.Parse(NodeListEditor.Item(Pos + 2).Attributes[1].Value), float.Parse(NodeListEditor.Item(Pos + 3).Attributes[1].Value)));
                        Pos += 4;
                    }
                }
                else
                {
                    foreach (XmlNode node in NodeListEditor)
                    {
                        arrayList.Add($"{node.Attributes[0].Value} = {node.Attributes[1].Value}");
                    }
                }
                string[] Entries = (string[])arrayList.ToArray(typeof(string));
                //ImGui.ListBox("Editor Entry's", ref currentitem, Entries, Entries.Length, (int)ImGui.GetWindowSize().Y / 18);
                ImGui.EndChild();
            }
            if (callFlag == "uninit")
            {
                _showFFXEditor = false;
                return;
            }
        }

        public static bool _cPickerIsEnable = false;

        public static XmlNode _cPickerRed;

        public static XmlNode _cPickerGreen;

        public static XmlNode _cPickerBlue;

        public static XmlNode _cPickerAlpha;

        public static Vector4 _cPicker = new Vector4();
    }
}
