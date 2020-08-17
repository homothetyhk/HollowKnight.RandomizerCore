using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using RandomizerCore.Data;

namespace RandomizerCore
{
    public class CostData
    {
        public Dictionary<string, int> modifiedGrubCosts;
        public Dictionary<string, int> modifiedEssenceCosts;
        public Dictionary<string, int> modifiedSimpleKeyCosts;

        public void Log()
        {
            foreach (var kvp in modifiedGrubCosts) Logger.LogFine($"{kvp.Key}: grub cost of {kvp.Value}");
            foreach (var kvp in modifiedEssenceCosts) Logger.LogFine($"{kvp.Key}: essence cost of {kvp.Value}");
            foreach (var kvp in modifiedSimpleKeyCosts) Logger.LogFine($"{kvp.Key}: simple cost of {kvp.Value}");
        }
    }

    public class CostRandomizer
    {
        public readonly int GrubCostMax;
        public readonly int GrubCostMin;

        public readonly int EssenceCostMax;
        public readonly int EssenceCostMin;

        readonly string[] locations;
        readonly Random rng;

        readonly LogicManager logicManager;
        readonly LocationData lData;


        public CostRandomizer(int seed, string[] locations, LocationData lData, LogicManager logicManager, int grubCostMax = 23, int grubCostMin = 1, int EssenceCostMax = 900, int EssenceCostMin = 1)
        {
            this.locations = locations;
            rng = new Random(seed);
            this.lData = lData;
            this.logicManager = logicManager;

            GrubCostMax = grubCostMax;
            GrubCostMin = grubCostMin;

            this.EssenceCostMax = EssenceCostMax;
            this.EssenceCostMin = EssenceCostMin;
        }

        public CostData Randomize()
        {
            return new CostData
            {
                modifiedGrubCosts = RandomizeGrubCosts(),
                modifiedEssenceCosts = RandomizeEssenceCosts(),
                modifiedSimpleKeyCosts = new Dictionary<string, int>(),
            };
        }

        Dictionary<string, int> RandomizeGrubCosts()
        {
            Dictionary<string, int> grubCosts = new Dictionary<string, int>();
            foreach (string location in locations)
            {
                if (lData.GetLocationDef(location).costType == CostType.Grub)
                {
                    int cost = NextGrubCost();
                    grubCosts.Add(location, cost);
                }
            }
            return grubCosts;
        }

        Dictionary<string, int> RandomizeEssenceCosts()
        {
            Dictionary<string, int> EssenceCosts = new Dictionary<string, int>();
            foreach (string location in locations)
            {
                if (lData.GetLocationDef(location).costType == CostType.Essence)
                {
                    int cost = NextEssenceCost();
                    EssenceCosts.Add(location, cost);
                }
            }
            return EssenceCosts;
        }

        private int NextGrubCost()
        {
            return rng.Next(GrubCostMax) + GrubCostMin;
        }

        private int NextEssenceCost()
        {
            return rng.Next(EssenceCostMax) + EssenceCostMin;
        }
    }
}
