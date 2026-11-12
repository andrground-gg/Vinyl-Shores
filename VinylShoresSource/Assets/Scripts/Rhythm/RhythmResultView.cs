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
        [SerializeField] private RectTransform[] starRoots;
        [SerializeField] private RectTransform[] fillMasks;
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

        public static int StarsForPercentage(double percent) => percent >= 90 ? 3 : percent >= 70 ? 2 : percent >= 50 ? 1 : 0;

        public void ResetView()
        {
            animating = false;
            EarnedStars = 0;
            if (!IsConfigured) return;
            details.SetActive(false);
            card.sizeDelta = new Vector2(700, 320);
            title.anchoredPosition = new Vector2(0, 100);
            message.anchoredPosition = Vector2.zero;
            message.sizeDelta = new Vector2(650, 110);
            restartButton.anchoredPosition = new Vector2(0, -105);
            for (int i = 0; i < 3; i++) SetStar(i, 0);
            score.text = "";
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
            card.sizeDelta = new Vector2(700, 580);
            title.anchoredPosition = new Vector2(0, 230);
            message.anchoredPosition = new Vector2(0, -95);
            message.sizeDelta = new Vector2(650, 110);
            restartButton.anchoredPosition = new Vector2(0, -235);
            UpdateResult();
        }

        private void Update()
        {
            if (animating) UpdateResult();
        }

        private void UpdateResult()
        {
            double elapsed = AudioSettings.dspTime - startedDsp;
            float p = Mathf.Clamp01((float)(elapsed / 1.2));
            double shownPercentage = Math.Floor(percentage * p * 100 + 0.000000001) / 100;
            score.text = $"<size=38>{shownPercentage:F2}%</size>\n{Mathf.RoundToInt(points * p):N0} / {maximum:N0}";
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
            starRoots[index].localScale = Vector3.one * (1 + Mathf.Sin(fill * Mathf.PI) * 0.18f);
        }
    }
}
