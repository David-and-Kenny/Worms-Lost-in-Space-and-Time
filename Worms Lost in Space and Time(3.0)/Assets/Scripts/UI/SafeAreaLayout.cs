using UnityEngine;

namespace Worms.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        private Rect previous;
        private Vector2 size;
        private void OnEnable() => Refresh();
        private void Update()
        {
            if (previous != Screen.safeArea || size != new Vector2(Screen.width, Screen.height)) Refresh();
        }
        private void Refresh() => Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));
        public void Apply(Rect safeArea, Vector2 screenSize)
        {
            if (screenSize.x <= 0 || screenSize.y <= 0) return;
            previous = safeArea;
            size = screenSize;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(safeArea.xMin / size.x, safeArea.yMin / size.y);
            rect.anchorMax = new Vector2(safeArea.xMax / size.x, safeArea.yMax / size.y);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
