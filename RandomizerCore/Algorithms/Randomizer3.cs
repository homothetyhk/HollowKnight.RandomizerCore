using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using RandomizerCore.Data;

namespace RandomizerCore.Algorithms
{
    public partial class Randomizer3 : Randomizer
    {
        readonly bool normalShopFill;
        readonly bool delinearizedShops;
        Random rng;

        public Randomizer3(int seed, DifficultySettings difficultySettings, RandomizationSettings randomizationSettings) 
            : base(seed, difficultySettings, randomizationSettings)
        {
            int shopItemCount = items.Length - locations.Length;
            delinearizedShops = shopItemCount >= 12;
            normalShopFill = shopItemCount >= 5;
            rng = new Random(seed);
        }

        public override List<ILP> RandomizeItems()
        {
            ProgressionManager pm = BuildProgressionManager();
            ReachableLocations rl = new ReachableLocations(locations, pm);
            FilledLocations fl = new FilledLocations(locations);

            List<int> permutedLocations = rng.Permute(locations.Length).ToList();
            List<int> permutedItems = rng.Permute(items.Length).ToList();

            int[] shops = permutedLocations.Where(l => LocationData.data.GetLocationDef(locations[l]).pool == Data.Pool.Shop).ToArray();

            if (!normalShopFill)
            {
                permutedLocations.RemoveAll(i => shops.Contains(i));
            }

            var progressionLookup = permutedItems.ToLookup(i => ItemData.data.GetItemDef(items[i]).progression);
            List<int> junkItems = progressionLookup[false].ToList();
            List<int> progressionItems = progressionLookup[true].ToList();

            // this should not be used after dividing by progression
            permutedItems = null;

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
                            
                            //Logger.LogDebug($"Adding item {items[item]} at location {locations[location]} from Progression. After add: rc={rl.ReachableCount}");
                        }
                        break;

                    case NextItemState.ForceProgression:
                        // take the location out *before* updating rl with new item
                        int lastLocation = PopNextLocation(permutedLocations, rl);
                        if (TryForceItem(progressionItems, pm, rl, out int forcedItem))
                        {
                            // TryForce saves progression and modifies progressionItems
                            //Logger.LogDebug($"Adding item {items[forcedItem]} at location {locations[lastLocation]} from ForceProgression. After add: rc={rl.ReachableCount}");

                            fl.Fill(lastLocation, forcedItem);
                            Delinearize(rng, fl, permutedLocations, shops);
                            break;
                        }
                        else
                        {
                            permutedLocations.Insert(0, lastLocation);
                            goto case NextItemState.OverflowProgression;
                        }

                    case NextItemState.OverflowProgression:
                        {
                            permutedLocations.AddRange(rng.Permute(fl.ClearStandby().ToArray()));

                            //Logger.LogDebug($"Reached overflow with {progressionItems.Count} remaining progression items and {locations.Length - rl.ReachableCount} unreachable locations.");
                            //Logger.LogDebug($"Found {progressionLookup[true].Count(i => !pm.Has(items[i]))} progression items not in pm.");
                            //progressionLookup[true].Where(i => !pm.Has(items[i])).Select(i => items[i]).Log();

                            int startCount = rl.ReachableCount;
                            do
                            {
                                int overflowItem = progressionItems.Pop();
                                int location = PopNextLocation(permutedLocations, rl);

                                //Logger.LogDebug($"Adding item {items[overflowItem]} at location {locations[location]} from Overflow. After add: rc={rl.ReachableCount}");
                                
                                pm.Add(items[overflowItem]);
                                fl.Fill(location, overflowItem);

                                adjReachableCount = rl.ReachableCount;
                            }
                            while (adjReachableCount == startCount && progressionItems.Any());
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

            List<ILP> ILPs = fl.GetStringILPs(items, locations);
            HandleDupes(ILPs);

            return ILPs;
        }

        private enum NextItemState
        {
            Junk,
            Progression,
            ForceProgression,
            OverflowProgression
        }

        // Choice function used for placing progression items
        private NextItemState GetNextItemState(Random rng, int progressionCount, int itemCount, int adjReachableCount, int fullReachableCount)
        {
            if (fullReachableCount == locations.Length)
            {
                return adjReachableCount > 0 ? NextItemState.Progression : NextItemState.OverflowProgression;
            }

            if (adjReachableCount > 1)
            {
                int choose = rng.Next(itemCount);
                if (choose < progressionCount) return NextItemState.Progression;
                else return NextItemState.Junk;
            }
            else
            {
                return NextItemState.ForceProgression;
            }
        }

        public int PopNextLocation(List<int> permutedLocations, ReachableLocations rl)
        {
            for (int i = 0; i < permutedLocations.Count; i++)
            {
                if (rl.CanReach(permutedLocations[i]))
                {
                    return permutedLocations.Pop(i);
                }
            }

            throw new InvalidOperationException("Entered PopNextLocation with no reachable locations.");
        }

        public bool TryForceItem(List<int> progressionItems, ProgressionManager pm, ReachableLocations rl, out int item)
        {
            for (int i = 0; i < progressionItems.Count; i++)
            {
                item = progressionItems[i];
                if (items[item] is string sItem)
                {
                    if (TestItem(pm, rl, sItem))
                    {
                        progressionItems.RemoveAt(i);
                        return true;
                    }
                }
            }

            item = 0;
            return false;
        }

        public bool TestItem(ProgressionManager pm, ReachableLocations rl, string item) 
        {
            pm.AddTemp(item);
            if (rl.TempCount > 0)
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

        private void Delinearize(Random rng, FilledLocations fl, List<int> permutedLocations, int[] shops)
        {
            if (difficultySettings.Cursed) return;

            // add back shops for rare consideration for late progression
            if (delinearizedShops && rng.Next(8) == 0)
            {
                int shop = shops[rng.Next(5)];
                int index = rng.Next(permutedLocations.Count);
                permutedLocations.Insert(index, shop);
            }

            // release standby location for rerandomization
            if (rng.Next(2) == 0)
            {
                int[] standbyLocations = locations.Select((l,i) => i).Where(l => fl.IsStandby(l)).ToArray();
                if (standbyLocations.Any())
                {
                    int location = rng.Next(standbyLocations);
                    fl.Recirculate(location);
                    int index = rng.Next(permutedLocations.Count);
                    permutedLocations.Insert(index, location);
                }
            }
        }

        private void HandleDupes(List<ILP> ILPs)
        {
            List<ILP> shopDupes = ILPs.Where(p => iData.GetItemDef(p.item).pool == Pool.Dupe && lData.GetLocationDef(p.location).pool == Pool.Shop).ToList();
            if (!shopDupes.Any()) return;

            while (shopDupes.Any())
            {
                int j = rng.Next(ILPs.Count / 5, ILPs.Count); // this is *not* a good proxy for location order
                ILP p = ILPs[j];

                if (iData.GetItemDef(p.item).pool == Pool.Dupe || iData.IsProgression(p.item) || lData.GetLocationDef(p.location).pool == Pool.Shop)
                {
                    continue;
                }
                else
                {
                    ILP dupe = shopDupes.Pop();

                    ILPs.Remove(p);
                    ILPs.Remove(dupe);

                    string origLocation = dupe.location;
                    dupe.location = p.location;
                    p.location = origLocation;

                    ILPs.Add(dupe);
                    ILPs.Add(p);
                }
            }
        }
    }
}
