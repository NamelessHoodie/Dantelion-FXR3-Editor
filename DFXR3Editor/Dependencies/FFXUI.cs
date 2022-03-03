using ImGuiNET;
using ImGuiNETAddons;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DFXR3Editor.Dependencies
{
    public class Ffxui
    {
        public Ffxui(XDocument xdoc, string loadedFilePath)
        {
            XDocLinq = xdoc;
            LoadedFilePath = loadedFilePath;
        }

        public unsafe bool RenderFfx()
        {
            string windowTitle = Path.GetFileName(LoadedFilePath);
            bool windowOpen = true;
            int indexFfx = MainUserInterface.OpenFfXs.IndexOf(this);
            if (ImGui.Begin(windowTitle + "##" + indexFfx, ref windowOpen, (MainUserInterface.SelectedFfxWindow == this ? ImGuiWindowFlags.UnsavedDocument : ImGuiWindowFlags.None)))
            {
                if (ImGui.IsWindowHovered() && (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseClicked(ImGuiMouseButton.Right)))
                {
                    MainUserInterface.SelectedFfxWindow = this;
                }
                if (!windowOpen)
                {
                    MainUserInterface.OpenFfXs.Remove(this);
                    if (MainUserInterface.OpenFfXs.Any())
                    {
                        MainUserInterface.SelectedFfxWindow = MainUserInterface.OpenFfXs.First();
                    }
                    else
                    {
                        MainUserInterface.SelectedFfxWindow = null;
                    }
                    return false;
                }
                foreach (XElement xElement in XDocLinq.Descendants("RootEffectCall"))
                {
                    PopulateTree(xElement);
                }
                ImGui.End();
            }
            return true;
        }

        //Logic 
        public void HotkeyListener()
        {
            { //Undo-Redo
                if (ImGui.GetIO().KeyCtrl & ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Z)) & ActionManager.CanUndo())
                    ActionManager.UndoAction();
                if (ImGui.GetIO().KeyCtrl & ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Y)) & ActionManager.CanRedo())
                    ActionManager.RedoAction();
            }
        }

        //XML
        public XDocument XDocLinq;
        public bool ShowFfxEditorFields = false;
        public bool ShowFfxEditorProperties = false;
        public uint TreeViewCurrentHighlighted = 0;
        public IEnumerable<XElement> NodeListEditor;
        public string AxBy;
        public string[] Fields;
        public XElement FfxPropertyEditorElement;

        // Save/Load Path
        public string LoadedFilePath = "";

        public ActionManager ActionManager = new ActionManager();
        public bool CollapseExpandTreeView = false;
        //UI Builders
        public void PopulateTree(XElement root)
        {
            if (root != null)
            {
                int rootIndexInParent = FfxHelperMethods.GetNodeIndexinParent(root);
                IEnumerable<XNode> commentsList = root.Nodes().Where(n => n.NodeType == XmlNodeType.Comment);
                XComment rootCommentNode = null;
                string rootComment = "";
                if (commentsList.Any())
                {
                    rootCommentNode = (XComment)commentsList.First();
                    rootComment = rootCommentNode.Value;
                }
                ImGui.PushID($"TreeFunctionlayer = {root.Name} ChildIndex = {rootIndexInParent}");
                IEnumerable<XElement> localNodeList = FfxHelperMethods.XmlChildNodesValid(root);
                if (root.Attribute("ActionID") != null)
                {
                    //actionIdDef.name.Replace(" ", "").Contains(MainUserInterface._SearchBarString.Replace(" ", "")
                    var actionNumericId = root.Attribute("ActionID").Value;
                    var actionIdDef = DefParser.ActionIdNameAndDescription(actionNumericId);
                    bool showActionSearch;
                    if (!MainUserInterface.IsSearchById)
                    {
                        var searchBarSpaceless = MainUserInterface.SearchBarString.Replace(" ", "").ToLower();
                        var actionNameSpaceless = actionIdDef.name.Replace(" ", "").ToLower();
                        showActionSearch = searchBarSpaceless.Length > 0 ? actionNameSpaceless.Contains(searchBarSpaceless) : true;
                    }
                    else
                    {
                        var searchBarSpaceless = MainUserInterface.SearchBarString.Replace(" ", "");
                        var actionId = actionNumericId;
                        showActionSearch = searchBarSpaceless.Length > 0 ? actionId.StartsWith(searchBarSpaceless) : true;
                    }

                    if (MainUserInterface.IsSearchBarOpen ? showActionSearch && FfxHelperMethods.ActionIdSupported.Contains(actionNumericId) : FfxHelperMethods.ActionIdSupported.Contains(actionNumericId) || MainUserInterface.Filtertoggle)
                    {
                        TreeviewExpandCollapseHandler(false);
                        if (ImGuiAddons.TreeNodeTitleColored($"Action('{actionIdDef.name}')", ImGuiAddons.GetStyleColorVec4Safe(ImGuiCol.CheckMark)))
                        {
                            ShowToolTipSimple("", $"Action ID={actionNumericId} Name={actionIdDef.name}", $"Description={actionIdDef.description}", false, 500);
                            TreeViewContextMenu(root, rootCommentNode, rootComment, "ActionID");
                            GetFfxProperties(root, "Properties1");
                            GetFfxProperties(root, "Properties2");
                            GetFfxFields(root, "Fields1");
                            GetFfxFields(root, "Fields2");
                            GetFfxFields(root, "Section10s");
                            ImGui.TreePop();
                        }
                        else
                        {
                            ShowToolTipSimple("", $"Action ID={actionNumericId} Name={actionIdDef.name}", $"Description={actionIdDef.description}", false, 500);
                            TreeViewContextMenu(root, rootCommentNode, rootComment, "ActionID");
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
                                                     where MainUserInterface.Filtertoggle || FfxHelperMethods.ActionIdSupported.Contains(node.Attribute("ActionID").Value)
                                                     select node;
                    if (tempnode.Any())
                    {
                        if (root.Name == "FFXEffectCallA")
                        {
                            DragAndDropXElement(root, FfxHelperMethods.EffectIdToName(root.Attribute("EffectID").Value), "a", "a");
                        }
                        TreeviewExpandCollapseHandler(false);
                        if (ImGuiAddons.TreeNodeTitleColored(FfxHelperMethods.EffectIdToName(root.Attribute("EffectID").Value), ImGuiAddons.GetStyleColorVec4Safe(ImGuiCol.TextDisabled)))
                        {
                            TreeViewContextMenu(root, rootCommentNode, rootComment, "FFXEffectCallA-B");
                            foreach (XElement node in localNodeList)
                            {
                                PopulateTree(node);
                            }
                            ImGui.TreePop();
                        }
                        else
                        {
                            TreeViewContextMenu(root, rootCommentNode, rootComment, "FFXEffectCallA-B");
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
                    TreeviewExpandCollapseHandler(false);
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
        public void GetFfxProperties(XElement root, string propertyType)
        {
            IEnumerable<XElement> localNodeList = from element0 in root.Elements(propertyType)
                                                  from element1 in element0.Elements("FFXProperty")
                                                  select element1;
            if (localNodeList.Any())
            {
                if (ImGui.GetIO().KeyShift)
                {
                    TreeviewExpandCollapseHandler(false);
                }
                if (ImGui.TreeNodeEx($"{propertyType}"))
                {
                    if (ImGui.BeginTable("##table2", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX))
                    {
                        ImGui.TableSetupColumn("Type");
                        ImGui.TableSetupColumn("Arg");
                        ImGui.TableSetupColumn("Field");
                        ImGui.TableSetupColumn("Input Type");
                        ImGui.TableHeadersRow();
                        foreach (XElement node in localNodeList)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            string localAxBy = $"A{node.Attribute("TypeEnumA").Value}B{node.Attribute("TypeEnumB").Value}";
                            string localIndex = $"{FfxHelperMethods.GetNodeIndexinParent(node)}:";
                            string[] localSlot = DefParser.GetDefPropertiesArray(node, propertyType);
                            string localInput = FfxHelperMethods.AxByToName(localAxBy);
                            string localLabel = $"{localIndex} {localSlot[0]}: {localSlot[1]} {localInput}";
                            ImGui.PushID($"ItemForLoopNode = {localLabel}");
                            IEnumerable<XElement> nodeListProcessing = FfxHelperMethods.XmlChildNodesValid(node.Element("Fields"));
                            uint idStorage = ImGui.GetID(localLabel);
                            ImGuiStoragePtr storage = ImGui.GetStateStorage();
                            bool selected = storage.GetBool(idStorage);
                            if (selected & idStorage != TreeViewCurrentHighlighted)
                            {
                                storage.SetBool(idStorage, false);
                                selected = false;
                            }
                            ImGui.Selectable($"{localSlot[0]}###{localLabel}", selected, ImGuiSelectableFlags.SpanAllColumns);
                            if (ImGui.IsItemClicked(ImGuiMouseButton.Left) & !selected)
                            {
                                TreeViewCurrentHighlighted = idStorage;
                                storage.SetBool(idStorage, true);
                                NodeListEditor = nodeListProcessing;
                                FfxPropertyEditorElement = node;
                                AxBy = localAxBy;
                                ShowFfxEditorProperties = true;
                                ShowFfxEditorFields = false;
                                ImGui.SetWindowFocus("FFXEditor");
                            }
                            ShowToolTipWiki("Wiki", localSlot);
                            ImGui.TableNextColumn();
                            ImGui.Text(localSlot[1]);
                            ImGui.TableNextColumn();
                            ImGui.Text(localSlot[2]);
                            ImGui.TableNextColumn();
                            ImGui.Text(localInput);
                            ImGui.PopID();
                        }
                        ImGui.EndTable();
                    }
                    ImGui.TreePop();
                }
            }
        }
        public void ShowToolTipWiki(string toolTipTitle, string[] localSlot)
        {
            string fullToolTip = "";
            string archetypeWiki = DefParser.DefXmlSymbolParser(localSlot[0]);
            string argumentsWiki = DefParser.DefXmlSymbolParser(localSlot[1]);
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
        public void GetFfxFields(XElement root, string fieldType)
        {
            IEnumerable<XElement> nodeListProcessing;
            if (fieldType == "Section10s")
            {
                nodeListProcessing = from element0 in root.Descendants(fieldType)
                                     from element1 in element0.Elements()
                                     from element2 in element1.Elements("Fields")
                                     from element3 in element2.Elements()
                                     select element3;
            }
            else
            {
                nodeListProcessing = from element0 in root.Descendants(fieldType)
                                     from element1 in element0.Elements()
                                     select element1;

                //NodeListProcessing = XMLChildNodesValid(root.Descendants(fieldType).First());
            }
            if (nodeListProcessing.Any())
            {
                uint idStorage = ImGui.GetID(fieldType);
                ImGuiStoragePtr storage = ImGui.GetStateStorage();
                bool selected = storage.GetBool(idStorage);
                if (selected & idStorage != TreeViewCurrentHighlighted)
                {
                    storage.SetBool(idStorage, false);
                    selected = false;
                }
                ImGuiTreeNodeFlags localTreeNodeFlags = ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth;
                if (selected)
                    localTreeNodeFlags |= ImGuiTreeNodeFlags.Selected;
                ImGui.TreeNodeEx($"{fieldType}", localTreeNodeFlags);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left) & !selected)
                {
                    TreeViewCurrentHighlighted = idStorage;
                    storage.SetBool(idStorage, true);
                    NodeListEditor = nodeListProcessing;
                    Fields = new string[] { fieldType, root.Attributes().ToArray()[0].Value };
                    ShowFfxEditorProperties = false;
                    ShowFfxEditorFields = true;
                    ImGui.SetWindowFocus("FFXEditor");
                }
            }
        }

        public void TreeViewContextMenu(XElement node, XComment rootCommentNode, string rootComment, string nodeType)
        {
            bool isComment = false;
            if (rootCommentNode != null)
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
                            node.Add(new XComment(rootComment));
                        }
                        if (ImGuiAddons.IsItemHoveredForTime(500, MainUserInterface.FrameRateForDelta, "HoverTimer"))
                        {
                            ImGui.Indent();
                            ImGui.Text("Adds a comment to the selected node.");
                            ImGui.Text("Remember to left click to modify!");
                            ImGui.Unindent();
                        }
                    }
                    if (nodeType != "ActionID")
                    {
                        if (ImGui.Selectable("Remove Node"))
                        {
                            var lst = new List<Action>();
                            lst.Add(new XElementRemove(node));
                            lst.Add(new ResetEditorSelection(this));
                            ActionManager.ExecuteAction(new CompoundAction(lst));
                        }
                    }
                    ImGui.EndPopup();
                }
            }

            if (isComment)
            {
                XCommentInputStyled(node, rootCommentNode, rootComment);
            }
        }
        public void XCommentInputStyled(XElement node, XComment rootCommentNode, string rootComment)
        {
            ImGui.SameLine();
            string textCommentLabel;
            if (rootComment != "")
                textCommentLabel = rootComment;
            else
                textCommentLabel = "*";

            uint idStorage = ImGui.GetID("CommentInput");
            ImGuiStoragePtr storage = ImGui.GetStateStorage();
            bool open = storage.GetBool(idStorage);
            Vector2 sizeText = ImGui.CalcTextSize(textCommentLabel);
            Vector2 imguiCursorPos = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(imguiCursorPos.X + 13f);
            ImDrawListPtr drawList = ImGuiNET.ImGui.GetWindowDrawList();
            Vector2 screenCursorPos = ImGuiNET.ImGui.GetCursorScreenPos();
            Vector2 p = new Vector2(screenCursorPos.X - 10f / 2f, screenCursorPos.Y - 5f / 2f);
            float commentBoxHeightTemp = sizeText.Y + 5f;
            float commentBoxWidthTemp = sizeText.X + 10f;
            Vector2 commentBoxSize = new Vector2(p.X + commentBoxWidthTemp, p.Y + commentBoxHeightTemp);
            drawList.AddRectFilled(p, commentBoxSize, ImGui.GetColorU32(ImGuiCol.FrameBg), 5f);
            drawList.AddRect(p, commentBoxSize, ImGui.GetColorU32(ImGuiCol.BorderShadow), 5f);
            if (open)
            {
                Vector2 avalaibleContentSpace = ImGui.GetContentRegionAvail();
                imguiCursorPos = ImGui.GetCursorPos();
                ImGui.SetCursorPos(new Vector2(imguiCursorPos.X - 2f, imguiCursorPos.Y - 3f));
                float inputBoxWidth = sizeText.X + 4f;
                if (inputBoxWidth > avalaibleContentSpace.X)
                    inputBoxWidth = avalaibleContentSpace.X;
                ImGui.SetNextItemWidth(inputBoxWidth);
                if (ImGui.InputText("##InputBox", ref rootComment, 256))
                {
                    rootCommentNode.Value = rootComment;
                }
                if ((!ImGui.IsItemHovered() & ImGui.IsMouseClicked(ImGuiMouseButton.COUNT)) || ImGui.IsItemDeactivated())
                {
                    storage.SetBool(idStorage, false);
                }
            }
            else
            {
                ImGui.Text(textCommentLabel);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    storage.SetBool(idStorage, true);
                }
                string popupName = "Comment Context Menu";
                ImGui.OpenPopupOnItemClick(popupName, ImGuiPopupFlags.MouseButtonRight);
                if (ImGui.IsPopupOpen(popupName))
                {
                    if (ImGui.BeginPopupContextWindow(popupName))
                    {
                        if (ImGui.Selectable("Remove Comment"))
                        {
                            rootCommentNode.Remove();
                        }
                        if (ImGuiAddons.IsItemHoveredForTime(500, MainUserInterface.FrameRateForDelta, "HoverTimer"))
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
        public void ShowToolTipSimple(string toolTipUid, string toolTipTitle, string toolTipText, bool isToolTipObjectSpawned, ImGuiPopupFlags clickButton)
        {
            float frameHeight = ImGui.GetFrameHeight();
            string localUid = toolTipUid + toolTipTitle;
            if (isToolTipObjectSpawned)
            {
                float cursorPosX = ImGui.GetCursorPosX();
                Vector2 screenCursorPos = ImGui.GetCursorScreenPos();
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                string text = "?";
                Vector2 sizeText = ImGui.CalcTextSize(text);

                float radius = frameHeight * 0.4f;

                ImGui.InvisibleButton(toolTipUid + "Invisible Button", sizeText);
                uint circleColor;
                if (ImGui.IsItemHovered())
                    circleColor = ImGui.GetColorU32(ImGuiCol.CheckMark);
                else
                    circleColor = ImGui.GetColorU32(ImGuiCol.TitleBgActive);
                Vector2 p = new Vector2(screenCursorPos.X + (sizeText.X / 2f), screenCursorPos.Y + (frameHeight / 2f));

                drawList.AddCircle(new Vector2(p.X, p.Y), radius, ImGui.GetColorU32(ImGuiCol.BorderShadow));
                drawList.AddCircleFilled(new Vector2(p.X, p.Y), radius, circleColor);

                ImGui.SameLine();
                ImGui.SetCursorPosX(cursorPosX);
                ImGui.Text(text);
            }
            ImGui.OpenPopupOnItemClick(localUid, clickButton);
            if (ImGui.IsPopupOpen(localUid))
            {
                Vector2 mousePos = ImGui.GetMousePos();
                Vector2 localTextSize = ImGui.CalcTextSize(toolTipText);
                float maxToolTipWidth = (float)MainUserInterface.Window.Width * 0.4f;
                Vector2 windowSize = new Vector2(maxToolTipWidth, localTextSize.Y);
                float windowWidth = mousePos.X;
                ImGui.SetNextWindowPos(new Vector2(windowWidth, mousePos.Y), ImGuiCond.Appearing);
                ImGui.SetNextWindowSize(windowSize, ImGuiCond.Appearing);
                if (ImGui.BeginPopupContextItem(localUid))
                {
                    ImGui.Text(toolTipTitle);
                    ImGui.NewLine();
                    ImGui.TextWrapped(toolTipText);
                    ImGui.EndPopup();
                }
            }
        }
        public void ShowToolTipSimple(string toolTipUid, string toolTipTitle, string toolTipText, bool isToolTipObjectSpawned, float hoveredMsForTooltip)
        {
            float frameHeight = ImGui.GetFrameHeight();
            string localUid = toolTipUid + toolTipTitle;
            if (isToolTipObjectSpawned)
            {
                float cursorPosX = ImGui.GetCursorPosX();
                Vector2 screenCursorPos = ImGui.GetCursorScreenPos();
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                string text = "?";
                Vector2 sizeText = ImGui.CalcTextSize(text);

                float radius = frameHeight * 0.4f;

                ImGui.InvisibleButton(toolTipUid + "Invisible Button", sizeText);
                uint circleColor;
                if (ImGui.IsItemHovered())
                    circleColor = ImGui.GetColorU32(ImGuiCol.CheckMark);
                else
                    circleColor = ImGui.GetColorU32(ImGuiCol.TitleBgActive);
                Vector2 p = new Vector2(screenCursorPos.X + (sizeText.X / 2f), screenCursorPos.Y + (frameHeight / 2f));

                drawList.AddCircle(new Vector2(p.X, p.Y), radius, ImGui.GetColorU32(ImGuiCol.BorderShadow));
                drawList.AddCircleFilled(new Vector2(p.X, p.Y), radius, circleColor);

                ImGui.SameLine();
                ImGui.SetCursorPosX(cursorPosX);
                ImGui.Text(text);
            }
            if (ImGuiAddons.IsItemHoveredForTime(hoveredMsForTooltip, MainUserInterface.FrameRateForDelta, toolTipUid + "Hovered") & !ImGui.IsPopupOpen(localUid))
            {
                Vector2 mousePos = ImGui.GetMousePos();
                Vector2 localTextSize = ImGui.CalcTextSize(toolTipText);
                float maxToolTipWidth = (float)MainUserInterface.Window.Width * 0.4f;
                Vector2 windowSize = new Vector2(maxToolTipWidth, localTextSize.Y);
                float windowWidth = mousePos.X;
                ImGui.SetNextWindowPos(new Vector2(windowWidth, mousePos.Y + frameHeight), ImGuiCond.Appearing);
                ImGui.SetNextWindowSize(windowSize, ImGuiCond.Appearing);
                if (ImGui.Begin(localUid, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar))
                {
                    ImGui.Text(toolTipTitle);
                    ImGui.NewLine();
                    ImGui.TextWrapped(toolTipText);
                    ImGui.End();
                }
            }
        }
        public unsafe void DragAndDropXElement(XElement root, string dragAndDropName, string dragAndDropSourceType, string dragAndDropTargetType)
        {
            var a = Vector2.Add(ImGui.GetCursorPos(), ImGui.GetCursorScreenPos());
            //ImGui.GetWindowDrawList().AddTriangleFilled(a, new Vector2(a.X - 5, a.Y + 5), new Vector2(a.X + 5, a.Y +5), ImGui.GetColorU32(ImGuiCol.ButtonActive));
            ImGuiAddons.ButtonGradient("*", ImGui.CalcTextSize("*"));
            if (ImGui.BeginDragDropSource())
            {
                ImGuiAddons.TreeNodeTitleColored(dragAndDropName);
                ImGui.SetDragDropPayload(dragAndDropSourceType, IntPtr.Zero, 0);
                MainUserInterface.DragAndDropBuffer = root;
                ImGui.EndDragDropSource();
            }
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload(dragAndDropTargetType);
                if (payload.NativePtr != null)
                {
                    ActionManager.ExecuteAction(new XElementAdd(root, MainUserInterface.DragAndDropBuffer));
                }
                ImGui.EndDragDropTarget();
            }
            ImGui.SameLine();
        }
        public void TreeviewExpandCollapseHandler(bool treeViewFinalize)
        {
            if (CollapseExpandTreeView != false)
            {
                if (treeViewFinalize)
                {
                    CollapseExpandTreeView = false;
                }
                else if (CollapseExpandTreeView == true)
                {
                    ImGui.SetNextItemOpen(true);

                }
            }
        }

        //Controls Editor
        public void IntInputDefaultNode(XElement node, string dataString)
        {
            string nodeValue = node.Attribute("Value").Value;
            ImGui.InputText(dataString, ref nodeValue, 10, ImGuiInputTextFlags.CharsDecimal);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                if (Int32.TryParse(nodeValue, out int intNodeValue))
                {
                    var actionList = new List<Action>();

                    if (node.Attribute(FfxHelperMethods.Xsi + "type").Value == "FFXFieldFloat")
                        actionList.Add(new ModifyXAttributeString(node.Attribute(FfxHelperMethods.Xsi + "type"), "FFXFieldInt"));
                    actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), intNodeValue));

                    ActionManager.ExecuteAction(new CompoundAction(actionList));
                }
            }
        }
        public void TextureShowAndInput(XElement node, string dataString)
        {
            string nodeValue = node.Attribute("Value").Value;
            ImGui.SetNextItemWidth(ImGui.CalcTextSize("000000").X);
            IntInputDefaultNode(node, dataString + "IntInputField");
            if (MainUserInterface.FfxTextureHandler != null)
            {
                if (int.TryParse(nodeValue, out int textureId))
                {
                    var a = MainUserInterface.FfxTextureHandler.GetFfxTextureIntPtr(textureId);
                    if (a.TextureExists)
                    {
                        ImGui.ImageButton(a.TextureHandle, new Vector2(MainUserInterface.TextureDisplaySize));
                    }
                    else
                    {
                        ImGui.SameLine();
                        ImGuiAddons.ButtonGradient("Texture Not Found, Press to select texture." + dataString + "Button");
                    }
                }
                string popupName = "Select Texture" + dataString + "PopUp";
                ImGui.OpenPopupOnItemClick(popupName, ImGuiPopupFlags.MouseButtonLeft);
                if (ImGui.IsPopupOpen(popupName))
                {
                    bool a = true;
                    ImGuiViewportPtr mainViewport = ImGui.GetMainViewport();
                    Vector2 popupSize = new Vector2(mainViewport.Size.X * 0.8f, mainViewport.Size.Y * 0.8f);
                    ImGui.SetNextWindowPos(new Vector2(mainViewport.Pos.X + mainViewport.Size.X * 0.5f, mainViewport.Pos.Y + mainViewport.Size.Y * 0.5f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
                    ImGui.SetNextWindowSize(popupSize);
                    if (ImGui.BeginPopupModal(popupName, ref a, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove))
                    {
                        if (ImGui.IsKeyDown(ImGui.GetKeyIndex(ImGuiKey.Escape)))
                        {
                            ImGui.CloseCurrentPopup();
                        }
                        for (int i = 0; i < MainUserInterface.FfxTextureHandler.FfxTexturesIdList.Count(); i++)
                        {
                            var str = MainUserInterface.FfxTextureHandler.FfxTexturesIdList[i].ToString();
                            var textureHandleAndBoolPair = MainUserInterface.FfxTextureHandler.GetFfxTextureIntPtr(int.Parse(str));
                            if (textureHandleAndBoolPair.TextureExists)
                            {
                                if (ImGui.ImageButton(textureHandleAndBoolPair.TextureHandle, new Vector2(MainUserInterface.TextureDisplaySize)))
                                {
                                    if (Int32.TryParse(str, out int intNodeValue))
                                    {
                                        var actionList = new List<Action>();

                                        if (node.Attribute(FfxHelperMethods.Xsi + "type").Value == "FFXFieldFloat")
                                            actionList.Add(new ModifyXAttributeString(node.Attribute(FfxHelperMethods.Xsi + "type"), "FFXFieldInt"));
                                        actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), intNodeValue));

                                        ActionManager.ExecuteAction(new CompoundAction(actionList));
                                        ImGui.CloseCurrentPopup();
                                    }
                                }
                                ImGui.SameLine();
                            }
                            ImGui.Text(str);
                        }
                        ImGui.EndPopup();
                    }
                }
            }
        }
        public void FloatSliderDefaultNode(XElement node, string dataString, float minimumValue, float maximumValue)
        {
            float nodeValue = float.Parse(node.Attribute("Value").Value);
            if (ImGui.SliderFloat(dataString, ref nodeValue, minimumValue, maximumValue))
            {
                if (ImGui.IsItemEdited())
                {
                    var actionList = new List<Action>();

                    if (node.Attribute(FfxHelperMethods.Xsi + "type").Value == "FFXFieldInt")
                        actionList.Add(new ModifyXAttributeString(node.Attribute(FfxHelperMethods.Xsi + "type"), "FFXFieldFloat"));
                    actionList.Add(new ModifyXAttributeFloat(node.Attribute("Value"), nodeValue));

                    ActionManager.ExecuteAction(new CompoundAction(actionList));
                }
            }
        }
        public void FloatInputDefaultNode(XElement node, string dataString)
        {
            string nodeValue = node.Attribute("Value").Value;
            ImGui.InputText(dataString, ref nodeValue, 16, ImGuiInputTextFlags.CharsDecimal);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                if (float.TryParse(nodeValue, out float floatNodeValue))
                {
                    var actionList = new List<Action>();

                    if (node.Attribute(FfxHelperMethods.Xsi + "type").Value == "FFXFieldInt")
                        actionList.Add(new ModifyXAttributeString(node.Attribute(FfxHelperMethods.Xsi + "type"), "FFXFieldFloat"));
                    actionList.Add(new ModifyXAttributeFloat(node.Attribute("Value"), floatNodeValue));

                    ActionManager.ExecuteAction(new CompoundAction(actionList));
                }
            }
        }
        public void BooleanIntInputDefaultNode(XElement node, string dataString)
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

                    if (node.Attribute(FfxHelperMethods.Xsi + "type").Value == "FFXFieldFloat")
                        actionList.Add(new ModifyXAttributeString(node.Attribute(FfxHelperMethods.Xsi + "type"), "FFXFieldInt"));
                    actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), 0));

                    ActionManager.ExecuteAction(new CompoundAction(actionList));
                }
                return;
            }
            if (ImGui.Checkbox(dataString, ref nodeValueBool))
            {
                var actionList = new List<Action>();

                if (node.Attribute(FfxHelperMethods.Xsi + "type").Value == "FFXFieldFloat")
                    actionList.Add(new ModifyXAttributeString(node.Attribute(FfxHelperMethods.Xsi + "type"), "FFXFieldInt"));
                actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), (nodeValueBool ? 1 : 0)));

                ActionManager.ExecuteAction(new CompoundAction(actionList));
            }
        }
        public void IntComboDefaultNode(XElement node, string comboTitle, string[] entriesArrayValues, string[] entriesArrayNames)
        {
            string blendModeCurrent = node.Attribute("Value").Value;
            if (ImGuiAddons.BeginComboFixed(comboTitle, blendModeCurrent))
            {
                for (int i = 0; i < entriesArrayNames.Count(); i++)
                {
                    if (ImGui.Selectable(entriesArrayNames[i]))
                    {
                        string tempstring = entriesArrayValues[i];
                        if (Int32.TryParse(tempstring, out int tempint))
                        {
                            var actionList = new List<Action>();

                            if (node.Attribute(FfxHelperMethods.Xsi + "type").Value == "FFXFieldFloat")
                                actionList.Add(new ModifyXAttributeString(node.Attribute(FfxHelperMethods.Xsi + "type"), "FFXFieldInt"));
                            actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), tempint));

                            ActionManager.ExecuteAction(new CompoundAction(actionList));
                        }
                    }
                }
                ImGuiAddons.EndComboFixed();
            }
        }
        public void IntComboNotLinearDefaultNode(XElement node, string comboTitle, XElement enumEntries)
        {
            string localSelectedItem;
            XElement currentNode = (from element in enumEntries.Descendants()
                                    where element.Attribute("value").Value == node.Attribute("Value").Value
                                    select element).First();
            if (currentNode != null)
                localSelectedItem = $"{currentNode.Attribute("value").Value}: {currentNode.Attribute("name").Value}";
            else
                localSelectedItem = $"{node.Attribute("Value").Value}: Not Enumerated";

            ArrayList localTempArray = new ArrayList();
            foreach (XElement node1 in FfxHelperMethods.XmlChildNodesValid(enumEntries))
            {
                localTempArray.Add($"{node1.Attribute("value").Value}: {node1.Attribute("name").Value}");
            }
            string[] localArray = new string[localTempArray.Count];
            localTempArray.CopyTo(localArray);

            if (ImGuiAddons.BeginComboFixed(comboTitle, localSelectedItem))
            {
                for (int i = 0; i < localArray.Length; i++)
                {
                    if (ImGui.Selectable(localArray[i]))
                    {
                        if (Int32.TryParse(FfxHelperMethods.XmlChildNodesValid(enumEntries).ToArray()[i].Attribute("value").Value, out int safetyNetInt))
                        {
                            var actionList = new List<Action>();

                            if (node.Attribute(FfxHelperMethods.Xsi + "type").Value == "FFXFieldFloat")
                                actionList.Add(new ModifyXAttributeString(node.Attribute(FfxHelperMethods.Xsi + "type"), "FFXFieldInt"));
                            actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), safetyNetInt));

                            ActionManager.ExecuteAction(new CompoundAction(actionList));
                        }
                    }
                }
                ImGuiAddons.EndComboFixed();
            }
        }

        //FFXPropertyHandler Functions Below here
        public void FfxPropertyA32B8StaticScalar(IEnumerable<XElement> nodeListEditor)
        {
            ImGui.BulletText("Single Static Scale Value:");
            ImGui.Indent();
            ImGui.Indent();
            FloatInputDefaultNode(nodeListEditor.ElementAt(0), "##Single Scalar Field");
            ImGui.Unindent();
            ImGui.Unindent();
        }
        public void FfxPropertyA35B11StaticColor(IEnumerable<XElement> nodeListEditor)
        {
            ImGui.BulletText("Single Static Color:");
            ImGui.Indent();
            ImGui.Indent();
            if (ImGui.ColorButton($"Static Color", new Vector4(float.Parse(nodeListEditor.ElementAt(0).Attribute("Value").Value), float.Parse(nodeListEditor.ElementAt(1).Attribute("Value").Value), float.Parse(nodeListEditor.ElementAt(2).Attribute("Value").Value), float.Parse(nodeListEditor.ElementAt(3).Attribute("Value").Value)), ImGuiColorEditFlags.AlphaPreview, new Vector2(30, 30)))
            {
                MainUserInterface.CPickerRed = nodeListEditor.ElementAt(0);
                MainUserInterface.CPickerGreen = nodeListEditor.ElementAt(1);
                MainUserInterface.CPickerBlue = nodeListEditor.ElementAt(2);
                MainUserInterface.CPickerAlpha = nodeListEditor.ElementAt(3);
                MainUserInterface.CPicker = new Vector4(float.Parse(MainUserInterface.CPickerRed.Attribute("Value").Value), float.Parse(MainUserInterface.CPickerGreen.Attribute("Value").Value), float.Parse(MainUserInterface.CPickerBlue.Attribute("Value").Value), float.Parse(MainUserInterface.CPickerAlpha.Attribute("Value").Value));
                MainUserInterface.CPickerIsEnable = true;
                ImGui.SetWindowFocus("FFX Color Picker");
            }
            ImGui.Unindent();
            ImGui.Unindent();
        }
        public void FfxPropertyA64B16ScalarInterpolationLinear(IEnumerable<XElement> nodeListEditor)
        {
            int stopsCount = Int32.Parse(nodeListEditor.ElementAt(0).Attribute("Value").Value);

            int pos = 2;
            if (ImGui.TreeNodeEx($"Scalar Stages: Total number of stages = {stopsCount}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGuiAddons.ButtonGradient("Decrease Stops Count") & stopsCount > 2)
                {
                    var nodeFresh = nodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

                    IEnumerable<XElement> tempXElementIEnumerable = FfxHelperMethods.XmlChildNodesValid(nodeFresh);

                    tempXElementIEnumerable.ElementAt(pos + (stopsCount * 2)).Remove();

                    tempXElementIEnumerable.ElementAt(pos + stopsCount).Remove();

                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (stopsCount - 1).ToString();

                    ActionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

                    stopsCount--;

                    nodeListEditor = tempXElementIEnumerable;
                }
                ImGui.SameLine();
                if (ImGuiAddons.ButtonGradient("Increase Stops Count") & stopsCount < 8)
                {
                    var nodeFresh = nodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

                    IEnumerable<XElement> tempXElementIEnumerable = FfxHelperMethods.XmlChildNodesValid(nodeListEditor.ElementAt(0).Parent);

                    tempXElementIEnumerable.ElementAt(pos + (stopsCount * 2)).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FfxHelperMethods.Xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );

                    tempXElementIEnumerable.ElementAt(pos + stopsCount).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FfxHelperMethods.Xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );

                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (stopsCount + 1).ToString();

                    ActionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

                    stopsCount++;

                    nodeListEditor = tempXElementIEnumerable;
                }
                for (int i = 0; i != stopsCount; i++)
                {
                    ImGui.Separator();
                    ImGui.NewLine();
                    { // Slider Stuff
                        ImGui.BulletText($"Stage {i + 1}: Position in time");
                        FloatSliderDefaultNode(nodeListEditor.ElementAt(i + 3), $"###Stage{i + 1}Slider1", 0.0f, 2.0f);
                    }

                    { // Scale Slider
                        ImGui.Indent();
                        int positionOffset = pos + stopsCount + (i + 1);
                        ImGui.Text($"Stage's Scale:");
                        ImGui.SameLine();
                        FloatSliderDefaultNode(nodeListEditor.ElementAt(positionOffset), $"###Stage{i + 1}Slider2", 0.0f, 5.0f);
                        ImGui.Unindent();
                    }
                    ImGui.NewLine();
                }
                ImGui.Separator();
                ImGui.TreePop();
            }
        }
        public void FfxPropertyA67B19ColorInterpolationLinear(IEnumerable<XElement> nodeListEditor)
        {

            int pos = 0;
            int stopsCount = Int32.Parse(nodeListEditor.ElementAt(0).Attribute("Value").Value);

            pos += 9;
            if (ImGui.TreeNodeEx($"Color Stages: Total number of stages = {stopsCount}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGuiAddons.ButtonGradient("Decrease Stops Count") & stopsCount > 2)
                {
                    var nodeFresh = nodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

                    IEnumerable<XElement> tempXElementIEnumerable = FfxHelperMethods.XmlChildNodesValid(nodeListEditor.ElementAt(0).Parent);
                    int localPos = 8;
                    for (int i = 0; i != 4; i++)
                    {
                        tempXElementIEnumerable.ElementAt((localPos + stopsCount + 1) + 8 + (4 * (stopsCount - 3))).Remove();
                    }
                    tempXElementIEnumerable.ElementAt(localPos + stopsCount).Remove();

                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (stopsCount - 1).ToString();

                    ActionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

                    stopsCount--;

                    nodeListEditor = tempXElementIEnumerable;
                }
                ImGui.SameLine();
                if (ImGuiAddons.ButtonGradient("Increase Stops Count") & stopsCount < 8)
                {
                    var nodeFresh = nodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

                    IEnumerable<XElement> tempXElementIEnumerable = FfxHelperMethods.XmlChildNodesValid(nodeListEditor.ElementAt(0).Parent);

                    int localPos = 8;

                    tempXElementIEnumerable.ElementAt(localPos + stopsCount).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FfxHelperMethods.Xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );
                    for (int i = 0; i != 4; i++) //append 4 nodes at the end of the childnodes list
                    {
                        int localElementCount = tempXElementIEnumerable.Count();

                        tempXElementIEnumerable.ElementAt(localElementCount - 1).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FfxHelperMethods.Xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );
                    }
                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (stopsCount + 1).ToString();

                    ActionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

                    stopsCount++;

                    nodeListEditor = tempXElementIEnumerable;
                }
                int localColorOffset = pos + 1;
                for (int i = 0; i != stopsCount; i++)
                {
                    ImGui.Separator();
                    ImGui.NewLine();
                    { // Slider Stuff
                        ImGui.BulletText($"Stage {i + 1}: Position in time");
                        FloatSliderDefaultNode(nodeListEditor.ElementAt(i + 9), $"###Stage{i + 1}Slider", 0.0f, 2.0f);
                    }

                    { // ColorButton
                        ImGui.Indent();
                        int positionOffset = localColorOffset + stopsCount - (i + 1);
                        ImGui.Text($"Stage's Color:");
                        ImGui.SameLine();
                        if (ImGui.ColorButton($"Stage Position {i}: Color", new Vector4(float.Parse(nodeListEditor.ElementAt(positionOffset).Attribute("Value").Value), float.Parse(nodeListEditor.ElementAt(positionOffset + 1).Attribute("Value").Value), float.Parse(nodeListEditor.ElementAt(positionOffset + 2).Attribute("Value").Value), float.Parse(nodeListEditor.ElementAt(positionOffset + 3).Attribute("Value").Value)), ImGuiColorEditFlags.AlphaPreview, new Vector2(30, 30)))
                        {
                            MainUserInterface.CPickerRed = nodeListEditor.ElementAt(positionOffset);
                            MainUserInterface.CPickerGreen = nodeListEditor.ElementAt(positionOffset + 1);
                            MainUserInterface.CPickerBlue = nodeListEditor.ElementAt(positionOffset + 2);
                            MainUserInterface.CPickerAlpha = nodeListEditor.ElementAt(positionOffset + 3);
                            MainUserInterface.CPicker = new Vector4(float.Parse(MainUserInterface.CPickerRed.Attribute("Value").Value), float.Parse(MainUserInterface.CPickerGreen.Attribute("Value").Value), float.Parse(MainUserInterface.CPickerBlue.Attribute("Value").Value), float.Parse(MainUserInterface.CPickerAlpha.Attribute("Value").Value));
                            MainUserInterface.CPickerIsEnable = true;
                            ImGui.SetWindowFocus("FFX Color Picker");
                        }
                        localColorOffset += 5;
                        ImGui.Unindent();
                    }
                    ImGui.NewLine();
                }
                ImGui.Separator();
                ImGui.TreePop();
            }
        }
        public void FfxPropertyA96B24ScalarInterpolationWithCustomCurve(IEnumerable<XElement> nodeListEditor)
        {
            int stopsCount = Int32.Parse(nodeListEditor.ElementAt(0).Attribute("Value").Value);

            int pos = 2;
            if (ImGui.TreeNodeEx($"Scalar Stages: Total number of stages = {stopsCount}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGuiAddons.ButtonGradient("Decrease Stops Count") & stopsCount > 2)
                {
                    var nodeFresh = nodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

                    IEnumerable<XElement> tempXElementIEnumerable = FfxHelperMethods.XmlChildNodesValid(nodeListEditor.ElementAt(0).Parent);

                    for (int i = 0; i < 2; i++)
                    {
                        tempXElementIEnumerable.ElementAt(pos + (stopsCount * 2) + (stopsCount * 2) - 1).Remove();
                    }

                    tempXElementIEnumerable.ElementAt(pos + (stopsCount * 2)).Remove();

                    tempXElementIEnumerable.ElementAt(pos + stopsCount).Remove();

                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (stopsCount - 1).ToString();

                    ActionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

                    stopsCount--;

                    nodeListEditor = tempXElementIEnumerable;
                }
                ImGui.SameLine();
                if (ImGuiAddons.ButtonGradient("Increase Stops Count") & stopsCount < 8)
                {
                    var nodeFresh = nodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

                    IEnumerable<XElement> tempXElementIEnumerable = FfxHelperMethods.XmlChildNodesValid(nodeListEditor.ElementAt(0).Parent);
                    tempXElementIEnumerable.ElementAt(pos + (stopsCount * 2) + (stopsCount * 2)).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FfxHelperMethods.Xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0")),
                        new XElement("FFXField", new XAttribute(FfxHelperMethods.Xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );

                    tempXElementIEnumerable.ElementAt(pos + (stopsCount * 2)).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FfxHelperMethods.Xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );

                    tempXElementIEnumerable.ElementAt(pos + stopsCount).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FfxHelperMethods.Xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );

                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (stopsCount + 1).ToString();

                    ActionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

                    stopsCount++;

                    nodeListEditor = tempXElementIEnumerable;
                }
                for (int i = 0; i != stopsCount; i++)
                {
                    ImGui.Separator();
                    ImGui.NewLine();
                    { // Slider Stuff
                        ImGui.BulletText($"Stage {i + 1}: Position in time");
                        FloatSliderDefaultNode(nodeListEditor.ElementAt(i + 3), $"###Stage{i + 1}Slider1", 0.0f, 2.0f);
                    }

                    { // Scale Slider
                        ImGui.Indent();
                        int positionOffset = pos + stopsCount + (i + 1);
                        ImGui.Text($"Stage's Scale:");
                        ImGui.SameLine();
                        FloatSliderDefaultNode(nodeListEditor.ElementAt(positionOffset), $"###Stage{i + 1}Slider2", 0.0f, 5.0f);
                        ImGui.Unindent();
                    }

                    { // Curve Slider
                        ImGui.Indent();
                        int positionOffset = pos + (stopsCount * 2) + ((i + 1) * 2 - 1);
                        ImGui.Text($"Stage's Curve Angle:");
                        ImGui.SameLine();
                        FloatSliderDefaultNode(nodeListEditor.ElementAt(positionOffset), $"###Stage{i + 1}Slider3", 0.0f, 5.0f);
                        ImGui.Unindent();
                    }
                    ImGui.NewLine();
                }
                ImGui.Separator();
                ImGui.TreePop();
            }
        }
        public void FfxPropertyA99B27ColorInterpolationWithCustomCurve(IEnumerable<XElement> nodeListEditor)
        {
            int pos = 0;
            int stopsCount = Int32.Parse(nodeListEditor.ElementAt(0).Attribute("Value").Value);
            pos += 9;

            if (ImGui.TreeNodeEx($"Color Stages: Total number of stages = {stopsCount}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGuiAddons.ButtonGradient("Decrease Stops Count") & stopsCount > 2)
                {
                    var nodeFresh = nodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

                    IEnumerable<XElement> tempXElementIEnumerable = FfxHelperMethods.XmlChildNodesValid(nodeListEditor.ElementAt(0).Parent);
                    int localPos = 8;

                    for (int i = 0; i != 4; i++)
                    {
                        tempXElementIEnumerable.ElementAt((localPos + stopsCount + 1) + 8 + (4 * (stopsCount - 3))).Remove();
                    }
                    for (int i = 0; i != 8; i++)
                    {
                        tempXElementIEnumerable.ElementAt(tempXElementIEnumerable.Count() - 1).Remove();
                    }
                    tempXElementIEnumerable.ElementAt(localPos + stopsCount).Remove();
                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (stopsCount - 1).ToString();

                    ActionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

                    stopsCount--;

                    nodeListEditor = tempXElementIEnumerable;
                }
                ImGui.SameLine();
                if (ImGuiAddons.ButtonGradient("Increase Stops Count") & stopsCount < 8)
                {
                    var nodeFresh = nodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

                    IEnumerable<XElement> tempXElementIEnumerable = FfxHelperMethods.XmlChildNodesValid(nodeListEditor.ElementAt(0).Parent);
                    int localPos = 8;

                    tempXElementIEnumerable.ElementAt(localPos + stopsCount).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FfxHelperMethods.Xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                    );

                    for (int i = 0; i != 4; i++) //append 4 fields after last color alpha
                    {
                        tempXElementIEnumerable.ElementAt((localPos + stopsCount + 1) + 8 + 4 + (4 * (stopsCount - 3))).AddAfterSelf(
                            new XElement("FFXField", new XAttribute(FfxHelperMethods.Xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );
                        for (int i2 = 0; i2 != 2; i2++)
                        {
                            tempXElementIEnumerable.ElementAt(tempXElementIEnumerable.Count() - 1).AddAfterSelf(
                                new XElement("FFXField", new XAttribute(FfxHelperMethods.Xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                            );
                        }
                    }
                    tempXElementIEnumerable.ElementAt(0).Attribute("Value").Value = (stopsCount + 1).ToString();

                    ActionManager.ExecuteAction(new XElementReplaceChildrenWithSnapshot(nodeFresh, nodeBackup));

                    stopsCount++;

                    nodeListEditor = tempXElementIEnumerable;
                }
                int localColorOffset = pos + 1;
                for (int i = 0; i != stopsCount; i++)
                {
                    ImGui.Separator();
                    ImGui.NewLine();
                    { // Slider Stuff
                        ImGui.BulletText($"Stage {i + 1}: Position in time");
                        FloatSliderDefaultNode(nodeListEditor.ElementAt(i + 9), $"###Stage{i + 1}Slider", 0.0f, 2.0f);
                    }

                    { // ColorButton
                        ImGui.Indent();
                        int positionOffset = localColorOffset + stopsCount - (i + 1);
                        ImGui.Text($"Stage's Color:");
                        ImGui.SameLine();
                        if (ImGui.ColorButton($"Stage Position {i}: Color", new Vector4(float.Parse(nodeListEditor.ElementAt(positionOffset).Attribute("Value").Value), float.Parse(nodeListEditor.ElementAt(positionOffset + 1).Attribute("Value").Value), float.Parse(nodeListEditor.ElementAt(positionOffset + 2).Attribute("Value").Value), float.Parse(nodeListEditor.ElementAt(positionOffset + 3).Attribute("Value").Value)), ImGuiColorEditFlags.AlphaPreview, new Vector2(30, 30)))
                        {
                            MainUserInterface.CPickerRed = nodeListEditor.ElementAt(positionOffset);
                            MainUserInterface.CPickerGreen = nodeListEditor.ElementAt(positionOffset + 1);
                            MainUserInterface.CPickerBlue = nodeListEditor.ElementAt(positionOffset + 2);
                            MainUserInterface.CPickerAlpha = nodeListEditor.ElementAt(positionOffset + 3);
                            MainUserInterface.CPicker = new Vector4(float.Parse(MainUserInterface.CPickerRed.Attribute("Value").Value), float.Parse(MainUserInterface.CPickerGreen.Attribute("Value").Value), float.Parse(MainUserInterface.CPickerBlue.Attribute("Value").Value), float.Parse(MainUserInterface.CPickerAlpha.Attribute("Value").Value));
                            MainUserInterface.CPickerIsEnable = true;
                            ImGui.SetWindowFocus("FFX Color Picker");
                        }
                        localColorOffset += 5;
                        ImGui.Unindent();
                    }

                    { // Slider Stuff for curvature
                        int localPos = 8;
                        int readpos = (localPos + stopsCount + 1) + 8 + 4 + (4 * (stopsCount - 3));
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
                                    FloatSliderDefaultNode(nodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    int localint = 2;
                                    ImGui.Text("Green:");
                                    ImGui.TableNextColumn();
                                    ImGui.Text("Stage's Curve Angle");
                                    ImGui.TableNextColumn();
                                    FloatSliderDefaultNode(nodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    int localint = 4;
                                    ImGui.Text("Blue:");
                                    ImGui.TableNextColumn();
                                    ImGui.Text("Stage's Curve Angle");
                                    ImGui.TableNextColumn();
                                    FloatSliderDefaultNode(nodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
                                }
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    int localint = 6;
                                    ImGui.Text("Alpha:");
                                    ImGui.TableNextColumn();
                                    ImGui.Text("Stage's Curve Angle");
                                    ImGui.TableNextColumn();
                                    FloatSliderDefaultNode(nodeListEditor.ElementAt(localproperfieldpos + localint), $"###Curve{localint}Stage{i + 1}FloatInput", 0.0f, 2.0f);
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
