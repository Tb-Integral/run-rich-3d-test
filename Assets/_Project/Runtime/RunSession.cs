using System;
using UnityEngine;

namespace RunRich
{
    [DefaultExecutionOrder(100)]
    public sealed class RunSession : MonoBehaviour
    {
        public enum RunState { Ready, Running, Finishing, Won, Lost }

        [SerializeField] private RunnerMotor motor;
        [SerializeField] private RunnerDragInput input;
        [SerializeField] private PlayerPresentation presentation;
        [SerializeField] private RunnerCamera followCamera;
        [SerializeField] private RunLevelSource levels;
        [SerializeField] private RunHud hud;
        [SerializeField] private PlayerWealth wealth;
        [SerializeField] private PickupCollector pickups;
        [SerializeField, Min(0)] private float finishDelay = 0.8f;
        private bool _initialized;
        private int _restartFrame = -1;
        private float _finishElapsed;

        public event Action Changed;
        public RunState State { get; private set; } = RunState.Ready;
        public int Score => wealth.Score;
        public int LevelNumber => levels.LevelNumber;

        private void OnEnable()
        {
            motor.ReachedEnd += BeginFinishing;
            wealth.Changed += NotifyChanged;
        }
        private void OnDisable()
        {
            motor.ReachedEnd -= BeginFinishing;
            wealth.Changed -= NotifyChanged;
        }
        private void Start() => Prepare(false);

        private void Update()
        {
            if (!_initialized) return;
            bool pressed = input.TryGetPressPosition(out var position);
            if (State == RunState.Ready && pressed && !hud.BlocksStart(position))
                StartRun();
            if (State != RunState.Finishing) return;
            _finishElapsed += Time.deltaTime;
            if (_finishElapsed >= finishDelay) CompleteWin();
        }

        public void StartRun()
        {
            if (!_initialized || State != RunState.Ready) return;
            levels.StartAttempt();
            SetState(RunState.Running);
            motor.BeginRun();
        }

        public void Restart()
        {
            if (!_initialized || _restartFrame == Time.frameCount) return;
            _restartFrame = Time.frameCount;
            Prepare(true);
        }

        private void Prepare(bool restart)
        {
            _initialized = false;
            motor.Stop();
            motor.BindPath(levels.Load(restart));
            motor.ResetToStart();
            followCamera.Snap();
            pickups.Bind(motor.Path);
            wealth.ResetValue();
            _finishElapsed = 0;
            _initialized = true;
            SetState(RunState.Ready);
        }

        public void BeginFinishing()
        {
            if (!_initialized || State != RunState.Running) return;
            motor.Stop();
            _finishElapsed = 0;
            SetState(RunState.Finishing);
        }

        public bool TryChangeScore(int delta)
        {
            if (!_initialized || !isActiveAndEnabled || State != RunState.Running) return false;
            // Проверяем исходную сумму: шкала ограничивает отображаемый счёт нулём.
            bool depleted = (long)wealth.Score + delta < 0;
            wealth.Change(delta);
            if (depleted) Lose();
            return true;
        }

        private void NotifyChanged() => Changed?.Invoke();

        public void CompleteWin()
        {
            if (State != RunState.Finishing) return;
            presentation.ShowVictory();
            SetState(RunState.Won);
        }

        public void Lose()
        {
            if (State != RunState.Running) return;
            motor.Stop();
            presentation.ShowDefeat();
            SetState(RunState.Lost);
        }

        private void SetState(RunState state)
        {
            State = state;
            Changed?.Invoke();
        }
    }
}
