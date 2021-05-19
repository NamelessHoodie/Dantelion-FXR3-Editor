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
using SoulsFormats;

namespace DFXR3Editor
{
    class MainUserInterface
    {
        private static Sdl2Window _window;
        private static GraphicsDevice _gd;
        private static CommandList _cl;
        private static ImGuiController _controller;
        private static readonly float FrameRateForDelta = 58.82352941176471f;

        // UI state
        private static Vector3 _clearColor = new Vector3(0.45f, 0.55f, 0.6f);
        private static uint mainViewPortDockSpaceID;
        private static bool _keyboardInputGuide = false;

        // Exception Handler
        private static bool _exceptionPopupOPen = false;
        private static string _exceptionTitleString = "";
        private static string _exceptionContentString = "";

        // Config
        private static readonly string iniPath = "Config/EditorConfigs.ini";

        private static readonly IniConfigFile _selectedTheme = new IniConfigFile("General", "Theme", "Red Clay", iniPath);
        private static string _activeTheme = _selectedTheme.ReadConfigsIni();

        // Supported FFX Arguments
        private static readonly string[] _actionIDSupported = DefParser.SupportedActionIDs();
        private static readonly string[] AxByColorArray = new string[] { "A19B7", "A35B11", "A67B19", "A99B27", "A4163B35" };
        private static readonly string[] AxByScalarArray = new string[] { "A0B0", "A16B4", "A32B8", "A64B16", "A96B24", "A4160B32" };

        // Save/Load Path
        private static string _loadedFilePath = "";
        //Theme Selector
        readonly static String[] _themeSelectorEntriesArray = { "Red Clay", "ImGui Dark", "ImGui Classic" };

        //XML
        private static XDocument xDocLinq;
        private static bool XMLOpen = false;
        private static bool _axbyDebugger = false;
        private static bool _filtertoggle = false;
        public static bool _showFFXEditorFields = false;
        public static bool _showFFXEditorProperties = false;
        private static uint treeViewCurrentHighlighted = 0;
        public static IEnumerable<XElement> NodeListEditor;
        public static string AxBy;
        public static string[] Fields;
        public static readonly XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        public static readonly XNamespace xsd = "http://www.w3.org/2001/XMLSchema";
        public static readonly XNamespace nm;
        private static XElement ffxPropertyEditorElement;

        //FFX Workshop Tools
        //<Color Editor>
        public static bool _cPickerIsEnable = false;

        public static XElement _cPickerRed;
        public static XElement _cPickerGreen;
        public static XElement _cPickerBlue;
        public static XElement _cPickerAlpha;
        public static Vector4 _cPicker = new Vector4();
        public static float _colorOverload = 1.0f;
        public static ActionManager actionManager = new ActionManager();
        // Color Editor

        [STAThread]
        static void Main()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            // Create window, GraphicsDevice, and all resources necessary for the demo.
            VeldridStartup.CreateWindowAndGraphicsDevice(new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "Dantelion FXR3 Editor"),
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

            //Theme Selector
            Themes.ThemesSelectorPush(_activeTheme);

            // Main application loop
            while (_window.Exists)
            {
                InputSnapshot snapshot = _window.PumpEvents();
                if (!_window.Exists) { break; }
                _controller.Update(1f / FrameRateForDelta, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

                //SetupMainDockingSpace
                ImGuiViewportPtr mainViewportPtr = ImGui.GetMainViewport();
                mainViewPortDockSpaceID = ImGui.DockSpaceOverViewport(mainViewportPtr);

                if (_controller.GetWindowMinimized(mainViewportPtr) == 0)
                {
                    SubmitMainWindowUI();
                }
                SubmitDockableUI();
                HotkeyListener();

                _cl.Begin();
                _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
                _cl.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 1f));
                _controller.Render(_gd, _cl);
                _cl.End();
                _gd.SubmitCommands(_cl);
                _gd.SwapBuffers(_gd.MainSwapchain);
                _controller.SwapExtraWindows(_gd);
                Thread.Sleep(17);
            }
            //Runtime Configs Save

