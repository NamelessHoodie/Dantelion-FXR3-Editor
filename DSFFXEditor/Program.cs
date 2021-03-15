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
        private static byte[] _memoryEditorData;
        private static string _activeTheme = "DarkRedClay"; //Initialized Default Theme
        private static uint MainViewport;

        //colorpicka
        private static Vector3 _CPickerColor = new Vector3(0, 0, 0);

        //checkbox

        static bool[] s_opened = { true, true, true, true }; // Persistent user state

        //Theme Selector
        private static int _themeSelectorSelectedItem = 0;
        private static String[] _themeSelectorEntriesArray = { "Red Clay", "ImGui Dark", "ImGui Light", "ImGui Classic" };

        //XML
        private static XmlDocument xDoc = new XmlDocument();
        private static bool XMLOpen = false;

        //FFX Workshop Tools
        //<Color Editor>
        public static bool _cPickerIsEnable = false;

        public static XmlNode _cPickerRed;

        public static XmlNode _cPickerGreen;

        public static XmlNode _cPickerBlue;

        public static XmlNode _cPickerAlpha;

        public static Vector4 _cPicker = new Vector4();

        public static float _colorOverload = 1.0f;
        //</Color Editor>
        //<Floating Point Editor>
        public static bool _floatEditorIsEnable = false;
        //</Floating Point Editor>

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
                if (XMLOpen == true)
                {
                    string[] ActionIDs = { "603", "609" };
                    PopulateTree(xDoc, ActionIDs);
                }
                ImGui.EndChild();
                if (_showFFXEditor)
                {
                    ImGui.NextColumn();
                    FFXEditor("runtime");
                }
                //Tools DockSpace Declaration
                uint WorkshopDockspace = ImGui.GetID("FFX Workshop");
                ImGui.NextColumn();
                ImGui.BeginChild("FFX Workshop");
                ImGui.DockSpace(WorkshopDockspace);
                ImGui.EndChild();
                //Declare Workshop Tools below here
                {
                    if (_cPickerIsEnable)
                    {
                        ImGui.SetNextWindowDockID(WorkshopDockspace, ImGuiCond.Appearing);
                        ImGui.Begin("FFX Color Picker");
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
                        ImGui.ColorPicker4("CPicker", ref _cPicker, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar);
                        ImGui.Separator();
                        ImGui.Text("Brightness Multiplier");
                        ImGui.SliderFloat("###Brightness Multiplier", ref _colorOverload, 1.0f, 10.0f);
                        ImGui.SameLine();
                        if (ImGui.Button("Commit Multiplier"))
                        {
                            _cPicker.X *= _colorOverload;
                            _cPicker.Y *= _colorOverload;
                            _cPicker.Z *= _colorOverload;
                        }
                        ImGui.Separator();
                        ImGui.End();
                    }
                }

                {
                    if (_floatEditorIsEnable)
                    {
                        ImGui.SetNextWindowDockID(WorkshopDockspace, ImGuiCond.Appearing);
                        ImGui.Begin("Floating Point Editor");
                        if (ImGui.Button("Close Floating Point Editor"))
                            _floatEditorIsEnable = false;
                        ImGui.End();
                    }
                    ImGui.End();
                }
            }
        }

        public static void PopulateTree(XmlDocument XMLDoc, string[] ActionIDs)
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
                                        if ((node2.Attributes[0].Value == "67" & node2.Attributes[1].Value == "19") || (node2.Attributes[0].Value == "35" & node2.Attributes[1].Value == "11") || (node2.Attributes[0].Value == "99" & node2.Attributes[1].Value == "27") || (node2.Attributes[0].Value == "4163" & node2.Attributes[1].Value == "35"))
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
                if (AXBX == "A67B19")
                {
                    int Pos = 0;
                    int StopsCount = Int32.Parse(NodeListEditor.Item(0).Attributes[1].Value);
                    Pos += 9;
                    if (ImGui.TreeNodeEx($"Color Stages: Total number of stages = {StopsCount}", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        int LocalColorOffset = Pos + 1;
                        for (int i = 0; i != StopsCount; i++)
                        {
                            ImGui.Separator();
                            ImGui.NewLine();
                            { // Slider Stuff
                                float localSlider = float.Parse(NodeListEditor.Item(i + 9).Attributes[1].Value);
                                ImGui.BulletText($"Stage {i}: Position in time");
                                if (ImGui.SliderFloat($"###Stage{i}Slider", ref localSlider, 0.0f, 2.0f))
                                {
                                    NodeListEditor.Item(i + 9).Attributes[1].Value = localSlider.ToString();
                                }
                                ImGui.SameLine();
                                ImGui.InputFloat($"###Stage{i}FloatInput", ref localSlider);
                                if (ImGui.IsItemDeactivatedAfterEdit())
                                {
                                    NodeListEditor.Item(i + 9).Attributes[1].Value = localSlider.ToString();
                                }
                            }

                            { // ColorButton
                                ImGui.Indent();
                                int PositionOffset = LocalColorOffset + StopsCount - (i + 1);
                                ImGui.Text($"Stage's Color:");
                                ImGui.SameLine();
                                if (ImGui.ColorButton($"Stage Position {i}: Color", new Vector4(float.Parse(NodeListEditor.Item(PositionOffset).Attributes[1].Value), float.Parse(NodeListEditor.Item(PositionOffset + 1).Attributes[1].Value), float.Parse(NodeListEditor.Item(PositionOffset + 2).Attributes[1].Value), float.Parse(NodeListEditor.Item(PositionOffset + 3).Attributes[1].Value)), ImGuiColorEditFlags.AlphaPreview, new Vector2(30, 30)))
                                {
                                    _cPickerRed = NodeListEditor.Item(PositionOffset);
                                    _cPickerGreen = NodeListEditor.Item(PositionOffset + 1);
                                    _cPickerBlue = NodeListEditor.Item(PositionOffset + 2);
                                    _cPickerAlpha = NodeListEditor.Item(PositionOffset + 3);
                                    _cPicker = new Vector4(float.Parse(_cPickerRed.Attributes[1].Value), float.Parse(_cPickerGreen.Attributes[1].Value), float.Parse(_cPickerBlue.Attributes[1].Value), float.Parse(_cPickerAlpha.Attributes[1].Value));
                                    _cPickerIsEnable = true;
                                    ImGui.SetWindowFocus("FFX Color Picker");
                                }
                                LocalColorOffset += 5;
                                ImGui.Unindent();
                            }
                            ImGui.NewLine();
                        }
                        ImGui.Separator();
                        ImGui.TreePop();
                    }
                }
                else if (AXBX == "A35B11")
                {
                    ImGui.BulletText("Single Static Color:");
                    ImGui.Indent();
                    ImGui.Indent();
                    if (ImGui.ColorButton($"Static Color", new Vector4(float.Parse(NodeListEditor.Item(0).Attributes[1].Value), float.Parse(NodeListEditor.Item(1).Attributes[1].Value), float.Parse(NodeListEditor.Item(2).Attributes[1].Value), float.Parse(NodeListEditor.Item(3).Attributes[1].Value)), ImGuiColorEditFlags.AlphaPreview, new Vector2(30, 30)))
                    {
                        _cPickerRed = NodeListEditor.Item(0);
                        _cPickerGreen = NodeListEditor.Item(1);
                        _cPickerBlue = NodeListEditor.Item(2);
                        _cPickerAlpha = NodeListEditor.Item(3);
                        _cPicker = new Vector4(float.Parse(_cPickerRed.Attributes[1].Value), float.Parse(_cPickerGreen.Attributes[1].Value), float.Parse(_cPickerBlue.Attributes[1].Value), float.Parse(_cPickerAlpha.Attributes[1].Value));
                        _cPickerIsEnable = true;
                        ImGui.SetWindowFocus("FFX Color Picker");
                    }
                    ImGui.Unindent();
                    ImGui.Unindent();
                }
                else
                {
                    foreach (XmlNode node in NodeListEditor)
                    {
                        ImGui.TextWrapped($"{node.Attributes[0].Value} = {node.Attributes[1].Value}");
                    }
                }
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
