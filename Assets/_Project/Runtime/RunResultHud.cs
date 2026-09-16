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
        [SerializeField, Min(0)] private float defeatDelay = 0.8f;
        private RunSession.RunState _previous;
        private float _elapsed;
        public bool IsVisible => screen.activeSelf;

        private void OnEnable()
        {
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
            if (session.State != RunSession.RunState.Lost || screen.activeSelf) return;
            _elapsed += Time.deltaTime;
            if (_elapsed >= defeatDelay) screen.SetActive(true);
        }
        private void Refresh()
        {
            var state = session.State;
            if (state != _previous) _elapsed = 0;
            _previous = state;
            bool won = state == RunSession.RunState.Won;
            bool lost = state == RunSession.RunState.Lost;
            screen.SetActive(won || (lost && _elapsed >= defeatDelay));
            next.gameObject.SetActive(won);
            retry.gameObject.SetActive(lost);
            title.text = won ? "ВЫ ПОБЕДИЛИ!" : "НЕ ПОВЕЗЛО!";
            details.text = won ? $"УРОВЕНЬ {session.LevelNumber} ЗАВЕРШЁН\n{session.Score} $  ×{session.ResultMultiplier}" : "ПОПРОБУЙТЕ ЕЩЁ РАЗ";
            actionLabel.text = $"ДАЛЕЕ\n{session.ResultScore} $";
        }
    }
}
