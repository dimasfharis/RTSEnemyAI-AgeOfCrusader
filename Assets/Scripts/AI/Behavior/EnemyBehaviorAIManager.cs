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

        private GoalCoordinator goalCoordinator;

        private List<AIGoal> currentGoals = new List<AIGoal>();

        // Ideal Value
        private float idealGoalScoutExecuteInterval = 30f;
        private float idealResourceNeedsPerResourceNode = 15f;

        private float idealUnitAmountToLaunchAttack = 25f;
        private float idealUnitAmountSoCalledLaunchAttack = 7f;

        private float idealPopulationPerHouse = 4f;
        private float idealResourceRatioForBuild = 1.3f;
        private float idealUnitNeedsAmountToBuild = 3f;

        private float idealBuildingPerDefensiveBuildingCount = 3f;

        private float idealUnitNeedsToPrioritizeTrainGoal = 8f;
        private float idealResourceRatioForTrain = 1.3f;

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
                militaryNeed
                * aiProfile.MilitaryMultiplier
                * GetStrategyMultiplier(AIGoalType.TrainUnit, 1);

            AIGoal trainWorkerGoal = new AIGoal(playerInfo, AIGoalType.TrainUnit, workerScore);
            trainWorkerGoal.SetUnitTrainingRequirements(new Dictionary<UnitType, int> { { UnitType.Worker, 1 } });
            currentGoals.Add(trainWorkerGoal);

            if (goalCoordinator.unitRequests.Count > 0)
                currentGoals.AddRange(GetMultipleMilitaryUnitTrainGoal(militaryScore));
        }

        private List<AIGoal> GetMultipleMilitaryUnitTrainGoal(float baseScore)
        {
            if (goalCoordinator.unitRequests.Count <= 0)
                return new List<AIGoal>();

            List<AIGoal> militaryGoals = new List<AIGoal>();

            foreach (var request in goalCoordinator.unitRequests)
            {
                float score = baseScore * request.Key.Score;
                AIGoal goal = new AIGoal(playerInfo, AIGoalType.TrainUnit, score);

                goal.SetUnitTrainingRequirements(request.Value.unitRequests);

                militaryGoals.Add(goal);
            }

            return militaryGoals;
        }

        #endregion

        #region Build Goals Evaluation

        private void EvaluateBuildGoals()
        {
            float houseBuildingNeed = CalculateHouseBuildingNeed();
            float barrackBuildingNeed = CalculateBarrackBuildingNeed();
            float siegeWorkshopBuildingNeed = CalculateSiegeWorkshopNeed();
            float defensiveBuildingNeed = CalculateDefensiveBuildingNeed();

            float houseScore =
                houseBuildingNeed
                * aiProfile.EconomyBuildingMultiplier
                * GetStrategyMultiplier(AIGoalType.BuildStructure, 0);

            float barrackScore =
                barrackBuildingNeed *
                aiProfile.MilitaryBuildingMultiplier *
                GetStrategyMultiplier(AIGoalType.BuildStructure, 1);

            float siegeScore =
                siegeWorkshopBuildingNeed *
                aiProfile.MilitaryBuildingMultiplier *
                GetStrategyMultiplier(AIGoalType.BuildStructure, 1);

            float defenseScore =
                defensiveBuildingNeed *
                aiProfile.DefenseMultiplier *
                GetStrategyMultiplier(AIGoalType.BuildStructure, 2);

            AIGoal buildHouseGoal = new AIGoal(playerInfo, AIGoalType.BuildStructure, houseScore);
            AIGoal buildBarrackGoal = new AIGoal(playerInfo, AIGoalType.BuildStructure, barrackScore);
            AIGoal buildSiegeGoal = new AIGoal(playerInfo, AIGoalType.BuildStructure, siegeScore);
            AIGoal buildDefensiveGoal = new AIGoal(playerInfo, AIGoalType.BuildStructure, defenseScore);

            buildHouseGoal.SetBuilding(BuildingType.House);
            buildBarrackGoal.SetBuilding(BuildingType.Barracks);
            buildSiegeGoal.SetBuilding(BuildingType.SiegeWorkshop);
            buildDefensiveGoal.SetBuilding(GetCurrentDefBuildingTypeNeeded());

            currentGoals.Add(buildHouseGoal);
            currentGoals.Add(buildBarrackGoal);
            currentGoals.Add(buildSiegeGoal);
            currentGoals.Add(buildDefensiveGoal);
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
                new AIGoal(playerInfo, AIGoalType.ResearchUpgrade, researchScore)
                .SetResearch(researchTypeNeeded));
        }

        #endregion

        #region Military Goals Evaluation

        private void EvaluateMilitaryGoals()
        {
            float attackOpportunity = CalculateAttackOpportunity();
            float resourceScoutNeed = CalculateResourceScoutNeed();
            float enemyScoutNeed = CalculateEnemyScoutNeed();
            float harassmentNeed = CalculateHarassmentNeed();
            float defenseNeed = CalculateDefenseNeed();

            float attackScore =
                attackOpportunity *
                aiProfile.AttackMultiplier *
                GetStrategyMultiplier(AIGoalType.LaunchAttackWave);

            float resourceScoutScore =
                resourceScoutNeed *
                aiProfile.ScoutMultiplier *
                GetStrategyMultiplier(AIGoalType.AssignScout, 0);

            float enemyScoutScore =
                enemyScoutNeed
                * aiProfile.ScoutMultiplier
                * GetStrategyMultiplier(AIGoalType.AssignScout, 1);

            float harassmentScore =
                harassmentNeed *
                aiProfile.HarassMultiplier *
                GetStrategyMultiplier(AIGoalType.AssignHarassment);

            float defenseScore =
                defenseNeed *
                aiProfile.DefenseMultiplier *
                GetStrategyMultiplier(AIGoalType.ReinforceDefense);

            currentGoals.Add(
                new AIGoal(playerInfo, AIGoalType.LaunchAttackWave, attackScore));

            AIGoal aiGoalResourceScout = new AIGoal(playerInfo, AIGoalType.AssignScout, resourceScoutScore);
            AIGoal aiGoalEnemyScout = new AIGoal(playerInfo, AIGoalType.AssignScout, enemyScoutScore);

            currentGoals.Add(aiGoalResourceScout);
            currentGoals.Add(aiGoalEnemyScout);

            currentGoals.Add(
                new AIGoal(playerInfo, AIGoalType.AssignHarassment, harassmentScore));

            AIGoal aiGoalDefense = new AIGoal(playerInfo, AIGoalType.ReinforceDefense, defenseScore);

            currentGoals.Add(aiGoalDefense);

            // Determine scout target location
            SetResourceScoutTargetPosition(aiGoalResourceScout);
            SetEnemyScoutTargetPosition(aiGoalEnemyScout);

            // Determine reinforce defense position
            SetBaseDefensePosition(aiGoalDefense);
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
            if (type == AIGoalType.BuildStructure && variant == 2) return 1.2f; //defensive building
            if (type == AIGoalType.AssignScout && variant == 0) return 1.2f; //resource scout
            if (type == AIGoalType.AssignScout && variant == 1) return 0.6f; //enemy scout
            if (type == AIGoalType.LaunchAttackWave) return 0.4f;
            return 1f;
        }

        private float StrategyAttack(AIGoalType type, int variant)
        {
            if (type == AIGoalType.TrainUnit && variant == 1) return 1.7f;
            if (type == AIGoalType.AssignScout && variant == 0) return 0.6f; //resource scout
            if (type == AIGoalType.AssignScout && variant == 1) return 1.3f; //enemy scout
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
            if (type == AIGoalType.AssignScout && variant == 0) return 1.1f; //resource scout
            if (type == AIGoalType.LaunchAttackWave) return 0.3f;
            return 1f;
        }

        #endregion

        #region Calculate Need

        private float CalculateWorkerNeed()
        {
            int currentWorkers = dataManager.GetWorkerCount();
            int idealWorkers = dataManager.GetIdealWorkerCount();
            // add isDependedByOther, if true, then increase the score

            if (idealWorkers == 0) return 0f;

            float ratio = (float)currentWorkers / idealWorkers;
            // sometimes, idealWorkers is 0, because 0 resource needs

            return Mathf.Clamp01(1f - ratio);
        }

        private float CalculateMilitaryNeed()
        {
            // Estimated power ratio
            float ourPower = dataManager.GetOurMilitaryPower();
            float enemyPower = dataManager.GetEstimatedEnemyMilitaryPower() != 0f ? dataManager.GetEstimatedEnemyMilitaryPower() : 0.01f;
            float powerRatio = ourPower / enemyPower;

            // Ratio of military unit needs
            int trainUnitsCount = dataManager.GetTotalUnitNeedsCount();
            float unitNeedsScore = trainUnitsCount / idealUnitNeedsToPrioritizeTrainGoal;

            // Ratio of bare minimum of available resources to train
            float unitResourceNeedsRatio = dataManager.CalculateRatioPrioritizedUnitNeedsResourceAmountNeeds(idealResourceRatioForTrain);

            return powerRatio * unitNeedsScore * unitResourceNeedsRatio;
        }

        private float CalculateDefensiveBuildingNeed()
        {
            // Military Power Ratio
            float militaryPowerRatio = dataManager.GetEstimatedMilitaryPowerRatio();
            float militaryPowerScore = 1f - militaryPowerRatio;

            // Building Count Ratio
            float nonDefensiveBuildingCount = dataManager.GetNonDefensiveBuildingCount();
            float buildingCountScore = nonDefensiveBuildingCount / idealBuildingPerDefensiveBuildingCount;

            // Percentage of available resources (settle condition for build defensive building)
            float buildingResourceNeedsRatio = dataManager.CalculateRatioResourceAmountNeeds(BuildingType.CannonTower, idealResourceRatioForBuild);

            return (militaryPowerScore + buildingCountScore + buildingResourceNeedsRatio) / 3;
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

        private float CalculateResourceScoutNeed()
        {
            // Percentage of ideal known resource node towards resource needs
            float idealKnownResourceNode = (float)dataManager.GetResourceNeedsAmount() / idealResourceNeedsPerResourceNode;
            int knownResourceNode = dataManager.GetKnownResourceNodeCount();
            float knownResourceNodeRatio = Mathf.Clamp01(1f - (knownResourceNode / idealKnownResourceNode));

            // Percentage of known resource node towards all of resource nodes
            int totalActiveResourceNodes = playerInfo.GameManager.ResourceNodeManager.GetTotalActiveNodes();
            float knownResourceNodeTowardsAllRatio = 1f - (knownResourceNode / totalActiveResourceNodes);

            // Percentage of known tile map towards all of tile map (Fog of war percentage)
            float mapControl = 1f - dataManager.GetMapControlValue();

            // Ideal time since last map scout
            float latestScoutGoalExecutedTime = dataManager.GetElapsedTimeSinceLastScout();
            float scoutGoalExecutedTimeRatio = latestScoutGoalExecutedTime / idealGoalScoutExecuteInterval;

            return knownResourceNodeRatio * knownResourceNodeTowardsAllRatio * mapControl * scoutGoalExecutedTimeRatio;
        }

        private float CalculateEnemyScoutNeed()
        {
            // Percentage of known enemy info towards actual enemy info
            float enemyInfoPercentage = 1f - dataManager.GetKnownEnemyInfoPercentage();

            // Ideal time since last enemy scout
            float latestScoutGoalExecutedTime = dataManager.GetElapsedTimeSinceLastScout();
            float scoutGoalExecutedTimeRatio = latestScoutGoalExecutedTime / idealGoalScoutExecuteInterval;

            return enemyInfoPercentage * scoutGoalExecutedTimeRatio;
        }

        private float CalculateHarassmentNeed()
        {
            float EnemyEcoExposure = dataManager.GetEnemyEcoExposureNormalized();
            return Mathf.Clamp01(EnemyEcoExposure);
        }

        private float CalculateDefenseNeed()
        {
            // Percentage of possibility the enemy will launch attack
            // known enemy military units towards ideal amount of unit to launch attack
            int knownEnemyMilitaryUnitCount = dataManager.GetEstimatedEnemyMilitaryUnitCount();
            float idealUnitAmountToLaunchAttackRatio = (float)knownEnemyMilitaryUnitCount / idealUnitAmountToLaunchAttack;

            // Percentage of actual enemy unit starting to enter the base
            int actualEnemyUnitsNearBase = dataManager.GetActualEnemyUnitsNearBaseInRadius(25f);
            float soCalledLaunchAttackScore = (float)actualEnemyUnitsNearBase / idealUnitAmountSoCalledLaunchAttack;

            // Percentage of our vs estimated enemy units. smaller our units, larger the score
            float ourUnitsNearBase = dataManager.GetOurUnitsNearBaseInRadius(20f);
            float ourVsEnemyEstimatedUnitAmount = 1f - ourUnitsNearBase / (knownEnemyMilitaryUnitCount + 1); // +1 to avoid division by zero

            return Mathf.Clamp01((idealUnitAmountToLaunchAttackRatio + soCalledLaunchAttackScore + soCalledLaunchAttackScore) / 3);
        }

        #endregion

        #region Building Needs
        // need more code polishing

        private float CalculateHouseBuildingNeed()
        {
            // Ratio of population capacity towards needs of unit training goals
            int populationNeeds = dataManager.GetPopulationCapacityAmountNeeds();
            int idealHouseAmount = Mathf.CeilToInt(populationNeeds / idealPopulationPerHouse);

            int ourHouseAmount = dataManager.GetBuildingCount(BuildingType.House);

            return 1f - (ourHouseAmount / (idealHouseAmount + 1)); // +1 to prevent divided by zero
        }

        private float CalculateBarrackBuildingNeed()
        {
            // Percentage of available resources (settle condition for build military building)
            float buildingResourceNeedsRatio = dataManager.CalculateRatioResourceAmountNeeds(BuildingType.Barracks, idealResourceRatioForBuild);

            // Increase score, if depended by other goals
            int trainUnitNeedsCount = dataManager.GetTrainUnitNeedsCount(BuildingType.Barracks);
            float trainUnitNeedsScore = trainUnitNeedsCount / idealUnitNeedsAmountToBuild;

            return buildingResourceNeedsRatio * trainUnitNeedsScore;
        }

        private float CalculateSiegeWorkshopNeed()
        {
            // Percentage of available resources (settle condition for build military building)
            float buildingResourceNeedsRatio = dataManager.CalculateRatioResourceAmountNeeds(BuildingType.SiegeWorkshop, idealResourceRatioForBuild);

            // Increase score, if depended by other goals
            int trainUnitNeedsCount = dataManager.GetTrainUnitNeedsCount(BuildingType.SiegeWorkshop);
            float trainUnitNeedsScore = trainUnitNeedsCount / idealUnitNeedsAmountToBuild;

            return buildingResourceNeedsRatio * trainUnitNeedsScore;
        }

        private BuildingType GetCurrentDefBuildingTypeNeeded()
        {
            int selectIndex = Random.Range(1, 2);

            if (selectIndex == 1)
            {
                return BuildingType.GuardTower;
            }else
            {
                return BuildingType.CannonTower;
            }
        }

        private ResearchType GetCurrentResearchTypeNeeded()
        {
            /*if (!dataManager.IsResearchCompleted(ResearchType.LevelUpAllAttackPoint))
                return ResearchType.LevelUpAllAttackPoint;*/

            return ResearchType.LevelUpBaseArmor; // Placeholder for debug & monitoring purpose
        }

        #endregion

        #region Goal Target Determination

        private void SetResourceScoutTargetPosition(AIGoal aiGoal)
        {
            if (aiGoal == null)
                return;

            Vector3 unknownTile = dataManager.GetUnexploredTilesRandomly();

            aiGoal.SetTargetPosition(unknownTile);
        }

        private void SetEnemyScoutTargetPosition(AIGoal aiGoal)
        {
            if (aiGoal == null)
                return;

            Vector3 aroundEnemyBaseTile = dataManager.GetTilesAroundEnemyBaseRandomly();

            aiGoal.SetTargetPosition(aroundEnemyBaseTile);
        }

        private void SetBaseDefensePosition(AIGoal aiGoal)
        {
            if (aiGoal == null)
                return;

            Vector3 defensePosition = dataManager.GetBaseDefensePosition();

            aiGoal.SetTargetPosition(defensePosition);
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