using UnityEngine;
using UnityEngine.UI;

namespace Worms.UI
{
    [RequireComponent(typeof(RectTransform), typeof(GridLayoutGroup))]
    public sealed class MenuLevelGrid : MonoBehaviour
    {
        [SerializeField] private RectTransform[] pipeLinks = new RectTransform[0];
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
            PositionLinks(columns, rows, size);
        }

        public void SetPipeLinks(RectTransform[] links)
        {
            pipeLinks = links;
            Refresh();
        }

        private void PositionLinks(int columns, int rows, Vector2 size)
        {
            int linkIndex = 0;
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                int index = row * columns + column;
                if (index >= transform.childCount) continue;
                Vector2 start = CellCenter(row, column, size);
                if (column + 1 < columns && index + 1 < transform.childCount)
                    PositionLink(linkIndex++, start, CellCenter(row, column + 1, size));
                if (row + 1 < rows && index + columns < transform.childCount)
                    PositionLink(linkIndex++, start, CellCenter(row + 1, column, size));
            }
            for (; linkIndex < pipeLinks.Length; linkIndex++)
                if (pipeLinks[linkIndex]) pipeLinks[linkIndex].gameObject.SetActive(false);
        }

        private Vector2 CellCenter(int row, int column, Vector2 size)
        {
            return new Vector2(-size.x / 2 + grid.padding.left + grid.cellSize.x / 2
                    + column * (grid.cellSize.x + grid.spacing.x),
                size.y / 2 - grid.padding.top - grid.cellSize.y / 2
                    - row * (grid.cellSize.y + grid.spacing.y));
        }

        private void PositionLink(int index, Vector2 start, Vector2 end)
        {
            if (index >= pipeLinks.Length || !pipeLinks[index]) return;
            var link = pipeLinks[index];
            Vector2 direction = end - start;
            link.gameObject.SetActive(true);
            link.anchorMin = link.anchorMax = new Vector2(.5f, .5f);
            link.anchoredPosition = (start + end) / 2;
            link.sizeDelta = new Vector2(direction.magnitude, 14);
            link.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        private void LateUpdate()
        {
            if (area && area.rect.size != previousSize) Refresh();
        }
    }
}
