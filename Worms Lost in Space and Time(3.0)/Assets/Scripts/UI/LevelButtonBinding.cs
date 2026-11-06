using UnityEngine;
using UnityEngine.UI;

namespace Worms.UI
{
    public sealed class LevelButtonBinding : MonoBehaviour
    {
        public string LevelId;
        public Button Button;
        public GameObject Lock;
        public GameObject Star;
        private void OnEnable() => Refresh();
        public void Refresh()
        {
            GameServices.EnsureInitialized();
            bool unlocked = GameServices.Progression.IsUnlocked(LevelId);
            if (Button != null) Button.interactable = unlocked;
            if (Lock != null) Lock.SetActive(!unlocked);
            if (Star != null) Star.SetActive(GameServices.Progression.HasStar(LevelId));
        }
    }
}
