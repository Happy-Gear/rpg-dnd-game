using System;
using System.Collections.Generic;

namespace RPGGame.Dice
{
    public class DiceRoller
    {
        private Random _random;
        
        public DiceRoller(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }
        
        /// <summary>
        /// Roll 1d6 for movement
        /// </summary>
        public DiceResult Roll1d6(string rollType = "generic")
        {
            int die1 = _random.Next(1, 7);
            
            return new DiceResult
            {
                Die1 = die1,
                Die2 = 0, // Not used for 1d6
                Total = die1,
                RollType = rollType
            };
        }
        
        /// <summary>
        /// Roll 2d6 for combat/dash movement
        /// </summary>
        public DiceResult Roll2d6(string rollType = "generic")
        {
            int die1 = _random.Next(1, 7);
            int die2 = _random.Next(1, 7);
            int total = die1 + die2;
            
            return new DiceResult
            {
                Die1 = die1,
                Die2 = die2,
                Total = total,
                RollType = rollType
            };
        }
    }
    
    public class DiceResult
    {
        public int Die1 { get; set; }
        public int Die2 { get; set; }
        public int Total { get; set; }
        public string RollType { get; set; }
        
        public bool Is1d6 => Die2 == 0;
        public bool Is2d6 => Die2 > 0;
        
        public override string ToString()
        {
            if (Is1d6)
            {
                return $"[{Die1}] = {Total}";
            }
            else
            {
                return $"[{Die1}+{Die2}] = {Total}";
            }
        }
    }
}