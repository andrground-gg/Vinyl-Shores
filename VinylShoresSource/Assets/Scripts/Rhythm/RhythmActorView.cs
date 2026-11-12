using System;
using UnityEngine;

namespace VinylShores.Rhythm
{
    public sealed class RhythmActorView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image actor;
        [SerializeField] private RectTransform shadow;
        [SerializeField] private Sprite idle;
        [SerializeField] private Sprite jump;
        private Vector2 home;
        private Vector2 shadowHome;
        private bool initialized;
        private bool moving;
        private bool settling;
        private bool sadStanding;
        private float sadX;
        private double hopStart;
        private double period = 1;
        private float fromX;
        private float toX = 18;
        private float height = 20;
        private double shockDsp = double.NegativeInfinity;
        private double lastShockBeat = double.NegativeInfinity;
        private Vector2 shockPosition;
        private double settleDsp;
        private Vector2 settlePosition;
        public bool IsConfigured => actor != null && shadow != null && idle != null && jump != null;
        public float CurrentHopHeight => height;
        public double CurrentHopPeriod => period;

        private void Initialize()
        {
            if (initialized) return;
            home = actor.rectTransform.anchoredPosition;
            shadowHome = shadow.anchoredPosition;
            initialized = true;
        }

        public void ResetPose()
        {
            if (!IsConfigured) return;
            Initialize();
            moving = settling = sadStanding = false;
            shockDsp = lastShockBeat = double.NegativeInfinity;
            actor.rectTransform.anchoredPosition = home;
            actor.rectTransform.localScale = Vector3.one;
            actor.rectTransform.localRotation = Quaternion.identity;
            actor.sprite = idle;
            shadow.anchoredPosition = shadowHome;
            shadow.localScale = Vector3.one;
        }

        public void Begin(double beat)
        {
            ResetPose();
            moving = true;
            hopStart = beat;
            period = Math.Ceiling(beat) - beat;
            if (period < 0.25) period += 1;
            height = 20;
            fromX = 0;
            toX = 18;
        }

        public void ReactSad(double beat)
        {
            if (!moving || beat - lastShockBeat < 2) return;
            lastShockBeat = beat;
            shockDsp = AudioSettings.dspTime;
            shockPosition = actor.rectTransform.anchoredPosition - home;
        }

        public void Settle()
        {
            if (!initialized || !moving) return;
            moving = false;
            settling = true;
            settleDsp = AudioSettings.dspTime;
            settlePosition = actor.rectTransform.anchoredPosition;
        }

        public void Tick(double beat, float energy)
        {
            if (!IsConfigured || !initialized) return;
            var root = actor.rectTransform;
            if (settling)
            {
                float p = Mathf.Clamp01((float)(AudioSettings.dspTime - settleDsp) / 0.2f);
                root.anchoredPosition = Vector2.Lerp(settlePosition, home, p);
                root.localRotation = Quaternion.identity;
                root.localScale = Vector3.one;
                actor.sprite = idle;
                shadow.anchoredPosition = shadowHome + Vector2.right * (root.anchoredPosition.x - home.x);
                shadow.localScale = Vector3.one;
                if (p >= 1) settling = false;
                return;
            }
            if (!moving) return;
            double shockAge = AudioSettings.dspTime - shockDsp;
            if (shockAge < 0.35)
            {
                float land = Mathf.Clamp01((float)shockAge / 0.1f);
                root.anchoredPosition = home + new Vector2(shockPosition.x, Mathf.Lerp(shockPosition.y, 0, land));
                float squash = shockAge < 0.2 ? land : 1 - Mathf.Clamp01(((float)shockAge - 0.2f) / 0.15f);
                root.localScale = new Vector3(1 + 0.15f * squash, 1 - 0.2f * squash, 1);
                root.localRotation = Quaternion.identity;
                actor.sprite = idle;
                shadow.anchoredPosition = shadowHome + Vector2.right * shockPosition.x;
                shadow.localScale = Vector3.one;
                hopStart = beat;
                fromX = shockPosition.x;
                ConfigureHop(energy);
                double cadence = period;
                double boundary = (Math.Floor(beat / cadence) + 1) * cadence;
                if (boundary - beat < 0.25) boundary += cadence;
                period = boundary - beat;
                toX = fromX >= 0 ? -Range(energy) : Range(energy);
                return;
            }
            if (energy < 0.35f)
            {
                if (!sadStanding) sadX = root.anchoredPosition.x - home.x;
                sadStanding = true;
                float pulse = (1 - Mathf.Cos((float)(beat * Math.PI))) * 0.5f;
                root.anchoredPosition = home + Vector2.right * sadX;
                root.localScale = new Vector3(1, 1 - 0.13f * pulse, 1);
                root.localRotation = Quaternion.identity;
                actor.sprite = idle;
                shadow.anchoredPosition = shadowHome + Vector2.right * sadX;
                shadow.localScale = Vector3.one;
                return;
            }
            if (sadStanding)
            {
                sadStanding = false;
                hopStart = beat;
                fromX = sadX;
                ConfigureHop(energy);
                toX = fromX >= 0 ? -Range(energy) : Range(energy);
            }
            while (beat >= hopStart + period)
            {
                hopStart += period;
                fromX = toX;
                ConfigureHop(energy);
                toX = fromX >= 0 ? -Range(energy) : Range(energy);
            }
            float progress = Mathf.Clamp01((float)((beat - hopStart) / period));
            float arc = Mathf.Sin(progress * Mathf.PI);
            float x = Mathf.Lerp(fromX, toX, Mathf.SmoothStep(0, 1, progress));
            root.anchoredPosition = home + new Vector2(x, height * arc);
            float squashLanding = 1 - Mathf.Clamp01(progress / 0.1f);
            root.localScale = new Vector3(1 + 0.08f * squashLanding, 1 - 0.08f * squashLanding, 1);
            root.localRotation = Quaternion.Euler(0, 0, Math.Sign(toX - fromX) * -5 * arc);
            actor.sprite = progress > 0.08f && progress < 0.92f ? jump : idle;
            shadow.anchoredPosition = shadowHome + Vector2.right * x;
            shadow.localScale = new Vector3(1 - arc * 0.25f, 1 - arc * 0.2f, 1);
        }

        private void ConfigureHop(float energy)
        {
            period = energy < 0.35f ? 2 : energy < 0.75f ? 1 : 0.5;
            height = energy < 0.35f ? 10 : energy < 0.75f ? 20 : 28;
        }

        private static float Range(float energy) => energy < 0.35f ? 12 : energy < 0.75f ? 18 : 24;
    }
}
