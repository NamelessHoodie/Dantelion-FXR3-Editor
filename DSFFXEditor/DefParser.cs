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
        public static XmlDocument FieldDef = new XmlDocument();
        public static void LoadXml()
        {
            FieldDef.Load("Defs/FieldsDef.xml");
        }

        public static void DefXMLParser(XmlNodeList NodeListEditor, string actionID, string fieldType)
        {
            do
            {
                XmlNode localNodeenums = FieldDef.SelectSingleNode($"descendant::Enums");
                XmlNode localNodeactionIDs = FieldDef.SelectSingleNode($"descendant::ActionIDs");
                if (localNodeactionIDs != null & localNodeenums != null)
                {
                    XmlNode localNodeactionID = localNodeactionIDs.SelectSingleNode($"descendant::ActionID[@ID={actionID}]");
                    if (localNodeactionID != null)
                    {
                        XmlNode localnodeField = localNodeactionID.SelectSingleNode(fieldType);
                        if (localnodeField != null)
                        {
                            XmlNodeList localNodeList = localnodeField.ChildNodes;
                            if (localNodeList.Count > 0)
                            {
                                if (fieldType == "Fields1" || fieldType == "Fields2")
                                {
                                    DefXMLFieldsHandler(NodeListEditor,localNodeList,localNodeenums,localNodeactionIDs);
                                }
                                else
                                {

                                }
                            }
                            else
                            {
                                ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "ERROR: NodeList Contains no nodes.");
                            }
                            return;
                        }
                    }
                }
            } while (false);
            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "ERROR: Defs/FieldsDef.xml is not valid.");
        }

        public static void DefXMLFieldsHandler(XmlNodeList NodeListEditor, XmlNodeList localNodeList, XmlNode localNodeenums, XmlNode localNodeactionIDs)
        {
            for (int i = 0; i < NodeListEditor.Count; i++)
            {

                XmlNode localLoopNode = localNodeList[i];
                if (localLoopNode != null)
                {
                    XmlAttribute localLoopAttributeName = localLoopNode.Attributes["name"];
                    XmlAttribute localLoopAttributeDataType = localLoopNode.Attributes["dataType"];
                    if (localLoopAttributeName != null & localLoopAttributeDataType != null)
                    {
                        string name = localLoopAttributeName.Value;
                        string dataType = localLoopAttributeDataType.Value;
                        if (dataType == "s32")
                        {
                            XmlAttribute localLoopAttributeEnum = localLoopNode.Attributes["enum"];
                            if (localLoopNode.Attributes["isBool"] != null)
                            {
                                DSFFXGUIMain.BooleanIntInputDefaultNode(NodeListEditor[i], name);
                            }
                            else if (localLoopAttributeEnum != null)
                            {
                                XmlNode localLoopEnum = localNodeenums.SelectSingleNode($"descendant::{localLoopAttributeEnum.Value}");
                                if (localLoopEnum != null & localLoopEnum.HasChildNodes)
                                {
                                    DSFFXGUIMain.IntComboNotLinearDefaultNode(NodeListEditor[i], name, localLoopEnum);
                                }
                                else
                                {
                                    ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), $"ERROR: Field's Enum({localLoopAttributeEnum.Value}) is invalid.");
                                }
                            }
                            else
                            {
                                DSFFXGUIMain.IntInputDefaultNode(NodeListEditor[i], name);
                            }
                        }
                        else if (dataType == "f32")
                        {
                            DSFFXGUIMain.FloatInputDefaultNode(NodeListEditor[i], name);
                        }
                        else if (dataType == "u")
                        {
                            string unkdataType = NodeListEditor[i].Attributes[0].Value;
                            if (unkdataType == "FFXFieldInt")
                            {
                                DSFFXGUIMain.IntInputDefaultNode(NodeListEditor[i], name);
                            }
                            else if (unkdataType == "FFXFieldFloat")
                            {
                                DSFFXGUIMain.FloatInputDefaultNode(NodeListEditor[i], name);
                            }
                        }
                        else
                        {
                            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "ERROR: Field's Data Type is invalid");
                        }
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "ERROR: Field's Data Type/Name Attribute is missing");
                    }
                }
                else
                {
                    ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "ERROR: Field Definition is missing");
                }
            }
        }
        public static void DefXMLPropertiesHandler(XmlNodeList NodeListEditor, XmlNodeList localNodeList, XmlNode localNodeenums, XmlNode localNodeactionIDs)
        {
            for (int i = 0; i < NodeListEditor.Count; i++)
            {

                XmlNode localLoopNode = localNodeList[i];
                if (localLoopNode != null)
                {
                    XmlAttribute localLoopAttributeName = localLoopNode.Attributes["name"];
                    XmlAttribute localLoopAttributeDataType = localLoopNode.Attributes["dataType"];
                    if (localLoopAttributeName != null & localLoopAttributeDataType != null)
                    {
                        string name = localLoopAttributeName.Value;
                        string dataType = localLoopAttributeDataType.Value;
                        if (dataType == "s32")
                        {
                            XmlAttribute localLoopAttributeEnum = localLoopNode.Attributes["enum"];
                            if (localLoopNode.Attributes["isBool"] != null)
                            {
                                DSFFXGUIMain.BooleanIntInputDefaultNode(NodeListEditor[i], name);
                            }
                            else if (localLoopAttributeEnum != null)
                            {
                                XmlNode localLoopEnum = localNodeenums.SelectSingleNode($"descendant::{localLoopAttributeEnum.Value}");
                                if (localLoopEnum != null & localLoopEnum.HasChildNodes)
                                {
                                    DSFFXGUIMain.IntComboNotLinearDefaultNode(NodeListEditor[i], name, localLoopEnum);
                                }
                                else
                                {
                                    ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), $"ERROR: Field's Enum({localLoopAttributeEnum.Value}) is invalid.");
                                }
                            }
                            else
                            {
                                DSFFXGUIMain.IntInputDefaultNode(NodeListEditor[i], name);
                            }
                        }
                        else if (dataType == "f32")
                        {
                            DSFFXGUIMain.FloatInputDefaultNode(NodeListEditor[i], name);
                        }
                        else if (dataType == "u")
                        {
                            string unkdataType = NodeListEditor[i].Attributes[0].Value;
                            if (unkdataType == "FFXFieldInt")
                            {
                                DSFFXGUIMain.IntInputDefaultNode(NodeListEditor[i], name);
                            }
                            else if (unkdataType == "FFXFieldFloat")
                            {
                                DSFFXGUIMain.FloatInputDefaultNode(NodeListEditor[i], name);
                            }
                        }
                        else
                        {
                            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "ERROR: Field's Data Type is invalid");
                        }
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "ERROR: Field's Data Type/Name Attribute is missing");
                    }
                }
                else
                {
                    ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "ERROR: Field Definition is missing");
                }
            }
        }

    }
}
