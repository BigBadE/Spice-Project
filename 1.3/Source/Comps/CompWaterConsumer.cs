using Verse;

namespace Spice.Comps
{
    public class CompWaterConsumer : ThingComp
    {
        public int waterDraw;
        public bool enabled;
        public bool hasWater;
        
        public virtual int NeededWater() => enabled ? waterDraw : 0;

        public virtual bool Consume(int water)
        {
            if(water > waterDraw)
            {
                hasWater = true;
                return true;
            }
            else
            {
                hasWater = false;
                return false;
            }
        }
    }
}