using UnityEngine;
using UnityEngine.EventSystems;

namespace Blackjack
{
    // Rounded geometry with a one-pixel alpha fringe; no texture asset required.
    /// <summary>Renders a rounded button or panel with an antialiased edge.</summary>
    public sealed class RoundedMenuGraphic : UnityEngine.UI.Image
    {
        public float Radius = 14;
        /// <summary>Builds rounded button geometry and a transparent fringe. A nonpositive effective radius uses the base rectangular image.</summary>
        /// <param name="mesh">Unity's vertex and triangle buffer for this UI element. Generated geometry changes appearance, not game state.</param>
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = GetPixelAdjustedRect();
            float radius = Mathf.Min(Radius, Mathf.Min(r.width, r.height) * .5f);
            if (radius <= 0) { base.OnPopulateMesh(mesh); return; }
            const int steps = 10, count = (steps + 1) * 4;
            mesh.AddVert(r.center, color, Vector2.zero);
            Color transparent = color; transparent.a = 0;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(corner < 2 ? r.xMax - radius : r.xMin + radius,
                    corner == 0 || corner == 3 ? r.yMax - radius : r.yMin + radius);
                for (int step = 0; step <= steps; step++)
                {
                    float angle = (90 - corner * 90 - step * 90f / steps) * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    mesh.AddVert(center + direction * (radius - .65f), color, Vector2.zero);
                    mesh.AddVert(center + direction * (radius + .65f), transparent, Vector2.zero);
                }
            }
            for (int i = 0; i < count; i++)
            {
                int a = 1 + i * 2, b = 1 + ((i + 1) % count) * 2;
                mesh.AddTriangle(0, a, b);
                mesh.AddTriangle(a, a + 1, b + 1); mesh.AddTriangle(a, b + 1, b);
            }
        }
    }

    /// <summary>Handles pointer and focus feedback or idle floating on a UI object.</summary>
    public sealed class BlackjackMenuMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        RectTransform rect;
        Vector2 origin, velocity;
        Vector3 scaleVelocity;
        bool hovered, selected, pressed;
        UnityEngine.UI.Selectable selectable;
        public bool Floating;
        public float Phase;
        /// <summary>Caches the rect, original anchored position and optional selectable.</summary>
        void Awake() { rect = (RectTransform)transform; origin = rect.anchoredPosition; selectable = GetComponent<UnityEngine.UI.Selectable>(); }
        /// <summary>Clears hover, selection and press flags when the component is shown.</summary>
        void OnEnable() { hovered = selected = pressed = false; }
        /// <summary>Restores the original position and scale and clears animation velocity.</summary>
        void OnDisable()
        {
            if (rect == null) return;
            rect.anchoredPosition = origin; rect.localScale = Vector3.one;
            velocity = Vector2.zero; scaleVelocity = Vector3.zero;
        }
        /// <summary>Eases toward the hover, press or idle position and scale using unscaled time.</summary>
        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            if (selectable != null && !selectable.IsInteractable()) hovered = selected = pressed = false;
            bool active = hovered || selected;
            float lift = Floating ? Mathf.Sin(Time.unscaledTime * .8f + Phase) * 4 : active ? 3 : 0;
            float scale = Floating ? 1 : pressed ? .975f : active ? 1.018f : 1;
            rect.anchoredPosition = Vector2.SmoothDamp(rect.anchoredPosition, origin + Vector2.up * lift, ref velocity, .14f, Mathf.Infinity, dt);
            rect.localScale = Vector3.SmoothDamp(rect.localScale, Vector3.one * scale, ref scaleVelocity, .11f, Mathf.Infinity, dt);
        }
        /// <summary>Sets hover feedback when the pointer enters.</summary>
        /// <param name="e">EventSystem payload for this interaction. This implementation ignores its contents.</param>
        public void OnPointerEnter(PointerEventData e) { hovered = true; }
        /// <summary>Clears hover and press feedback when the pointer leaves.</summary>
        /// <param name="e">EventSystem payload for this interaction. This implementation ignores its contents.</param>
        public void OnPointerExit(PointerEventData e) { hovered = pressed = false; }
        /// <summary>Enables compressed press feedback.</summary>
        /// <param name="e">EventSystem payload for this interaction. This implementation ignores its contents.</param>
        public void OnPointerDown(PointerEventData e) { pressed = true; }
        /// <summary>Clears compressed press feedback.</summary>
        /// <param name="e">EventSystem payload for this interaction. This implementation ignores its contents.</param>
        public void OnPointerUp(PointerEventData e) { pressed = false; }
        /// <summary>Enables selection feedback for the focused control.</summary>
        /// <param name="e">EventSystem payload for this interaction. This implementation ignores its contents.</param>
        public void OnSelect(BaseEventData e) { selected = true; }
        /// <summary>Clears selection and press feedback.</summary>
        /// <param name="e">EventSystem payload for this interaction. This implementation ignores its contents.</param>
        public void OnDeselect(BaseEventData e) { selected = pressed = false; }
    }
}
