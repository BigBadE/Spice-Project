using System.Collections.Generic;
using Spice.Comps;

namespace Spice.Water
{
    public class WaterNet
    {
        public List<CompWaterStorage> Storages = new List<CompWaterStorage>();
        public List<CompWaterConsumer> Consumers = new List<CompWaterConsumer>();

        public void Tick()
        {
            Dictionary<CompWaterStorage, int> drawing = new Dictionary<CompWaterStorage, int>();
            foreach (CompWaterConsumer consumer in Consumers)
            {
                int draw = consumer.NeededWater();
                if (draw != 0)
                {
                    drawing.Clear();
                    foreach (CompWaterStorage storage in Storages)
                    {
                        if (storage.water > draw)
                        {
                            drawing.Add(storage, draw);
                            break;
                        }

                        if (storage.water > 0)
                        {
                            drawing.Add(storage, storage.water);
                            draw -= storage.water;
                        }
                    }
                }

                if (consumer.Consume(consumer.NeededWater() - draw))
                {
                    foreach (KeyValuePair<CompWaterStorage,int> pair in drawing)
                    {
                        pair.Key.DrawWater(pair.Value);
                    }
                }
            }
        }
    }
}