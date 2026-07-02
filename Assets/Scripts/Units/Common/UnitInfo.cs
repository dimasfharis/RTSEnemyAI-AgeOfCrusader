using RTS.Common.Enums;
using RTS.Common.Structs;
using RTS.Core;
using RTS.Units.Data;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Units.Common
{
    [System.Serializable]
    public class UnitInfo
    {
        [Header("Identity")]
        public int playerNumber;
        public UnitType unitType;
        public UnitRole unitRole;

        [Header("Cost")]
        public List<ResourceAmount> unitCostList;
        public float unitTrainingTime;

        [Header("Health")]
        public float unitHealth;
        public float unitMaxHealth;
        public bool isDead;

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
        public int carryingAmount;
        public int carryingCapacity;

        [Header("Worker Build")]
        public float buildSpeed;

        #region Initialization

        public UnitInfo(UnitInfoSO template, PlayerInfo playerInfo)
        {
            InitFromTemplate(template, playerInfo);
        }

        private void InitFromTemplate(UnitInfoSO so, PlayerInfo playerInfo)
        {
            // Identity
            playerNumber = playerInfo.PlayerNumber;
            unitType = so.unitType;
            unitRole = so.unitRole;

            // Cost
            unitCostList = new List<ResourceAmount>(so.unitCostList);
            unitTrainingTime = so.unitTrainingTime;

            // Health
            unitHealth = so.unitMaxHealth;
            unitMaxHealth = so.unitMaxHealth;

            // Combat
            attackDamage = so.attackDamage;
            attackRange = so.attackRange;
            attackCooldown = so.attackCooldown;
            attackPower = so.attackPower;

            // Movement
            moveSpeed = so.moveSpeed;

            // Line of Sight
            lineOfSightRange = so.lineOfSightRange;

            // Resource (Worker-related)
            carryingCapacity = so.carryingCapacity;

            // Worker Build
            buildSpeed = so.buildSpeed;
        }

        #endregion
    }
}
