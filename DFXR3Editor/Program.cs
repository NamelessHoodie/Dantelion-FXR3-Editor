﻿using System;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using ImGuiNET;
using System.Xml.Linq;
using ImGuiNETAddons;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using SoulsFormats;
using DFXR3Editor.Dependencies;
using System.Windows.Forms;
using FXR3 = FXR3_XMLR.FXR3;
using FFXPatchTest;

namespace DFXR3Editor
{
    static class MainUserInterface
    {
        public static Sdl2Window Window;
        public static GraphicsDevice Gd;
        private static CommandList _cl;
        public static ImGuiController Controller;
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
        private static readonly Vector3 ClearColor = new Vector3(0.45f, 0.55f, 0.6f);
        private static uint _mainViewPortDockSpaceId;
        private static bool _keyboardInputGuide = false;
        private static bool _axbyDebugger = false;
        public static XElement DragAndDropBuffer = null;
        public static ImGuiFxrTextureHandler FfxTextureHandler;
        public static bool IsSearchById = false;
        public static string SearchBarString = "";
        public static bool IsSearchBarOpen = false;

        // Config
        private static readonly string IniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/EditorConfigs.ini");
        private static readonly IniConfigFile TextureDisplaySizeConfig = new IniConfigFile("UIConfigs", "textureDisplaySizeInt32", "100", IniPath);
        public static int TextureDisplaySize = int.Parse(TextureDisplaySizeConfig.ReadConfigsIni());
        private static readonly IniConfigFile SelectedThemeConfig = new IniConfigFile("General", "Theme", "Red Clay", IniPath);
        private static string _activeTheme = SelectedThemeConfig.ReadConfigsIni();

        //Theme Selector
        static readonly String[] ThemeSelectorEntriesArray = { "Red Clay", "ImGui Dark", "ImGui Classic" };

        public static bool Filtertoggle = false;

        public static FFXUI SelectedFfxWindow;
        public static List<FFXUI> OpenFfXs = new List<FFXUI>();

        //<Color Editor>
        public static bool CPickerIsEnable = false;
        public static XElement CPickerRed;
        public static XElement CPickerGreen;
        public static XElement CPickerBlue;
        public static XElement CPickerAlpha;
        public static Vector4 CPicker = new Vector4();
        public static float ColorOverload = 1.0f;
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
                out Window,
                out Gd);
            Window.Resized += () =>
            {
                Gd.MainSwapchain.Resize((uint)Window.Width, (uint)Window.Height);
                Controller.WindowResized(Window.Width, Window.Height);
            };
            _cl = Gd.ResourceFactory.CreateCommandList();

            Controller = new ImGuiController(Gd, Window, Gd.MainSwapchain.Framebuffer.OutputDescription, Window.Width, Window.Height);

            //Theme Selector
            Themes.ThemesSelectorPush(_activeTheme);

            // Main application loop
            while (Window.Exists)
            {
                InputSnapshot snapshot = Window.PumpEvents();
                if (!Window.Exists) { break; }
                Controller.Update(1f / FrameRateForDelta, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

                //SetupMainDockingSpace
                ImGuiViewportPtr mainViewportPtr = ImGui.GetMainViewport();
                _mainViewPortDockSpaceId = ImGui.DockSpaceOverViewport(mainViewportPtr);

                ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

                HotKeyGlobalListener();
                if (Controller.GetWindowMinimized(mainViewportPtr) == 0)
                {
                    SubmitMainMenuBar();
                    SubmitMainWindowUi();
                }
                SubmitDockableUi();
                if (OpenFfXs.Any())
                {
                    SelectedFfxWindow.HotkeyListener();
                }

                _cl.Begin();
                _cl.SetFramebuffer(Gd.MainSwapchain.Framebuffer);
                _cl.ClearColorTarget(0, new RgbaFloat(ClearColor.X, ClearColor.Y, ClearColor.Z, 1f));
                Controller.Render(Gd, _cl);
                _cl.End();
                Gd.SubmitCommands(_cl);
                Gd.SwapBuffers(Gd.MainSwapchain);
                Controller.SwapExtraWindows(Gd);
                Thread.Sleep(17);
            }
            //Runtime Configs Save
            TextureDisplaySizeConfig.WriteConfigsIni(TextureDisplaySize);

            // Clean up Veldrid resources
            Gd.WaitForIdle();
            Controller.Dispose();
            _cl.Dispose();
            Gd.Dispose();
        }

