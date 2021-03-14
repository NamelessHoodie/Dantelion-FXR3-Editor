using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Xml;

namespace DSFFXEditor
{
    public class XmlNodeToColor
    {
        public float Red;
        public float Green;
        public float Blue;
        public float Alpha;
        public Vector4 ColorsVector4 = new Vector4();

        public XmlNodeToColor()
        {
        }
        public XmlNodeToColor(XmlNode RedNode, XmlNode GreenNode, XmlNode BlueNode, XmlNode AlphaNode)
        {
            float Red = float.Parse(RedNode.Attributes[1].Value);
            float Green = float.Parse(RedNode.Attributes[1].Value);
            float Blue = float.Parse(RedNode.Attributes[1].Value);
            float Alpha = float.Parse(RedNode.Attributes[1].Value);

            this.Red = Red;
            this.Green = Green;
            this.Blue = Blue;
            this.Alpha = Alpha;
            this.ColorsVector4 = new Vector4(Red, Green, Blue, Alpha);
        }
    }
}
