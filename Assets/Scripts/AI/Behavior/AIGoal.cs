using RTS.Common.Enums;
using RTS.Units.Common;
using UnityEngine;
using System.Collections.Generic;
using System;
using RTS.Managers;
using RTS.Core;
using RTS.Common.Structs;

namespace RTS.AI.Behavior
{
    public class AIGoal
    {
        // References
        PlayerInfo playerInfo;
        ResourceManager resourceManager;

        // Events
        public event Action<AIGoal> OnExecuteStarted;
        public event Action<int, int> OnProgressChanged;
        public event Action<AIGoal> OnCompleted;

        // Goal Status
        public AIGoalType GoalType { get; private set; }
        public float Score { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsFulfillingReqProgress { get; private set; }
        public bool IsExecuteStarted;
        public bool IsCompleted { get; private set; }

        // Goal Progress
        public int currentProgress { get; private set; }
        public int targetAmount;

        // Unit Training Goal
        public Dictionary<UnitType, int> UnitTrainingRequirements { get; private set; }

        // Optional Payload
        public BuildingType BuildingType { get; private set; }
        public ResearchType ResearchType { get; private set; }
        public Vector3 TargetPosition { get; private set; }
        public List<BaseUnitController> AssignedUnits;

        #region Initialization

        public AIGoal(PlayerInfo playerInfo, AIGoalType type, float score)
        {
            this.playerInfo = playerInfo;
            resourceManager = playerInfo.ResourceManager;

            GoalType = type;
            Score = score;

            UnitTrainingRequirements = new Dictionary<UnitType, int>();
            AssignedUnits = new List<BaseUnitController>();

            currentProgress = 0;
            targetAmount = 0;
            BuildingType = BuildingType.None;
            ResearchType = ResearchType.None;
            TargetPosition = Vector3.zero;

            IsActive = false;
            IsFulfillingReqProgress = false;
            IsExecuteStarted = false;
            IsCompleted = false;

            OnExecuteStarted += AIGoal_OnExecuteStarted;
            OnProgressChanged += AIGoal_OnProgressChanged;
            OnCompleted += AIGoal_OnCompleted;
        }

        #endregion

        #region Lifecycle

        public void StartExecute()
        {
            OnExecuteStarted?.Invoke(this);
        }

        public void AddProgress(int progress)
        {
            Debug.Log($"{currentProgress} / {targetAmount}");

            currentProgress += progress;
            OnProgressChanged?.Invoke(currentProgress, targetAmount);
        }

        private void AIGoal_OnExecuteStarted(AIGoal aiGoal)
        {
            IsExecuteStarted = true;
        }

        private void AIGoal_OnProgressChanged(int currentProgress, int targetAmount)
        {
            if (currentProgress >= targetAmount)
            {
                OnCompleted?.Invoke(this);
            }
        }

        private void AIGoal_OnCompleted(AIGoal aiGoal)
        {
            IsFulfillingReqProgress = false;
            IsExecuteStarted = false;
            IsCompleted = true;

            OnProgressChanged -= AIGoal_OnProgressChanged;
            OnCompleted -= AIGoal_OnCompleted;
        }

        #endregion

        #region Relation

        public void UnlinkRelations()
        {
            foreach (var unit in AssignedUnits)
            {
                unit.UnassignGoal();
            }

            AssignedUnits.Clear();
        }

        #endregion

        #region Resource Needs

        public List<ResourceAmount> GetResourceNeeds()
        {
            switch (GoalType)
            {
                case AIGoalType.TrainUnit:
                    return GetTrainUnitResourceNeeds();

                case AIGoalType.BuildStructure:
                    return GetBuildStructureResourceNeeds();

                default:
                    return new List<ResourceAmount>();
            }
        }

        private List<ResourceAmount> GetTrainUnitResourceNeeds()
        {
            return resourceManager.GetProductionResourceAmount(UnitTrainingRequirements);
        }

        private List<ResourceAmount> GetBuildStructureResourceNeeds()
        {
            return resourceManager.GetStructureResourceAmount(new Dictionary<BuildingType, int> { { BuildingType, 1 } });
        }

        #endregion

        #region Public API

        public AIGoal SetBuilding(BuildingType buildingType)
        {
            BuildingType = buildingType;
            return this;
        }

        public AIGoal SetResearch(ResearchType researchType)
        {
            ResearchType = researchType;
            return this;
        }

        public AIGoal SetTargetPosition(Vector3 position)
        {
            TargetPosition = position;
            return this;
        }

        public void SetUnitTrainingRequirements(Dictionary<UnitType, int> requirements)
        {
            UnitTrainingRequirements = requirements;
        }

        public void AssignUnits(List<BaseUnitController> units)
        {
            AssignedUnits = units;
        }

        public void MarkActive()
        {
            IsActive = true;
        }

        public void MarkFulfillingProgress()
        {
            IsFulfillingReqProgress = true;
        }

        public void MarkCompleted()
        {
            IsCompleted = true;
            IsActive = false;
        }

        #endregion
    }
}