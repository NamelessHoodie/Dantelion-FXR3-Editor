using System;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using ImGuiNET;
using ImPlotNET;
using imnodesNET;
using ImGuizmoNET;
using System.Xml.Linq;
using System.Collections;
using ImGuiNETAddons;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Threading;

namespace DSFFXEditor
{
    class DSFFXGUIMain
    {
        private static Sdl2Window _window;
        private static GraphicsDevice _gd;
        private static CommandList _cl;
        private static ImGuiController _controller;

        // UI state
        private static Vector3 _clearColor = new Vector3(0.45f, 0.55f, 0.6f);
        private static uint MainViewport;
        private static bool _keyboardInputGuide = false;

        // Save/Load Path
        private static String _loadedFilePath = "";

        //colorpicka
        private static Vector3 _CPickerColor = new Vector3(0, 0, 0);

        //checkbox

        static bool[] s_opened = { true, true, true, true }; // Persistent user state

        //Theme Selector
        readonly static String[] _themeSelectorEntriesArray = { "Red Clay", "ImGui Dark", "ImGui Light", "ImGui Classic" };

        //XML
        private static XDocument xDocLinq;
        private static bool XMLOpen = false;
        private static bool _axbyDebugger = false;
        private static bool experimentalLinqXmlReader = false;
        private static bool _filtertoggle = false;
        public static bool _showFFXEditorFields = false;
        public static bool _showFFXEditorProperties = false;
        private static uint treeViewCurrentHighlighted = 0;
        public static IEnumerable<XElement> NodeListEditor;
        public static string AxBy;
        public static string[] Fields;
        private static XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

        //FFX Workshop Tools
        //<Color Editor>
        public static bool _cPickerIsEnable = false;

        public static XElement _cPickerRed;
        public static XElement _cPickerGreen;
        public static XElement _cPickerBlue;
        public static XElement _cPickerAlpha;
        public static Vector4 _cPicker = new Vector4();
        public static float _colorOverload = 1.0f;
        // Color Editor

        [STAThread]
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            // Create window, GraphicsDevice, and all resources necessary for the demo.
            VeldridStartup.CreateWindowAndGraphicsDevice(new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "Dark Souls FFX Editor"),
                new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
                GraphicsBackend.Direct3D11,
                out _window,
                out _gd);
            _window.Resized += () =>
            {
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _controller.WindowResized(_window.Width, _window.Height);
            };
            _cl = _gd.ResourceFactory.CreateCommandList();
            _controller = new ImGuiController(_gd, _window, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);

            //Runtime Configs Load
            DSFFXConfig.ReadConfigs();

