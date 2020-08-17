using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerCore
{
    public class FilledLocations
    {
        struct IndexPair
        {
            public int itemIndex;
            public int locationIndex;

            public IndexPair(int i1, int i2)
            {
                itemIndex = i1;
                locationIndex = i2;
            }
        }


        private enum FillState
        {
            Empty = 0,
            Filled = 1,
            Standby = 2,
        }

        FillState[] fillStates;
        List<IndexPair> ILPs = new List<IndexPair>();
        public int NonemptyCount { get; private set; } = 0;


        public FilledLocations(string[] locations)
        {
            fillStates = new FillState[locations.Length];
        }

        public void Fill(int location, int item)
        {
            NonemptyCount++;
            fillStates[location] = FillState.Filled;
            ILPs.Add(new IndexPair(item, location));
        }

        public void PlaceStandby(int location)
        {
            NonemptyCount++;
            fillStates[location] = FillState.Standby;
        }

        public List<int> ClearStandby()
        {
            List<int> standbyIndices = new List<int>();

            for (int i = 0; i < fillStates.Length; i++)
            {
                if (fillStates[i] == FillState.Standby)
                {
                    fillStates[i] = FillState.Empty;
                    standbyIndices.Add(i);
                }
            }

            return standbyIndices;
        }

        public void FillStandby(int location, int item)
        {
            fillStates[location] = FillState.Filled;
            ILPs.Add(new IndexPair(item, location));
        }

        public void Recirculate(int location)
        {
            NonemptyCount--;
            fillStates[location] = FillState.Empty;
        }

        public bool IsEmpty(int location)
        {
            return fillStates[location] == FillState.Empty;
        }

        public bool IsStandby(int location)
        {
            return fillStates[location] == FillState.Standby;
        }

        public bool IsFilled(int location)
        {
            return fillStates[location] == FillState.Filled;
        }

        public List<ILP> GetStringILPs(string[] items, string[] locations)
        {
            return ILPs.Select(pair => new ILP(items[pair.itemIndex], locations[pair.locationIndex])).ToList();
        }
    }
}
