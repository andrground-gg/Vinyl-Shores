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
        [Header("Combo intensity")]
        [SerializeField, Min(2)] private int motionStartsAt = 10;
        [SerializeField, Min(3)] private int fullIntensityAt = 60;
        [SerializeField, Range(0, 3)] private float maxDriftPixels = 2.5f;
        [SerializeField, Range(0, 2)] private float maxSwayDegrees = 1.5f;
        [SerializeField] private Color warmColor = new Color(1, 0.81f, 0.35f);
        [SerializeField] private Color intenseColor = new Color(1, 0.40f, 0.24f);
        private bool initialized;
        private Vector2 home;
        private Vector2 numberHome;
        private Vector3 numberScale;
        private Quaternion numberRotation;
        private Color colorFrom;
        private Color colorTarget;
        private double changedDsp;
        private bool breaking;
        private bool milestone;
        private Color comboColor = new Color(1, 0.87f, 0.53f);
        public void SetTheme(Color color) => comboColor = color;
        public int CurrentCombo { get; private set; }
        public bool IsVisible => group != null && group.alpha > 0;
        public bool IsConfigured => group != null && content != null && number != null;

        private void Initialize()
        {
            if (initialized || !IsConfigured) return;
            home = content.anchoredPosition;
            numberHome = number.rectTransform.anchoredPosition;
            numberScale = number.rectTransform.localScale;
            numberRotation = number.rectTransform.localRotation;
            initialized = true;
        }

        private void Awake()
        {
            Initialize();
            ResetView();
        }

        public void SetCombo(int combo)
        {
            Initialize();
            if (combo == CurrentCombo) return;
            int previous = CurrentCombo;
            CurrentCombo = combo;
            changedDsp = AudioSettings.dspTime;
            breaking = combo < 2 && previous >= 2;
            milestone = combo >= 10 && combo % 10 == 0;
            if (combo >= 2)
            {
                number.text = combo.ToString();
                colorFrom = number.color;
                float warmAt = Mathf.Lerp(2, fullIntensityAt, 0.4f);
                colorTarget = combo <= warmAt ?
                    Color.Lerp(comboColor, warmColor, Mathf.InverseLerp(2, warmAt, combo)) :
                    Color.Lerp(warmColor, intenseColor, Mathf.InverseLerp(warmAt, fullIntensityAt, combo));
                group.alpha = 1;
            }
            else if (breaking)
            {
                number.color = new Color(1, 0.38f, 0.34f);
                ResetNumberPose();
            }
            else group.alpha = 0;
        }

        private void LateUpdate()
        {
            if (!IsConfigured) return;
            float age = (float)(AudioSettings.dspTime - changedDsp);
            if (breaking)
            {
                float p = Mathf.Clamp01(age / 0.24f);
                group.alpha = 1 - p;
                content.localScale = Vector3.one * (1 - p * 0.15f);
                content.anchoredPosition = home + Vector2.down * (p * 12);
            }
            else if (CurrentCombo >= 2)
            {
                number.color = Color.Lerp(colorFrom, colorTarget, Mathf.SmoothStep(0, 1, age / 0.18f));
                AnimateNumber();
                float p = Mathf.Clamp01(age / 0.32f);
                float bounce = Mathf.Sin(p * Mathf.PI);
                float intensity = Mathf.Clamp01((CurrentCombo - 2) / 38f);
                float scale = Mathf.Min(0.28f, 0.10f + intensity * 0.14f + (milestone ? 0.04f : 0));
                float lift = Mathf.Min(18, 6 + intensity * 10 + (milestone ? 2 : 0));
                content.localScale = new Vector3(1 + bounce * scale, 1 + bounce * scale * 1.2f, 1);
                content.anchoredPosition = home + Vector2.up * (bounce * lift);
                content.localRotation = Quaternion.Euler(0, 0, (milestone ? -1.5f : 0) * bounce);
            }
        }

        private void AnimateNumber()
        {
            if (CurrentCombo < motionStartsAt)
            {
                ResetNumberPose();
                return;
            }
            float strength = Mathf.Lerp(0.15f, 1, Mathf.Clamp01(
                (CurrentCombo - motionStartsAt) / (float)Mathf.Max(1, fullIntensityAt - motionStartsAt)));
            double phase = AudioSettings.dspTime * Mathf.Lerp(3.5f, 6, strength);
            var rect = number.rectTransform;
            rect.anchoredPosition = numberHome + new Vector2(
                (float)Math.Sin(phase) * maxDriftPixels * strength,
                (float)Math.Sin(phase * 2) * maxDriftPixels * strength * 0.65f);
            rect.localRotation = numberRotation * Quaternion.Euler(0, 0,
                (float)Math.Sin(phase) * maxSwayDegrees * strength);
            rect.localScale = numberScale * (1 + (float)Math.Sin(phase * 2) * 0.015f * strength);
        }

        private void ResetNumberPose()
        {
            if (number == null) return;
            number.rectTransform.anchoredPosition = numberHome;
            number.rectTransform.localScale = numberScale;
            number.rectTransform.localRotation = numberRotation;
        }

        public void ResetView()
        {
            Initialize();
            CurrentCombo = 0;
            breaking = false;
            if (group != null) group.alpha = 0;
            if (number != null)
            {
                number.text = "";
                number.color = comboColor;
                ResetNumberPose();
            }
            if (content != null)
            {
                content.anchoredPosition = home;
                content.localScale = Vector3.one;
                content.localRotation = Quaternion.identity;
            }
        }
    }
}
