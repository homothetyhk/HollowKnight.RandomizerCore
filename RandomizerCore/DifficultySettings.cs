using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace RandomizerCore
{
    public class DifficultySettings
    {
        public bool ShadeSkips;
        public bool AcidSkips;
        public bool SpikeTunnels;
        public bool MildSkips;
        public bool SpicySkips;
        public bool FireballSkips;
        public bool DarkRooms;
        public bool Cursed;

        public override string ToString()
        {
            string s = string.Empty;
            foreach (FieldInfo f in typeof(DifficultySettings).GetFields())
            {
                s += $"{f.Name}: {f.GetValue(this)}\n";
            }
            return s;
        }
    }
}
