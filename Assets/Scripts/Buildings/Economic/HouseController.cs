using RTS.Buildings.Common;

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