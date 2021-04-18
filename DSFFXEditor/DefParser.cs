using System;
using System.Collections.Generic;
using System.Text;
using ImGuiNET;
using System.Numerics;
using System.Collections;
using System.Xml.Linq;
using System.Linq;

namespace DSFFXEditor
{
    class DefParser
    {
        private static XDocument FieldDef = XDocument.Load("Defs/DefActionID.xml");
        private static XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        public static void DefXMLParser(IEnumerable<XElement> NodeListEditorIEnumerable, string actionID, string fieldType)
        {
            if (ImGui.BeginTable(actionID + fieldType, 3, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersInnerV))
            {
                XElement[] NodeListEditor = NodeListEditorIEnumerable.ToArray();
                XElement localNodeenums = FieldDef.Descendants($"Enums").First();
                ImGui.TableSetupColumn("dataType", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
                ImGui.TableSetupColumn("Text");
                ImGui.TableSetupColumn("Controls");
                IEnumerable<XElement> LocalNodeIEnumerable = from element0 in FieldDef.Descendants("ActionIDs")
                                                             from element1 in element0.Descendants("ActionID")
                                                             where element1.Attribute("ID").Value == actionID
                                                             from element2 in element1.Descendants(fieldType)
                                                             from element3 in element2.Descendants()
                                                             select element3;
                if (LocalNodeIEnumerable.Count() > 0)
                {
                    XElement[] localNodeList = LocalNodeIEnumerable.ToArray();
                    for (int i = 0; i < NodeListEditor.Count(); i++)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        string name = "Unk" + i.ToString();
                        string dataType = "";
                        XElement localLoopNode = localNodeList[i];
                        if (localLoopNode != null)
                        {
                            XAttribute localLoopAttributeWiki = localLoopNode.Attribute("wiki");
                            XAttribute localLoopAttributeName = localLoopNode.Attribute("name");
                            XAttribute localLoopAttributeDataType = localLoopNode.Attribute("dataType");
                            if (localLoopAttributeName != null)
                            {
                                string tempName = localLoopAttributeName.Value;
                                if (tempName != "")
                                    name = tempName;
                            }
                            if (localLoopAttributeDataType != null)
                            {
                                string tempType = localLoopAttributeDataType.Value;
                                if (tempType != "")
                                    dataType = tempType;
                            }
                            if (dataType == "s32")
                            {
                                XAttribute localLoopAttributeEnum = localLoopNode.Attribute("enum");
                                if (localLoopNode.Attribute("isBool") != null)
                                {
                                    ImGui.Text(dataType);
                                    ImGui.TableNextColumn();
                                    ImGui.Text(name);
                                    ImGui.TableNextColumn();
                                    DSFFXGUIMain.BooleanIntInputDefaultNode(NodeListEditor[i], "##" + name);
                                }
                                else if (localLoopAttributeEnum != null)
                                {
                                    XElement localLoopEnum = localNodeenums.Descendants(localLoopAttributeEnum.Value).First();
                                    if (localLoopEnum != null & localLoopEnum.Elements().Count() > 0)
                                    {
                                        ImGui.Text(dataType);
                                        ImGui.TableNextColumn();
                                        ImGui.Text(name);
                                        ImGui.TableNextColumn();
                                        DSFFXGUIMain.IntComboNotLinearDefaultNode(NodeListEditor[i], "##" + name, localLoopEnum);
                                    }
                                    else
                                    {
                                        ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), $"ERROR: Field's Enum({localLoopAttributeEnum.Value}) is invalid.");
                                    }
                                }
                                else
                                {
                                    ImGui.Text(dataType);
                                    ImGui.TableNextColumn();
                                    ImGui.Text(name);
                                    ImGui.TableNextColumn();
                                    DSFFXGUIMain.IntInputDefaultNode(NodeListEditor[i], "##" + name);
                                }
                            }
                            else if (dataType == "f32")
                            {
                                ImGui.Text(dataType);
                                ImGui.TableNextColumn();
                                ImGui.Text(name);
                                ImGui.TableNextColumn();
                                DSFFXGUIMain.FloatInputDefaultNode(NodeListEditor[i], "##" + name);
                            }
                            else
                            {
                                string unkdataType = NodeListEditor[i].Attribute(xsi + "type").Value;
                                if (unkdataType == "FFXFieldInt")
                                {
                                    ImGui.Text("s32?");
                                    ImGui.TableNextColumn();
                                    ImGui.Text(name);
                                    ImGui.TableNextColumn();
                                    DSFFXGUIMain.IntInputDefaultNode(NodeListEditor[i], "##" + name);
                                }
                                else if (unkdataType == "FFXFieldFloat")
                                {
                                    ImGui.Text("f32?");
                                    ImGui.TableNextColumn();
                                    ImGui.Text(name);
                                    ImGui.TableNextColumn();
                                    DSFFXGUIMain.FloatInputDefaultNode(NodeListEditor[i], "##" + name);
                                }
                            }
                            if (localLoopAttributeWiki != null)
                            {
                                ImGui.SameLine();
                                DSFFXGUIMain.ShowToolTipSimple(i.ToString(), $"{fieldType}: ToolTip:", localLoopAttributeWiki.Value, true, ImGuiPopupFlags.MouseButtonRight);
                            }
                        }
                        else
                        {
                            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "ERROR: Field Definition is missing");
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < NodeListEditor.Count(); i++)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        string name = "Unk" + i.ToString();
                        string unkdataType = NodeListEditor[i].Attribute(xsi + "type").Value;
                        if (unkdataType == "FFXFieldInt")
                        {
                            ImGui.Text("s32?");
                            ImGui.TableNextColumn();
                            ImGui.Text(name);
                            ImGui.TableNextColumn();
                            DSFFXGUIMain.IntInputDefaultNode(NodeListEditor[i], "##" + name);
                        }
                        else if (unkdataType == "FFXFieldFloat")
                        {
                            ImGui.Text("f32?");
                            ImGui.TableNextColumn();
                            ImGui.Text(name);
                            ImGui.TableNextColumn();
                            DSFFXGUIMain.FloatInputDefaultNode(NodeListEditor[i], "##" + name);
                        }
                    }
                }
                ImGui.EndTable();
            }
        }

        public static string DefXMLSymbolParser(string symbol)
        {
            IEnumerable<XAttribute> localSymbolIEnumerable = from element0 in FieldDef.Descendants($"symbolsDef")
                                                             from element1 in element0.Descendants("entry")
                                                             where element1.Attribute("symbol").Value == symbol
                                                             where element1.Attribute("def") != null
                                                             select element1.Attribute("def");
            if (localSymbolIEnumerable.Count() > 0)
            {
                return localSymbolIEnumerable.First().Value;
            }
            return "Symbol Not Found";
        }
        public static string[] GetDefPropertiesArray(XElement root, string fieldType)
        {
            string defaultType = "[u]";
            string defaultArg = "[u]";
            string defaultName = "Unk";
            string defaultwiki = null;
            int localActionID = Int32.Parse(root.Parent.Parent.Attribute("ActionID").Value);
            int localPropertyIndex = DSFFXGUIMain.GetNodeIndexinParent(root);
            IEnumerable<XElement> idk = from element0 in FieldDef.Descendants("ActionIDs")
                                        from element1 in element0.Descendants("ActionID")
                                        where element1.Attribute("ID").Value == localActionID.ToString()
                                        from element2 in element1.Descendants(fieldType)
                                        from element3 in element2.Descendants()
                                        select element3;
            ;
            if (idk.Count() > 0)
            {
                XElement[] localNodeList = idk.ToArray();
                XElement localLoopNode = localNodeList[localPropertyIndex];
                if (localLoopNode != null)
                {
                    XAttribute localLoopAttributetype = localLoopNode.Attribute("type");
                    XAttribute localLoopAttributearg = localLoopNode.Attribute("arg");
                    XAttribute localLoopAttributename = localLoopNode.Attribute("name");
                    XAttribute localLoopAttributewiki = localLoopNode.Attribute("wiki");
                    string type;
                    string arg;
                    string name;
                    string wiki;

                    if (localLoopAttributetype != null)
                    {
                        type = localLoopAttributetype.Value;
                        if (type == "")
                            type = defaultType;
                    }
                    else
                        type = defaultType;

                    if (localLoopAttributearg != null)
                    {
                        arg = localLoopAttributearg.Value;
                        if (arg == "")
                            arg = defaultArg;
                    }
                    else
                        arg = defaultArg;

                    if (localLoopAttributename != null)
                    {

                        name = localLoopAttributename.Value;
                        if (name == "")
                            name = defaultName;
                    }
                    else
                        name = defaultName;

                    if (localLoopAttributewiki != null)
                    {
                        wiki = localLoopAttributewiki.Value;
                    }
                    else
                        wiki = defaultwiki;

                    return new string[] { type, arg, name, wiki };
                }
            }
            return new string[] { "[u]", "[u]", "Unk" };
        }
    }
}
