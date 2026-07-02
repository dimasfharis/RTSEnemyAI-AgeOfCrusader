using System.Collections.Generic;
using UnityEngine;
using RTS.Common.Enums;
using RTS.Common.Structs;

namespace RTS.Units.Data
{
    [CreateAssetMenu(
        fileName = "UnitInfoSO_",
        menuName = "RTS/Units/Unit Info SO"
    )]
    public class UnitInfoSO : ScriptableObject
    {
        [Header("Prefab")]
        public GameObject unitPrefab;

        [Header("Identity")]
        public UnitType unitType;
        public UnitRole unitRole;

        [Header("Production")]
        public List<BuildingType> requiredProductionBuildings;
        public List<ResourceAmount> unitCostList;
        public float unitTrainingTime;

        [Header("Health")]
        public float unitMaxHealth;

        [Header("Combat")]
        public int attackDamage;
        public float attackRange;
        public float attackCooldown;
        public int attackPower;

        [Header("Movement")]
        public float moveSpeed;

        [Header("Line of Sight")]
        public float lineOfSightRange;

        [Header("Resource (Worker-related)")]
        public int carryingCapacity;

        [Header("Worker Build")]
        public float buildSpeed;
    }
}