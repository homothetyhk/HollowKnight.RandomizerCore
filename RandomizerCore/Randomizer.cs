using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomizerCore.Data;

namespace RandomizerCore
{
    // rip ValueTuple
    public struct ILP
    {
        public string item;
        public string location;

        public ILP(string item, string location)
        {
            this.item = item;
            this.location = location;
        }

        public override string ToString()
        {
            return $"i: {item}, l: {location}";
        }
    }

    public abstract class Randomizer
    {
        public Randomizer(int seed, DifficultySettings difficultySettings, RandomizationSettings randomizationSettings)
        {
            this.seed = seed;
            this.difficultySettings = difficultySettings;
            this.randomizationSettings = randomizationSettings;
            this.items = GetItems();
            this.locations = GetLocations();
            this.transitions = GetTransitions();
        }

        public void Run()
        {
            attempts = 0;
            StartRandomizer.RandomizeStart();
            StartRandomizer.GetRandomStartItems();

            if (randomizationSettings.RandomizeTransitions)
            {
                do
                {
                    attempts++;
                    TPs = RandomizeTransitions();
                    ILPs = RandomizeItems(TPs);
                }
                while (!Validator.Validate(ILPs, sData.GetStartDef(randomizationSettings.StartName), TPs));
            }
            else
            {
                do
                {
                    attempts++;
                    ILPs = RandomizeItems();
                }
                while (!Validator.Validate(ILPs, sData.GetStartDef(randomizationSettings.StartName)));
            }
        }

        public abstract List<ILP> RandomizeItems();
        public virtual List<ILP> RandomizeItems(Dictionary<string, string> transitionPlacements) => RandomizeItems();
        public virtual Dictionary<string, string> RandomizeTransitions() => new Dictionary<string, string>();

        public string[] items;
        public string[] locations;
        public string[] transitions;
        public List<ILP> ILPs;
        public Dictionary<string, string> TPs;
        public int attempts;

        public readonly int seed;
        public readonly DifficultySettings difficultySettings;
        public readonly RandomizationSettings randomizationSettings;
        private CostData _cData;
        private Validator _validator;
        private CostRandomizer _costRandomizer;
        private StartRandomizer _startRandomizer;

        public ProgressionManager BuildProgressionManager()
        {
            ProgressionManager pm = new ProgressionManager(logicManager, difficultySettings, wData, iData, cData);
            pm.Add(sData.GetStartDef(randomizationSettings.StartName));
            return pm;
        }

        public virtual string[] GetItems()
        {
            List<Pool> pools = randomizationSettings.GetItemPools();
            IEnumerable<string> uniqueItems = iData.Filter(def => pools.Contains(def.pool)).ToArray();
            List<string> items = new List<string>();
            foreach (string item in uniqueItems)
            {
                int count = Math.Max(iData.GetItemDef(item).count, 1);
                items.AddRange(Enumerable.Repeat(item, count));
            }
            return items.ToArray();
        }

        public virtual string[] GetLocations()
        {
            List<Pool> pools = randomizationSettings.GetLocationPools();
            return lData.Filter(def => pools.Contains(def.pool)).ToArray();
        }

        public virtual string[] GetTransitions()
        {
            return tData?.TransitionNames;
        }

        public virtual string GetSpoiler()
        {
            if (!Validator.Validated) return null;
            string s = string.Empty;

            s += "ITEM PLACEMENTS\n\n";
            foreach (ILP p in ILPs)
            {
                s += $"{p.item}<---at--->{p.location}\n";
            }

            if (randomizationSettings.RandomizeTransitions)
            {
                s += "\nTRANSITION PLACEMENTS\n";
                foreach (var kvp in TPs)
                {
                    s += $"{kvp.Key}<------>{kvp.Value}\n";
                }
            }

            s += $"\nSeed: {seed}";
            s += $"Difficulty Settings:\n{difficultySettings}";
            s += $"Randomization Settings:\n{difficultySettings}";

            return s;
        }

        public virtual Validator Validator
        {
            get => _validator = _validator ?? new Validator(ValidationFlags, this);
        }

        public virtual ValidationFlag[] ValidationFlags
        {
            get => new ValidationFlag[]
            {
                ValidationFlag.AllItemsPlaced,
                ValidationFlag.AllLocationsSingleFilledOrShop,
                ValidationFlag.AllLocationsReachable
            };
        }

        public virtual CostRandomizer CostRandomizer
        {
            get => _costRandomizer = _costRandomizer ?? new CostRandomizer(seed, locations, lData, logicManager);
        }

        public virtual CostData cData
        {
            get => _cData = _cData ?? CostRandomizer.Randomize();
        }

        public virtual StartRandomizer StartRandomizer
        {
            get => _startRandomizer = _startRandomizer ?? new StartRandomizer(seed, randomizationSettings, sData);
        }

        public virtual LogicManager logicManager
        {
            get => randomizationSettings.RandomizeRooms ? LogicManager.RoomLogicManager : 
                randomizationSettings.RandomizeAreas ? LogicManager.AreaLogicManager : LogicManager.ItemLogicManager;
        }

        public virtual WaypointData wData
        {
            get => randomizationSettings.RandomizeRooms ? null : WaypointData.data;
        }

        public virtual ItemData iData
        {
            get => ItemData.data;
        }

        public virtual LocationData lData
        {
            get => LocationData.data;
        }

        public virtual TransitionData tData
        {
            get => randomizationSettings.RandomizeRooms ? TransitionData.roomData : TransitionData.areaData;
        }

        public virtual StartData sData
        {
            get => StartData.data;
        }
    }
}
