using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace RandomizerCore.Data
{
    public class VanillaData
    {
        public static readonly VanillaData data;

        readonly ILookup<string, string> vanillaPlacements;

        public IEnumerable<string> GetVanillaItems(string location)
        {
            return vanillaPlacements[location];
        }

        public VanillaData(ILookup<string, string> lookup)
        {
            vanillaPlacements = lookup;
        }

        static VanillaData Parse(XmlNodeList nodes)
        {
            return new VanillaData(
                nodes.Cast<XmlNode>()
                .SelectMany(node =>
                {
                    return node["items"].InnerText.Split(',').Select(i => new Pair<string, string>(node.Attributes["name"].InnerText, i));
                }).ToLookup(p => p.Item1, p => p.Item2));
        }

        static VanillaData()
        {
           data = Parse(XmlUtil.LoadEmbeddedXml("RandomizerCore.Resources.vanilla.xml").SelectNodes("randomizer/item"));
        }
    }
}
