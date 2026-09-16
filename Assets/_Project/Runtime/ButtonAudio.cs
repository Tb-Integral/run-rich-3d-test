using UnityEngine;
using UnityEngine.UI;

namespace RunRich
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonAudio : MonoBehaviour
    {
        [SerializeField] private RunAudio audioFeedback;
        private Button _button;
        private void Awake() => _button = GetComponent<Button>();
        private void OnEnable() => _button.onClick.AddListener(Play);
        private void OnDisable() => _button.onClick.RemoveListener(Play);
        private void Play() => audioFeedback.PlayClick();
    }
}
