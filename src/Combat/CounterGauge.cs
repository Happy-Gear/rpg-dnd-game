using System;

namespace RPGGame.Combat
{
    /// <summary>
    /// Counter gauge system for "badminton streak" mechanic
    /// </summary>
    public class CounterGauge
    {
        private int _current = 0;
        private const int MAX_COUNTER = 6;
        
        public int Current => _current;
        public int Maximum => MAX_COUNTER;
        public bool IsReady => _current >= MAX_COUNTER;
        public float FillPercentage => (float)_current / MAX_COUNTER;
        
        /// <summary>
        /// Add counter points from successful over-defense
        /// Only positive values are added (negative values are ignored)
        /// </summary>
        public void AddCounter(int amount)
        {
            // Only add positive amounts (prevent counter reduction via negative values)
            if (amount > 0)
            {
                _current = Math.Min(MAX_COUNTER, _current + amount);
            }
        }
        
        /// <summary>
        /// Consume counter for immediate attack
        /// </summary>
        public bool ConsumeCounter()
        {
            if (IsReady)
            {
                _current = 0;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Reset counter (e.g., when taking damage without defending)
        /// </summary>
        public void Reset()
        {
            _current = 0;
        }
        
        public override string ToString()
        {
            return $"Counter: {_current}/{MAX_COUNTER}" + (IsReady ? " [READY!]" : "");
        }
    }
}