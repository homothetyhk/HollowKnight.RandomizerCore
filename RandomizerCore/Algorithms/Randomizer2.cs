using RandomizerCore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;

namespace RandomizerCore.Algorithms
{
    public class Randomizer2 : Randomizer
    {
        readonly Random rng;

        public Randomizer2(int seed, DifficultySettings difficultySettings, RandomizationSettings randomizationSettings)
            : base(seed, difficultySettings, randomizationSettings)
        {
            rng = new Random(seed);
        }

        public override List<ILP> RandomizeItems()
        {
            int[] locationDepths = new int[locations.Length];
            int currentDepth = 1;
            ProgressionManager pm = BuildProgressionManager();
            ReachableLocations rl = new ReachableLocations(locations, pm, autoupdate: true);
            List<int> unplacedItems = Enumerable.Range(0, items.Length).ToList();

            List<int> weightedLocations = new List<int>();

            List<int> progression = GetForceProgressionList(unplacedItems, rl, pm);
            FilledLocations fl = new FilledLocations(locations);

            // first pass: essential progression
            while (progression.Any())
            {
                UpdateWithWeights(rl, locationDepths, currentDepth, weightedLocations);
                int location = rng.Next(weightedLocations);
                int item = rng.Next(progression);

                weightedLocations.RemoveAll(i => i == location);
                unplacedItems.Remove(item);
                pm.Add(items[item]);

                fl.Fill(location, item);
                progression = GetForceProgressionList(unplacedItems, rl, pm);
                currentDepth++;
            }
            Logger.LogDebug($"Exited first pass with depth {currentDepth} and adj reachable {rl.ReachableCount - fl.NonemptyCount}");

            // second pass: unused progression
            SquareWeightedLocations(weightedLocations, locationDepths);
            progression = GetUnusedProgressionList(unplacedItems);
            while (progression.Any())
            {
                int item = rng.Next(progression);
                int location = rng.Next(weightedLocations);

                pm.Add(items[item]);
                progression.Remove(item);
                unplacedItems.Remove(item);
                weightedLocations.RemoveAll(i => i == location);

                fl.Fill(location, item);
                UpdateWithSquaredWeights(rl, locationDepths, currentDepth++, weightedLocations);
            }

            // third pass: junk in remaining locations
            List<int> unplacedLocations = Enumerable.Range(0, locations.Length).Where(l => fl.IsEmpty(l)).ToList();
            while (unplacedLocations.Any())
            {
                int item = rng.Next(unplacedItems);
                int location = rng.Next(unplacedLocations);

                unplacedItems.Remove(item);
                unplacedLocations.Remove(location);

                fl.Fill(location, item);
            }

            // fourth pass: junk in random shops
            int[] shops = locations.Select((s, i) => new Pair<string, int>(s, i)).Where(p => lData.GetLocationDef(p.Item1).pool == Data.Pool.Shop).Select(p => p.Item2).ToArray();
            while (unplacedItems.Any())
            {
                int item = rng.Next(unplacedItems);
                int location = rng.Next(shops);

                unplacedItems.Remove(item);
                fl.Fill(location, item);
            }

            List<ILP> ILPs = fl.GetStringILPs(items, locations);
            HandleDupes(ILPs);
            return ILPs;
        }

        private void UpdateWithWeights(ReachableLocations rl, int[] locationDepths, int currentDepth, List<int> weightedLocations)
        {
            for (int i = 0; i < locations.Length; i++)
            {
                if (rl.CanReach(i) && locationDepths[i] == 0)
                {
                    locationDepths[i] = currentDepth;
                    weightedLocations.AddRange(Enumerable.Repeat(i, currentDepth));
                }
            }
        }

        private void UpdateWithSquaredWeights(ReachableLocations rl, int[] locationDepths, int currentDepth, List<int> weightedLocations)
        {
            for (int i = 0; i < locations.Length; i++)
            {
                if (rl.CanReach(i) && locationDepths[i] == 0)
                {
                    locationDepths[i] = currentDepth;
                    weightedLocations.AddRange(Enumerable.Repeat(i, currentDepth * currentDepth));
                }
            }
        }

        private void SquareWeightedLocations(List<int> weightedLocations, int[] locationDepths)
        {
            HashSet<int> unplaced = new HashSet<int>(weightedLocations);
            weightedLocations = new List<int>();
            foreach (int i in unplaced)
            {
                weightedLocations.AddRange(Enumerable.Repeat(i, locationDepths[i] * locationDepths[i]));
            }
        }

        private List<int> GetForceProgressionList(List<int> unplacedItems, ReachableLocations rl, ProgressionManager pm)
        {
            List<int> progression = new List<int>();

            foreach (int item in unplacedItems)
            {
                if (!iData.IsProgression(items[item])) continue;

                if (TestItem(pm ,rl, items[item]))
                {
                    progression.Add(item);
                }
            }

            return progression;
        }

        private List<int> GetUnusedProgressionList(List<int> unplacedItems)
        {
            return unplacedItems.Where(i => iData.IsProgression(items[i])).ToList();
        }

        private bool TestItem(ProgressionManager pm, ReachableLocations rl, string item)
        {
            pm.AddTemp(item);
            if (rl.TempCount > 0)
            {
                pm.RemoveTempItems();
                return true;
            }
            else
            {
                pm.RemoveTempItems();
                return false;
            }
        }

        private void HandleDupes(List<ILP> ILPs)
        {
            List<ILP> shopDupes = ILPs.Where(p => iData.GetItemDef(p.item).pool == Pool.Dupe && lData.GetLocationDef(p.location).pool == Pool.Shop).ToList();
            if (!shopDupes.Any()) return;

            while (shopDupes.Any())
            {
                int j = rng.Next(ILPs.Count / 5, ILPs.Count);
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
