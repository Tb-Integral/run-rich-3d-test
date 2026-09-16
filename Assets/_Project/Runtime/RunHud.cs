using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RunRich
{
    public sealed class RunHud : MonoBehaviour
    {
        [SerializeField] private RunSession session;
        [SerializeField] private Text levelLabel;
        [SerializeField] private Text scoreLabel;
        [SerializeField] private GameObject tutorial;
        [SerializeField] private RectTransform hand;
        [SerializeField] private Button restart;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private float handTravel = 125;
        [SerializeField] private float handPeriod = 1.5f;
        private readonly List<RaycastResult> _hits = new();
        private Vector2 _handOrigin;
        private float _tutorialTime;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreen;

        private void Awake() => _handOrigin = hand.anchoredPosition;
        private void OnEnable()
        {
            session.Changed += Refresh;
            restart.onClick.AddListener(session.Restart);
            Refresh();
        }
        private void OnDisable()
        {
            session.Changed -= Refresh;
            restart.onClick.RemoveListener(session.Restart);
        }
        private void Update()
        {
            ApplySafeArea();
            if (!tutorial.activeSelf) return;
            _tutorialTime += Time.unscaledDeltaTime;
            hand.anchoredPosition = _handOrigin + Vector2.right * Mathf.Sin(_tutorialTime * Mathf.PI * 2 / Mathf.Max(0.1f, handPeriod)) * handTravel;
        }
        private void Refresh()
        {
            levelLabel.text = $"УРОВЕНЬ {session.LevelNumber}";
            scoreLabel.text = $"{session.Score} $";
            restart.gameObject.SetActive(session.State == RunSession.RunState.Ready || session.State == RunSession.RunState.Running);
            tutorial.SetActive(session.State == RunSession.RunState.Ready);
            if (tutorial.activeSelf) { _tutorialTime = 0; hand.anchoredPosition = _handOrigin; }
        }
        public bool BlocksStart(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;
            _hits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screenPosition }, _hits);
            return _hits.Count > 0;
        }
        private void ApplySafeArea()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            Rect area = Screen.safeArea;
            if (screen.x <= 0 || screen.y <= 0 || (area == _lastSafeArea && screen == _lastScreen)) return;
            safeArea.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
            safeArea.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            _lastSafeArea = area;
            _lastScreen = screen;
        }
    }
}
