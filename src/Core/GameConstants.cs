namespace RPGGame.Core
{
    /// <summary>
    /// Game balance constants - centralized for easy tweaking
    /// </summary>
    public static class GameConstants
    {
        // Grid Settings
        public const int GRID_WIDTH = 8;
        public const int GRID_HEIGHT = 8;
        
        // Combat Constants
        public const int ATTACK_STAMINA_COST = 3;
        public const int DEFEND_STAMINA_COST = 2;
        public const int MOVE_STAMINA_COST = 1;
        public const int REST_STAMINA_RESTORE = 5;
        
        // Counter System
        public const int MAX_COUNTER_GAUGE = 6;
        
        // Default Character Stats
        public const int DEFAULT_HEALTH = 100;
        public const int DEFAULT_STAMINA = 20;
        public const int DEFAULT_STAT_VALUE = 10;
        
        // Movement
        public const int ATTACK_RANGE = 1; // Adjacent positions
        public const int MAX_ACTIONS_PER_TURN = 2;
    }
}