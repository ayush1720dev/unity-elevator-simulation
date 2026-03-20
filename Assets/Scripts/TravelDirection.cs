namespace ElevatorSimulation
{
    public enum TravelDirection
    {
        None = 0,
        Up = 1,
        Down = -1
    }

    public static class TravelDirectionExtensions
    {
        public static string ToArrow(this TravelDirection direction)
        {
            switch (direction)
            {
                case TravelDirection.Up:
                    return "Up";
                case TravelDirection.Down:
                    return "Down";
                default:
                    return "Idle";
            }
        }
    }
}
