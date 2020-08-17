using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomizerCore.Data;

namespace RandomizerCore.Algorithms
{
    public partial class Randomizer3
    {
        public override Dictionary<string, string> RandomizeTransitions()
        {
            ProgressionManager pm = new ProgressionManager(logicManager, difficultySettings, wData, iData, cData);

            PlacedTransitions pt = new PlacedTransitions(transitions, pm, tData);
            ReachableTransitions rt = new ReachableTransitions(transitions, pt.placedTransitions, pm);
            
            ReachableLocations rl = new ReachableLocations(locations, pm, autoupdate: false);
            FilledLocations fl = new FilledLocations(locations);
            
            List<int> importantItems = FetchImportantItems(rng);
            List<int> transitionOrder = rng.Permute(transitions.Length).ToList();

            List<int> oneWayIn = transitionOrder.Where(i => tData.GetTransitionDef(transitions[i]).sides == Data.GateSides.In).ToList();
            List<int> oneWayOut = transitionOrder.Where(i => tData.GetTransitionDef(transitions[i]).sides == Data.GateSides.Out).ToList();
            while (oneWayIn.Any())
            {
                int entrance = oneWayIn.Pop();
                int exit = oneWayOut.Pop();
                transitionOrder.Remove(entrance);
                transitionOrder.Remove(exit);
                pt.Place(entrance, exit);
            }

            // set up auto-update for preplaced items/transitions
            VanillaManager vm = new VanillaManager(randomizationSettings, iData, pm);
            PrePlacedManager oneWayPPM = new PrePlacedManager(pt.GetPlacedTransitions().SelectMany(kvp => new[] { new ILP(kvp.Key, kvp.Key), new ILP(kvp.Key, kvp.Value) }).ToList(), pm);

            while (transitionOrder.Any())
            {
                int adjReachableCount = rt.ReachableCount - pt.PlacedCount;

                int entrance = transitionOrder.Pop(t => rt.CanReach(t));
                if (adjReachableCount > 1 || transitionOrder.Count == 1)
                {
                    int exit = transitionOrder.Pop(t => MatchPosition(entrance, t));
                    pt.Place(entrance, exit);
                    continue;
                }

                else if (adjReachableCount == 1)
                {
                    if (transitionOrder.TryPop(t => MatchPosition(entrance, t) && !rt.CanReach(t), out int exit))
                    {
                        pt.Place(entrance, exit);
                        continue;
                    }
                }

                // Running low on transitions--try placing items to open more slots
                UpdateItems(importantItems, rl, fl, pm);
                if (adjReachableCount < rt.ReachableCount - pt.PlacedCount)
                {
                    if (transitionOrder.TryPop(t => MatchPosition(entrance, t) && !rt.CanReach(t), out int exit))
                    {
                        pt.Place(entrance, exit);
                        continue;
                    }
                }

                // Unable to open transitions through items--proceed to exhaustively check next transition
                if (TryForceTransition(transitionOrder, pm, rt, entrance, out int forcedExit))
                {
                    pt.Place(entrance, forcedExit);
                    continue;
                }

                // Unable to find matching transition by any means... Terminating randomization early.
                return null;

            }

            return pt.GetPlacedTransitions();
        }

