using RimWorld;

namespace Spice.Defs
{
    [DefOf]
    public class SpiceStatsDefOf
    {
        public static StatDef Spice_Water;
        public static StatDef Spice_ThirstRateMultiplier;
        
        static SpiceStatsDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (SpiceStatsDefOf));
    }
}