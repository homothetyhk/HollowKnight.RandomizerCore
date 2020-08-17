using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace RandomizerCore.Data
{
    public struct TransitionDef
    {
        public string sceneName;
        public string doorName;
        public string areaName;

        public GatePosition position;

        public string toScene;
        public string toGate;

        public string logic;

        public bool isolated;
        public bool deadEnd;
        public GateSides sides; // 0 == 2-way, 1 == can only go in, 2 == can only come out
        public int essenceCost;
        public int grubCost;
        public int simpleCost;
    }

    public enum GatePosition
    {
        top = 0,
        right = 1,
        left = 2,
        bottom = 3
    }

    public enum GateSides
    {
        TwoWay = 0,
        In = 1,
        Out = 2
    }

    public class TransitionData
    {
        public static TransitionData areaData;
        public static TransitionData roomData;


        Dictionary<string, TransitionDef> _transitions;
        public string[] TransitionNames;

        public TransitionData(Dictionary<string, TransitionDef> transitions)
        {
            _transitions = transitions;
            TransitionNames = transitions.Keys.ToArray();
        }

        public TransitionDef GetTransitionDef(string name)
        {
            return _transitions[name];
        }

        public static TransitionData Parse(XmlNodeList nodes)
        {
            return new TransitionData(nodes.Cast<XmlNode>()
                .Select(node => XmlUtil.DeserializeByReflection<TransitionDef>(node))
                .ToDictionary(pair => pair.Item1, pair => pair.Item2));
        }

        static TransitionData()
        {
            areaData = Parse(XmlUtil.LoadEmbeddedXml("RandomizerCore.Resources.areas.xml").SelectNodes("randomizer/transition"));
            roomData = Parse(XmlUtil.LoadEmbeddedXml("RandomizerCore.Resources.rooms.xml").SelectNodes("randomizer/transition"));
        }
    }
}
