using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RandomizerCore.Data;

namespace RandomizerCore
{
    public class RandomizationSettings
    {
        public bool RandomizeTransitions => RandomizeAreas || RandomizeRooms;
        public bool RandomizeAreas;
        public bool RandomizeRooms;

        public bool CreateSpoilerLog;

        public bool Cursed;
        public bool RandomizeStartItems;
        public bool RandomizeStartLocation;
        public string StartName;

        public bool RandomizeDreamers;
        public bool RandomizeSkills;
        public bool RandomizeCharms;
        public bool RandomizeKeys;
        public bool RandomizeGeoChests;
        public bool RandomizeMaskShards;
        public bool RandomizeVesselFragments;
        public bool RandomizeCharmNotches;
        public bool RandomizePaleOre;
        public bool RandomizeRancidEggs;
        public bool RandomizeRelics;
        public bool RandomizeMaps;
        public bool RandomizeStags;
        public bool RandomizeGrubs;
        public bool RandomizeWhisperingRoots;

        public bool DuplicateMajorItems;

        public bool GetRandomizeByPool(string pool)
        {
            try
            {
                return GetRandomizeByPool((Pool)Enum.Parse(typeof(Pool), pool));
            }
            catch
            {
                return false;
            }
        }

        public bool GetRandomizeByPool(Pool pool)
        {
            switch (pool)
            {
                case Pool.Shop:
                    return true;
                case Pool.Start:
                    return RandomizeStartItems;
                case Pool.Dreamer:
                    return RandomizeDreamers;
                case Pool.Skill:
                    return RandomizeSkills;
                case Pool.Charm:
                    return RandomizeCharms;
                case Pool.Key:
                    return RandomizeKeys;
                case Pool.Mask:
                    return RandomizeMaskShards;
                case Pool.Vessel:
                    return RandomizeVesselFragments;
                case Pool.Ore:
                    return RandomizePaleOre;
                case Pool.Notch:
                    return RandomizeCharmNotches;
                case Pool.Geo:
                    return RandomizeGeoChests;
                case Pool.Egg:
                    return RandomizeRancidEggs;
                case Pool.Relic:
                    return RandomizeRelics;
                case Pool.Map:
                    return RandomizeMaps;
                case Pool.Stag:
                    return RandomizeStags;
                case Pool.Grub:
                    return RandomizeGrubs;
                case Pool.Root:
                    return RandomizeWhisperingRoots;
                case Pool.Dupe:
                    return DuplicateMajorItems;
                default:
                    return false;
            }
        }

        public List<Pool> GetItemPools()
        {
            List<Pool> pools = new List<Pool>();
            if (RandomizeDreamers) pools.Add(Pool.Dreamer);
            if (RandomizeSkills) pools.Add(Pool.Skill);
            if (RandomizeCharms) pools.Add(Pool.Charm);
            if (RandomizeKeys) pools.Add(Pool.Key);
            if (RandomizeGeoChests) pools.Add(Pool.Geo);
            if (RandomizeMaskShards) pools.Add(Pool.Mask);
            if (RandomizeVesselFragments) pools.Add(Pool.Vessel);
            if (RandomizePaleOre) pools.Add(Pool.Ore);
            if (RandomizeCharmNotches) pools.Add(Pool.Notch);
            if (RandomizeRancidEggs) pools.Add(Pool.Egg);
            if (RandomizeRelics) pools.Add(Pool.Relic);
            if (RandomizeMaps) pools.Add(Pool.Map);
            if (RandomizeStags) pools.Add(Pool.Stag);
            if (RandomizeGrubs) pools.Add(Pool.Grub);
            if (RandomizeWhisperingRoots) pools.Add(Pool.Root);
            if (DuplicateMajorItems) pools.Add(Pool.Dupe);
            return pools;
        }

        public List<Pool> GetLocationPools()
        {
            List<Pool> pools = new List<Pool>();
            pools.Add(Pool.Shop);
            if (RandomizeStartItems) pools.Add(Pool.Start);
            pools.AddRange(GetItemPools());
            return pools;
        }

        public override string ToString()
        {
            string s = string.Empty;
            foreach (FieldInfo f in typeof(RandomizationSettings).GetFields())
            {
                s += $"{f.Name}: {f.GetValue(this)}\n";
            }
            return s;
        }
    }
}
