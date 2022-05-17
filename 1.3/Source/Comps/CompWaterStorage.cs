using System;
using RimWorld;
using Verse;

namespace Spice.Comps
{
    public class CompWaterStorage : ThingComp
    {
        public int water;

        public virtual void DrawWater(int drawing)
        {
            if (drawing > water)
            {
                Log.Warning("Tried to draw more water than stored from " + parent + "\n" + new Exception());
            }

            water -= drawing;
        }
    }
}