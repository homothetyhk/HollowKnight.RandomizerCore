using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomizerCore.Data;

namespace RandomizerCore
{
    public class PlacedTransitions
    {
        bool[] placed;
        ProgressionManager pm;
        TransitionData tData;
        string[] transitions;
        public Dictionary<int, int> placedTransitions;

        public PlacedTransitions(string[] transitions, ProgressionManager pm, TransitionData tData)
        {
            placed = new bool[transitions.Length];
            this.transitions = transitions;
            this.pm = pm;
            this.tData = tData;
            placedTransitions = new Dictionary<int, int>();
        }

        public static Dictionary<int, int> ConvertStringPlacementsToInt(string[] transitions, Dictionary<string, string> stringPlacements)
        {
            var placedTransitions = new Dictionary<int, int>();

            Dictionary<string, int> index = new Dictionary<string, int>();

            for (int i = 0; i < transitions.Length; i++)
            {
                index.Add(transitions[i], i);
            }

            for (int i = 0; i < transitions.Length; i++)
            {
                if (stringPlacements.TryGetValue(transitions[i], out string exit))
                {
                    placedTransitions.Add(i, index[exit]);
                }
            }
            return placedTransitions;
        }

        public int PlacedCount => placed.Count(b => b);
        public void Place(int t1, int t2)
        {
            placed[t1] = true;
            placed[t2] = true;
            pm.Add(new string[] { transitions[t1], transitions[t2] });
            placedTransitions.Add(t1, t2);
            if (tData.GetTransitionDef(transitions[t2]).sides != GateSides.Out)
            {
                placedTransitions.Add(t2, t1);
            }
        }

        public bool TryGetTransition(int entrance, out int exit)
        {
            return placedTransitions.TryGetValue(entrance, out exit);
        }

        public Dictionary<string, string> GetPlacedTransitions() => placedTransitions.ToDictionary(kvp => transitions[kvp.Key], kvp => transitions[kvp.Value]);
    }
}