        public override List<ILP> RandomizeItems(Dictionary<string, string> transitionPlacements)
        {
            Random rng = new Random(seed);
            ProgressionManager pm = new ProgressionManager(logicManager, difficultySettings, wData, iData, cData);
            ReachableTransitions rt = new ReachableTransitions(transitions, 
                PlacedTransitions.ConvertStringPlacementsToInt(transitions, transitionPlacements), pm);

            ReachableLocations rl = new ReachableLocations(locations, pm);
            FilledLocations fl = new FilledLocations(locations);

            List<int> permutedLocations = rng.Permute(locations.Length).ToList();
            List<int> permutedItems = rng.Permute(items.Length).ToList();

            int[] shops = permutedLocations.Where(l => lData.GetLocationDef(locations[l]).pool == Data.Pool.Shop).ToArray();

            if (!normalShopFill)
            {
                permutedLocations.RemoveAll(i => shops.Contains(i));
            }

            var progressionLookup = permutedItems.ToLookup(i => iData.GetItemDef(items[i]).progression);
            List<int> junkItems = progressionLookup[false].ToList();
            List<int> progressionItems = progressionLookup[true].ToList();

            int progressionCount = progressionItems.Count;

            //auto-update vanilla progression
            VanillaManager vm = new VanillaManager(randomizationSettings, iData, pm);

            while (progressionItems.Any())
            {
                int itemCount = items.Length - fl.NonemptyCount;
                int fullReachableCount = rl.ReachableCount;
                int adjReachableCount = fullReachableCount - fl.NonemptyCount;

                switch (GetNextItemState(rng, progressionItems.Count, itemCount, adjReachableCount, fullReachableCount))
                {
                    case NextItemState.Junk:
                        {
                            int location = PopNextLocation(permutedLocations, rl);
                            fl.PlaceStandby(location);
                        }
                        break;

                    case NextItemState.Progression:
                        {
                            int location = PopNextLocation(permutedLocations, rl);
                            int item = progressionItems.Pop();
                            pm.Add(items[item]);
                            fl.Fill(location, item);
                        }
                        break;

                    case NextItemState.ForceProgression:
                        if (TryForceItem(permutedItems, pm, rl, out int forcedItem))
                        {
                            int location = PopNextLocation(permutedLocations, rl);
                            fl.Fill(location, forcedItem);
                            Delinearize(rng, fl, permutedLocations, shops);
                            break;
                        }
                        else goto case NextItemState.OverflowProgression;

                    case NextItemState.OverflowProgression:
                        {
                            permutedLocations.AddRange(rng.Permute(fl.ClearStandby().ToArray()));

                            int startCount = rl.ReachableCount;
                            do
                            {
                                int overflowItem = progressionItems.Pop();
                                pm.Add(items[overflowItem]);
                                fl.Fill(PopNextLocation(permutedLocations, rl), overflowItem);

                                adjReachableCount = rl.ReachableCount;
                            }
                            while (adjReachableCount == startCount);
                        }
                        break;
                }
            }

            permutedLocations.AddRange(rng.Permute(fl.ClearStandby().ToArray()));
            while (permutedLocations.Any())
            {
                fl.Fill(permutedLocations.Pop(), junkItems.Pop());
            }
            while (junkItems.Any())
            {
                fl.Fill(rng.Next(shops), junkItems.Pop());
            }

            return fl.GetStringILPs(items, locations);
        }


        private void UpdateItems(List<int> importantItems, ReachableLocations rl, FilledLocations fl, ProgressionManager pm)
        {
            if (!importantItems.Any()) return;
            rl.Update(temp: false);

            bool updated;
            do
            {
                for (int l = 0; l < locations.Length; l++)
                {
                    if (rl.CanReach(l) && fl.IsEmpty(l))
                    {
                        int item = importantItems.Pop();
                        pm.Add(items[item]);
                        fl.Fill(l, item);
                        if (!importantItems.Any()) return;
                    }
                }
                rl.Update(out updated);
            }
            while (updated);
        }

        private List<int> FetchImportantItems(Random rng)
        {
            List<int> importantItems = new List<int>();
            int[] itemOrder = rng.Permute(items.Length).Where(i => iData.GetItemDef(items[i]).progression).ToArray();
            importantItems.AddRange(itemOrder.Where(i => iData.GetItemDef(items[i]).pool == Pool.Skill));
            importantItems.AddRange(itemOrder.Where(i => iData.GetItemDef(items[i]).pool == Pool.Stag));
            importantItems.AddRange(itemOrder.Where(i => iData.GetItemDef(items[i]).pool == Pool.Key));
            importantItems.AddRange(itemOrder.Where(i => iData.GetItemDef(items[i]).pool == Pool.Dreamer));
            importantItems.AddRange(itemOrder.Where(i => iData.GetItemDef(items[i]).pool == Pool.Charm));
            return importantItems;
        }

        public bool MatchPosition(int t1, int t2)
        {
            GatePosition p1 = tData.GetTransitionDef(transitions[t1]).position;
            GatePosition p2 = tData.GetTransitionDef(transitions[t2]).position;

            switch (p1)
            {
                case GatePosition.left:
                    return p2 == GatePosition.right;
                case GatePosition.bottom:
                    return p2 == GatePosition.top;
                case GatePosition.top:
                    return p2 == GatePosition.bottom;
                case GatePosition.right:
                    return p2 == GatePosition.left;
            }
            throw new ArgumentException($"Invalid GatePosition value on {transitions[t1]} or {transitions[t2]}");
        }

        private bool TryForceTransition(List<int> transitionOrder, ProgressionManager pm, ReachableTransitions rt, int t1, out int t2)
        {
            for (int i = 0; i < transitionOrder.Count; i++)
            {
                t2 = transitionOrder[i];
                if (MatchPosition(t1, t2) && TestTransition(pm, rt, t2))
                {
                    transitionOrder.RemoveAt(i);
                    return true;
                }
            }

            t2 = -1;
            return false;
        }

        private bool TestTransition(ProgressionManager pm, ReachableTransitions rt, int t2)
        {
            pm.AddTemp(transitions[t2]);
            if (rt.TempCount > 0)
            {
                pm.SaveTempItems();
                return true;
            }
            else
            {
                pm.RemoveTempItems();
                return false;
            }
        }
    }
}
