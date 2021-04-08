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
using System.IO;
using System.Linq;

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
        private static bool _isStripXml = false;

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
        private static bool _axbyDebugger = false;

        //FFX Workshop Tools
        //<Color Editor>
        public static bool _cPickerIsEnable = false;

        public static XmlNode _cPickerRed;

        public static XmlNode _cPickerGreen;

        public static XmlNode _cPickerBlue;

        public static XmlNode _cPickerAlpha;

        public static Vector4 _cPicker = new Vector4();

        public static float _colorOverload = 1.0f;
        // Color Editor

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
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.NavEnableKeyboard;

            DSFFXThemes.ThemesSelector(_activeTheme); //Default Theme
            DefParser.Initialize();
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
                                if (XMLOpen)
                                    CloseOpenFFXWithoutSaving();
                                _loadedFilePath = ofd.FileName;
                                XMLOpen = true;
                                if (_isStripXml)
                                {
                                    FileStream file = File.OpenRead(ofd.FileName);
                                    XmlReaderSettings settings = new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = true };
                                    XmlReader xmlReader = XmlReader.Create(file, settings);
                                    xDoc.Load(xmlReader);
                                }
                                else
                                {
                                    xDoc.Load(ofd.FileName);
                                }
                            }
                        }
                        if (_loadedFilePath != "" & XMLOpen)
                        {
                            if (ImGui.MenuItem("Save Open FFX *XML"))
                            {
                                xDoc.Save(_loadedFilePath);
                            }
                        }
                        else
                        {
                            ImGui.TextColored(new Vector4(0.5f,0.5f,0.5f,1f), "Save Open FFX *XML");
                        }
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("Themes"))
                    {
                        if (ImGui.Combo("Theme Selector", ref _themeSelectorSelectedItem, _themeSelectorEntriesArray, _themeSelectorEntriesArray.Length))
                        {
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
                        }
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu("Useful Info"))
                    {
                        // Strip Xml Start
                        ImGui.Text("Strip XML Comments");
                        ImGui.SameLine();
                        if (ImGuiAddons.ToggleButton("Strip XML", ref _isStripXml) & XMLOpen)
                        {
                            CloseOpenFFXWithoutSaving();
                        }
                        ImGui.SameLine();
                        ShowToolTipSimple("", "Strip XML ToolTip:", "If the program is crashing upon trying to edit a property, consider enabling this.\nIt will strip invalid elements from the XML, including comments and whitespaces.\n\nWarning: Enabling/Disabling this will close the open FFX without saving.\n\nWarning 2: This option is likely obsolete, comments should not count as valid nodes anymore. I will leave it here as legacy in order to ensure it is there if needed.",true,ImGuiPopupFlags.MouseButtonRight);
                        // Strip Xml End

                        // Keybord Interactions Start
                        ImGui.Text("Keyboard Interactions Guide");
                        ImGui.SameLine();
                        ImGuiAddons.ToggleButton("Keyboard InteractionsToggle", ref _keyboardInputGuide);
                        // Keybord Interactions End

                        // AxBy Debugger Start
                        ImGui.Text("AxBy Debugger");
                        ImGui.SameLine();
                        ImGuiAddons.ToggleButton("AxByDebugger", ref _axbyDebugger);
                        // AxBy Debugger End

                        // No Action ID Filter Start
                        ImGui.Text("No ActionID Filter");
                        ImGui.SameLine();
                        ImGuiAddons.ToggleButton("No ActionID Filter", ref _filtertoggle);
                        // No Action ID Filter End

                        ImGui.EndMenu();
                    }
                    ImGui.EndMainMenuBar();
                }
                ImGui.DockSpace(MainViewport, new Vector2(0, 0));
                ImGui.End();
            }

            { //Declare Standalone Windows here
                // Color Picker
                if (_cPickerIsEnable)
                {
                    float popupWidth = _window.Width * 0.7f;
                    float popupHeight = _window.Height * 0.7f;
                    ImGui.SetNextWindowDockID(MainViewport, ImGuiCond.Once);
                    if (ImGui.Begin("FFX Color Picker"))
                    {
                        if (ImGuiAddons.ButtonGradient("Close Color Picker"))
                            _cPickerIsEnable = false;
                        ImGui.SameLine();
                        if (ImGuiAddons.ButtonGradient("Commit Color Change"))
                        {
                            if (_cPickerRed.Attributes[0].Value == "FFXFieldInt" || _cPickerGreen.Attributes[0].Value == "FFXFieldInt" || _cPickerBlue.Attributes[0].Value == "FFXFieldInt" || _cPickerAlpha.Attributes[0].Value == "FFXFieldInt")
                            {
                                _cPickerRed.Attributes[0].Value = "FFXFieldFloat";
                                _cPickerGreen.Attributes[0].Value = "FFXFieldFloat";
                                _cPickerBlue.Attributes[0].Value = "FFXFieldFloat";
                                _cPickerAlpha.Attributes[0].Value = "FFXFieldFloat";
                            }
                            _cPickerRed.Attributes[1].Value = _cPicker.X.ToString("#.0000");
                            _cPickerGreen.Attributes[1].Value = _cPicker.Y.ToString("#.0000");
                            _cPickerBlue.Attributes[1].Value = _cPicker.Z.ToString("#.0000");
                            _cPickerAlpha.Attributes[1].Value = _cPicker.W.ToString("#.0000");
                        }
                        Vector2 mEME = ImGui.GetWindowSize();
                        if (mEME.X > mEME.Y)
                        {
                            ImGui.SetNextItemWidth(mEME.Y * 0.80f);
                        }
                        ImGui.ColorPicker4("CPicker", ref _cPicker, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoTooltip);
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
                        ImGui.End();
                    }
                }
                // Keyboard Guide
                if (_keyboardInputGuide)
                {
                    ImGui.SetNextWindowDockID(MainViewport);
                    ImGui.Begin("Keyboard Guide", ImGuiWindowFlags.MenuBar);
                    ImGui.BeginMenuBar();
                    ImGui.EndMenuBar();
                    ImGui.ShowUserGuide();
                    ImGui.End();
                }
                //Currently Unused FFXProperty Changer
                if (_axbyEditorIsPopup)
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
                ImGui.Columns(2);
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
            }
        }

        private static bool _filtertoggle = false;
        private static void PopulateTree(XmlNode root)
        {
            if (root is XmlElement)
            {
                ImGui.PushID($"TreeFunctionlayer = {root.Name} ChildIndex = {GetNodeIndexinParent(root)}");
                string[] _actionIDsFilter = { "600", "601", "602", "603", "604", "605", "606", "607", "609", "10012" };
                XmlNodeList localNodeList = XMLChildNodesValid(root);
                if (root.Attributes["ActionID"] != null)
                {
                    if (_actionIDsFilter.Contains(root.Attributes[0].Value) || _filtertoggle)
                    {
                        if (ImGui.TreeNodeEx($"ActionID = {root.Attributes[0].Value}", ImGuiTreeNodeFlags.None))
                        {
                            GetFFXProperties(root, "Properties1");
                            GetFFXProperties(root, "Properties2");
                            GetFFXFields(root, "Fields1");
                            GetFFXFields(root, "Fields2");
                            ImGui.TreePop();
                        }
                    }
                }
                else if (root.Name == "EffectAs" || root.Name == "EffectBs" || root.Name == "RootEffectCall" || root.Name == "Actions")
                {
                    if (localNodeList.Count > 0)
                    {
                        foreach (XmlNode node in localNodeList)
                        {
                            PopulateTree(node);
                        }
                    }
                }
                else if (root.Name == "FFXEffectCallA" || root.Name == "FFXEffectCallB")
                {
                    bool localLoopPass = false;
                    foreach (XmlNode node in root.SelectNodes("descendant::FFXActionCall[@ActionID]"))
                    {
                        if (_actionIDsFilter.Contains(node.Attributes[0].Value) || _filtertoggle)
                        {
                            localLoopPass = true;
                            break;
                        }
                    }
                    if (root.Name == "FFXEffectCallA" & localNodeList.Count > 0 & localLoopPass)
                    {
                        if (ImGui.TreeNodeEx($"FFX Container = {root.Attributes[0].Value}"))
                        {
                            foreach (XmlNode node in localNodeList)
                            {
                                PopulateTree(node);
                            }
                            ImGui.TreePop();
                        }
                    }
                    else if (root.Name == "FFXEffectCallB" & localNodeList.Count > 0 & localLoopPass)
                    {
                        if (ImGui.TreeNodeEx($"FFX Call"))
                        {
                            foreach (XmlNode node in localNodeList)
                            {
                                PopulateTree(node);
                            }
                            ImGui.TreePop();
                        }
                    }
                    else
                    {
                        foreach (XmlNode node in localNodeList)
                        {
                            PopulateTree(node);
                        }
                    }
                }
                else
                {
                    if (ImGui.TreeNodeEx($"{root.Name}"))
                    {
                        //DoWork(root);
                        foreach (XmlNode node in localNodeList)
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

        public static int GetNodeIndexinParent(XmlNode Node)
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
        public static string[] Fields;
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
                //ImGui.PushItemWidth(ImGui.GetColumnWidth() * 0.4f);
                DefParser.DefXMLParser(NodeListEditor, Fields[1], Fields[0]);
                //ImGui.PopItemWidth();
            }
            ImGui.EndChild();
            //
            if (_axbyDebugger)
            {
                ImGui.SetNextWindowDockID(MainViewport);
                ImGui.Begin("axbxDebug");
                int integer = 0;
                foreach (XmlNode node in XMLChildNodesValid(NodeListEditor.Item(0).ParentNode))
                {
                    ImGui.Text($"TempID = '{integer}' XMLElementName = '{node.LocalName}' AttributesNum = '{node.Attributes.Count}' Attributes({node.Attributes[0].Name} = '{node.Attributes[0].Value}', {node.Attributes[1].Name} = '{float.Parse(node.Attributes[1].Value)}')");
                    integer++;
                }
                ImGui.End();
            }
        }
        private static void GetFFXFields(XmlNode root, string fieldType)
        {
            XmlNodeList NodeListProcessing = XMLChildNodesValid(root.SelectNodes($"descendant::{fieldType}")[0]);
            if (NodeListProcessing.Count > 0)
            {
                uint IDStorage = ImGui.GetID(fieldType);
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
                ImGui.TreeNodeEx($"{fieldType}", localTreeNodeFlags);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left) & !selected)
                {
                    treeViewCurrentHighlighted = IDStorage;
                    storage.SetBool(IDStorage, true);
                    NodeListEditor = NodeListProcessing;
                    Fields = new string[] { fieldType, root.Attributes[0].Value };
                    _showFFXEditorProperties = false;
                    _showFFXEditorFields = true;
                }
            }
        }
        private static uint treeViewCurrentHighlighted = 0;
        public static void ShowToolTipWiki(string toolTipTitle, string[] localSlot)
        {
            string fullToolTip = "";
            string archetypeWiki = DefParser.DefXMLSymbolParser(localSlot[0]);
            string argumentsWiki = DefParser.DefXMLSymbolParser(localSlot[1]);
            if (localSlot.Length >= 4)
            {
                if (localSlot[3] != null)
                {
                    fullToolTip += $"FFX Property Slot ToolTip:\n{localSlot[3]}\n\n";
                }
            }
            fullToolTip += $"Type = {localSlot[0]}: {archetypeWiki}.\n\n";
            fullToolTip += $"Arg = {localSlot[1]}: {argumentsWiki}.";
            ShowToolTipSimple("", toolTipTitle, fullToolTip, false, ImGuiPopupFlags.MouseButtonRight);
        }
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
                            string[] localSlot = DefParser.GetDefPropertiesArray(Node, PropertyType);
                            string localInput = AxByToName(Node);
                            string localLabel = $"{localIndex} {localSlot[0]}: {localSlot[1]} {localInput}";
                            ImGui.PushID($"ItemForLoopNode = {localLabel}");
                            if (localAxBy == "A67B19" || localAxBy == "A35B11" || localAxBy == "A99B27" || (Node.Attributes[0].Value == "A4163B35"))
                            {
                                XmlNodeList NodeListProcessing = XMLChildNodesValid(Node.SelectNodes("Fields")[0]);
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
                                ShowToolTipWiki("Wiki", localSlot);
                                ImGui.TableNextColumn();
                                ImGui.Text(localSlot[1]);
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
                                Vector2 cursorPos = ImGui.GetCursorPos();
                                ImGui.Indent();
                                ImGui.Text(localSlot[0]);
                                ImGui.Unindent();
                                ImGui.SetCursorPos(cursorPos);
                                ImGui.Selectable($"###{localLabel}", false, ImGuiSelectableFlags.SpanAllColumns);
                                ShowToolTipWiki("Wiki", localSlot);
                                ImGui.TableNextColumn();
                                ImGui.Text(localSlot[1]);
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
                        ImGui.BulletText($"Stage {i + 1}: Position in time");
                        FloatSliderDefaultNode(NodeListEditor.Item(i + 9), $"###Stage{i + 1}Slider", 0.0f, 2.0f);
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
                        ImGui.BulletText($"Stage {i + 1}: Position in time");
                        FloatSliderDefaultNode(NodeListEditor.Item(i + 9), $"###Stage{i + 1}Slider", 0.0f, 2.0f);
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
                                    ImGui.Text("Curve Point 0 = ");
                                    ImGui.SameLine();
                                    FloatSliderDefaultNode(NodeListEditor.Item(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                {
                                    int localint = 1;
                                    ImGui.Text("Curve Point 1 = ");
                                    ImGui.SameLine();
                                    FloatSliderDefaultNode(NodeListEditor.Item(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                ImGui.Unindent();
                                ImGui.TreePop();
                            }

                            if (ImGui.TreeNodeEx("Green: Curve Points", ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                ImGui.Indent();
                                {
                                    int localint = 2;
                                    ImGui.Text("Curve Point 0 = ");
                                    ImGui.SameLine();
                                    FloatSliderDefaultNode(NodeListEditor.Item(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                {
                                    int localint = 3;
                                    ImGui.Text("Curve Point 1 = ");
                                    ImGui.SameLine();
                                    FloatSliderDefaultNode(NodeListEditor.Item(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                ImGui.Unindent();
                                ImGui.TreePop();
                            }

                            if (ImGui.TreeNodeEx("Blue: Curve Points", ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                ImGui.Indent();
                                {
                                    int localint = 4;
                                    ImGui.Text("Curve Point 0 = ");
                                    ImGui.SameLine();
                                    FloatSliderDefaultNode(NodeListEditor.Item(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                {
                                    int localint = 5;
                                    ImGui.Text("Curve Point 1 = ");
                                    ImGui.SameLine();
                                    FloatSliderDefaultNode(NodeListEditor.Item(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                ImGui.Unindent();
                                ImGui.TreePop();
                            }

                            if (ImGui.TreeNodeEx("Alpha: Curve Points", ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                ImGui.Indent();
                                {
                                    int localint = 6;
                                    ImGui.Text("Curve Point 0 = ");
                                    ImGui.SameLine();
                                    FloatSliderDefaultNode(NodeListEditor.Item(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }

                                {
                                    int localint = 7;
                                    ImGui.Text("Curve Point 0 = ");
                                    ImGui.SameLine();
                                    FloatSliderDefaultNode(NodeListEditor.Item(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
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

        public static void IntInputDefaultNode(XmlNode node, string dataString)
        {
            string nodeValue = node.Attributes[1].Value;
            if (ImGui.InputText(dataString, ref nodeValue, 10, ImGuiInputTextFlags.CharsDecimal))
            {
                int intNodeValue;
                if (Int32.TryParse(nodeValue, out intNodeValue))
                {
                    if (node.Attributes[0].Value == "FFXFieldFloat")
                        node.Attributes[0].Value = "FFXFieldInt";
                    node.Attributes[1].Value = intNodeValue.ToString();
                }
            }
        }
        public static void FloatSliderDefaultNode(XmlNode node, string dataString, float minimumValue, float maximumValue)
        {
            float nodeValue = float.Parse(node.Attributes[1].Value);
            if (ImGui.SliderFloat(dataString, ref nodeValue, minimumValue, maximumValue))
            {
                if (node.Attributes[0].Value == "FFXFieldInt")
                    node.Attributes[0].Value = "FFXFieldFloat";
                node.Attributes[1].Value = nodeValue.ToString("#.0000");
            }
        }
        public static void FloatInputDefaultNode(XmlNode node, string dataString)
        {
            string nodeValue = node.Attributes[1].Value;
            if (ImGui.InputText(dataString, ref nodeValue, 16, ImGuiInputTextFlags.CharsDecimal))
            {
                float floatNodeValue;
                if (float.TryParse(nodeValue, out floatNodeValue))
                {
                    if (node.Attributes[0].Value == "FFXFieldInt")
                        node.Attributes[0].Value = "FFXFieldFloat";
                    node.Attributes[1].Value = floatNodeValue.ToString("#.0000");
                }
            }
        }
        public static void BooleanIntInputDefaultNode(XmlNode node, string dataString)
        {
            int nodeValue = Int32.Parse(node.Attributes[1].Value);
            bool nodeValueBool = false;
            if (nodeValue == 1)
                nodeValueBool = true;
            else if (nodeValue == 0)
                nodeValueBool = false;
            else
            {
                ImGui.Text("Error: Bool Invalid, current value is: " + nodeValue.ToString());
                if (ImGui.Button("Set Bool to False"))
                {
                    if (node.Attributes[0].Value == "FFXFieldFloat")
                        node.Attributes[0].Value = "FFXFieldInt";
                    node.Attributes[1].Value = 0.ToString();
                }
                return;
            }
            if (ImGui.Checkbox(dataString, ref nodeValueBool))
            {
                if (node.Attributes[0].Value == "FFXFieldFloat")
                    node.Attributes[0].Value = "FFXFieldInt";
                node.Attributes[1].Value = (nodeValueBool ? 1 : 0).ToString();
            }
        }
        public static void IntComboDefaultNode(XmlNode node, string comboTitle, string[] entriesArrayValues, string[] entriesArrayNames)
        {
            int blendModeCurrent = Int32.Parse(node.Attributes[1].Value);
            if (ImGui.Combo(comboTitle, ref blendModeCurrent, entriesArrayNames, entriesArrayNames.Length))
            {
                if (node.Attributes[0].Value == "FFXFieldFloat")
                    node.Attributes[0].Value = "FFXFieldInt";
                string tempstring = entriesArrayValues[blendModeCurrent];
                int tempint;
                if (Int32.TryParse(tempstring, out tempint))
                {
                    node.Attributes[1].Value = tempint.ToString();
                }
            }
        }
        public static void IntComboNotLinearDefaultNode(XmlNode node, string comboTitle, XmlNode EnumEntries)
        {
            string localSelectedItem;
            XmlNode CurrentNode = EnumEntries.SelectSingleNode($"descendant::*[@value={node.Attributes[1].Value}]");
            if (CurrentNode != null)
                localSelectedItem = $"{CurrentNode.Attributes["value"].Value}: {CurrentNode.Attributes["name"].Value}";
            else
                localSelectedItem = $"{node.Attributes[1].Value}: Not Enumerated";

            ArrayList localTempArray = new ArrayList();
            foreach (XmlNode node1 in XMLChildNodesValid(EnumEntries))
            {
                localTempArray.Add($"{node1.Attributes["value"].Value}: {node1.Attributes["name"].Value}");
            }
            string[] localArray = new string[localTempArray.Count];
            localTempArray.CopyTo(localArray);

            if (ImGui.BeginCombo(comboTitle, localSelectedItem))
            {
                for (int i = 0; i < localArray.Length; i++)
                {
                    if (ImGui.Selectable(localArray[i]))
                    {
                        if (node.Attributes[0].Value == "FFXFieldFloat")
                            node.Attributes[0].Value = "FFXFieldInt";
                        int safetyNetInt;
                        if (Int32.TryParse(XMLChildNodesValid(EnumEntries)[i].Attributes["value"].Value, out safetyNetInt))
                        {
                            node.Attributes[1].Value = safetyNetInt.ToString();
                        }
                    }
                }
                ImGui.EndCombo();
            }
        }
        public static void ShowToolTipSimple(string toolTipUID, string toolTipTitle, string toolTipText, bool isToolTipObjectSpawned, ImGuiPopupFlags popupTriggerCond)
        {
            string localUID = toolTipUID + toolTipTitle;
            if (isToolTipObjectSpawned)
                ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "(?)");
            ImGui.OpenPopupOnItemClick(localUID, popupTriggerCond);
            if (ImGui.IsPopupOpen(localUID))
            {
                Vector2 mousePos = ImGui.GetMousePos();
                Vector2 localTextSize = ImGui.CalcTextSize(toolTipText);
                float maxToolTipWidth = (float)_window.Width * 0.4f;
                float windowWidth;
                float windowHeight;
                Vector2 windowSize = new Vector2(maxToolTipWidth, localTextSize.Y);
                if (mousePos.X > (float)(_window.Width / 2))
                    windowWidth = mousePos.X - maxToolTipWidth;
                else
                    windowWidth = mousePos.X;
                ImGui.SetNextWindowPos(new Vector2(windowWidth, mousePos.Y), ImGuiCond.Appearing);
                ImGui.SetNextWindowSize(windowSize, ImGuiCond.Appearing);
                if (ImGui.BeginPopupContextItem(localUID))
                {
                    ImGui.Text(toolTipTitle);
                    ImGui.NewLine();
                    ImGui.TextWrapped(toolTipText);
                    ImGui.EndPopup();
                }
            }
        }

        private static void CloseOpenFFXWithoutSaving() 
        {
            XMLOpen = false;
            _cPickerIsEnable = false;
            _showFFXEditorFields = false;
            _showFFXEditorProperties = false;
            xDoc = new XmlDocument();
        }

        public static XmlNodeList XMLChildNodesValid(XmlNode Node) 
        {
            return Node.SelectNodes("*");
        }
    }
}
