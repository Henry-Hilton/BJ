using UnityEngine;

namespace Blackjack
{
    // Rounded vector avatar with diagonal vertex-color interpolation, no image files.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BlackjackAvatarGraphic : UnityEngine.UI.MaskableGraphic
    {
        Color first, second;
        bool gradient;
        public void Apply(BlackjackPlayerProfile profile)
        {
            first = profile.ColorA; second = profile.ColorB; gradient = profile.avatarStyle == AvatarStyle.Gradient;
            SetVerticesDirty();
        }
        Color Sample(Vector2 point, Rect rect)
        {
            float t = ((point.x - rect.xMin) / rect.width + (rect.yMax - point.y) / rect.height) * .5f;
            return gradient ? Color.Lerp(first, second, t) : first;
        }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear(); Rect r = GetPixelAdjustedRect();
            if (r.width <= 0 || r.height <= 0) return;
            float radius = Mathf.Min(r.width, r.height) * .25f;
            const int steps = 12, count = (steps + 1) * 4;
            mesh.AddVert(r.center, Sample(r.center, r), Vector2.zero);
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(corner < 2 ? r.xMax - radius : r.xMin + radius,
                    corner == 0 || corner == 3 ? r.yMax - radius : r.yMin + radius);
                for (int step = 0; step <= steps; step++)
                {
                    float angle = (90 - corner * 90 - step * 90f / steps) * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 inner = center + direction * (radius - .65f);
                    Color c = Sample(inner, r); mesh.AddVert(inner, c, Vector2.zero);
                    c.a = 0; mesh.AddVert(center + direction * (radius + .65f), c, Vector2.zero);
                }
            }
            for (int i = 0; i < count; i++)
            {
                int a = 1 + i * 2, b = 1 + ((i + 1) % count) * 2;
                mesh.AddTriangle(0,a,b); mesh.AddTriangle(a,a+1,b+1); mesh.AddTriangle(a,b+1,b);
            }
        }
        public static Color InitialColor(BlackjackPlayerProfile profile)
        {
            Color middle = profile.avatarStyle == AvatarStyle.Gradient ? Color.Lerp(profile.ColorA, profile.ColorB, .5f) : profile.ColorA;
            float light = .2126f * Mathf.GammaToLinearSpace(middle.r) + .7152f * Mathf.GammaToLinearSpace(middle.g) + .0722f * Mathf.GammaToLinearSpace(middle.b);
            return light > .179f ? new Color(.02f,.04f,.03f) : new Color(1f,.98f,.93f);
        }
    }
}
