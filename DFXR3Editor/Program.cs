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
using System.Diagnostics;
using DFXR3Editor.Dependencies;
using System.Windows.Forms;

namespace DFXR3Editor
{
    class MainUserInterface
    {
        public static Sdl2Window _window;
        private static GraphicsDevice _gd;
        private static CommandList _cl;
        private static ImGuiController _controller;
        public static readonly float FrameRateForDelta = 58.82352941176471f;

        // Exception Handler
        private static bool _exceptionPopupOPen = false;
        private static string _exceptionTitleString = "";
        private static string _exceptionContentString = "";
        private static void ShowExceptionPopup(string exceptionTitle, Exception exceptionToDisplay)
        {
            _exceptionPopupOPen = true;
            _exceptionTitleString = exceptionTitle;
            _exceptionContentString = exceptionToDisplay.ToString();
        }

        // UI state
        private static Vector3 _clearColor = new Vector3(0.45f, 0.55f, 0.6f);
        public static uint mainViewPortDockSpaceID;
        private static bool _keyboardInputGuide = false;
        public static bool _axbyDebugger = false;
        public static XElement dragAndDropBuffer = null;

        // Config
        private static readonly string iniPath = "Config/EditorConfigs.ini";

        private static readonly IniConfigFile _selectedTheme = new IniConfigFile("General", "Theme", "Red Clay", iniPath);
        private static string _activeTheme = _selectedTheme.ReadConfigsIni();

        //Theme Selector
        readonly static String[] _themeSelectorEntriesArray = { "Red Clay", "ImGui Dark", "ImGui Classic" };

        public static bool _filtertoggle = false;

        public static FFXUI selectedFFXWindow;
        public static List<FFXUI> openFFXs = new List<FFXUI>();

        //<Color Editor>
        public static bool _cPickerIsEnable = false;
        public static XElement _cPickerRed;
        public static XElement _cPickerGreen;
        public static XElement _cPickerBlue;
        public static XElement _cPickerAlpha;
        public static Vector4 _cPicker = new Vector4();
        public static float _colorOverload = 1.0f;
        // Color Editor


