using RTS.Common.Enums;
using RTS.Common.Structs;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Units.Data
{
    public class UnitDatabase : MonoBehaviour
    {
        [Header("Unit Templates")]
        [SerializeField] private List<UnitInfoSO> unitTemplates;

        private Dictionary<UnitType, UnitInfoSO> unitDictionary;

        #region Initialization

        private void Awake()
        {
            InitializeDictionary();
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
    }
}