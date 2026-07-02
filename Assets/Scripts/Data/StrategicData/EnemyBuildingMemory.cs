using RTS.Common.Enums;
using System.Collections.Generic;
using UnityEngine;


namespace RTS.Data.StrategicData
{
    public class EnemyBuildingMemory
    {
        public BuildingType Type;

        public EnemyBuildingMemory(BuildingType type)
        {
            Type = type;
        }
    }
}