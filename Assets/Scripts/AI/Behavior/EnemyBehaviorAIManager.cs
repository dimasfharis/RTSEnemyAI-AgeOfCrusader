using UnityEngine;
using System.Collections.Generic;
using RTS.Core;
using RTS.Common.Enums;
using RTS.AI.Decision;
using RTS.Data;
using System.Linq;
using RTS.AI.GoalManagement;

namespace RTS.AI.Behavior
{
    public class EnemyBehaviorAIManager
    {
        private PlayerInfo playerInfo;
        private DecisionMakingAIManager decisionManager;
        private AIProfileSO aiProfile;
        private DataManager dataManager;

        private List<AIGoal> currentGoals = new List<AIGoal>();

        private GoalCoordinator goalCoordinator;

        #region Initialization

        public EnemyBehaviorAIManager(PlayerInfo owner, AIManager aiManager)
        {
            playerInfo = owner;
            decisionManager = aiManager.GetDecisionMakingAIManager();
            aiProfile = aiManager.GetAIProfile();
            dataManager = owner.DataManager;

            goalCoordinator = new GoalCoordinator(playerInfo, this);
        }

        #endregion

        #region Update Lifecycle

        public void Tick()
        {
            goalCoordinator.Tick();
        }

        #endregion

        #region Goal Evaluation

        public List<AIGoal> EvaluateGoals()
        {
            currentGoals.Clear();

            EvaluateTrainGoals();
            EvaluateBuildGoals();
            EvaluateResearchGoals();
            EvaluateMilitaryGoals();

            SortGoals();

            return currentGoals;
        }

        #endregion

        #region Train Goals Evaluation

        private void EvaluateTrainGoals()
        {
            float workerNeed = CalculateWorkerNeed();
            float militaryNeed = CalculateMilitaryNeed();

            float workerScore =
                workerNeed *
                aiProfile.WorkerMultiplier *
                GetStrategyMultiplier(AIGoalType.TrainUnit, 0);

            float militaryScore =
                militaryNeed *
                aiProfile.MilitaryMultiplier *
                GetStrategyMultiplier(AIGoalType.TrainUnit, 1);

            currentGoals.Add(
                new AIGoal(AIGoalType.TrainUnit, workerScore)
                .SetUnit(UnitType.Worker));

            if (goalCoordinator.unitRequests.Count > 0)
                currentGoals.AddRange(GetMilitaryGoalMultiplied(militaryScore));
        }

        private List<AIGoal> GetMilitaryGoalMultiplied(float baseScore)
        {
            if (goalCoordinator.unitRequests.Count <= 0)
            {
                return null;
            }

            List<AIGoal> militaryGoals = new List<AIGoal>();

            foreach (var request in goalCoordinator.unitRequests)
            {
                float score = baseScore * request.Key.Score;
                AIGoal goal = new AIGoal(AIGoalType.TrainUnit, score);

                goal.SetUnitTrainingRequirements(request.Value.unitRequests);

                militaryGoals.Add(goal);
            }

            return militaryGoals;
        }

        #endregion

        #region Build Goals Evaluation

        private void EvaluateBuildGoals()
        {
            float economyBuildingNeed = CalculateEconomyBuildingNeed();
            float militaryBuildingNeed = CalculateMilitaryBuildingNeed();
            float defensiveBuildingNeed = CalculateDefensiveBuildingNeed();
            float defenseNeed = CalculateDefenseNeed();

            BuildingType ecoBuildingTypeNeeded = GetCurrentEcoBuildingTypeNeeded();
            BuildingType milBuildingTypeNeeded = GetCurrentMilBuildingTypeNeeded();
            BuildingType defBuildingTypeNeeded = GetCurrentDefBuildingTypeNeeded();

            float ecoScore =
                economyBuildingNeed *
                aiProfile.EconomyBuildingMultiplier *
                GetStrategyMultiplier(AIGoalType.BuildStructure, 0);

            float militaryScore =
                militaryBuildingNeed *
                aiProfile.MilitaryBuildingMultiplier *
                GetStrategyMultiplier(AIGoalType.BuildStructure, 1);

            float buildDefenseScore =
                defensiveBuildingNeed *
                aiProfile.DefenseMultiplier *
                GetStrategyMultiplier(AIGoalType.BuildStructure, 2);

            float defenseScore =
                defenseNeed *
                aiProfile.DefenseMultiplier *
                GetStrategyMultiplier(AIGoalType.ReinforceDefense);

            currentGoals.Add(
                new AIGoal(AIGoalType.BuildStructure, ecoScore)
                .SetBuilding(ecoBuildingTypeNeeded));

            currentGoals.Add(
                new AIGoal(AIGoalType.BuildStructure, militaryScore)
                .SetBuilding(milBuildingTypeNeeded));

            currentGoals.Add(
                new AIGoal(AIGoalType.BuildStructure, buildDefenseScore)
                .SetBuilding(defBuildingTypeNeeded));

            currentGoals.Add(
                new AIGoal(AIGoalType.ReinforceDefense, defenseScore));
        }

