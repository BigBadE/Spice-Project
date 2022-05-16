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
        public const float EatCorpseBodyPartsUntilFoodLevelPct = 0.9f;
        public const TargetIndex IngestibleSourceInd = TargetIndex.A;
        private const TargetIndex TableCellInd = TargetIndex.B;
        private const TargetIndex ExtraIngestiblesToCollectInd = TargetIndex.C;

        public bool EatingFromInventory => eatingFromInventory;

        private Thing IngestibleSource => job.GetTarget(TargetIndex.A).Thing;

        private float ChewDurationMultiplier
        {
            get
            {
                Thing ingestibleSource = IngestibleSource;
                return ingestibleSource.def.ingestible != null && !ingestibleSource.def.ingestible.useEatingSpeedStat
                    ? 1f
                    : 1f / pawn.GetStatValue(StatDefOf.EatingSpeed);
            }
        }

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
            usingNutrientPasteDispenser = IngestibleSource is Building_NutrientPasteDispenser;
            eatingFromInventory =
                pawn.inventory != null && pawn.inventory.Contains(IngestibleSource);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Faction != null && !(IngestibleSource is Building_NutrientPasteDispenser))
            {
                Thing ingestibleSource = IngestibleSource;
                if (!pawn.Reserve((LocalTargetInfo) ingestibleSource, job, 10,
                    FoodUtility.GetMaxAmountToPickup(ingestibleSource, pawn, job.count),
                    errorOnFailed: errorOnFailed))
                    return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            if(!(IngestibleSource is Building)) {
                this.FailOn(() => !IngestibleSource.Destroyed);
            }

            // ISSUE: reference to a compiler-generated method
            Toil chew = Toils_Ingest.ChewIngestible(pawn, ChewDurationMultiplier, TargetIndex.A, TargetIndex.B)
                .FailOn(new Func<Toil, bool>(\u003CMakeNewToils\u003Eb__16_1))
                .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            foreach (Toil ingestToil in PrepareToIngestToils(chew))
                yield return ingestToil;
            yield return chew;
            yield return Toils_Ingest.FinalizeIngest(pawn, TargetIndex.A);
            // ISSUE: reference to a compiler-generated method
            yield return Toils_Jump.JumpIf(chew, \u003CMakeNewToils\u003Eb__16_2);
        }

        private IEnumerable<Toil> PrepareToIngestToils(Toil chewToil)
        {
            if (usingNutrientPasteDispenser)
                return PrepareToIngestToils_Dispenser();
            return pawn.RaceProps.ToolUser
                ? PrepareToIngestToils_ToolUser(chewToil)
                : PrepareToIngestToils_NonToolUser();
        }

        private IEnumerable<Toil> PrepareToIngestToils_Dispenser()
        {
            JobDriver_Drink jobDriverIngest = this;
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Ingest.TakeMealFromDispenser(TargetIndex.A, jobDriverIngest.pawn);
            yield return Toils_Ingest.CarryIngestibleToChewSpot(jobDriverIngest.pawn, TargetIndex.A)
                .FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Ingest.FindAdjacentEatSurface(TargetIndex.B, TargetIndex.A);
        }

        private IEnumerable<Toil> PrepareToIngestToils_ToolUser(Toil chewToil)
        {
            if (eatingFromInventory)
            {
                yield return Toils_Misc.TakeItemFromInventoryToCarrier(jobDriverIngest.pawn, TargetIndex.A);
            }
            else
            {
                yield return ReserveWater();
                Toil gotoToPickup = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                    .FailOnDespawnedNullOrForbidden(TargetIndex.A);
                // ISSUE: reference to a compiler-generated method
                yield return Toils_Jump.JumpIf(gotoToPickup,
                    jobDriverIngest.\u003CPrepareToIngestToils_ToolUser\u003Eb__19_0);
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
                    .FailOnDespawnedNullOrForbidden(TargetIndex.A);
                yield return Toils_Jump.Jump(chewToil);
                yield return gotoToPickup;
                yield return Toils_Ingest.PickupIngestible(TargetIndex.A, jobDriverIngest.pawn);
                gotoToPickup = null;
            }

            if (jobDriverIngest.job.takeExtraIngestibles > 0)
            {
                foreach (Toil extraIngestible in jobDriverIngest.TakeExtraIngestibles())
                    yield return extraIngestible;
            }

            if (!jobDriverIngest.pawn.Drafted)
                yield return Toils_Ingest.CarryIngestibleToChewSpot(jobDriverIngest.pawn, TargetIndex.A)
                    .FailOnDestroyedOrNull(TargetIndex.A);
            yield return Toils_Ingest.FindAdjacentEatSurface(TargetIndex.B, TargetIndex.A);
        }

        private IEnumerable<Toil> PrepareToIngestToils_NonToolUser()
        {
            yield return ReserveDrink();
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        }

        private IEnumerable<Toil> TakeExtraIngestibles()
        {
            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                Toil reserveExtraFoodToCollect = Toils_Ingest.ReserveFoodFromStackForIngesting(TargetIndex.C);
                Toil findExtraFoodToCollect = new Toil();
                findExtraFoodToCollect.initAction = () =>
                {
                    if (pawn.inventory.innerContainer.TotalStackCountOfDef(IngestibleSource.def) >=
                        job.takeExtraIngestibles)
                        return;
                    Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                        ThingRequest.ForDef(IngestibleSource.def), PathEndMode.Touch, TraverseParms.For(pawn),
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
                        pawn.inventory.innerContainer.TotalStackCountOfDef(IngestibleSource.def));
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