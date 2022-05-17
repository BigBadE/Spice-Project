using Spice.Defs;
using UnityEngine;
using Verse;

namespace Spice.Comps
{
    public class CompRainCollector : CompWaterStorage
    {
        private const int RainCollectionPerTick = 1;
        private float _rainRate;

        public override void CompTick()
        {
            water += Mathf.RoundToInt(RainCollectionPerTick * _rainRate);
            base.CompTick();
        }
        
        public override void CompTickLong()
        {
            WeatherDef weather = parent.Map.weatherManager.curWeather;
            if (weather.HasModExtension<WaterGeneratingWeatherModExtension>())
            {
                _rainRate = weather.GetModExtension<WaterGeneratingWeatherModExtension>().waterRate;
            } else if (weather.rainRate > 0)
            {
                _rainRate = weather.rainRate;
            }
        }
    }
}