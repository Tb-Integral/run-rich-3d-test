using UnityEngine;

namespace RunRich
{
    public sealed class RunnerMotor : MonoBehaviour
    {
        [SerializeField] private TrackPath path;
        [SerializeField] private RunnerSettings settings;
        [SerializeField] private RunnerDragInput dragInput;
        [SerializeField] private PlayerPresentation presentation;
        [SerializeField] private bool startAutomatically = true;

        public TrackPath Path => path;
        public RunnerSettings Settings => settings;
        public float Distance { get; private set; }
        public float LateralOffset { get; private set; }
        public bool IsRunning { get; private set; }
        public Pose TrackPose => path.Evaluate(Distance);
        public float LateralLimit => Mathf.Max(0, path.Width * 0.5f - settings.EdgePadding);

        private void Start()
        {
            if (path == null || settings == null || dragInput == null || presentation == null)
            {
                Debug.LogError("Не назначены зависимости движения Runner.", this);
                enabled = false;
                return;
            }
            ResetToStart();
            if (startAutomatically) BeginRun();
        }

        private void Update()
        {
            if (!IsRunning) return;
            Tick(Time.deltaTime, dragInput.ReadDelta());
        }

        public void BeginRun()
        {
            if (!isActiveAndEnabled || path == null || settings == null || Distance >= path.Length) return;
            dragInput.ResetGesture();
            IsRunning = true;
            presentation.SetWalking(true);
        }

        public void Stop()
        {
            IsRunning = false;
            if (dragInput != null) dragInput.ResetGesture();
            if (presentation != null)
            {
                presentation.SetLateralMotion(0);
                presentation.SetWalking(false);
            }
        }

        public void ResetToStart()
        {
            Stop();
            Distance = 0;
            LateralOffset = 0;
            if (presentation != null) presentation.ResetPresentation();
            if (path != null) ApplyPose();
        }

        public void Tick(float deltaTime, float normalizedDrag)
        {
            if (!IsRunning || deltaTime <= 0) return;
            // Не умножаем перемещение указателя на deltaTime: оно уже накоплено за кадр.
            float previousOffset = LateralOffset;
            LateralOffset = Mathf.Clamp(LateralOffset + normalizedDrag * path.Width * settings.DragSensitivity, -LateralLimit, LateralLimit);
            // На краю фактического бокового движения нет: модель возвращается прямо.
            presentation.SetLateralMotion((LateralOffset - previousOffset) / deltaTime);
            Distance = Mathf.Min(path.Length, Distance + settings.ForwardSpeed * deltaTime);
            ApplyPose();
            if (Distance >= path.Length) Stop();
        }

        private void ApplyPose()
        {
            Pose pose = TrackPose;
            transform.SetPositionAndRotation(pose.position + pose.rotation * Vector3.right * LateralOffset, pose.rotation);
        }

        private void OnDisable() => Stop();
    }
}
