using UnityEngine;
using UnityEngine.UI;
using Worms.Core;
using Worms.Navigation;

namespace Worms.UI
{
    public enum UiScreen { MainMenu, WorldMenu, Story, Hud, Completion, Death }

    /// <summary>Editable serialized view. Scene adapters supply state; buttons request navigation.</summary>
    public sealed class GameUiView : MonoBehaviour
    {
        public UiScreen ScreenKind;
        public Text Title;
        public Text Body;
        public Text SaveStatus;
        public Button Primary;
        public Button Back;
        public Button Menu;
        public Button Quit;
        public Button WorldOne;
        public Button WorldTwo;
        public Button RetrySave;
        public Image OxygenFill;
        private CompletionResult completion;
        private bool retryArmed;
        private string presentedSaveStatus;

        private void Awake()
        {
            if (Primary != null) Primary.onClick.AddListener(PrimaryPressed);
            if (Back != null) Back.onClick.AddListener(() => SceneNavigator.Instance.BackToWorld());
            if (Menu != null) Menu.onClick.AddListener(() => SceneNavigator.Instance.MainMenu());
            if (Quit != null) Quit.onClick.AddListener(() => SceneNavigator.Instance.Quit());
            if (WorldOne != null) WorldOne.onClick.AddListener(() => SceneNavigator.Instance.OpenWorld("world-1"));
            if (WorldTwo != null) WorldTwo.onClick.AddListener(() => SceneNavigator.Instance.OpenWorld("world-2"));
            if (RetrySave != null) RetrySave.onClick.AddListener(RetrySaving);
        }
        private void OnEnable()
        {
            retryArmed = false;
            RefreshSaveStatus();
        }
        private void Update()
        {
            RefreshSaveStatus();
            if (!UiPointer.Pressed) retryArmed = true;
            if (ScreenKind == UiScreen.Death && retryArmed && UiPointer.PressBegan && !UiPointer.OverControl)
                SceneNavigator.Instance.Restart();
        }
        private void PrimaryPressed()
        {
            if (ScreenKind == UiScreen.Completion) SceneNavigator.Instance.OpenNext(completion);
            else if (ScreenKind == UiScreen.Death) SceneNavigator.Instance.Restart();
            else SceneNavigator.Instance.Continue();
        }
        public void BindCompletion(CompletionResult result)
        {
            completion = result;
            if (Title != null) Title.text = result.NextLevelId == null ? "Journey complete!" : "Level complete!";
            if (Body != null) Body.text = result.ElapsedSeconds.ToString("0.00") + " seconds" + (result.StarEarned ? "  •  Star earned!" : "");
            if (Primary != null) Primary.gameObject.SetActive(result.NextLevelId != null);
            RefreshSaveStatus();
        }
        public void SetOxygen(float fraction)
        {
            if (OxygenFill != null) OxygenFill.fillAmount = Mathf.Clamp01(fraction);
            if (Body != null) Body.text = "Oxygen " + Mathf.RoundToInt(Mathf.Clamp01(fraction) * 100) + "%";
        }
        public void RefreshSaveStatus()
        {
            GameServices.EnsureInitialized();
            string error = GameServices.Saves.LastError;
            string status = !string.IsNullOrEmpty(error)
                ? "Save status: " + error
                : GameServices.LegacySaveDetected ? GameServices.LegacySaveNotice : string.Empty;
            if (presentedSaveStatus == status) return;
            presentedSaveStatus = status;
            if (SaveStatus != null) SaveStatus.text = status;
            if (RetrySave != null) RetrySave.gameObject.SetActive(!string.IsNullOrEmpty(error));
        }
        public void RetrySaving()
        {
            GameServices.Saves.TrySave();
            RefreshSaveStatus();
        }
    }
}
