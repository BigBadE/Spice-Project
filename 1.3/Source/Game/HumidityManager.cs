using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Spice.Game
{
    public class HumidityManager : MapComponent
    {
        private Dictionary<Room, HumidityData> RoomManager = new Dictionary<Room, HumidityData>();
        private HumidityData OutdoorsHumidity;

        public HumidityManager(Map map) : base(map)
        {
        }

        public void Recalculate(Room room)
        {
            RoomManager[room].CalculateEquilibrium(room);
        }

        public override void FinalizeInit()
        {
            Tile tileInfo = map.TileInfo;
            OutdoorsHumidity = new HumidityData
            {
                Equilibrium = GetTileHumidity(tileInfo)
            };
        }

        public override void MapComponentTick()
        {
            foreach (KeyValuePair<Room, HumidityData> pair in RoomManager)
            {
                pair.Value.Tick(this, pair.Key, OutdoorsHumidity.Equilibrium);
            }
        }

        public HumidityData GetHumidity(Room room)
        {
            return room == null ? OutdoorsHumidity : room.UsesOutdoorTemperature ? OutdoorsHumidity : RoomManager[room];
        }

        public static float GetTileHumidity(Tile tileInfo)
        {
            return Mathf.Clamp01(tileInfo.rainfall / 1.12f * .75f +
                                 (1 - tileInfo.temperature / 50) * .05f +
                                 (int)tileInfo.hilliness / 5f * .2f);
        }
        
        public void Remove(Room room)
        {
            RoomManager.Remove(room);
        }

        public void AddRoom(Room room)
        {
            RoomManager.Add(room, CreateData(room));
        }

        private HumidityData CreateData(Room room)
        {
            HumidityData data = new HumidityData();
            data.CalculateEquilibrium(room);
            return data;
        }
    }

    [HarmonyPatch(typeof(Room), nameof(Room.MakeNew))]
    public class NewRoomPatch
    {
        public void Postfix(Map map, ref Room __result)
        {
            map.GetComponent<HumidityManager>().AddRoom(__result);
        }
    }

    [HarmonyPatch(typeof(Room), nameof(Room.Notify_RoomShapeChanged))]
    public class RoomChangePatch
    {
        public void Postfix(Room __instance)
        {
            if (__instance.Dereferenced)
            {
                __instance.Map.GetComponent<HumidityManager>().Remove(__instance);
            }
            else
            {
                __instance.Map.GetComponent<HumidityManager>().Recalculate(__instance);
            }
        }
    }
}