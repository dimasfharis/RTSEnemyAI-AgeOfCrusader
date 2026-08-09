using RTS.Buildings.Common;
using RTS.Managers;
using RTS.Managers.Log;
using UnityEngine;

namespace RTS.Buildings.Economic
{
    public class HouseController : BaseBuildingController
    {
        #region Population

        public override void OnBuildingActivated()
        {
            if (playerInfo != null)
            {
                playerInfo.ResourceManager.AddPopulationCapacity(buildingInfo.populationProvided);
            }

            base.OnBuildingActivated();

            // Log to building feature
            PlayerLogManager logManager = playerInfo.PlayerLogManager;
            ResourceManager resourceManager = playerInfo.ResourceManager;
            logManager.LogBuildingFeature($"({Time.time}) population capacity is increase by {buildingInfo.populationProvided}. current capacity is {resourceManager.GetPopulationCapacity()}");
        }

        public override void OnBuildingDestroyedAction()
        {
            if (playerInfo != null)
            {
                playerInfo.ResourceManager.RemovePopulationCapacity(buildingInfo.populationProvided);
            }

            base.OnBuildingDestroyedAction();
        }

        #endregion
    }
}