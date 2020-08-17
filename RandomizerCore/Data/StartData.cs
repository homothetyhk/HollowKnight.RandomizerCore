using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace RandomizerCore.Data
{
    public struct StartDef
    {
        // respawn marker properties
        public string sceneName;
        public float x;
        public float y;
        public string zone;

        // logic info
        public string waypoint;
        public string areaTransition;
        public string roomTransition;

        // control for menu select
        public bool itemSafe; // safe := no items required to get to Dirtmouth
        public bool areaSafe; // safe := no items required to get to an area transition
        public bool roomSafe; // safe := no items required to get to a room transition
    }

    public class StartData
    {
        public static StartData data;

        Dictionary<string, StartDef> _starts;
        public string[] StartLocations;

        public StartData(Dictionary<string, StartDef> starts)
        {
            _starts = starts;
            StartLocations = starts.Keys.ToArray();
        }

        public StartDef GetStartDef(string name)
        {
            return _starts[name];
        }

        public static StartData Parse(XmlNodeList nodes)
        {
            return new StartData(nodes.Cast<XmlNode>()
                .Select(node => XmlUtil.DeserializeByReflection<StartDef>(node))
                .ToDictionary(pair => pair.Item1, pair => pair.Item2));
        }

        static StartData()
        {
            data = Parse(XmlUtil.LoadEmbeddedXml("RandomizerCore.Resources.startlocations.xml").SelectNodes("randomizer/start"));
        }
    }
}
