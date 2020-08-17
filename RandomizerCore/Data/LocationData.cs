using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace RandomizerCore.Data
{
    public struct LocationDef
    {
        public string itemLogic;
        public string areaLogic;
        public string roomLogic;

        public Pool pool;
        public string areaName;
        public CostType costType;

        public int essenceCost;
        public int grubCost;
        public int simpleCost;
    }

    public enum Pool
    {
        None,
        Start,
        Shop,
        Dreamer,
        Skill,
        Charm,
        Key,
        Mask,
        Vessel,
        Ore,
        Notch,
        Geo,
        Egg,
        Relic,
        Grub,
        Root,
        Map,
        Stag,
        Essence_Boss,
        Cursed,
        Dupe,
        Fake,
    }

    public enum CostType
    {
        None = 0,
        Geo,
        Essence,
        Simple,
        Grub,
        Wraiths,
        Dreamnail,
        WhisperingRoot,
    }

    public class LocationData
    {
        public static LocationData data;

        Dictionary<string, LocationDef> _items;
        public string[] LocationNames;

        public LocationData(Dictionary<string, LocationDef> items)
        {
            _items = items;
            LocationNames = items.Keys.ToArray();
        }

        public LocationDef GetLocationDef(string name)
        {
            return _items[name];
        }

        public IEnumerable<string> Filter(Func<LocationDef, bool> func)
        {
            return _items.Keys.Where(i => func(_items[i]));
        }

        public static LocationData Parse(XmlNodeList nodes)
        {
            return new LocationData(nodes.Cast<XmlNode>()
                .Select(node => XmlUtil.DeserializeByReflection<LocationDef>(node))
                .ToDictionary(pair => pair.Item1, pair => pair.Item2));
        }

        static LocationData()
        {
            data = Parse(XmlUtil.LoadEmbeddedXml("RandomizerCore.Resources.locations.xml").SelectNodes("randomizer/item"));
        }
    }
}
