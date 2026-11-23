using System;
using TMPro;
using UnityEngine;

namespace VinylShores.Rhythm
{
    public sealed class RhythmResultView : MonoBehaviour
    {
        [SerializeField] private RectTransform card;
        [SerializeField] private RectTransform title;
        [SerializeField] private RectTransform message;
        [SerializeField] private RectTransform restartButton;
        [SerializeField] private GameObject details;
        [SerializeField] private TMP_Text score;
        [SerializeField] private TMP_Text percentageText;
        [SerializeField] private TMP_Text[] statisticValues;
        [SerializeField] private TMP_Text bestComboValue;
        [SerializeField] private GameObject headerFrame;
        [SerializeField] private RectTransform[] starRoots;
        [SerializeField] private RectTransform[] fillMasks;
        private UnityEngine.UI.Button returnButton;
        private UnityEngine.UI.Button resumeButton;
        private UnityEngine.UI.Button pauseExitButton;
        private bool animating;
        private double startedDsp;
        private int points;
        private int maximum;
        private double percentage;
        public int EarnedStars { get; private set; }
        public bool IsConfigured => card != null && title != null && message != null && restartButton != null &&
            details != null && score != null && starRoots != null && starRoots.Length == 3 &&
            fillMasks != null && fillMasks.Length == 3 && Array.TrueForAll(starRoots, r => r != null) &&
            Array.TrueForAll(fillMasks, r => r != null);

        public static int RoundedPercentage(double percent) => (int)Math.Clamp(
            Math.Round(percent, MidpointRounding.AwayFromZero), 0, 100);
        public static int StarsForPercentage(double percent)
        {
            return percent >= 90 ? 3 : percent >= 70 ? 2 : percent >= 50 ? 1 : 0;
        }

        public void ResetView()
        {
            animating = false;
            if (returnButton != null) returnButton.gameObject.SetActive(false);
            if (resumeButton != null) resumeButton.gameObject.SetActive(false);
            if (pauseExitButton != null) pauseExitButton.gameObject.SetActive(false);
            EarnedStars = 0;
            if (!IsConfigured) return;
            details.SetActive(false);
            if (headerFrame != null) headerFrame.SetActive(false);
            message.gameObject.SetActive(true);
            title.GetComponent<TMP_Text>().fontSize = 48;
            title.sizeDelta = new Vector2(650, 75);
            restartButton.sizeDelta = new Vector2(240, 54);
            restartButton.GetComponentInChildren<TMP_Text>(true).fontSize = 32;
            card.sizeDelta = new Vector2(700, 320);
            title.anchoredPosition = new Vector2(0, 100);
            message.anchoredPosition = Vector2.zero;
            message.sizeDelta = new Vector2(650, 110);
            restartButton.anchoredPosition = new Vector2(0, -105);
            for (int i = 0; i < 3; i++) SetStar(i, 0);
            score.text = "";
        }

        public void ShowPause(Action resume, Action exit)
        {
            if (resumeButton == null)
                resumeButton = CreateButton("ResumeButton", resume);
            if (exit != null && pauseExitButton == null)
                pauseExitButton = CreateButton("PauseReturnToBeachButton", exit);

            card.sizeDelta = new Vector2(460, exit != null ? 330 : 270);
            title.sizeDelta = new Vector2(420, 60);
            title.anchoredPosition = new Vector2(0, exit != null ? 112 : 82);
            var heading = title.GetComponent<TMP_Text>();
            heading.text = "Paused";
            heading.fontSize = 52;
            message.gameObject.SetActive(false);

            float firstRow = exit != null ? 40 : 10;
            LayoutPauseButton(resumeButton, "Resume", firstRow);
            LayoutPauseButton(restartButton.GetComponent<UnityEngine.UI.Button>(), "Restart", firstRow - 68);
            if (exit != null) LayoutPauseButton(pauseExitButton, "Return to beach", firstRow - 136);
        }

        public void SelectResumeButton() => resumeButton.Select();

