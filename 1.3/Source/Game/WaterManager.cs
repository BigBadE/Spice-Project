using System.Collections.Generic;
using Spice.Water;
using Verse;

namespace Spice.Game
{
    public class WaterManager : MapComponent
    {
        private List<WaterNet> waterNets = new List<WaterNet>();
        
        public WaterManager(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            foreach (WaterNet waterNet in waterNets)
            {
                waterNet.Tick();
            }
        }
    }
}