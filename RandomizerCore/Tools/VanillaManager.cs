using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomizerCore.Data;

namespace RandomizerCore
{
    public class VanillaManager : PrePlacedManager
    {
        public VanillaManager(RandomizationSettings settings, ItemData iData, ProgressionManager pm) : base(GetVanillaPlacements(settings, iData), pm)
        {
        }

        public static List<ILP> GetVanillaPlacements(RandomizationSettings settings, ItemData iData) =>
            LocationData.data
            .Filter(def => !settings.GetRandomizeByPool(def.pool))
            .SelectMany(l => VanillaData.data.GetVanillaItems(l).Where(i => ItemData.data.IsProgression(i)).Select(i => new ILP(i, l)))
            .ToList();
    }
}
