using System.Collections.Generic;
using RimWorld;
using Spice.Hediffs;
using UnityEngine;
using Verse;

namespace Spice.Needs
{
    public class Need_Water : Need
    {
        private int lastNonDehydratedTick = -99999;
        public const float BaseFoodFallPerTick = 2.666667E-05f;
        public const float FallPerTickFactor_Hungry = 0.5f;
        public const float FallPerTickFactor_UrgentlyHungry = 0.25f;
        private const float BaseMalnutritionSeverityPerDay = 0.17f;
        private const float BaseMalnutritionSeverityPerInterval = 0.001133333f;

        public bool Starving => CurCategory == HungerCategory.Starving;

        public float PercentageThreshUrgentlyHungry => pawn.RaceProps.FoodLevelPercentageWantEat * 0.4f;

        public float PercentageThreshHungry => pawn.RaceProps.FoodLevelPercentageWantEat * 0.8f;

        public float NutritionBetweenHungryAndFed => (1f - PercentageThreshHungry) * MaxLevel;

        public HungerCategory CurCategory
        {
            get
            {
                if (CurLevelPercentage <= 0.0)
                    return HungerCategory.Starving;
                if (CurLevelPercentage < (double) PercentageThreshUrgentlyHungry)
                    return HungerCategory.UrgentlyHungry;
                return CurLevelPercentage < (double) PercentageThreshHungry
                    ? HungerCategory.Hungry
                    : HungerCategory.Fed;
            }
        }

        public float FoodFallPerTick => WaterPerTickAssuming();

        public int TicksUntilHungryWhenFed => Mathf.CeilToInt(NutritionBetweenHungryAndFed /
                                                              WaterPerTickAssuming());

        public int TicksUntilHungryWhenFedIgnoringMalnutrition => Mathf.CeilToInt(NutritionBetweenHungryAndFed /
            WaterPerTickAssuming(true));

        public override int GUIChangeArrow => -1;

        public override float MaxLevel => pawn.BodySize * pawn.ageTracker.CurLifeStage.foodMaxFactor;

        public float NutritionWanted => MaxLevel - CurLevel;

        private float DehydrationRate =>
            (float) (Need_Food.BaseHungerRateFactor(pawn.ageTracker.CurLifeStage, pawn.def) *
                     (double) pawn.health.hediffSet.HungerRateFactor *
                     (pawn.story == null || pawn.story.traits == null
                         ? 1.0
                         : pawn.story.traits.HungerRateFactor)) *
            pawn.GetStatValue(StatDefOf.HungerRateMultiplier);

        private float DehydrationRateIgnoringMalnutrition =>
            (float) (pawn.ageTracker.CurLifeStage.hungerRateFactor *
                     (double) pawn.RaceProps.baseHungerRate *
                     pawn.health.hediffSet.GetHungerRateFactor(HediffDefOf.Malnutrition) *
                     (pawn.story == null || pawn.story.traits == null
                         ? 1.0
                         : pawn.story.traits.HungerRateFactor)) *
            pawn.GetStatValue(StatDefOf.HungerRateMultiplier);

        public int TicksDehydrated => Mathf.Max(0, Find.TickManager.TicksGame - lastNonDehydratedTick);

        private float DehydrationSeverityPerInterval =>
            0.001133333f * Mathf.Lerp(0.8f, 1.2f, Rand.ValueSeeded(pawn.thingIDNumber ^ 2551674));

        public Need_Water(Pawn pawn)
            : base(pawn)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastNonDehydratedTick, "lastNonDehydratedTick", -99999);
        }

        public float WaterPerTickAssuming(bool ignoreDehydration = false)
        {
            float hungerRate = ignoreDehydration ? DehydrationRateIgnoringMalnutrition : DehydrationRate;
            return hungerRate;
        }

        public override void NeedInterval()
        {
            if (!Starving)
            {
                lastNonDehydratedTick = Find.TickManager.TicksGame;
            }

            if (IsFrozen)
            {
                return;
            }

            CurLevel -= FoodFallPerTick * 150f;

            if (Starving)
                HealthUtility.AdjustSeverity(pawn, SpiceHediffsDefOf.Spice_Dehydration, DehydrationSeverityPerInterval);
            else
                HealthUtility.AdjustSeverity(pawn, SpiceHediffsDefOf.Spice_Dehydration, -DehydrationSeverityPerInterval);
        }

        public override void SetInitialLevel()
        {
            CurLevelPercentage = pawn.RaceProps.Humanlike ? 0.8f : Rand.Range(0.5f, 0.9f);
            
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            lastNonDehydratedTick = Find.TickManager.TicksGame;
        }

        public override string GetTipString() => LabelCap + ": " + CurLevelPercentage.ToStringPercent() +
                                                 " (" + CurLevel.ToString("0.##") + " / " +
                                                 MaxLevel.ToString("0.##") + ")\n" + def.description;

        public override void DrawOnGUI(Rect rect, int maxThresholdMarkers = 2147483647, float customMargin = -1f,
            bool drawArrows = true, bool doTooltip = true, Rect? rectForTooltip = null)
        {
            if (threshPercents == null)
            {
                threshPercents = new List<float>();
            }
            threshPercents.Clear();
            threshPercents.Add(PercentageThreshHungry);
            threshPercents.Add(PercentageThreshUrgentlyHungry);

            base.DrawOnGUI(rect, maxThresholdMarkers, customMargin, drawArrows, doTooltip, rectForTooltip);
        }

        public static float BaseThirstRateFactor(LifeStageDef lifeStage, ThingDef pawnDef) =>
            lifeStage.hungerRateFactor * pawnDef.race.baseHungerRate;
    }
}