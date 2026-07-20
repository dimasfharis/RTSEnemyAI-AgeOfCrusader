using RTS.AI.Behavior;
using RTS.Common.Enums;
using RTS.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTS.AI.GoalManagement
{
    public class GoalCoordinator
    {
        public static event Action<List<AIGoal>, List<AIGoal>, PlayerInfo> OnGoalsGeneratedChanged;

        private const int MaxGoalPoolSize = 10;
        private const int MaxActiveGoals = 4;

        private float goalsGeneratingTime;
        private const float updateGoalsTime = 12f;
        private float goalExecutorTime;
        private const float updateExecutorTime = 0.5f;

        private List<AIGoal> goalPool;
        private List<AIGoal> activeGoals;

        private PlayerInfo playerInfo;
        private EnemyBehaviorAIManager behaviorManager;

        private ProductionGoalExecutor productionExecutor;
        private BuildGoalExecutor buildExecutor;
        private ResearchGoalExecutor researchExecutor;
        private MilitaryGoalExecutor militaryExecutor;

        // ===== Requirement Fullfillment =====
        public Dictionary<AIGoal, UnitsRequest> unitRequests = new Dictionary<AIGoal, UnitsRequest>();

        #region Initialization

        public GoalCoordinator(PlayerInfo owner, EnemyBehaviorAIManager behaviorManager)
        {
            playerInfo = owner;

            this.behaviorManager = behaviorManager;

            productionExecutor = new ProductionGoalExecutor(playerInfo);
            buildExecutor = new BuildGoalExecutor(playerInfo);
            researchExecutor = new ResearchGoalExecutor(playerInfo);
            militaryExecutor = new MilitaryGoalExecutor(playerInfo, this);

            goalPool = new List<AIGoal>();
            activeGoals = new List<AIGoal>();

            /*MaintainGoalPool();
            ActivateTopGoals();*/
        }

        #endregion

        #region Update Loop

        public void Tick()
        {
            GoalGeneratingTick();
            GoalExecutorTick();
        }

        private void GoalGeneratingTick()
        {
            goalsGeneratingTime += Time.deltaTime;

            if (goalsGeneratingTime >= updateGoalsTime)
            {
                goalsGeneratingTime = 0f;

                MaintainGoalPool();
                ActivateTopGoals();
                ExecuteActiveGoals();
                CleanupCompletedGoals();

                OnGoalsGeneratedChanged.Invoke(goalPool, activeGoals, playerInfo);
            }
        }

        private void GoalExecutorTick()
        {
            goalExecutorTime += Time.deltaTime;

            if (goalExecutorTime >= updateExecutorTime)
            {
                goalExecutorTime = 0f;

                productionExecutor.Tick();
                buildExecutor.Tick();
                researchExecutor.Tick();
                militaryExecutor.Tick();
            }
        }

        #endregion

        #region Maintain Goal Pool

        private void MaintainGoalPool()
        {
            if (goalPool.Count >= MaxGoalPoolSize)
                return;

            List<AIGoal> newGoals = behaviorManager.EvaluateGoals();

            foreach (var goal in newGoals)
            {
                if (goalPool.Count >= MaxGoalPoolSize)
                    break;

                // Avoid adding duplicate goal types to the pool
                if (!goalPool.Any(g => g.GoalType == goal.GoalType))
                    goalPool.Add(goal);
            }


            // Keep the goal pool sorted by score and trim to max size
            goalPool = goalPool
                .OrderByDescending(g => g.Score)
                .Take(MaxGoalPoolSize)
                .ToList();
        }

        #endregion

        #region Activate Top Goals

        private void ActivateTopGoals()
        {
            if (goalPool.Count > 0)
            {
                goalPool = goalPool
                .OrderByDescending(g => g.Score)
                .ToList();

                foreach (var goal in goalPool)
                {
                    if (activeGoals.Count >= MaxActiveGoals)
                        break;

                    if (activeGoals.Contains(goal))
                        continue;

                    if (CanActivate(goal))
                    {
                        goal.MarkActive();
                        activeGoals.Add(goal);
                    }
                }
            }
        }

        private bool CanActivate(AIGoal goal)
        {
            foreach (var active in activeGoals)
            {
                if (IsConflict(active, goal))
                    return false;
            }

            return true;
        }

        private bool IsConflict(AIGoal a, AIGoal b)
        {
            return false; // Placeholder for actual conflict logic based on goal types and payloads
        }

        #endregion

        #region Execute Active Goals

        private void ExecuteActiveGoals()
        {
            if (activeGoals.Count > 0)
            {
                activeGoals = activeGoals
                .OrderByDescending(g => g.Score)
                .ToList();

                foreach (var goal in activeGoals)
                {
                    ExecuteGoal(goal);
                }
            }
        }

        private void ExecuteGoal(AIGoal goal)
        {
            switch (goal.GoalType)
            {
                case AIGoalType.TrainUnit:
                    productionExecutor.Execute(goal);
                    break;

                case AIGoalType.BuildStructure:
                    buildExecutor.Execute(goal);
                    break;

                case AIGoalType.ResearchUpgrade:
                    researchExecutor.Execute(goal);
                    break;

                case AIGoalType.LaunchAttackWave:
                case AIGoalType.AssignScout:
                case AIGoalType.AssignHarassment:
                case AIGoalType.ReinforceDefense:
                    //militaryExecutor.Execute(goal);
                    break;
            }
        }

        #endregion

        #region Cleanup Completed Goals

        private void CleanupCompletedGoals()
        {
            activeGoals.RemoveAll(g => g.IsCompleted == true);
            goalPool.RemoveAll(g => g.IsCompleted == true || g.IsActive == true);
        }

        #endregion

        #region Requirement Fulfillment

        public void AddUnitRequest(AIGoal goal, Dictionary<UnitType, int> unitRequests)
        {
            if (this.unitRequests.ContainsKey(goal))
            {
                this.unitRequests[goal] = new UnitsRequest(unitRequests);
            }
            else
            {
                this.unitRequests.Add(goal, new UnitsRequest(unitRequests));
            }
        }

        #endregion

        #region Getter

        public MilitaryGoalExecutor GetMilitaryGoalExecutor()
        {
            return militaryExecutor;
        }

        public List<AIGoal> GetActiveGoals()
        {
            return activeGoals;
        }

        #endregion
    }

    #region Structs

    public struct UnitsRequest
    {
        public Dictionary<UnitType, int> unitRequests;

        public UnitsRequest(Dictionary<UnitType, int> unitRequests)
        {
            this.unitRequests = unitRequests;
        }
    }

    #endregion
}