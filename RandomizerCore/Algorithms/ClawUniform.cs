using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerCore.Algorithms
{
    public class ClawUniform : Randomizer
    {
        public readonly int clawLocation;
        readonly int clawIndex;
        const string clawName = "Mantis_Claw";
        Random rng;

        public ClawUniform(int seed, DifficultySettings difficultySettings, RandomizationSettings randomizationSettings)
            : base(seed, difficultySettings, randomizationSettings)
        {
            ProgressionManager pm = new ProgressionManager(logicManager, difficultySettings, wData, iData, cData);
            ReachableLocations rl = new ReachableLocations(locations, pm);
            pm.Add(sData.GetStartDef(randomizationSettings.StartName));
            IEnumerable<string> progression = items.Where(i => iData.GetItemDef(i).progression && i != clawName);
            //Logger.Log("Progression items used in ClawUniform:");
            //progression.Log();
            pm.Add(progression);
            pm.Add(transitions ?? new string[0]);
            rng = new Random(seed);

            string[] reachable = rl.GetReachableLocations();
            Logger.Log("Reachable locations found in ClawUniform:");
            reachable.Log();
            clawLocation = Array.IndexOf(locations, rng.Next(reachable));
            clawIndex = Array.IndexOf(items, clawName);
        }


        public override List<ILP> RandomizeItems()
        {
            int[] itemOrder = rng.Permute(items.Length);
            int j = Array.IndexOf(itemOrder, clawIndex);
            itemOrder.Swap(j, clawLocation);

            List<ILP> ILPs = new List<ILP>();

            string[] shops = locations.Where(l => lData.GetLocationDef(l).pool == Data.Pool.Shop).ToArray();

            for (int i = 0; i < locations.Length; i++)
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