            // Clean up Veldrid resources
            _gd.WaitForIdle();
            _controller.Dispose();
            _cl.Dispose();
            _gd.Dispose();
        }
        private static void HotkeyListener()
        {
            { //Undo-Redo
                if (ImGui.GetIO().KeyCtrl & ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Z)) & actionManager.CanUndo())
                    actionManager.UndoAction();
                if (ImGui.GetIO().KeyCtrl & ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Y)) & actionManager.CanRedo())
                    actionManager.RedoAction();
            }
        }
        private static unsafe void SubmitMainWindowUI()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Load FFX *XML"))
                    {
                        try
                        {
                            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog
                            {
                                Filter = "XML|*.xml"
                            };
                            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                if (XMLOpen)
                                    CloseOpenFFXWithoutSaving();
                                _loadedFilePath = ofd.FileName;
                                XMLOpen = true;
                                xDocLinq = XDocument.Load(ofd.FileName);

                                if (xDocLinq.Element("FXR3") == null || xDocLinq.Element("RootEffectCall") == null)
                                {
                                    throw new Exception("This xml file is not a valid FFX, it does not contain the FXR3 node or the RootEffectCall node.");
                                }

                            }

                        }
                        catch (Exception exception)
                        {
                            CloseOpenFFXWithoutSaving();
                            ShowExceptionPopup("ERROR: *.xml loading failed", exception);
                        }
                    }
                    if (ImGui.MenuItem("Load FFX *FXR"))
                    {
                        try
                        {
                            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog
                            {
                                Filter = "FXR|*.fxr"
                            };
                            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                if (XMLOpen)
                                    CloseOpenFFXWithoutSaving();
                                _loadedFilePath = ofd.FileName;
                                XMLOpen = true;
                                xDocLinq = FXR3_XMLR.FXR3EnhancedSerialization.FXR3ToXML(FXR3_XMLR.FXR3.Read(ofd.FileName));
                            }
                        }
                        catch (Exception exception)
                        {
                            CloseOpenFFXWithoutSaving();
                            ShowExceptionPopup("ERROR: *.fxr loading failed", exception);
                        }
                    }
                    if (_loadedFilePath != "" & XMLOpen)
                    {
                        if (ImGui.MenuItem("Save Open FFX"))
                        {
                            try
                            {
                                if (_loadedFilePath.EndsWith(".xml"))
                                {
                                    xDocLinq.Save(_loadedFilePath);
                                    FXR3_XMLR.FXR3EnhancedSerialization.XMLToFXR3(xDocLinq).Write(_loadedFilePath.Substring(0, _loadedFilePath.Length - 4));
                                }
                                else if (_loadedFilePath.EndsWith(".fxr"))
                                {
                                    xDocLinq.Save(_loadedFilePath + ".xml");
                                    FXR3_XMLR.FXR3EnhancedSerialization.XMLToFXR3(xDocLinq).Write(_loadedFilePath);
                                }
                            }
                            catch (Exception exception)
                            {
                                CloseOpenFFXWithoutSaving();
                                ShowExceptionPopup("ERROR: FFX saving failed", exception);
                            }
                        }
                    }
                    else
                    {
                        ImGui.TextDisabled("Save Open FFX");
                    }
                    if (actionManager.CanUndo())
                    {
                        if (ImGui.MenuItem("Undo"))
                        {
                            actionManager.UndoAction();
                        }
                    }
                    else
                    {
                        ImGui.TextDisabled("Undo");
                    }
                    if (actionManager.CanRedo())
                    {
                        if (ImGui.MenuItem("Redo"))
                        {
                            actionManager.RedoAction();
                        }
                    }
                    else
                    {
                        ImGui.TextDisabled("Redo");
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Themes"))
                {
                    if (ImGui.BeginCombo("Theme Selector", _activeTheme))
                    {
                        foreach (string str in _themeSelectorEntriesArray)
                        {
                            bool selected = false;
                            if (str == _activeTheme)
                                selected = true;
                            if (ImGui.Selectable(str, selected))
                            {
                                _activeTheme = str;
                                Themes.ThemesSelectorPush(_activeTheme);
                                _selectedTheme.WriteConfigsIni(_activeTheme);
                            }
                        }
                        ImGui.EndCombo();
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
                ImGui.SetNextWindowDockID(mainViewPortDockSpaceID, ImGuiCond.Appearing);
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
        }
        private static void SubmitDockableUI()
        {
            { //Declare Standalone Windows here
                // Color Picker
                if (_cPickerIsEnable)
                {
                    ImGui.SetNextWindowDockID(mainViewPortDockSpaceID, ImGuiCond.FirstUseEver);
                    if (ImGui.Begin("FFX Color Picker", ref _cPickerIsEnable))
                    {
                        Vector2 mEME = ImGui.GetWindowSize();
                        if (mEME.X > mEME.Y)
                        {
                            ImGui.SetNextItemWidth(mEME.Y * 0.80f);
                        }
                        ImGui.ColorPicker4("CPicker", ref _cPicker, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoTooltip);
                        if (ImGui.IsItemDeactivatedAfterEdit())
                        {
                            var actionList = new List<Action>();

                            if (_cPickerRed.Attribute(xsi + "type").Value == "FFXFieldInt" || _cPickerGreen.Attribute(xsi + "type").Value == "FFXFieldInt" || _cPickerBlue.Attribute(xsi + "type").Value == "FFXFieldInt" || _cPickerAlpha.Attribute(xsi + "type").Value == "FFXFieldInt")
                            {
                                actionList.Add(new ModifyXAttributeString(_cPickerRed.Attribute(xsi + "type"), "FFXFieldFloat"));
                                actionList.Add(new ModifyXAttributeString(_cPickerGreen.Attribute(xsi + "type"), "FFXFieldFloat"));
                                actionList.Add(new ModifyXAttributeString(_cPickerBlue.Attribute(xsi + "type"), "FFXFieldFloat"));
                                actionList.Add(new ModifyXAttributeString(_cPickerAlpha.Attribute(xsi + "type"), "FFXFieldFloat"));
                            }
                            actionList.Add(new ModifyXAttributeFloat(_cPickerRed.Attribute("Value"), _cPicker.X));
                            actionList.Add(new ModifyXAttributeFloat(_cPickerGreen.Attribute("Value"), _cPicker.Y));
                            actionList.Add(new ModifyXAttributeFloat(_cPickerBlue.Attribute("Value"), _cPicker.Z));
                            actionList.Add(new ModifyXAttributeFloat(_cPickerAlpha.Attribute("Value"), _cPicker.W));

                            actionManager.ExecuteAction(new CompoundAction(actionList));
                        }
                        ImGui.Separator();
                        ImGui.Text("Brightness Multiplier");
                        ImGui.SliderFloat("###Brightness Multiplier", ref _colorOverload, 0, 10f);
                        ImGui.SameLine();
                        if (ImGuiAddons.ButtonGradient("Multiply Color"))
                        {
                            List<Action> actions = new List<Action>();
                            _cPicker.X *= _colorOverload;
                            _cPicker.Y *= _colorOverload;
                            _cPicker.Z *= _colorOverload;
                            actions.Add(new EditPublicCPickerVector4(new Vector4(_cPicker.X *= _colorOverload, _cPicker.Y *= _colorOverload, _cPicker.Z *= _colorOverload, _cPicker.W)));
                            actions.Add(new ModifyXAttributeFloat(_cPickerRed.Attribute("Value"), _cPicker.X));
                            actions.Add(new ModifyXAttributeFloat(_cPickerGreen.Attribute("Value"), _cPicker.Y));
                            actions.Add(new ModifyXAttributeFloat(_cPickerBlue.Attribute("Value"), _cPicker.Z));
                            actionManager.ExecuteAction(new CompoundAction(actions));
                        }
                        ImGui.End();
                    }
                }
                // Keyboard Guide
                if (_keyboardInputGuide)
                {
                    ImGui.SetNextWindowDockID(mainViewPortDockSpaceID, ImGuiCond.FirstUseEver);
                    ImGui.Begin("Keyboard Guide", ref _keyboardInputGuide, ImGuiWindowFlags.MenuBar);
                    ImGui.BeginMenuBar();
                    ImGui.EndMenuBar();
                    ImGui.ShowUserGuide();
                    ImGui.End();
                }
                if (_axbyDebugger)
                {
                    ImGui.SetNextWindowDockID(mainViewPortDockSpaceID, ImGuiCond.FirstUseEver);
                    ImGui.Begin("axbxDebug", ref _axbyDebugger);
                    if (NodeListEditor != null)
                    {
                        if (NodeListEditor.Any() & (_showFFXEditorFields || _showFFXEditorProperties))
                        {
                            int integer = 0;
                            foreach (XNode node in NodeListEditor.ElementAt(0).Parent.Nodes())
                            {
                                ImGui.Text($"Index = '{integer} Node = '{node}')");
                                integer++;
                            }
                        }
                    }
                    ImGui.End();
                }
                if (_exceptionPopupOPen)
                {
                    if (!ImGui.IsPopupOpen(_exceptionTitleString))
                    {
                        ImGui.OpenPopup(_exceptionTitleString);
                    }
                    if (ImGui.IsPopupOpen(_exceptionTitleString))
                    {
                        ImGuiViewportPtr mainViewport = ImGui.GetMainViewport();
                        Vector2 textInputSize = new Vector2(mainViewport.Size.X * 0.8f, mainViewport.Size.Y * 0.8f);
                        ImGui.SetNextWindowPos(new Vector2(mainViewport.Pos.X + mainViewport.Size.X * 0.5f, mainViewport.Pos.Y + mainViewport.Size.Y * 0.5f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
                        if (ImGui.BeginPopupModal(_exceptionTitleString, ref _exceptionPopupOPen, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize))
                        {
                            ImGui.InputTextMultiline("TextInput", ref _exceptionContentString, 1024, textInputSize, ImGuiInputTextFlags.ReadOnly);
                            if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Escape)))
                            {
                                _exceptionPopupOPen = false;
                            }
                            ImGui.EndPopup();
                        }
                    }
                }
            }
        }
        private static void ShowExceptionPopup(string exceptionTitle, Exception exceptionToDisplay)
        {
            _exceptionPopupOPen = true;
            _exceptionTitleString = exceptionTitle;
            _exceptionContentString = exceptionToDisplay.ToString();
        }
        private static void PopulateTree(XElement root)
        {
            if (root != null)
            {
                int rootIndexInParent = GetNodeIndexinParent(root);
                IEnumerable<XNode> CommentsList = root.Nodes().Where(n => n.NodeType == XmlNodeType.Comment);
                XComment RootCommentNode = null;
                string RootComment = "";
                if (CommentsList.Any())
                {
                    RootCommentNode = (XComment)CommentsList.First();
                    RootComment = RootCommentNode.Value;
                }
                ImGui.PushID($"TreeFunctionlayer = {root.Name} ChildIndex = {rootIndexInParent}");
                IEnumerable<XElement> localNodeList = XMLChildNodesValid(root);
                if (root.Attribute(XName.Get("ActionID")) != null)
                {
                    if (_actionIDSupported.Contains(root.Attribute("ActionID").Value) || _filtertoggle)
                    {
                        if (ImGuiAddons.TreeNodeTitleColored($"Action('{DefParser.ActionIDName(root.Attribute("ActionID").Value)}')", ImGuiAddons.GetStyleColorVec4Safe(ImGuiCol.CheckMark)))
                        {
                            TreeViewContextMenu(root, RootCommentNode, RootComment);
                            GetFFXProperties(root, "Properties1");
                            GetFFXProperties(root, "Properties2");
                            GetFFXFields(root, "Fields1");
                            GetFFXFields(root, "Fields2");
                            ImGui.TreePop();
                        }
                        else
                            TreeViewContextMenu(root, RootCommentNode, RootComment);
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
                                                     where _filtertoggle || _actionIDSupported.Contains(node.Attribute("ActionID").Value)
                                                     select node;
                    if (tempnode.Any())
                    {
                        //XCommentInputStyled(root, RootCommentNode, RootComment);
                        if (ImGuiAddons.TreeNodeTitleColored(EffectIDToName(root.Attribute("EffectID").Value), ImGuiAddons.GetStyleColorVec4Safe(ImGuiCol.TextDisabled)))
                        {
                            TreeViewContextMenu(root, RootCommentNode, RootComment);
                            foreach (XElement node in localNodeList)
                            {
                                PopulateTree(node);
                            }
                            ImGui.TreePop();
                        }
                        else
                            TreeViewContextMenu(root, RootCommentNode, RootComment);
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
        public static void TreeViewContextMenu(XElement Node, XComment RootCommentNode, string RootComment)
        {
            bool isComment = false;
            if (RootCommentNode != null)
                isComment = true;

            string popupName = "Treeview Context Menu";
            ImGui.OpenPopupOnItemClick(popupName, ImGuiPopupFlags.MouseButtonRight);
            if (ImGui.IsPopupOpen(popupName))
            {
                if (ImGui.BeginPopupContextWindow(popupName))
                {
                    if (!isComment)
                    {
                        if (ImGui.Selectable("Add Comment"))
                        {
                            Node.Add(new XComment(RootComment));
                        }
                        if (ImGuiAddons.isItemHoveredForTime(500, FrameRateForDelta, "HoverTimer"))
                        {
                            ImGui.Indent();
                            ImGui.Text("Adds a comment to the selected node.");
                            ImGui.Text("Remember to left click to modify!");
                            ImGui.Unindent();
                        }
                    }
                    ImGui.EndPopup();
                }
            }

            if (isComment)
            {
                XCommentInputStyled(Node, RootCommentNode, RootComment);
            }
        }
        public static void XCommentInputStyled(XElement Node, XComment RootCommentNode, string RootComment)
        {
            ImGui.SameLine();
            string textCommentLabel;
            if (RootComment != "")
                textCommentLabel = RootComment;
            else
                textCommentLabel = "*";

            uint IDStorage = ImGui.GetID("CommentInput");
            ImGuiStoragePtr storage = ImGui.GetStateStorage();
            bool open = storage.GetBool(IDStorage);
            Vector2 sizeText = ImGui.CalcTextSize(textCommentLabel);
            Vector2 imguiCursorPos = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(imguiCursorPos.X + 13f);
            ImDrawListPtr draw_list = ImGuiNET.ImGui.GetWindowDrawList();
            Vector2 screenCursorPos = ImGuiNET.ImGui.GetCursorScreenPos();
            Vector2 p = new Vector2(screenCursorPos.X - 10f / 2f, screenCursorPos.Y - 5f / 2f);
            float commentBoxHeightTemp = sizeText.Y + 5f;
            float commentBoxWidthTemp = sizeText.X + 10f;
            Vector2 commentBoxSize = new Vector2(p.X + commentBoxWidthTemp, p.Y + commentBoxHeightTemp);
            draw_list.AddRectFilled(p, commentBoxSize, ImGui.GetColorU32(ImGuiCol.FrameBg), 5f);
            draw_list.AddRect(p, commentBoxSize, ImGui.GetColorU32(ImGuiCol.BorderShadow), 5f);
            if (open)
            {
                Vector2 avalaibleContentSpace = ImGui.GetContentRegionAvail();
                imguiCursorPos = ImGui.GetCursorPos();
                ImGui.SetCursorPos(new Vector2(imguiCursorPos.X - 2f, imguiCursorPos.Y - 3f));
                float inputBoxWidth = sizeText.X + 4f;
                if (inputBoxWidth > avalaibleContentSpace.X)
                    inputBoxWidth = avalaibleContentSpace.X;
                ImGui.SetNextItemWidth(inputBoxWidth);
                if (ImGui.InputText("##InputBox", ref RootComment, 256))
                {
                    RootCommentNode.Value = RootComment;
                }
                if ((!ImGui.IsItemHovered() & ImGui.IsMouseClicked(ImGuiMouseButton.COUNT)) || ImGui.IsItemDeactivated())
                {
                    storage.SetBool(IDStorage, false);
                }
            }
            else
            {
                ImGui.Text(textCommentLabel);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    storage.SetBool(IDStorage, true);
                }
                string popupName = "Comment Context Menu";
                ImGui.OpenPopupOnItemClick(popupName, ImGuiPopupFlags.MouseButtonRight);
                if (ImGui.IsPopupOpen(popupName))
                {
                    if (ImGui.BeginPopupContextWindow(popupName))
                    {
                        if (ImGui.Selectable("Remove Comment"))
                        {
                            RootCommentNode.Remove();
                        }
                        if (ImGuiAddons.isItemHoveredForTime(500, FrameRateForDelta, "HoverTimer"))
                        {
                            ImGui.Indent();
                            ImGui.Text("Removes the comment from the selected node.");
                            ImGui.Text("Remember to left click to modify!");
                            ImGui.Unindent();
                        }
                        ImGui.EndPopup();
                    }
                }
            }
        }
        public static IEnumerable<XElement> XMLChildNodesValid(XElement Node)
        {
            //return from element in Node.Elements()
            //       where element.NodeType == XmlNodeType.Element
            //       select element;

            return Node.Elements();
        }
        public static int GetNodeIndexinParent(XElement Node)
        {
            //return Node.ElementsBeforeSelf().Where(n => n.NodeType == XmlNodeType.Element).Count();
            return Node.ElementsBeforeSelf().Count();
        }
        private static void CloseOpenFFXWithoutSaving()
        {
            _loadedFilePath = "";
            XMLOpen = false;
            xDocLinq = null;
            _cPickerIsEnable = false;
            _showFFXEditorFields = false;
            _showFFXEditorProperties = false;
        }
        public static void ShowToolTipSimple(string toolTipUID, string toolTipTitle, string toolTipText, bool isToolTipObjectSpawned, ImGuiPopupFlags clickButton)
        {
            float frameHeight = ImGui.GetFrameHeight();
            string localUID = toolTipUID + toolTipTitle;
            if (isToolTipObjectSpawned)
            {
                float cursorPosX = ImGui.GetCursorPosX();
                Vector2 screenCursorPos = ImGui.GetCursorScreenPos();
                ImDrawListPtr draw_list = ImGui.GetWindowDrawList();

                string text = "?";
                Vector2 sizeText = ImGui.CalcTextSize(text);

                float radius = frameHeight * 0.4f;

                ImGui.InvisibleButton(toolTipUID + "Invisible Button", sizeText);
                uint circleColor;
                if (ImGui.IsItemHovered())
                    circleColor = ImGui.GetColorU32(ImGuiCol.CheckMark);
                else
                    circleColor = ImGui.GetColorU32(ImGuiCol.TitleBgActive);
                Vector2 p = new Vector2(screenCursorPos.X + (sizeText.X / 2f), screenCursorPos.Y + (frameHeight / 2f));

                draw_list.AddCircle(new Vector2(p.X, p.Y), radius, ImGui.GetColorU32(ImGuiCol.BorderShadow));
                draw_list.AddCircleFilled(new Vector2(p.X, p.Y), radius, circleColor);

                ImGui.SameLine();
                ImGui.SetCursorPosX(cursorPosX);
                ImGui.Text(text);
            }
            ImGui.OpenPopupOnItemClick(localUID, clickButton);
            if (ImGui.IsPopupOpen(localUID))
            {
                Vector2 mousePos = ImGui.GetMousePos();
                Vector2 localTextSize = ImGui.CalcTextSize(toolTipText);
                float maxToolTipWidth = (float)_window.Width * 0.4f;
                Vector2 windowSize = new Vector2(maxToolTipWidth, localTextSize.Y);
                float windowWidth = mousePos.X;
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
        public static void ShowToolTipSimple(string toolTipUID, string toolTipTitle, string toolTipText, bool isToolTipObjectSpawned, float hoveredMsForTooltip)
        {
            float frameHeight = ImGui.GetFrameHeight();
            string localUID = toolTipUID + toolTipTitle;
            if (isToolTipObjectSpawned)
            {
                float cursorPosX = ImGui.GetCursorPosX();
                Vector2 screenCursorPos = ImGui.GetCursorScreenPos();
                ImDrawListPtr draw_list = ImGui.GetWindowDrawList();

                string text = "?";
                Vector2 sizeText = ImGui.CalcTextSize(text);

                float radius = frameHeight * 0.4f;

                ImGui.InvisibleButton(toolTipUID + "Invisible Button", sizeText);
                uint circleColor;
                if (ImGui.IsItemHovered())
                    circleColor = ImGui.GetColorU32(ImGuiCol.CheckMark);
                else
                    circleColor = ImGui.GetColorU32(ImGuiCol.TitleBgActive);
                Vector2 p = new Vector2(screenCursorPos.X + (sizeText.X / 2f), screenCursorPos.Y + (frameHeight / 2f));

                draw_list.AddCircle(new Vector2(p.X, p.Y), radius, ImGui.GetColorU32(ImGuiCol.BorderShadow));
                draw_list.AddCircleFilled(new Vector2(p.X, p.Y), radius, circleColor);

                ImGui.SameLine();
                ImGui.SetCursorPosX(cursorPosX);
                ImGui.Text(text);
            }
            if (ImGuiAddons.isItemHoveredForTime(hoveredMsForTooltip, FrameRateForDelta, toolTipUID + "Hovered") & !ImGui.IsPopupOpen(localUID))
            {
                Vector2 mousePos = ImGui.GetMousePos();
                Vector2 localTextSize = ImGui.CalcTextSize(toolTipText);
                float maxToolTipWidth = (float)_window.Width * 0.4f;
                Vector2 windowSize = new Vector2(maxToolTipWidth, localTextSize.Y);
                float windowWidth = mousePos.X;
                ImGui.SetNextWindowPos(new Vector2(windowWidth, mousePos.Y + frameHeight), ImGuiCond.Appearing);
                ImGui.SetNextWindowSize(windowSize, ImGuiCond.Appearing);
                if (ImGui.Begin(localUID, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar))
                {
                    ImGui.Text(toolTipTitle);
                    ImGui.NewLine();
                    ImGui.TextWrapped(toolTipText);
                    ImGui.End();
                }
            }
        }
        private static void GetFFXProperties(XElement root, string PropertyType)
        {
            IEnumerable<XElement> localNodeList = from element0 in root.Elements(PropertyType)
                                                  from element1 in element0.Elements("FFXProperty")
                                                  select element1;
            if (localNodeList.Any())
            {
                if (ImGui.TreeNodeEx($"{PropertyType}"))
                {
                    if (ImGui.BeginTable("##table2", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX))
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
                            string localInput = AxByToName(localAxBy);
                            string localLabel = $"{localIndex} {localSlot[0]}: {localSlot[1]} {localInput}";
                            ImGui.PushID($"ItemForLoopNode = {localLabel}");
                            if (AxByScalarArray.Contains(localAxBy) || AxByColorArray.Contains(localAxBy))
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
                                ImGui.Selectable($"{localSlot[0]}###{localLabel}", selected, ImGuiSelectableFlags.SpanAllColumns);
                                if (ImGui.IsItemClicked(ImGuiMouseButton.Left) & !selected)
                                {
                                    treeViewCurrentHighlighted = IDStorage;
                                    storage.SetBool(IDStorage, true);
                                    NodeListEditor = NodeListProcessing;
                                    ffxPropertyEditorElement = Node;
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
                    ImGui.TreePop();
                }
            }
        }
        public static void IntInputDefaultNode(XElement node, string dataString)
        {
            string nodeValue = node.Attribute("Value").Value;
            ImGui.InputText(dataString, ref nodeValue, 10, ImGuiInputTextFlags.CharsDecimal);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                if (Int32.TryParse(nodeValue, out int intNodeValue))
                {
                    var actionList = new List<Action>();

                    if (node.Attribute(xsi + "type").Value == "FFXFieldFloat")
                        actionList.Add(new ModifyXAttributeString(node.Attribute(xsi + "type"), "FFXFieldInt"));
                    actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), intNodeValue));

                    actionManager.ExecuteAction(new CompoundAction(actionList));
                }
            }
        }
        public static void FloatSliderDefaultNode(XElement node, string dataString, float minimumValue, float maximumValue)
        {
            float nodeValue = float.Parse(node.Attribute("Value").Value);
            if (ImGui.SliderFloat(dataString, ref nodeValue, minimumValue, maximumValue))
            {
                if (ImGui.IsItemEdited())
                {
                    var actionList = new List<Action>();

                    if (node.Attribute(xsi + "type").Value == "FFXFieldInt")
                        actionList.Add(new ModifyXAttributeString(node.Attribute(xsi + "type"), "FFXFieldFloat"));
                    actionList.Add(new ModifyXAttributeFloat(node.Attribute("Value"), nodeValue));

                    actionManager.ExecuteAction(new CompoundAction(actionList));
                }
            }
        }
        public static void FloatInputDefaultNode(XElement node, string dataString)
        {
            string nodeValue = node.Attribute("Value").Value;
            ImGui.InputText(dataString, ref nodeValue, 16, ImGuiInputTextFlags.CharsDecimal);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                if (float.TryParse(nodeValue, out float floatNodeValue))
                {
                    var actionList = new List<Action>();

                    if (node.Attribute(xsi + "type").Value == "FFXFieldInt")
                        actionList.Add(new ModifyXAttributeString(node.Attribute(xsi + "type"), "FFXFieldFloat"));
                    actionList.Add(new ModifyXAttributeFloat(node.Attribute("Value"), floatNodeValue));

                    actionManager.ExecuteAction(new CompoundAction(actionList));
                }
            }
        }
        public static void BooleanIntInputDefaultNode(XElement node, string dataString)
        {
            int nodeValue = Int32.Parse(node.Attribute("Value").Value);
            bool nodeValueBool;
            if (nodeValue == 1)
                nodeValueBool = true;
            else if (nodeValue == 0)
                nodeValueBool = false;
            else
            {
                ImGui.Text("Error: Bool Invalid, current value is: " + nodeValue.ToString());
                if (ImGui.Button("Set Bool to False"))
                {
                    var actionList = new List<Action>();

                    if (node.Attribute(xsi + "type").Value == "FFXFieldFloat")
                        actionList.Add(new ModifyXAttributeString(node.Attribute(xsi + "type"), "FFXFieldInt"));
                    actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), 0));

                    actionManager.ExecuteAction(new CompoundAction(actionList));
                }
                return;
            }
            if (ImGui.Checkbox(dataString, ref nodeValueBool))
            {
                var actionList = new List<Action>();

                if (node.Attribute(xsi + "type").Value == "FFXFieldFloat")
                    actionList.Add(new ModifyXAttributeString(node.Attribute(xsi + "type"), "FFXFieldInt"));
                actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), (nodeValueBool ? 1 : 0)));

                actionManager.ExecuteAction(new CompoundAction(actionList));
            }
        }
        public static void IntComboDefaultNode(XElement node, string comboTitle, string[] entriesArrayValues, string[] entriesArrayNames)
        {
            int blendModeCurrent = Int32.Parse(node.Attribute("Value").Value);
            if (ImGui.Combo(comboTitle, ref blendModeCurrent, entriesArrayNames, entriesArrayNames.Length))
            {
                string tempstring = entriesArrayValues[blendModeCurrent];
                if (Int32.TryParse(tempstring, out int tempint))
                {
                    var actionList = new List<Action>();

                    if (node.Attribute(xsi + "type").Value == "FFXFieldFloat")
                        actionList.Add(new ModifyXAttributeString(node.Attribute(xsi + "type"), "FFXFieldInt"));
                    actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), tempint));

                    actionManager.ExecuteAction(new CompoundAction(actionList));
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
                        if (Int32.TryParse(XMLChildNodesValid(EnumEntries).ToArray()[i].Attribute("value").Value, out int safetyNetInt))
                        {
                            var actionList = new List<Action>();

                            if (node.Attribute(xsi + "type").Value == "FFXFieldFloat")
                                actionList.Add(new ModifyXAttributeString(node.Attribute(xsi + "type"), "FFXFieldInt"));
                            actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), safetyNetInt));

                            actionManager.ExecuteAction(new CompoundAction(actionList));
                        }
                    }
                }
                ImGui.EndCombo();
            }
        }
        private static string AxByToName(string FFXPropertyAxBy)
        {
            return FFXPropertyAxBy switch
            {
                "A0B0" => "Static 0",
                "A16B4" => "Static 1",
                "A19B7" => "Static Opaque White",
                "A32B8" => "Static Input",
                "A35B11" => "Static Input",
                "A64B16" => "Linear Interpolation",
                "A67B19" => "Linear Interpolation",
                "A96B24" => "Curve interpolation",
                "A99B27" => "Curve interpolation",
                "A4160B32" => "Loop Linear Interpolation",
                "A4163B35" => "Loop Linear Interpolation",
                _ => FFXPropertyAxBy,
            };
        }
        private static string EffectIDToName(string EffectID)
        {
            return EffectID switch
            {
                "1002" => "Thresholds<'LOD'>",
                "1004" => "Effect()",
                "2000" => "Root<>",
                "2001" => "Reference<'FXR'>",
                "2002" => "Container<'LOD'>",
                "2200" => "Container<'Effect'>",
                "2202" => "Container<'Effect+'>",
                _ => EffectID,
            };
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
            ShowToolTipSimple("", toolTipTitle, fullToolTip, false, 1000f);
        }
        private static void GetFFXFields(XElement root, string fieldType)
        {
            IEnumerable<XElement> NodeListProcessing = XMLChildNodesValid(root.Descendants(fieldType).First());
            if (NodeListProcessing.Any())
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
        private static void AxBySwapper()
        {
            ImGui.BulletText("Input Type:");
            ImGui.SameLine();
            if (ImGui.BeginCombo("##Current AxBy", AxByToName(AxBy)))
            {
                if (AxByColorArray.Contains(AxBy))
                {
                    foreach (string str in AxByColorArray)
                    {
                        bool selected = false;
                        if (AxBy == str)
                            selected = true;
                        if (ImGui.Selectable(AxByToName(str), selected) & str != AxBy)
                        {
                            XElement axbyElement = ffxPropertyEditorElement;
                            if (str == "A19B7")
                            {
                                XElement templateXElement = DefParser.TemplateGetter("19", "7");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "19"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "7"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection());

                                    actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            else if (str == "A35B11")
                            {
                                XElement templateXElement = DefParser.TemplateGetter("35", "11");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "35"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "11"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection());

                                    actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            else if (str == "A67B19")
                            {
                                if (AxBy == "A4163B35")
                                {
                                    var actionListQuick = new List<Action>();

                                    actionListQuick.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "67"));
                                    actionListQuick.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "19"));

                                    actionListQuick.Add(new ResetEditorSelection());

                                    actionManager.ExecuteAction(new CompoundAction(actionListQuick));
                                    return;
                                }
                                XElement templateXElement = DefParser.TemplateGetter("67", "19");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "67"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "19"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection());

                                    actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            else if (str == "A99B27")
                            {
                                XElement templateXElement = DefParser.TemplateGetter("99", "27");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "99"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "27"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection());

                                    actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            else if (str == "A4163B35")
                            {
                                if (AxBy == "A67B19")
                                {
                                    var actionListQuick = new List<Action>();

                                    actionListQuick.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "4163"));
                                    actionListQuick.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "35"));

                                    actionListQuick.Add(new ResetEditorSelection());

                                    actionManager.ExecuteAction(new CompoundAction(actionListQuick));
                                    return;
                                }
                                XElement templateXElement = DefParser.TemplateGetter("4163", "35");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "4163"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "35"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection());

                                    actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            return;
                        }
                    }
                }
                else if (AxByScalarArray.Contains(AxBy))
                {
                    foreach (string str in AxByScalarArray)
                    {
                        bool selected = false;
                        if (AxBy == str)
                            selected = true;
                        if (ImGui.Selectable(AxByToName(str), selected) & str != AxBy)
                        {
                            XElement axbyElement = ffxPropertyEditorElement;
                            if (str == "A0B0")
                            {
                                XElement templateXElement = DefParser.TemplateGetter("0", "0");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "0"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "0"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection());

                                    actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            else if (str == "A16B4")
                            {
                                XElement templateXElement = DefParser.TemplateGetter("16", "4");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "16"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "4"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection());

                                    actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            else if (str == "A32B8")
                            {
                                XElement templateXElement = DefParser.TemplateGetter("32", "8");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "32"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "8"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection());

                                    actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            else if (str == "A64B16")
                            {
                                XElement templateXElement = DefParser.TemplateGetter("64", "16");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "64"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "16"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection());

                                    actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            else if (str == "A96B24")
                            {
                                XElement templateXElement = DefParser.TemplateGetter("96", "24");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "96"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "24"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection());

                                    actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            else if (str == "A4160B32")
                            {
                                XElement templateXElement = DefParser.TemplateGetter("4160", "32");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "4160"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "32"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection());

                                    actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            return;
                        }
                    }
                }
                else
                {
                    ImGui.Selectable(AxBy, true);
                }
                ImGui.EndCombo();
            }
        }
        public static void ResetEditorSelection() 
        {
            _showFFXEditorProperties = false;
            treeViewCurrentHighlighted = 0;
            _cPickerIsEnable = false;
            if (NodeListEditor.Any())
                NodeListEditor = XMLChildNodesValid(NodeListEditor.First().Parent);
        }
        public static void FFXEditor()
        {
            ImGui.BeginChild("TxtEdit");
            //var meme = ImGui.GetWindowDrawList();

            //var meme1 = ImGui.GetCursorScreenPos();
            //meme.AddRectFilled(meme1 , meme1 + ImGui.GetWindowSize(), ImGui.GetColorU32(ImGuiCol.ButtonHovered));
            if (_showFFXEditorProperties)
            {
                AxBySwapper();
                ImGui.NewLine();
                switch (AxBy)
                {
                    case "A0B0":
                        break;
                    case "A16B4":
                        break;
                    case "A19B7":
                        break;
                    case "A32B8":
                        FFXPropertyA32B8StaticScalar(NodeListEditor);
                        break;
                    case "A35B11":
                        FFXPropertyA35B11StaticColor(NodeListEditor);
                        break;
                    case "A64B16":
                        FFXPropertyA64B16ScalarInterpolationLinear(NodeListEditor);
                        break;
                    case "A67B19":
                        FFXPropertyA67B19ColorInterpolationLinear(NodeListEditor);
                        break;
                    case "A96B24":
                        FFXPropertyA96B24ScalarInterpolationWithCustomCurve(NodeListEditor);
                        break;
                    case "A99B27":
                        FFXPropertyA99B27ColorInterpolationWithCustomCurve(NodeListEditor);
                        break;
                    case "A4163B35":
                        FFXPropertyA67B19ColorInterpolationLinear(NodeListEditor);
                        break;
                    case "A4160B32":
                        FFXPropertyA64B16ScalarInterpolationLinear(NodeListEditor);
                        break;
                    default:
                        ImGui.Text("ERROR: FFX Property Handler not found, using Default Handler.");
                        foreach (XElement node in NodeListEditor)
                        {
                            string dataType = node.Attribute(xsi + "type").Value;
                            int nodeIndex = GetNodeIndexinParent(node);
                            if (dataType == "FFXFieldFloat")
                            {
                                FloatInputDefaultNode(node, dataType + "##" + nodeIndex.ToString());
                            }
                            else if (dataType == "FFXFieldInt")
                            {
                                IntInputDefaultNode(node, dataType + "##" + nodeIndex.ToString());
                            }
                        }
                        break;
                }
            }
            else if (_showFFXEditorFields)
            {
                DefParser.DefXMLParser(NodeListEditor, Fields[1], Fields[0]);
            }
            ImGui.EndChild();
        }
        //FFXPropertyHandler Functions Below here
        public static void FFXPropertyA32B8StaticScalar(IEnumerable<XElement> NodeListEditor)
        {
            ImGui.BulletText("Single Static Scale Value:");
            ImGui.Indent();
            ImGui.Indent();
            FloatInputDefaultNode(NodeListEditor.ElementAt(0), "##Single Scalar Field");
            ImGui.Unindent();
            ImGui.Unindent();
        }
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
        public static void FFXPropertyA64B16ScalarInterpolationLinear(IEnumerable<XElement> NodeListEditor)
        {
            int StopsCount = Int32.Parse(NodeListEditor.ElementAt(0).Attribute("Value").Value);

            int Pos = 2;
            if (ImGui.TreeNodeEx($"Scalar Stages: Total number of stages = {StopsCount}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGuiAddons.ButtonGradient("Decrease Stops Count") & StopsCount > 2)
                {
                    var nodeFresh = NodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

                    IEnumerable<XElement> tempXElementIEnumerable = XMLChildNodesValid(nodeFresh);

                    tempXElementIEnumerable.ElementAt(Pos + (StopsCount * 2)).Remove();

                    tempXElementIEnumerable.ElementAt(Pos + StopsCount).Remove();

                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (StopsCount - 1).ToString();

                    actionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

                    StopsCount--;

                    NodeListEditor = tempXElementIEnumerable;
                }
                ImGui.SameLine();
                if (ImGuiAddons.ButtonGradient("Increase Stops Count") & StopsCount < 8)
                {
                    var nodeFresh = NodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

                    IEnumerable<XElement> tempXElementIEnumerable = XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent);

                    tempXElementIEnumerable.ElementAt(Pos + (StopsCount * 2)).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );

                    tempXElementIEnumerable.ElementAt(Pos + StopsCount).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );

                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (StopsCount + 1).ToString();

                    actionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

                    StopsCount++;

                    NodeListEditor = tempXElementIEnumerable;
                }
                for (int i = 0; i != StopsCount; i++)
                {
                    ImGui.Separator();
                    ImGui.NewLine();
                    { // Slider Stuff
                        ImGui.BulletText($"Stage {i + 1}: Position in time");
                        FloatSliderDefaultNode(NodeListEditor.ElementAt(i + 3), $"###Stage{i + 1}Slider1", 0.0f, 2.0f);
                    }

                    { // Scale Slider
                        ImGui.Indent();
                        int PositionOffset = Pos + StopsCount + (i + 1);
                        ImGui.Text($"Stage's Scale:");
                        ImGui.SameLine();
                        FloatSliderDefaultNode(NodeListEditor.ElementAt(PositionOffset), $"###Stage{i + 1}Slider2", 0.0f, 5.0f);
                        ImGui.Unindent();
                    }
                    ImGui.NewLine();
                }
                ImGui.Separator();
                ImGui.TreePop();
            }
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
                    var nodeFresh = NodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

                    IEnumerable<XElement> tempXElementIEnumerable = XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent);
                    int LocalPos = 8;
                    for (int i = 0; i != 4; i++)
                    {
                        tempXElementIEnumerable.ElementAt((LocalPos + StopsCount + 1) + 8 + (4 * (StopsCount - 3))).Remove();
                    }
                    tempXElementIEnumerable.ElementAt(LocalPos + StopsCount).Remove();

                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (StopsCount - 1).ToString();

                    actionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

                    StopsCount--;

                    NodeListEditor = tempXElementIEnumerable;
                }
                ImGui.SameLine();
                if (ImGuiAddons.ButtonGradient("Increase Stops Count") & StopsCount < 8)
                {
                    var nodeFresh = NodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

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

                    actionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

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
        public static void FFXPropertyA96B24ScalarInterpolationWithCustomCurve(IEnumerable<XElement> NodeListEditor)
        {
            int StopsCount = Int32.Parse(NodeListEditor.ElementAt(0).Attribute("Value").Value);

            int Pos = 2;
            if (ImGui.TreeNodeEx($"Scalar Stages: Total number of stages = {StopsCount}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGuiAddons.ButtonGradient("Decrease Stops Count") & StopsCount > 2)
                {
                    var nodeFresh = NodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

                    IEnumerable<XElement> tempXElementIEnumerable = XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent);

                    for (int i = 0; i < 2; i++)
                    {
                        tempXElementIEnumerable.ElementAt(Pos + (StopsCount * 2) + (StopsCount * 2) - 1).Remove();
                    }

                    tempXElementIEnumerable.ElementAt(Pos + (StopsCount * 2)).Remove();

                    tempXElementIEnumerable.ElementAt(Pos + StopsCount).Remove();

                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (StopsCount - 1).ToString();

                    actionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

                    StopsCount--;

                    NodeListEditor = tempXElementIEnumerable;
                }
                ImGui.SameLine();
                if (ImGuiAddons.ButtonGradient("Increase Stops Count") & StopsCount < 8)
                {
                    var nodeFresh = NodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

                    IEnumerable<XElement> tempXElementIEnumerable = XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent);
                    tempXElementIEnumerable.ElementAt(Pos + (StopsCount * 2) + (StopsCount * 2)).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0")),
                        new XElement("FFXField", new XAttribute(xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );

                    tempXElementIEnumerable.ElementAt(Pos + (StopsCount * 2)).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );

                    tempXElementIEnumerable.ElementAt(Pos + StopsCount).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );

                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (StopsCount + 1).ToString();

                    actionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

                    StopsCount++;

                    NodeListEditor = tempXElementIEnumerable;
                }
                for (int i = 0; i != StopsCount; i++)
                {
                    ImGui.Separator();
                    ImGui.NewLine();
                    { // Slider Stuff
                        ImGui.BulletText($"Stage {i + 1}: Position in time");
                        FloatSliderDefaultNode(NodeListEditor.ElementAt(i + 3), $"###Stage{i + 1}Slider1", 0.0f, 2.0f);
                    }

                    { // Scale Slider
                        ImGui.Indent();
                        int PositionOffset = Pos + StopsCount + (i + 1);
                        ImGui.Text($"Stage's Scale:");
                        ImGui.SameLine();
                        FloatSliderDefaultNode(NodeListEditor.ElementAt(PositionOffset), $"###Stage{i + 1}Slider2", 0.0f, 5.0f);
                        ImGui.Unindent();
                    }

                    { // Curve Slider
                        ImGui.Indent();
                        int PositionOffset = Pos + (StopsCount * 2) + ((i + 1) * 2 - 1);
                        ImGui.Text($"Stage's Curve Angle:");
                        ImGui.SameLine();
                        FloatSliderDefaultNode(NodeListEditor.ElementAt(PositionOffset), $"###Stage{i + 1}Slider3", 0.0f, 5.0f);
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
                    var nodeFresh = NodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

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

                    actionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

                    StopsCount--;

                    NodeListEditor = tempXElementIEnumerable;
                }
                ImGui.SameLine();
                if (ImGuiAddons.ButtonGradient("Increase Stops Count") & StopsCount < 8)
                {
                    var nodeFresh = NodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

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

                    actionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

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
                        ImGui.Indent();
                        if (ImGui.TreeNodeEx($"Custom Curve Settngs###{i + 1}CurveSettingsNode"))
                        {
                            if (ImGui.BeginTable($"Custom Curve Settngs###{i + 1}CurveSettingsTable", 3))
                            {
                                ImGui.TableSetupColumn("Color", ImGuiTableColumnFlags.WidthFixed);
                                ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthFixed);
                                ImGui.TableSetupColumn("Control");
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    int localint = 0;
                                    ImGui.Text("Red:");
                                    ImGui.TableNextColumn();
                                    ImGui.Text("Stage's Curve Angle");
                                    ImGui.TableNextColumn();
                                    FloatSliderDefaultNode(NodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    int localint = 2;
                                    ImGui.Text("Green:");
                                    ImGui.TableNextColumn();
                                    ImGui.Text("Stage's Curve Angle");
                                    ImGui.TableNextColumn();
                                    FloatSliderDefaultNode(NodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    int localint = 4;
                                    ImGui.Text("Blue:");
                                    ImGui.TableNextColumn();
                                    ImGui.Text("Stage's Curve Angle");
                                    ImGui.TableNextColumn();
                                    FloatSliderDefaultNode(NodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    int localint = 6;
                                    ImGui.Text("Alpha:");
                                    ImGui.TableNextColumn();
                                    ImGui.Text("Stage's Curve Angle");
                                    ImGui.TableNextColumn();
                                    FloatSliderDefaultNode(NodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                ImGui.EndTable();
                            }
                            ImGui.TreePop();
                        }
                        ImGui.Unindent();
                    }
                    ImGui.NewLine();
                }
                ImGui.Separator();
                ImGui.TreePop();
            }
        }
    }
}
