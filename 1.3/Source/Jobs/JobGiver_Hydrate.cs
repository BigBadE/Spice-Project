using RimWorld;
using Spice.Defs;
using Spice.Needs;
using Spice.Util;
using Verse;
using Verse.AI;

namespace Spice.Jobs
{
    public class JobGiver_Hydrate : ThinkNode_JobGiver
  {
    private float minLevelPercentage;
    private float maxLevelPercentage = 1f;
    public bool forceScanWholeMap;

    public override ThinkNode DeepCopy(bool resolve = true)
    {
      JobGiver_Hydrate jobGiverGetFood = (JobGiver_Hydrate) base.DeepCopy(resolve);
      jobGiverGetFood.minLevelPercentage = minLevelPercentage;
      jobGiverGetFood.maxLevelPercentage = maxLevelPercentage;
      jobGiverGetFood.forceScanWholeMap = forceScanWholeMap;
      return jobGiverGetFood;
    }

    public override float GetPriority(Pawn pawn)
    {
      Need_Water water = pawn.needs.TryGetNeed<Need_Water>();
      return water == null || water.CurLevel == 0 && 
        FoodUtility.ShouldBeFedBySomeone(pawn) || water.CurLevelPercentage < minLevelPercentage || 
        water.CurLevelPercentage > (double) maxLevelPercentage  ? 0.0f : 9.5f;
    }

    protected override Job TryGiveJob(Pawn pawn)
    {
      Need_Water water = pawn.needs.TryGetNeed<Need_Water>();
      if (water == null || water.CurLevelPercentage < minLevelPercentage || water.CurLevelPercentage > maxLevelPercentage)
        return null;
      
      Thing waterSource;
      if (!WaterUtility.TryFindBestFoodSourceFor_NewTemp(pawn, pawn, out waterSource,
        canUsePackAnimalInventory: true, forceScanWholeMap: forceScanWholeMap))
      {
        return null;
      }

      float itemWater = WaterUtility.GetWater(waterSource);
      Pawn pawn2 = waterSource.ParentHolder is Pawn_InventoryTracker parentHolder ? parentHolder.pawn : null;
      if (pawn2 != null && pawn2 != pawn)
      {
        Job job3 = JobMaker.MakeJob(JobDefOf.TakeFromOtherInventory, (LocalTargetInfo) waterSource, (LocalTargetInfo) (Thing) pawn2);
        job3.count = WaterUtility.WillDrinkStackCountOf(pawn, itemWater);
        return job3;
      }
      Job job4 = JobMaker.MakeJob(SpiceJobDefOf.Spice_Drink, (LocalTargetInfo) waterSource);
      job4.count = WaterUtility.WillDrinkStackCountOf(pawn, itemWater);
      return job4;
    }
  }
}