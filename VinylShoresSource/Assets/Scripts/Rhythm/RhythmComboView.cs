using System;
using TMPro;
using UnityEngine;

namespace VinylShores.Rhythm
{
    public sealed class RhythmComboView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_Text number;
        private Vector2 home;
        private double changedDsp;
        private bool breaking;
        private bool milestone;
        private Color comboColor = new Color(1, 0.87f, 0.53f);
        public void SetTheme(Color color) => comboColor = color;
        public int CurrentCombo { get; private set; }
        public bool IsVisible => group != null && group.alpha > 0;
        public bool IsConfigured => group != null && content != null && number != null;

        private void Awake()
        {
            home = content.anchoredPosition;
            ResetView();
        }

        public void SetCombo(int combo)
        {
            if (combo == CurrentCombo) return;
            int previous = CurrentCombo;
            CurrentCombo = combo;
            changedDsp = AudioSettings.dspTime;
            breaking = combo < 2 && previous >= 2;
            milestone = combo >= 10 && combo % 10 == 0;
            if (combo >= 2)
            {
                number.text = combo.ToString();
                number.color = comboColor;
                group.alpha = 1;
            }
            else if (breaking) number.color = new Color(1, 0.38f, 0.34f);
            else group.alpha = 0;
        }

        private void LateUpdate()
        {
            if (!IsConfigured) return;
            float age = (float)(AudioSettings.dspTime - changedDsp);
            if (breaking)
            {
                float p = Mathf.Clamp01(age / 0.16f);
                group.alpha = 1 - p;
                content.localScale = Vector3.one * (1 - p * 0.15f);
                content.anchoredPosition = home + Vector2.down * (p * 8);
            }
            else if (CurrentCombo >= 2)
            {
                float p = Mathf.Clamp01(age / 0.32f);
                float bounce = Mathf.Sin(p * Mathf.PI);
                float intensity = Mathf.Clamp01((CurrentCombo - 2) / 38f);
                float scale = Mathf.Min(0.38f, 0.16f + intensity * 0.17f + (milestone ? 0.05f : 0));
                float lift = Mathf.Min(18, 6 + intensity * 10 + (milestone ? 2 : 0));
                content.localScale = new Vector3(1 + bounce * scale, 1 + bounce * scale * 1.2f, 1);
                content.anchoredPosition = home + Vector2.up * (bounce * lift);
            }
        }

        public void ResetView()
        {
            CurrentCombo = 0;
            breaking = false;
            if (group != null) group.alpha = 0;
            if (number != null) number.text = "";
            if (content != null)
            {
                content.anchoredPosition = home;
                content.localScale = Vector3.one;
            }
        }
    }
}
