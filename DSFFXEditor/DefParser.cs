using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using ImGuiNET;
using System.Numerics;
using System.Collections;

namespace DSFFXEditor
{
    class DefParser
    {
        private static XmlDocument FieldDef = new XmlDocument();
        public static void Initialize()
        {
            FieldDef.Load("Defs/DefActionID.xml");
        }

        public static void DefXMLParser(XmlNodeList NodeListEditor, string actionID, string fieldType)
        {
            if (ImGui.BeginTable(actionID + fieldType, 3, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersInnerV))
            {
                ImGui.TableSetupColumn("dataType", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
                ImGui.TableSetupColumn("Text");
                ImGui.TableSetupColumn("Controls");
                XmlNode localNodeenums = FieldDef.SelectSingleNode($"descendant::Enums");
                XmlNode localNodeactionIDs = FieldDef.SelectSingleNode($"descendant::ActionIDs");
                if (localNodeactionIDs != null & localNodeenums != null)
                {
                    XmlNode localNodeactionID = localNodeactionIDs.SelectSingleNode($"descendant::ActionID[@ID='{actionID}']");
                    if (localNodeactionID != null)
                    {
                        XmlNode localnodeField = localNodeactionID.SelectSingleNode(fieldType);
                        if (localnodeField != null)
                        {
                            XmlNodeList localNodeList = localnodeField.ChildNodes;
                            if (localNodeList.Count > 0)
                            {
                                for (int i = 0; i < NodeListEditor.Count; i++)
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    string name = "Unk" + i.ToString();
                                    string dataType = "";
                                    XmlNode localLoopNode = localNodeList[i];
                                    if (localLoopNode != null)
                                    {
                                        XmlAttribute localLoopAttributeWiki = localLoopNode.Attributes["wiki"];
                                        XmlAttribute localLoopAttributeName = localLoopNode.Attributes["name"];
                                        XmlAttribute localLoopAttributeDataType = localLoopNode.Attributes["dataType"];
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
                                            XmlAttribute localLoopAttributeEnum = localLoopNode.Attributes["enum"];
                                            if (localLoopNode.Attributes["isBool"] != null)
                                            {
                                                ImGui.Text(dataType);
                                                ImGui.TableNextColumn();
                                                ImGui.Text(name);
                                                ImGui.TableNextColumn();
                                                DSFFXGUIMain.BooleanIntInputDefaultNode(NodeListEditor[i], "##" + name);
                                            }
                                            else if (localLoopAttributeEnum != null)
                                            {
                                                XmlNode localLoopEnum = localNodeenums.SelectSingleNode($"descendant::{localLoopAttributeEnum.Value}");
                                                if (localLoopEnum != null & localLoopEnum.HasChildNodes)
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
                                            string unkdataType = NodeListEditor[i].Attributes[0].Value;
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
                                            DSFFXGUIMain.ShowToolTipSimple(i.ToString(),$"{fieldType}: ToolTip:", localLoopAttributeWiki.Value, true,ImGuiPopupFlags.MouseButtonRight);
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
                                ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "ERROR: NodeList Contains no nodes.");
                            }
                            ImGui.EndTable();
                            return;
                        }
                    }
                }
                for (int i = 0; i < NodeListEditor.Count; i++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    string name = "Unk" + i.ToString();
                    string unkdataType = NodeListEditor[i].Attributes[0].Value;
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
                ImGui.EndTable();
            }
        }

        public static string DefXMLSymbolParser(string symbol) 
        {
            XmlNode localSymbolsDefNode = FieldDef.SelectSingleNode($"descendant::symbolsDef");
            if (localSymbolsDefNode != null)
            {
                XmlNode localSymbol = localSymbolsDefNode.SelectSingleNode($"descendant::entry[@symbol='{symbol}']");
                if (localSymbol != null)
                {
                    XmlAttribute localDefAttribute = localSymbol.Attributes["def"];
                    if (localDefAttribute != null)
                    {
                        return localDefAttribute.Value;
                    }
                }
            }
            return "Symbol Not Found";
        }
        public static string[] GetDefPropertiesArray(XmlNode root, string fieldType)
        {
            string defaultType = "[u]";
            string defaultArg = "[u]";
            string defaultName = "Unk";
            string defaultwiki = null;
            int localActionID = Int32.Parse(root.ParentNode.ParentNode.Attributes[0].Value);
            int localPropertyIndex = DSFFXGUIMain.GetNodeIndexinParent(root);
            XmlNode localNodeactionIDs = FieldDef.SelectSingleNode($"descendant::ActionIDs");
            if (localNodeactionIDs != null)
            {
                XmlNode localNodeactionID = localNodeactionIDs.SelectSingleNode($"descendant::ActionID[@ID='{localActionID}']");
                if (localNodeactionID != null)
                {
                    XmlNode localnodeField = localNodeactionID.SelectSingleNode(fieldType);
                    if (localnodeField != null)
                    {
                        XmlNodeList localNodeList = localnodeField.ChildNodes;
                        if (localNodeList.Count > 0)
                        {
                            XmlNode localLoopNode = localNodeList[localPropertyIndex];
                            if (localLoopNode != null)
                            {
                                XmlAttribute localLoopAttributetype = localLoopNode.Attributes["type"];
                                XmlAttribute localLoopAttributearg = localLoopNode.Attributes["arg"];
                                XmlAttribute localLoopAttributename = localLoopNode.Attributes["name"];
                                XmlAttribute localLoopAttributewiki = localLoopNode.Attributes["wiki"];
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

                                return new string[] { type, arg, name, wiki};
                            }
                        }
                    }
                }
            }
            return new string[] { "[u]", "[u]", "Unk" };
        }
    }
}
