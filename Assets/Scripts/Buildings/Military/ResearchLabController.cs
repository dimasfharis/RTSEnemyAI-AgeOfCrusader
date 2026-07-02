using RTS.Buildings.Common;
using RTS.Common.Enums;
using RTS.Buildings.Common.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Buildings.Military
{
    public class ResearchLabController : BaseBuildingController, IStatResearcher
    {
        public event Action<float, float> OnResearchProgressChanged;
        public event Action<ResearchType> OnStatResearched;

        private Queue<ResearchType> researchQueue = new Queue<ResearchType>();

        private float currentResearchProgress;

        #region Update Lifecycle

        protected override void Update()
        {
            base.Update();
            HandleResearch();
        }

        #endregion

        #region Stat Research

        public bool TryResearch(ResearchType researchType)
        {
            if (!CanStartResearch(researchType))
                return false;

            var cost = playerInfo.DataManager.researchDatabase.GetResourceCost(researchType);
            bool canStart = playerInfo.ResourceManager.SpendResource(cost);

            if (canStart)
            {
                researchQueue.Enqueue(researchType);
                return true;
            }

            return false;
        }

        private bool CanStartResearch(ResearchType researchType)
        {
            if (playerInfo.ResearchManager.IsResearched(researchType))
                return false;

            var cost = playerInfo.DataManager.researchDatabase.GetResourceCost(researchType);

            if (!playerInfo.ResourceManager.CanAfford(cost))
                return false;

            return true;
        }

        #endregion

        #region Research Lifecycle

        private void HandleResearch()
        {
            if (researchQueue.Count <= 0)
                return;

            currentResearchProgress += Time.deltaTime;

            ResearchType currentResearch = researchQueue.Peek();
            float researchTime = playerInfo.DataManager.researchDatabase.GetResearchTime(currentResearch);

            OnResearchProgressChanged.Invoke(currentResearchProgress, researchTime);

            if (currentResearchProgress >= researchTime)
            {
                currentResearchProgress = 0f;

                researchQueue.Dequeue();
                playerInfo.ResearchManager.AddResearchedType(currentResearch);

                OnStatResearched.Invoke(currentResearch);
            }
        }

        #endregion
    }
}