        private static void LoadFfxFromXml(XDocument fxrXml, string filePath, FXR3 loadTimeFxr3)
        {
            if (fxrXml.Element("FXR3") == null || fxrXml.Root.Element("RootEffectCall") == null)
            {
                throw new Exception("This file is not a valid FFX, it does not contain the FXR3 node or the RootEffectCall node.");
            }
            else
            {
                SelectedFfxWindow = new FFXUI(fxrXml, filePath, loadTimeFxr3);
                OpenFfXs.Add(SelectedFfxWindow);
            }
        }

        private static void SubmitMainMenuBar()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Load FFX"))
                    {
#if RELEASE
                        try
                        {
#endif
                        System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
                        ofd.Filter = "FFX|*.fxr;*.xml";
                        ofd.Title = "Open FFX";

                        if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            if (Path.GetExtension(ofd.FileName) == ".fxr")
                            {
                                var fxr3 = FXR3.Read(ofd.FileName);
                                var fxrXml = FXR3_XMLR.FXR3EnhancedSerialization.FXR3ToXML(fxr3);
                                LoadFfxFromXml(fxrXml, ofd.FileName, fxr3);
                            }
                            else if (Path.GetExtension(ofd.FileName) == ".xml")
                            {
                                var fxrXml = XDocument.Load(ofd.FileName);
                                var fxr3 = FXR3_XMLR.FXR3EnhancedSerialization.XMLToFXR3(SelectedFfxWindow.xDocLinq);
                                LoadFfxFromXml(fxrXml, ofd.FileName, fxr3);
                            }
                        }
#if RELEASE
                    }
                        catch (Exception exception)
                        {
                            ShowExceptionPopup("ERROR: FFX loading failed", exception);
                        }
#endif
                    }
                    if (ImGui.MenuItem("Save", OpenFfXs.Any()))
                    {
                        try
                        {
                            if (SelectedFfxWindow._loadedFilePath.EndsWith(".xml"))
                            {
                                SelectedFfxWindow.xDocLinq.Save(SelectedFfxWindow._loadedFilePath);
                                FXR3_XMLR.FXR3EnhancedSerialization.XMLToFXR3(SelectedFfxWindow.xDocLinq).Write(SelectedFfxWindow._loadedFilePath.Substring(0, SelectedFfxWindow._loadedFilePath.Length - 4));
                            }
                            else if (SelectedFfxWindow._loadedFilePath.EndsWith(".fxr"))
                            {
                                SelectedFfxWindow.xDocLinq.Save($"{SelectedFfxWindow._loadedFilePath}.xml");
                                FXR3_XMLR.FXR3EnhancedSerialization.XMLToFXR3(SelectedFfxWindow.xDocLinq).Write(SelectedFfxWindow._loadedFilePath);
                            }
                        }
                        catch (Exception exception)
                        {
                            ShowExceptionPopup("ERROR: FFX saving failed", exception);
                        }
                    }
                    if (ImGui.MenuItem("Save as", OpenFfXs.Any()))
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
                                    FXR3_XMLR.FXR3EnhancedSerialization.XMLToFXR3(SelectedFfxWindow.xDocLinq).Write(saveFileDialog1.FileName);
                                }
                                else if (Path.GetExtension(saveFileDialog1.FileName) == ".xml")
                                {
                                    SelectedFfxWindow.xDocLinq.Save(saveFileDialog1.FileName);
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            ShowExceptionPopup("ERROR: FFX saving failed", exception);
                        }
                    }
                    if (ImGui.MenuItem("Load FFX Resources For Texture Display - Requires Relatively Good Hardware", FfxTextureHandler == null))
                    {
                        var ofd = new OpenFileDialog() { Title = "Open frpg_sfxbnd_commoneffects_resource.ffxbnd", Filter = "FfxRes|frpg_sfxbnd_commoneffects_resource.ffxbnd.dcx" };
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            FfxTextureHandler = new ImGuiFxrTextureHandler(BND4.Read(ofd.FileName));
                        }
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Edit"))
                {
                    if (ImGui.MenuItem("Undo", "Ctrl Z", false, SelectedFfxWindow != null && SelectedFfxWindow.actionManager.CanUndo()))
                    {
                        SelectedFfxWindow.actionManager.UndoAction();
                    }
                    if (ImGui.MenuItem("Redo", "Ctrl Y", false, SelectedFfxWindow != null && SelectedFfxWindow.actionManager.CanRedo()))
                    {
                        SelectedFfxWindow.actionManager.RedoAction();
                    }
                    if (ImGui.MenuItem("Extend Active FFX Treeview", SelectedFfxWindow != null))
                    {
                        SelectedFfxWindow.collapseExpandTreeView = true;
                    }
                    if (ImGuiAddons.isItemHoveredForTime(500, MainUserInterface.FrameRateForDelta, "HoverTimerTreeViewExpander"))
                    {
                        ImGui.Indent();
                        ImGui.Text("Holding Shift while clicking this button will expand properties aswell as the treeview itself.");
                        ImGui.Unindent();
                    }
                    if (ImGui.MenuItem("Search Actions..."))
                    {
                        IsSearchBarOpen = !IsSearchBarOpen;
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("UI Configs"))
                {
                    if (ImGuiAddons.BeginComboFixed("Theme Selector", _activeTheme))
                    {
                        foreach (string str in ThemeSelectorEntriesArray)
                        {
                            bool selected = str == _activeTheme;
                            if (ImGui.Selectable(str, selected))
                            {
                                _activeTheme = str;
                                Themes.ThemesSelectorPush(_activeTheme);
                                SelectedThemeConfig.WriteConfigsIni(_activeTheme);
                            }
                        }
                        ImGuiAddons.EndComboFixed();
                    }
                    ImGui.InputInt("Displayed Texture Size", ref TextureDisplaySize, 10, 100);
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
                    ImGuiAddons.ToggleButton("No ActionID Filter", ref Filtertoggle);
                    // No Action ID Filter End
                    if (ImGui.MenuItem("Lock DFXR3E Input", "Shift-Escape"))
                    {
                        MessageBox.Show("DFXR3E Inputs are locked, press OK to unlock");
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Experimental Memes"))
                {
                    if (ImGui.MenuItem("Experimental Meme Reload Lol", SelectedFfxWindow != null))
                    {
                        FFXReloader.Reload(SelectedFfxWindow.loadTimeFxr3.Write(), FXR3_XMLR.FXR3EnhancedSerialization.XMLToFXR3(SelectedFfxWindow.xDocLinq).Write());
                    }
                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }
        }

        private static void HotKeyGlobalListener()
        {
            { //Undo-Redo
                if (ImGui.GetIO().KeyShift && ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Escape)))
                {
                    MessageBox.Show("DFXR3E Inputs are locked, press OK to unlock");
                }
            }
        }
        private static void SubmitMainWindowUi()
        {
            ImGui.SetNextWindowDockID(_mainViewPortDockSpaceId, ImGuiCond.FirstUseEver);
            for (int i = 0; i < OpenFfXs.Count(); i++)
            {
                ImGui.SetNextWindowDockID(_mainViewPortDockSpaceId, ImGuiCond.FirstUseEver);
                if (OpenFfXs[i].RenderFFX())
                {
                    OpenFfXs[i].TreeviewExpandCollapseHandler(true);
                }
            }
            ImGui.SetNextWindowDockID(_mainViewPortDockSpaceId, ImGuiCond.FirstUseEver);
            if (ImGui.Begin("FFXEditor", ImGuiWindowFlags.NoMove))
            {
                if (SelectedFfxWindow != null)
                {
                    if (SelectedFfxWindow._showFFXEditorProperties || SelectedFfxWindow._showFFXEditorFields)
                    {
                        FfxEditor();
                    }
                }
                ImGui.End();
            }
        }
        private static void SubmitDockableUi()
        {
            { //Declare Standalone Windows here
                // Color Picker
                if (CPickerIsEnable)
                {
                    ImGui.SetNextWindowDockID(_mainViewPortDockSpaceId, ImGuiCond.FirstUseEver);
                    if (ImGui.Begin("FFX Color Picker", ref CPickerIsEnable))
                    {
                        Vector2 mEme = ImGui.GetWindowSize();
                        if (mEme.X > mEme.Y)
                        {
                            ImGui.SetNextItemWidth(mEme.Y * 0.80f);
                        }
                        ImGui.ColorPicker4("CPicker", ref CPicker, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoTooltip);
                        if (ImGui.IsItemDeactivatedAfterEdit())
                        {
                            var actionList = new List<Action>();

                            if (CPickerRed.Attribute(FFXHelperMethods.xsi + "type")?.Value == "FFXFieldInt" || CPickerGreen.Attribute(FFXHelperMethods.xsi + "type")?.Value == "FFXFieldInt" || CPickerBlue.Attribute(FFXHelperMethods.xsi + "type")?.Value == "FFXFieldInt" || CPickerAlpha.Attribute(FFXHelperMethods.xsi + "type")?.Value == "FFXFieldInt")
                            {
                                actionList.Add(new ModifyXAttributeString(CPickerRed.Attribute(FFXHelperMethods.xsi + "type"), "FFXFieldFloat"));
                                actionList.Add(new ModifyXAttributeString(CPickerGreen.Attribute(FFXHelperMethods.xsi + "type"), "FFXFieldFloat"));
                                actionList.Add(new ModifyXAttributeString(CPickerBlue.Attribute(FFXHelperMethods.xsi + "type"), "FFXFieldFloat"));
                                actionList.Add(new ModifyXAttributeString(CPickerAlpha.Attribute(FFXHelperMethods.xsi + "type"), "FFXFieldFloat"));
                            }
                            actionList.Add(new ModifyXAttributeFloat(CPickerRed.Attribute("Value"), CPicker.X));
                            actionList.Add(new ModifyXAttributeFloat(CPickerGreen.Attribute("Value"), CPicker.Y));
                            actionList.Add(new ModifyXAttributeFloat(CPickerBlue.Attribute("Value"), CPicker.Z));
                            actionList.Add(new ModifyXAttributeFloat(CPickerAlpha.Attribute("Value"), CPicker.W));

                            SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
                        }
                        ImGui.Separator();
                        ImGui.Text("Brightness Multiplier");
                        ImGui.SliderFloat("###Brightness Multiplier", ref ColorOverload, 0, 10f);
                        ImGui.SameLine();
                        if (ImGuiAddons.ButtonGradient("Multiply Color"))
                        {
                            List<Action> actions = new List<Action>();
                            CPicker.X *= ColorOverload;
                            CPicker.Y *= ColorOverload;
                            CPicker.Z *= ColorOverload;
                            actions.Add(new EditPublicCPickerVector4(new Vector4(CPicker.X *= ColorOverload, CPicker.Y *= ColorOverload, CPicker.Z *= ColorOverload, CPicker.W)));
                            actions.Add(new ModifyXAttributeFloat(CPickerRed.Attribute("Value"), CPicker.X));
                            actions.Add(new ModifyXAttributeFloat(CPickerGreen.Attribute("Value"), CPicker.Y));
                            actions.Add(new ModifyXAttributeFloat(CPickerBlue.Attribute("Value"), CPicker.Z));
                            SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actions));
                        }
                        ImGui.End();
                    }
                }
                // Keyboard Guide
                if (_keyboardInputGuide)
                {
                    ImGui.SetNextWindowDockID(_mainViewPortDockSpaceId, ImGuiCond.FirstUseEver);
                    ImGui.Begin("Keyboard Guide", ref _keyboardInputGuide, ImGuiWindowFlags.MenuBar);
                    ImGui.BeginMenuBar();
                    ImGui.EndMenuBar();
                    ImGui.ShowUserGuide();
                    ImGui.End();
                }
                if (_axbyDebugger)
                {
                    ImGui.SetNextWindowDockID(_mainViewPortDockSpaceId, ImGuiCond.FirstUseEver);
                    ImGui.Begin("axbxDebug", ref _axbyDebugger);
                    if (SelectedFfxWindow.NodeListEditor != null)
                    {
                        if (SelectedFfxWindow.NodeListEditor.Any() & (SelectedFfxWindow._showFFXEditorFields || SelectedFfxWindow._showFFXEditorProperties))
                        {
                            int integer = 0;
                            var xElement = SelectedFfxWindow.NodeListEditor.ElementAt(0).Parent;
                            if (xElement != null)
                                foreach (XNode node in xElement.Nodes())
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
                if (MainUserInterface.IsSearchBarOpen)
                {
                    var viewport = ImGui.GetMainViewport();

                    ImGui.SetNextWindowSize(new Vector2(300, 80));
                    ImGui.SetNextWindowPos(new Vector2(viewport.Pos.X + viewport.Size.X - 15, viewport.Pos.Y + 38), ImGuiCond.None, new Vector2(1, 0));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
                    if (ImGui.Begin("Action Search", ref MainUserInterface.IsSearchBarOpen, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove))
                    {
                        ImGui.SetNextItemWidth(190);
                        ImGui.InputText("Action Search", ref MainUserInterface.SearchBarString, 1024);
                        ImGui.Checkbox("Search By ID", ref MainUserInterface.IsSearchById);

                        ImGui.End();
                    }
                }
            }
        }
        public static void CloseOpenFfxWithoutSaving(FFXUI ffxUi)
        {
            ffxUi._loadedFilePath = "";
            ffxUi.xDocLinq = null;
            CPickerIsEnable = false;
            ffxUi._showFFXEditorFields = false;
            ffxUi._showFFXEditorProperties = false;
        }
        public static void ResetEditorSelection(FFXUI ffxUi)
        {
            ffxUi._showFFXEditorFields = false;
            ffxUi._showFFXEditorProperties = false;
            ffxUi.treeViewCurrentHighlighted = 0;
            CPickerIsEnable = false;
            if (ffxUi.NodeListEditor != null)
            {
                if (ffxUi.NodeListEditor.Any())
                {
                    ffxUi.NodeListEditor = FFXHelperMethods.XMLChildNodesValid(ffxUi.NodeListEditor.First().Parent);
                }
            }
        }

        private static void FfxEditor()
        {
            ImGui.SetNextWindowDockID(_mainViewPortDockSpaceId, ImGuiCond.FirstUseEver);
            if (SelectedFfxWindow._showFFXEditorProperties)
            {
                AxBySwapper();
                ImGui.NewLine();
                switch (SelectedFfxWindow.AxBy)
                {
                    case "A0B0":
                        break;
                    case "A16B4":
                        break;
                    case "A19B7":
                        break;
                    case "A32B8":
                        SelectedFfxWindow.FFXPropertyA32B8StaticScalar(SelectedFfxWindow.NodeListEditor);
                        break;
                    case "A35B11":
                        SelectedFfxWindow.FFXPropertyA35B11StaticColor(SelectedFfxWindow.NodeListEditor);
                        break;
                    case "A64B16":
                        SelectedFfxWindow.FFXPropertyA64B16ScalarInterpolationLinear(SelectedFfxWindow.NodeListEditor);
                        break;
                    case "A67B19":
                        SelectedFfxWindow.FFXPropertyA67B19ColorInterpolationLinear(SelectedFfxWindow.NodeListEditor);
                        break;
                    case "A96B24":
                        SelectedFfxWindow.FFXPropertyA96B24ScalarInterpolationWithCustomCurve(SelectedFfxWindow.NodeListEditor);
                        break;
                    case "A99B27":
                        SelectedFfxWindow.FFXPropertyA99B27ColorInterpolationWithCustomCurve(SelectedFfxWindow.NodeListEditor);
                        break;
                    case "A4163B35":
                        SelectedFfxWindow.FFXPropertyA67B19ColorInterpolationLinear(SelectedFfxWindow.NodeListEditor);
                        break;
                    case "A4160B32":
                        SelectedFfxWindow.FFXPropertyA64B16ScalarInterpolationLinear(SelectedFfxWindow.NodeListEditor);
                        break;
                    default:
                        ImGui.Text("ERROR: FFX Property Handler not found, using Default Handler.");
                        foreach (XElement node in SelectedFfxWindow.NodeListEditor)
                        {
                            string dataType = node.Attribute(FFXHelperMethods.xsi + "type")?.Value;
                            int nodeIndex = FFXHelperMethods.GetNodeIndexinParent(node);
                            if (dataType == "FFXFieldFloat")
                            {
                                SelectedFfxWindow.FloatInputDefaultNode(node, $"{dataType}##{nodeIndex}");
                            }
                            else if (dataType == "FFXFieldInt")
                            {
                                SelectedFfxWindow.IntInputDefaultNode(node, $"{dataType}##{nodeIndex}");
                            }
                        }
                        break;
                }
            }
            else if (SelectedFfxWindow._showFFXEditorFields)
            {
                DefParser.DefXMLParser(SelectedFfxWindow.NodeListEditor, SelectedFfxWindow.Fields[1], SelectedFfxWindow.Fields[0]);
            }
        }
        private static void AxBySwapper()
        {
            ImGui.BulletText("Input Type:");
            ImGui.SameLine();
            if (ImGuiAddons.BeginComboFixed("##Current AxBy", FFXHelperMethods.AxByToName(SelectedFfxWindow.AxBy)))
            {
                if (FFXHelperMethods.AxByColorArray.Contains(SelectedFfxWindow.AxBy))
                {
                    foreach (string str in FFXHelperMethods.AxByColorArray)
                    {
                        bool selected = SelectedFfxWindow.AxBy == str;
                        if (ImGui.Selectable(FFXHelperMethods.AxByToName(str), selected) & str != SelectedFfxWindow.AxBy)
                        {
                            XElement axbyElement = SelectedFfxWindow.ffxPropertyEditorElement;
                            if (str == "A19B7")
                            {
                                XElement templateXElement = DefParser.TemplateGetter("19", "7");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "19"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "7"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection(SelectedFfxWindow));

                                    SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
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

                                    actionList.Add(new ResetEditorSelection(SelectedFfxWindow));

                                    SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            else if (str == "A67B19")
                            {
                                if (SelectedFfxWindow.AxBy == "A4163B35")
                                {
                                    var actionListQuick = new List<Action>();

                                    actionListQuick.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "67"));
                                    actionListQuick.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "19"));

                                    actionListQuick.Add(new ResetEditorSelection(SelectedFfxWindow));

                                    SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionListQuick));
                                    return;
                                }
                                XElement templateXElement = DefParser.TemplateGetter("67", "19");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "67"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "19"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection(SelectedFfxWindow));

                                    SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
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

                                    actionList.Add(new ResetEditorSelection(SelectedFfxWindow));

                                    SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            else if (str == "A4163B35")
                            {
                                if (SelectedFfxWindow.AxBy == "A67B19")
                                {
                                    var actionListQuick = new List<Action>();

                                    actionListQuick.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "4163"));
                                    actionListQuick.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "35"));

                                    actionListQuick.Add(new ResetEditorSelection(SelectedFfxWindow));

                                    SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionListQuick));
                                    return;
                                }
                                XElement templateXElement = DefParser.TemplateGetter("4163", "35");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "4163"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "35"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection(SelectedFfxWindow));

                                    SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            return;
                        }
                    }
                }
                else if (FFXHelperMethods.AxByScalarArray.Contains(SelectedFfxWindow.AxBy))
                {
                    foreach (string str in FFXHelperMethods.AxByScalarArray)
                    {
                        bool selected = SelectedFfxWindow.AxBy == str;
                        if (ImGui.Selectable(FFXHelperMethods.AxByToName(str), selected) & str != SelectedFfxWindow.AxBy)
                        {
                            XElement axbyElement = SelectedFfxWindow.ffxPropertyEditorElement;
                            if (str == "A0B0")
                            {
                                XElement templateXElement = DefParser.TemplateGetter("0", "0");
                                if (templateXElement != null)
                                {
                                    var actionList = new List<Action>();

                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "0"));
                                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "0"));

                                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                                    actionList.Add(new ResetEditorSelection(SelectedFfxWindow));

                                    SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
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

                                    actionList.Add(new ResetEditorSelection(SelectedFfxWindow));

                                    SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
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

                                    actionList.Add(new ResetEditorSelection(SelectedFfxWindow));

                                    SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
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

                                    actionList.Add(new ResetEditorSelection(SelectedFfxWindow));

                                    SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
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

                                    actionList.Add(new ResetEditorSelection(SelectedFfxWindow));

                                    SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
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

                                    actionList.Add(new ResetEditorSelection(SelectedFfxWindow));

                                    SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
                                }
                            }
                            return;
                        }
                    }
                }
                else
                {
                    ImGui.Selectable(SelectedFfxWindow.AxBy, true);
                }
                ImGuiAddons.EndComboFixed();
            }
            ImGui.SameLine();
            if (ImGuiAddons.ButtonGradient("Flip C/S"))
            {
                XElement axbyElement = SelectedFfxWindow.ffxPropertyEditorElement;
                if (FFXHelperMethods.AxByColorArray.Contains(SelectedFfxWindow.AxBy))
                {
                    XElement templateXElement = DefParser.TemplateGetter("0", "0");
                    if (templateXElement != null)
                    {
                        var actionList = new List<Action>();

                        actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "0"));
                        actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "0"));

                        actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                        actionList.Add(new ResetEditorSelection(SelectedFfxWindow));

                        SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
                    }
                }
                else
                {
                    XElement templateXElement = DefParser.TemplateGetter("19", "7");
                    if (templateXElement == null) return;
                    var actionList = new List<Action>();

                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumA"), "19"));
                    actionList.Add(new ModifyXAttributeString(axbyElement.Attribute("TypeEnumB"), "7"));

                    actionList.Add(new XElementReplaceChildren(axbyElement, templateXElement));

                    actionList.Add(new ResetEditorSelection(SelectedFfxWindow));

                    SelectedFfxWindow.actionManager.ExecuteAction(new CompoundAction(actionList));
                }
            }
        }
    }
}
