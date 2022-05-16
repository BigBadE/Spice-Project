using System.Collections.Generic;
using Spice.Comps;
using Verse;

namespace Spice.Game
{
    public class HumidityData
    {
        private const float DoorLeakagePerTick = .001f;
            
        public float CurrentHumidity;
        public float Equilibrium;
        
        public Dictionary<Room, float> Leakage = new Dictionary<Room, float>();
        public float OutsideLeakage;
        
        public void CalculateEquilibrium(Room room)
        {
            Leakage.Clear();
            foreach (Region region in room.Regions)
            {
                foreach (RegionLink link in region.links)
                {
                    if (link.RegionA.Room == room && link.RegionB.Room == room ||
                        (!link.RegionA.IsDoorway || link.RegionA.door.GetComp<CompHumiditySealed>() != null) && 
                        (!link.RegionB.IsDoorway || link.RegionB.door.GetComp<CompHumiditySealed>() != null))
                    {
                        continue;
                    }

                    Room other = link.RegionA.Room == room ? link.RegionB.Room : link.RegionA.Room;

                    if (other == null)
                    {
                        OutsideLeakage += DoorLeakagePerTick;
                    } else if (Leakage.ContainsKey(other))
                    {
                        Leakage[other] += DoorLeakagePerTick;
                    }
                    else
                    {
                        Leakage.Add(other, DoorLeakagePerTick);
                    }
                }
            }

            if (room.OpenRoofCount > 0)
            {
                OutsideLeakage += room.OpenRoofCount / 10f;
                OutsideLeakage = OutsideLeakage > 1 ? 1 : OutsideLeakage;
            }
        }

        public void Tick(HumidityManager humidityManager, Room room, float outsideHumidity)
        {
            foreach (KeyValuePair<Room, float> pair in Leakage)
            {
                if (room == null)
                {
                    CurrentHumidity += (outsideHumidity - CurrentHumidity) * pair.Value;
                    continue;
                }

                HumidityData otherRoom = humidityManager.GetHumidity(pair.Key);

                //Only push humidity to other rooms, don't pull
                if (otherRoom.CurrentHumidity < CurrentHumidity)
                {
                    float change = (CurrentHumidity - otherRoom.CurrentHumidity) * pair.Value;
                    CurrentHumidity -= change;
                    otherRoom.CurrentHumidity += change;
                }
            }
        }
    }
}