        private static void LayoutPauseButton(UnityEngine.UI.Button button, string label, float y)
        {
            var rect = (RectTransform)button.transform;
            rect.sizeDelta = new Vector2(330, 54);
            rect.anchoredPosition = new Vector2(0, y);
            var text = button.GetComponentInChildren<TMP_Text>(true);
            text.text = label;
            text.fontSize = 32;
            button.interactable = true;
            button.gameObject.SetActive(true);
        }

        private UnityEngine.UI.Button CreateButton(string objectName, Action action)
        {
            var copy = Instantiate(restartButton.gameObject, restartButton.parent);
            copy.name = objectName;
            var button = copy.GetComponent<UnityEngine.UI.Button>();
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            button.onClick.AddListener(() => action());
            return button;
        }

        public void Show(RhythmScoreSession session)
        {
            points = session.Score;
            maximum = session.MaximumScore;
            percentage = session.ScorePercentage;
            EarnedStars = StarsForPercentage(percentage);
            startedDsp = AudioSettings.dspTime;
            animating = true;
            details.SetActive(true);
            if (headerFrame != null) headerFrame.SetActive(true);
            card.sizeDelta = new Vector2(1100, 560);
            title.anchoredPosition = new Vector2(0, 230);
            title.sizeDelta = new Vector2(1000, 130);
            title.GetComponent<TMP_Text>().fontSize = 96;
            message.gameObject.SetActive(false);
            restartButton.anchoredPosition = new Vector2(255, -218);
            restartButton.sizeDelta = new Vector2(470, 58);
            var buttonLabel = restartButton.GetComponentInChildren<TMP_Text>(true);
            buttonLabel.fontSize = 56;
            buttonLabel.text = "Restart";
            restartButton.GetComponent<UnityEngine.UI.Button>().interactable = true;
            if (statisticValues != null && statisticValues.Length == 5)
            {
                int[] values = { session.Perfects, session.Goods, session.Oks, session.Misses, session.ExtraPresses };
                for (int i = 0; i < 5; i++) statisticValues[i].text = values[i].ToString();
            }
            if (bestComboValue != null) bestComboValue.text = session.BestCombo.ToString();
            ShowReturnButton(true);
            UpdateResult();
        }

        public void ShowReturnButton(bool resultLayout)
        {
            if (!BeachRhythmSession.IsActive || restartButton == null) return;
            if (returnButton == null)
                returnButton = CreateButton("ReturnToBeachButton", BeachRhythmSession.ReturnToBeach);
            var rect = (RectTransform)returnButton.transform;
            rect.sizeDelta = resultLayout ? new Vector2(470, 58) : new Vector2(240, 54);
            rect.anchoredPosition = resultLayout ? new Vector2(-255, -218) : new Vector2(-140, -105);
            if (!resultLayout) restartButton.anchoredPosition = new Vector2(140, -105);
            var label = returnButton.GetComponentInChildren<TMP_Text>(true);
            label.text = "Return to beach";
            label.fontSize = resultLayout ? 42 : 24;
            returnButton.interactable = true;
            returnButton.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (animating) UpdateResult();
        }

        private void UpdateResult()
        {
            double elapsed = AudioSettings.dspTime - startedDsp;
            float p = Mathf.Clamp01((float)(elapsed / 1.2));
            int shownPercentage = (int)Math.Floor(Math.Clamp(percentage * p, 0, 100));
            if (percentageText != null) percentageText.text = shownPercentage + "%";
            score.text = Math.Round(points * p).ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ") +
                " / " + maximum.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");
            double previousEnd = 0;
            for (int i = 0; i < 3; i++)
            {
                if (i >= EarnedStars)
                {
                    SetStar(i, 0);
                    continue;
                }
                double threshold = i == 0 ? 50 : i == 1 ? 70 : 90;
                double start = Math.Max(previousEnd, 1.2 * threshold / percentage);
                previousEnd = start + 0.25;
                float fill = Mathf.Clamp01((float)((elapsed - start) / 0.25));
                SetStar(i, fill);
            }
            if (elapsed >= Math.Max(1.2, previousEnd)) animating = false;
        }

        private void SetStar(int index, float fill)
        {
            fillMasks[index].SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100 * fill);
            starRoots[index].localScale = Vector3.one * (1.25f * (1 + Mathf.Sin(fill * Mathf.PI) * 0.18f));
        }
    }
}
