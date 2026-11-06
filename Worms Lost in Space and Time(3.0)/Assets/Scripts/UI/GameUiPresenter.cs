using UnityEngine;
using Worms.Core;

namespace Worms.UI
{
    public sealed class GameUiPresenter : MonoBehaviour
    {
        public GameUiView Hud;
        public GameUiView Completion;
        public GameUiView Death;
        private LevelSessionHost host;
        private Oxygen oxygen;
        private void Start()
        {
            host = LevelSessionHost.GetOrCreate();
            if (host == null || host.Session == null) return;
            host.Changed += Present;
            oxygen = FindFirstObjectByType<Oxygen>();
            if (oxygen != null)
            {
                oxygen.FractionChanged += ShowOxygen;
                ShowOxygen(oxygen.Fraction);
            }
            Present(host.Session);
        }
        private void ShowOxygen(float fraction) => Hud.SetOxygen(fraction);
        private void Present(LevelSession state)
        {
            Hud.gameObject.SetActive(state.State == SessionState.Playing);
            Completion.gameObject.SetActive(state.State == SessionState.Completed);
            Death.gameObject.SetActive(state.State == SessionState.Dead);
            if (state.Completion != null) Completion.BindCompletion(state.Completion);
        }
        private void OnDestroy()
        {
            if (host != null) host.Changed -= Present;
            if (oxygen != null) oxygen.FractionChanged -= ShowOxygen;
        }
    }
}
