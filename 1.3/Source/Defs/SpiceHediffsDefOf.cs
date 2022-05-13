using RimWorld;
using Verse;

namespace Spice.Hediffs
{
    [DefOf]
    public class SpiceHediffsDefOf
    {
        public static HediffDef Spice_Dehydration;
        
        static SpiceHediffsDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (SpiceHediffsDefOf));
    }
}