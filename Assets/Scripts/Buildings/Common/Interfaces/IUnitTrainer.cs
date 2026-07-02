
using RTS.Common.Enums;
using System;

namespace RTS.Buildings.Common.Interfaces
{
    public interface IUnitTrainer
    {
        event Action<float, float> OnTrainingProgressChanged;
        public event Action<UnitType> OnUnitTrained;

        bool TryTrainUnit(UnitType unitType);
    }
}