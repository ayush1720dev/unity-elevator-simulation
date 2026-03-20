using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ElevatorSimulation
{
    public class ElevatorController : MonoBehaviour
    {
        public event Action<ElevatorController, IList<HallRequest>> HallRequestsServed;
        public event Action<ElevatorController> QueueChanged;

        public int ElevatorIndex { get; private set; }
        public int CurrentFloor { get; private set; }
        public ElevatorState State { get; private set; }
        public TravelDirection Direction { get; private set; }
        public int PendingStopCount { get { return pendingStops.Count; } }

        private readonly HashSet<int> pendingStops = new HashSet<int>();
        private readonly HashSet<int> pendingCabStops = new HashSet<int>();
        private readonly Dictionary<int, HashSet<TravelDirection>> assignedHallStops = new Dictionary<int, HashSet<TravelDirection>>();
        private readonly List<HallRequest> servedBuffer = new List<HallRequest>();
        private readonly List<int> sortBuffer = new List<int>();

        private IList<float> floorAnchors;
        private IList<string> floorLabels;
        private RectTransform carRect;
        private Text titleText;
        private Text currentFloorText;
        private Text directionText;
        private Text queueText;
        private Image carImage;
        private float moveSpeed;
        private float stopDelay;
        private int activeTargetFloor = -1;
        private float stopTimer;
        private bool isConfigured;

        public void Configure(
            int elevatorIndex,
            IList<string> labels,
            IList<float> anchors,
            RectTransform movingCar,
            Text elevatorTitle,
            Text floorDisplay,
            Text directionDisplay,
            Text queueDisplay,
            Image elevatorCarImage,
            float configuredMoveSpeed,
            float configuredStopDelay)
        {
            ElevatorIndex = elevatorIndex;
            floorLabels = labels;
            floorAnchors = anchors;
            carRect = movingCar;
            titleText = elevatorTitle;
            currentFloorText = floorDisplay;
            directionText = directionDisplay;
            queueText = queueDisplay;
            carImage = elevatorCarImage;
            moveSpeed = configuredMoveSpeed;
            stopDelay = configuredStopDelay;
            CurrentFloor = 0;
            State = ElevatorState.Idle;
            Direction = TravelDirection.None;
            activeTargetFloor = -1;
            stopTimer = 0f;
            isConfigured = true;

            if (carRect != null && floorAnchors != null && floorAnchors.Count > 0)
            {
                Vector2 anchoredPosition = carRect.anchoredPosition;
                anchoredPosition.y = floorAnchors[0];
                carRect.anchoredPosition = anchoredPosition;
            }

            RefreshUi();
        }

        public void AddCabRequest(int floor)
        {
            if (!IsValidFloor(floor))
            {
                return;
            }

            CabRequest request = new CabRequest(ElevatorIndex, floor);
            if (pendingCabStops.Contains(request.Floor))
            {
                return;
            }

            pendingCabStops.Add(request.Floor);
            pendingStops.Add(request.Floor);
            QueueChangedSafeInvoke();
            EvaluateQueue();
        }

        public void AssignHallRequest(HallRequest request)
        {
            if (!IsValidFloor(request.Floor))
            {
                return;
            }

            HashSet<TravelDirection> directionsForFloor;
            if (!assignedHallStops.TryGetValue(request.Floor, out directionsForFloor))
            {
                directionsForFloor = new HashSet<TravelDirection>();
                assignedHallStops[request.Floor] = directionsForFloor;
            }

            if (!directionsForFloor.Add(request.Direction))
            {
                return;
            }

            pendingStops.Add(request.Floor);
            QueueChangedSafeInvoke();
            EvaluateQueue();
        }

        public bool HasCabRequest(int floor)
        {
            return pendingCabStops.Contains(floor);
        }

        public bool IsIdle()
        {
            return State == ElevatorState.Idle && pendingStops.Count == 0 && activeTargetFloor < 0 && stopTimer <= 0f;
        }

        public bool CanServeHallCall(int floor, TravelDirection requestDirection)
        {
            if (!IsValidFloor(floor))
            {
                return false;
            }

            if (IsIdle())
            {
                return true;
            }

            if (Direction == TravelDirection.None || requestDirection == TravelDirection.None || Direction != requestDirection)
            {
                return false;
            }

            float currentTravelFloor = GetCurrentTravelFloor();
            if (Direction == TravelDirection.Up)
            {
                return floor >= Mathf.FloorToInt(currentTravelFloor - 0.01f);
            }

            return floor <= Mathf.CeilToInt(currentTravelFloor + 0.01f);
        }

        public int GetScoreForHallCall(int floor, TravelDirection requestDirection)
        {
            int distanceScore = Mathf.RoundToInt(Mathf.Abs(GetCurrentTravelFloor() - floor) * 100f);
            int loadScore = PendingStopCount * 10;

            if (IsIdle())
            {
                return distanceScore + loadScore + ElevatorIndex;
            }

            if (CanServeHallCall(floor, requestDirection))
            {
                return 1000 + distanceScore + loadScore + ElevatorIndex;
            }

            return 5000 + GetFallbackScore(floor);
        }

        public int GetFallbackScore(int floor)
        {
            int routeDistance;
            if (Direction == TravelDirection.Up)
            {
                int highestPending = GetExtremePendingFloor(true);
                routeDistance = floor >= CurrentFloor
                    ? floor - CurrentFloor
                    : (highestPending - CurrentFloor) + (highestPending - floor);
            }
            else if (Direction == TravelDirection.Down)
            {
                int lowestPending = GetExtremePendingFloor(false);
                routeDistance = floor <= CurrentFloor
                    ? CurrentFloor - floor
                    : (CurrentFloor - lowestPending) + (floor - lowestPending);
            }
            else
            {
                routeDistance = Mathf.Abs(CurrentFloor - floor);
            }

            return (routeDistance * 100) + (PendingStopCount * 10) + ElevatorIndex;
        }

        public string GetQueuePreview()
        {
            List<int> orderedStops = GetOrderedStops();
            if (orderedStops.Count == 0)
            {
                return "Queue: Empty";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("Queue: ");

            for (int index = 0; index < orderedStops.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(" -> ");
                }

                builder.Append(GetFloorLabel(orderedStops[index]));
            }

            return builder.ToString();
        }

        public void TickMovement(float deltaTime)
        {
            if (!isConfigured)
            {
                return;
            }

            if (stopTimer > 0f)
            {
                stopTimer -= deltaTime;
                if (stopTimer <= 0f)
                {
                    stopTimer = 0f;
                    SelectNextTarget();
                }

                RefreshUi();
                return;
            }

            if (activeTargetFloor < 0)
            {
                if (pendingStops.Count > 0)
                {
                    SelectNextTarget();
                }
                else
                {
                    State = ElevatorState.Idle;
                    Direction = TravelDirection.None;
                    RefreshUi();
                }

                return;
            }

            float targetY = floorAnchors[activeTargetFloor];
            Vector2 currentPosition = carRect.anchoredPosition;
            float nextY = Mathf.MoveTowards(currentPosition.y, targetY, moveSpeed * deltaTime);
            carRect.anchoredPosition = new Vector2(currentPosition.x, nextY);

            if (Mathf.Abs(nextY - targetY) <= 0.01f)
            {
                carRect.anchoredPosition = new Vector2(currentPosition.x, targetY);
                CurrentFloor = activeTargetFloor;
                activeTargetFloor = -1;
                BeginStopAtFloor(CurrentFloor);
            }
            else
            {
                RefreshUi();
            }
        }

        private void EvaluateQueue()
        {
            if (!isConfigured)
            {
                return;
            }

            if (State == ElevatorState.MovingUp || State == ElevatorState.MovingDown)
            {
                RefreshUi();
                return;
            }

            if (stopTimer > 0f)
            {
                RefreshUi();
                return;
            }

            if (pendingStops.Contains(CurrentFloor))
            {
                BeginStopAtFloor(CurrentFloor);
                return;
            }

            SelectNextTarget();
        }

        private void BeginStopAtFloor(int floor)
        {
            servedBuffer.Clear();

            HashSet<TravelDirection> assignedDirections;
            if (assignedHallStops.TryGetValue(floor, out assignedDirections))
            {
                foreach (TravelDirection direction in assignedDirections)
                {
                    servedBuffer.Add(new HallRequest(floor, direction));
                }

                assignedHallStops.Remove(floor);
            }

            pendingStops.Remove(floor);
            pendingCabStops.Remove(floor);

            State = ElevatorState.Stopped;
            stopTimer = stopDelay;
            CurrentFloor = floor;

            if (pendingStops.Count == 0)
            {
                Direction = TravelDirection.None;
            }

            QueueChangedSafeInvoke();
            if (servedBuffer.Count > 0 && HallRequestsServed != null)
            {
                HallRequestsServed(this, servedBuffer);
            }

            RefreshUi();
        }

        private void SelectNextTarget()
        {
            if (pendingStops.Count == 0)
            {
                activeTargetFloor = -1;
                State = ElevatorState.Idle;
                Direction = TravelDirection.None;
                RefreshUi();
                return;
            }

            int nextFloor;
            if (Direction == TravelDirection.Up)
            {
                nextFloor = FindNextAbove(CurrentFloor);
                if (nextFloor < 0)
                {
                    nextFloor = FindNextBelow(CurrentFloor);
                    Direction = nextFloor >= 0 ? TravelDirection.Down : TravelDirection.None;
                }
            }
            else if (Direction == TravelDirection.Down)
            {
                nextFloor = FindNextBelow(CurrentFloor);
                if (nextFloor < 0)
                {
                    nextFloor = FindNextAbove(CurrentFloor);
                    Direction = nextFloor >= 0 ? TravelDirection.Up : TravelDirection.None;
                }
            }
            else
            {
                nextFloor = FindClosestStop(CurrentFloor);
                if (nextFloor > CurrentFloor)
                {
                    Direction = TravelDirection.Up;
                }
                else if (nextFloor < CurrentFloor)
                {
                    Direction = TravelDirection.Down;
                }
                else
                {
                    Direction = TravelDirection.None;
                }
            }

            if (nextFloor < 0)
            {
                activeTargetFloor = -1;
                State = ElevatorState.Idle;
                Direction = TravelDirection.None;
                RefreshUi();
                return;
            }

            if (nextFloor == CurrentFloor)
            {
                BeginStopAtFloor(CurrentFloor);
                return;
            }

            activeTargetFloor = nextFloor;
            State = Direction == TravelDirection.Up ? ElevatorState.MovingUp : ElevatorState.MovingDown;
            RefreshUi();
        }

        private int FindClosestStop(int fromFloor)
        {
            int closestFloor = -1;
            int closestDistance = int.MaxValue;

            foreach (int floor in pendingStops)
            {
                int distance = Mathf.Abs(floor - fromFloor);
                if (distance < closestDistance || (distance == closestDistance && (closestFloor < 0 || floor < closestFloor)))
                {
                    closestDistance = distance;
                    closestFloor = floor;
                }
            }

            return closestFloor;
        }

        private int FindNextAbove(int fromFloor)
        {
            int nextFloor = int.MaxValue;

            foreach (int floor in pendingStops)
            {
                if (floor > fromFloor && floor < nextFloor)
                {
                    nextFloor = floor;
                }
            }

            return nextFloor == int.MaxValue ? -1 : nextFloor;
        }

        private int FindNextBelow(int fromFloor)
        {
            int nextFloor = int.MinValue;

            foreach (int floor in pendingStops)
            {
                if (floor < fromFloor && floor > nextFloor)
                {
                    nextFloor = floor;
                }
            }

            return nextFloor == int.MinValue ? -1 : nextFloor;
        }

        private int GetExtremePendingFloor(bool highest)
        {
            int result = CurrentFloor;
            foreach (int floor in pendingStops)
            {
                if (highest && floor > result)
                {
                    result = floor;
                }
                else if (!highest && floor < result)
                {
                    result = floor;
                }
            }

            return result;
        }

        private float GetCurrentTravelFloor()
        {
            if (carRect == null || floorAnchors == null || floorAnchors.Count <= 1)
            {
                return CurrentFloor;
            }

            float minAnchor = floorAnchors[0];
            float maxAnchor = floorAnchors[floorAnchors.Count - 1];
            float normalized = Mathf.InverseLerp(minAnchor, maxAnchor, carRect.anchoredPosition.y);
            return normalized * (floorAnchors.Count - 1);
        }

        private List<int> GetOrderedStops()
        {
            sortBuffer.Clear();
            foreach (int floor in pendingStops)
            {
                sortBuffer.Add(floor);
            }

            sortBuffer.Sort();

            List<int> orderedStops = new List<int>();
            if (Direction == TravelDirection.Down)
            {
                for (int index = sortBuffer.Count - 1; index >= 0; index--)
                {
                    int floor = sortBuffer[index];
                    if (floor < CurrentFloor)
                    {
                        orderedStops.Add(floor);
                    }
                }

                for (int index = 0; index < sortBuffer.Count; index++)
                {
                    int floor = sortBuffer[index];
                    if (floor >= CurrentFloor)
                    {
                        orderedStops.Add(floor);
                    }
                }
            }
            else
            {
                for (int index = 0; index < sortBuffer.Count; index++)
                {
                    int floor = sortBuffer[index];
                    if (floor > CurrentFloor)
                    {
                        orderedStops.Add(floor);
                    }
                }

                for (int index = sortBuffer.Count - 1; index >= 0; index--)
                {
                    int floor = sortBuffer[index];
                    if (floor <= CurrentFloor)
                    {
                        orderedStops.Add(floor);
                    }
                }
            }

            return orderedStops;
        }

        private string GetFloorLabel(int floor)
        {
            if (floorLabels == null || floor < 0 || floor >= floorLabels.Count)
            {
                return floor.ToString();
            }

            return floorLabels[floor];
        }

        private bool IsValidFloor(int floor)
        {
            return floorAnchors != null && floor >= 0 && floor < floorAnchors.Count;
        }

        private void QueueChangedSafeInvoke()
        {
            if (QueueChanged != null)
            {
                QueueChanged(this);
            }
        }

        private void RefreshUi()
        {
            if (!isConfigured)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = string.Format("Elevator {0}", ElevatorIndex + 1);
            }

            if (currentFloorText != null)
            {
                int displayedFloor = Mathf.Clamp(Mathf.RoundToInt(GetCurrentTravelFloor()), 0, floorLabels.Count - 1);
                currentFloorText.text = string.Format("Floor: {0}", GetFloorLabel(displayedFloor));
            }

            if (directionText != null)
            {
                directionText.text = string.Format("State: {0}", Direction.ToArrow());
            }

            if (queueText != null)
            {
                queueText.text = GetQueuePreview();
            }

            if (carImage != null)
            {
                if (State == ElevatorState.MovingUp || State == ElevatorState.MovingDown)
                {
                    carImage.color = new Color(0.94f, 0.61f, 0.23f, 1f);
                }
                else if (State == ElevatorState.Stopped)
                {
                    carImage.color = new Color(0.24f, 0.72f, 0.43f, 1f);
                }
                else
                {
                    carImage.color = new Color(0.16f, 0.49f, 0.86f, 1f);
                }
            }
        }
    }
}
