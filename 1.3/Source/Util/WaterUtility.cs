using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Spice.Comps;
using Spice.Defs;
using Spice.Needs;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Spice.Util
{
    public static class WaterUtility
    {
        private static HashSet<Thing> filtered = new HashSet<Thing>();

        public static bool TryFindBestFoodSourceFor_NewTemp(
            Pawn getter,
            Pawn eater,
            out Thing foodSource,
            bool canUseInventory = true,
            bool canUsePackAnimalInventory = false,
            bool allowForbidden = false,
            bool forceScanWholeMap = false,
            bool ignoreReservations = false)
        {
            bool canManipulateTools = getter.RaceProps.ToolUser &&
                                      getter.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
            Pawn packAnimal = null;
            if (canUseInventory && canManipulateTools)
            {
                Thing inventoryWater = BestWaterInInventory(getter);

                if (inventoryWater != null)
                {
                    foodSource = inventoryWater;
                    return true;
                }
            }

            Thing thing2 = BestWaterSourceOnMap(getter, eater, allowForbidden, forceScanWholeMap, ignoreReservations);
            if (thing2 == null && canUseInventory && canUsePackAnimalInventory &&
                canManipulateTools &&
                eater.IsColonist && getter.IsColonist && getter.Map != null)
            {
                thing2 = FirstWaterInClosestPackAnimalInventory(getter, ref packAnimal);
            }

            if (thing2 != null)
            {
                foodSource = thing2;
                return true;
            }

            foodSource = null;
            return false;
        }

        public static Thing BestWaterSourceOnMap(
            Pawn getter,
            Pawn eater,
            bool allowForbidden = false,
            bool forceScanWholeMap = false,
            bool ignoreReservations = false)
        {
            bool getterCanManipulate = getter.RaceProps.ToolUser &&
                                       getter.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
            if (!getterCanManipulate && getter != eater)
            {
                Log.Error(getter + " tried to find water to bring to " + eater + " but " +
                          getter + " is incapable of Manipulation.");
                return null;
            }

            bool FoodValidator(Thing t)
            {
                int stackCount = 1;
                return t.def.HasModExtension<WaterModExtension>() && (allowForbidden || !t.IsForbidden(getter)) &&
                       (getter.AnimalAwareOf(t) || forceScanWholeMap) &&
                       (ignoreReservations || getter.CanReserve((LocalTargetInfo) t, 10, stackCount));
            }

            ThingRequest thingRequest = ThingRequest.ForGroup(ThingRequestGroup.HaulableEverOrMinifiable);
            Thing bestThing;
            if (getter.RaceProps.Humanlike)
            {
                bestThing = SpawnedWaterSearchInnerScan(eater, getter.Position,
                    getter.Map.listerThings.ThingsMatching(thingRequest), PathEndMode.ClosestTouch,
                    TraverseParms.For(getter), validator: FoodValidator);
            }
            else
            {
                int maxRegionsToScan = getter.Faction == Faction.OfPlayer ? 100 : 30;
                filtered.Clear();
                foreach (Thing thing in GenRadial.RadialDistinctThingsAround(getter.Position, getter.Map, 2f, true))
                {
                    if (thing is Pawn pawn3 && pawn3 != getter && pawn3.RaceProps.Animal && pawn3.CurJob != null &&
                        pawn3.CurJob.def == JobDefOf.Ingest && pawn3.CurJob.GetTarget(TargetIndex.A).HasThing)
                        filtered.Add(pawn3.CurJob.GetTarget(TargetIndex.A).Thing);
                }

                bool ignoreEntirelyForbiddenRegions = !allowForbidden &&
                                                      ForbidUtility.CaresAboutForbidden(getter, true) &&
                                                      getter.playerSettings != null && getter.playerSettings
                                                          .EffectiveAreaRestrictionInPawnCurrentMap != null;
                bestThing = GenClosest.ClosestThingReachable(getter.Position, getter.Map, thingRequest,
                    PathEndMode.ClosestTouch, TraverseParms.For(getter),
                    validator: thing => FoodValidator(thing) && !filtered.Contains(thing),
                    searchRegionsMax: maxRegionsToScan, ignoreEntirelyForbiddenRegions: ignoreEntirelyForbiddenRegions);
                filtered.Clear();
                if (bestThing == null)
                {
                    bestThing = GenClosest.ClosestThingReachable(getter.Position, getter.Map, thingRequest,
                        PathEndMode.ClosestTouch, TraverseParms.For(getter), validator: FoodValidator,
                        searchRegionsMax: maxRegionsToScan,
                        ignoreEntirelyForbiddenRegions: ignoreEntirelyForbiddenRegions);
                }
            }

            return bestThing;
        }

        public static float GetWater(Thing waterSource)
        {
            return waterSource?.GetStatValue(SpiceStatsDefOf.Spice_Water) ?? waterSource.TryGetComp<CompWaterStorage>().water;
        }
        
        public static int WillDrinkStackCountOf(Pawn ingester, float singleDrinkWater)
        {
            int num = StackCountForWater(ingester.needs.TryGetNeed<Need_Water>().WaterWanted, singleDrinkWater);
            if (num < 1)
            {
                num = 1;
            }

            return num;
        }
        
        public static int StackCountForWater(float wantedWater, float singleDrinkWater) => 
            Mathf.Max(Mathf.RoundToInt(wantedWater / singleDrinkWater), 1);

        private static Thing SpawnedWaterSearchInnerScan(
            Pawn eater,
            IntVec3 root,
            List<Thing> searchSet,
            PathEndMode peMode,
            TraverseParms traverseParams,
            float maxDistance = 9999f,
            Predicate<Thing> validator = null)
        {
            if (searchSet == null)
                return null;
            Pawn pawn = traverseParams.pawn ?? eater;
            Thing thing = null;
            float thingDistance = float.MinValue;
            foreach (Thing checking in searchSet)
            {
                float lengthManhattan = (root - checking.Position).LengthManhattan;
                if (lengthManhattan > (double) maxDistance)
                {
                    continue;
                }

                if (lengthManhattan <= (double) thingDistance &&
                    pawn.Map.reachability.CanReach(root, (LocalTargetInfo) checking, peMode, traverseParams) &&
                    checking.Spawned && (validator == null || validator(checking)))
                {
                    thing = checking;
                    thingDistance = lengthManhattan;
                }
            }

            return thing;
        }

        [CanBeNull]
        private static Thing FirstWaterInClosestPackAnimalInventory(Pawn getter, ref Pawn packAnimal)
        {
            Thing found = null;

            foreach (Pawn spawnedColonyAnimal in getter.Map.mapPawns.SpawnedColonyAnimals)
            {
                Thing bestWater = BestWaterInInventory(spawnedColonyAnimal);
                if (bestWater != null && (packAnimal == null ||
                                          (getter.Position - packAnimal.Position).LengthManhattan >
                                          (getter.Position - spawnedColonyAnimal.Position).LengthManhattan) &&
                    !spawnedColonyAnimal.IsForbidden(getter) &&
                    getter.CanReach((LocalTargetInfo) (Thing) spawnedColonyAnimal, PathEndMode.OnCell, Danger.Some))
                {
                    packAnimal = spawnedColonyAnimal;
                    found = bestWater;
                }
            }

            return found;
        }

        private static Thing BestWaterInInventory(Pawn holder)
        {
            return holder.inventory?.innerContainer.FirstOrFallback(thing =>
                thing.def.HasModExtension<WaterModExtension>());
        }
    }
}