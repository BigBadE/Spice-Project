using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Spice.Jobs
{
    public class Toils_Drink
    {
        public static Toil DrinkWater(Pawn drinker, TargetIndex drinkIndex)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                Pawn actor = toil.actor;
                Thing thing = actor.CurJob.GetTarget(drinkIndex).Thing;
                if (!thing.IngestibleNow)
                {
                    drinker.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
                else
                {
                    toil.actor.pather.StopDead();
                    actor.jobs.curDriver.ticksLeftThisToil =
                        Mathf.RoundToInt(thing.def.ingestible.baseIngestTicks);
                    if (!thing.Spawned)
                        return;
                    thing.Map.physicalInteractionReservationManager.Reserve(drinker, actor.CurJob,
                        (LocalTargetInfo)thing);
                }
            };
            toil.tickAction = () =>
            {
                if (drinker != toil.actor)
                {
                    toil.actor.rotationTracker.FaceCell(drinker.Position);
                }
                else
                {
                    LocalTargetInfo target = toil.actor.CurJob.GetTarget(drinkIndex);
                    Thing thing = target.Thing;
                    if (thing != null && thing.Spawned)
                    {
                        toil.actor.rotationTracker.FaceCell(thing.Position);
                    }
                }

                toil.actor.GainComfortFromCellIfPossible();
            };
            toil.WithProgressBar(drinkIndex, () =>
            {
                Thing thing = toil.actor.CurJob.GetTarget(drinkIndex).Thing;
                return thing == null
                    ? 1f
                    : (float)(1.0 - toil.actor.jobs.curDriver.ticksLeftThisToil /
                        (double)Mathf.Round(thing.def.ingestible.baseIngestTicks));
            });
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.FailOnDestroyedOrNull(drinkIndex);
            toil.AddFinishAction(() =>
            {
                Thing thing = drinker?.CurJob?.GetTarget(drinkIndex).Thing;
                if (thing == null ||
                    !drinker.Map.physicalInteractionReservationManager.IsReservedBy(drinker, (LocalTargetInfo)thing))
                    return;
                drinker.Map.physicalInteractionReservationManager.Release(drinker, toil.actor.CurJob,
                    (LocalTargetInfo)thing);
            });
            toil.handlingFacing = true;
            AddDrinkEffects(toil, drinker, drinkIndex);
            return toil;
        }

        public static void AddDrinkEffects(Toil toil, Pawn drinker, TargetIndex drinkIndex)
        {
            toil.WithEffect(() =>
            {
                LocalTargetInfo target = toil.actor.CurJob.GetTarget(drinkIndex);
                if (!target.HasThing)
                    return null;
                EffecterDef effecterDef = target.Thing.def.ingestible.ingestEffect;
                if (drinker.RaceProps.intelligence < Intelligence.ToolUser &&
                    target.Thing.def.ingestible.ingestEffectEat != null)
                    effecterDef = target.Thing.def.ingestible.ingestEffectEat;
                return effecterDef;
            }, () =>
            {
                if (!toil.actor.CurJob.GetTarget(drinkIndex).HasThing)
                    return (LocalTargetInfo)(Thing)null;
                Thing thing = toil.actor.CurJob.GetTarget(drinkIndex).Thing;
                if (drinker != toil.actor)
                    return (LocalTargetInfo)(Thing)drinker;
                return (LocalTargetInfo)thing;
            });
            toil.PlaySustainerOrSound(() =>
            {
                if (!drinker.RaceProps.Humanlike)
                    return null;
                LocalTargetInfo target = toil.actor.CurJob.GetTarget(drinkIndex);
                return !target.HasThing ? null : target.Thing.def.ingestible.ingestSound;
            });
        }
    }
}