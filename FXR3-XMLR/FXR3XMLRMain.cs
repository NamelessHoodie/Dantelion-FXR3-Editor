using SoulsFormats;
using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace FXR3XMLR
{
    public class FXR3XMLRMain
    {
        [STAThread]
        static void Main(string[] args)
        {
            void USAGE()
            {
                Console.WriteLine("Usage: FXMLRDS3 < *.fxr | *.fxr.xml >");
            }

            if (args.Length < 1)
            {
                USAGE();
                return;
            }

            void doArg(string arg)
            {
                string fileName = arg.ToLower();
                if (fileName.EndsWith(".fxr.xml"))
                {
                    XDocument XMLFXR3 = XDocument.Load(fileName);
                    if (XMLFXR3 == null)
                        return;
                    FXR3 ffx = XMLToFXR3(XMLFXR3);
                    if (ffx == null)
                        return;
                    ffx.Write(fileName.Substring(0, fileName.Length - 4));
                }
                else if (fileName.EndsWith(".fxr"))
                {
                    FXR3 ffx = FXR3.Read(fileName);
                    if (ffx == null)
                        return;
                    FXR3ToXML(ffx).Save(fileName + ".xml");
                }
                else
                {
                    USAGE();
                    return;
                }
            }

            for (int i = 0; i < args.Length; i++)
            {
                doArg(args[i]);
            }
        }
        public static FXR3 XMLToFXR3(XDocument XML)
        {
            XmlSerializer test = new XmlSerializer(typeof(FXR3));
            XmlReader xmlReader = XML.CreateReader();

            return (FXR3)test.Deserialize(xmlReader);
        }
        public static XDocument FXR3ToXML(FXR3 fxr)
        {
            XDocument XDoc = new XDocument();

            using (var xmlWriter = XDoc.CreateWriter())
            {
                var thing = new XmlSerializer(typeof(FXR3));
                thing.Serialize(xmlWriter, fxr);
            }

            return XDoc;
        }
    }
}