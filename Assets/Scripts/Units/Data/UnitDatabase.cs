using RTS.Common.Enums;
using RTS.Common.Structs;
using RTS.Units.Common;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Units.Data
{
    public class UnitDatabase : MonoBehaviour
    {
        [Header("Unit Templates")]
        [SerializeField] private List<UnitInfoSO> unitTemplates;

        private Dictionary<UnitType, UnitInfoSO> unitDictionary;

        private Dictionary<(UnitRole, UnitRole), float> attackPriorityTable;

        #region Initialization

        private void Awake()
        {
            InitializeDictionary();
            InitializeAttackPriorityTable();
        }

        private void InitializeDictionary()
        {
            unitDictionary = new Dictionary<UnitType, UnitInfoSO>();

            foreach (var template in unitTemplates)
            {
                if (template == null)
                    continue;

                if (unitDictionary.ContainsKey(template.unitType))
                {
                    Debug.LogWarning($"Duplicate UnitType detected: {template.unitType}");
                    continue;
                }

                unitDictionary.Add(template.unitType, template);
            }
        }

        #endregion

        #region Public Methods

        public UnitInfoSO GetUnitTemplate(UnitType unitType)
        {
            if (unitDictionary == null)
            {
                Debug.LogError("UnitDatabase not initialized.");
                return null;
            }

            if (!unitDictionary.TryGetValue(unitType, out var template))
            {
                Debug.LogError($"UnitType not found in database: {unitType}");
                return null;
            }

            return template;
        }

        public GameObject GetUnitPrefab(UnitType unitType)
        {
            UnitInfoSO unitInfo = GetUnitTemplate(unitType);

            if (unitInfo == null)
            {
                Debug.LogError($"UnitInfoSO not found for UnitType: {unitType}");
                return null;
            }

            return unitInfo.unitPrefab;
        }

        public BuildingType GetRequiredBuilding(UnitType unitType)
        {
            UnitInfoSO unitInfo = GetUnitTemplate(unitType);

            if (unitInfo == null)
            {
                Debug.LogError($"UnitInfoSO not found for UnitType: {unitType}");
                return BuildingType.None;
            }

            return unitInfo.requiredProductionBuildings[0];
        }

        public List<ResourceAmount> GetUnitCost(UnitType unitType)
        {
            UnitInfoSO unitInfo = GetUnitTemplate(unitType);

            if (unitInfo == null)
            {
                Debug.LogError($"UnitInfoSO not found for UnitType: {unitType}");
                return null;
            }

            return unitInfo.unitCostList;
        }

        public int GetUnitAttackPower(UnitType unitType)
        {
            UnitInfoSO unitInfo = GetUnitTemplate(unitType);

            if (unitInfo == null)
            {
                Debug.LogError($"UnitInfoSO not found for UnitType: {unitType}");
                return 0;
            }

            return unitInfo.attackPower;
        }

        public float GetUnitTrainingTime(UnitType unitType)
        {
            UnitInfoSO unitInfo = GetUnitTemplate(unitType);

            if (unitInfo == null)
            {
                Debug.LogError($"UnitInfoSO not found for UnitType: {unitType}");
                return 0f;
            }

            return unitInfo.unitTrainingTime;
        }

        #endregion

        #region Target Prioritization

        private void InitializeAttackPriorityTable()
        {
            attackPriorityTable = new Dictionary<(UnitRole, UnitRole), float>()
            {
                // Melee Units
                { (UnitRole.Melee, UnitRole.Worker), 0.4f },
                { (UnitRole.Melee, UnitRole.Melee), 0.8f },
                { (UnitRole.Melee, UnitRole.Ranged), 1.2f },
                { (UnitRole.Melee, UnitRole.Siege), 1.4f },

                // Ranged Units
                { (UnitRole.Ranged, UnitRole.Worker), 0.4f },
                { (UnitRole.Ranged, UnitRole.Melee), 1.0f },
                { (UnitRole.Ranged, UnitRole.Ranged), 1.4f },
                { (UnitRole.Ranged, UnitRole.Siege), 1.6f },

                // Siege Units
                { (UnitRole.Siege, UnitRole.Worker), 0.4f },
                { (UnitRole.Siege, UnitRole.Melee), 1.2f },
                { (UnitRole.Siege, UnitRole.Ranged), 1.6f },
                { (UnitRole.Siege, UnitRole.Siege), 1.6f },
            };
        }

        public float CalculateUnitRoleTargetPriority(BaseUnitController selfUnit, BaseUnitController opponentUnit)
        {
            UnitRole selfUnitRole = selfUnit.GetUnitInfo().unitRole;
            UnitRole opponentUnitRole = opponentUnit.GetUnitInfo().unitRole;

            var key = (selfUnitRole, opponentUnitRole);

            if (attackPriorityTable.TryGetValue(key, out float priority))
            {
                return priority;
            }
            else
            {
                Debug.LogWarning($"No priority value found for {selfUnitRole} vs {opponentUnitRole}. Returning default priority of 0.5.");
                return 1.0f; // Default priority if not found
            }
        }

        #endregion
    }
}