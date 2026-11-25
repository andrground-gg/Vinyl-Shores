using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using VinylShores.Rhythm;

namespace VinylShores.UI
{
    /// <summary>
    /// Reusable exploration pause menu. Escape toggles a full-screen panel with a
    /// Resume button and a short controls hint. While paused it disables the
    /// shared player controller, freezes the player body and disables the
    /// "Player" action map so NPC interaction and dialogue advancement cannot
    /// run behind the menu.
    ///
    /// Resume restores the exact state that was captured when pausing. In
    /// particular, if a dialogue had already locked movement, movement stays
    /// locked until the dialogue closes. The component never changes
    /// <see cref="Time.timeScale"/> and never touches audio or the rhythm
    /// challenge, which owns its own Escape/pause path.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExplorationPauseMenu : MonoBehaviour
    {
        private const string PanelObjectName = "PausePanel";
        private const string BoxObjectName = "PauseBox";
        private const string TitleObjectName = "PauseTitle";
        private const string HintObjectName = "ControlsHint";
        private const string ResumeObjectName = "ResumeButton";
        private const string ResumeLabelObjectName = "Label";
        private const int SortingOrder = 50;

        private static ExplorationPauseMenu instance;

        [Header("Scene references (auto-resolved when empty)")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private Rigidbody2D body;

        [Header("Generated UI references")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Text hintText;

        private InputActionMap playerMap;
        private bool isPaused;
        private bool movementWasEnabled;
        private bool bodyWasSimulated;
        private bool playerMapWasEnabled;

        public bool IsPaused => isPaused;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                // A second copy would create a duplicate panel and fight over the
                // same input map. Keep the first one only.
                Destroy(gameObject);
                return;
            }

            instance = this;
            EnsureCanvas();
            BuildUIIfNeeded();
            EnsureEventSystem();
            ResolveReferences();
            SetPanelVisible(false);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnDisable()
        {
            // Leaving Play mode, unloading the scene or deactivating the beach
            // for a rhythm challenge must not leave the project-wide input map
            // disabled or the player frozen.
            if (isPaused)
            {
                Resume();
            }
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            // The rhythm challenge handles its own Escape press. It also
            // deactivates the beach roots, but this guard keeps the boundary
            // explicit and safe if that changes.
            if (BeachRhythmSession.IsActive)
            {
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        private void LateUpdate()
        {
            if (!isPaused)
            {
                return;
            }

            // Re-assert the freeze in case another system re-enables the
            // controller or the body while the menu is open.
            if (movement != null && movement.enabled)
            {
                movement.enabled = false;
            }

            if (body != null && body.simulated)
            {
                body.simulated = false;
            }

            if (playerMap != null && playerMap.enabled)
            {
                playerMap.Disable();
            }
        }

        /// <summary>Opens or closes the menu.</summary>
        public void Toggle()
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        /// <summary>Freezes exploration input and shows the menu.</summary>
        public void Pause()
        {
            if (isPaused)
            {
                return;
            }

            ResolveReferences();

            movementWasEnabled = movement != null && movement.enabled;
            bodyWasSimulated = body != null && body.simulated;
            playerMapWasEnabled = playerMap != null && playerMap.enabled;

            if (movement != null)
            {
                movement.enabled = false;
            }

            if (body != null)
            {
                body.simulated = false;
            }

            if (playerMap != null)
            {
                playerMap.Disable();
            }

            isPaused = true;
            SetPanelVisible(true);
            SelectResumeButton();
        }

        /// <summary>Restores the state captured by <see cref="Pause"/>.</summary>
        public void Resume()
        {
            if (!isPaused)
            {
                return;
            }

            isPaused = false;
            SetPanelVisible(false);

            if (playerMap != null && playerMapWasEnabled)
            {
                playerMap.Enable();
            }

            // Restore exactly what was captured: if the dialogue had already
            // disabled movement, this keeps it disabled.
            if (movement != null)
            {
                movement.enabled = movementWasEnabled;
            }

            if (body != null)
            {
                body.simulated = bodyWasSimulated;
            }
        }

        private void ResolveReferences()
        {
            if (movement == null)
            {
                movement = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
            }

            if (body == null && movement != null)
            {
                body = movement.GetComponent<Rigidbody2D>();
            }

            if (playerMap == null)
            {
                InputActionAsset actions = InputSystem.actions;
                if (actions != null)
                {
                    playerMap = actions.FindActionMap("Player", false);
                }
            }
        }

        private void SelectResumeButton()
        {
            if (resumeButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
            }
        }

        private void SetPanelVisible(bool visible)
        {
            if (panel != null)
            {
                panel.SetActive(visible);
            }
        }

        private static void EnsureEventSystem()
        {
            EventSystem existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                InputSystemUIInputModule existingModule = existing.GetComponent<InputSystemUIInputModule>();
                if (existingModule != null && existingModule.actionsAsset == null)
                {
                    existingModule.AssignDefaultActions();
                }

                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule module = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            // Uses the Input System package's default UI actions (Point/Click),
            // so the Resume button works without touching the project actions.
            module.AssignDefaultActions();
        }

        /// <summary>
        /// Builds the panel hierarchy when it is missing. Also called by the
        /// editor prefab builder so the saved prefab contains the real UI.
        /// </summary>
        public void BuildUIIfNeeded()
        {
            EnsureCanvas();

            if (panel == null)
            {
                Transform existing = transform.Find(PanelObjectName);
                if (existing != null)
                {
                    panel = existing.gameObject;
                }
            }

            if (panel == null)
            {
                CreatePanel();
            }
            else
            {
                BindExistingPanel();
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(Resume);
            }

            SetPanelVisible(false);
        }

        private void EnsureCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void CreatePanel()
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0)
            {
                uiLayer = 5;
            }

            panel = new GameObject(PanelObjectName, typeof(RectTransform), typeof(Image));
            panel.layer = uiLayer;
            panel.transform.SetParent(transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;

            Image background = panel.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.65f);
            background.raycastTarget = true;

            GameObject box = new GameObject(BoxObjectName, typeof(RectTransform), typeof(Image));
            box.layer = uiLayer;
            box.transform.SetParent(panel.transform, false);

            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = new Vector2(560f, 320f);

            Image boxImage = box.GetComponent<Image>();
            boxImage.color = new Color(0.07f, 0.09f, 0.13f, 0.98f);
            boxImage.raycastTarget = true;

            Text title = CreateText(
                box.transform,
                TitleObjectName,
                "Paused",
                44,
                TextAnchor.MiddleCenter,
                Color.white);
            title.fontStyle = FontStyle.Bold;
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -28f);
            titleRect.sizeDelta = new Vector2(0f, 60f);

            hintText = CreateText(
                box.transform,
                HintObjectName,
                "WASD / Arrows   Move\n" +
                "E   Interact\n" +
                "Esc   Pause / Resume\n" +
                "Click Resume to continue",
                22,
                TextAnchor.UpperCenter,
                new Color(0.82f, 0.87f, 0.93f, 1f));
            RectTransform hintRect = hintText.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 1f);
            hintRect.anchorMax = new Vector2(1f, 1f);
            hintRect.pivot = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, -96f);
            hintRect.sizeDelta = new Vector2(-80f, 120f);

