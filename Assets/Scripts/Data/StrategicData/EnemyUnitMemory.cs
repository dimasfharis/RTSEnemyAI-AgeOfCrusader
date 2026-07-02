using RTS.Common.Enums;
using UnityEngine;
using System.Collections.Generic;

namespace RTS.Data.StrategicData
{
    public class EnemyUnitMemory
    {
        public UnitType UnitType;

        public EnemyUnitMemory(UnitType type)
        {
            UnitType = type;
        }
    }
}