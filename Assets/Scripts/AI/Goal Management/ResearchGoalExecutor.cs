using RTS.AI.Behavior;
using RTS.Core;
using RTS.Managers;
using RTS.Managers.Research;
using RTS.Common.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.AI.GoalManagement
{
    public class ResearchGoalExecutor
    {
        private PlayerInfo playerInfo;
        private ResearchManager researchManager;
        private ResourceManager resourceManager;
        private BuildingManager buildingManager;

        private const float executionTime = 12f;
        private float currentTime;

        #region Initialization

        public ResearchGoalExecutor(PlayerInfo owner)
        {
            playerInfo = owner;
            researchManager = owner.ResearchManager;
            resourceManager = owner.ResourceManager;
            buildingManager = owner.BuildingManager;
        }

        #endregion

        #region Tick

        public void Tick()
        {

        }

        #endregion

        #region Execution

        public void Execute(AIGoal goal)
        {
            currentTime += Time.deltaTime;

            if (currentTime >= executionTime)
            {
                currentTime = 0f;

                Debug.Log("executing research goal...");
            }

            /*if (goal.GoalType != AIGoalType.ResearchUpgrade)
                return;

            if (goal.IsCompleted)
                return;

            TryExecuteResearch(goal);*/
        }

        private void TryExecuteResearch(AIGoal goal)
        {
            // Get Research Type
            ResearchType researchType = goal.ResearchType;

            List<ResearchType> researched = researchManager.GetResearchedType();
            List<ResearchType> available = researchManager.GetAvailableResearchType();

            if (researched.Contains(researchType))
            {
                goal.MarkCompleted();
                return;
            }

            if (!available.Contains(researchType))
                return;

            // Has enough resource checking
            var cost = playerInfo.DataManager.researchDatabase.GetResourceCost(researchType);
            if (!resourceManager.CanAfford(cost))
                return;

            /*bool success = researchManager.TryStartResearch(researchType);

            if (success)
            {
                goal.MarkCompleted();
            }*/
        }

        #endregion
    }
}