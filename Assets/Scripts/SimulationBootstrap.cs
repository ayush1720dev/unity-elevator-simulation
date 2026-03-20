using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElevatorSimulation
{
    public class SimulationBootstrap : MonoBehaviour
    {
        private const int ElevatorCount = 3;
        private static readonly string[] FloorLabels = { "G", "1", "2", "3" };
        private static SimulationBootstrap instance;

        private readonly List<ElevatorController> elevators = new List<ElevatorController>();
        private readonly List<FloorCallPanel> floorPanels = new List<FloorCallPanel>();
        private readonly List<ShaftRuntime> shafts = new List<ShaftRuntime>();

        private ElevatorDispatcher dispatcher;
        private Font defaultFont;
        private bool hasBuiltUi;

        private readonly Color backgroundColor = new Color(0.09f, 0.12f, 0.17f, 1f);
        private readonly Color cardColor = new Color(0.12f, 0.17f, 0.24f, 0.96f);
        private readonly Color panelColor = new Color(0.17f, 0.24f, 0.32f, 1f);
        private readonly Color buttonColor = new Color(0.25f, 0.43f, 0.66f, 1f);
        private readonly Color activeButtonColor = new Color(0.93f, 0.58f, 0.23f, 1f);
        private readonly Color textPrimaryColor = new Color(0.95f, 0.97f, 1f, 1f);
        private readonly Color textSecondaryColor = new Color(0.68f, 0.76f, 0.85f, 1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBootstrap()
        {
            if (FindObjectOfType<SimulationBootstrap>() != null)
            {
                return;
            }

            GameObject bootstrapObject = new GameObject("SimulationBootstrap");
            bootstrapObject.AddComponent<SimulationBootstrap>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            dispatcher = gameObject.AddComponent<ElevatorDispatcher>();
            defaultFont = ResolveFont();
            BuildUi();
        }

        private void Start()
        {
            StartCoroutine(FinalizeUiAfterLayout());
        }

        private void Update()
        {
            for (int index = 0; index < elevators.Count; index++)
            {
                elevators[index].TickMovement(Time.deltaTime);
            }
        }

        private IEnumerator FinalizeUiAfterLayout()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();

            for (int index = 0; index < shafts.Count; index++)
            {
                ConfigureShaft(shafts[index], index);
            }

            dispatcher.Initialize(elevators, floorPanels);
        }

        private void BuildUi()
        {
            if (hasBuiltUi)
            {
                return;
            }

            hasBuiltUi = true;
            EnsureEventSystem();

            Canvas canvas = CreateCanvas("ElevatorCanvas");
            RectTransform mainPanel = CreatePanel("MainPanel", canvas.transform, backgroundColor);
            StretchToParent(mainPanel, 28f);

            HorizontalLayoutGroup layout = mainPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 24f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            RectTransform buildingCard = CreateCard("BuildingCard", mainPanel, cardColor, 2f);
            RectTransform hallCard = CreateCard("HallCard", mainPanel, cardColor, 1f);

            BuildBuildingPanel(buildingCard);
            BuildHallPanel(hallCard);
        }

        private void BuildBuildingPanel(RectTransform root)
        {
            VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateText("Title", root, "Elevator System Dispatch Simulator", 28, FontStyle.Bold, textPrimaryColor);
            CreateText("Subtitle", root, "Three elevators, four floors, realistic dispatching, and independent queues.", 15, FontStyle.Normal, textSecondaryColor);

            RectTransform shaftRow = CreateTransparentLayoutNode("ShaftRow", root);
            HorizontalLayoutGroup shaftLayout = shaftRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            shaftLayout.spacing = 18f;
            shaftLayout.childControlWidth = true;
            shaftLayout.childControlHeight = true;
            shaftLayout.childForceExpandWidth = true;
            shaftLayout.childForceExpandHeight = true;

            LayoutElement shaftRowElement = shaftRow.gameObject.AddComponent<LayoutElement>();
            shaftRowElement.flexibleHeight = 1f;
            shaftRowElement.preferredHeight = 720f;

            for (int index = 0; index < ElevatorCount; index++)
            {
                shafts.Add(CreateShaftCard(shaftRow, index));
            }
        }

        private void BuildHallPanel(RectTransform root)
        {
            VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateText("Header", root, "Hall Calls", 24, FontStyle.Bold, textPrimaryColor);
            CreateText("Description", root, "Request an elevator from any floor. Active hall calls stay highlighted until the assigned elevator serves them.", 15, FontStyle.Normal, textSecondaryColor);

            for (int floor = FloorLabels.Length - 1; floor >= 0; floor--)
            {
                RectTransform floorCard = CreatePanel("Floor_" + floor, root, panelColor);
                LayoutElement floorElement = floorCard.gameObject.AddComponent<LayoutElement>();
                floorElement.preferredHeight = 110f;

                HorizontalLayoutGroup cardLayout = floorCard.gameObject.AddComponent<HorizontalLayoutGroup>();
                cardLayout.padding = new RectOffset(14, 14, 12, 12);
                cardLayout.spacing = 10f;
                cardLayout.childControlWidth = true;
                cardLayout.childControlHeight = true;
                cardLayout.childForceExpandWidth = false;
                cardLayout.childForceExpandHeight = false;

                RectTransform labelColumn = CreateTransparentLayoutNode("LabelColumn", floorCard);
                VerticalLayoutGroup labelLayout = labelColumn.gameObject.AddComponent<VerticalLayoutGroup>();
                labelLayout.spacing = 4f;
                labelLayout.childControlWidth = true;
                labelLayout.childControlHeight = false;
                LayoutElement labelElement = labelColumn.gameObject.AddComponent<LayoutElement>();
                labelElement.flexibleWidth = 1f;
                labelElement.minWidth = 120f;

                CreateText("FloorLabel", labelColumn, "Floor " + FloorLabels[floor], 20, FontStyle.Bold, textPrimaryColor);
                Text statusText = CreateText("StatusLabel", labelColumn, "Assigned: None", 14, FontStyle.Normal, textSecondaryColor);

                RectTransform buttonRow = CreateTransparentLayoutNode("Buttons", floorCard);
                HorizontalLayoutGroup buttonLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
                buttonLayout.spacing = 10f;
                buttonLayout.childControlWidth = false;
                buttonLayout.childControlHeight = false;
                buttonLayout.childForceExpandWidth = false;
                buttonLayout.childForceExpandHeight = false;

                Button upButton = null;
                Button downButton = null;

                if (floor < FloorLabels.Length - 1)
                {
                    upButton = CreateButton("Up", buttonRow, "Up", buttonColor, textPrimaryColor, new Vector2(86f, 44f));
                }

                if (floor > 0)
                {
                    downButton = CreateButton("Down", buttonRow, "Down", buttonColor, textPrimaryColor, new Vector2(86f, 44f));
                }

                FloorCallPanel panel = floorCard.gameObject.AddComponent<FloorCallPanel>();
                panel.Initialize(floor, dispatcher, upButton, downButton, statusText, buttonColor, activeButtonColor);
                floorPanels.Add(panel);
            }
        }

        private ShaftRuntime CreateShaftCard(RectTransform parent, int elevatorIndex)
        {
            RectTransform card = CreatePanel("ElevatorCard_" + elevatorIndex, parent, panelColor);
            LayoutElement element = card.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
            element.preferredWidth = 260f;

            VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text title = CreateText("Title", card, "Elevator " + (elevatorIndex + 1), 20, FontStyle.Bold, textPrimaryColor);
            Text floorText = CreateText("Floor", card, "Floor: G", 14, FontStyle.Bold, textSecondaryColor);
            Text directionText = CreateText("Direction", card, "State: Idle", 14, FontStyle.Normal, textSecondaryColor);
            Text queueText = CreateText("Queue", card, "Queue: Empty", 13, FontStyle.Normal, textSecondaryColor);

            RectTransform shaftBody = CreatePanel("ShaftBody", card, new Color(0.1f, 0.14f, 0.2f, 1f));
            LayoutElement shaftElement = shaftBody.gameObject.AddComponent<LayoutElement>();
            shaftElement.preferredHeight = 420f;
            shaftElement.flexibleHeight = 1f;

            RectTransform car = CreatePanel("Car", shaftBody, new Color(0.16f, 0.49f, 0.86f, 1f));
            car.anchorMin = new Vector2(0.5f, 0f);
            car.anchorMax = new Vector2(0.5f, 0f);
            car.pivot = new Vector2(0.5f, 0.5f);
            car.sizeDelta = new Vector2(112f, 68f);

            RectTransform cabContainer = CreatePanel("CabPanel", card, new Color(0.14f, 0.19f, 0.27f, 1f));
            LayoutElement cabElement = cabContainer.gameObject.AddComponent<LayoutElement>();
            cabElement.preferredHeight = 144f;

            VerticalLayoutGroup cabLayout = cabContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            cabLayout.padding = new RectOffset(12, 12, 12, 12);
            cabLayout.spacing = 8f;
            cabLayout.childControlWidth = true;
            cabLayout.childControlHeight = false;
            cabLayout.childForceExpandWidth = true;
            cabLayout.childForceExpandHeight = false;

            CreateText("CabTitle", cabContainer, "Cab Requests", 14, FontStyle.Bold, textPrimaryColor);

            RectTransform gridRoot = CreateTransparentLayoutNode("CabGrid", cabContainer);
            GridLayoutGroup grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(90f, 36f);
            grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            LayoutElement gridElement = gridRoot.gameObject.AddComponent<LayoutElement>();
            gridElement.preferredHeight = 84f;

            ElevatorController controller = card.gameObject.AddComponent<ElevatorController>();
            ElevatorCabPanel cabPanel = cabContainer.gameObject.AddComponent<ElevatorCabPanel>();
            elevators.Add(controller);

            for (int floor = FloorLabels.Length - 1; floor >= 0; floor--)
            {
                int buttonFloor = floor;
                Button button = CreateButton("CabButton_" + floor, gridRoot, FloorLabels[floor], buttonColor, textPrimaryColor, Vector2.zero);
                button.onClick.AddListener(delegate { controller.AddCabRequest(buttonFloor); });
                cabPanel.RegisterButton(buttonFloor, button);
            }

            return new ShaftRuntime
            {
                Controller = controller,
                CabPanel = cabPanel,
                CarRect = car,
                ShaftBody = shaftBody,
                TitleText = title,
                FloorText = floorText,
                DirectionText = directionText,
                QueueText = queueText,
                CarImage = car.GetComponent<Image>()
            };
        }

        private void ConfigureShaft(ShaftRuntime shaft, int elevatorIndex)
        {
            float bodyHeight = shaft.ShaftBody.rect.height;
            float minY = 42f;
            float maxY = Mathf.Max(minY, bodyHeight - 42f);
            float step = (maxY - minY) / (FloorLabels.Length - 1);

            List<float> anchors = new List<float>();
            for (int floor = 0; floor < FloorLabels.Length; floor++)
            {
                float y = minY + (step * floor);
                anchors.Add(y);

                RectTransform line = CreatePanel("FloorLine_" + floor, shaft.ShaftBody, new Color(0.3f, 0.37f, 0.47f, 0.7f));
                line.anchorMin = new Vector2(0f, 0f);
                line.anchorMax = new Vector2(1f, 0f);
                line.pivot = new Vector2(0.5f, 0.5f);
                line.sizeDelta = new Vector2(-32f, 2f);
                line.anchoredPosition = new Vector2(0f, y - 22f);

                Text label = CreateText("FloorMarker_" + floor, shaft.ShaftBody, FloorLabels[floor], 14, FontStyle.Bold, textSecondaryColor);
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(0f, 0f);
                labelRect.pivot = new Vector2(0f, 0.5f);
                labelRect.sizeDelta = new Vector2(24f, 18f);
                labelRect.anchoredPosition = new Vector2(10f, y - 2f);
            }

            shaft.CarRect.SetAsLastSibling();

            shaft.Controller.Configure(
                elevatorIndex,
                FloorLabels,
                anchors,
                shaft.CarRect,
                shaft.TitleText,
                shaft.FloorText,
                shaft.DirectionText,
                shaft.QueueText,
                shaft.CarImage,
                120f,
                1f);

            shaft.CabPanel.Initialize(shaft.Controller, buttonColor, activeButtonColor);
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private Canvas CreateCanvas(string name)
        {
            GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private RectTransform CreateCard(string name, RectTransform parent, Color color, float flexibleWidth)
        {
            RectTransform rect = CreatePanel(name, parent, color);
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = flexibleWidth;
            layout.flexibleHeight = 1f;
            layout.minHeight = 680f;
            return rect;
        }

        private RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            Image image = panel.GetComponent<Image>();
            image.color = color;
            return panel.GetComponent<RectTransform>();
        }

        private RectTransform CreateTransparentLayoutNode(string name, Transform parent)
        {
            GameObject node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            return node.GetComponent<RectTransform>();
        }

        private Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle fontStyle, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.font = defaultFont;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            LayoutElement layout = textObject.AddComponent<LayoutElement>();
            layout.minHeight = fontSize + 10f;
            return text;
        }

        private Button CreateButton(string name, Transform parent, string label, Color background, Color textColor, Vector2 size)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = background;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            button.colors = colors;

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            if (size != Vector2.zero)
            {
                rect.sizeDelta = size;
                LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
                layout.preferredWidth = size.x;
                layout.preferredHeight = size.y;
            }

            Text buttonText = CreateText("Label", buttonObject.transform, label, 14, FontStyle.Bold, textColor);
            buttonText.alignment = TextAnchor.MiddleCenter;
            StretchToParent(buttonText.rectTransform, 0f);
            return button;
        }

        private void StretchToParent(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private Font ResolveFont()
        {
            Font builtInFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtInFont != null)
            {
                return builtInFont;
            }

            builtInFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (builtInFont != null)
            {
                return builtInFont;
            }

            return Font.CreateDynamicFontFromOSFont("Arial", 16);
        }

        private struct ShaftRuntime
        {
            public ElevatorController Controller;
            public ElevatorCabPanel CabPanel;
            public RectTransform CarRect;
            public RectTransform ShaftBody;
            public Text TitleText;
            public Text FloorText;
            public Text DirectionText;
            public Text QueueText;
            public Image CarImage;
        }
    }
}
