using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ElevatorSimulation
{
    public class ElevatorCabPanel : MonoBehaviour
    {
        private readonly Dictionary<int, Button> floorButtons = new Dictionary<int, Button>();

        private ElevatorController elevator;
        private Color idleColor;
        private Color activeColor;

        public void Initialize(ElevatorController controller, Color defaultButtonColor, Color selectedButtonColor)
        {
            if (elevator != null)
            {
                elevator.QueueChanged -= HandleQueueChanged;
            }

            elevator = controller;
            idleColor = defaultButtonColor;
            activeColor = selectedButtonColor;

            elevator.QueueChanged += HandleQueueChanged;
            RefreshButtons();
        }

        public void RegisterButton(int floor, Button button)
        {
            floorButtons[floor] = button;
        }

        private void HandleQueueChanged(ElevatorController controller)
        {
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            foreach (KeyValuePair<int, Button> pair in floorButtons)
            {
                bool active = elevator != null && elevator.HasCabRequest(pair.Key);
                pair.Value.interactable = !active;

                Image image = pair.Value.GetComponent<Image>();
                if (image != null)
                {
                    image.color = active ? activeColor : idleColor;
                }
            }
        }
    }
}
