using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace RandomizerCore.Data
{
    public struct LogicDef
    {
        public string name;
        public int[] logic;
        public int grubCost;
        public int essenceCost;
        public int simpleCost;
    }

    public enum LogicMode
    {
        Item,
        Area,
        Room,
        Custom
    }

    public class LogicManager
    {
        public static LogicManager ItemLogicManager;
        public static LogicManager AreaLogicManager;
        public static LogicManager RoomLogicManager;

        private Dictionary<string, LogicDef> logicDefs;
        public int progressionMax => processor.progressionIndex.Count;
        private Dictionary<string, int> origGrubCosts = new Dictionary<string, int>();
        private Dictionary<string, int> origEssenceCosts = new Dictionary<string, int>();
        private LogicProcessor processor;
        public readonly LogicMode mode;

        public LogicManager(Dictionary<string, LogicDef> logicDefs, LogicProcessor processor, LogicMode mode)
        {
            this.logicDefs = logicDefs;
            this.processor = processor;
            this.mode = mode;
        }

        public int GetProgressionIndex(string s)
        {
            return processor.progressionIndex[s];
        }

        public bool ParseLogic(ProgressionManager pm, string location)
        {
            LogicDef logicDef = logicDefs[location];
            return ParseLogic(pm, logicDef);
        }

        public bool ParseLogic(ProgressionManager pm, LogicDef logicDef)
        {
            int[] logic = logicDef.logic;

            Stack<bool> stack = new Stack<bool>();

            for (int i = 0; i < logic.Length; i++)
            {
                switch (logic[i])
                {
                    //AND
                    case -2:
                        if (stack.Count < 2)
                        {
                            //RandomizerMod.Instance.LogWarn($"Could not parse logic for \"{location}\": Found + when stack contained less than 2 items");
                            return false;
                        }

                        stack.Push(stack.Pop() & stack.Pop());
                        break;
                    //OR
                    case -1:
                        if (stack.Count < 2)
                        {
                            //RandomizerMod.Instance.LogWarn($"Could not parse logic for \"{location}\": Found | when stack contained less than 2 items");
                            return false;
                        }
                        stack.Push(stack.Pop() | stack.Pop());
                        break;
                    //EVERYTHING - DO NOT USE, WILL BREAK THE RANDOMIZER
                    case 0:
                        stack.Push(false);
                        break;
                    // ESSENCECOUNT
                    case -3:
                        int essenceCost = pm.cData.modifiedEssenceCosts.TryGetValue(logicDef.name, out essenceCost) ? essenceCost : logicDef.essenceCost;
                        stack.Push(pm.essence >= essenceCost + pm.essenceTolerance);
                        break;
                    // GRUBCOUNT
                    case -4:
                        int grubCost = pm.cData.modifiedGrubCosts.TryGetValue(logicDef.name, out grubCost) ? grubCost : logicDef.grubCost;
                        stack.Push(pm.grubs >= grubCost + pm.grubTolerance);
                        break;
                    // SIMPLECOUNT
                    case -5:
                        int simpleCost = pm.cData.modifiedSimpleKeyCosts.TryGetValue(logicDef.name, out simpleCost) ? simpleCost : logicDef.simpleCost;
                        stack.Push(pm.simpleKeys >= simpleCost);
                        break;
                    default:
                        stack.Push(pm.obtained[logic[i]]);
                        break;
                }
            }

            if (stack.Count == 0)
            {
                //LogWarn($"Could not parse logic for \"{location}\": Stack empty after parsing");
                return false;
            }

            if (stack.Count != 1)
            {
                //LogWarn($"Extra items in stack after parsing logic for \"{location}\"");
            }

            return stack.Pop();
        }

        public bool ParseUnprocessedLogic(ProgressionManager pm, string goal)
        {
            LogicDef def = new LogicDef
            {
                logic = processor.ShuntingYard(goal)
            };

            return ParseLogic(pm, def);
        }

        static LogicManager()
        {
            LogicProcessor logicProcessor = LogicProcessor.defaultProcessor;

            Dictionary<string, LogicDef> logicDefs = new Dictionary<string, LogicDef>();
            logicProcessor.ProcessLocationLogic(LocationData.data, logicDefs, LogicMode.Item);
            logicProcessor.ProcessWaypointLogic(WaypointData.data, logicDefs, LogicMode.Item);
            ItemLogicManager = new LogicManager(logicDefs, logicProcessor, LogicMode.Item);

            logicDefs = new Dictionary<string, LogicDef>();
            logicProcessor.ProcessLocationLogic(LocationData.data, logicDefs, LogicMode.Area);
            logicProcessor.ProcessTransitionLogic(TransitionData.areaData, logicDefs);
            logicProcessor.ProcessWaypointLogic(WaypointData.data, logicDefs, LogicMode.Area);
            AreaLogicManager = new LogicManager(logicDefs, logicProcessor, LogicMode.Area);

            logicDefs = new Dictionary<string, LogicDef>();
            logicProcessor.ProcessLocationLogic(LocationData.data, logicDefs, LogicMode.Room);
            logicProcessor.ProcessTransitionLogic(TransitionData.roomData, logicDefs);
            RoomLogicManager = new LogicManager(logicDefs, logicProcessor, LogicMode.Room);
        }
    }
}
