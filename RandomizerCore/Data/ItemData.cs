using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace RandomizerCore.Data
{
    public struct ItemDef
    {
        public Pool pool;
        public string areaName;
        public bool progression;
        public string shopName;
        public string name;
        public int essence;
        public int count;
    }

    public enum IntType
    {
        None,
        Essence,
        Grub,
        Simple
    }

    public class ItemData
    {
        public static ItemData data;

        Dictionary<string, ItemDef> _items;
        public string[] ItemNames;
        public string[] ProgressionItems;

        public ItemData(Dictionary<string, ItemDef> items)
        {
            _items = items;
            ItemNames = items.Keys.ToArray();
            ProgressionItems = items.Where(i => i.Value.progression).Select(i => i.Key).ToArray();
        }

        public ItemDef GetItemDef(string name)
        {
            return _items[name];
        }

        public IEnumerable<string> Filter(Func<ItemDef, bool> func)
        {
            return _items.Keys.Where(i => func(_items[i]));
        }

        public static ItemData Parse(XmlNodeList nodes)
        {
            return new ItemData(nodes.Cast<XmlNode>()
                .Select(node => XmlUtil.DeserializeByReflection<ItemDef>(node))
                .ToDictionary(pair => pair.Item1, pair => pair.Item2));
        }

        

        public bool CheckIfIntProgression(string name, out IntType type, out int value)
        {
            type = IntType.None;
            value = 0;

            if (!_items.TryGetValue(name, out ItemDef def))
            {
                return false;
            }

            if (def.essence > 0)
            {
                type = IntType.Essence;
                value = def.essence;
                return true;
            }

            if (def.pool == Pool.Grub)
            {
                type = IntType.Grub;
                value = 1;
                return true;
            }

            if (name == "Simple_Key")
            {
                type = IntType.Simple;
                value = 1;
                return true;
            }
            return false;
        }

        public bool IsProgression(string name)
        {
            return _items.TryGetValue(name, out ItemDef def) && def.progression;
        }

        static ItemData()
        {
             data = Parse(XmlUtil.LoadEmbeddedXml("RandomizerCore.Resources.items.xml").SelectNodes("randomizer/item"));
        }
    }
}
