using RTS.AI.Behavior;
using RTS.Buildings.Common;
using RTS.Common.DataClass;
using RTS.Common.Enums;
using RTS.Core;
using RTS.Data;
using RTS.Managers;
using RTS.Managers.Map;
using RTS.Units.Common;
using RTS.Units.Military;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTS.AI.GoalManagement
{
    public class MilitaryGoalExecutor
    {
        private PlayerInfo playerInfo;
        private GoalCoordinator goalCoordinator;
        private MilitaryUnitManager militaryUnitManager;
        private DataManager dataManager;
        private MapManager mapManager;

        private Dictionary<AIGoal, MilitaryGroup> activeGroups;
        private List<AIGoal> goalsToRemove;

        private BaseBuildingController townCenter;

        // Requirement Parameters
        private int requiredUnitsForScout = 2;
        private int requiredTilesForScout = 300;
        private int requiredUnitsForDefense = 6;
        private int requiredDefenseTime = 45;

        // Timer
        private float defenseGoalTimer;
        private float defenseGoalInterval = 1f;

        #region Initialization

        public MilitaryGoalExecutor(PlayerInfo owner, GoalCoordinator goalCoordinator)
        {
            this.playerInfo = owner;
            this.goalCoordinator = goalCoordinator;
            militaryUnitManager = owner.MilitaryUnitManager;
            dataManager = owner.DataManager;
            mapManager = owner.MapManager;

            activeGroups = new Dictionary<AIGoal, MilitaryGroup>();
            goalsToRemove = new List<AIGoal>();

            // dont search here
            townCenter = playerInfo.BuildingManager.GetBuilding(BuildingType.TownCenter);
        }

        #endregion

        #region Tick

        public void Tick()
        {
            if (activeGroups.Count <= 0) return;

            foreach (var goal in activeGroups.Keys)
            {
                GoalTypeTick(goal);
            }

            CheckRemovedGoals();
        }

        private void GoalTypeTick(AIGoal goal)
        {
            switch (goal.GoalType)
            {
                case AIGoalType.LaunchAttackWave:
                    AttackWaveTick(goal);
                    break;

                case AIGoalType.AssignScout:
                    ScoutTick(goal);
                    break;

                case AIGoalType.AssignHarassment:
                    HarassmentTick(goal);
                    break;

                case AIGoalType.ReinforceDefense:
                    ReinforceDefenseTick(goal);
                    break;
            }
        }

        private void CheckRemovedGoals()
        {
            for (int i = 0; i < goalsToRemove.Count; i++)
            {
                activeGroups.Remove(goalsToRemove[i]);
            }
        }

        #endregion

        #region Execution

        public void Execute(AIGoal goal)
        {
            if (goal.IsCompleted)
                return;

            switch (goal.GoalType)
            {
                case AIGoalType.LaunchAttackWave:
                    //ExecuteAttackWave(goal);
                    break;

                case AIGoalType.AssignScout:
                    ExecuteScout(goal);
                    break;

                case AIGoalType.AssignHarassment:
                    //ExecuteHarassment(goal);
                    break;

                case AIGoalType.ReinforceDefense:
                    ExecuteReinforceDefense(goal);
                    break;
            }
        }

        #endregion

        #region Attack Wave

        private void ExecuteAttackWave(AIGoal goal)
        {
            if (!activeGroups.ContainsKey(goal))
            {
                InitAttackWaveGroup(goal);
            }

            MilitaryGroup group = activeGroups[goal];

            if (!group.isEngaged)
            {
                // militaryUnitManager.IssueMoveCommand(group.Units, group.TargetPosition);

                group.isEngaged = true;
            }
        }

        private void AttackWaveTick(AIGoal aiGoal)
        {

        }

        private void InitAttackWaveGroup(AIGoal goal)
        {
            // InitializeGroup(goal);
        }

        #endregion

        #region Scout

        private void ExecuteScout(AIGoal goal)
        {
            if (!CanExecuteScout(goal))
            {
                FulfillScoutRequirements(goal);
                return;
            }

            if (!activeGroups.ContainsKey(goal))
            {
                InitScoutGroup(goal);
                goal.OnExecuteStarted += Scout_OnExecuteStarted;
                goal.OnCompleted += Scout_OnCompleted;
            }
        }

        private void ScoutTick(AIGoal goal)
        {
            if (CanExecuteScout(goal) && !goal.IsExecuteStarted && !goal.IsCompleted)
            {
                ScoutStartExecute(goal);
                goal.StartExecute();
            }
        }

        private void ScoutStartExecute(AIGoal goal)
        {
            MilitaryGroup group = activeGroups[goal];

            Vector3 scoutPoint = goal.TargetPosition;

            if (!group.isEngaged)
            {
                militaryUnitManager.IssueMoveCommand(group.units, scoutPoint);
                goal.IsExecuteStarted = true;
            }
        }

        private bool CanExecuteScout(AIGoal goal)
        {
            // Check if there are units available for scouting
            if (militaryUnitManager.GetAvailableUnits().Count < requiredUnitsForScout)
                return false;

            return true;
        }

        private void FulfillScoutRequirements(AIGoal goal)
        {
            if (!goal.IsFulfillingReqProgress)
            {
                goal.MarkFulfillingProgress();

                Dictionary<UnitType, int> requiredUnits = GetRecommendedUnit(goal.GoalType);
                goalCoordinator.AddUnitRequest(goal, requiredUnits);

                Debug.Log($"Player {playerInfo.PlayerNumber} Request unit composition to {goal.GoalType}");
            }
        }

        private void InitScoutGroup(AIGoal goal)
        {
            List<MilitaryUnitController> availableUnits = militaryUnitManager.GetAvailableUnits();

            List<MilitaryUnitController> assignedUnits = availableUnits
                .Take(requiredUnitsForScout)
                .ToList();

            MilitaryGroup group = new MilitaryGroup(assignedUnits.Cast<BaseUnitController>().ToList(), playerInfo, goal)
            {
                targetPosition = goal.TargetPosition,
            };

            foreach (var unit in assignedUnits)
            {
                unit.activatedGoal = goal;
                goal.AssignedUnits.Add(unit);
            }

            goal.targetAmount = requiredTilesForScout;

            activeGroups.Add(goal, group);
        }

        private void Scout_OnExecuteStarted(AIGoal aiGoal)
        {
            goalCoordinator.RecordExecutedGoal(aiGoal);
        }

        private void Scout_OnCompleted(AIGoal aiGoal)
        {
            Vector3 townCenterPosition = playerInfo.BuildingManager.GetBuilding(BuildingType.TownCenter).transform.position;
            MilitaryGroup group = activeGroups[aiGoal];
            List<BaseUnitController> assignedUnits = group.units;

            militaryUnitManager.IssueMoveCommand(assignedUnits, townCenterPosition);

            goalCoordinator.RecordCompletedGoal(aiGoal);

            aiGoal.UnlinkRelations();
            activeGroups[aiGoal].OnGroupDisbanded();
            activeGroups.Remove(aiGoal);

            // Send report log later
        }

        #endregion

        #region Harassment

        private void ExecuteHarassment(AIGoal goal)
        {
            if (!activeGroups.ContainsKey(goal))
            {
                InitHarassmentGroup(goal);
            }

            MilitaryGroup group = activeGroups[goal];

            Vector3 ecoTarget = dataManager.GetEstimatedExposedEnemyWorkerPosition();

            if (!group.isEngaged)
            {
                // militaryUnitManager.IssueMoveCommand(group.Units, ecoTarget);
            }
        }

        private void HarassmentTick(AIGoal aiGoal)
        {

        }

        private void InitHarassmentGroup(AIGoal goal)
        {
            //InitializeGroup(goal);
        }

        #endregion

        #region Reinforce Defense

        private void ExecuteReinforceDefense(AIGoal goal)
        {
            if (!CanExecuteDefense(goal))
            {
                FulfillDefenseRequirements(goal);
                return;
            }

            if (!activeGroups.ContainsKey(goal))
            {
                InitDefenseGroup(goal);
                goal.OnExecuteStarted += Defense_OnExecuteStarted;
                goal.OnCompleted += Defense_OnCompleted;
            }
        }

        private void ReinforceDefenseTick(AIGoal goal)
        {
            if (CanExecuteDefense(goal) && !goal.IsExecuteStarted && !goal.IsCompleted)
            {
                DefenseStartExecute(goal);
                goal.StartExecute();
            }

            if (goal.IsExecuteStarted)
            {
                defenseGoalTimer += Time.deltaTime;

                if (defenseGoalTimer >= defenseGoalInterval)
                {
                    defenseGoalInterval++;

                    goal.AddProgress(1);
                }
            }
        }

        private void DefenseStartExecute(AIGoal goal)
        {
            MilitaryGroup group = activeGroups[goal];

            Vector3 defensePoint = goal.TargetPosition;

            if (!group.isEngaged)
            {
                militaryUnitManager.IssueMoveCommand(group.units, defensePoint);
            }
        }

        private bool CanExecuteDefense(AIGoal goal)
        {
            // Check if there are duplicated defense goal

            return true;
        }

        private void FulfillDefenseRequirements(AIGoal goal)
        {
            if (!goal.IsFulfillingReqProgress)
            {
                goal.MarkFulfillingProgress();

                Dictionary<UnitType, int> requiredUnits = GetRecommendedUnit(goal.GoalType);
                goalCoordinator.AddUnitRequest(goal, requiredUnits);

                Debug.Log($"Player {playerInfo.PlayerNumber} Request unit composition to {goal.GoalType}");
            }
        }

        private void InitDefenseGroup(AIGoal goal)
        {
            List<MilitaryUnitController> availableUnits = militaryUnitManager.GetAvailableUnits();

            List<MilitaryUnitController> assignedUnits = availableUnits
                .Take(requiredUnitsForDefense)
                .ToList();

            MilitaryGroup group = new MilitaryGroup(assignedUnits.Cast<BaseUnitController>().ToList(), playerInfo, goal)
            {
                targetPosition = goal.TargetPosition,
            };

            foreach (var unit in assignedUnits)
            {
                unit.activatedGoal = goal;
                goal.AssignedUnits.Add(unit);
            }

            // Set defense time
            goal.targetAmount = requiredDefenseTime;

            activeGroups.Add(goal, group);
        }

        private void Defense_OnExecuteStarted(AIGoal aiGoal)
        {

        }

        private void Defense_OnCompleted(AIGoal aiGoal)
        {
            aiGoal.UnlinkRelations();
            activeGroups[aiGoal].OnGroupDisbanded();
            goalsToRemove.Add(aiGoal);

            // Send report log later
        }

        #endregion

        #region Unit Composition Recommendation

        private Dictionary<UnitType, int> GetRecommendedUnit(AIGoalType goalType)
        {
            Dictionary<UnitType, int> recommendedUnits = new Dictionary<UnitType, int>();

            switch (goalType)
            {
                case AIGoalType.AssignScout:
                    recommendedUnits = GetUnitCompositionScout();
                    break;
                case AIGoalType.AssignHarassment:
                    recommendedUnits = GetUnitCompositionHarassment();
                    break;
                case AIGoalType.ReinforceDefense:
                    recommendedUnits = GetUnitCompositionReinforceDefense();
                    break;
                case AIGoalType.LaunchAttackWave:
                    recommendedUnits = GetUnitCompositionLaunchAttackWave();
                    break;
                default:
                    recommendedUnits.Add(UnitType.Militia, 1);
                    break;
            }

            return recommendedUnits;
        }

        private Dictionary<UnitType, int> GetUnitCompositionScout()
        {
            Dictionary<UnitType, int> recommendedUnits = new Dictionary<UnitType, int>();

            recommendedUnits.Add(UnitType.Militia, 2);

            return recommendedUnits;
        }

        private Dictionary<UnitType, int> GetUnitCompositionHarassment()
        {
            Dictionary<UnitType, int> recommendedUnits = new Dictionary<UnitType, int>();

            recommendedUnits.Add(UnitType.Swordsman, 1);
            recommendedUnits.Add(UnitType.Crossbowman, 1);

            return recommendedUnits;
        }

        private Dictionary<UnitType, int> GetUnitCompositionReinforceDefense()
        {
            Dictionary<UnitType, int> recommendedUnits = dataManager.GetTrainUnitComposition();

            return recommendedUnits;
        }

        private Dictionary<UnitType, int> GetUnitCompositionLaunchAttackWave()
        {
            Dictionary<UnitType, int> recommendedUnits = dataManager.GetTrainUnitComposition();

            return recommendedUnits;
        }

        #endregion

        #region Completion Check

        public bool IsGoalCompleted(AIGoal goal)
        {
            if (!activeGroups.ContainsKey(goal))
                return true;

            MilitaryGroup group = activeGroups[goal];

            group.units.RemoveAll(u => u == null);

            if (group.units.Count == 0)
            {
                CleanUp(goal);
                return true;
            }

            switch (goal.GoalType)
            {
                case AIGoalType.LaunchAttackWave:
                    if (AllUnitsDeadOrIdle(group))
                    {
                        CleanUp(goal);
                        return true;
                    }
                    break;

                case AIGoalType.AssignScout:
                    return false; // patrols are ongoing

                case AIGoalType.AssignHarassment:
                    if (AllUnitsDeadOrIdle(group))
                    {
                        CleanUp(goal);
                        return true;
                    }
                    break;

                case AIGoalType.ReinforceDefense:
                    if (dataManager.GetEstimatedEnemyUnitsInRadius(townCenter.transform.position, 20f) <= 0f)
                    {
                        CleanUp(goal);
                        return true;
                    }
                    break;
            }

            return false;
        }

        #endregion

        #region Helpers

        private bool AreUnitsNearTarget(MilitaryGroup group)
        {
            foreach (var unit in group.units)
            {
                if (Vector3.Distance(unit.transform.position, group.targetPosition) > 5f)
                    return false;
            }

            return true;
        }

        private bool AllUnitsDeadOrIdle(MilitaryGroup group)
        {
            return group.units.All(u => u == null || u.IsIdle());
        }

        private Vector3 DetermineTargetPosition(AIGoal goal)
        {
            switch (goal.GoalType)
            {
                case AIGoalType.LaunchAttackWave:
                    return mapManager.GetKnownEnemyBasePosition();

                case AIGoalType.AssignHarassment:
                    return dataManager.GetEstimatedExposedEnemyWorkerPosition();

                case AIGoalType.AssignScout:
                    //return dataManager.GetScoutPoint();

                case AIGoalType.ReinforceDefense:
                    return dataManager.GetBaseDefensePosition();
            }

            return Vector3.zero;
        }

        private void CleanUp(AIGoal goal)
        {
            if (!activeGroups.ContainsKey(goal))
                return;

            foreach (var unit in activeGroups[goal].units)
            {
                if (unit != null)
                    militaryUnitManager.IssueStopCommand(unit);
            }

            activeGroups.Remove(goal);
        }

        #endregion
    }
}