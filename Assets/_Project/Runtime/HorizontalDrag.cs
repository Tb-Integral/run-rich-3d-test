namespace RunRich
{
    // Общая обработка мыши и касания без зависимости от частоты кадров.
    public sealed class HorizontalDrag
    {
        private bool _held;
        private int _pointerId;
        private float _previousX;
        private float _previousWidth;

        public float Sample(bool held, int pointerId, float x, float screenWidth)
        {
            if (!held || screenWidth <= 0) { Reset(); return 0; }
            bool continuing = _held && _pointerId == pointerId && _previousWidth == screenWidth;
            float delta = continuing ? (x - _previousX) / screenWidth : 0;
            _held = true;
            _pointerId = pointerId;
            _previousX = x;
            _previousWidth = screenWidth;
            return delta;
        }

        public void Reset() => _held = false;
    }
}
