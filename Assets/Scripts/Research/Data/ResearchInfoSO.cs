using RTS.Common.Structs;
using RTS.Common.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Research.Data
{
    [CreateAssetMenu(
        fileName = "ResearchInfoSO_",
        menuName = "RTS/Research/Research Info SO"
    )]
    public class ResearchInfoSO : ScriptableObject
    {
        [Header("Identity")]
        public ResearchType researchType;

        [Header("Cost")]
        public List<ResourceAmount> researchCostList;

        [Header("Research Time")]
        public float researchTime;

        [Header("Stats")]
        // value in percentage
        public float carryingCapacity;
        public float maxHealth;
        public float attackPoint;
        public float towerDamage;
        public float baseMaxHealth;
        public float unitTrainingSpeed;
    }
}