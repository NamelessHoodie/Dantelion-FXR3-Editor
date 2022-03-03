using ImGuiNET;
using ImGuiNETAddons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace DFXR3Editor.Dependencies
{
    public class FfxHelperMethods
    {
        public static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
        public static readonly XNamespace Xsd = "http://www.w3.org/2001/XMLSchema";

        // Supported FFX Arguments
        public static readonly string[] ActionIdSupported = DefParser.SupportedActionIDs();
        public static readonly string[] AxByColorArray = new string[] { "A19B7", "A35B11", "A67B19", "A99B27", "A4163B35" };
        public static readonly string[] AxByScalarArray = new string[] { "A0B0", "A16B4", "A32B8", "A64B16", "A96B24", "A4160B32" };

        public static IEnumerable<XElement> XmlChildNodesValid(XElement node)
        {
            //return from element in Node.Elements()
            //       where element.NodeType == XmlNodeType.Element
            //       select element;

            return node.Elements();
        }
        public static int GetNodeIndexinParent(XElement node)
        {
            //return Node.ElementsBeforeSelf().Where(n => n.NodeType == XmlNodeType.Element).Count();
            return node.ElementsBeforeSelf().Count();
        }
        public static string AxByToName(string ffxPropertyAxBy)
        {
            return ffxPropertyAxBy switch
            {
                "A0B0" => "Static 0",
                "A16B4" => "Static 1",
                "A19B7" => "Static Opaque White",
                "A32B8" => "Static Input",
                "A35B11" => "Static Input",
                "A64B16" => "Linear Interpolation",
                "A67B19" => "Linear Interpolation",
                "A96B24" => "Curve interpolation",
                "A99B27" => "Curve interpolation",
                "A4160B32" => "Loop Linear Interpolation",
                "A4163B35" => "Loop Linear Interpolation",
                _ => ffxPropertyAxBy,
            };
        }
        public static string EffectIdToName(string effectId)
        {
            return effectId switch
            {
                "1002" => "Thresholds<'LOD'>",
                "1004" => "Effect()",
                "2000" => "Root<>",
                "2001" => "Reference<'FXR'>",
                "2002" => "Container<'LOD'>",
                "2200" => "Container<'Effect'>",
                "2202" => "Container<'Effect+'>",
                _ => effectId,
            };
        }
    }
}
