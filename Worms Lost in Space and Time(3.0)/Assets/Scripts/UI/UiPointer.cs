using UnityEngine;
using Worms.Input;

namespace Worms.UI
{
    public static class UiPointer
    {
        public static bool Pressed => PointerInput.Held;
        public static bool PressBegan => PointerInput.PressBegan;
        public static bool OverControl => PointerInput.OverControl;
        public static bool IsOverControl(Vector2 screenPosition) => PointerInput.IsOverControl(screenPosition);
    }
}
