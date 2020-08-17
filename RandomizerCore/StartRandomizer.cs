using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomizerCore.Data;

namespace RandomizerCore
{
    public class StartRandomizer
    {
        int seed;
        RandomizationSettings settings;
        StartData sData;


        public StartRandomizer(int seed, RandomizationSettings settings, StartData sData)
        {
            this.seed = seed;
            this.settings = settings;
            this.sData = sData;
        }

        public void RandomizeStart()
        {
            if (!settings.RandomizeStartLocation)
            {
                return;
            }
            else
            {
                Random rng = new Random(seed);
                if (settings.RandomizeStartItems)
                {
                    settings.StartName = rng.Next(sData.StartLocations);
                }
                else if (settings.RandomizeRooms)
                {
                    settings.StartName = rng.Next(sData.StartLocations.Where(s => sData.GetStartDef(s).roomSafe).ToArray());
                }
                else if (settings.RandomizeAreas)
                {
                    settings.StartName = rng.Next(sData.StartLocations.Where(s => sData.GetStartDef(s).areaSafe).ToArray());
                }
                else
                {
                    settings.StartName = rng.Next(sData.StartLocations.Where(s => sData.GetStartDef(s).itemSafe).ToArray());
                }
            }
        }

        public List<string> GetRandomStartItems()
        {
            List<string> items = new List<string>();
            return items;
        }
    }
}
