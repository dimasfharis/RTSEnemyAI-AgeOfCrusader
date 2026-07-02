using RTS.Core;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RTS.Units.Common
{
    public class UnitView : MonoBehaviour
    {
        private PlayerInfo playerInfo;
        private BaseUnitController baseUnitController;

        private float showHealthBarTime = 0f;
        private float hideHealthBarTime = 5f;

        // Identity
        [SerializeField] private TextMeshProUGUI playerNumberText;

        // Health Bar
        [SerializeField] private GameObject healthBarContainer;
        [SerializeField] private Image healthBar;

        #region Initialization

        public void Init(PlayerInfo owner, BaseUnitController baseUnitController)
        {
            playerInfo = owner;
            this.baseUnitController = baseUnitController;

            SetPlayerNumber(playerInfo.PlayerNumber);
            healthBarContainer.SetActive(false);

            baseUnitController.OnUnitHealthChanged += UpdateHealthBar;
            baseUnitController.OnUnitDead += OnUnitDead;
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            showHealthBarTime += Time.deltaTime;

            if (showHealthBarTime > hideHealthBarTime)
            {
                healthBarContainer?.SetActive(false);
            }
        }

        #endregion

        #region Unit Lifecycle

        private void OnUnitDead(BaseUnitController unit)
        {
            baseUnitController.OnUnitHealthChanged -= UpdateHealthBar;
            baseUnitController.OnUnitDead -= OnUnitDead;
        }

        #endregion

        #region Health Bar

        private void UpdateHealthBar(float currentHitPoint, float maxHitPoint, BaseUnitController unit)
        {
            healthBarContainer.SetActive(true);
            showHealthBarTime = 0f;

            float healthPercent = Mathf.Clamp01(currentHitPoint / maxHitPoint);
            healthBar.fillAmount = healthPercent;
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