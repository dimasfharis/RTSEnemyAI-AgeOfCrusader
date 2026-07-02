using RTS.Common.Enums;
using RTS.Core;
using RTS.Buildings.Common.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RTS.Buildings.Common
{
    public class BuildingView : MonoBehaviour
    {
        private PlayerInfo playerInfo;
        private BaseBuildingController baseBuildingController;

        private float showHealthBarTime = 0f;
        private float hideHealthBarTime = 5f;

        // Identity
        [SerializeField] private TextMeshProUGUI playerNumberText;

        // Build Progress
        [SerializeField] private GameObject buildProgressContainer;
        [SerializeField] private Image buildProgressBar;

        // Health Bar
        [SerializeField] private GameObject healthBarContainer;
        [SerializeField] private Image healthBar;

        // Training Progress
        [SerializeField] private GameObject trainingProgressContainer;
        [SerializeField] private Image trainingProgressBar;

        // Research Progress
        [SerializeField] private GameObject researchProgressContainer;
        [SerializeField] private Image researchProgressBar;

        #region Initialization

        public void Init(PlayerInfo owner, BaseBuildingController baseBuildingController)
        {
            playerInfo = owner;
            this.baseBuildingController = baseBuildingController;

            SetPlayerNumber(playerInfo.PlayerNumber);
            buildProgressContainer.SetActive(false);
            healthBarContainer.SetActive(false);

            baseBuildingController.OnBuildingInstantiated += OnBuildingInstantiated;
            baseBuildingController.OnBuildProgressChanged += UpdateBuildProgressBar;
            baseBuildingController.OnBuildingConstructed += OnBuildingConstructed;
            baseBuildingController.OnBuildingHealthChanged += UpdateHealthBar;
            baseBuildingController.OnBuildingDestroyed += OnBuildingDestroyed;

            if (baseBuildingController is IUnitTrainer unitTrainerBuilding)
            {
                trainingProgressContainer.SetActive(false);

                unitTrainerBuilding.OnTrainingProgressChanged += UpdateTrainingProgress;
                unitTrainerBuilding.OnUnitTrained += OnUnitTrained;
            }

            if (baseBuildingController is IStatResearcher statResearcherBuilding)
            {
                researchProgressContainer.SetActive(false);

                statResearcherBuilding.OnResearchProgressChanged += UpdateResearchProgress;
                statResearcherBuilding.OnStatResearched += OnStatResearched;
            }
        }

        #endregion

        #region Unity Lifecycle

        public void Update()
        {
            showHealthBarTime += Time.deltaTime;

            if (showHealthBarTime > hideHealthBarTime)
            {
                healthBarContainer?.SetActive(false);
            }
        }

        #endregion

        #region Building Lifecycle

        private void OnBuildingInstantiated(BaseBuildingController building)
        {
            buildProgressContainer.SetActive(true);
            buildProgressBar.fillAmount = 0f;
        }

        private void OnBuildingConstructed(BaseBuildingController building)
        {
            buildProgressContainer.SetActive(false);
            healthBarContainer.SetActive(true);

            float healthPercent = building.GetBuildingInfo().currentHitPoint / building.GetBuildingInfo().maxHitPoint;
            healthBar.fillAmount = healthPercent;
        }

        private void OnBuildingDestroyed(BaseBuildingController building)
        {
            baseBuildingController.OnBuildingInstantiated -= OnBuildingInstantiated;
            baseBuildingController.OnBuildProgressChanged -= UpdateBuildProgressBar;
            baseBuildingController.OnBuildingConstructed -= OnBuildingConstructed;
            baseBuildingController.OnBuildingHealthChanged -= UpdateHealthBar;
            baseBuildingController.OnBuildingDestroyed -= OnBuildingDestroyed;

            if (baseBuildingController is IUnitTrainer unitTrainerBuilding)
            {
                unitTrainerBuilding.OnTrainingProgressChanged -= UpdateTrainingProgress;
                unitTrainerBuilding.OnUnitTrained -= OnUnitTrained;
            }

            if (baseBuildingController is IStatResearcher statResearcherBuilding)
            {
                statResearcherBuilding.OnResearchProgressChanged -= UpdateResearchProgress;
                statResearcherBuilding.OnStatResearched -= OnStatResearched;
            }
        }

        #endregion

        #region Build Progress

        private void UpdateBuildProgressBar(float progressBuild, float buildTime)
        {
            float progress = Mathf.Clamp01(progressBuild / buildTime);

            buildProgressBar.fillAmount = progress;
        }

        #endregion

        #region Health Bar

        private void UpdateHealthBar(float currentHitPoint, float maxHitPoint)
        {
            healthBarContainer.SetActive(true);
            showHealthBarTime = 0f;

            float healthPercent = Mathf.Clamp01(currentHitPoint / maxHitPoint);
            healthBar.fillAmount = healthPercent;
        }

        #endregion

        #region Unit Training

        private void UpdateTrainingProgress(float progressTraining, float trainingTime)
        {
            trainingProgressContainer.SetActive(true);

            float progress = Mathf.Clamp01(progressTraining / trainingTime);

            trainingProgressBar.fillAmount = progress;
        }

        private void OnUnitTrained(UnitType unitType)
        {
            trainingProgressContainer.SetActive(false);
        }

        #endregion

        #region Stat Research

        private void UpdateResearchProgress(float progressResearch, float researchTime)
        {
            researchProgressContainer.SetActive(true);

            float progress = Mathf.Clamp01(progressResearch / researchTime);

            researchProgressBar.fillAmount = progress;
        }

        private void OnStatResearched(ResearchType researchType)
        {
            researchProgressContainer.SetActive(false);
        }

        #endregion

        #region Public API

        public void SetPlayerNumber(int playerNumber)
        {
            playerNumberText.text = $"{playerNumber}";
        }

        #endregion
    }
}