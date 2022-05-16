using RimWorld;
using Verse;

namespace Spice.Defs
{
    [DefOf]
    public class SpiceHediffsDefOf
    {
        public static HediffDef Spice_Dehydration;
        
        static SpiceHediffsDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (SpiceHediffsDefOf));
    }
}