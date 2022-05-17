using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Spice.Jobs
{
    public class JobDriver_Drink : JobDriver
    {
        private bool usingNutrientPasteDispenser;
        private bool eatingFromInventory;

        private Thing WaterSource => job.GetTarget(TargetIndex.A).Thing;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref usingNutrientPasteDispenser, "usingNutrientPasteDispenser");
            Scribe_Values.Look(ref eatingFromInventory, "eatingFromInventory");
        }

        public override string GetReport()
        {
            if (usingNutrientPasteDispenser)
                return JobUtility.GetResolvedJobReportRaw(job.def.reportString, ThingDefOf.MealNutrientPaste.label,
                    ThingDefOf.MealNutrientPaste, "", "", "", "");
            Thing thing = job.targetA.Thing;
            if (thing != null && thing.def.ingestible != null)
            {
                if (!thing.def.ingestible.ingestReportStringEat.NullOrEmpty() &&
                    (thing.def.ingestible.ingestReportString.NullOrEmpty() ||
                     pawn.RaceProps.intelligence < Intelligence.ToolUser))
                    return thing.def.ingestible.ingestReportStringEat.Formatted(
                        (NamedArgument) job.targetA.Thing.LabelShort, (NamedArgument) job.targetA.Thing);
                if (!thing.def.ingestible.ingestReportString.NullOrEmpty())
                    return thing.def.ingestible.ingestReportString.Formatted(
                        (NamedArgument) job.targetA.Thing.LabelShort, (NamedArgument) job.targetA.Thing);
            }

            return base.GetReport();
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            usingNutrientPasteDispenser = WaterSource is Building_NutrientPasteDispenser;
            eatingFromInventory =
                pawn.inventory != null && pawn.inventory.Contains(WaterSource);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Faction != null && !(WaterSource is Building_NutrientPasteDispenser))
            {
                Thing waterSource = WaterSource;
                if (!pawn.Reserve((LocalTargetInfo) waterSource, job, 10,
                    FoodUtility.GetMaxAmountToPickup(waterSource, pawn, job.count),
                    errorOnFailed: errorOnFailed))
                    return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            if(!(WaterSource is Building)) {
                this.FailOn(() => !WaterSource.Destroyed);
            }

            Toil drink = Toils_Drink.DrinkWater(pawn, TargetIndex.A)
                .FailOn(toil => !WaterSource.Spawned && pawn.carryTracker?.CarriedThing != WaterSource)
                .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            foreach (Toil ingestToil in PrepareToDrinkToils(drink))
            {
                yield return ingestToil;
            }

            yield return drink;
            yield return Toils_Ingest.FinalizeIngest(pawn, TargetIndex.A);
        }

        private IEnumerable<Toil> PrepareToDrinkToils(Toil chewToil)
        {
            return pawn.RaceProps.ToolUser
                ? PrepareToIngestToils_ToolUser(chewToil)
                : PrepareToIngestToils_NonToolUser();
        }

        private IEnumerable<Toil> PrepareToIngestToils_ToolUser(Toil chewToil)
        {
            if (eatingFromInventory)
            {
                yield return Toils_Misc.TakeItemFromInventoryToCarrier(pawn, TargetIndex.A);
            }
            else
            {
                yield return ReserveDrink();
                Toil gotoToPickup = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                    .FailOnDespawnedNullOrForbidden(TargetIndex.A);
                
                yield return Toils_Jump.JumpIf(gotoToPickup,
                    () => pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
                    .FailOnDespawnedNullOrForbidden(TargetIndex.A);
                yield return Toils_Jump.Jump(chewToil);
                yield return gotoToPickup;
                yield return Toils_Ingest.PickupIngestible(TargetIndex.A, pawn);
                gotoToPickup = null;
            }

            if (job.takeExtraIngestibles > 0)
            {
                foreach (Toil extraIngestible in TakeExtraDrinks())
                    yield return extraIngestible;
            }
        }

        private IEnumerable<Toil> PrepareToIngestToils_NonToolUser()
        {
            yield return ReserveDrink();
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        }

        private IEnumerable<Toil> TakeExtraDrinks()
        {
            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                Toil reserveExtraFoodToCollect = Toils_Ingest.ReserveFoodFromStackForIngesting(TargetIndex.C);
                Toil findExtraFoodToCollect = new Toil();
                findExtraFoodToCollect.initAction = () =>
                {
                    if (pawn.inventory.innerContainer.TotalStackCountOfDef(WaterSource.def) >=
                        job.takeExtraIngestibles)
                        return;
                    Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                        ThingRequest.ForDef(WaterSource.def), PathEndMode.Touch, TraverseParms.For(pawn),
                        30f,
                        x =>
                            pawn.CanReserve((LocalTargetInfo) x, 10, 1) && !x.IsForbidden(pawn) &&
                            x.IsSociallyProper(pawn));
                    if (thing == null)
                        return;
                    job.SetTarget(TargetIndex.C, (LocalTargetInfo) thing);
                    JumpToToil(reserveExtraFoodToCollect);
                };
                findExtraFoodToCollect.defaultCompleteMode = ToilCompleteMode.Instant;
                yield return Toils_Jump.Jump(findExtraFoodToCollect);
                yield return reserveExtraFoodToCollect;
                yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.Touch);
                yield return Toils_Haul.TakeToInventory(TargetIndex.C,
                    () =>
                        job.takeExtraIngestibles -
                        pawn.inventory.innerContainer.TotalStackCountOfDef(WaterSource.def));
                yield return findExtraFoodToCollect;
            }
        }

        private Toil ReserveDrink() => new Toil
        {
            initAction = () =>
            {
                if (pawn.Faction == null)
                    return;
                Thing thing = job.GetTarget(TargetIndex.A).Thing;
                if (pawn.carryTracker.CarriedThing == thing)
                    return;
                int maxAmountToPickup = FoodUtility.GetMaxAmountToPickup(thing, pawn, job.count);
                if (maxAmountToPickup == 0)
                    return;
                if (!pawn.Reserve((LocalTargetInfo) thing, job, 10, maxAmountToPickup))
                {
                    Log.Error("Pawn drink reservation for " + pawn + " on job " + this +
                              " failed, because it could not register drink from " + thing + " - amount: " +
                              maxAmountToPickup);
                    pawn.jobs.EndCurrentJob(JobCondition.Errored);
                }

                job.count = maxAmountToPickup;
            },
            defaultCompleteMode = ToilCompleteMode.Instant,
            atomicWithPrevious = true
        };

        public override bool ModifyCarriedThingDrawPos(
            ref Vector3 drawPos,
            ref bool behind,
            ref bool flip)
        {
            IntVec3 cell = job.GetTarget(TargetIndex.B).Cell;
            return ModifyCarriedThingDrawPosWorker(ref drawPos, ref behind, ref flip, cell, pawn);
        }

        public static bool ModifyCarriedThingDrawPosWorker(
            ref Vector3 drawPos,
            ref bool behind,
            ref bool flip,
            IntVec3 placeCell,
            Pawn pawn)
        {
            if (pawn.pather.Moving)
                return false;
            Thing carriedThing = pawn.carryTracker.CarriedThing;
            if (carriedThing == null || !carriedThing.IngestibleNow)
                return false;
            if (placeCell.IsValid && placeCell.AdjacentToCardinal(pawn.Position) && placeCell.HasEatSurface(pawn.Map) &&
                carriedThing.def.ingestible.ingestHoldUsesTable)
            {
                drawPos = new Vector3(placeCell.x + 0.5f, drawPos.y, placeCell.z + 0.5f);
                return true;
            }

            if (carriedThing.def.ingestible.ingestHoldOffsetStanding != null)
            {
                HoldOffset holdOffset = carriedThing.def.ingestible.ingestHoldOffsetStanding.Pick(pawn.Rotation);
                if (holdOffset != null)
                {
                    drawPos += holdOffset.offset;
                    behind = holdOffset.behind;
                    flip = holdOffset.flip;
                    return true;
                }
            }

            return false;
        }
    }
}