        #endregion

        #region Research Goals Evaluation

        private void EvaluateResearchGoals()
        {
            float researchNeed = CalculateResearchNeed();

            ResearchType researchTypeNeeded = GetCurrentResearchTypeNeeded();

            float researchScore =
                researchNeed *
                aiProfile.ResearchMultiplier *
                GetStrategyMultiplier(AIGoalType.ResearchUpgrade);

            currentGoals.Add(
                new AIGoal(AIGoalType.ResearchUpgrade, researchScore)
                .SetResearch(researchTypeNeeded));
        }

        #endregion

        #region Military Goals Evaluation

        private void EvaluateMilitaryGoals()
        {
            float attackOpportunity = CalculateAttackOpportunity();
            float patrolNeed = CalculatePatrolNeed();
            float harassmentNeed = CalculateHarassmentNeed();

            float attackScore =
                attackOpportunity *
                aiProfile.AttackMultiplier *
                GetStrategyMultiplier(AIGoalType.LaunchAttackWave);

            float patrolScore =
                patrolNeed *
                aiProfile.PatrolMultiplier *
                GetStrategyMultiplier(AIGoalType.AssignScout);

            float harassmentScore =
                harassmentNeed *
                aiProfile.HarassMultiplier *
                GetStrategyMultiplier(AIGoalType.AssignHarassment);

            currentGoals.Add(
                new AIGoal(AIGoalType.LaunchAttackWave, attackScore));

            currentGoals.Add(
                new AIGoal(AIGoalType.AssignScout, patrolScore));

            currentGoals.Add(
                new AIGoal(AIGoalType.AssignHarassment, harassmentScore));
        }

        #endregion

        #region Strategy Multiplier

        // using variant as argument; 0 for worker/economy, 1 for military

        private float GetStrategyMultiplier(AIGoalType goalType, int variant = -1)
        {
            AIStrategyMode mode = decisionManager.GetCurrentAIStrategyMode();

            switch (mode)
            {
                case AIStrategyMode.Economic:
                    return StrategyEconomy(goalType, variant);

                case AIStrategyMode.Attack:
                    return StrategyAttack(goalType, variant);

                case AIStrategyMode.Defend:
                    return StrategyDefend(goalType, variant);

                case AIStrategyMode.Recovery:
                    return StrategyRecovery(goalType, variant);

                default:
                    return 1f;
            }
        }

        private float StrategyEconomy(AIGoalType type, int variant)
        {
            if (type == AIGoalType.TrainUnit && variant == 0) return 1.6f; //worker
            if (type == AIGoalType.TrainUnit && variant == 1) return 0.6f; //military
            if (type == AIGoalType.BuildStructure && variant == 0) return 1.5f; //economy building
            if (type == AIGoalType.BuildStructure && variant == 1) return 0.7f; //military building
            if (type == AIGoalType.LaunchAttackWave) return 0.4f;
            return 1f;
        }

        private float StrategyAttack(AIGoalType type, int variant)
        {
            if (type == AIGoalType.TrainUnit && variant == 1) return 1.7f;
            if (type == AIGoalType.LaunchAttackWave) return 1.8f;
            if (type == AIGoalType.AssignHarassment) return 1.4f;
            return 1f;
        }

        private float StrategyDefend(AIGoalType type, int variant)
        {
            if (type == AIGoalType.ReinforceDefense) return 1.8f;
            if (type == AIGoalType.AssignScout) return 1.5f;
            if (type == AIGoalType.BuildStructure && variant == 2) return 1.8f; //defensive building
            if (type == AIGoalType.BuildStructure && variant == 1) return 0.9f; //military building
            if (type == AIGoalType.BuildStructure && variant == 0) return 0.6f; //economy building
            return 1f;
        }

        private float StrategyRecovery(AIGoalType type, int variant)
        {
            if (type == AIGoalType.TrainUnit && variant == 0) return 1.7f;
            if (type == AIGoalType.BuildStructure) return 1.6f;
            if (type == AIGoalType.LaunchAttackWave) return 0.3f;
            return 1f;
        }

