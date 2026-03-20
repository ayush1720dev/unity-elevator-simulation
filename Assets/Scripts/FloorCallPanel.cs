using UnityEngine;
using UnityEngine.UI;

namespace ElevatorSimulation
{
    public class FloorCallPanel : MonoBehaviour
    {
        public int FloorIndex { get; private set; }

        private Button upButton;
        private Button downButton;
        private Text statusText;
        private ElevatorDispatcher dispatcher;
        private Color normalColor;
        private Color activeColor;

        public void Initialize(
            int floorIndex,
            ElevatorDispatcher elevatorDispatcher,
            Button upCallButton,
            Button downCallButton,
            Text panelStatusText,
            Color idleButtonColor,
            Color activeButtonColor)
        {
            FloorIndex = floorIndex;
            dispatcher = elevatorDispatcher;
            upButton = upCallButton;
            downButton = downCallButton;
            statusText = panelStatusText;
            normalColor = idleButtonColor;
            activeColor = activeButtonColor;

            if (upButton != null)
            {
                upButton.onClick.RemoveAllListeners();
                upButton.onClick.AddListener(delegate { dispatcher.RequestHallCall(FloorIndex, TravelDirection.Up); });
                SetButtonState(upButton, false);
            }

            if (downButton != null)
            {
                downButton.onClick.RemoveAllListeners();
                downButton.onClick.AddListener(delegate { dispatcher.RequestHallCall(FloorIndex, TravelDirection.Down); });
                SetButtonState(downButton, false);
            }

            RefreshStatus();
        }

        public void SetRequestState(TravelDirection direction, bool active)
        {
            if (direction == TravelDirection.Up && upButton != null)
            {
                SetButtonState(upButton, active);
            }
            else if (direction == TravelDirection.Down && downButton != null)
            {
                SetButtonState(downButton, active);
            }

            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (statusText == null)
            {
                return;
            }

            bool upActive = upButton != null && !upButton.interactable;
            bool downActive = downButton != null && !downButton.interactable;

            if (upActive && downActive)
            {
                statusText.text = "Assigned: Up and Down";
            }
            else if (upActive)
            {
                statusText.text = "Assigned: Up";
            }
            else if (downActive)
            {
                statusText.text = "Assigned: Down";
            }
            else
            {
                statusText.text = "Assigned: None";
            }
        }

        private void SetButtonState(Button button, bool active)
        {
            button.interactable = !active;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? activeColor : normalColor;
            }
        }
    }
}
