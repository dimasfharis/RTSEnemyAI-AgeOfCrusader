using RTS.Common.Enums;
using RTS.Common.Structs;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Research.Data
{
    public class ResearchDatabase : MonoBehaviour
    {
        [Header("Research Templates")]
        [SerializeField] private List<ResearchInfoSO> researchTemplates;

        private Dictionary<ResearchType, ResearchInfoSO> researchDictionary;

        #region Initialization

        private void Awake()
        {
            InitializeDictionary();
        }

        private void InitializeDictionary()
        {
            researchDictionary = new Dictionary<ResearchType, ResearchInfoSO>();

            foreach (var template in researchTemplates)
            {
                if (template == null)
                    continue;

                if (researchDictionary.ContainsKey(template.researchType))
                {
                    Debug.LogWarning($"Duplicate ResearchType detected: {template.researchType}");
                    continue;
                }

                researchDictionary.Add(template.researchType, template);
            }
        }

        #endregion

        #region Get Upgraded Stat

        public float GetUpgradedStat(ResearchType type)
        {
            switch (type)
            {
                case ResearchType.LevelUpCarryingCapacity:
                    return researchDictionary[type].carryingCapacity;

                case ResearchType.LevelUpWorkerHP:
                    return researchDictionary[type].maxHealth;

                case ResearchType.LevelUpAllHPUnits:
                    return researchDictionary[type].maxHealth;

                case ResearchType.LevelUpAllAttackPoint:
                    return researchDictionary[type].attackPoint;

                case ResearchType.LevelUpTowerDamage:
                    return researchDictionary[type].towerDamage;

                case ResearchType.LevelUpBaseArmor:
                    return researchDictionary[type].baseMaxHealth;

                case ResearchType.LevelUpUnitTrainingSpeed:
                    return researchDictionary[type].unitTrainingSpeed;

                default:
                    return 0f;
            }
        }

        #endregion

        #region Public Methods

        public ResearchInfoSO GetResearchTemplate(ResearchType researchType)
        {
            if (researchDictionary == null)
            {
                Debug.LogError("ResearchDatabase not initialized");
                return null;
            }

            if (!researchDictionary.TryGetValue(researchType, out var template))
            {
                Debug.LogError($"ResearchType not found in database: {researchType}");
                return null;
            }

            return template;
        }

        public List<ResourceAmount> GetResourceCost(ResearchType researchType)
        {
            return GetResearchTemplate(researchType).researchCostList;
        }

        public float GetResearchTime(ResearchType researchType)
        {
            return GetResearchTemplate(researchType).researchTime;
        }

        #endregion
    }
}