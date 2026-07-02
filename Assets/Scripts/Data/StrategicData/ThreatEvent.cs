using UnityEngine;

namespace RTS.Data.StrategicData
{
    public class ThreatEvent
    {
        public Vector3 Position;
        public float ThreatLevel;
        public float TimeStamp;

        public ThreatEvent(Vector3 position, float threatLevel)
        {
            Position = position;
            ThreatLevel = threatLevel;
            TimeStamp = Time.time;
        }
    }
}