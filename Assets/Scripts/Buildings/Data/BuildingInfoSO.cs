using System.Collections.Generic;
using UnityEngine;
using RTS.Common.Enums;
using RTS.Common.Structs;

namespace RTS.Buildings.Data
{
    [CreateAssetMenu(
        fileName = "BuildingInfoSO_",
        menuName = "RTS/Buildings/Building Info SO"
    )]
    public class  BuildingInfoSO : ScriptableObject
    {
        [Header("Prefab")]
        public GameObject buildingPrefab;

        [Header("Identity")]
        public BuildingType buildingType;
        public BuildingCategory buildingCategory;

        [Header("Dimension")]
        public int length;
        public int width;

        [Header("Cost")]
        public List<ResourceAmount> buildingCostList;

        [Header("Construction")]
        public float buildTime;

        [Header("Health")]
        public int maxHitPoint;

        [Header("Line of Sight")]
        public float lineOfSightRange;

        [Header("Defense")]
        public bool canAttack;
        public int attackDamage;
        public float attackAreaOfEffectRadius;
        public float attackRange;
        public float attackCooldown;
        public int defensePower;

        [Header("Economy")]
        public bool isResourceDeposit;
        public List<ResourceType> acceptedResources;

        [Header("Population")]
        public int populationProvided;

        [Header("Production")]
        public bool canTrainUnit;
        public List<UnitType> trainableUnits;
    }
}