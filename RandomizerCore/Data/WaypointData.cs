using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace RandomizerCore.Data
{
    public struct Waypoint
    {
        public string itemLogic;
        public string areaLogic;
        public int essenceCost;
        public int grubCost;
        public int simpleCost;
    }

    public class WaypointData
    {
        public static WaypointData data;

        private Dictionary<string,Waypoint> _waypoints;
        public string[] WaypointNames;

        public WaypointData(Dictionary<string, Waypoint> waypoints)
        {
            _waypoints = waypoints;
            WaypointNames = waypoints.Keys.ToArray();
        }

        public Waypoint GetWaypoint(string name)
        {
            return _waypoints[name];
        }

        public static WaypointData Parse(XmlNodeList nodes)
        {
            return new WaypointData(nodes.Cast<XmlNode>()
                .Select(node => XmlUtil.DeserializeByReflection<Waypoint>(node))
                .ToDictionary(pair => pair.Item1, pair => pair.Item2));
        }

        static WaypointData()
        {
            data = Parse(XmlUtil.LoadEmbeddedXml("RandomizerCore.Resources.waypoints.xml").SelectNodes("randomizer/item"));
        }
    }
}
