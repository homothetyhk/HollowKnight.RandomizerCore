using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RandomizerCore.Data;

namespace RandomizerCore
{
    public class Validator
    {
        public bool Validated
        {
            get; private set;
        }
        readonly ValidationFlag[] flags;
        readonly string goal;
        readonly string[] items;
        readonly string[] locations;
        readonly string[] transitions;
        readonly DifficultySettings settings;
        readonly LogicManager lm;
        readonly Randomizer R;
        ReachableLocations rl;
        ReachableTransitions rt;
        ProgressionManager pm;

        public delegate void OnValidateHandler(List<ILP> ILPs, Dictionary<string, string> TPs);
        public OnValidateHandler OnValidate = (a,b) => { };

        public Validator(ValidationFlag[] flags, Randomizer R, string goal = null)
        {
            this.flags = flags;
            this.items = R.items;
            this.locations = R.locations;
            this.transitions = R.transitions;
            this.settings = R.difficultySettings;
            this.lm = R.logicManager;
            this.goal = goal;
            this.R = R;
        }

        public bool Validate(List<ILP> ILPs, StartDef start, Dictionary<string, string> TPs = null)
        {
            OnValidate.Invoke(ILPs, TPs);
            Reset(TPs);

            foreach (ValidationFlag flag in flags)
            {
                switch (flag)
                {
                    case ValidationFlag.AllLocationsFilled:
                        if (CheckLocationsPresent(ILPs)) continue;
                        else
                        {
                            LogValidationFail(flag);
                            return false;
                        }

                    case ValidationFlag.AllItemsPlaced:
                        if (CheckItemsPresent(ILPs, out List<string> extraItems, out List<string> missingItems)) continue;
                        else
                        {
                            LogValidationFail(flag);
                            Logger.LogFine("Failed to place items:");
                            missingItems.Log();
                            Logger.LogFine("Overplaced items:");
                            extraItems.Log();
                            
                            return false;
                        }

                    case ValidationFlag.AllLocationsSingleFilledOrShop:
                        if (CheckLocationCounts(ILPs)) continue;
                        else
                        {
                            LogValidationFail(flag);
                            return false;
                        }

                    case ValidationFlag.NoDupesInShops:
                        if (ILPs.All(p => R.iData.GetItemDef(p.item).pool != Pool.Dupe || R.lData.GetLocationDef(p.location).pool != Pool.Shop)) continue;
                        else
                        {
                            LogValidationFail(flag);
                            ILPs.Where(p => R.iData.GetItemDef(p.item).pool == Pool.Dupe && R.lData.GetLocationDef(p.location).pool == Pool.Shop).Log();
                            return false;
                        }

                    case ValidationFlag.AllLocationsReachable:
                        if (!rl.AllReachable)
                        {
                            RecursivelyUpdateReachable(ILPs);
                        }
                        if (rl.AllReachable) continue;
                        else
                        {
                            LogValidationFail(flag);
                            Logger.LogDebug($"Able to reach {rl.ReachableCount} out of {locations.Length} locations.");
                            Logger.LogDebug($"Essence: {pm.essence}");
                            Logger.LogDebug($"Grubs: {pm.grubs}");
                            Logger.LogDebug($"Simple keys: {pm.simpleKeys}");
                            
                            if (locations.Length - rl.ReachableCount > 0)
                            {
                                R.cData.Log();
                                ILPs.Where(p => ItemData.data.GetItemDef(p.item).progression && !rl.CanReach(p.location)).Log();
                            }

                            //Logger.LogFine("Unable to reach:");
                            //locations.Except(rl.GetReachableLocations()).Log();
                            return false;
                        }

                    case ValidationFlag.AllProgressionReachable:
                        if (!rl.AllReachable)
                        {
                            RecursivelyUpdateReachable(ILPs);
                        }
                        if (locations.Intersect(ILPs.Where(p => ItemData.data.GetItemDef(p.item).progression).Select(p => p.location)).Select((l, i) => i).All(i => rl.CanReach(i))) continue;
                        else
                        {
                            LogValidationFail(flag);
                            return false;
                        }

                    case ValidationFlag.AllTransitionsReachable:
                        if (!rt.AllReachable)
                        {
                            RecursivelyUpdateReachable(ILPs);
                        }
                        if (rt.AllReachable) continue;
                        else
                        {
                            LogValidationFail(flag);
                            return false;
                        }

                    case ValidationFlag.GoalReachable:
                        if (CheckGoal()) continue;
                        else
                        {
                            LogValidationFail(flag);
                            return false;
                        }
                }
            }
            Validated = true;
            return true;
        }

        private bool CheckLocationsPresent(List<ILP> ILPs)
        {
            return ILPs.Select(p => p.location).Intersect(locations).Count() == locations.Length;
        }

        private bool CheckItemsPresent(List<ILP> ILPs, out List<string> extraItems, out List<string> missingItems)
        {
            IOrderedEnumerable<string> placement = ILPs.Select(p => p.item).OrderBy(i => i);
            IOrderedEnumerable<string> comparer = items.OrderBy(i => i);

            if (placement.SequenceEqual(comparer))
            {
                extraItems = null;
                missingItems = null;
                return true;
            }
            else
            {
                extraItems = placement.Except(comparer).ToList();
                missingItems = comparer.Except(placement).ToList();
                return false;
            }
        }

        private bool CheckLocationCounts(List<ILP> ILPs)
        {
            Dictionary<string, int> counts = ILPs.ToLookup(p => p.location, p => p.item).ToDictionary(g => g.Key, g => g.Count());

            foreach (string location in locations)
            {
                if (counts.TryGetValue(location, out int count) && ((count == 1) || (LocationData.data.GetLocationDef(location).pool == Pool.Shop))) continue;
                else return false;
            }

            return true;
        }

        public bool CheckGoal()
        {
            return lm.ParseUnprocessedLogic(pm, goal);
        }

        private void RecursivelyUpdateReachable(List<ILP> ILPs)
        {
            bool update1 = false;
            bool update2 = false;

            ILookup<string,string> lookup = ILPs.ToLookup(p => p.location, p => p.item);

            do
            {
                rl?.Update(lookup, out update1);
                rt?.Update(out update2);
            }
            while (update1 || update2);
        }

        private void LogValidationFail(ValidationFlag flag)
        {
            Logger.LogDebug($"Validation failed on flag: {flag}!");
        }

        private void Reset(Dictionary<string, string> TPs)
        {
            pm = R.BuildProgressionManager();

            if (TPs != null)
            {
                rt = new ReachableTransitions(transitions, PlacedTransitions.ConvertStringPlacementsToInt(transitions, TPs), pm);
            }

            rl = new ReachableLocations(locations, pm, autoupdate: false);
            new VanillaManager(R.randomizationSettings, R.iData, pm);
        }
    }

    public enum ValidationFlag
    {
        None = 0,
        AllLocationsFilled,
        AllItemsPlaced,
        AllLocationsSingleFilledOrShop,
        NoDupesInShops,
        AllLocationsReachable,
        AllProgressionReachable,
        AllTransitionsReachable,
        GoalReachable,
    }
}
