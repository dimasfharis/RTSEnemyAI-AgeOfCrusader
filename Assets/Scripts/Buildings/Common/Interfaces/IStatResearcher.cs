using System;
using RTS.Common.Enums;

namespace RTS.Buildings.Common.Interfaces
{
    public interface IStatResearcher
    {
        event Action<float, float> OnResearchProgressChanged;
        public event Action<ResearchType> OnStatResearched;

        bool TryResearch(ResearchType researchType);
    }
}