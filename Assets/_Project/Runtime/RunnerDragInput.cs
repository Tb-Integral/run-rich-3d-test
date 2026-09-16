using UnityEngine;
using UnityEngine.InputSystem;

namespace RunRich
{
    public sealed class RunnerDragInput : MonoBehaviour
    {
        private readonly HorizontalDrag _drag = new();
        private bool _focused = true;
        private bool _paused;

        public float ReadDelta()
        {
            if (!isActiveAndEnabled || !_focused || _paused) { ResetGesture(); return 0; }
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
                return _drag.Sample(true, touch.primaryTouch.touchId.ReadValue(), touch.primaryTouch.position.ReadValue().x, Screen.width);
            var mouse = Mouse.current;
            return mouse == null ? _drag.Sample(false, -1, 0, Screen.width)
                : _drag.Sample(mouse.leftButton.isPressed, -1, mouse.position.ReadValue().x, Screen.width);
        }

        public void ResetGesture() => _drag.Reset();
        private void OnDisable() => ResetGesture();
        private void OnApplicationFocus(bool focused) { _focused = focused; ResetGesture(); }
        private void OnApplicationPause(bool paused) { _paused = paused; ResetGesture(); }
    }
}
