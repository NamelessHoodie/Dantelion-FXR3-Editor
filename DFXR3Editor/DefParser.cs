using System;
using System.Collections.Generic;
using System.Text;
using ImGuiNET;
using System.Numerics;
using System.Collections;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using DFXR3Editor.Dependencies;

namespace DFXR3Editor
{
    class DefParser
    {
        private static readonly string FieldDefPath = "Defs/DefActionID.xml";
        private static readonly string TemplateDefPath = "Defs/TemplateDef.xml";
        private static readonly XDocument FieldDef = XDocument.Load(FieldDefPath);
        private static XDocument _templateDef = XDocument.Load(TemplateDefPath);
        private static XElement _symbolsDefElements = FieldDef.Root.Element("symbolsDef");
        private static XElement _enumsElement = FieldDef.Root.Element("Enums");
        private static readonly IEnumerable<XElement> ActionIdElements = FieldDef.Root.Element("ActionIDs").Elements("ActionID");
        private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
        public static void DefXmlParser(IEnumerable<XElement> nodeListEditorIEnumerable, string actionId, string fieldType)
        {
            if (ImGui.BeginTable(actionId + fieldType, 3, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersInnerV))
            {
                XElement[] nodeListEditor = nodeListEditorIEnumerable.ToArray();
                ImGui.TableSetupColumn("dataType", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
                ImGui.TableSetupColumn("Text");
                ImGui.TableSetupColumn("Controls");
                IEnumerable<XElement> localNodeIEnumerable = from element0 in ActionIdElements
                                                             where element0.Attribute("ID").Value == actionId
                                                             from element1 in element0.Elements(fieldType)
                                                             from element2 in element1.Elements()
                                                             select element2;
                if (localNodeIEnumerable.Any())
                {
                    XElement[] localNodeList = localNodeIEnumerable.ToArray();
                    for (int i = 0; i < nodeListEditor.Length; i++)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        string index = i.ToString();
                        string name = "Unk" + index;
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
                                    MainUserInterface.SelectedFfxWindow.BooleanIntInputDefaultNode(nodeListEditor[i], "##" + index);
                                }
                                else if (localLoopNode.Attribute("isResourceTexture") != null)
                                {
                                    ImGui.Text(dataType);
                                    ImGui.TableNextColumn();
                                    ImGui.Text(name);
                                    ImGui.TableNextColumn();
                                    MainUserInterface.SelectedFfxWindow.TextureShowAndInput(nodeListEditor[i], "##" + index);
                                }
                                else if (localLoopAttributeEnum != null)
                                {
                                    XElement localLoopEnum = _enumsElement.Element(localLoopAttributeEnum.Value);
                                    if (localLoopEnum != null & localLoopEnum.Elements().Any())
                                    {
                                        ImGui.Text(dataType);
                                        ImGui.TableNextColumn();
                                        ImGui.Text(name);
                                        ImGui.TableNextColumn();
                                        MainUserInterface.SelectedFfxWindow.IntComboNotLinearDefaultNode(nodeListEditor[i], "##" + index, localLoopEnum);
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
                                    MainUserInterface.SelectedFfxWindow.IntInputDefaultNode(nodeListEditor[i], "##" + index);
                                }
                            }
                            else if (dataType == "f32")
                            {
                                ImGui.Text(dataType);
                                ImGui.TableNextColumn();
                                ImGui.Text(name);
                                ImGui.TableNextColumn();
                                MainUserInterface.SelectedFfxWindow.FloatInputDefaultNode(nodeListEditor[i], "##" + index);
                            }
                            else
                            {
                                string unkdataType = nodeListEditor[i].Attribute(Xsi + "type").Value;
                                if (unkdataType == "FFXFieldInt")
                                {
                                    ImGui.Text("s32?");
                                    ImGui.TableNextColumn();
                                    ImGui.Text(name);
                                    ImGui.TableNextColumn();
                                    MainUserInterface.SelectedFfxWindow.IntInputDefaultNode(nodeListEditor[i], "##" + index);
                                }
                                else if (unkdataType == "FFXFieldFloat")
                                {
                                    ImGui.Text("f32?");
                                    ImGui.TableNextColumn();
                                    ImGui.Text(name);
                                    ImGui.TableNextColumn();
                                    MainUserInterface.SelectedFfxWindow.FloatInputDefaultNode(nodeListEditor[i], "##" + index);
                                }
                            }
                            if (localLoopAttributeWiki != null)
                            {
                                ImGui.SameLine();
                                MainUserInterface.SelectedFfxWindow.ShowToolTipSimple(i.ToString(), $"{fieldType}: ToolTip:", localLoopAttributeWiki.Value, true, ImGuiPopupFlags.MouseButtonLeft);
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
                    for (int i = 0; i < nodeListEditor.Length; i++)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        string index = i.ToString();
                        string name = "Unk" + i.ToString();
                        string unkdataType = nodeListEditor[i].Attribute(Xsi + "type").Value;
                        if (unkdataType == "FFXFieldInt")
                        {
                            ImGui.Text("s32?");
                            ImGui.TableNextColumn();
                            ImGui.Text(name);
                            ImGui.TableNextColumn();
                            MainUserInterface.SelectedFfxWindow.IntInputDefaultNode(nodeListEditor[i], "##" + index);
                        }
                        else if (unkdataType == "FFXFieldFloat")
                        {
                            ImGui.Text("f32?");
                            ImGui.TableNextColumn();
                            ImGui.Text(name);
                            ImGui.TableNextColumn();
                            MainUserInterface.SelectedFfxWindow.FloatInputDefaultNode(nodeListEditor[i], "##" + index);
                        }
                    }
                }
                ImGui.EndTable();
            }
        }
        public static string DefXmlSymbolParser(string symbol)
        {
            IEnumerable<XAttribute> localSymbolIEnumerable = from element0 in _symbolsDefElements.Elements()
                                                             where element0.Attribute("symbol").Value == symbol
                                                             where element0.Attribute("def") != null
                                                             select element0.Attribute("def");
            if (localSymbolIEnumerable.Any())
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
            string localActionId = root.Parent.Parent.Attribute("ActionID").Value;
            int localPropertyIndex = FfxHelperMethods.GetNodeIndexinParent(root);
            IEnumerable<XElement> idk = from element0 in ActionIdElements
                                        where element0.Attribute("ID").Value == localActionId
                                        from element1 in element0.Elements(fieldType)
                                        from element2 in element1.Elements()
                                        select element2;
            ;
            if (idk.Any())
            {
                if (idk.Count() - 1 >= localPropertyIndex)
                {
                    XElement localLoopNode = idk.ElementAt(localPropertyIndex);
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
        public static string[] SupportedActionIDs()
        {
            List<string> actionIdList = new List<string>();
            foreach (XElement element in ActionIdElements)
            {
                XAttribute attribute = element.Attribute("ID");
                if (attribute != null)
                {
                    actionIdList.Add(attribute.Value);
                }
            }
            return actionIdList.ToArray();
        }
        public static (string name, string description) ActionIdNameAndDescription(string actionId)
        {
            IEnumerable<XElement> actionIDsMatch = ActionIdElements.Where(i => i.Attribute("ID").Value == actionId);
            if (actionIDsMatch.Any())
            {
                var actionIdxElement = actionIDsMatch.First();
                return (actionIdxElement.Attribute("name").Value, actionIdxElement.Attribute("description")?.Value ?? "");
            }
            else
            {
                return (actionId, "");
            }
        }
        public static XElement TemplateGetter(string typeEnumA, string typeEnumB)
        {
            IEnumerable<XElement> templateXElements = from element in _templateDef.Root.Element("AxByTemplates").Elements()
                                                      where (element.Attribute("TypeEnumA").Value == typeEnumA & element.Attribute("TypeEnumB").Value == typeEnumB)
                                                      select element;
            if (templateXElements.Any())
            {
                return new XElement(templateXElements.First());
            }
            return null;
        }
        public static void TemplateWriter(XElement newXEelement, string sectionName)
        {
            XElement templateSection = _templateDef.Root.Element(sectionName);
            if (templateSection != null)
            {
                templateSection.Add(new XElement(newXEelement));
            }
            else
            {
                _templateDef.Root.Add(new XElement(sectionName, new XElement(newXEelement)));
            }

            _templateDef.Save(TemplateDefPath);
            _templateDef = XDocument.Load(TemplateDefPath);
        }
    }
}
