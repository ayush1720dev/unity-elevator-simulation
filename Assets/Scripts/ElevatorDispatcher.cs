using System.Collections.Generic;
using UnityEngine;

namespace ElevatorSimulation
{
    public class ElevatorDispatcher : MonoBehaviour
    {
        private readonly Dictionary<HallRequest, ElevatorController> claimedRequests = new Dictionary<HallRequest, ElevatorController>();
        private readonly Dictionary<int, FloorCallPanel> floorPanels = new Dictionary<int, FloorCallPanel>();
        private readonly List<ElevatorController> elevators = new List<ElevatorController>();

        public void Initialize(IList<ElevatorController> elevatorControllers, IList<FloorCallPanel> panels)
        {
            claimedRequests.Clear();
            floorPanels.Clear();
            elevators.Clear();

            for (int index = 0; index < elevatorControllers.Count; index++)
            {
                ElevatorController elevator = elevatorControllers[index];
                elevators.Add(elevator);
                elevator.HallRequestsServed -= HandleHallRequestsServed;
                elevator.HallRequestsServed += HandleHallRequestsServed;
            }

            for (int index = 0; index < panels.Count; index++)
            {
                FloorCallPanel panel = panels[index];
                floorPanels[panel.FloorIndex] = panel;
            }
        }

        public void RequestHallCall(int floor, TravelDirection direction)
        {
            HallRequest request = new HallRequest(floor, direction);
            if (claimedRequests.ContainsKey(request))
            {
                return;
            }

            ElevatorController selectedElevator = SelectElevator(request);
            if (selectedElevator == null)
            {
                return;
            }

            claimedRequests[request] = selectedElevator;
            selectedElevator.AssignHallRequest(request);

            FloorCallPanel panel;
            if (floorPanels.TryGetValue(floor, out panel))
            {
                panel.SetRequestState(direction, true);
            }
        }

        private ElevatorController SelectElevator(HallRequest request)
        {
            ElevatorController bestIdle = null;
            int bestIdleScore = int.MaxValue;

            ElevatorController bestDirectional = null;
            int bestDirectionalScore = int.MaxValue;

            ElevatorController bestFallback = null;
            int bestFallbackScore = int.MaxValue;

            for (int index = 0; index < elevators.Count; index++)
            {
                ElevatorController elevator = elevators[index];
                if (elevator.IsIdle())
                {
                    int score = elevator.GetScoreForHallCall(request.Floor, request.Direction);
                    if (score < bestIdleScore)
                    {
                        bestIdleScore = score;
                        bestIdle = elevator;
                    }

                    continue;
                }

                if (elevator.CanServeHallCall(request.Floor, request.Direction))
                {
                    int score = elevator.GetScoreForHallCall(request.Floor, request.Direction);
                    if (score < bestDirectionalScore)
                    {
                        bestDirectionalScore = score;
                        bestDirectional = elevator;
                    }
                }

                int fallbackScore = elevator.GetFallbackScore(request.Floor);
                if (fallbackScore < bestFallbackScore)
                {
                    bestFallbackScore = fallbackScore;
                    bestFallback = elevator;
                }
            }

            if (bestIdle != null)
            {
                return bestIdle;
            }

            if (bestDirectional != null)
            {
                return bestDirectional;
            }

            return bestFallback;
        }

        private void HandleHallRequestsServed(ElevatorController elevator, IList<HallRequest> servedRequests)
        {
            for (int index = 0; index < servedRequests.Count; index++)
            {
                HallRequest request = servedRequests[index];
                ElevatorController claimedElevator;
                if (claimedRequests.TryGetValue(request, out claimedElevator) && claimedElevator == elevator)
                {
                    claimedRequests.Remove(request);

                    FloorCallPanel panel;
                    if (floorPanels.TryGetValue(request.Floor, out panel))
                    {
                        panel.SetRequestState(request.Direction, false);
                    }
                }
            }
        }
    }
}
