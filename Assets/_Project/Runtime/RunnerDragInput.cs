using UnityEngine;
using UnityEngine.InputSystem;

namespace RunRich
{
    public sealed class RunnerDragInput : MonoBehaviour
    {
        private readonly HorizontalDrag _drag = new();
        private bool _focused = true;
        private bool _paused;
        private bool _pressHeld;

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

        public bool TryGetPressPosition(out Vector2 position)
        {
            position = default;
            if (!isActiveAndEnabled || !_focused || _paused) return false;
            var touch = Touchscreen.current;
            bool held = touch != null && touch.primaryTouch.press.isPressed;
            if (held) position = touch.primaryTouch.position.ReadValue();
            else if (Mouse.current != null)
            {
                held = Mouse.current.leftButton.isPressed;
                position = Mouse.current.position.ReadValue();
            }
            bool pressed = held && !_pressHeld;
            _pressHeld = held;
            return pressed;
        }

        public void ResetGesture() => _drag.Reset();
        private void OnDisable() { _pressHeld = true; ResetGesture(); }
        private void OnApplicationFocus(bool focused) { _focused = focused; _pressHeld = true; ResetGesture(); }
        private void OnApplicationPause(bool paused) { _paused = paused; _pressHeld = true; ResetGesture(); }
    }
}
