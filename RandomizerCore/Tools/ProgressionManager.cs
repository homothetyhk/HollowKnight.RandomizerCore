using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using RandomizerCore.Data;

namespace RandomizerCore
{
    public class ProgressionManager
    {
        public bool[] obtained;

        public int essence;
        public int essenceTolerance;
        public int grubs;
        public int grubTolerance;
        public int simpleKeys;
        private LogicManager lm;
        private ItemData iData;
        public readonly CostData cData;

        private int Index(string progression) => lm.GetProgressionIndex(progression);

        private bool temp;
        private bool updateWaypoints;

        private string[] waypointNames;
        public HashSet<string> tempItems;

        public delegate void AfterAddItemHandler(bool temp);
        public AfterAddItemHandler AfterAddItem = new AfterAddItemHandler((bool temp) => { });

        public delegate void AfterEndTempHandler(bool tempSaved);
        public AfterEndTempHandler AfterEndTemp = new AfterEndTempHandler((bool tempSaved) => { });

        public ProgressionManager(LogicManager lm, DifficultySettings settings, WaypointData waypoints, ItemData itemData, CostData costData)
        {
            this.lm = lm;
            obtained = new bool[lm.progressionMax];

            if (waypoints != null)
            {
                this.updateWaypoints = true;
                waypointNames = waypoints.WaypointNames;
            }
            iData = itemData;
            cData = costData;

            ApplyDifficultySettings(settings);
        }

        public bool Has(string item)
        {
            return obtained[Index(item)];
        }

        public bool CanGet(string item)
        {
            return lm.ParseLogic(this, item);
        }

        public void Add(string item)
        {
            if (temp)
            {
                tempItems.Add(item);
            }

            obtained[Index(item)] = true;
            if (iData.CheckIfIntProgression(item, out IntType type, out int value))
            {
                switch (type)
                {
                    case IntType.Essence:
                        essence += value;
                        break;
                    case IntType.Grub:
                        grubs += value;
                        break;
                    case IntType.Simple:
                        simpleKeys += value;
                        break;
                }
            }

            UpdateWaypoints();
            AfterAddItem.Invoke(temp);
        }

        public void Add(StartDef start)
        {
            if (updateWaypoints) Add(start.waypoint);
            if (lm.mode == LogicMode.Area) Add(start.areaTransition);
            if (lm.mode == LogicMode.Room) Add(start.roomTransition);
            UpdateWaypoints();
        }

        public void Remove(string item)
        {
            obtained[Index(item)] = false;
            if (iData.CheckIfIntProgression(item, out IntType type, out int value))
            {
                switch (type)
                {
                    case IntType.Essence:
                        essence -= value;
                        break;
                    case IntType.Grub:
                        grubs -= value;
                        break;
                    case IntType.Simple:
                        simpleKeys -= value;
                        break;
                }
            }
        }

        public void Add(IEnumerable<string> items)
        {
            foreach (string item in items)
            {
                obtained[Index(item)] = true;
                if (temp)
                {
                    tempItems.Add(item);
                }
                if (iData.CheckIfIntProgression(item, out IntType type, out int value))
                {
                    switch (type)
                    {
                        case IntType.Essence:
                            essence += value;
                            break;
                        case IntType.Grub:
                            grubs += value;
                            break;
                        case IntType.Simple:
                            simpleKeys += value;
                            break;
                    }
                }
            }
            UpdateWaypoints();
            AfterAddItem.Invoke(temp);
        }

        public void Remove(IEnumerable<string> items)
        {
            foreach (string item in items)
            {
                obtained[Index(item)] = false;
                if (iData.CheckIfIntProgression(item, out IntType type, out int value))
                {
                    switch (type)
                    {
                        case IntType.Essence:
                            essence -= value;
                            break;
                        case IntType.Grub:
                            grubs -= value;
                            break;
                        case IntType.Simple:
                            simpleKeys -= value;
                            break;
                    }
                }
            }
        }

        public void AddTemp(string item)
        {
            temp = true;
            if (tempItems == null)
            {
                tempItems = new HashSet<string>();
            }
            Add(item);
        }

        public void RemoveTempItems()
        {
            temp = false;
            Remove(tempItems);
            tempItems = new HashSet<string>();
            AfterEndTemp.Invoke(false);
        }

        public void SaveTempItems()
        {
            temp = false;

            tempItems = new HashSet<string>();
            AfterEndTemp.Invoke(true);
        }

        private void ApplyDifficultySettings(DifficultySettings settings)
        {
            if (settings.ShadeSkips) Add("SHADESKIPS");
            if (settings.AcidSkips) Add("ACIDSKIPS");
            if (settings.SpikeTunnels) Add("SPIKETUNNELS");
            if (settings.SpicySkips) Add("SPICYSKIPS");
            if (settings.FireballSkips) Add("FIREBALLSKIPS");
            if (settings.DarkRooms) Add("DARKROOMS");
            if (settings.MildSkips) Add("MILDSKIPS");
            if (!settings.Cursed) Add("NOTCURSED");
            if (settings.Cursed) Add("CURSED");

            essenceTolerance = settings.SpicySkips ? 50 : settings.MildSkips ? 100 : 150;
            grubTolerance = settings.SpicySkips ? 1 : settings.MildSkips ? 2 : 3;
        }

        public void UpdateWaypoints()
        {
            if (!updateWaypoints) return;

            foreach(string waypoint in waypointNames)
            {
                if (!Has(waypoint) && CanGet(waypoint))
                {
                    Add(waypoint);
                }
            }
        }
    }
}
