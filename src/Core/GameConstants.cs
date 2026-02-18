using RPGGame.Core;

namespace RPGGame.Core
{
    /// <summary>
    /// Game balance constants - now delegates to GameConfig for centralized configuration.
    /// Kept as a compatibility shim until all references are migrated directly to GameConfig.
    /// </summary>
    public static class GameConstants
    {
        // Grid Settings (viewport dimensions - now 64x64 sight range)
        public static int GRID_WIDTH => GameConfig.Current.Grid.Viewport.Width;
        public static int GRID_HEIGHT => GameConfig.Current.Grid.Viewport.Height;
        
        // Combat Constants
        public static int ATTACK_STAMINA_COST => GameConfig.Current.Combat.StaminaCosts.Attack;
        public static int DEFEND_STAMINA_COST => GameConfig.Current.Combat.StaminaCosts.Defend;
        public static int MOVE_STAMINA_COST => GameConfig.Current.Combat.StaminaCosts.Move;
        public static int REST_STAMINA_RESTORE => GameConfig.Current.Combat.RestStaminaRestore;
        
        // Counter System
        public static int MAX_COUNTER_GAUGE => GameConfig.Current.Combat.CounterGauge.Maximum;
        
        // Default Character Stats
        public static int DEFAULT_HEALTH => GameConfig.Current.Characters.Defaults.Health;
        public static int DEFAULT_STAMINA => GameConfig.Current.Characters.Defaults.Stamina;
        public static int DEFAULT_STAT_VALUE => GameConfig.Current.Characters.Defaults.StatValue;
        
        // Movement
        public static int ATTACK_RANGE => GameConfig.Current.Combat.AttackRange;
        public static int MAX_ACTIONS_PER_TURN => GameConfig.Current.Turns.MaxActionsAfterMove;
    }
}