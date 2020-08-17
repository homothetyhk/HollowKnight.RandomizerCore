using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerCore.Data
{
    public class LogicProcessor
    {
        public static LogicProcessor defaultProcessor;

        Dictionary<string, int[]> macros;
        public Dictionary<string, int> progressionIndex;
        static Dictionary<string, int> operators = new Dictionary<string, int>
        {
            { "|", -1 },
            { "+", -2 },
            { "ESSENCECOUNT", -3 },
            { "GRUBCOUNT", -4 },
            { "SIMPLECOUNT", -5 },
        };
        static string[] settings = new string[]
        {
            "MILDSKIPS", "SHADESKIPS", "ACIDSKIPS", "SPIKETUNNELS", "FIREBALLSKIPS", "DARKROOMS", "SPICYSKIPS", "NOTCURSED", "CURSED"
        };

        public LogicProcessor(MacroData macros, ItemData items, WaypointData waypoints, TransitionData transitions)
        {
            IndexProgression(items, waypoints, transitions);
            ProcessMacros(macros);
        }

        public void ProcessLocationLogic(LocationData locations, Dictionary<string, LogicDef> logic, LogicMode mode)
        {
            foreach (string location in locations.LocationNames)
            {
                LocationDef def = locations.GetLocationDef(location);
                string rawLogic = string.Empty;
                switch (mode)
                {
                    case LogicMode.Item:
                        rawLogic = def.itemLogic;
                        break;
                    case LogicMode.Area:
                        rawLogic = def.areaLogic;
                        break;
                    case LogicMode.Room:
                        rawLogic = def.roomLogic;
                        break;
                }

                logic[location] = new LogicDef
                {
                    name = location,
                    logic = ShuntingYard(rawLogic),
                    essenceCost = def.essenceCost,
                    grubCost = def.grubCost,
                    simpleCost = def.simpleCost,
                };
            }
        }

        public void ProcessTransitionLogic(TransitionData transitions, Dictionary<string, LogicDef> logic)
        {
            foreach (string transition in transitions.TransitionNames)
            {
                TransitionDef def = transitions.GetTransitionDef(transition);
                logic[transition] = new LogicDef
                {
                    name = transition,
                    logic = ShuntingYard(def.logic),
                    essenceCost = def.essenceCost,
                    grubCost = def.grubCost,
                    simpleCost = def.simpleCost,
                };
            }
        }

        public void ProcessWaypointLogic(WaypointData waypoints, Dictionary<string, LogicDef> logic, LogicMode mode)
        {
            foreach (string waypoint in waypoints.WaypointNames)
            {
                Waypoint def = waypoints.GetWaypoint(waypoint);
                logic[waypoint] = new LogicDef
                {
                    name = waypoint,
                    logic = mode == LogicMode.Item ? ShuntingYard(def.itemLogic) : ShuntingYard(def.areaLogic),
                    essenceCost = def.essenceCost,
                    grubCost = def.grubCost,
                    simpleCost = def.simpleCost,
                };
            }
        }

        private void IndexProgression(ItemData items, WaypointData waypoints, TransitionData transitions)
        {
            progressionIndex = new Dictionary<string, int>();
            int i = 1;
            foreach (string setting in settings)
            {
                Assign(ref i, setting);
            }
            foreach (string item in items?.ProgressionItems ?? new string[0])
            {
                Assign(ref i, item);
            }
            foreach (string waypoint in waypoints?.WaypointNames ?? new string[0])
            {
                Assign(ref i, waypoint);
            }
            foreach (string transition in transitions?.TransitionNames ?? new string[0])
            {
                Assign(ref i, transition);
            }
        }

        private void Assign(ref int i, string key)
        {
            progressionIndex[key] = i++;
        }

        private void ProcessMacros(MacroData macroData)
        {
            macros = new Dictionary<string, int[]>();
            foreach (var macro in macroData.Macros)
            {
                macros[macro.Key] = ShuntingYard(macro.Value);
            }
        }

        public int[] ShuntingYard(string infix)
        {
            Helper.last = infix = infix ?? string.Empty;
            int i = 0;
            Stack<string> stack = new Stack<string>();
            List<int> postfix = new List<int>();

            while (i < infix.Length)
            {
                string op = GetNextOperator(infix, ref i);
                Helper.last += $"\n{op}";

                // Easiest way to deal with whitespace between operators
                if (op.Trim(' ') == string.Empty)
                {
                    continue;
                }

                if (op == "+" || op == "|")
                {
                    while (stack.Count != 0 && (op == "|" || op == "+" && stack.Peek() != "|") && stack.Peek() != "(")
                    {
                        postfix.Add(GetLogicValue(stack.Pop()));
                    }

                    stack.Push(op);
                }
                else if (op == "(")
                {
                    stack.Push(op);
                }
                else if (op == ")")
                {
                    while (stack.Peek() != "(")
                    {
                        postfix.Add(GetLogicValue(stack.Pop()));
                    }

                    stack.Pop();
                }
                else
                {
                    // Parse macros
                    if (macros.TryGetValue(op, out int[] macro))
                    {
                        postfix.AddRange(macro);
                    }
                    else
                    {
                        postfix.Add(GetLogicValue(op));
                    }
                }
            }

            while (stack.Count != 0)
            {
                postfix.Add(GetLogicValue(stack.Pop()));
            }

            return postfix.ToArray();
        }

        private int GetLogicValue(string op)
        {
            if (operators.TryGetValue(op, out int val)) return val;
            if (progressionIndex.TryGetValue(op, out val)) return val;
            throw new ArgumentException($"{op} has no assigned logic value.");
        }

        private static string GetNextOperator(string infix, ref int i)
        {
            int start = i;

            if (infix[i] == '(' || infix[i] == ')' || infix[i] == '+' || infix[i] == '|')
            {
                i++;
                return infix[i - 1].ToString();
            }

            while (i < infix.Length && infix[i] != '(' && infix[i] != ')' && infix[i] != '+' && infix[i] != '|')
            {
                i++;
            }

            return infix.Substring(start, i - start).Trim(' ');
        }

        static LogicProcessor()
        {
            defaultProcessor = new LogicProcessor(MacroData.data, ItemData.data, WaypointData.data, TransitionData.roomData);
        }

    }

    public static class Helper
    {
        public static string last;
    }
}
