using System.Collections.Generic;
using RimWorld;
using Spice.Defs;
using Spice.Game;
using UnityEngine;
using Verse;

namespace Spice.Needs
{
    public class Need_Water : Need
    {
        private int lastNonDehydratedTick = -99999;

        public bool Dehydrated => CurLevelPercentage <= 0;

        public float PercentageThreshUrgentlyThirsty => pawn.RaceProps.FoodLevelPercentageWantEat * 0.4f;

        public float PercentageThreshThirsty => pawn.RaceProps.FoodLevelPercentageWantEat * 0.8f;

        public override int GUIChangeArrow => -1;

        public override float MaxLevel => pawn.BodySize * pawn.ageTracker.CurLifeStage.foodMaxFactor;

        public float WaterWanted => MaxLevel - CurLevel;

        //TODO switch to thirst rate somehow
        private float DehydrationRate =>
            (float) (BaseThirstRateFactor(pawn.ageTracker.CurLifeStage, pawn.def) *
                     (double) pawn.health.hediffSet.HungerRateFactor *
                     (pawn.story?.traits?.HungerRateFactor ?? 1.0)) *
            pawn.GetStatValue(SpiceStatsDefOf.Spice_ThirstRateMultiplier) * 
            (1-GetHumidity())*.75f;

        public int TicksDehydrated => Mathf.Max(0, Find.TickManager.TicksGame - lastNonDehydratedTick);

        private float DehydrationSeverityPerInterval =>
            0.001133333f * Mathf.Lerp(0.8f, 1.2f, Rand.ValueSeeded(pawn.thingIDNumber ^ 2551674));

        public Need_Water(Pawn pawn)
            : base(pawn)
        {
            curLevelInt = .8f;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastNonDehydratedTick, "lastNonDehydratedTick", -99999);
        }

        private float GetHumidity()
        {
            if (pawn.Map != null)
            {
                return pawn.Map.GetComponent<HumidityManager>().GetHumidity(pawn.GetRoom()).CurrentHumidity;
            }

            if (pawn.Tile != -1)
            {
                return HumidityManager.GetTileHumidity(Current.Game.World.grid[pawn.Tile]);
            }

            return 1;
        }
        
        public override void NeedInterval()
        {
            if (!Dehydrated)
            {
                lastNonDehydratedTick = Find.TickManager.TicksGame;
            }

            if (IsFrozen)
            {
                return;
            }

            float lost = DehydrationRate * 150f;
            CurLevel -= lost;

            if (Dehydrated)
            {
                HealthUtility.AdjustSeverity(pawn, SpiceHediffsDefOf.Spice_Dehydration, DehydrationSeverityPerInterval);
            }
            else
            {
                HealthUtility.AdjustSeverity(pawn, SpiceHediffsDefOf.Spice_Dehydration,
                    -DehydrationSeverityPerInterval);
            }

            if (pawn.Map != null)
            {
                pawn.Map.GetComponent<HumidityManager>().GetHumidity(pawn.GetRoom()).CurrentHumidity +=
                    lost / pawn.GetRoom().CellCount;
            }
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
            threshPercents.Add(PercentageThreshThirsty);
            threshPercents.Add(PercentageThreshUrgentlyThirsty);

            base.DrawOnGUI(rect, maxThresholdMarkers, customMargin, drawArrows, doTooltip, rectForTooltip);
        }

        public static float BaseThirstRateFactor(LifeStageDef lifeStage, ThingDef pawnDef) =>
            lifeStage.hungerRateFactor * pawnDef.race.baseHungerRate;
    }
}