            CreateResumeButton(box.transform, uiLayer);
        }

        private void BindExistingPanel()
        {
            if (resumeButton == null)
            {
                Transform button = panel.transform.Find(BoxObjectName + "/" + ResumeObjectName);
                if (button != null)
                {
                    resumeButton = button.GetComponent<Button>();
                }
            }

            if (hintText == null)
            {
                Transform hint = panel.transform.Find(BoxObjectName + "/" + HintObjectName);
                if (hint != null)
                {
                    hintText = hint.GetComponent<Text>();
                }
            }
        }

        private void CreateResumeButton(Transform parent, int uiLayer)
        {
            GameObject buttonObject = new GameObject(
                ResumeObjectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.layer = uiLayer;
            buttonObject.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 36f);
            buttonRect.sizeDelta = new Vector2(240f, 60f);

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.18f, 0.42f, 0.75f, 1f);

            resumeButton = buttonObject.GetComponent<Button>();
            resumeButton.targetGraphic = buttonImage;

            ColorBlock colors = resumeButton.colors;
            colors.normalColor = new Color(0.18f, 0.42f, 0.75f, 1f);
            colors.highlightedColor = new Color(0.25f, 0.52f, 0.88f, 1f);
            colors.pressedColor = new Color(0.12f, 0.30f, 0.58f, 1f);
            colors.selectedColor = colors.highlightedColor;
            resumeButton.colors = colors;

            Text label = CreateText(
                buttonObject.transform,
                ResumeLabelObjectName,
                "Resume",
                26,
                TextAnchor.MiddleCenter,
                Color.white);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private static Text CreateText(
            Transform parent,
            string objectName,
            string content,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            }

            return font;
        }
    }
}
