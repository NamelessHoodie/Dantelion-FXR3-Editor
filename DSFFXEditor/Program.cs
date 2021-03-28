using System;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using ImGuiNET;
using ImPlotNET;
using ImNodesNET;
using ImGuizmoNET;
using System.Xml;
using System.Collections;
using ImGuiNETAddons;
using System.Xml.Linq;

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
        private static bool _keyboardInputGuide = false;

        // Save/Load Path
        private static String _loadedFilePath = "";

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
        private static bool _axbxDebug = false;

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

        [STAThread]
        static void Main()
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

        private static bool _axbyEditorIsPopup = false;
        private static int _axbyEditorSelectedItem;
        private static XmlNode _axbyeditoractionidnode;
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
                                _loadedFilePath = ofd.FileName;
                                xDoc.Load(ofd.FileName);
                                XMLOpen = true;
                            }
                        }
                        if (_loadedFilePath != "")
                        {
                            if (ImGui.MenuItem("Save Open FFX *XML"))
                            {
                                xDoc.Save(_loadedFilePath);
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
                    if (ImGui.BeginMenu("Useful Info"))
                    {
                        ImGui.Text("Keyboard Interactions Guide");
                        ImGui.SameLine();
                        ImGuiAddons.ToggleButton("Keyboard InteractionsToggle", ref _keyboardInputGuide);
                        ImGui.Text("axbx Debugger");
                        ImGui.SameLine();
                        ImGuiAddons.ToggleButton("axbxDebugger", ref _axbxDebug);
                        ImGui.Text("No ActionID Filter");
                        ImGui.SameLine();
                        ImGuiAddons.ToggleButton("No ActionID Filter", ref _filtertoggle);
                        ImGui.EndMenu();
                    }
                    ImGui.EndMainMenuBar();
                }
                ImGui.DockSpace(MainViewport, new Vector2(0, 0));
                ImGui.End();
            }

            { //Declare Standalone Windows here
                if (_keyboardInputGuide)
                {
                    ImGui.SetNextWindowDockID(MainViewport);
                    ImGui.Begin("Keyboard Guide", ImGuiWindowFlags.MenuBar);
                    ImGui.BeginMenuBar();
                    ImGui.EndMenuBar();
                    ImGui.ShowUserGuide();
                    ImGui.End();
                }
                if (_axbyEditorIsPopup) //Currently Unused FFXProperty Changer
                {
                    if (!ImGui.IsPopupOpen("AxByTypeEditor"))
                    {
                        ImGui.OpenPopup("AxByTypeEditor");
                    }
                    float popupWidth = 400;
                    float popupHeight = 250;
                    ImGui.SetNextWindowSize(new Vector2(popupWidth, popupHeight));
                    ImGui.SetNextWindowPos(new Vector2(viewport->Pos.X + (viewport->Size.X / 2) - (popupWidth / 2), viewport->Pos.Y + (viewport->Size.Y / 2) - (popupHeight / 2)));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8.0f);
                    if (ImGui.BeginPopupModal("AxByTypeEditor", ref _axbyEditorIsPopup, ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize))
                    {
                        //ImGui.PushStyleColor(ImGuiCol.ModalWindowDimBg, ImGui.GetColorU32(ImGuiCol.ButtonHovered));
                        ArrayList localaxbylist = new ArrayList();
                        string actionid = _axbyeditoractionidnode.ParentNode.ParentNode.Attributes[0].Value;
                        int indexinparent = GetNodeIndexinParent(_axbyeditoractionidnode);
                        localaxbylist.Add($"{indexinparent}: A{_axbyeditoractionidnode.Attributes[0].Value}B{_axbyeditoractionidnode.Attributes[1].Value}");
                        string[] meme = new string[localaxbylist.Count];

                        localaxbylist.CopyTo(meme);
                        ImGui.Text("FFXProperty Type Editor");
                        ImGui.Text(actionid);
                        ImGui.Combo("i am a combo", ref _axbyEditorSelectedItem, meme, meme.Length);

                        if (ImGui.Button("OK")) { ImGui.CloseCurrentPopup(); }
                        ImGui.SameLine();
                        if (ImGui.Button("Cancel")) { ImGui.CloseCurrentPopup(); }
                        if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Escape))) { ImGui.CloseCurrentPopup(); }
                        ImGui.EndPopup();
                    }
                    ImGui.PopStyleVar();
                    if (!ImGui.IsPopupOpen("AxByTypeEditor"))
                    {
                        _axbyEditorIsPopup = false;
                    }
                }
            }

            { //Main Window Here
                ImGui.SetNextWindowDockID(MainViewport, ImGuiCond.Appearing);
                ImGui.Begin("FFXEditor", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
                ImGui.Columns(3);
                ImGui.BeginChild("FFXTreeView");
                if (XMLOpen == true)
                {
                    PopulateTree(xDoc.SelectSingleNode("descendant::RootEffectCall"));

                }
                ImGui.EndChild();
                if (_showFFXEditorProperties || _showFFXEditorFields)
                {
                    ImGui.NextColumn();
                    FFXEditor();
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
                        if (ImGuiAddons.ButtonGradient("Close Color Picker"))
                            _cPickerIsEnable = false;
                        ImGui.SameLine();
                        if (ImGuiAddons.ButtonGradient("Commit Color Change"))
                        {
                            _cPickerRed.Attributes[1].Value = MathF.Round(_cPicker.X, 4, MidpointRounding.ToZero).ToString();
                            _cPickerGreen.Attributes[1].Value = MathF.Round(_cPicker.Y, 4, MidpointRounding.ToZero).ToString();
                            _cPickerBlue.Attributes[1].Value = MathF.Round(_cPicker.Z, 4, MidpointRounding.ToZero).ToString();
                            _cPickerAlpha.Attributes[1].Value = MathF.Round(_cPicker.W, 4, MidpointRounding.ToZero).ToString();
                        }
                        ImGui.ColorPicker4("CPicker", ref _cPicker, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar);
                        ImGui.Separator();
                        ImGui.Text("Brightness Multiplier");
                        ImGui.SliderFloat("###Brightness Multiplier", ref _colorOverload, 1.0f, 10.0f);
                        ImGui.SameLine();
                        if (ImGuiAddons.ButtonGradient("Apply Change"))
                        {
                            _cPicker.X *= _colorOverload;
                            _cPicker.Y *= _colorOverload;
                            _cPicker.Z *= _colorOverload;
                        }
                        ImGui.Separator();
                        ImGui.End();
                    }
                }
            }
        }

        private static bool _filtertoggle = false;
        private static void PopulateTree(XmlNode root)
        {
            if (root is XmlElement)
            {
                ImGui.PushID($"TreeFunctionlayer = {root.Name} ChildIndex = {GetNodeIndexinParent(root)}");
                if (root.Attributes["ActionID"] != null)
                {
                    string[] _actionIDsFilter = { "600", "601", "602", "603", "604", "605", "606", "607", "609", "10012" };
                    if (_actionIDsFilter.Contains(root.Attributes[0].Value) || _filtertoggle)
                    {
                        if (ImGui.TreeNodeEx($"ActionID = {root.Attributes[0].Value}", ImGuiTreeNodeFlags.None))
                        {
                            GetFFXProperties(root, "Properties1");
                            GetFFXProperties(root, "Properties2");
                            GetFFXFields(root, "F1");
                            GetFFXFields(root, "F2");
                            ImGui.TreePop();
                        }
                    }
                }
                else if (root.Name == "EffectAs" || root.Name == "EffectBs" || root.Name == "RootEffectCall" || root.Name == "Actions")
                {
                    if (root.HasChildNodes)
                    {
                        foreach (XmlNode node in root.ChildNodes)
                        {
                            PopulateTree(node);
                        }
                    }
                }
                else if (root.Name == "FFXEffectCallA")
                {
                    if (root.HasChildNodes)
                    {
                        if (ImGui.TreeNodeEx($"FFX Container = {root.Attributes[0].Value}"))
                        {
                            foreach (XmlNode node in root.ChildNodes)
                            {
                                PopulateTree(node);
                            }
                            ImGui.TreePop();
                        }
                    }
                }
                else if (root.Name == "FFXEffectCallB")
                {
                    if (root.HasChildNodes)
                    {
                        if (ImGui.TreeNodeEx($"FFX Call"))
                        {
                            foreach (XmlNode node in root.ChildNodes)
                            {
                                PopulateTree(node);
                            }
                            ImGui.TreePop();
                        }
                    }
                }
                else
                {
                    if (ImGui.TreeNodeEx($"{root.Name}"))
                    {
                        //DoWork(root);
                        foreach (XmlNode node in root.ChildNodes)
                        {
                            PopulateTree(node);
                        }
                        ImGui.TreePop();
                    }
                }
                ImGui.PopID();
            }
            else if (root is XmlText)
            { }
            else if (root is XmlComment)
            { }
        }

        private static int GetNodeIndexinParent(XmlNode Node)
        {
            int ChildIndex = 0;
            if (Node.PreviousSibling != null)
            {
                XmlNode LocalNode = Node.PreviousSibling;
                ChildIndex++;
                while (LocalNode.PreviousSibling != null)
                {
                    LocalNode = LocalNode.PreviousSibling;
                    ChildIndex++;
                }
            }
            return ChildIndex;
        }

        public static bool _showFFXEditorFields = false;
        public static bool _showFFXEditorProperties = false;
        public static int currentitem = 0;
        public static XmlNodeList NodeListEditor;
        public static string Fields;
        public static string AxBy;
        public static bool pselected = false;

        public static void FFXEditor()
        {
            ImGui.BeginChild("TxtEdit");
            if (_showFFXEditorProperties)
            {
                switch (AxBy)
                {
                    case "A35B11":
                        ImGui.Text("FFX Property = A35B11");
                        FFXPropertyA35B11StaticColor(NodeListEditor);
                        break;
                    case "A67B19":
                        ImGui.Text("FFX Property = A67B19");
                        FFXPropertyA67B19ColorInterpolationLinear(NodeListEditor);
                        break;
                    case "A99B27":
                        ImGui.Text("FFX Property = A99B27");
                        FFXPropertyA99B27ColorInterpolationWithCustomCurve(NodeListEditor);
                        break;
                    case "A4163B35":
                        ImGui.Text("FFX Property = A4163B35");
                        FFXPropertyA67B19ColorInterpolationLinear(NodeListEditor);
                        break;
                    default:
                        ImGui.Text("ERROR: FFX Property Handler not found, using Default Read Only Handler.");
                        foreach (XmlNode node in NodeListEditor)
                        {
                            ImGui.TextWrapped($"{node.Attributes[0].Value} = {node.Attributes[1].Value}");
                        }
                        break;
                }
            }
            else if (_showFFXEditorFields)
            {
                if (Fields.Contains("F1"))
                {
                    switch (Fields)
                    {
                        case "F1MEME":
                            ImGui.Text("FFX Property = A35B11");
                            FFXPropertyA35B11StaticColor(NodeListEditor);
                            break;
                        default:
                            ImGui.Text("ERROR: FFX Fields1 Handler not found, using Default Read Only Handler.");
                            foreach (XmlNode node in NodeListEditor)
                            {
                                ImGui.TextWrapped($"FFXField({node.Attributes[0].Value}) = {node.Attributes[1].Value}");
                            }
                            break;
                    }
                }
                else if (Fields.Contains("F2"))
                {
                    switch (Fields)
                    {
                        case "F2MEME":
                            ImGui.Text("FFX Property = A35B11");
                            FFXPropertyA35B11StaticColor(NodeListEditor);
                            break;
                        default:
                            ImGui.Text("ERROR: FFX Fields2 Handler not found, using Default Read Only Handler.");
                            foreach (XmlNode node in NodeListEditor)
                            {
                                ImGui.TextWrapped($"FFXField({node.Attributes[0].Value}) = {node.Attributes[1].Value}");
                            }
                            break;
                    }
                }

            }
            ImGui.EndChild();
            //
            if (_axbxDebug)
            {
                ImGui.SetNextWindowDockID(MainViewport);
                ImGui.Begin("axbxDebug");
                int integer = 0;
                foreach (XmlNode node in NodeListEditor.Item(0).ParentNode.ChildNodes)
                {
                    ImGui.Text($"TempID = '{integer}' XMLElementName = '{node.LocalName}' AttributesNum = '{node.Attributes.Count}' Attributes({node.Attributes[0].Name} = '{node.Attributes[0].Value}', {node.Attributes[1].Name} = '{float.Parse(node.Attributes[1].Value)}')");
                    integer++;
                }
                ImGui.End();
            }
        }
        private static void GetFFXFields(XmlNode root, string fieldType)
        {
            string localFieldTypeString = "Fields1";
            string fieldNodeLabel = "Fields 1";
            if (fieldType == "F2")
            {
                localFieldTypeString = "Fields2";
                fieldNodeLabel = "Fields 2";
            }
            XmlNodeList NodeListProcessing = root.SelectNodes($"descendant::{localFieldTypeString}")[0].ChildNodes;
            if (NodeListProcessing.Count > 0)
            {
                uint IDStorage = ImGui.GetID(fieldNodeLabel);
                ImGuiStoragePtr storage = ImGui.GetStateStorage();
                bool selected = storage.GetBool(IDStorage);
                if (selected & IDStorage != treeViewCurrentHighlighted)
                {
                    storage.SetBool(IDStorage, false);
                    selected = false;
                }
                ImGuiTreeNodeFlags localTreeNodeFlags = ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth;
                if (selected)
                    localTreeNodeFlags |= ImGuiTreeNodeFlags.Selected;
                ImGui.TreeNodeEx($"{fieldNodeLabel}", localTreeNodeFlags);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left) & !selected)
                {
                    treeViewCurrentHighlighted = IDStorage;
                    storage.SetBool(IDStorage, true);
                    NodeListEditor = NodeListProcessing;
                    Fields = $"{fieldType}{root.Attributes[0]}";
                    _showFFXEditorProperties = false;
                    _showFFXEditorFields = true;
                }
            }
        }
        private static uint treeViewCurrentHighlighted = 0;
        private static void GetFFXProperties(XmlNode root, string PropertyType)
        {
            XmlNodeList localNodeList = root.SelectNodes($"descendant::{PropertyType}/FFXProperty");
            if (localNodeList.Count > 0)
            {
                if (ImGui.TreeNodeEx($"{PropertyType}"))
                {
                    ImGui.Unindent();
                    if (ImGui.BeginTable("##table2", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
                    {
                        ImGui.TableSetupColumn("Type");
                        ImGui.TableSetupColumn("Arg");
                        ImGui.TableSetupColumn("Field");
                        ImGui.TableSetupColumn("Input Type");
                        ImGui.TableHeadersRow();
                        foreach (XmlNode Node in localNodeList)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            string localAxBy = $"A{Node.Attributes[0].Value}B{Node.Attributes[1].Value}";
                            string localIndex = $"{GetNodeIndexinParent(Node)}:";
                            string[] localSlot = ActionIDtoIndextoName(Node);
                            string localInput = AxByToName(Node);
                            string localLabel = $"{localIndex} {localSlot[0]}: {localSlot[1]} {localInput}";
                            ImGui.PushID($"ItemForLoopNode = {localLabel}");
                            if (localAxBy == "A67B19" || localAxBy == "A35B11" || localAxBy == "A99B27" || (Node.Attributes[0].Value == "A4163B35"))
                            {
                                XmlNodeList NodeListProcessing = Node.SelectNodes("Fields")[0].ChildNodes;
                                uint IDStorage = ImGui.GetID(localLabel);
                                ImGuiStoragePtr storage = ImGui.GetStateStorage();
                                bool selected = storage.GetBool(IDStorage);
                                if (selected & IDStorage != treeViewCurrentHighlighted)
                                {
                                    storage.SetBool(IDStorage, false);
                                    selected = false;
                                }
                                Vector2 cursorPos = ImGui.GetCursorPos();
                                ImGui.BulletText($"{localSlot[0]}");
                                if (ImGui.IsItemHovered() & ImGui.GetIO().KeyAlt)
                                    ShowToolTip(localSlot[0], "Type");
                                ImGui.SetCursorPos(cursorPos);
                                ImGui.Selectable($"###{localLabel}", selected, ImGuiSelectableFlags.SpanAllColumns);
                                if (ImGui.IsItemClicked(ImGuiMouseButton.Left) & !selected)
                                {
                                    treeViewCurrentHighlighted = IDStorage;
                                    storage.SetBool(IDStorage, true);
                                    NodeListEditor = NodeListProcessing;
                                    AxBy = localAxBy;
                                    _showFFXEditorProperties = true;
                                    _showFFXEditorFields = false;
                                }
                                ImGui.TableNextColumn();
                                ImGui.Text(localSlot[1]);
                                if (ImGui.IsItemHovered() & ImGui.GetIO().KeyAlt)
                                    ShowToolTip(localSlot[1], "Arg");
                                ImGui.TableNextColumn();
                                ImGui.Text(localSlot[2]);
                                ImGui.TableNextColumn();
                                ImGui.Text(localInput);
                                /*if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                                {
                                    _axbyeditoractionidnode = Node;
                                    _axbyEditorIsPopup = true;
                                }*/
                            }
                            else
                            {
                                ImGui.Indent();
                                ImGui.Text(localSlot[0]);
                                if (ImGui.IsItemHovered() & ImGui.GetIO().KeyAlt)
                                    ShowToolTip(localSlot[0], "Type");
                                ImGui.Unindent();
                                ImGui.TableNextColumn();
                                ImGui.Text(localSlot[1]);
                                if (ImGui.IsItemHovered() & ImGui.GetIO().KeyAlt)
                                    ShowToolTip(localSlot[1], "Arg");
                                ImGui.TableNextColumn();
                                ImGui.Text(localSlot[2]);
                                ImGui.TableNextColumn();
                                ImGui.Text(localInput);
                            }
                            ImGui.PopID();
                        }
                        ImGui.EndTable();
                    }
                    ImGui.Indent();
                    ImGui.TreePop();
                }
            }
        }
        private static void ShowToolTip(string input, string toolTipType)
        {
            ImGuiWindowFlags localtoolTipFlags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.Tooltip;
            if (toolTipType == "Arg" || toolTipType == "Type")
            {
                string localOutput;
                switch (input)
                {
                    case "[C]":
                        localOutput = $"{input} = Color Archetype";
                        break;
                    case "[S]":
                        localOutput = $"{input} = Scalar Archetype";
                        break;
                    case "[P]":
                        localOutput = $"{input} = Particle Argument";
                        break;
                    case "[PG]":
                        localOutput = $"{input} = Particle On Generation Argument";
                        break;
                    case "[TG]":
                        localOutput = $"{input} = Trail On Generation Argument";
                        break;
                    case "[E]":
                        localOutput = $"{input} = Effect Argument";
                        break;
                    case "[u]":
                        localOutput = $"{input} = Unknown";
                        break;
                    default:
                        localOutput = $"{input} = No Tooltip was found for the symbol";
                        break;
                }
                ImGui.SetNextWindowPos(ImGui.GetCursorPos());
                if (ImGui.Begin("StandardToolTip", localtoolTipFlags))
                {
                    ImGui.Text($"{toolTipType} Tooltip:");
                    ImGui.Text(localOutput);
                    ImGui.End();
                }
            }
            else
            {
                ImGui.SetNextWindowPos(ImGui.GetCursorPos());
                if (ImGui.Begin("StandardToolTip", localtoolTipFlags))
                {
                    ImGui.Text($"{toolTipType} Tooltip:");
                    ImGui.Text("ERROR: No Tooltip was found");
                    ImGui.End();
                }
            }
        }
        private static string[] ActionIDtoIndextoName(XmlNode Node)
        {
            int localActionID = Int32.Parse(Node.ParentNode.ParentNode.Attributes[0].Value);
            int localPropertyIndex = GetNodeIndexinParent(Node);
            string scalar = "[S]";
            string color = "[C]";
            string particleArg = "[P]";
            string effectArg = "[E]";
            string particleGenArg = "[PG]";
            string unknown = "[u]";
            string trailArg = "[T]";
            string trailGenArg = "[TG]";
            if (Node.ParentNode.Name == "Properties1") //Properties1 Here
            {
                switch (localActionID)
                {
                    case 600:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, particleArg, "Scale*" };
                            case 1:
                                return new string[] { color, particleArg, "Color*" };
                            case 2:
                                return new string[] { color, particleGenArg, "Color*" };
                            case 3:
                                return new string[] { color, effectArg, "Color*" };
                        }
                        break;
                    case 601:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, particleGenArg, "Lenght*" };
                            case 1:
                                return new string[] { color, particleArg, "Color*" };
                            case 2:
                                return new string[] { color, particleArg, "Color*" };
                            case 3:
                                return new string[] { color, effectArg, "Start Color" };
                            case 4:
                                return new string[] { color, effectArg, "End Color" };
                            case 5:
                                return new string[] { scalar, particleArg, "Lenght*" };
                            case 6:
                                return new string[] { color, effectArg, "Color*" };
                        }
                        break;
                    case 602:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, particleGenArg, "X Scale*" };
                            case 1:
                                return new string[] { scalar, particleGenArg, "Y Scale*" };
                            case 2:
                                return new string[] { color, particleArg, "Color*" };
                            case 3:
                                return new string[] { color, particleArg, "Color*" };
                            case 4:
                                return new string[] { color, effectArg, "Top Color" };
                            case 5:
                                return new string[] { color, effectArg, "Bottom Color" };
                            case 6:
                                return new string[] { scalar, particleArg, "Z Scale*" };
                            case 7:
                                return new string[] { scalar, particleArg, "Y Scale*" };
                            case 8:
                                return new string[] { color, effectArg, "Color*" };
                        }
                        break;
                    case 603:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, particleArg, "X Offset" };
                            case 1:
                                return new string[] { scalar, particleArg, "Y Offset" };
                            case 2:
                                return new string[] { scalar, particleArg, "Z Offset" };
                            case 3:
                                return new string[] { scalar, particleArg, "Scale*" };
                            case 4:
                                return new string[] { scalar, unknown, "Unk" };
                            case 5:
                                return new string[] { color, particleArg, "Color*" };
                            case 6:
                                return new string[] { color, particleGenArg, "Color*" };
                            case 7:
                                return new string[] { color, effectArg, "Color*" };
                            case 8:
                                return new string[] { scalar, particleArg, "Opacity Threshold" };
                            case 9:
                                return new string[] { scalar, particleArg, "X Rotation" };
                            case 10:
                                return new string[] { scalar, particleArg, "Y Rotation" };
                            case 11:
                                return new string[] { scalar, particleArg, "Z Rotation" };
                            case 12:
                                return new string[] { scalar, particleArg, "X Rotation° Speed" };
                            case 13:
                                return new string[] { scalar, particleArg, "X Rotation° Speed*" };
                            case 14:
                                return new string[] { scalar, particleArg, "Y Rotation° Speed" };
                            case 15:
                                return new string[] { scalar, particleArg, "Y Rotation° Speed*" };
                            case 16:
                                return new string[] { scalar, particleArg, "Z Rotation° Speed" };
                            case 17:
                                return new string[] { scalar, particleArg, "Z Rotation° Speed*" };
                            case 18:
                                return new string[] { scalar, unknown, "-Z Position" };
                            case 19:
                                return new string[] { scalar, unknown, "Texture Frame Offset" };
                            case 20:
                                return new string[] { scalar, unknown, "Texture Frame Index" };
                            case 21:
                                return new string[] { scalar, unknown, "Unk" };
                            case 22:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;
                    case 604:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, particleArg, "X Offset" };
                            case 1:
                                return new string[] { scalar, particleArg, "Y Offset" };
                            case 2:
                                return new string[] { scalar, particleArg, "Z Offset" };
                            case 3:
                                return new string[] { scalar, particleArg, "Scale*" };
                            case 4:
                                return new string[] { scalar, unknown, "Unk" };
                            case 5:
                                return new string[] { scalar, unknown, "Unk" };
                            case 6:
                                return new string[] { scalar, unknown, "Unk" };
                            case 7:
                                return new string[] { scalar, particleArg, "Z Rotation" };
                            case 8:
                                return new string[] { scalar, unknown, "Unk" };
                            case 9:
                                return new string[] { scalar, unknown, "Unk" };
                            case 10:
                                return new string[] { scalar, unknown, "Unk" };
                            case 11:
                                return new string[] { scalar, unknown, "Unk" };
                            case 12:
                                return new string[] { scalar, unknown, "Unk" };
                            case 13:
                                return new string[] { scalar, unknown, "Unk" };
                            case 14:
                                return new string[] { color, particleArg, "Color multiplier" };
                            case 15:
                                return new string[] { color, unknown, "Unk" };
                            case 16:
                                return new string[] { color, unknown, "Unk" };
                            case 17:
                                return new string[] { color, unknown, "Unk" };
                            case 18:
                                return new string[] { color, particleArg, "Color multiplier" };
                            case 19:
                                return new string[] { color, unknown, "Unk" };
                            case 20:
                                return new string[] { scalar, unknown, "Unk" };
                            case 21:
                                return new string[] { scalar, unknown, "1st Texture Frame Offset" };
                            case 22:
                                return new string[] { scalar, unknown, "1st Texture Frame Index 1" };
                            case 23:
                                return new string[] { scalar, unknown, "Unk" };
                            case 24:
                                return new string[] { scalar, unknown, "Unk" };
                            case 25:
                                return new string[] { scalar, unknown, "Unk" };
                            case 26:
                                return new string[] { scalar, unknown, "Unk" };
                            case 27:
                                return new string[] { scalar, unknown, "Unk" };
                            case 28:
                                return new string[] { scalar, unknown, "Unk" };
                            case 29:
                                return new string[] { scalar, unknown, "2nd Texture X Scroll Speed" };
                            case 30:
                                return new string[] { scalar, unknown, "2nd Texture Y Scroll Speed" };
                            case 31:
                                return new string[] { scalar, unknown, "Unk" };
                            case 32:
                                return new string[] { scalar, unknown, "Unk" };
                            case 33:
                                return new string[] { scalar, unknown, "Unk" };
                            case 34:
                                return new string[] { scalar, unknown, "Unk" };
                            case 35:
                                return new string[] { scalar, unknown, "3rd Texture X Scroll Speed" };
                            case 36:
                                return new string[] { scalar, unknown, "3rd Texture Y Scroll Speed" };
                            case 37:
                                return new string[] { scalar, unknown, "Unk" };
                            case 38:
                                return new string[] { scalar, unknown, "Unk" };
                            case 39:
                                return new string[] { scalar, unknown, "Unk" };
                            case 40:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;
                    case 605:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, particleArg, "X Scale*" };
                            case 1:
                                return new string[] { scalar, particleArg, "Y Scale*" };
                            case 2:
                                return new string[] { scalar, particleArg, "Z Scale*" };
                            case 3:
                                return new string[] { scalar, particleArg, "X Rotation" };
                            case 4:
                                return new string[] { scalar, particleArg, "Y Rotation" };
                            case 5:
                                return new string[] { scalar, particleArg, "Z Rotation" };
                            case 6:
                                return new string[] { scalar, particleArg, "X Rotation° Speed" };
                            case 7:
                                return new string[] { scalar, particleArg, "X Rotation° Speed*" };
                            case 8:
                                return new string[] { scalar, particleArg, "Y Rotation° Speed" };
                            case 9:
                                return new string[] { scalar, particleArg, "Y Rotation° Speed*" };
                            case 10:
                                return new string[] { scalar, particleArg, "Z Rotation° Speed" };
                            case 11:
                                return new string[] { scalar, particleArg, "Z Rotation° Speed*" };
                            case 12:
                                return new string[] { color, particleArg, "Color*" };
                            case 13:
                                return new string[] { color, particleGenArg, "Color*" };
                            case 14:
                                return new string[] { color, effectArg, "Color*" };
                            case 15:
                                return new string[] { scalar, unknown, "Unk" };
                            case 16:
                                return new string[] { scalar, unknown, "Texture Frame Index" };
                            case 17:
                                return new string[] { scalar, unknown, "Unk" };
                            case 18:
                                return new string[] { scalar, unknown, "Unk" };
                            case 19:
                                return new string[] { scalar, unknown, "Unk" };
                            case 20:
                                return new string[] { scalar, unknown, "Unk" };
                            case 21:
                                return new string[] { scalar, unknown, "Unk" };
                            case 22:
                                return new string[] { scalar, unknown, "Unk" };
                            case 23:
                                return new string[] { scalar, unknown, "Unk" };
                            case 24:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;
                    case 606:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, unknown, "Scale*" };
                            case 1:
                                return new string[] { scalar, unknown, "Unk" };
                            case 2:
                                return new string[] { scalar, unknown, "Unk" };
                            case 3:
                                return new string[] { scalar, unknown, "Unk" };
                            case 4:
                                return new string[] { color, trailArg, "Color*" };
                            case 5:
                                return new string[] { color, trailGenArg, "Color*" };
                            case 6:
                                return new string[] { color, effectArg, "Color*" };
                            case 7:
                                return new string[] { scalar, unknown, "Unk" };
                            case 8:
                                return new string[] { scalar, unknown, "Unk" };
                            case 9:
                                return new string[] { scalar, trailArg, "Texture Frame Index" };
                            case 10:
                                return new string[] { scalar, unknown, "Unk" };
                            case 11:
                                return new string[] { scalar, unknown, "Unk" };
                            case 12:
                                return new string[] { scalar, unknown, "Unk" };
                            case 13:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;
                    case 607:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, unknown, "Unk" };
                            case 1:
                                return new string[] { scalar, unknown, "Unk" };
                            case 2:
                                return new string[] { scalar, unknown, "Unk" };
                            case 3:
                                return new string[] { scalar, unknown, "Unk" };
                            case 4:
                                return new string[] { scalar, unknown, "Unk" };
                            case 5:
                                return new string[] { scalar, unknown, "Unk" };
                            case 6:
                                return new string[] { color, particleArg, "Color*" };
                            case 7:
                                return new string[] { color, unknown, "Color*" };
                            case 8:
                                return new string[] { scalar, unknown, "Unk" };
                            case 9:
                                return new string[] { scalar, unknown, "Unk" };
                            case 10:
                                return new string[] { scalar, unknown, "Unk" };
                            case 11:
                                return new string[] { scalar, unknown, "Unk" };
                            case 12:
                                return new string[] { scalar, unknown, "Unk" };
                            case 13:
                                return new string[] { scalar, unknown, "Unk" };
                            case 14:
                                return new string[] { scalar, unknown, "Unk" };
                            case 15:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;
                    case 609:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { color, unknown, "Light Color" };
                            case 1:
                                return new string[] { color, unknown, "Specual Color" };
                            case 2:
                                return new string[] { scalar, unknown, "Light Radius" };
                            case 3:
                                return new string[] { scalar, unknown, "Unk" };
                            case 4:
                                return new string[] { scalar, unknown, "Unk" };
                            case 5:
                                return new string[] { scalar, unknown, "Unk" };
                            case 6:
                                return new string[] { scalar, unknown, "Unk" };
                            case 7:
                                return new string[] { scalar, unknown, "Unk" };
                            case 8:
                                return new string[] { scalar, unknown, "Unk" };
                            case 9:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;
                    case 10012:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, unknown, "Trail Size" };
                            case 1:
                                return new string[] { scalar, unknown, "Scale*" };
                            case 2:
                                return new string[] { scalar, unknown, "Unk" };
                            case 3:
                                return new string[] { scalar, unknown, "Unk" };
                            case 4:
                                return new string[] { color, trailArg, "Color*" };
                            case 5:
                                return new string[] { color, trailGenArg, "Color*" };
                            case 6:
                                return new string[] { color, effectArg, "Color*" };
                            case 7:
                                return new string[] { scalar, unknown, "Unk" };
                            case 8:
                                return new string[] { scalar, unknown, "Unk" };
                            case 9:
                                return new string[] { scalar, unknown, "Unk" };
                            case 10:
                                return new string[] { scalar, unknown, "Segment Tex Width" };
                            case 11:
                                return new string[] { scalar, unknown, "Horizontal Tex Scroll speed" };
                            case 12:
                                return new string[] { scalar, unknown, "Vertical Tex Offset Range" };
                            case 13:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;
                }
            }
            else //Properties2 Here
            {
                switch (localActionID)
                {
                    case 600:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, unknown, "Unk" };
                            case 1:
                                return new string[] { scalar, unknown, "Unk" };
                            case 2:
                                return new string[] { scalar, unknown, "Unk" };
                            case 3:
                                return new string[] { color, unknown, "Unk" };
                            case 4:
                                return new string[] { color, unknown, "Unk" };
                            case 5:
                                return new string[] { color, unknown, "Unk" };
                            case 6:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;

                    case 601:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, unknown, "Brightness*" };
                            case 1:
                                return new string[] { scalar, unknown, "Brightness*" };
                            case 2:
                                return new string[] { scalar, unknown, "Unk" };
                            case 3:
                                return new string[] { color, unknown, "Unk" };
                            case 4:
                                return new string[] { color, unknown, "Unk" };
                            case 5:
                                return new string[] { color, unknown, "Unk" };
                            case 6:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;

                    case 602:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, unknown, "Brightness*" };
                            case 1:
                                return new string[] { scalar, unknown, "Brightness*" };
                            case 2:
                                return new string[] { scalar, unknown, "Unk" };
                            case 3:
                                return new string[] { color, unknown, "Unk" };
                            case 4:
                                return new string[] { color, unknown, "Unk" };
                            case 5:
                                return new string[] { color, unknown, "Unk" };
                            case 6:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;

                    case 603:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, unknown, "Brightness*" };
                            case 1:
                                return new string[] { scalar, unknown, "Brightness*" };
                            case 2:
                                return new string[] { scalar, unknown, "Unk" };
                            case 3:
                                return new string[] { color, unknown, "Unk" };
                            case 4:
                                return new string[] { color, unknown, "Unk" };
                            case 5:
                                return new string[] { color, unknown, "Unk" };
                            case 6:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;
                    case 604:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, unknown, "Brightness*" };
                            case 1:
                                return new string[] { scalar, unknown, "Brightness*" };
                            case 2:
                                return new string[] { scalar, unknown, "Unk" };
                            case 3:
                                return new string[] { color, unknown, "Unk" };
                            case 4:
                                return new string[] { color, unknown, "Unk" };
                            case 5:
                                return new string[] { color, unknown, "Unk" };
                            case 6:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;
                    case 605:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, unknown, "Brightness*" };
                            case 1:
                                return new string[] { scalar, unknown, "Brightness*" };
                            case 2:
                                return new string[] { scalar, unknown, "Unk" };
                            case 3:
                                return new string[] { color, unknown, "Unk" };
                            case 4:
                                return new string[] { color, unknown, "Unk" };
                            case 5:
                                return new string[] { color, unknown, "Unk" };
                            case 6:
                                return new string[] { scalar, unknown, "Unk" };
                            case 7:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;
                    case 606:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, unknown, "Unk*" };
                            case 1:
                                return new string[] { scalar, unknown, "Unk*" };
                            case 2:
                                return new string[] { scalar, unknown, "Unk" };
                            case 3:
                                return new string[] { color, unknown, "Unk" };
                            case 4:
                                return new string[] { color, unknown, "Unk" };
                            case 5:
                                return new string[] { color, unknown, "Unk" };
                            case 6:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;
                    case 607:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, unknown, "Unk*" };
                            case 1:
                                return new string[] { scalar, unknown, "Unk*" };
                            case 2:
                                return new string[] { scalar, unknown, "Unk" };
                            case 3:
                                return new string[] { color, unknown, "Unk" };
                            case 4:
                                return new string[] { color, unknown, "Unk" };
                            case 5:
                                return new string[] { color, unknown, "Unk" };
                            case 6:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;
                    case 609:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, unknown, "Unk" };
                            case 1:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;
                    case 10012:
                        switch (localPropertyIndex)
                        {
                            case 0:
                                return new string[] { scalar, unknown, "Brightness*" };
                            case 1:
                                return new string[] { scalar, unknown, "Brightness*" };
                            case 2:
                                return new string[] { scalar, unknown, "Unk" };
                            case 3:
                                return new string[] { color, unknown, "Unk" };
                            case 4:
                                return new string[] { color, unknown, "Unk" };
                            case 5:
                                return new string[] { color, unknown, "Unk" };
                            case 6:
                                return new string[] { scalar, unknown, "Unk" };
                        }
                        break;
                }
            }
            return new string[] { "[u]", "[u]", "Unk" };
        }
        private static string AxByToName(XmlNode FFXProperty)
        {
            string localAxBy = $"A{FFXProperty.Attributes[0].Value}B{FFXProperty.Attributes[1].Value}";
            string outputName;
            switch (localAxBy)
            {
                case "A0B0":
                    outputName = "Static 0";
                    break;
                case "A16B4":
                    outputName = "Static 1";
                    break;
                case "A19B7":
                    outputName = "Static Opaque White";
                    break;
                case "A32B8":
                    outputName = "Static Input";
                    break;
                case "A35B11":
                    outputName = "Static Input";
                    break;
                case "A64B16":
                    outputName = "Linear Interpolation";
                    break;
                case "A67B19":
                    outputName = "Linear Interpolation";
                    break;
                case "A96B24":
                    outputName = "Curve interpolation";
                    break;
                case "A99B27":
                    outputName = "Curve interpolation";
                    break;
                case "A4160B32":
                    outputName = "Loop Linear Interpolation";
                    break;
                case "A4163B35":
                    outputName = "Loop Linear Interpolation";
                    break;
                default:
                    outputName = "NoNameHandler";
                    break;
            }
            return outputName;
        }
        //FFXPropertyHandler Functions Below here
        public static void FFXPropertyA35B11StaticColor(XmlNodeList NodeListEditor)
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
        public static void FFXPropertyA67B19ColorInterpolationLinear(XmlNodeList NodeListEditor)
        {

            int Pos = 0;
            int StopsCount = Int32.Parse(NodeListEditor.Item(0).Attributes[1].Value);

            //NodeListEditor.Item(0).ParentNode.RemoveAll();
            Pos += 9;
            if (ImGui.TreeNodeEx($"Color Stages: Total number of stages = {StopsCount}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGuiAddons.ButtonGradient("Decrease Stops Count") & StopsCount > 2)
                {
                    int LocalPos = 8;
                    for (int i = 0; i != 4; i++)
                    {
                        NodeListEditor.Item(0).ParentNode.RemoveChild(NodeListEditor.Item((LocalPos + StopsCount + 1) + 8 + (4 * (StopsCount - 3))));
                    }
                    NodeListEditor.Item(0).ParentNode.RemoveChild(NodeListEditor.Item(LocalPos + StopsCount));
                    NodeListEditor.Item(0).Attributes[1].Value = (StopsCount - 1).ToString();
                    StopsCount--;
                }
                ImGui.SameLine();
                if (ImGuiAddons.ButtonGradient("Increase Stops Count") & StopsCount < 8)
                {
                    int LocalPos = 8;
                    XmlNode newElem = xDoc.CreateNode("element", "FFXField", "");
                    XmlAttribute Att = xDoc.CreateAttribute("xsi:type", "http://www.w3.org/2001/XMLSchema-instance");
                    XmlAttribute Att2 = xDoc.CreateAttribute("Value");
                    Att.Value = "FFXFieldFloat";
                    Att2.Value = "0";
                    newElem.Attributes.Append(Att);
                    newElem.Attributes.Append(Att2);
                    NodeListEditor.Item(0).ParentNode.InsertAfter(newElem, NodeListEditor.Item(LocalPos + StopsCount));
                    for (int i = 0; i != 4; i++) //append 4 nodes at the end of the childnodes list
                    {
                        XmlNode loopNewElem = xDoc.CreateNode("element", "FFXField", "");
                        XmlAttribute loopAtt = xDoc.CreateAttribute("xsi:type", "http://www.w3.org/2001/XMLSchema-instance");
                        XmlAttribute loopAtt2 = xDoc.CreateAttribute("Value");
                        loopAtt.Value = "FFXFieldFloat";
                        loopAtt2.Value = "0";
                        loopNewElem.Attributes.Append(loopAtt);
                        loopNewElem.Attributes.Append(loopAtt2);
                        NodeListEditor.Item(0).ParentNode.AppendChild(loopNewElem);
                    }
                    NodeListEditor.Item(0).Attributes[1].Value = (StopsCount + 1).ToString();
                    StopsCount++;
                }
                int LocalColorOffset = Pos + 1;
                for (int i = 0; i != StopsCount; i++)
                {
                    ImGui.Separator();
                    ImGui.NewLine();
                    { // Slider Stuff
                        float localSlider = float.Parse(NodeListEditor.Item(i + 9).Attributes[1].Value);
                        ImGui.BulletText($"Stage {i + 1}: Position in time");
                        if (ImGui.SliderFloat($"###Stage{i + 1}Slider", ref localSlider, 0.0f, 2.0f))
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
        public static void FFXPropertyA99B27ColorInterpolationWithCustomCurve(XmlNodeList NodeListEditor)
        {
            int Pos = 0;
            int StopsCount = Int32.Parse(NodeListEditor.Item(0).Attributes[1].Value);
            Pos += 9;

            if (ImGui.TreeNodeEx($"Color Stages: Total number of stages = {StopsCount}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGuiAddons.ButtonGradient("Decrease Stops Count") & StopsCount > 2)
                {
                    int LocalPos = 8;
                    for (int i = 0; i != 4; i++)
                    {
                        NodeListEditor.Item(0).ParentNode.RemoveChild(NodeListEditor.Item((LocalPos + StopsCount + 1) + 8 + (4 * (StopsCount - 3))));
                    }
                    for (int i = 0; i != 8; i++)
                    {
                        NodeListEditor.Item(0).ParentNode.RemoveChild(NodeListEditor.Item(NodeListEditor.Count - 1));
                    }
                    NodeListEditor.Item(0).ParentNode.RemoveChild(NodeListEditor.Item(LocalPos + StopsCount));
                    NodeListEditor.Item(0).Attributes[1].Value = (StopsCount - 1).ToString();
                    StopsCount--;
                }
                ImGui.SameLine();
                if (ImGuiAddons.ButtonGradient("Increase Stops Count") & StopsCount < 8)
                {
                    int LocalPos = 8;
                    XmlNode newElem = xDoc.CreateNode("element", "FFXField", "");
                    XmlAttribute Att = xDoc.CreateAttribute("xsi:type", "http://www.w3.org/2001/XMLSchema-instance");
                    XmlAttribute Att2 = xDoc.CreateAttribute("Value");
                    Att.Value = "FFXFieldFloat";
                    Att2.Value = "0";
                    newElem.Attributes.Append(Att);
                    newElem.Attributes.Append(Att2);
                    NodeListEditor.Item(0).ParentNode.InsertAfter(newElem, NodeListEditor.Item(LocalPos + StopsCount));
                    for (int i = 0; i != 4; i++) //append 4 fields after last color alpha
                    {
                        XmlNode loopNewElem = xDoc.CreateNode("element", "FFXField", "");
                        XmlAttribute loopAtt = xDoc.CreateAttribute("xsi:type", "http://www.w3.org/2001/XMLSchema-instance");
                        XmlAttribute loopAtt2 = xDoc.CreateAttribute("Value");
                        loopAtt.Value = "FFXFieldFloat";
                        loopAtt2.Value = "0";
                        loopNewElem.Attributes.Append(loopAtt);
                        loopNewElem.Attributes.Append(loopAtt2);
                        NodeListEditor.Item(0).ParentNode.InsertAfter(loopNewElem, NodeListEditor.Item((LocalPos + StopsCount + 1) + 8 + 4 + (4 * (StopsCount - 3))));
                        for (int i2 = 0; i2 != 2; i2++)
                        {
                            XmlNode loop1NewElem = xDoc.CreateNode("element", "FFXField", "");
                            XmlAttribute loop1Att = xDoc.CreateAttribute("xsi:type", "http://www.w3.org/2001/XMLSchema-instance");
                            XmlAttribute loop1Att2 = xDoc.CreateAttribute("Value");
                            loop1Att.Value = "FFXFieldFloat";
                            loop1Att2.Value = "0";
                            loop1NewElem.Attributes.Append(loop1Att);
                            loop1NewElem.Attributes.Append(loop1Att2);
                            NodeListEditor.Item(0).ParentNode.AppendChild(loop1NewElem);
                        }
                    }
                    NodeListEditor.Item(0).Attributes[1].Value = (StopsCount + 1).ToString();
                    StopsCount++;
                }
                int LocalColorOffset = Pos + 1;
                for (int i = 0; i != StopsCount; i++)
                {
                    ImGui.Separator();
                    ImGui.NewLine();
                    { // Slider Stuff
                        float localSlider = float.Parse(NodeListEditor.Item(i + 9).Attributes[1].Value);
                        ImGui.BulletText($"Stage {i + 1}: Position in time");
                        if (ImGui.SliderFloat($"###Stage{i + 1}Slider", ref localSlider, 0.0f, 2.0f))
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

                    { // Slider Stuff for curvature
                        int LocalPos = 8;
                        int readpos = (LocalPos + StopsCount + 1) + 8 + 4 + (4 * (StopsCount - 3));
                        int localproperfieldpos = readpos + (i * 8);
                        if (ImGui.TreeNodeEx($"Custom Curve Settngs###{i + 1}CurveSettings"))
                        {
                            if (ImGui.TreeNodeEx("Red: Curve Points", ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                ImGui.Indent();
                                {
                                    int localint = 0;
                                    float localSlider = float.Parse(NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value);
                                    ImGui.Text("Curve Point 0 = ");
                                    ImGui.SameLine();
                                    if (ImGui.SliderFloat($"###Curve{localint}Stage{i + 1}FloatInput", ref localSlider, 0.0f, 2.0f))
                                    {
                                        NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value = localSlider.ToString();
                                    }
                                }
                                {
                                    int localint = 1;
                                    float localSlider = float.Parse(NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value);
                                    ImGui.Text("Curve Point 1 = ");
                                    ImGui.SameLine();
                                    if (ImGui.SliderFloat($"###Curve{localint}Stage{i + 1}FloatInput", ref localSlider, 0.0f, 2.0f))
                                    {
                                        NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value = localSlider.ToString();
                                    }
                                }
                                ImGui.Unindent();
                                ImGui.TreePop();
                            }

                            if (ImGui.TreeNodeEx("Green: Curve Points", ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                ImGui.Indent();
                                {
                                    int localint = 2;
                                    float localSlider = float.Parse(NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value);
                                    ImGui.Text("Curve Point 0 = ");
                                    ImGui.SameLine();
                                    if (ImGui.SliderFloat($"###Curve{localint}Stage{i + 1}FloatInput", ref localSlider, 0.0f, 2.0f))
                                    {
                                        NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value = localSlider.ToString();
                                    }
                                }
                                {
                                    int localint = 3;
                                    float localSlider = float.Parse(NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value);
                                    ImGui.Text("Curve Point 1 = ");
                                    ImGui.SameLine();
                                    if (ImGui.SliderFloat($"###Curve{localint}Stage{i + 1}FloatInput", ref localSlider, 0.0f, 2.0f))
                                    {
                                        NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value = localSlider.ToString();
                                    }
                                }
                                ImGui.Unindent();
                                ImGui.TreePop();
                            }

                            if (ImGui.TreeNodeEx("Blue: Curve Points", ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                ImGui.Indent();
                                {
                                    int localint = 4;
                                    float localSlider = float.Parse(NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value);
                                    ImGui.Text("Curve Point 0 = ");
                                    ImGui.SameLine();
                                    if (ImGui.SliderFloat($"###Curve{localint}Stage{i + 1}FloatInput", ref localSlider, 0.0f, 2.0f))
                                    {
                                        NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value = localSlider.ToString();
                                    }
                                }
                                {
                                    int localint = 5;
                                    float localSlider = float.Parse(NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value);
                                    ImGui.Text("Curve Point 1 = ");
                                    ImGui.SameLine();
                                    if (ImGui.SliderFloat($"###Curve{localint}Stage{i + 1}FloatInput", ref localSlider, 0.0f, 2.0f))
                                    {
                                        NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value = localSlider.ToString();
                                    }
                                }
                                ImGui.Unindent();
                                ImGui.TreePop();
                            }

                            if (ImGui.TreeNodeEx("Alpha: Curve Points", ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                ImGui.Indent();
                                {
                                    int localint = 6;
                                    float localSlider = float.Parse(NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value);
                                    ImGui.Text("Curve Point 0 = ");
                                    ImGui.SameLine();
                                    if (ImGui.SliderFloat($"###Curve{localint}Stage{i + 1}FloatInput", ref localSlider, 0.0f, 2.0f))
                                    {
                                        NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value = localSlider.ToString();
                                    }
                                }

                                {
                                    int localint = 7;
                                    float localSlider = float.Parse(NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value);
                                    ImGui.Text("Curve Point 0 = ");
                                    ImGui.SameLine();
                                    if (ImGui.SliderFloat($"###Curve{localint}Stage{i + 1}FloatInput", ref localSlider, 0.0f, 2.0f))
                                    {
                                        NodeListEditor.Item(localproperfieldpos + localint).Attributes[1].Value = localSlider.ToString();
                                    }
                                }
                                ImGui.Unindent();
                                ImGui.TreePop();
                            }
                            ImGui.TreePop();
                        }
                    }

                    ImGui.NewLine();
                }
                ImGui.Separator();
                ImGui.TreePop();
            }
        }
    }
}
