using UnityEngine;

namespace Blackjack
{
    // Retains card views across refreshes: new cards slide in, existing cards settle,
    // and the dealer's hole card flips only when its sprite actually changes.
    /// <summary>Presentation-only arrival and flip animation for a reused card view.</summary>
    public sealed class BlackjackCardMotion : MonoBehaviour
    {
        RectTransform rect;
        UnityEngine.UI.Image face;
        CanvasGroup opacity;
        Vector2 target, velocity;
        Sprite pending;
        float elapsed, delay, flipTime = -1;
        bool swapped;
        /// <summary>Sets up a new card view, starts it transparent and offsets it from its intended slot.</summary>
        /// <param name="image">Image component that receives the face or back sprite. Passing a different image animates that image instead.</param>
        /// <param name="position">Destination in anchored UI coordinates. Changing it moves the card's final slot.</param>
        /// <param name="stagger">Arrival delay in unscaled seconds. Larger values space out the appearance of newly created cards.</param>
        public void Initialize(UnityEngine.UI.Image image, Vector2 position, float stagger)
        {
            rect = (RectTransform)transform; face = image; target = position; delay = stagger;
            opacity = gameObject.AddComponent<CanvasGroup>(); opacity.alpha = 0;
            rect.anchoredPosition = target + new Vector2(90, 18);
        }
        /// <summary>Moves the destination and starts a flip only if the requested sprite is new.</summary>
        /// <param name="position">Destination in anchored UI coordinates. Changing it moves the card's final slot.</param>
        /// <param name="sprite">Image to display. A different sprite triggers a flip in a card view, or changes the artwork on the decorative menu card.</param>
        public void Set(Vector2 position, Sprite sprite)
        {
            target = position;
            if (face.sprite == sprite || pending == sprite) return;
            pending = sprite; flipTime = 0; swapped = false;
        }
        /// <summary>Advances arrival, movement and flip using unscaled time. The sprite swaps halfway through the flip.</summary>
        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            elapsed += dt;
            if (elapsed < delay) return;
            float arrival = Mathf.Clamp01((elapsed - delay) / .28f);
            opacity.alpha = Mathf.SmoothStep(0, 1, arrival);
            rect.anchoredPosition = Vector2.SmoothDamp(rect.anchoredPosition, target, ref velocity, .13f, Mathf.Infinity, dt);
            if (flipTime < 0) return;
            flipTime += dt;
            float progress = Mathf.Clamp01(flipTime / .32f);
            if (progress >= .5f && !swapped) { face.sprite = pending; swapped = true; }
            rect.localScale = new Vector3(Mathf.Max(.02f, Mathf.Abs(Mathf.Cos(progress * Mathf.PI))), 1, 1);
            if (progress >= 1) { rect.localScale = Vector3.one; pending = null; flipTime = -1; }
        }
    }
}
