using UnityEngine;
using UnityEngine.UI;

namespace RunRich
{
    [DefaultExecutionOrder(200)]
    public sealed class WealthHud : MonoBehaviour
    {
        [SerializeField] private PlayerWealth wealth;
        [SerializeField] private RunSession session;
        [SerializeField] private Transform target;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private RectTransform bar;
        [SerializeField] private RectTransform fill;
        [SerializeField] private Graphic fillGraphic;
        [SerializeField] private Text status;
        [SerializeField] private Text popup;
        [SerializeField] private Vector3 headOffset = new Vector3(0, 1.52f, 0);
        [SerializeField] private Vector2 popupOffset = new Vector2(175, -160);
        [SerializeField, Min(0)] private float fillDamping = 0.12f;
        [SerializeField, Min(0.1f)] private float popupDuration = 0.85f;
        [SerializeField, Min(0)] private float comboWindow = 0.55f;
        [SerializeField] private Color positiveColor = new Color(0.18f, 0.85f, 0);
        [SerializeField] private Color negativeColor = new Color(1, 0.06f, 0.04f);
        private float _displayed;
        private float _popupElapsed = float.PositiveInfinity;
        private int _combo;
        private Canvas _canvas;

        private void Awake() => _canvas = canvasRect.GetComponent<Canvas>();

        private void OnEnable()
        {
            wealth.Changed += Refresh;
            wealth.AmountChanged += ShowAmount;
            session.Changed += RefreshState;
            Refresh();
            RefreshState();
        }

        private void OnDisable()
        {
            wealth.Changed -= Refresh;
            wealth.AmountChanged -= ShowAmount;
            session.Changed -= RefreshState;
        }

        private void Refresh()
        {
            var tier = wealth.Tier;
            status.text = tier.label;
            status.color = fillGraphic.color = tier.color;
        }

        private void RefreshState()
        {
            bool visible = session.State == RunSession.RunState.Running || session.State == RunSession.RunState.Finishing;
            bar.gameObject.SetActive(visible);
            if (!visible)
            {
                _displayed = wealth.Normalized;
                _popupElapsed = float.PositiveInfinity;
                _combo = 0;
                popup.gameObject.SetActive(false);
            }
        }

        private void ShowAmount(int delta)
        {
            _combo = _popupElapsed < comboWindow && System.Math.Sign(_combo) == System.Math.Sign(delta) ? _combo + delta : delta;
            _popupElapsed = 0;
            popup.text = $"{(_combo > 0 ? "+" : "−")} {System.Math.Abs(_combo)} $";
            popup.color = delta > 0 ? positiveColor : negativeColor;
            popup.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            Vector3 screen = worldCamera.WorldToScreenPoint(target.position + headOffset);
            var uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, uiCamera, out var local);
            bar.anchoredPosition = local;
            float blend = fillDamping <= 0 ? 1 : 1 - Mathf.Exp(-Time.deltaTime / fillDamping);
            _displayed = Mathf.Lerp(_displayed, wealth.Normalized, blend);
            fill.anchorMax = new Vector2(_displayed, 1);
            fill.gameObject.SetActive(_displayed > 0.001f);
            if (!popup.gameObject.activeSelf) return;
            _popupElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_popupElapsed / popupDuration);
            popup.rectTransform.anchoredPosition = local + popupOffset + Vector2.up * (progress * 65);
            var color = popup.color;
            color.a = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.45f, 1, progress));
            popup.color = color;
            popup.rectTransform.localScale = Vector3.one * (1 + 0.18f * Mathf.Sin(progress * Mathf.PI));
            if (progress >= 1) popup.gameObject.SetActive(false);
        }
    }
}
