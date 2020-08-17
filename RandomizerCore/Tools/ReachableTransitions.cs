using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerCore
{
    public class ReachableTransitions
    {
        private bool[] reachable;
        private bool[] tempReachable;

        private string[] transitions;
        private Dictionary<int, int> placements;
        private ProgressionManager pm;

        public ReachableTransitions(string[] transitions, Dictionary<int, int> placements, ProgressionManager pm)
        {
            this.transitions = transitions;
            this.placements = placements;
            this.pm = pm;

            reachable = new bool[transitions.Length];
            ReachableCount = 0;
            tempReachable = new bool[transitions.Length];
            TempCount = 0;

            pm.AfterAddItem += Update;
            pm.AfterEndTemp += EndTemp;
        }

        public bool CanReach(int index)
        {
            return reachable[index];
        }

        public bool CanReach(string transition)
        {
            return reachable[Array.IndexOf(transitions, transition)];
        }

        public int ReachableCount
        {
            get; private set;
        }

        public bool AllReachable
        {
            get => ReachableCount == transitions.Length;
        }

        public int TempCount
        {
            get; private set;
        }

        private void Update(bool temp)
        {
            for (int i = 0; i < transitions.Length; i++)
            {
                if (!reachable[i])
                {
                    if (pm.CanGet(transitions[i]) || pm.Has(transitions[i]))
                    {
                        reachable[i] = true;
                        
                        if (temp) tempReachable[i] = true;

                        if (placements.TryGetValue(i, out int j))
                        {
                            reachable[j] = true;
                            if (temp) tempReachable[j] = true;
                        }
                    }
                }
            }
        }

        public void Update(out bool updated)
        {
            updated = false;
            for (int i = 0; i < transitions.Length; i++)
            {
                if (!reachable[i])
                {
                    if (pm.CanGet(transitions[i]) || pm.Has(transitions[i]))
                    {
                        reachable[i] = true;
                        updated = true;

                        if (placements.TryGetValue(i, out int j))
                        {
                            reachable[j] = true;
                        }
                    }
                }
            }
        }

        private void EndTemp(bool tempSaved)
        {
            TempCount = 0;
            for (int i = 0; i< reachable.Length; i++)
            {
                if (tempReachable[i])
                {
                    tempReachable[i] = false;
                    if (!tempSaved)
                    {
                        reachable[i] = false;
                        ReachableCount--;
                    }
                }
            }
        }
    }
}