        //FFX Workshop Tools


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
                if (openFFXs.Any())
                {
                    selectedFFXWindow.HotkeyListener();
                }

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
        private static unsafe void SubmitMainWindowUI()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Load FFX"))
                    {
                        try
                        {
                            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
                            ofd.Filter = "FFX|*.fxr;*.xml";
                            ofd.Title = "Open FFX";

                            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                if (Path.GetExtension(ofd.FileName) == ".fxr")
                                {
                                    var fxrXml = FXR3_XMLR.FXR3EnhancedSerialization.FXR3ToXML(FXR3_XMLR.FXR3.Read(ofd.FileName));
                                    selectedFFXWindow = new FFXUI(fxrXml, ofd.FileName);
                                    openFFXs.Add(selectedFFXWindow);
                                }
                                else if (Path.GetExtension(ofd.FileName) == ".xml")
                                {
                                    var fxrXml = XDocument.Load(ofd.FileName);
                                    if (fxrXml.Element("FXR3") == null || fxrXml.Root.Element("RootEffectCall") == null)
                                    {
                                        throw new Exception("This xml file is not a valid FFX, it does not contain the FXR3 node or the RootEffectCall node.");
                                    }
                                    else
                                    {
                                        selectedFFXWindow = new FFXUI(fxrXml, ofd.FileName);
                                        openFFXs.Add(selectedFFXWindow);
                                    }
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            ShowExceptionPopup("ERROR: FFX loading failed", exception);
                        }
                    }
                    if (ImGui.MenuItem("Save", openFFXs.Any()))
                    {
                        try
                        {
                            if (selectedFFXWindow._loadedFilePath.EndsWith(".xml"))
                            {
                                selectedFFXWindow.xDocLinq.Save(selectedFFXWindow._loadedFilePath);
                                FXR3_XMLR.FXR3EnhancedSerialization.XMLToFXR3(selectedFFXWindow.xDocLinq).Write(selectedFFXWindow._loadedFilePath.Substring(0, selectedFFXWindow._loadedFilePath.Length - 4));
                            }
                            else if (selectedFFXWindow._loadedFilePath.EndsWith(".fxr"))
                            {
                                selectedFFXWindow.xDocLinq.Save(selectedFFXWindow._loadedFilePath + ".xml");
                                FXR3_XMLR.FXR3EnhancedSerialization.XMLToFXR3(selectedFFXWindow.xDocLinq).Write(selectedFFXWindow._loadedFilePath);
                            }
                        }
                        catch (Exception exception)
                        {
                            ShowExceptionPopup("ERROR: FFX saving failed", exception);
                        }
                    }
                    if (ImGui.MenuItem("Save as", openFFXs.Any()))
                    {
                        try
                        {
                            System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
                            saveFileDialog1.Filter = "FXR|*.fxr|XML|*.xml";
                            saveFileDialog1.Title = "Save FFX as";

                            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                if (Path.GetExtension(saveFileDialog1.FileName) == ".fxr")
                                {
                                    FXR3_XMLR.FXR3EnhancedSerialization.XMLToFXR3(selectedFFXWindow.xDocLinq).Write(saveFileDialog1.FileName);
                                }
                                else if (Path.GetExtension(saveFileDialog1.FileName) == ".xml")
                                {
                                    selectedFFXWindow.xDocLinq.Save(saveFileDialog1.FileName);
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            ShowExceptionPopup("ERROR: FFX saving failed", exception);
                        }
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Edit"))
                {
                    if (ImGui.MenuItem("Undo", "Ctrl Z", false, selectedFFXWindow != null ? selectedFFXWindow.actionManager.CanUndo() : false ))
                    {
                        selectedFFXWindow.actionManager.UndoAction();
                    }
                    if (ImGui.MenuItem("Redo", "Ctrl Y", false, selectedFFXWindow != null ? selectedFFXWindow.actionManager.CanRedo() : false))
                    {
                        selectedFFXWindow.actionManager.RedoAction();
                    }
                    if (ImGui.MenuItem("Extend Active FFX Treeview", selectedFFXWindow != null))
                    {
                        selectedFFXWindow.collapseExpandTreeView = true;
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
                    if (ImGui.MenuItem("Lock DFXR3E Input"))
                    {
                        MessageBox.Show("DFXR3E Inputs are locked, press OK to unlock");
                    }

                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }
            ImGui.SetNextWindowDockID(mainViewPortDockSpaceID, ImGuiCond.FirstUseEver);
            for (int i = 0; i < openFFXs.Count(); i++)
            {
                ImGui.SetNextWindowDockID(mainViewPortDockSpaceID, ImGuiCond.FirstUseEver);
                if (openFFXs[i].RenderFFX())
                {
                    openFFXs[i].TreeviewExpandCollapseHandler(true);
                }
            }
            ImGui.SetNextWindowDockID(mainViewPortDockSpaceID, ImGuiCond.FirstUseEver);
            if (ImGui.Begin("FFXEditor"))
            {
                if (selectedFFXWindow != null)
                {
                    if (selectedFFXWindow._showFFXEditorProperties || selectedFFXWindow._showFFXEditorFields)
                    {
                        FFXEditor();
                    }
                }
                ImGui.End();
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

                            if (_cPickerRed.Attribute(FFXHelperMethods.xsi + "type").Value == "FFXFieldInt" || _cPickerGreen.Attribute(FFXHelperMethods.xsi + "type").Value == "FFXFieldInt" || _cPickerBlue.Attribute(FFXHelperMethods.xsi + "type").Value == "FFXFieldInt" || _cPickerAlpha.Attribute(FFXHelperMethods.xsi + "type").Value == "FFXFieldInt")
                            {
                                actionList.Add(new ModifyXAttributeString(_cPickerRed.Attribute(FFXHelperMethods.xsi + "type"), "FFXFieldFloat"));
                                actionList.Add(new ModifyXAttributeString(_cPickerGreen.Attribute(FFXHelperMethods.xsi + "type"), "FFXFieldFloat"));
                                actionList.Add(new ModifyXAttributeString(_cPickerBlue.Attribute(FFXHelperMethods.xsi + "type"), "FFXFieldFloat"));
                                actionList.Add(new ModifyXAttributeString(_cPickerAlpha.Attribute(FFXHelperMethods.xsi + "type"), "FFXFieldFloat"));
                            }
                            actionList.Add(new ModifyXAttributeFloat(_cPickerRed.Attribute("Value"), _cPicker.X));
                            actionList.Add(new ModifyXAttributeFloat(_cPickerGreen.Attribute("Value"), _cPicker.Y));
                            actionList.Add(new ModifyXAttributeFloat(_cPickerBlue.Attribute("Value"), _cPicker.Z));
                            actionList.Add(new ModifyXAttributeFloat(_cPickerAlpha.Attribute("Value"), _cPicker.W));

                            selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
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
                            selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actions));
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
                    if (selectedFFXWindow.NodeListEditor != null)
                    {
                        if (selectedFFXWindow.NodeListEditor.Any() & (selectedFFXWindow._showFFXEditorFields || selectedFFXWindow._showFFXEditorProperties))
                        {
                            int integer = 0;
                            foreach (XNode node in selectedFFXWindow.NodeListEditor.ElementAt(0).Parent.Nodes())
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
        public static void CloseOpenFFXWithoutSaving(FFXUI ffxUI)
        {
            ffxUI._loadedFilePath = "";
            ffxUI.xDocLinq = null;
            _cPickerIsEnable = false;
            ffxUI._showFFXEditorFields = false;
            ffxUI._showFFXEditorProperties = false;
        }
        public static void ResetEditorSelection(FFXUI ffxUI)
        {
            ffxUI._showFFXEditorFields = false;
            ffxUI._showFFXEditorProperties = false;
            ffxUI.treeViewCurrentHighlighted = 0;
            _cPickerIsEnable = false;
            if (ffxUI.NodeListEditor != null)
            {
                if (ffxUI.NodeListEditor.Any())
                {
                    ffxUI.NodeListEditor = FFXHelperMethods.XMLChildNodesValid(ffxUI.NodeListEditor.First().Parent);
                }
            }
        }
        public static void FFXEditor()
        {
            ImGui.SetNextWindowDockID(mainViewPortDockSpaceID, ImGuiCond.FirstUseEver);
            if (selectedFFXWindow._showFFXEditorProperties)
            {
                AxBySwapper();
                ImGui.NewLine();
                switch (selectedFFXWindow.AxBy)
                {
                    case "A0B0":
                        break;
                    case "A16B4":
                        break;
                    case "A19B7":
                        break;
                    case "A32B8":
                        selectedFFXWindow.FFXPropertyA32B8StaticScalar(selectedFFXWindow.NodeListEditor);
                        break;
                    case "A35B11":
                        selectedFFXWindow.FFXPropertyA35B11StaticColor(selectedFFXWindow.NodeListEditor);
                        break;
                    case "A64B16":
                        selectedFFXWindow.FFXPropertyA64B16ScalarInterpolationLinear(selectedFFXWindow.NodeListEditor);
                        break;
                    case "A67B19":
                        selectedFFXWindow.FFXPropertyA67B19ColorInterpolationLinear(selectedFFXWindow.NodeListEditor);
                        break;
                    case "A96B24":
                        selectedFFXWindow.FFXPropertyA96B24ScalarInterpolationWithCustomCurve(selectedFFXWindow.NodeListEditor);
                        break;
                    case "A99B27":
                        selectedFFXWindow.FFXPropertyA99B27ColorInterpolationWithCustomCurve(selectedFFXWindow.NodeListEditor);
                        break;
                    case "A4163B35":
                        selectedFFXWindow.FFXPropertyA67B19ColorInterpolationLinear(selectedFFXWindow.NodeListEditor);
                        break;
                    case "A4160B32":
                        selectedFFXWindow.FFXPropertyA64B16ScalarInterpolationLinear(selectedFFXWindow.NodeListEditor);
                        break;
                    default:
                        ImGui.Text("ERROR: FFX Property Handler not found, using Default Handler.");
                        foreach (XElement node in selectedFFXWindow.NodeListEditor)
                        {
                            string dataType = node.Attribute(FFXHelperMethods.xsi + "type").Value;
                            int nodeIndex = FFXHelperMethods.GetNodeIndexinParent(node);
                            if (dataType == "FFXFieldFloat")
                            {
                                selectedFFXWindow.FloatInputDefaultNode(node, dataType + "##" + nodeIndex.ToString());
                            }
                            else if (dataType == "FFXFieldInt")
                            {
                                selectedFFXWindow.IntInputDefaultNode(node, dataType + "##" + nodeIndex.ToString());
                            }
                        }
                        break;
                }
            }
            else if (selectedFFXWindow._showFFXEditorFields)
            {
                DefParser.DefXMLParser(selectedFFXWindow.NodeListEditor, selectedFFXWindow.Fields[1], selectedFFXWindow.Fields[0]);
            }
        }
        private static void AxBySwapper()
        {
            ImGui.BulletText("Input Type:");
            ImGui.SameLine();
            if (ImGui.BeginCombo("##Current AxBy", FFXHelperMethods.AxByToName(selectedFFXWindow.AxBy)))
            {
                if (FFXHelperMethods.AxByColorArray.Contains(selectedFFXWindow.AxBy))
                {
                    foreach (string str in FFXHelperMethods.AxByColorArray)
                    {
                        bool selected = false;
                        if (selectedFFXWindow.AxBy == str)
                            selected = true;
                        if (ImGui.Selectable(FFXHelperMethods.AxByToName(str), selected) & str != selectedFFXWindow.AxBy)
                        {
                            XElement axbyElement = selectedFFXWindow.ffxPropertyEditorElement;
                            if (str == "A19B7")
                            {
                                XElement templateXElement = DefParser.TemplateGetter("19", "7");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "19"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "7"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection(selectedFFXWindow));

                                    selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
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

                                    actionList.Add(new ResetEditorSelection(selectedFFXWindow));

                                    selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            else if (str == "A67B19")
                            {
                                if (selectedFFXWindow.AxBy == "A4163B35")
                                {
                                    var actionListQuick = new List<Action>();

                                    actionListQuick.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "67"));
                                    actionListQuick.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "19"));

                                    actionListQuick.Add(new ResetEditorSelection(selectedFFXWindow));

                                    selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionListQuick));
                                    return;
                                }
                                XElement templateXElement = DefParser.TemplateGetter("67", "19");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "67"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "19"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection(selectedFFXWindow));

                                    selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
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

                                    actionList.Add(new ResetEditorSelection(selectedFFXWindow));

                                    selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            else if (str == "A4163B35")
                            {
                                if (selectedFFXWindow.AxBy == "A67B19")
                                {
                                    var actionListQuick = new List<Action>();

                                    actionListQuick.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "4163"));
                                    actionListQuick.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "35"));

                                    actionListQuick.Add(new ResetEditorSelection(selectedFFXWindow));

                                    selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionListQuick));
                                    return;
                                }
                                XElement templateXElement = DefParser.TemplateGetter("4163", "35");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "4163"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "35"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection(selectedFFXWindow));

                                    selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            return;
                        }
                    }
                }
                else if (FFXHelperMethods.AxByScalarArray.Contains(selectedFFXWindow.AxBy))
                {
                    foreach (string str in FFXHelperMethods.AxByScalarArray)
                    {
                        bool selected = false;
                        if (selectedFFXWindow.AxBy == str)
                            selected = true;
                        if (ImGui.Selectable(FFXHelperMethods.AxByToName(str), selected) & str != selectedFFXWindow.AxBy)
                        {
                            XElement axbyElement = selectedFFXWindow.ffxPropertyEditorElement;
                            if (str == "A0B0")
                            {
                                XElement templateXElement = DefParser.TemplateGetter("0", "0");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "0"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "0"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection(selectedFFXWindow));

                                    selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
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

                                    actionList.Add(new ResetEditorSelection(selectedFFXWindow));

                                    selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
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

                                    actionList.Add(new ResetEditorSelection(selectedFFXWindow));

                                    selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
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

                                    actionList.Add(new ResetEditorSelection(selectedFFXWindow));

                                    selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
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

                                    actionList.Add(new ResetEditorSelection(selectedFFXWindow));

                                    selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
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

                                    actionList.Add(new ResetEditorSelection(selectedFFXWindow));

                                    selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            return;
                        }
                    }
                }
                else
                {
                    ImGui.Selectable(selectedFFXWindow.AxBy, true);
                }
                ImGui.EndCombo();
            }
            ImGui.SameLine();
            if (ImGuiAddons.ButtonGradient("Flip C/S"))
            {
                XElement axbyElement = selectedFFXWindow.ffxPropertyEditorElement;
                if (FFXHelperMethods.AxByColorArray.Contains(selectedFFXWindow.AxBy))
                {
                    XElement templateXElement = DefParser.TemplateGetter("0", "0");
                    if (templateXElement != null)
                    {
                        var actionList = new List<Action>();

                        actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "0"));
                        actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "0"));

                        actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                        actionList.Add(new ResetEditorSelection(selectedFFXWindow));

                        selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
                    }
                }
                else
                {
                    XElement templateXElement = DefParser.TemplateGetter("19", "7");
                    if (templateXElement != null)
                    {
                        var actionList = new List<Action>();

                        actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "19"));
                        actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "7"));

                        actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                        actionList.Add(new ResetEditorSelection(selectedFFXWindow));

                        selectedFFXWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
                    }
                }
            }
        }
    }
}
