using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DFXR3Editor.Dependencies
{
    public class FFXHelperMethods
    {
        public static readonly XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        public static readonly XNamespace xsd = "http://www.w3.org/2001/XMLSchema";

        // Supported FFX Arguments
        public static readonly string[] _actionIDSupported = DefParser.SupportedActionIDs();
        public static readonly string[] AxByColorArray = new string[] { "A19B7", "A35B11", "A67B19", "A99B27", "A4163B35" };
        public static readonly string[] AxByScalarArray = new string[] { "A0B0", "A16B4", "A32B8", "A64B16", "A96B24", "A4160B32" };

        public static IEnumerable<XElement> XMLChildNodesValid(XElement Node)
        {
            //return from element in Node.Elements()
            //       where element.NodeType == XmlNodeType.Element
            //       select element;

            return Node.Elements();
        }
        public static int GetNodeIndexinParent(XElement Node)
        {
            //return Node.ElementsBeforeSelf().Where(n => n.NodeType == XmlNodeType.Element).Count();
            return Node.ElementsBeforeSelf().Count();
        }
        public static string AxByToName(string FFXPropertyAxBy)
        {
            return FFXPropertyAxBy switch
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
                _ => FFXPropertyAxBy,
            };
        }
        public static string EffectIDToName(string EffectID)
        {
            return EffectID switch
            {
                "1002" => "Thresholds<'LOD'>",
                "1004" => "Effect()",
                "2000" => "Root<>",
                "2001" => "Reference<'FXR'>",
                "2002" => "Container<'LOD'>",
                "2200" => "Container<'Effect'>",
                "2202" => "Container<'Effect+'>",
                _ => EffectID,
            };
        }
    }
}
