using UnityEngine;
using UnityEngine.UI;

namespace RunRich
{
    public sealed class RunResultHud : MonoBehaviour
    {
        [SerializeField] private RunSession session;
        [SerializeField] private GameObject screen;
        [SerializeField] private Text title;
        [SerializeField] private Text details;
        [SerializeField] private Text actionLabel;
        [SerializeField] private Button next;
        [SerializeField] private Button retry;
        [SerializeField] private GameObject victoryBackdrop;
        [SerializeField, Min(0)] private float defeatDelay = 0.8f;
        [SerializeField, Min(0.01f)] private float textEntranceDuration = 0.5f;
        [SerializeField, Range(0, 0.05f)] private float textPulseAmount = 0.018f;
        [SerializeField, Min(0.01f)] private float textPulsePeriod = 1.8f;
        [SerializeField] private Vector2 victoryDetailsPosition = new Vector2(0, -140);
        [SerializeField] private Vector2 defeatDetailsPosition = new Vector2(0, -420);
        private RunSession.RunState _previous;
        private float _elapsed;
        private bool _initialized;
        private float _textElapsed;
        public bool IsVisible => screen.activeSelf;

        private void OnEnable()
        {
            _initialized = false;
            session.Changed += Refresh;
            next.onClick.AddListener(session.NextLevel);
            retry.onClick.AddListener(session.Restart);
            Refresh();
        }
        private void OnDisable()
        {
            session.Changed -= Refresh;
            next.onClick.RemoveListener(session.NextLevel);
            retry.onClick.RemoveListener(session.Restart);
        }
        private void Update()
        {
            if (session.State == RunSession.RunState.Lost && !screen.activeSelf)
            {
                _elapsed += Time.deltaTime;
                if (_elapsed >= defeatDelay) screen.SetActive(true);
            }
            if (!screen.activeSelf) return;
            _textElapsed += Time.unscaledDeltaTime;
            AnimateText();
        }
        private void AnimateText()
        {
            float progress = Mathf.Clamp01(_textElapsed / textEntranceDuration);
            float remaining = progress - 1;
            float pop = 1 + 2.7f * remaining * remaining * remaining + 1.7f * remaining * remaining;
            float pulseTime = Mathf.Max(0, _textElapsed - textEntranceDuration);
            float pulse = Mathf.Sin(pulseTime * Mathf.PI * 2 / textPulsePeriod) * textPulseAmount;
            title.rectTransform.localScale = Vector3.one * (Mathf.LerpUnclamped(0.72f, 1, pop) + pulse);
            float tilt = _previous == RunSession.RunState.Lost ? Mathf.Sin(progress * Mathf.PI * 4) * (1 - progress) * 5 : 0;
            title.rectTransform.localRotation = Quaternion.Euler(0, 0, tilt);
            SetAlpha(title, Mathf.Clamp01(progress * 3));
            float detailProgress = Mathf.Clamp01((progress - 0.2f) / 0.8f);
            details.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1, detailProgress);
            SetAlpha(details, detailProgress);
        }
        private static void SetAlpha(Text text, float alpha)
        {
            var color = text.color; color.a = alpha; text.color = color;
        }
        private void Refresh()
        {
            var state = session.State;
            if (_initialized && state == _previous) return;
            _initialized = true;
            if (state != _previous) _elapsed = 0;
            _previous = state;
            bool won = state == RunSession.RunState.Won;
            bool lost = state == RunSession.RunState.Lost;
            _textElapsed = 0;
            if (victoryBackdrop != null) victoryBackdrop.SetActive(won);
            details.rectTransform.anchoredPosition = won ? victoryDetailsPosition : defeatDetailsPosition;
            AnimateText();
            screen.SetActive(won || (lost && _elapsed >= defeatDelay));
            next.gameObject.SetActive(won);
            retry.gameObject.SetActive(lost);
            title.text = won ? "ВЫ ПОБЕДИЛИ!" : "НЕ ПОВЕЗЛО!";
            details.text = won ? $"УРОВЕНЬ {session.LevelNumber} ЗАВЕРШЁН\n{session.Score} $  ×{session.ResultMultiplier}" : "ПОПРОБУЙТЕ ЕЩЁ РАЗ";
            actionLabel.text = $"ДАЛЕЕ\n{session.ResultScore} $";
        }
    }
}
