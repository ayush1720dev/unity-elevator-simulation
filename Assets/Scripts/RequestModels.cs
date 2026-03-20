using System;

namespace ElevatorSimulation
{
    [Serializable]
    public struct HallRequest : IEquatable<HallRequest>
    {
        public int Floor;
        public TravelDirection Direction;

        public HallRequest(int floor, TravelDirection direction)
        {
            Floor = floor;
            Direction = direction;
        }

        public bool Equals(HallRequest other)
        {
            return Floor == other.Floor && Direction == other.Direction;
        }

        public override bool Equals(object obj)
        {
            return obj is HallRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Floor * 397) ^ (int)Direction;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", Floor, Direction);
        }
    }

    [Serializable]
    public struct CabRequest : IEquatable<CabRequest>
    {
        public int ElevatorIndex;
        public int Floor;

        public CabRequest(int elevatorIndex, int floor)
        {
            ElevatorIndex = elevatorIndex;
            Floor = floor;
        }

        public bool Equals(CabRequest other)
        {
            return ElevatorIndex == other.ElevatorIndex && Floor == other.Floor;
        }

        public override bool Equals(object obj)
        {
            return obj is CabRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ElevatorIndex * 397) ^ Floor;
            }
        }
    }
}
