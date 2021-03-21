using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using ImGuiNET;
using ImGuiNETAddons;

namespace DSFFXEditor
{
    class tempclass
    {
        private static void PopulateTree(XmlNode root)
        {
            if (root is XmlElement)
            {
                if (root.Attributes["ActionID"] != null)
                {
                    string[] _actionIDsFilter = { "600", "601", "602", "603", "604", "605", "606", "607", "609", "10012" };
                    if (_actionIDsFilter.Contains(root.Attributes[0].Value) || _filtertoggle)
                    {
                        TreeCounter++;
                        ImGui.PushID($"Tree{TreeCounter}");
                        if (ImGui.TreeNodeEx($"ActionID = {root.Attributes[0].Value}", ImGuiTreeNodeFlags.None))
                        {
                            foreach (XmlNode node in root.SelectNodes("descendant::FFXProperty"))
                            {
                                if ((node.Attributes[0].Value == "67" & node.Attributes[1].Value == "19") || (node.Attributes[0].Value == "35" & node.Attributes[1].Value == "11") || (node.Attributes[0].Value == "99" & node.Attributes[1].Value == "27") || (node.Attributes[0].Value == "4163" & node.Attributes[1].Value == "35"))
                                {
                                    if (ImGui.TreeNodeEx($"A{node.Attributes[0].Value}B{node.Attributes[1].Value}", ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.Leaf))
                                    {
                                        XmlNodeList NodeListProcessing = node.SelectNodes("Fields")[0].ChildNodes;
                                        ImGui.SameLine();
                                        if (ImGuiAddons.ButtonGradient("Edit Here"))
                                        {
                                            NodeListEditor = NodeListProcessing;
                                            AXBX = $"A{node.Attributes[0].Value}B{node.Attributes[1].Value}";
                                            _showFFXEditor = true;
                                        }
                                        ImGui.TreePop();
                                    }
                                }
                                else
                                {
                                    if (ImGui.TreeNodeEx($"A{node.Attributes[0].Value}B{node.Attributes[1].Value}", ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.Leaf))
                                    {
                                        ImGui.SameLine();
                                        if (ImGui.Button("Error: No Handler"))
                                        {
                                        }
                                        ImGui.TreePop();
                                    }
                                }
                            }
                            ImGui.TreePop();
                        }
                        ImGui.PopID();
                    }
                }
                else if (root.Name == "EffectAs" || root.Name == "EffectBs" || root.Name == "RootEffectCall" || root.Name == "Actions")
                {
                    if (root.HasChildNodes)
                        PopulateTree(root.FirstChild);
                }
                else if (root.Name == "FFXEffectCallA")
                {
                    if (root.HasChildNodes)
                    {
                        TreeCounter++;
                        ImGui.PushID($"Tree{TreeCounter}");
                        if (ImGui.TreeNodeEx($"FFX Container", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            //DoWork(root);
                            PopulateTree(root.FirstChild);
                            ImGui.TreePop();
                        }
                        ImGui.PopID();
                    }
                }
                else if (root.Name == "FFXEffectCallB")
                {
                    if (root.HasChildNodes)
                    {
                        TreeCounter++;
                        ImGui.PushID($"Tree{TreeCounter}");
                        if (ImGui.TreeNodeEx($"FFX Call", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            //DoWork(root);
                            PopulateTree(root.FirstChild);
                            ImGui.TreePop();
                        }
                        ImGui.PopID();
                    }
                }
                else if (root.HasChildNodes)
                {
                    TreeCounter++;
                    ImGui.PushID($"Tree{TreeCounter}");
                    if (ImGui.TreeNodeEx($"{root.Name}###ID{TreeCounter}", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        //DoWork(root);
                        PopulateTree(root.FirstChild);
                        ImGui.TreePop();
                    }
                    ImGui.PopID();
                }
                else
                {
                    //DoWork(root);
                }
                if (root.NextSibling != null)
                {
                    PopulateTree(root.NextSibling);
                }
                else
                {
                }
            }
            else if (root is XmlText)
            { }
            else if (root is XmlComment)
            { }
        }

    }
}
