using System;
using UnityEngine;
using UnityEngine.UI;

namespace VinylShores.Dialogue
{
    /// <summary>
    /// Minimal screen-space dialogue UI built with legacy uGUI Text so it does
    /// not depend on imported fonts, TMP or imported sprite settings.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueUI : MonoBehaviour
    {
        private const string PanelName = "DialoguePanel";
        private const string SpeakerName = "SpeakerText";
        private const string BodyName = "BodyText";
        private const string HintName = "HintText";
        private const string PortraitName = "Portrait";
        private const string PromptName = "DialoguePrompt";
        private const string PromptTextName = "PromptText";

        private static Font defaultFont;

        [Header("Generated UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text hintText;
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Text promptText;

        private DialogueLine[] currentLines;
        private int currentIndex;
        private int openedFrame = -1;

        /// <summary>True while a dialogue is being displayed.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// True on the frame in which the current dialogue was opened. Used to
        /// stop a second nearby NPC from advancing it on the same key press.
        /// </summary>
        public bool OpenedThisFrame
        {
            get { return IsOpen && openedFrame == Time.frameCount; }
        }

        /// <summary>Raised once each time the dialogue is closed.</summary>
        public event Action Closed;

        private void Awake()
        {
            BuildUIIfNeeded();
        }

        /// <summary>
        /// Creates any missing uGUI objects and references. Safe to call from
        /// edit mode, which is what the scene builder does.
        /// </summary>
        public void BuildUIIfNeeded()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            if (panel == null)
            {
                panel = FindOrCreatePanel(canvas.transform);
            }

            if (speakerText == null)
            {
                speakerText = FindText(panel.transform, SpeakerName);
            }

            if (portraitImage == null)
            {
                portraitImage = FindImage(panel.transform, PortraitName);
            }

            if (bodyText == null)
            {
                bodyText = FindText(panel.transform, BodyName);
            }

            if (hintText == null)
            {
                hintText = FindText(panel.transform, HintName);
            }

            if (promptRoot == null)
            {
                promptRoot = FindOrCreatePrompt(canvas.transform);
            }

            if (promptText == null && promptRoot != null)
            {
                promptText = promptRoot.GetComponentInChildren<Text>(true);
            }

            if (promptText != null)
            {
                promptText.text = "Press E to talk";
            }

            if (!IsOpen && panel != null)
            {
                panel.SetActive(false);
            }

            if (promptRoot != null)
            {
                promptRoot.SetActive(false);
            }
        }

        /// <summary>Starts a new dialogue from the first line.</summary>
        public void Open(DialogueLine[] lines)
        {
            if (lines == null || lines.Length == 0)
            {
                Debug.LogWarning("DialogueUI.Open was called without any lines.", this);
                return;
            }

            BuildUIIfNeeded();

            currentLines = lines;
            currentIndex = 0;
            IsOpen = true;
            openedFrame = Time.frameCount;

            if (panel != null)
            {
                panel.SetActive(true);
            }

            if (promptRoot != null)
            {
                promptRoot.SetActive(false);
            }

            RenderCurrentLine();
        }

        /// <summary>Shows the next line, or closes the dialogue after the last one.</summary>
        public void Advance()
        {
            if (!IsOpen)
            {
                return;
            }

            if (currentLines == null || currentLines.Length == 0)
            {
                Close();
                return;
            }

            currentIndex++;
            if (currentIndex >= currentLines.Length)
            {
                Close();
                return;
            }

            RenderCurrentLine();
        }

        /// <summary>Closes the dialogue and raises <see cref="Closed"/>.</summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            currentLines = null;
            currentIndex = 0;
            openedFrame = -1;

            if (portraitImage != null)
            {
                portraitImage.sprite = null;
                portraitImage.enabled = false;
            }

            if (panel != null)
            {
                panel.SetActive(false);
            }

            Action handler = Closed;
            if (handler != null)
            {
                handler.Invoke();
            }
        }

        /// <summary>Shows or hides the "press E" prompt.</summary>
        public void ShowPrompt(bool visible)
        {
            if (promptRoot == null)
            {
                BuildUIIfNeeded();
            }

            if (promptRoot == null)
            {
                return;
            }

            promptRoot.SetActive(visible && !IsOpen);
        }

        private void RenderCurrentLine()
        {
            DialogueLine line = currentLines[currentIndex];

            if (portraitImage != null)
            {
                Sprite portrait = line != null ? line.portrait : null;
                portraitImage.sprite = portrait;
                portraitImage.enabled = portrait != null;
            }

            if (speakerText != null)
            {
                speakerText.text = line != null ? line.speaker : string.Empty;
            }

            if (bodyText != null)
            {
                bodyText.text = line != null ? line.text : string.Empty;
            }

            if (hintText != null)
            {
                hintText.text = currentIndex == currentLines.Length - 1 ? "[E] Close" : "[E] Continue";
            }
        }

        private GameObject FindOrCreatePanel(Transform canvasTransform)
        {
            Transform existing = canvasTransform.Find(PanelName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject panelObject = new GameObject(
                PanelName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panelObject.layer = UiLayer;
            panelObject.transform.SetParent(canvasTransform, false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.offsetMin = new Vector2(40f, 30f);
            panelRect.offsetMax = new Vector2(-40f, 210f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.03f, 0.04f, 0.06f, 0.92f);
            panelImage.raycastTarget = false;

            Image portrait = CreateImage(panelObject.transform, PortraitName);
            RectTransform portraitRect = portrait.rectTransform;
            portraitRect.anchorMin = new Vector2(0f, 1f);
            portraitRect.anchorMax = new Vector2(0f, 1f);
            portraitRect.pivot = new Vector2(0f, 1f);
            portraitRect.anchoredPosition = new Vector2(20f, -20f);
            portraitRect.sizeDelta = new Vector2(120f, 120f);
            portrait.preserveAspect = true;
            portrait.enabled = false;

            Text speaker = CreateText(panelObject.transform, SpeakerName, 24, TextAnchor.UpperLeft);
            speaker.fontStyle = FontStyle.Bold;
            speaker.color = new Color(0.98f, 0.83f, 0.45f, 1f);
            RectTransform speakerRect = speaker.rectTransform;
            speakerRect.anchorMin = new Vector2(0f, 1f);
            speakerRect.anchorMax = new Vector2(1f, 1f);
            speakerRect.pivot = new Vector2(0.5f, 1f);
            speakerRect.offsetMin = new Vector2(160f, -50f);
            speakerRect.offsetMax = new Vector2(-24f, -18f);

            Text body = CreateText(panelObject.transform, BodyName, 22, TextAnchor.UpperLeft);
            RectTransform bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(160f, 48f);
            bodyRect.offsetMax = new Vector2(-24f, -64f);

            Text hint = CreateText(panelObject.transform, HintName, 18, TextAnchor.LowerCenter);
            hint.color = new Color(0.72f, 0.78f, 0.85f, 1f);
            RectTransform hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 10f);
            hintRect.sizeDelta = new Vector2(500f, 28f);

            return panelObject;
        }

        private GameObject FindOrCreatePrompt(Transform canvasTransform)
        {
            Transform existing = canvasTransform.Find(PromptName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject promptObject = new GameObject(
                PromptName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            promptObject.layer = UiLayer;
            promptObject.transform.SetParent(canvasTransform, false);

            RectTransform promptRect = promptObject.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0f);
            promptRect.anchorMax = new Vector2(0.5f, 0f);
            promptRect.pivot = new Vector2(0.5f, 0f);
            promptRect.anchoredPosition = new Vector2(0f, 245f);
            promptRect.sizeDelta = new Vector2(240f, 44f);

            Image promptImage = promptObject.GetComponent<Image>();
            promptImage.color = new Color(0.03f, 0.04f, 0.06f, 0.85f);
            promptImage.raycastTarget = false;

            Text text = CreateText(promptObject.transform, PromptTextName, 20, TextAnchor.MiddleCenter);
            text.text = "Press E to talk";
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);

            return promptObject;
        }

        private static Text FindText(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static Image FindImage(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private static Image CreateImage(Transform parent, string name)
        {
            GameObject imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.layer = UiLayer;
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.layer = UiLayer;
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Font GetDefaultFont()
        {
            if (defaultFont != null)
            {
                return defaultFont;
            }

            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (defaultFont == null)
            {
                defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 24);
            }

            return defaultFont;
        }

        private static int UiLayer
        {
            get
            {
                int layer = LayerMask.NameToLayer("UI");
                return layer >= 0 ? layer : 5;
            }
        }
    }
}