            //Theme Selector
            DSFFXThemes.ThemesSelector(DSFFXConfig._activeTheme);

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

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
                _controller.SwapExtraWindows(_gd);
                Thread.Sleep(16);
            }
            //Runtime Configs Save
            DSFFXConfig.SaveConfigs();

            // Clean up Veldrid resources
            _gd.WaitForIdle();
            _controller.Dispose();
            _cl.Dispose();
            _gd.Dispose();
        }
        static void SetThing(out float i, float val) { i = val; }
        private static unsafe void SubmitUI()
        {
            ImGuiViewport* viewport = ImGui.GetMainViewport();
            MainViewport = ImGui.DockSpaceOverViewport(viewport);

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
                            xDocLinq = XDocument.Load(ofd.FileName);

                        }
                    }
                    if (_loadedFilePath != "" & XMLOpen)
                    {
                        if (ImGui.MenuItem("Save Open FFX *XML"))
                        {
                            xDocLinq.Save(_loadedFilePath);
                        }
                    }
                    else
                    {
                        ImGui.TextDisabled("Save Open FFX *XML");
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Themes"))
                {
                    if (ImGui.Combo("Theme Selector", ref DSFFXConfig._themeSelectorSelectedItem, _themeSelectorEntriesArray, _themeSelectorEntriesArray.Length))
                    {
                        switch (DSFFXConfig._themeSelectorSelectedItem)
                        {
                            case 0:
                                DSFFXConfig._activeTheme = "DarkRedClay";
                                break;
                            case 1:
                                DSFFXConfig._activeTheme = "ImGuiDark";
                                break;
                            case 2:
                                DSFFXConfig._activeTheme = "ImGuiLight";
                                break;
                            case 3:
                                DSFFXConfig._activeTheme = "ImGuiClassic";
                                break;
                            default:
                                break;
                        }
                        DSFFXThemes.ThemesSelector(DSFFXConfig._activeTheme);
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Useful Info"))
                {
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

            { //Main Window Here
                ImGui.SetNextWindowDockID(MainViewport, ImGuiCond.Appearing);
                ImGui.Begin("FFXEditor", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
                ImGui.Columns(2);
                ImGui.BeginChild("FFXTreeView");
                if (XMLOpen)
                {
                    foreach (XElement xElement in xDocLinq.Descendants("RootEffectCall"))
                    {
                        PopulateTree(xElement);
                    }
                }
                ImGui.EndChild();
                if (_showFFXEditorProperties || _showFFXEditorFields)
                {
                    ImGui.NextColumn();
                    FFXEditor();
                }
            }
            { //Declare Standalone Windows here
                // Color Picker
                if (_cPickerIsEnable)
                {
                    ImGui.SetNextWindowDockID(MainViewport, ImGuiCond.FirstUseEver);
                    if (ImGui.Begin("FFX Color Picker", ref _cPickerIsEnable))
                    {
                        if (ImGuiAddons.ButtonGradient("Commit Color Change"))
                        {
                            if (_cPickerRed.Attribute(xsi + "type").Value == "FFXFieldInt" || _cPickerGreen.Attribute(xsi + "type").Value == "FFXFieldInt" || _cPickerBlue.Attribute(xsi + "type").Value == "FFXFieldInt" || _cPickerAlpha.Attribute(xsi + "type").Value == "FFXFieldInt")
                            {
                                _cPickerRed.Attribute(xsi + "type").Value = "FFXFieldFloat";
                                _cPickerGreen.Attribute(xsi + "type").Value = "FFXFieldFloat";
                                _cPickerBlue.Attribute(xsi + "type").Value = "FFXFieldFloat";
                                _cPickerAlpha.Attribute(xsi + "type").Value = "FFXFieldFloat";
                            }
                            _cPickerRed.Attribute("Value").Value = _cPicker.X.ToString("#.0000");
                            _cPickerGreen.Attribute("Value").Value = _cPicker.Y.ToString("#.0000");
                            _cPickerBlue.Attribute("Value").Value = _cPicker.Z.ToString("#.0000");
                            _cPickerAlpha.Attribute("Value").Value = _cPicker.W.ToString("#.0000");
                        }
                        Vector2 mEME = ImGui.GetWindowSize();
                        if (mEME.X > mEME.Y)
                        {
                            ImGui.SetNextItemWidth(mEME.Y * 0.80f);
                        }
                        ImGui.ColorPicker4("CPicker", ref _cPicker, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoTooltip);
                        ImGui.Separator();
                        ImGui.Text("Brightness Multiplier");
                        ImGui.SliderFloat("###Brightness Multiplier", ref _colorOverload, -10f, 10f);
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
                    ImGui.SetNextWindowDockID(MainViewport, ImGuiCond.FirstUseEver);
                    ImGui.Begin("Keyboard Guide", ref _keyboardInputGuide, ImGuiWindowFlags.MenuBar);
                    ImGui.BeginMenuBar();
                    ImGui.EndMenuBar();
                    ImGui.ShowUserGuide();
                    ImGui.End();
                }
                if (experimentalLinqXmlReader)
                {
                    string[] aaa = new string[] { "609", "603" };
                    ImGui.SetNextWindowDockID(MainViewport, ImGuiCond.Appearing);
                    ImGui.Begin("urmom");
                    IEnumerable<XElement> enumerablememe = xDocLinq.Descendants();
                    var memeidk = from memeitem in enumerablememe
                                  where memeitem.Name == "FFXActionCall"
                                  where aaa.Contains(memeitem.Attribute("ActionID").Value)
                                  select memeitem;
                    foreach (var meme in memeidk)
                    {
                        ImGui.Text(meme.ToString());
                    }
                    ImGui.End();
                }
            }

            ImGuiIOPtr io = ImGui.GetIO();
            SetThing(out io.DeltaTime, 2f);
        }
        private static void PopulateTree(XElement root)
        {
            if (root != null)
            {
                ImGui.PushID($"TreeFunctionlayer = {root.Name} ChildIndex = {GetNodeIndexinParent(root)}");
                string[] _actionIDsFilter = { "600", "601", "602", "603", "604", "605", "606", "607", "609", "10012" };
                IEnumerable<XElement> localNodeList = XMLChildNodesValid(root);
                if (root.Attribute(XName.Get("ActionID")) != null)
                {
                    if (_actionIDsFilter.Contains(root.Attribute("ActionID").Value) || _filtertoggle)
                    {
                        if (ImGui.TreeNodeEx($"ActionID = {root.Attribute("ActionID").Value}", ImGuiTreeNodeFlags.None))
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
                    foreach (XElement node in localNodeList)
                    {
                        PopulateTree(node);
                    }
                }
                else if (root.Name == "FFXEffectCallA" || root.Name == "FFXEffectCallB")
                {
                    IEnumerable<XElement> tempnode = from node in root.Descendants()
                                                     where node.Name == "FFXActionCall" & node.Attribute("ActionID") != null
                                                     where _filtertoggle || _actionIDsFilter.Contains(node.Attribute("ActionID").Value)
                                                     select node;
                    if (tempnode.Count() > 0)
                    {
                        if (root.Name == "FFXEffectCallA")
                        {
                            if (ImGui.TreeNodeEx($"FFX Container = {root.Attribute("EffectID").Value}"))
                            {
                                foreach (XElement node in localNodeList)
                                {
                                    PopulateTree(node);
                                }
                                ImGui.TreePop();
                            }
                        }
                        else if (root.Name == "FFXEffectCallB")
                        {
                            if (ImGui.TreeNodeEx($"FFX Call"))
                            {
                                foreach (XElement node in localNodeList)
                                {
                                    PopulateTree(node);
                                }
                                ImGui.TreePop();
                            }
                        }
                    }
                    else
                    {
                        foreach (XElement node in localNodeList)
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
                        foreach (XElement node in localNodeList)
                        {
                            PopulateTree(node);
                        }
                        ImGui.TreePop();
                    }
                }
                ImGui.PopID();
            }
        }
        public static IEnumerable<XElement> XMLChildNodesValid(XElement Node)
        {

            return from element in Node.Elements()
                   where element.NodeType == XmlNodeType.Element
                   select element;
        }
        public static int GetNodeIndexinParent(XElement Node)
        {
            return Node.ElementsBeforeSelf().Where(n => n.NodeType == XmlNodeType.Element).Count();
        }
        private static void CloseOpenFFXWithoutSaving()
        {
            XMLOpen = false;
            _cPickerIsEnable = false;
            _showFFXEditorFields = false;
            _showFFXEditorProperties = false;
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
        private static void GetFFXProperties(XElement root, string PropertyType)
        {
            IEnumerable<XElement> localNodeList = from element0 in root.Elements(PropertyType)
                                                  from element1 in element0.Elements("FFXProperty")
                                                  select element1;
            if (localNodeList.Count() > 0)
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
                        foreach (XElement Node in localNodeList)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            string localAxBy = $"A{Node.Attribute("TypeEnumA").Value}B{Node.Attribute("TypeEnumB").Value}";
                            string localIndex = $"{GetNodeIndexinParent(Node)}:";
                            string[] localSlot = DefParser.GetDefPropertiesArray(Node, PropertyType);
                            string localInput = AxByToName(Node);
                            string localLabel = $"{localIndex} {localSlot[0]}: {localSlot[1]} {localInput}";
                            ImGui.PushID($"ItemForLoopNode = {localLabel}");
                            if (localAxBy == "A67B19" || localAxBy == "A35B11" || localAxBy == "A99B27" || "null" == "A4163B35")
                            {
                                IEnumerable<XElement> NodeListProcessing = XMLChildNodesValid(Node.Element("Fields"));
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
        public static void IntInputDefaultNode(XElement node, string dataString)
        {
            string nodeValue = node.Attribute("Value").Value;
            if (ImGui.InputText(dataString, ref nodeValue, 10, ImGuiInputTextFlags.CharsDecimal))
            {
                int intNodeValue;
                if (Int32.TryParse(nodeValue, out intNodeValue))
                {
                    if (node.Attribute(xsi + "type").Value == "FFXFieldFloat")
                        node.Attribute(xsi + "type").Value = "FFXFieldInt";
                    node.Attribute("Value").Value = intNodeValue.ToString();
                }
            }
        }
        public static void FloatSliderDefaultNode(XElement node, string dataString, float minimumValue, float maximumValue)
        {
            float nodeValue = float.Parse(node.Attribute("Value").Value);
            if (ImGui.SliderFloat(dataString, ref nodeValue, minimumValue, maximumValue))
            {
                if (node.Attribute(xsi + "type").Value == "FFXFieldInt")
                    node.Attribute(xsi + "type").Value = "FFXFieldFloat";
                node.Attribute("Value").Value = nodeValue.ToString("#.0000");
            }
        }
        public static void FloatInputDefaultNode(XElement node, string dataString)
        {
            string nodeValue = node.Attribute("Value").Value;
            if (ImGui.InputText(dataString, ref nodeValue, 16, ImGuiInputTextFlags.CharsDecimal))
            {
                float floatNodeValue;
                if (float.TryParse(nodeValue, out floatNodeValue))
                {
                    if (node.Attribute(xsi + "type").Value == "FFXFieldInt")
                        node.Attribute(xsi + "type").Value = "FFXFieldFloat";
                    node.Attribute("Value").Value = floatNodeValue.ToString("#.0000");
                }
            }
        }
        public static void BooleanIntInputDefaultNode(XElement node, string dataString)
        {
            int nodeValue = Int32.Parse(node.Attribute("Value").Value);
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
                    if (node.Attribute(xsi + "type").Value == "FFXFieldFloat")
                        node.Attribute(xsi + "type").Value = "FFXFieldInt";
                    node.Attribute("Value").Value = 0.ToString();
                }
                return;
            }
            if (ImGui.Checkbox(dataString, ref nodeValueBool))
            {
                if (node.Attribute(xsi + "type").Value == "FFXFieldFloat")
                    node.Attribute(xsi + "type").Value = "FFXFieldInt";
                node.Attribute("Value").Value = (nodeValueBool ? 1 : 0).ToString();
            }
        }
        public static void IntComboDefaultNode(XElement node, string comboTitle, string[] entriesArrayValues, string[] entriesArrayNames)
        {
            int blendModeCurrent = Int32.Parse(node.Attribute("Value").Value);
            if (ImGui.Combo(comboTitle, ref blendModeCurrent, entriesArrayNames, entriesArrayNames.Length))
            {
                if (node.Attribute(xsi + "type").Value == "FFXFieldFloat")
                    node.Attribute(xsi + "type").Value = "FFXFieldInt";
                string tempstring = entriesArrayValues[blendModeCurrent];
                int tempint;
                if (Int32.TryParse(tempstring, out tempint))
                {
                    node.Attribute("Value").Value = tempint.ToString();
                }
            }
        }
        public static void IntComboNotLinearDefaultNode(XElement node, string comboTitle, XElement EnumEntries)
        {
            string localSelectedItem;
            XElement CurrentNode = (from element in EnumEntries.Descendants()
                                    where element.Attribute("value").Value == node.Attribute("Value").Value
                                    select element).First();
            if (CurrentNode != null)
                localSelectedItem = $"{CurrentNode.Attribute("value").Value}: {CurrentNode.Attribute("name").Value}";
            else
                localSelectedItem = $"{node.Attribute("Value").Value}: Not Enumerated";

            ArrayList localTempArray = new ArrayList();
            foreach (XElement node1 in XMLChildNodesValid(EnumEntries))
            {
                localTempArray.Add($"{node1.Attribute("value").Value}: {node1.Attribute("name").Value}");
            }
            string[] localArray = new string[localTempArray.Count];
            localTempArray.CopyTo(localArray);

            if (ImGui.BeginCombo(comboTitle, localSelectedItem))
            {
                for (int i = 0; i < localArray.Length; i++)
                {
                    if (ImGui.Selectable(localArray[i]))
                    {
                        if (node.Attribute(xsi + "type").Value == "FFXFieldFloat")
                            node.Attribute(xsi + "type").Value = "FFXFieldInt";
                        int safetyNetInt;
                        if (Int32.TryParse(XMLChildNodesValid(EnumEntries).ToArray()[i].Attribute("value").Value, out safetyNetInt))
                        {
                            node.Attribute("Value").Value = safetyNetInt.ToString();
                        }
                    }
                }
                ImGui.EndCombo();
            }
        }
        private static string AxByToName(XElement FFXProperty)
        {
            string localAxBy = $"A{FFXProperty.Attribute("TypeEnumA").Value}B{FFXProperty.Attribute("TypeEnumB").Value}";
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
        private static void GetFFXFields(XElement root, string fieldType)
        {
            IEnumerable<XElement> NodeListProcessing = XMLChildNodesValid(root.Descendants(fieldType).First());
            if (NodeListProcessing.Count() > 0)
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
                    Fields = new string[] { fieldType, root.Attributes().ToArray()[0].Value };
                    _showFFXEditorProperties = false;
                    _showFFXEditorFields = true;
                }
            }
        }
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
                        foreach (XElement node in NodeListEditor)
                        {
                            ImGui.TextWrapped($"{node.Attribute(xsi + "type").Value} = {node.Attribute("Value").Value}");
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
                ImGui.SetNextWindowDockID(MainViewport, ImGuiCond.FirstUseEver);
                ImGui.Begin("axbxDebug", ref _axbyDebugger);
                int integer = 0;
                foreach (XElement node in XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent))
                {
                    ImGui.Text($"Index = '{integer} Node = '{node}')");
                    integer++;
                }
                ImGui.End();
            }
        }
        //FFXPropertyHandler Functions Below here
        public static void FFXPropertyA35B11StaticColor(IEnumerable<XElement> NodeListEditor)
        {
            ImGui.BulletText("Single Static Color:");
            ImGui.Indent();
            ImGui.Indent();
            if (ImGui.ColorButton($"Static Color", new Vector4(float.Parse(NodeListEditor.ElementAt(0).Attribute("Value").Value), float.Parse(NodeListEditor.ElementAt(1).Attribute("Value").Value), float.Parse(NodeListEditor.ElementAt(2).Attribute("Value").Value), float.Parse(NodeListEditor.ElementAt(3).Attribute("Value").Value)), ImGuiColorEditFlags.AlphaPreview, new Vector2(30, 30)))
            {
                _cPickerRed = NodeListEditor.ElementAt(0);
                _cPickerGreen = NodeListEditor.ElementAt(1);
                _cPickerBlue = NodeListEditor.ElementAt(2);
                _cPickerAlpha = NodeListEditor.ElementAt(3);
                _cPicker = new Vector4(float.Parse(_cPickerRed.Attribute("Value").Value), float.Parse(_cPickerGreen.Attribute("Value").Value), float.Parse(_cPickerBlue.Attribute("Value").Value), float.Parse(_cPickerAlpha.Attribute("Value").Value));
                _cPickerIsEnable = true;
                ImGui.SetWindowFocus("FFX Color Picker");
            }
            ImGui.Unindent();
            ImGui.Unindent();
        }
        public static void FFXPropertyA67B19ColorInterpolationLinear(IEnumerable<XElement> NodeListEditor)
        {

            int Pos = 0;
            int StopsCount = Int32.Parse(NodeListEditor.ElementAt(0).Attribute("Value").Value);

            Pos += 9;
            if (ImGui.TreeNodeEx($"Color Stages: Total number of stages = {StopsCount}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGuiAddons.ButtonGradient("Decrease Stops Count") & StopsCount > 2)
                {
                    IEnumerable<XElement> tempXElementIEnumerable = XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent);
                    int LocalPos = 8;
                    for (int i = 0; i != 4; i++)
                    {
                        tempXElementIEnumerable.ElementAt((LocalPos + StopsCount + 1) + 8 + (4 * (StopsCount - 3))).Remove();
                    }
                    tempXElementIEnumerable.ElementAt(LocalPos + StopsCount).Remove();

                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (StopsCount - 1).ToString();
                    StopsCount--;

                    NodeListEditor = tempXElementIEnumerable;
                }
                ImGui.SameLine();
                if (ImGuiAddons.ButtonGradient("Increase Stops Count") & StopsCount < 8)
                {
                    IEnumerable<XElement> tempXElementIEnumerable = XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent);

                    int LocalPos = 8;

                    tempXElementIEnumerable.ElementAt(LocalPos + StopsCount).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );
                    for (int i = 0; i != 4; i++) //append 4 nodes at the end of the childnodes list
                    {
                        int localElementCount = tempXElementIEnumerable.Count();

                        tempXElementIEnumerable.ElementAt(localElementCount - 1).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );
                    }
                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (StopsCount + 1).ToString();
                    StopsCount++;


                    NodeListEditor = tempXElementIEnumerable;
                }
                int LocalColorOffset = Pos + 1;
                for (int i = 0; i != StopsCount; i++)
                {
                    ImGui.Separator();
                    ImGui.NewLine();
                    { // Slider Stuff
                        ImGui.BulletText($"Stage {i + 1}: Position in time");
                        FloatSliderDefaultNode(NodeListEditor.ElementAt(i + 9), $"###Stage{i + 1}Slider", 0.0f, 2.0f);
                    }

                    { // ColorButton
                        ImGui.Indent();
                        int PositionOffset = LocalColorOffset + StopsCount - (i + 1);
                        ImGui.Text($"Stage's Color:");
                        ImGui.SameLine();
                        if (ImGui.ColorButton($"Stage Position {i}: Color", new Vector4(float.Parse(NodeListEditor.ElementAt(PositionOffset).Attribute("Value").Value), float.Parse(NodeListEditor.ElementAt(PositionOffset + 1).Attribute("Value").Value), float.Parse(NodeListEditor.ElementAt(PositionOffset + 2).Attribute("Value").Value), float.Parse(NodeListEditor.ElementAt(PositionOffset + 3).Attribute("Value").Value)), ImGuiColorEditFlags.AlphaPreview, new Vector2(30, 30)))
                        {
                            _cPickerRed = NodeListEditor.ElementAt(PositionOffset);
                            _cPickerGreen = NodeListEditor.ElementAt(PositionOffset + 1);
                            _cPickerBlue = NodeListEditor.ElementAt(PositionOffset + 2);
                            _cPickerAlpha = NodeListEditor.ElementAt(PositionOffset + 3);
                            _cPicker = new Vector4(float.Parse(_cPickerRed.Attribute("Value").Value), float.Parse(_cPickerGreen.Attribute("Value").Value), float.Parse(_cPickerBlue.Attribute("Value").Value), float.Parse(_cPickerAlpha.Attribute("Value").Value));
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
        public static void FFXPropertyA99B27ColorInterpolationWithCustomCurve(IEnumerable<XElement> NodeListEditor)
        {
            int Pos = 0;
            int StopsCount = Int32.Parse(NodeListEditor.ElementAt(0).Attribute("Value").Value);
            Pos += 9;

            if (ImGui.TreeNodeEx($"Color Stages: Total number of stages = {StopsCount}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGuiAddons.ButtonGradient("Decrease Stops Count") & StopsCount > 2)
                {
                    IEnumerable<XElement> tempXElementIEnumerable = XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent);
                    int LocalPos = 8;

                    for (int i = 0; i != 4; i++)
                    {
                        tempXElementIEnumerable.ElementAt((LocalPos + StopsCount + 1) + 8 + (4 * (StopsCount - 3))).Remove();
                    }
                    for (int i = 0; i != 8; i++)
                    {
                        tempXElementIEnumerable.ElementAt(tempXElementIEnumerable.Count() - 1).Remove();
                    }
                    tempXElementIEnumerable.ElementAt(LocalPos + StopsCount).Remove();
                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (StopsCount - 1).ToString();
                    StopsCount--;

                    NodeListEditor = tempXElementIEnumerable;
                }
                ImGui.SameLine();
                if (ImGuiAddons.ButtonGradient("Increase Stops Count") & StopsCount < 8)
                {
                    IEnumerable<XElement> tempXElementIEnumerable = XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent);
                    int LocalPos = 8;

                    tempXElementIEnumerable.ElementAt(LocalPos + StopsCount).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                    );

                    for (int i = 0; i != 4; i++) //append 4 fields after last color alpha
                    {
                        tempXElementIEnumerable.ElementAt((LocalPos + StopsCount + 1) + 8 + 4 + (4 * (StopsCount - 3))).AddAfterSelf(
                            new XElement("FFXField", new XAttribute(xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );
                        for (int i2 = 0; i2 != 2; i2++)
                        {
                            tempXElementIEnumerable.ElementAt(tempXElementIEnumerable.Count() - 1).AddAfterSelf(
                                new XElement("FFXField", new XAttribute(xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                            );
                        }
                    }
                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (StopsCount + 1).ToString();
                    StopsCount++;

                    NodeListEditor = tempXElementIEnumerable;
                }
                int LocalColorOffset = Pos + 1;
                for (int i = 0; i != StopsCount; i++)
                {
                    ImGui.Separator();
                    ImGui.NewLine();
                    { // Slider Stuff
                        ImGui.BulletText($"Stage {i + 1}: Position in time");
                        FloatSliderDefaultNode(NodeListEditor.ElementAt(i + 9), $"###Stage{i + 1}Slider", 0.0f, 2.0f);
                    }

                    { // ColorButton
                        ImGui.Indent();
                        int PositionOffset = LocalColorOffset + StopsCount - (i + 1);
                        ImGui.Text($"Stage's Color:");
                        ImGui.SameLine();
                        if (ImGui.ColorButton($"Stage Position {i}: Color", new Vector4(float.Parse(NodeListEditor.ElementAt(PositionOffset).Attribute("Value").Value), float.Parse(NodeListEditor.ElementAt(PositionOffset + 1).Attribute("Value").Value), float.Parse(NodeListEditor.ElementAt(PositionOffset + 2).Attribute("Value").Value), float.Parse(NodeListEditor.ElementAt(PositionOffset + 3).Attribute("Value").Value)), ImGuiColorEditFlags.AlphaPreview, new Vector2(30, 30)))
                        {
                            _cPickerRed = NodeListEditor.ElementAt(PositionOffset);
                            _cPickerGreen = NodeListEditor.ElementAt(PositionOffset + 1);
                            _cPickerBlue = NodeListEditor.ElementAt(PositionOffset + 2);
                            _cPickerAlpha = NodeListEditor.ElementAt(PositionOffset + 3);
                            _cPicker = new Vector4(float.Parse(_cPickerRed.Attribute("Value").Value), float.Parse(_cPickerGreen.Attribute("Value").Value), float.Parse(_cPickerBlue.Attribute("Value").Value), float.Parse(_cPickerAlpha.Attribute("Value").Value));
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
                                    FloatSliderDefaultNode(NodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                {
                                    int localint = 1;
                                    ImGui.Text("Curve Point 1 = ");
                                    ImGui.SameLine();
                                    FloatSliderDefaultNode(NodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
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
                                    FloatSliderDefaultNode(NodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                {
                                    int localint = 3;
                                    ImGui.Text("Curve Point 1 = ");
                                    ImGui.SameLine();
                                    FloatSliderDefaultNode(NodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
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
                                    FloatSliderDefaultNode(NodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                {
                                    int localint = 5;
                                    ImGui.Text("Curve Point 1 = ");
                                    ImGui.SameLine();
                                    FloatSliderDefaultNode(NodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
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
                                    FloatSliderDefaultNode(NodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }

                                {
                                    int localint = 7;
                                    ImGui.Text("Curve Point 0 = ");
                                    ImGui.SameLine();
                                    FloatSliderDefaultNode(NodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
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
