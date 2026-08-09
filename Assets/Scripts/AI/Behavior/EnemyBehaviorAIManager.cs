using UnityEngine;
using System.Collections.Generic;
using RTS.Core;
using RTS.Common.Enums;
using RTS.AI.Decision;
using RTS.Data;
using System.Linq;
using RTS.AI.GoalManagement;
using System;

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
        private float idealGoalScoutExecuteInterval = 25f;
        private float idealResourceNeedsPerResourceNode = 15f;

        private float idealUnitAmountToLaunchAttack = 17f;
        private float idealUnitAmountSoCalledLaunchAttack = 7f;

        private float idealPopulationPerHouse = 4f;
        private float idealResourceRatioForBuild = 1.3f;
        private float idealUnitNeedsAmountToBuild = 3f;

        private float idealBuildingPerDefensiveBuildingCount = 3f;

        private float idealUnitNeedsToPrioritizeTrainGoal = 8f;
        private float idealResourceRatioForTrain = 1.3f;

        private float idealGoalAttackExecuteInterval = 120f;
        private float idealResourceTotalForAttack = 200f;

        private float idealUnitAmountToHarass = 7f;

        // Goal Dependency
        private Dictionary<BuildingType, AIGoal> buildGoalDepended;

        #region Initialization

        public EnemyBehaviorAIManager(PlayerInfo owner, AIManager aiManager)
        {
            playerInfo = owner;
            decisionManager = aiManager.GetDecisionMakingAIManager();
            aiProfile = aiManager.GetAIProfile();
            dataManager = owner.DataManager;

            goalCoordinator = new GoalCoordinator(playerInfo, this);

            buildGoalDepended = new Dictionary<BuildingType, AIGoal>();
            GoalDependencyInit();
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
            float attackNeed = CalculateAttackNeed();
            float resourceScoutNeed = CalculateResourceScoutNeed();
            float enemyScoutNeed = CalculateEnemyScoutNeed();
            float harassmentNeed = CalculateHarassmentNeed();
            float defenseNeed = CalculateDefenseNeed();

            float attackScore =
                attackNeed
                * aiProfile.AttackMultiplier
                * GetStrategyMultiplier(AIGoalType.LaunchAttackWave);

            float resourceScoutScore =
                resourceScoutNeed
                * aiProfile.ScoutMultiplier
                * GetStrategyMultiplier(AIGoalType.AssignScout, 0);

            float enemyScoutScore =
                enemyScoutNeed
                * aiProfile.ScoutMultiplier
                * GetStrategyMultiplier(AIGoalType.AssignScout, 1);

            float harassmentScore =
                harassmentNeed
                * aiProfile.HarassMultiplier
                * GetStrategyMultiplier(AIGoalType.AssignHarassment);

            float defenseScore =
                defenseNeed
                * aiProfile.DefenseMultiplier
                * GetStrategyMultiplier(AIGoalType.ReinforceDefense);

            AIGoal aiGoalAttack = new AIGoal(playerInfo, AIGoalType.LaunchAttackWave, attackScore);
            AIGoal aiGoalResourceScout = new AIGoal(playerInfo, AIGoalType.AssignScout, resourceScoutScore);
            AIGoal aiGoalEnemyScout = new AIGoal(playerInfo, AIGoalType.AssignScout, enemyScoutScore);
            AIGoal aiGoalHarassment = new AIGoal(playerInfo, AIGoalType.AssignHarassment, harassmentScore);
            AIGoal aiGoalDefense = new AIGoal(playerInfo, AIGoalType.ReinforceDefense, defenseScore);

            currentGoals.Add(aiGoalAttack);
            currentGoals.Add(aiGoalResourceScout);
            currentGoals.Add(aiGoalEnemyScout);
            currentGoals.Add(aiGoalHarassment);
            currentGoals.Add(aiGoalDefense);

            // Determine attack target position
            SetAttackTargetPosition(aiGoalAttack);

            // Determine scout target position
            SetResourceScoutTargetPosition(aiGoalResourceScout);
            SetEnemyScoutTargetPosition(aiGoalEnemyScout);

            // Determine harassment target position
            SetHarassmentTargetPosition(aiGoalHarassment);

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
            int idealWorkers = dataManager.GetIdealWorkerCount() == 0 ? 1 : dataManager.GetIdealWorkerCount();
            // add isDependedByOther, if true, then increase the score

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
            return 0f;

            /*float gameTime = dataManager.GetGameTimeNormalized();
            float techProgress = dataManager.GetTechProgressNormalized();

            return Mathf.Clamp01(gameTime - techProgress);*/
        }

        private float CalculateAttackNeed()
        {
            // Level of AI economy
            int ourTotalResource = dataManager.GetTotalResource();
            float resourceRatioScore = ourTotalResource / idealResourceTotalForAttack;

            // Ideal of military unit count
            int availableMilitaryUnit = dataManager.GetOurMilitaryUnitAvailable();
            float militaryUnitCountScore = availableMilitaryUnit / idealUnitAmountToLaunchAttack;

            // Percentage of known enemy info towards actual enemy info
            float enemyInfoPercentage = 1f - dataManager.GetKnownEnemyInfoPercentage();

            // Estimated power ratio
            float ourPower = dataManager.GetOurMilitaryPower();
            float enemyPower = dataManager.GetEstimatedEnemyMilitaryPower() != 0f ? dataManager.GetEstimatedEnemyMilitaryPower() : 20f;
            float powerRatio = ourPower / enemyPower;

            // Ideal time since last attack
            float latestAttackGoalExecutedTime = dataManager.GetElapsedTimeSinceLastGoalExecuted(AIGoalType.LaunchAttackWave);
            float attackGoalExecutedTimeRatio = latestAttackGoalExecutedTime / idealGoalAttackExecuteInterval;

            return (resourceRatioScore + militaryUnitCountScore + (enemyInfoPercentage * powerRatio) + attackGoalExecutedTimeRatio) / 4;
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
            float latestScoutGoalExecutedTime = dataManager.GetElapsedTimeSinceLastGoalExecuted(AIGoalType.AssignScout);
            float scoutGoalExecutedTimeRatio = latestScoutGoalExecutedTime / idealGoalScoutExecuteInterval;

            return knownResourceNodeRatio * knownResourceNodeTowardsAllRatio * mapControl * scoutGoalExecutedTimeRatio;
        }

        private float CalculateEnemyScoutNeed()
        {
            // Percentage of known enemy info towards actual enemy info
            float enemyInfoPercentage = 1f - dataManager.GetKnownEnemyInfoPercentage();

            // Ideal time since last enemy scout
            float latestScoutGoalExecutedTime = dataManager.GetElapsedTimeSinceLastGoalExecuted(AIGoalType.AssignScout);
            float scoutGoalExecutedTimeRatio = latestScoutGoalExecutedTime / idealGoalScoutExecuteInterval;

            return enemyInfoPercentage * scoutGoalExecutedTimeRatio;
        }

        private float CalculateHarassmentNeed()
        {
            // Ideal of military unit count
            int availableMilitaryUnit = dataManager.GetOurMilitaryUnitAvailable();
            float militaryUnitCountScore = availableMilitaryUnit / idealUnitAmountToHarass;

            // Estimated enemy worker exposed from base position
            float EnemyEcoExposure = dataManager.GetEnemyEcoExposureNormalized();

            return militaryUnitCountScore * EnemyEcoExposure;
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

        private float CalculateHouseBuildingNeed()
        {
            // Ratio of population capacity towards needs of unit training goals
            int populationNeeds = dataManager.GetPopulationCapacityAmountNeeds();
            int idealHouseAmount = Mathf.CeilToInt(populationNeeds / idealPopulationPerHouse);

            int ourHouseAmount = dataManager.GetBuildingCount(BuildingType.House);

            // Check if depended by other goal
            float dependedScore = buildGoalDepended[BuildingType.House] != null ? 1.3f : 1f;

            return (1f - (ourHouseAmount / (idealHouseAmount + 1))) * dependedScore; // +1 to prevent divided by zero
        }

        private float CalculateBarrackBuildingNeed()
        {
            // Percentage of available resources (settle condition for build military building)
            float buildingResourceNeedsRatio = dataManager.CalculateRatioResourceAmountNeeds(BuildingType.Barracks, idealResourceRatioForBuild);

            // Increase score, if depended by other goals
            int trainUnitNeedsCount = dataManager.GetTrainUnitNeedsCount(BuildingType.Barracks);
            float trainUnitNeedsScore = trainUnitNeedsCount / idealUnitNeedsAmountToBuild;

            // Check if depended by other goal
            float dependedScore = buildGoalDepended[BuildingType.Barracks] != null ? 1.3f : 1f;

            return buildingResourceNeedsRatio * trainUnitNeedsScore * dependedScore;
        }

        private float CalculateSiegeWorkshopNeed()
        {
            // Percentage of available resources (settle condition for build military building)
            float buildingResourceNeedsRatio = dataManager.CalculateRatioResourceAmountNeeds(BuildingType.SiegeWorkshop, idealResourceRatioForBuild);

            // Increase score, if depended by other goals
            int trainUnitNeedsCount = dataManager.GetTrainUnitNeedsCount(BuildingType.SiegeWorkshop);
            float trainUnitNeedsScore = trainUnitNeedsCount / idealUnitNeedsAmountToBuild;

            // Check if depended by other goal
            float dependedScore = buildGoalDepended[BuildingType.SiegeWorkshop] != null ? 1.3f : 1f;

            return buildingResourceNeedsRatio * trainUnitNeedsScore * dependedScore;
        }

        private BuildingType GetCurrentDefBuildingTypeNeeded()
        {
            int selectIndex = UnityEngine.Random.Range(1, 3);

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

        private void SetAttackTargetPosition(AIGoal aiGoal)
        {
            if (aiGoal == null || aiGoal.GoalType != AIGoalType.LaunchAttackWave)
                return;

            Vector3 aroundEnemyBaseTile = dataManager.GetTilesAroundEnemyBaseRandomly(10f);

            aiGoal.SetTargetPosition(aroundEnemyBaseTile);
        }

        private void SetResourceScoutTargetPosition(AIGoal aiGoal)
        {
            if (aiGoal == null || aiGoal.GoalType != AIGoalType.AssignScout)
                return;

            Vector3 unknownTile = dataManager.GetUnexploredTilesRandomly();

            aiGoal.SetTargetPosition(unknownTile);
        }

        private void SetEnemyScoutTargetPosition(AIGoal aiGoal)
        {
            if (aiGoal == null || aiGoal.GoalType != AIGoalType.AssignScout)
                return;

            Vector3 aroundEnemyBaseTile = dataManager.GetTilesAroundEnemyBaseRandomly(18f);

            aiGoal.SetTargetPosition(aroundEnemyBaseTile);
        }

        private void SetHarassmentTargetPosition(AIGoal aiGoal)
        {
            if (aiGoal == null || aiGoal.GoalType != AIGoalType.AssignHarassment)
                return;

            Vector3 aroundEnemyBaseTile = dataManager.GetTilesAroundEnemyBaseRandomly(15f);

            aiGoal.SetTargetPosition(aroundEnemyBaseTile);
        }

        private void SetBaseDefensePosition(AIGoal aiGoal)
        {
            if (aiGoal == null || aiGoal.GoalType != AIGoalType.ReinforceDefense)
                return;

            Vector3 defensePosition = dataManager.GetBaseDefensePosition();

            aiGoal.SetTargetPosition(defensePosition);
        }

        #endregion

        #region Goal Dependency

        private void GoalDependencyInit()
        {
            foreach (BuildingType buildingType in Enum.GetValues(typeof(BuildingType)))
            {
                if (buildingType == BuildingType.None)
                    continue;

                buildGoalDepended[buildingType] = null;
            }
        }

        public void SetBuildGoalDependency(List<BuildingType> buildingType, AIGoal aiGoal)
        {
            if (buildingType == null)
                return;

            foreach (var type in buildingType)
            {
                SetBuildGoalDependency(type, aiGoal);
            }
        }

        public void SetBuildGoalDependency(BuildingType buildingType, AIGoal aiGoal)
        {
            buildGoalDepended[buildingType] = aiGoal;
        }

        public void UnsetBuildGoalDependency(AIGoal aiGoal)
        {
            if (aiGoal == null)
                return;

            foreach (var buildGoal in buildGoalDepended)
            {
                if (buildGoal.Value == aiGoal)
                {
                    SetBuildGoalDependency(buildGoal.Key, null);
                }
            }
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