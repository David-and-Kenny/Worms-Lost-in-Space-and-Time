using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Worms.Input
{
    /// <summary>Unity pointer sampling shared by menu and gameplay adapters.</summary>
    public static class PointerInput
    {
        public static bool Held => UnityEngine.Input.GetMouseButton(0) || TouchHeld;
        public static bool PressBegan => UnityEngine.Input.GetMouseButtonDown(0) ||
            (UnityEngine.Input.touchCount > 0 && UnityEngine.Input.GetTouch(0).phase == TouchPhase.Began);
        public static bool TouchHeld
        {
            get
            {
                for (int i = 0; i < UnityEngine.Input.touchCount; i++)
                    if (IsActive(UnityEngine.Input.GetTouch(i))) return true;
                return false;
            }
        }
        public static bool OverControl
        {
            get
            {
                for (int i = 0; i < UnityEngine.Input.touchCount; i++)
                {
                    Touch touch = UnityEngine.Input.GetTouch(i);
                    if (IsActive(touch) && IsOverControl(touch.position)) return true;
                }
                return UnityEngine.Input.GetMouseButton(0) && IsOverControl(UnityEngine.Input.mousePosition);
            }
        }
        private static bool IsActive(Touch touch)
        {
            return touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
        }
        public static bool IsOverControl(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;
            var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            foreach (var hit in hits)
                if (hit.gameObject.GetComponentInParent<Selectable>() != null) return true;
            return false;
        }
    }
}
