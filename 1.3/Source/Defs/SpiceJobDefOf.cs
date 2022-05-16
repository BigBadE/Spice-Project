using RimWorld;
using Verse;

namespace Spice.Defs
{
    [DefOf]
    public class SpiceJobDefOf
    {
        public static JobDef Spice_Hydrate;
        public static JobDef Spice_Drink;
        
        static SpiceJobDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (SpiceJobDefOf));
    }
}