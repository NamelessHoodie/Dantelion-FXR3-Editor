using ImGuiNET;
using ImGuiNETAddons;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DFXR3Editor.Dependencies
{
    public class FFXUI
    {
        public FFXUI(XDocument xdoc, string loadedFilePath)
        {
            xDocLinq = xdoc;
            _loadedFilePath = loadedFilePath;
        }

        public unsafe bool RenderFFX()
        {
            string windowTitle = Path.GetFileName(_loadedFilePath);
            bool windowOpen = true;
            int indexFfx = MainUserInterface.openFFXs.IndexOf(this);
            if (ImGui.Begin(windowTitle + "##" + indexFfx, ref windowOpen, (MainUserInterface.selectedFFXWindow == this ? ImGuiWindowFlags.UnsavedDocument : ImGuiWindowFlags.None)))
            {
                if (ImGui.IsWindowHovered() && (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseClicked(ImGuiMouseButton.Right)))
                {
                    MainUserInterface.selectedFFXWindow = this;
                }
                if (!windowOpen)
                {
                    MainUserInterface.openFFXs.Remove(this);
                    if (MainUserInterface.openFFXs.Any())
                    {
                        MainUserInterface.selectedFFXWindow = MainUserInterface.openFFXs.First();
                    }
                    else
                    {
                        MainUserInterface.selectedFFXWindow = null;
                    }
                    return false;
                }
                foreach (XElement xElement in xDocLinq.Descendants("RootEffectCall"))
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
                if (ImGui.GetIO().KeyCtrl & ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Z)) & actionManager.CanUndo())
                    actionManager.UndoAction();
                if (ImGui.GetIO().KeyCtrl & ImGui.IsKeyPressed(ImGui.GetKeyIndex(ImGuiKey.Y)) & actionManager.CanRedo())
                    actionManager.RedoAction();
            }
        }

        //XML
        public XDocument xDocLinq;
        public bool _showFFXEditorFields = false;
        public bool _showFFXEditorProperties = false;
        public uint treeViewCurrentHighlighted = 0;
        public IEnumerable<XElement> NodeListEditor;
        public string AxBy;
        public string[] Fields;
        public XElement ffxPropertyEditorElement;

        // Save/Load Path
        public string _loadedFilePath = "";

        public ActionManager actionManager = new ActionManager();
        public bool collapseExpandTreeView = false;
        //UI Builders
        public void PopulateTree(XElement root)
        {
            if (root != null)
            {
                int rootIndexInParent = FFXHelperMethods.GetNodeIndexinParent(root);
                IEnumerable<XNode> CommentsList = root.Nodes().Where(n => n.NodeType == XmlNodeType.Comment);
                XComment RootCommentNode = null;
                string RootComment = "";
                if (CommentsList.Any())
                {
                    RootCommentNode = (XComment)CommentsList.First();
                    RootComment = RootCommentNode.Value;
                }
                ImGui.PushID($"TreeFunctionlayer = {root.Name} ChildIndex = {rootIndexInParent}");
                IEnumerable<XElement> localNodeList = FFXHelperMethods.XMLChildNodesValid(root);
                if (root.Attribute(XName.Get("ActionID")) != null)
                {
                    if (FFXHelperMethods._actionIDSupported.Contains(root.Attribute("ActionID").Value) || MainUserInterface._filtertoggle)
                    {
                        TreeviewExpandCollapseHandler(false);
                        if (ImGuiAddons.TreeNodeTitleColored($"Action('{DefParser.ActionIDName(root.Attribute("ActionID").Value)}')", ImGuiAddons.GetStyleColorVec4Safe(ImGuiCol.CheckMark)))
                        {
                            TreeViewContextMenu(root, RootCommentNode, RootComment, "ActionID");
                            GetFFXProperties(root, "Properties1");
                            GetFFXProperties(root, "Properties2");
                            GetFFXFields(root, "Fields1");
                            GetFFXFields(root, "Fields2");
                            GetFFXFields(root, "Section10s");
                            ImGui.TreePop();
                        }
                        else
                            TreeViewContextMenu(root, RootCommentNode, RootComment, "ActionID");
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
                                                     where MainUserInterface._filtertoggle || FFXHelperMethods._actionIDSupported.Contains(node.Attribute("ActionID").Value)
                                                     select node;
                    if (tempnode.Any())
                    {
                        if (root.Name == "FFXEffectCallA")
                        {
                            DragAndDropXElement(root, FFXHelperMethods.EffectIDToName(root.Attribute("EffectID").Value), "a", "a");
                        }
                        TreeviewExpandCollapseHandler(false);
                        if (ImGuiAddons.TreeNodeTitleColored(FFXHelperMethods.EffectIDToName(root.Attribute("EffectID").Value), ImGuiAddons.GetStyleColorVec4Safe(ImGuiCol.TextDisabled)))
                        {
                            TreeViewContextMenu(root, RootCommentNode, RootComment, "FFXEffectCallA-B");
                            foreach (XElement node in localNodeList)
                            {
                                PopulateTree(node);
                            }
                            ImGui.TreePop();
                        }
                        else
                        {
                            TreeViewContextMenu(root, RootCommentNode, RootComment, "FFXEffectCallA-B");
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
        public void GetFFXProperties(XElement root, string PropertyType)
        {
            IEnumerable<XElement> localNodeList = from element0 in root.Elements(PropertyType)
                                                  from element1 in element0.Elements("FFXProperty")
                                                  select element1;
            if (localNodeList.Any())
            {
                if (ImGui.GetIO().KeyShift)
                {
                    TreeviewExpandCollapseHandler(false);
                }
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
                            string localIndex = $"{FFXHelperMethods.GetNodeIndexinParent(Node)}:";
                            string[] localSlot = DefParser.GetDefPropertiesArray(Node, PropertyType);
                            string localInput = FFXHelperMethods.AxByToName(localAxBy);
                            string localLabel = $"{localIndex} {localSlot[0]}: {localSlot[1]} {localInput}";
                            ImGui.PushID($"ItemForLoopNode = {localLabel}");
                            if (FFXHelperMethods.AxByScalarArray.Contains(localAxBy) || FFXHelperMethods.AxByColorArray.Contains(localAxBy))
                            {
                                IEnumerable<XElement> NodeListProcessing = FFXHelperMethods.XMLChildNodesValid(Node.Element("Fields"));
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
        public void ShowToolTipWiki(string toolTipTitle, string[] localSlot)
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
        public void GetFFXFields(XElement root, string fieldType)
        {
            IEnumerable<XElement> NodeListProcessing;
            if (fieldType == "Section10s")
            {
                NodeListProcessing = from element0 in root.Descendants(fieldType)
                                     from element1 in element0.Elements()
                                     from element2 in element1.Elements("Fields")
                                     from element3 in element2.Elements()
                                     select element3;
            }
            else
            {
                NodeListProcessing = from element0 in root.Descendants(fieldType)
                                     from element1 in element0.Elements()
                                     select element1;

                //NodeListProcessing = XMLChildNodesValid(root.Descendants(fieldType).First());
            }
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
        public void TreeViewContextMenu(XElement Node, XComment RootCommentNode, string RootComment, string nodeType)
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
                        if (ImGuiAddons.isItemHoveredForTime(500, MainUserInterface.FrameRateForDelta, "HoverTimer"))
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
                            lst.Add(new XElementRemove(Node));
                            lst.Add(new ResetEditorSelection(this));
                            actionManager.ExecuteAction(new CompoundAction(lst));
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
        public void XCommentInputStyled(XElement Node, XComment RootCommentNode, string RootComment)
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
                        if (ImGuiAddons.isItemHoveredForTime(500, MainUserInterface.FrameRateForDelta, "HoverTimer"))
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
        public void ShowToolTipSimple(string toolTipUID, string toolTipTitle, string toolTipText, bool isToolTipObjectSpawned, ImGuiPopupFlags clickButton)
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
                float maxToolTipWidth = (float)MainUserInterface._window.Width * 0.4f;
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
        public void ShowToolTipSimple(string toolTipUID, string toolTipTitle, string toolTipText, bool isToolTipObjectSpawned, float hoveredMsForTooltip)
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
            if (ImGuiAddons.isItemHoveredForTime(hoveredMsForTooltip, MainUserInterface.FrameRateForDelta, toolTipUID + "Hovered") & !ImGui.IsPopupOpen(localUID))
            {
                Vector2 mousePos = ImGui.GetMousePos();
                Vector2 localTextSize = ImGui.CalcTextSize(toolTipText);
                float maxToolTipWidth = (float)MainUserInterface._window.Width * 0.4f;
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
        public unsafe void DragAndDropXElement(XElement root, string dragAndDropName, string dragAndDropSourceType, string dragAndDropTargetType)
        {
            var a = Vector2.Add(ImGui.GetCursorPos(), ImGui.GetCursorScreenPos());
            //ImGui.GetWindowDrawList().AddTriangleFilled(a, new Vector2(a.X - 5, a.Y + 5), new Vector2(a.X + 5, a.Y +5), ImGui.GetColorU32(ImGuiCol.ButtonActive));
            ImGuiAddons.ButtonGradient("*", ImGui.CalcTextSize("*"));
            if (ImGui.BeginDragDropSource())
            {
                ImGuiAddons.TreeNodeTitleColored(dragAndDropName);
                ImGui.SetDragDropPayload(dragAndDropSourceType, IntPtr.Zero, 0);
                MainUserInterface.dragAndDropBuffer = root;
                ImGui.EndDragDropSource();
            }
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload(dragAndDropTargetType);
                if (payload.NativePtr != null)
                {
                    actionManager.ExecuteAction(new XElementAdd(root, MainUserInterface.dragAndDropBuffer));
                }
                ImGui.EndDragDropTarget();
            }
            ImGui.SameLine();
        }
        public void TreeviewExpandCollapseHandler(bool TreeViewFinalize)
        {
            if (collapseExpandTreeView != false)
            {
                if (TreeViewFinalize)
                {
                    collapseExpandTreeView = false;
                }
                else if (collapseExpandTreeView == true)
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

                    if (node.Attribute(FFXHelperMethods.xsi + "type").Value == "FFXFieldFloat")
                        actionList.Add(new ModifyXAttributeString(node.Attribute(FFXHelperMethods.xsi + "type"), "FFXFieldInt"));
                    actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), intNodeValue));

                    actionManager.ExecuteAction(new CompoundAction(actionList));
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

                    if (node.Attribute(FFXHelperMethods.xsi + "type").Value == "FFXFieldInt")
                        actionList.Add(new ModifyXAttributeString(node.Attribute(FFXHelperMethods.xsi + "type"), "FFXFieldFloat"));
                    actionList.Add(new ModifyXAttributeFloat(node.Attribute("Value"), nodeValue));

                    actionManager.ExecuteAction(new CompoundAction(actionList));
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

                    if (node.Attribute(FFXHelperMethods.xsi + "type").Value == "FFXFieldInt")
                        actionList.Add(new ModifyXAttributeString(node.Attribute(FFXHelperMethods.xsi + "type"), "FFXFieldFloat"));
                    actionList.Add(new ModifyXAttributeFloat(node.Attribute("Value"), floatNodeValue));

                    actionManager.ExecuteAction(new CompoundAction(actionList));
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

                    if (node.Attribute(FFXHelperMethods.xsi + "type").Value == "FFXFieldFloat")
                        actionList.Add(new ModifyXAttributeString(node.Attribute(FFXHelperMethods.xsi + "type"), "FFXFieldInt"));
                    actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), 0));

                    actionManager.ExecuteAction(new CompoundAction(actionList));
                }
                return;
            }
            if (ImGui.Checkbox(dataString, ref nodeValueBool))
            {
                var actionList = new List<Action>();

                if (node.Attribute(FFXHelperMethods.xsi + "type").Value == "FFXFieldFloat")
                    actionList.Add(new ModifyXAttributeString(node.Attribute(FFXHelperMethods.xsi + "type"), "FFXFieldInt"));
                actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), (nodeValueBool ? 1 : 0)));

                actionManager.ExecuteAction(new CompoundAction(actionList));
            }
        }
        public void IntComboDefaultNode(XElement node, string comboTitle, string[] entriesArrayValues, string[] entriesArrayNames)
        {
            int blendModeCurrent = Int32.Parse(node.Attribute("Value").Value);
            if (ImGui.Combo(comboTitle, ref blendModeCurrent, entriesArrayNames, entriesArrayNames.Length))
            {
                string tempstring = entriesArrayValues[blendModeCurrent];
                if (Int32.TryParse(tempstring, out int tempint))
                {
                    var actionList = new List<Action>();

                    if (node.Attribute(FFXHelperMethods.xsi + "type").Value == "FFXFieldFloat")
                        actionList.Add(new ModifyXAttributeString(node.Attribute(FFXHelperMethods.xsi + "type"), "FFXFieldInt"));
                    actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), tempint));

                    actionManager.ExecuteAction(new CompoundAction(actionList));
                }
            }
        }
        public void IntComboNotLinearDefaultNode(XElement node, string comboTitle, XElement EnumEntries)
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
            foreach (XElement node1 in FFXHelperMethods.XMLChildNodesValid(EnumEntries))
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
                        if (Int32.TryParse(FFXHelperMethods.XMLChildNodesValid(EnumEntries).ToArray()[i].Attribute("value").Value, out int safetyNetInt))
                        {
                            var actionList = new List<Action>();

                            if (node.Attribute(FFXHelperMethods.xsi + "type").Value == "FFXFieldFloat")
                                actionList.Add(new ModifyXAttributeString(node.Attribute(FFXHelperMethods.xsi + "type"), "FFXFieldInt"));
                            actionList.Add(new ModifyXAttributeInt(node.Attribute("Value"), safetyNetInt));

                            actionManager.ExecuteAction(new CompoundAction(actionList));
                        }
                    }
                }
                ImGui.EndCombo();
            }
        }

        //FFXPropertyHandler Functions Below here
        public void FFXPropertyA32B8StaticScalar(IEnumerable<XElement> NodeListEditor)
        {
            ImGui.BulletText("Single Static Scale Value:");
            ImGui.Indent();
            ImGui.Indent();
            FloatInputDefaultNode(NodeListEditor.ElementAt(0), "##Single Scalar Field");
            ImGui.Unindent();
            ImGui.Unindent();
        }
        public void FFXPropertyA35B11StaticColor(IEnumerable<XElement> NodeListEditor)
        {
            ImGui.BulletText("Single Static Color:");
            ImGui.Indent();
            ImGui.Indent();
            if (ImGui.ColorButton($"Static Color", new Vector4(float.Parse(NodeListEditor.ElementAt(0).Attribute("Value").Value), float.Parse(NodeListEditor.ElementAt(1).Attribute("Value").Value), float.Parse(NodeListEditor.ElementAt(2).Attribute("Value").Value), float.Parse(NodeListEditor.ElementAt(3).Attribute("Value").Value)), ImGuiColorEditFlags.AlphaPreview, new Vector2(30, 30)))
            {
                MainUserInterface._cPickerRed = NodeListEditor.ElementAt(0);
                MainUserInterface._cPickerGreen = NodeListEditor.ElementAt(1);
                MainUserInterface._cPickerBlue = NodeListEditor.ElementAt(2);
                MainUserInterface._cPickerAlpha = NodeListEditor.ElementAt(3);
                MainUserInterface._cPicker = new Vector4(float.Parse(MainUserInterface._cPickerRed.Attribute("Value").Value), float.Parse(MainUserInterface._cPickerGreen.Attribute("Value").Value), float.Parse(MainUserInterface._cPickerBlue.Attribute("Value").Value), float.Parse(MainUserInterface._cPickerAlpha.Attribute("Value").Value));
                MainUserInterface._cPickerIsEnable = true;
                ImGui.SetWindowFocus("FFX Color Picker");
            }
            ImGui.Unindent();
            ImGui.Unindent();
        }
        public void FFXPropertyA64B16ScalarInterpolationLinear(IEnumerable<XElement> NodeListEditor)
        {
            int StopsCount = Int32.Parse(NodeListEditor.ElementAt(0).Attribute("Value").Value);

            int Pos = 2;
            if (ImGui.TreeNodeEx($"Scalar Stages: Total number of stages = {StopsCount}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGuiAddons.ButtonGradient("Decrease Stops Count") & StopsCount > 2)
                {
                    var nodeFresh = NodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

                    IEnumerable<XElement> tempXElementIEnumerable = FFXHelperMethods.XMLChildNodesValid(nodeFresh);

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

                    IEnumerable<XElement> tempXElementIEnumerable = FFXHelperMethods.XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent);

                    tempXElementIEnumerable.ElementAt(Pos + (StopsCount * 2)).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FFXHelperMethods.xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );

                    tempXElementIEnumerable.ElementAt(Pos + StopsCount).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FFXHelperMethods.xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
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
        public void FFXPropertyA67B19ColorInterpolationLinear(IEnumerable<XElement> NodeListEditor)
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

                    IEnumerable<XElement> tempXElementIEnumerable = FFXHelperMethods.XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent);
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

                    IEnumerable<XElement> tempXElementIEnumerable = FFXHelperMethods.XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent);

                    int LocalPos = 8;

                    tempXElementIEnumerable.ElementAt(LocalPos + StopsCount).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FFXHelperMethods.xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );
                    for (int i = 0; i != 4; i++) //append 4 nodes at the end of the childnodes list
                    {
                        int localElementCount = tempXElementIEnumerable.Count();

                        tempXElementIEnumerable.ElementAt(localElementCount - 1).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FFXHelperMethods.xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
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
                            MainUserInterface._cPickerRed = NodeListEditor.ElementAt(PositionOffset);
                            MainUserInterface._cPickerGreen = NodeListEditor.ElementAt(PositionOffset + 1);
                            MainUserInterface._cPickerBlue = NodeListEditor.ElementAt(PositionOffset + 2);
                            MainUserInterface._cPickerAlpha = NodeListEditor.ElementAt(PositionOffset + 3);
                            MainUserInterface._cPicker = new Vector4(float.Parse(MainUserInterface._cPickerRed.Attribute("Value").Value), float.Parse(MainUserInterface._cPickerGreen.Attribute("Value").Value), float.Parse(MainUserInterface._cPickerBlue.Attribute("Value").Value), float.Parse(MainUserInterface._cPickerAlpha.Attribute("Value").Value));
                            MainUserInterface._cPickerIsEnable = true;
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
        public void FFXPropertyA96B24ScalarInterpolationWithCustomCurve(IEnumerable<XElement> NodeListEditor)
        {
            int StopsCount = Int32.Parse(NodeListEditor.ElementAt(0).Attribute("Value").Value);

            int Pos = 2;
            if (ImGui.TreeNodeEx($"Scalar Stages: Total number of stages = {StopsCount}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGuiAddons.ButtonGradient("Decrease Stops Count") & StopsCount > 2)
                {
                    var nodeFresh = NodeListEditor.ElementAt(0).Parent;

                    var nodeBackup = new XElement(nodeFresh);

                    IEnumerable<XElement> tempXElementIEnumerable = FFXHelperMethods.XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent);

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

                    IEnumerable<XElement> tempXElementIEnumerable = FFXHelperMethods.XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent);
                    tempXElementIEnumerable.ElementAt(Pos + (StopsCount * 2) + (StopsCount * 2)).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FFXHelperMethods.xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0")),
                        new XElement("FFXField", new XAttribute(FFXHelperMethods.xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );

                    tempXElementIEnumerable.ElementAt(Pos + (StopsCount * 2)).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FFXHelperMethods.xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );

                    tempXElementIEnumerable.ElementAt(Pos + StopsCount).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FFXHelperMethods.xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
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
        public void FFXPropertyA99B27ColorInterpolationWithCustomCurve(IEnumerable<XElement> NodeListEditor)
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

                    IEnumerable<XElement> tempXElementIEnumerable = FFXHelperMethods.XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent);
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

                    IEnumerable<XElement> tempXElementIEnumerable = FFXHelperMethods.XMLChildNodesValid(NodeListEditor.ElementAt(0).Parent);
                    int LocalPos = 8;

                    tempXElementIEnumerable.ElementAt(LocalPos + StopsCount).AddAfterSelf(
                        new XElement("FFXField", new XAttribute(FFXHelperMethods.xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                    );

                    for (int i = 0; i != 4; i++) //append 4 fields after last color alpha
                    {
                        tempXElementIEnumerable.ElementAt((LocalPos + StopsCount + 1) + 8 + 4 + (4 * (StopsCount - 3))).AddAfterSelf(
                            new XElement("FFXField", new XAttribute(FFXHelperMethods.xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
                        );
                        for (int i2 = 0; i2 != 2; i2++)
                        {
                            tempXElementIEnumerable.ElementAt(tempXElementIEnumerable.Count() - 1).AddAfterSelf(
                                new XElement("FFXField", new XAttribute(FFXHelperMethods.xsi + "type", "FFXFieldFloat"), new XAttribute("Value", "0"))
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
                            MainUserInterface._cPickerRed = NodeListEditor.ElementAt(PositionOffset);
                            MainUserInterface._cPickerGreen = NodeListEditor.ElementAt(PositionOffset + 1);
                            MainUserInterface._cPickerBlue = NodeListEditor.ElementAt(PositionOffset + 2);
                            MainUserInterface._cPickerAlpha = NodeListEditor.ElementAt(PositionOffset + 3);
                            MainUserInterface._cPicker = new Vector4(float.Parse(MainUserInterface._cPickerRed.Attribute("Value").Value), float.Parse(MainUserInterface._cPickerGreen.Attribute("Value").Value), float.Parse(MainUserInterface._cPickerBlue.Attribute("Value").Value), float.Parse(MainUserInterface._cPickerAlpha.Attribute("Value").Value));
                            MainUserInterface._cPickerIsEnable = true;
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