        #endregion

        #region Calculate Need

        private float CalculateWorkerNeed()
        {
            int currentWorkers = dataManager.GetWorkerCount();
            int idealWorkers = dataManager.GetIdealWorkerCount();

            if (idealWorkers == 0) return 0f;

            float ratio = (float)currentWorkers / idealWorkers;

            return Mathf.Clamp01(1f - ratio);
        }

        private float CalculateMilitaryNeed()
        {
            float ourPower = dataManager.GetOurMilitaryPower();
            float enemyPower = dataManager.GetEstimatedEnemyMilitaryPower();

            if (enemyPower == 0) return 0.3f;

            float ratio = ourPower / enemyPower;

            return Mathf.Clamp01(1f - ratio);
        }

        private float CalculateEconomyBuildingNeed()
        {
            float resourceSaturation = dataManager.GetResourceSaturation();
            return Mathf.Clamp01(1f - resourceSaturation);
        }

        private float CalculateMilitaryBuildingNeed()
        {
            int militaryBuildings = dataManager.GetMilitaryBuildingCount();
            int idealMilitaryBuildings = dataManager.GetIdealMilitaryBuildingCount();

            return Mathf.Clamp01(1f - (float)militaryBuildings / idealMilitaryBuildings);
        }

        private float CalculateDefensiveBuildingNeed()
        {
            float enemyThreat = dataManager.GetEnemyThreatLevel();
            float baseDefense = dataManager.GetBaseDefenseLevel();

            if (baseDefense == 0) return 1f;

            return Mathf.Clamp01(enemyThreat / baseDefense);
        }

        private float CalculateDefenseNeed()
        {
            float enemyNearBase = dataManager.GetEnemyUnitsNearBase();
            float ourUnitsNearBase = dataManager.GetOurUnitsNearBase();

            return Mathf.Clamp01(enemyNearBase / (ourUnitsNearBase + 1)); // +1 to avoid division by zero
        }

        private float CalculateResearchNeed()
        {
            float gameTime = dataManager.GetGameTimeNormalized();
            float techProgress = dataManager.GetTechProgressNormalized();

            return Mathf.Clamp01(gameTime - techProgress);
        }

        private float CalculateAttackOpportunity()
        {
            float ourPower = dataManager.GetOurMilitaryPower();
            float enemyPower = dataManager.GetEstimatedEnemyMilitaryPower();

            if (enemyPower == 0) return 1f;

            float ratio = ourPower / enemyPower;

            return Mathf.Clamp01(ratio - 1f);
        }

        private float CalculatePatrolNeed()
        {
            float mapControl = dataManager.GetMapControlValue();
            return Mathf.Clamp01(1f - mapControl);
        }

        private float CalculateHarassmentNeed()
        {
            float EnemyEcoExposure = dataManager.GetEnemyEcoExposureNormalized();
            return Mathf.Clamp01(EnemyEcoExposure);
        }

        #endregion

        #region Get Current Need
        // need more code polishing

        private BuildingType GetCurrentEcoBuildingTypeNeeded()
        {
            /*if (dataManager.IsGoldIncomeLow())
                return BuildingType.GoldMine;*/

            return BuildingType.LumberMill; // Placeholder for debug & monitoring purpose
        }

        private BuildingType GetCurrentMilBuildingTypeNeeded()
        {
            /*if (playerInfo.BuildingManager.HasBuilding(BuildingType.Barracks) == false)
                return BuildingType.Barracks;*/

            return BuildingType.SiegeWorkshop; // Placeholder for debug & monitoring purpose
        }

        private BuildingType GetCurrentDefBuildingTypeNeeded()
        {
            /*float threatDirection = dataManager.GetHighestThreatDirection();

            if (threatDirection < 0.5f)
                return BuildingType.Wall;*/

            return BuildingType.GuardTower; // Placeholder for debug & monitoring purpose
        }

        private ResearchType GetCurrentResearchTypeNeeded()
        {
            /*if (!dataManager.IsResearchCompleted(ResearchType.LevelUpAllAttackPoint))
                return ResearchType.LevelUpAllAttackPoint;*/

            return ResearchType.LevelUpBaseArmor; // Placeholder for debug & monitoring purpose
        }

        #endregion

        #region Goals Sorting

        private void SortGoals()
        {
            currentGoals = currentGoals
                .OrderByDescending(g => g.Score)
                .ToList();
        }

        #endregion

        #region Getter

        public GoalCoordinator GetGoalCoordinator()
        {
            return goalCoordinator;
        }

        #endregion
    }
}