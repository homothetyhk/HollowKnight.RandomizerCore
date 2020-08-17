using RandomizerCore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerCore
{
    public class ReachableLocations
    {
        private bool[] reachable;
        private bool[] tempReachable;

        private string[] locations;
        private int length;
        private ProgressionManager pm;

        public ReachableLocations(string[] locations, ProgressionManager pm, bool autoupdate = true)
        {
            this.locations = locations;
            length = locations.Length;
            this.pm = pm;
            reachable = new bool[length];
            tempReachable = new bool[length];
            ReachableCount = 0;
            TempCount = 0;
            
            if (autoupdate)
            {
                pm.AfterAddItem += Update;
                pm.AfterEndTemp += EndTemp;
                Update(temp: false);
            }
        }

        public bool CanReach(int index)
        {
            return reachable[index];
        }

        public bool CanReach(string location)
        {
            return reachable[Array.IndexOf(locations, location)];
        }

        public string[] GetReachableLocations()
        {
            return reachable.Select((b, i) => new Pair<bool, int>(b, i)).Where(p => p.Item1).Select(p => locations[p.Item2]).ToArray();
        }

        public int ReachableCount
        {
            get; private set;
        }

        public bool AllReachable
        {
            get => ReachableCount == length;
        }

        public int TempCount
        {
            get; private set;
        }

        public void LogTemp()
        {
            Logger.LogDebug($"Found {TempCount} new locations:");
            tempReachable.Select((b, i) => new Pair<bool, int>(b, i)).Where(p => p.Item1).Select(p => locations[p.Item2]).Log();
        }

        public void Update(bool temp)
        {
            for (int i = 0; i < length; i++)
            {
                if (!reachable[i] && pm.CanGet(locations[i]))
                {
                    reachable[i] = true;
                    ReachableCount++;
                    if (temp)
                    {
                        tempReachable[i] = true;
                        TempCount++;
                    }
                }
            }
        }

        public void Update(out bool updated)
        {
            updated = false;

            for (int i = 0; i < length; i++)
            {
                if (!reachable[i] && pm.CanGet(locations[i]))
                {
                    reachable[i] = true;
                    ReachableCount++;
                    updated = true;
                }
            }
        }

        public void Update(ILookup<string, string> lookup, out bool updated, ItemData iData = null)
        {
            updated = false;
            iData = iData ?? ItemData.data;

            for (int i = 0; i < length; i++)
            {
                if (!reachable[i] && pm.CanGet(locations[i]))
                {
                    reachable[i] = true;
                    ReachableCount++;
                    updated = true;
                    //Logger.LogFine($"Reachable locations:\n{locations[i]}");
                    //lookup[locations[i]].Log();
                    pm.Add(lookup[locations[i]].Where(item => iData.GetItemDef(item).progression));
                }
            }
        }

        private void EndTemp(bool tempSaved)
        {
            TempCount = 0;
            for (int i = 0; i < length; i++)
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
