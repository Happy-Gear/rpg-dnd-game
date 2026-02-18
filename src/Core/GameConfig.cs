using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Grid;

namespace RPGGame.Core
{
    /// <summary>
    /// Centralized game configuration loaded from balance.json.
    /// Single source of truth for all balance parameters.
    /// </summary>
    public class GameConfig
    {
        private static GameConfig _instance;
        private static readonly object _lock = new object();
        private static string _configPath;

        [JsonPropertyName("combat")]
        public CombatConfig Combat { get; set; } = new();

        [JsonPropertyName("characters")]
        public CharacterConfig Characters { get; set; } = new();

        [JsonPropertyName("grid")]
        public GridConfig Grid { get; set; } = new();

        [JsonPropertyName("turns")]
        public TurnConfig Turns { get; set; } = new();

        [JsonPropertyName("movement")]
        public MovementConfig Movement { get; set; } = new();

        public static GameConfig Current
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new GameConfig();
                    }
                }
                return _instance;
            }
        }

        public static GameConfig LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Config file not found: {filePath}", filePath);

            var json = File.ReadAllText(filePath);
            return Load(json, filePath);
        }

        public static GameConfig Load(string json, string sourcePath = null)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var config = JsonSerializer.Deserialize<GameConfig>(json, options)
                ?? throw new JsonException("Deserialization returned null");

            config.Validate();

            lock (_lock)
            {
                _instance = config;
                _configPath = sourcePath;
            }

            return config;
        }

        public static void ResetToDefaults()
        {
            lock (_lock)
            {
                _instance = new GameConfig();
                _configPath = null;
            }
        }

        public static bool Reload()
        {
            if (string.IsNullOrEmpty(_configPath) || !File.Exists(_configPath))
                return false;

            try
            {
                LoadFromFile(_configPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Validate()
        {
            var errors = new List<string>();

            // Combat
            if (Combat.StaminaCosts.Attack < 0) errors.Add("combat.staminaCosts.attack must be >= 0");
            if (Combat.StaminaCosts.Defend < 0) errors.Add("combat.staminaCosts.defend must be >= 0");
            if (Combat.StaminaCosts.Move < 0) errors.Add("combat.staminaCosts.move must be >= 0");
            if (Combat.RestStaminaRestore < 1) errors.Add("combat.restStaminaRestore must be >= 1");
            if (Combat.AttackRange < 1) errors.Add("combat.attackRange must be >= 1");
            if (Combat.CounterGauge.Maximum < 1) errors.Add("combat.counterGauge.maximum must be >= 1");

            // Characters
            if (Characters.Defaults.Health < 1) errors.Add("characters.defaults.health must be >= 1");
            if (Characters.Defaults.Stamina < 1) errors.Add("characters.defaults.stamina must be >= 1");
            if (Characters.Defaults.StatValue < 0) errors.Add("characters.defaults.statValue must be >= 0");

            // Grid
            if (Grid.Viewport.Width < 4) errors.Add("grid.viewport.width must be >= 4");
            if (Grid.Viewport.Height < 4) errors.Add("grid.viewport.height must be >= 4");
            if (Grid.Arena.Diameter < 4) errors.Add("grid.arena.diameter must be >= 4");
            if (Grid.StartingPositions == null || Grid.StartingPositions.Count == 0)
                errors.Add("grid.startingPositions must have at least one entry");

            if (Grid.StartingPositions != null)
            {
                for (int i = 0; i < Grid.StartingPositions.Count; i++)
                {
                    var pos = Grid.StartingPositions[i];
                    if (!Grid.IsValidArenaPosition(pos.X, pos.Y))
                        errors.Add($"grid.startingPositions[{i}] ({pos.X},{pos.Y}) is outside the circular arena");
                }
            }

            // Turns
            if (Turns.MaxActionsBase < 1) errors.Add("turns.maxActionsBase must be >= 1");
            if (Turns.MaxActionsAfterMove < Turns.MaxActionsBase) errors.Add("turns.maxActionsAfterMove must be >= maxActionsBase");
            if (Turns.ForcedRestRestore < 1) errors.Add("turns.forcedRestRestore must be >= 1");
            if (Turns.MaxSkipAttempts < 1) errors.Add("turns.maxSkipAttempts must be >= 1");

            // Movement
            if (Movement.SimpleMove.DiceCount < 1) errors.Add("movement.simpleMove.diceCount must be >= 1");
            if (Movement.SimpleMove.DiceSides < 2) errors.Add("movement.simpleMove.diceSides must be >= 2");
            if (Movement.DashMove.DiceCount < 1) errors.Add("movement.dashMove.diceCount must be >= 1");
            if (Movement.DashMove.DiceSides < 2) errors.Add("movement.dashMove.diceSides must be >= 2");

            if (errors.Count > 0)
                throw new InvalidOperationException("Invalid config:\n  " + string.Join("\n  ", errors));
        }
    }

    // === Config section classes ===

    public class CombatConfig
    {
        [JsonPropertyName("staminaCosts")]
        public StaminaCostsConfig StaminaCosts { get; set; } = new();

        [JsonPropertyName("restStaminaRestore")]
        public int RestStaminaRestore { get; set; } = 5;

        [JsonPropertyName("attackRange")]
        public int AttackRange { get; set; } = 1;

        [JsonPropertyName("counterGauge")]
        public CounterGaugeConfig CounterGauge { get; set; } = new();
    }

    public class StaminaCostsConfig
    {
        [JsonPropertyName("attack")]
        public int Attack { get; set; } = 3;

        [JsonPropertyName("defend")]
        public int Defend { get; set; } = 2;

        [JsonPropertyName("move")]
        public int Move { get; set; } = 1;
    }

    public class CounterGaugeConfig
    {
        [JsonPropertyName("maximum")]
        public int Maximum { get; set; } = 6;
    }

    public class CharacterConfig
    {
        [JsonPropertyName("defaults")]
        public CharacterDefaultsConfig Defaults { get; set; } = new();
    }

    public class CharacterDefaultsConfig
    {
        [JsonPropertyName("health")]
        public int Health { get; set; } = 100;

        [JsonPropertyName("stamina")]
        public int Stamina { get; set; } = 20;

        [JsonPropertyName("statValue")]
        public int StatValue { get; set; } = 10;
    }

    public class GridConfig
    {
        [JsonPropertyName("width")]
        public int Width { get; set; } = 64;

        [JsonPropertyName("height")]
        public int Height { get; set; } = 64;

        [JsonPropertyName("viewport")]
        public ViewportConfig Viewport { get; set; } = new();

        [JsonPropertyName("arena")]
        public ArenaConfig Arena { get; set; } = new();

        [JsonPropertyName("startingPositions")]
        public List<PositionConfig> StartingPositions { get; set; } = new()
        {
            new() { X = 62, Y = 62 },
            new() { X = 66, Y = 66 },
            new() { X = 62, Y = 66 },
            new() { X = 66, Y = 62 }
        };

        /// <summary>Check if a world position is inside the circular arena</summary>
        public bool IsValidArenaPosition(int x, int y)
        {
            double radius = Arena.Diameter / 2.0;
            double dx = x - Arena.CenterX;
            double dy = y - Arena.CenterY;
            return (dx * dx + dy * dy) <= (radius * radius);
        }

        /// <summary>Get viewport bounds centered on player (minX, maxX, minY, maxY)</summary>
        public (int minX, int maxX, int minY, int maxY) GetViewportBounds(int playerX, int playerY)
        {
            int halfW = Viewport.Width / 2;
            int halfH = Viewport.Height / 2;
            return (playerX - halfW, playerX + halfW, playerY - halfH, playerY + halfH);
        }

        /// <summary>Convert world coords to viewport coords. Returns null if outside viewport.</summary>
        public (int vx, int vy)? WorldToViewport(int playerX, int playerY, int worldX, int worldY)
        {
            var (minX, maxX, minY, maxY) = GetViewportBounds(playerX, playerY);
            if (worldX < minX || worldX > maxX || worldY < minY || worldY > maxY)
                return null;
            return (worldX - minX, worldY - minY);
        }

        /// <summary>Convert viewport coords back to world coords</summary>
        public (int wx, int wy) ViewportToWorld(int playerX, int playerY, int viewX, int viewY)
        {
            var (minX, _, minY, _) = GetViewportBounds(playerX, playerY);
            return (minX + viewX, minY + viewY);
        }

        public List<Position> GetStartingPositions()
        {
            var positions = new List<Position>();
            foreach (var p in StartingPositions)
                positions.Add(new Position(p.X, p.Y));
            return positions;
        }
    }

    public class ViewportConfig
    {
        [JsonPropertyName("width")]
        public int Width { get; set; } = 64;

        [JsonPropertyName("height")]
        public int Height { get; set; } = 64;
    }

    public class ArenaConfig
    {
        [JsonPropertyName("diameter")]
        public int Diameter { get; set; } = 128;

        [JsonPropertyName("centerX")]
        public int CenterX { get; set; } = 64;

        [JsonPropertyName("centerY")]
        public int CenterY { get; set; } = 64;
    }

    public class PositionConfig
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }
    }

    public class TurnConfig
    {
        [JsonPropertyName("maxActionsBase")]
        public int MaxActionsBase { get; set; } = 1;

        [JsonPropertyName("maxActionsAfterMove")]
        public int MaxActionsAfterMove { get; set; } = 2;

        [JsonPropertyName("forcedRestRestore")]
        public int ForcedRestRestore { get; set; } = 5;

        [JsonPropertyName("maxSkipAttempts")]
        public int MaxSkipAttempts { get; set; } = 10;
    }

    public class MovementConfig
    {
        [JsonPropertyName("simpleMove")]
        public DiceConfig SimpleMove { get; set; } = new() { DiceCount = 1, DiceSides = 6 };

        [JsonPropertyName("dashMove")]
        public DiceConfig DashMove { get; set; } = new() { DiceCount = 2, DiceSides = 6 };
    }

    public class DiceConfig
    {
        [JsonPropertyName("diceCount")]
        public int DiceCount { get; set; }

        [JsonPropertyName("diceSides")]
        public int DiceSides { get; set; }

        public override string ToString() => $"{DiceCount}d{DiceSides}";
    }
}