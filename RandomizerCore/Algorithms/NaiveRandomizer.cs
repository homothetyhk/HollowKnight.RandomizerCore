using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomizerCore.Data;

namespace RandomizerCore.Algorithms
{
    public class NaiveRandomizer : Randomizer
    {
        Random rng;

        public NaiveRandomizer(int seed, DifficultySettings difficultySettings, RandomizationSettings randomizationSettings)
            : base(seed, difficultySettings, randomizationSettings)
        {
            rng = new Random(seed);
        }


        public override List<ILP> RandomizeItems()
        {
            int[] itemOrder = rng.Permute(items.Length);
            List<ILP> ILPs = new List<ILP>();
            string[] shops = locations.Where(l => lData.GetLocationDef(l).pool == Data.Pool.Shop).ToArray();

            for (int i = 0; i< locations.Length; i++)
            {
                ILPs.Add(new ILP(items[itemOrder[i]], locations[i]));
            }
            for (int i = locations.Length; i < items.Length; i++)
            {
                ILPs.Add(new ILP(items[itemOrder[i]], rng.Next(shops)));
            }
            return ILPs;
        }
    }
}
