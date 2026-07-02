using UnityEngine;
using RTS.Core;
using RTS.Common.Enums;
using System.Collections.Generic;
using System;

namespace RTS.AI.Decision
{
    public class DecisionMakingAIManager
    {
        public static event Action<Dictionary<AIStrategyMode, float>, PlayerInfo> OnStrategyModeUpdated;

        [Header("References")]
        private AIProfileSO aiProfile;
        private PlayerInfo playerInfo;
        private AIStrategyMode currentStrategyMode;

        [Header("Properties")]
        private float evaluationTime = 8f;
        private float currentTime = 0f;

        [Header("Debug & Monitoring Purpose")]
        private Dictionary<AIStrategyMode, float> strategyModesCalculated = new Dictionary<AIStrategyMode, float>();

        #region Initialization

        public DecisionMakingAIManager(PlayerInfo owner, AIProfileSO aiProfileSO)
        {
            playerInfo = owner;
            aiProfile = aiProfileSO;
        }

        #endregion

        #region Update Loop

        public void Tick()
        {
            currentTime += Time.deltaTime;

            if (currentTime >= evaluationTime)
            {
                currentTime = 0f;

                EvaluateStrategicFocus();
            }
        }

        #endregion

        #region Focus Evaluation

        private void EvaluateStrategicFocus()
        {
            float economyScore = CalculateEconomyScore();
            float attackScore = CalculateAttackScore();
            float defendScore = CalculateDefendScore();
            float recoveryScore = CalculateRecoveryScore();

            currentStrategyMode = GetHighestScoreFocus(
                economyScore,
                attackScore,
                defendScore,
                recoveryScore);

            // Debug & Monitoring Purpose Only
            UpdateStrategyModesCalculated(economyScore, attackScore, defendScore, recoveryScore);
            OnStrategyModeUpdated.Invoke(strategyModesCalculated, playerInfo);
        }

        #endregion

        #region Score Calculation

        private float CalculateEconomyScore()
        {
            float lowResource = 1f - GetNormalizedResource();
            float lowIncome = 1f - GetNormalizedIncome();
            float lowThreat = 1f - GetNormalizedEnemyThreat();

            return
                (lowResource * aiProfile.OurResourceMultiplier)
                + (lowIncome * aiProfile.IncomeMultiplier)
                + (lowThreat * aiProfile.EnemyThreatMultiplier);
        }

        private float CalculateAttackScore()
        {
            return
                (GetNormalizedOurPower() * aiProfile.OurPowerMultiplier)
                - (GetNormalizedEnemyPower() * aiProfile.EnemyPowerMultiplier)
                + (GetNormalizedResource() * (1f / aiProfile.OurResourceMultiplier))
                - (GetNormalizedBaseDamage() * aiProfile.BaseDamageMultiplier);
        }

        private float CalculateDefendScore()
        {
            return
                (GetNormalizedEnemyThreat() * aiProfile.EnemyThreatMultiplier)
                + (GetNormalizedBaseDamage() * aiProfile.BaseDamageMultiplier)
                + (GetNormalizedEnemyPower() * aiProfile.EnemyPowerMultiplier);
        }

        private float CalculateRecoveryScore()
        {
            float lowPower = 1f - GetNormalizedOurPower();
            float lowResource = 1f - GetNormalizedResource();

            return
                (GetNormalizedBaseDamage() * aiProfile.BaseDamageMultiplier)
                + (lowPower * (1f / aiProfile.OurPowerMultiplier))
                + (lowResource * aiProfile.OurResourceMultiplier);
        }

        #endregion

        #region Normalized Data

        private float GetNormalizedOurPower()
        {
            return Mathf.Clamp01(
                playerInfo.DataManager.GetOurMilitaryPower() / 1000f);
        }

        private float GetNormalizedEnemyPower()
        {
            return Mathf.Clamp01(
                playerInfo.DataManager.GetEstimatedEnemyMilitaryPower() / 1000f);
        }

        private float GetNormalizedResource()
        {
            return Mathf.Clamp01(
                playerInfo.DataManager.GetTotalResourceStockpile() / 2000f);
        }

        private float GetNormalizedIncome()
        {
            return Mathf.Clamp01(
                playerInfo.DataManager.GetResourceIncomeRate() / 200f);
        }

        private float GetNormalizedEnemyThreat()
        {
            return Mathf.Clamp01(
                playerInfo.DataManager.GetEnemyThreatLevel() / 20f);
        }

        private float GetNormalizedBaseDamage()
        {
            return Mathf.Clamp01(
                playerInfo.DataManager.GetBaseDamageLevel());
        }

        #endregion

        #region Strategy Mode Selection

        private AIStrategyMode GetHighestScoreFocus(
            float economy,
            float attack,
            float defend,
            float recovery)
        {
            float max = economy;
            AIStrategyMode selected = AIStrategyMode.Economic;

            if (attack > max)
            {
                max = attack;
                selected = AIStrategyMode.Attack;
            }

            if (defend > max)
            {
                max = defend;
                selected = AIStrategyMode.Defend;
            }

            if (recovery > max)
            {
                selected = AIStrategyMode.Recovery;
            }

            return selected;
        }

        #endregion

        #region Public API

        public AIStrategyMode GetCurrentAIStrategyMode()
        {
            return currentStrategyMode;
        }

        #endregion

        #region Debug & Monitoring Purpose Only

        private void UpdateStrategyModesCalculated(float economyScore, float attackScore, float defendScore, float recoveryScore)
        {
            strategyModesCalculated.Clear();

            strategyModesCalculated.Add(AIStrategyMode.Economic, economyScore);
            strategyModesCalculated.Add(AIStrategyMode.Attack, attackScore);
            strategyModesCalculated.Add(AIStrategyMode.Defend, defendScore);
            strategyModesCalculated.Add(AIStrategyMode.Recovery, recoveryScore);
        }

        #endregion
    }
}