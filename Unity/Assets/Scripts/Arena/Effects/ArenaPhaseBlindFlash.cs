using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UltimoPilar.Arena
{
    /// <summary>
    /// Covers the screen with a white flash so a monument swap can happen while the player is blinded.
    /// </summary>
    public sealed class ArenaPhaseBlindFlash
    {
        private const string OverlayObjectName = "PhaseBlindFlash";
        private const int OverlaySortingOrder = 25;
        private const float MinimumDurationSeconds = 0f;
        private const float FullyTransparentAlpha = 0f;

        private readonly Transform host;
        private Image overlay;

        /// <summary>Creates a flash presenter parented to the given arena object.</summary>
        /// <param name="host">The transform that owns the generated overlay.</param>
        public ArenaPhaseBlindFlash(Transform host)
        {
            this.host = host;
        }

        /// <summary>
        /// Fades in, invokes <paramref name="onCovered"/> at peak white, then fades out.
        /// </summary>
        public IEnumerator Play(
            float fadeInSeconds,
            float holdSeconds,
            float fadeOutSeconds,
            float peakAlpha,
            Color color,
            Action onCovered)
        {
            EnsureOverlay();
            float clampedPeak = Mathf.Clamp01(peakAlpha);
            yield return Fade(FullyTransparentAlpha, clampedPeak, fadeInSeconds, color);
            onCovered?.Invoke();
            if (holdSeconds > MinimumDurationSeconds)
            {
                yield return new WaitForSeconds(holdSeconds);
            }

            yield return Fade(clampedPeak, FullyTransparentAlpha, fadeOutSeconds, color);
            SetAlpha(color, FullyTransparentAlpha);
        }

        /// <summary>Hides the overlay immediately, used when the match resets.</summary>
        public void Hide()
        {
            if (overlay == null)
            {
                return;
            }

            SetAlpha(overlay.color, FullyTransparentAlpha);
        }

        private IEnumerator Fade(float fromAlpha, float toAlpha, float durationSeconds, Color color)
        {
            if (durationSeconds <= MinimumDurationSeconds)
            {
                SetAlpha(color, toAlpha);
                yield break;
            }

            float elapsedSeconds = 0f;
            while (elapsedSeconds < durationSeconds)
            {
                elapsedSeconds += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedSeconds / durationSeconds);
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, progress);
                SetAlpha(color, alpha);
                yield return null;
            }

            SetAlpha(color, toAlpha);
        }

        private void EnsureOverlay()
        {
            if (overlay != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject(
                OverlayObjectName,
                typeof(RectTransform),
                typeof(Canvas));
            if (host != null)
            {
                canvasObject.transform.SetParent(host, false);
            }

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = OverlaySortingOrder;

            GameObject imageObject = new GameObject(
                "Flash",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            overlay = imageObject.GetComponent<Image>();
            overlay.raycastTarget = false;
            SetAlpha(Color.white, FullyTransparentAlpha);
        }

        private void SetAlpha(Color color, float alpha)
        {
            if (overlay == null)
            {
                return;
            }

            color.a = alpha;
            overlay.color = color;
        }
    }
}
