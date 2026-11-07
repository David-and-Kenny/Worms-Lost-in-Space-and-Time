using UnityEngine;
using UnityEngine.UI;

namespace Worms.UI
{
    [RequireComponent(typeof(RectTransform), typeof(GridLayoutGroup))]
    public sealed class MenuLevelGrid : MonoBehaviour
    {
        private RectTransform area;
        private GridLayoutGroup grid;
        private Vector2 previousSize;

        private void OnEnable() => Refresh();
        private void OnRectTransformDimensionsChange() => Refresh();

        public void Refresh()
        {
            if (!area) area = GetComponent<RectTransform>();
            if (!grid) grid = GetComponent<GridLayoutGroup>();
            if (!area || !grid || area.rect.width <= 0 || area.rect.height <= 0) return;
            Vector2 size = area.rect.size;
            int columns = size.x > size.y * 1.3f ? 5 : 2;
            int rows = Mathf.Max(1, Mathf.CeilToInt((float)transform.childCount / columns));
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.cellSize = new Vector2(
                Mathf.Max(1, (size.x - grid.padding.horizontal - grid.spacing.x * (columns - 1)) / columns),
                Mathf.Max(1, (size.y - grid.padding.vertical - grid.spacing.y * (rows - 1)) / rows));
            previousSize = size;
        }

        private void LateUpdate()
        {
            if (area && area.rect.size != previousSize) Refresh();
        }
    }
}
