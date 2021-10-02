using System.Collections.Generic;
using ImGuiNET;
using System.Numerics;
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
        private static XDocument TemplateDef = XDocument.Load(TemplateDefPath);
        private static XElement symbolsDefElements = FieldDef.Root.Element("symbolsDef");
        private static XElement enumsElement = FieldDef.Root.Element("Enums");
        private static readonly IEnumerable<XElement> actionIdElements = FieldDef.Root.Element("ActionIDs").Elements("ActionID");
        private static readonly XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        public static void DefXMLParser(IEnumerable<XElement> NodeListEditorIEnumerable, string actionID, string fieldType)
        {
            if (ImGui.BeginTable(actionID + fieldType, 3, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersInnerV))
            {
                XElement[] NodeListEditor = NodeListEditorIEnumerable.ToArray();
                ImGui.TableSetupColumn("dataType", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize);
                ImGui.TableSetupColumn("Text");
                ImGui.TableSetupColumn("Controls");
                IEnumerable<XElement> LocalNodeIEnumerable = from element0 in actionIdElements
                                                             where element0.Attribute("ID").Value == actionID
                                                             from element1 in element0.Elements(fieldType)
                                                             from element2 in element1.Elements()
                                                             select element2;
                if (LocalNodeIEnumerable.Any())
                {
                    XElement[] localNodeList = LocalNodeIEnumerable.ToArray();
                    for (int i = 0; i < NodeListEditor.Length; i++)
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
                                    MainUserInterface.SelectedFfxWindow.BooleanIntInputDefaultNode(NodeListEditor[i], "##" + index);
                                }
                                else if (localLoopNode.Attribute("isResourceTexture") != null)
                                {
                                    ImGui.Text(dataType);
                                    ImGui.TableNextColumn();
                                    ImGui.Text(name);
                                    ImGui.TableNextColumn();
                                    MainUserInterface.SelectedFfxWindow.TextureShowAndInput(NodeListEditor[i], "##" + index);
                                }
                                else if (localLoopAttributeEnum != null)
                                {
                                    XElement localLoopEnum = enumsElement.Element(localLoopAttributeEnum.Value);
                                    if (localLoopEnum != null & localLoopEnum.Elements().Any())
                                    {
                                        ImGui.Text(dataType);
                                        ImGui.TableNextColumn();
                                        ImGui.Text(name);
                                        ImGui.TableNextColumn();
                                        MainUserInterface.SelectedFfxWindow.IntComboNotLinearDefaultNode(NodeListEditor[i], "##" + index, localLoopEnum);
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
                                    MainUserInterface.SelectedFfxWindow.IntInputDefaultNode(NodeListEditor[i], "##" + index);
                                }
                            }
                            else if (dataType == "f32")
                            {
                                ImGui.Text(dataType);
                                ImGui.TableNextColumn();
                                ImGui.Text(name);
                                ImGui.TableNextColumn();
                                MainUserInterface.SelectedFfxWindow.FloatInputDefaultNode(NodeListEditor[i], "##" + index);
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
                                    MainUserInterface.SelectedFfxWindow.IntInputDefaultNode(NodeListEditor[i], "##" + index);
                                }
                                else if (unkdataType == "FFXFieldFloat")
                                {
                                    ImGui.Text("f32?");
                                    ImGui.TableNextColumn();
                                    ImGui.Text(name);
                                    ImGui.TableNextColumn();
                                    MainUserInterface.SelectedFfxWindow.FloatInputDefaultNode(NodeListEditor[i], "##" + index);
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
                    for (int i = 0; i < NodeListEditor.Length; i++)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        string index = i.ToString();
                        string name = "Unk" + i.ToString();
                        string unkdataType = NodeListEditor[i].Attribute(xsi + "type").Value;
                        if (unkdataType == "FFXFieldInt")
                        {
                            ImGui.Text("s32?");
                            ImGui.TableNextColumn();
                            ImGui.Text(name);
                            ImGui.TableNextColumn();
                            MainUserInterface.SelectedFfxWindow.IntInputDefaultNode(NodeListEditor[i], "##" + index);
                        }
                        else if (unkdataType == "FFXFieldFloat")
                        {
                            ImGui.Text("f32?");
                            ImGui.TableNextColumn();
                            ImGui.Text(name);
                            ImGui.TableNextColumn();
                            MainUserInterface.SelectedFfxWindow.FloatInputDefaultNode(NodeListEditor[i], "##" + index);
                        }
                    }
                }
                ImGui.EndTable();
            }
        }
        public static string DefXMLSymbolParser(string symbol)
        {
            IEnumerable<XAttribute> localSymbolIEnumerable = from element0 in symbolsDefElements.Elements()
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
            string localActionID = root.Parent.Parent.Attribute("ActionID").Value;
            int localPropertyIndex = FFXHelperMethods.GetNodeIndexinParent(root);
            IEnumerable<XElement> idk = from element0 in actionIdElements
                                        where element0.Attribute("ID").Value == localActionID
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
            List<string> ActionIDList = new List<string>();
            foreach (XElement Element in actionIdElements)
            {
                XAttribute Attribute = Element.Attribute("ID");
                if (Attribute != null)
                {
                    ActionIDList.Add(Attribute.Value);
                }
            }
            return ActionIDList.ToArray();
        }
        public static (string name, string description) ActionIDNameAndDescription(string ActionID)
        {
            IEnumerable<XElement> actionIDsMatch = actionIdElements.Where(i => i.Attribute("ID").Value == ActionID);
            if (actionIDsMatch.Any())
            {
                var actionIDXElement = actionIDsMatch.First();
                return (actionIDXElement.Attribute("name").Value, actionIDXElement.Attribute("description")?.Value ?? "");
            }
            else
            {
                return (ActionID, "");
            }
        }
        public static XElement TemplateGetter(string TypeEnumA, string TypeEnumB)
        {
            IEnumerable<XElement> templateXElements = from element in TemplateDef.Root.Element("AxByTemplates").Elements()
                                                      where (element.Attribute("TypeEnumA").Value == TypeEnumA & element.Attribute("TypeEnumB").Value == TypeEnumB)
                                                      select element;
            if (templateXElements.Any())
            {
                return new XElement(templateXElements.First());
            }
            return null;
        }
        public static void TemplateWriter(XElement newXEelement, string sectionName)
        {
            XElement templateSection = TemplateDef.Root.Element(sectionName);
            if (templateSection != null)
            {
                templateSection.Add(new XElement(newXEelement));
            }
            else
            {
                TemplateDef.Root.Add(new XElement(sectionName, new XElement(newXEelement)));
            }

            TemplateDef.Save(TemplateDefPath);
            TemplateDef = XDocument.Load(TemplateDefPath);
        }
    }
}
