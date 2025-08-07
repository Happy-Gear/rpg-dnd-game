using System;

namespace RPGGame.Characters
{
    /// <summary>
    /// Character stats system - expandable for future 6-stat system
    /// </summary>
    public class CharacterStats
    {
        public int Strength { get; set; } = 10;
        public int Endurance { get; set; } = 10;
        public int Charisma { get; set; } = 10;
        public int Intelligence { get; set; } = 10;
        public int Agility { get; set; } = 10;
        // Future: Add 6th stat here
        
        public CharacterStats() { }
        
        public CharacterStats(int str, int end, int cha, int intel, int agi)
        {
            Strength = str;
            Endurance = end;
            Charisma = cha;
            Intelligence = intel;
            Agility = agi;
        }
    }
}
