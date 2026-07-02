using RTS.Common.Enums;
using RTS.Common.Structs;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Buildings.Data
{
    public class BuildingDatabase : MonoBehaviour
    {
        [Header("Building Templates")]
        [SerializeField] private List<BuildingInfoSO> buildingTemplates;

        private Dictionary<BuildingType, BuildingInfoSO> buildingDictionary;

        #region Initialization

        private void Awake()
        {
            InitializeDictionary();
        }

        private void InitializeDictionary()
        {
            buildingDictionary = new Dictionary<BuildingType, BuildingInfoSO>();

            foreach (var template in buildingTemplates)
            {
                if (template == null)
                    continue;

                if (buildingDictionary.ContainsKey(template.buildingType))
                {
                    Debug.LogWarning($"Duplicate BuildingType detected: {template.buildingType}");
                    continue;
                }

                buildingDictionary.Add(template.buildingType, template);
            }
        }

        #endregion

        #region Public Methods

        public BuildingInfoSO GetBuildingTemplate(BuildingType buildingType)
        {
            if (buildingDictionary == null)
            {
                Debug.LogError("BuildingDatabase not initialized.");
                return null;
            }

            if (!buildingDictionary.TryGetValue(buildingType, out var template))
            {
                Debug.LogError($"BuildingType not found in database: {buildingType}");
                return null;
            }

            return template;
        }

        public List<ResourceAmount> GetBuildingCost(BuildingType buildingType)
        {
            BuildingInfoSO buildingInfo = GetBuildingTemplate(buildingType);

            if (buildingInfo == null)
            {
                Debug.LogError($"Cannot get cost for unknown BuildingType: {buildingType}");
                return null;
            }

            return buildingInfo.buildingCostList;
        }

        public BuildingCategory GetBuildingCategory(BuildingType buildingType)
        {
            BuildingInfoSO buildingInfo = GetBuildingTemplate(buildingType);

            return buildingInfo.buildingCategory;
        }

        public List<ResourceType> GetBuildingAcceptedResources(BuildingType buildingType)
        {
            BuildingInfoSO buildingInfo = GetBuildingTemplate(buildingType);

            return buildingInfo.acceptedResources;
        }

        #endregion
    }
}