using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace RandomizerCore.Data
{
    public class MacroData
    {
        public static MacroData data;

        public Dictionary<string, string> Macros;
        public MacroData(Dictionary<string, string> macros) => Macros = macros;

        public static MacroData Parse(XmlNodeList nodes)
        {
            Dictionary<string, string> macros = new Dictionary<string, string>();
            foreach (XmlNode node in nodes)
            {
                macros[node.Attributes["name"].InnerText] = node.InnerText;
            }
            return new MacroData(macros);
        }

        static MacroData()
        {
            data = Parse(XmlUtil.LoadEmbeddedXml("RandomizerCore.Resources.macros.xml").SelectNodes("randomizer/macro"));
        }
    }
}
