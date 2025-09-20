using System;
using RPGGame.Grid;

namespace RPGGame.Input
{
    /// <summary>
    /// Handles console input processing for game actions and movement
    /// </summary>
    public class InputHandler
    {
        /// <summary>
        /// Process a console key press and return the appropriate command
        /// </summary>
        public InputCommand ProcessKeyInput(ConsoleKeyInfo keyInfo)
        {
            // Handle movement keys
            if (IsMovementKey(keyInfo))
            {
                var direction = GetDirection(keyInfo);
                return new InputCommand
                {
                    Type = InputType.Movement,
                    Direction = direction,
                    Key = keyInfo.Key,
                    RawInput = keyInfo.KeyChar.ToString()
                };
            }
            
            // Handle movement mode control keys
            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    return new InputCommand
                    {
                        Type = InputType.Confirm,
                        Key = keyInfo.Key,
                        RawInput = "enter"
                    };
                    
                case ConsoleKey.Escape:
                    return new InputCommand
                    {
                        Type = InputType.Cancel,
                        Key = keyInfo.Key,
                        RawInput = "escape"
                    };
                    
                case ConsoleKey.R:
                    return new InputCommand
                    {
                        Type = InputType.Reset,
                        Key = keyInfo.Key,
                        RawInput = "r"
                    };
                    
                case ConsoleKey.H:
                    return new InputCommand
                    {
                        Type = InputType.Help,
                        Key = keyInfo.Key,
                        RawInput = "h"
                    };
                    
                case ConsoleKey.Spacebar:
                    return new InputCommand
                    {
                        Type = InputType.Confirm,
                        Key = keyInfo.Key,
                        RawInput = "space"
                    };
                    
                default:
                    return new InputCommand
                    {
                        Type = InputType.Invalid,
                        Key = keyInfo.Key,
                        RawInput = keyInfo.KeyChar.ToString()
                    };
            }
        }
        
        /// <summary>
        /// Process string-based input for game actions (legacy support)
        /// </summary>
        public GameActionCommand ProcessActionInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new GameActionCommand { Type = GameActionType.Invalid, RawInput = input };
            
            var parts = input.ToLower().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0];
            
            switch (command)
            {
                case "attack" or "atk":
                    return new GameActionCommand
                    {
                        Type = GameActionType.Attack,
                        Target = parts.Length > 1 ? parts[1] : null,
                        RawInput = input
                    };
                    
                case "move" or "mov":
                    if (parts.Length >= 3 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
                    {
                        return new GameActionCommand
                        {
                            Type = GameActionType.MoveToPosition,
                            TargetPosition = new Position(x, y),
                            RawInput = input
                        };
                    }
                    return new GameActionCommand
                    {
                        Type = GameActionType.EnterMovementMode,
                        RawInput = input
                    };
                    
                case "dash":
                    if (parts.Length >= 3 && int.TryParse(parts[1], out int dashX) && int.TryParse(parts[2], out int dashY))
                    {
                        return new GameActionCommand
                        {
                            Type = GameActionType.DashToPosition,
                            TargetPosition = new Position(dashX, dashY),
                            RawInput = input
                        };
                    }
                    return new GameActionCommand
                    {
                        Type = GameActionType.EnterDashMode,
                        RawInput = input
                    };
                    
                case "rest":
                    return new GameActionCommand
                    {
                        Type = GameActionType.Rest,
                        RawInput = input
                    };
                    
                case "defend" or "def":
                    return new GameActionCommand
                    {
                        Type = GameActionType.Defend,
                        RawInput = input
                    };
                    
                case "help" or "h" or "?":
                    return new GameActionCommand
                    {
                        Type = GameActionType.Help,
                        RawInput = input
                    };
                    
                case "quit" or "exit":
                    return new GameActionCommand
                    {
                        Type = GameActionType.Quit,
                        RawInput = input
                    };
                    
                // Defense choices
                case "take":
                    return new GameActionCommand
                    {
                        Type = GameActionType.DefenseTakeDamage,
                        RawInput = input
                    };
                    
                // Movement mode commands (string fallback)
                case "w" or "up":
                    return new GameActionCommand
                    {
                        Type = GameActionType.MovementStep,
                        Direction = Direction.Up,
                        RawInput = input
                    };
                    
                case "s" or "down":
                    return new GameActionCommand
                    {
                        Type = GameActionType.MovementStep,
                        Direction = Direction.Down,
                        RawInput = input
                    };
                    
                case "a" or "left":
                    return new GameActionCommand
                    {
                        Type = GameActionType.MovementStep,
                        Direction = Direction.Left,
                        RawInput = input
                    };
                    
                case "d" or "right":
                    return new GameActionCommand
                    {
                        Type = GameActionType.MovementStep,
                        Direction = Direction.Right,
                        RawInput = input
                    };
                    
                case "enter" or "confirm":
                    return new GameActionCommand
                    {
                        Type = GameActionType.Confirm,
                        RawInput = input
                    };
                    
                case "r" or "reset":
                    return new GameActionCommand
                    {
                        Type = GameActionType.Reset,
                        RawInput = input
                    };
                    
                case "esc" or "escape" or "cancel":
                    return new GameActionCommand
                    {
                        Type = GameActionType.Cancel,
                        RawInput = input
                    };
                    
                case "skip" or "continue":
                    return new GameActionCommand
                    {
                        Type = GameActionType.Skip,
                        RawInput = input
                    };
                    
                default:
                    // Try to parse coordinates
                    if (parts.Length == 2 && int.TryParse(parts[0], out int coordX) && int.TryParse(parts[1], out int coordY))
                    {
                        return new GameActionCommand
                        {
                            Type = GameActionType.Coordinates,
                            TargetPosition = new Position(coordX, coordY),
                            RawInput = input
                        };
                    }
                    
                    return new GameActionCommand
                    {
                        Type = GameActionType.Invalid,
                        RawInput = input
                    };
            }
        }
        
        /// <summary>
        /// Check if a key is a movement key (WASD or arrows)
        /// </summary>
        public bool IsMovementKey(ConsoleKeyInfo keyInfo)
        {
            return keyInfo.Key switch
            {
                ConsoleKey.W or ConsoleKey.UpArrow => true,
                ConsoleKey.A or ConsoleKey.LeftArrow => true,
                ConsoleKey.S or ConsoleKey.DownArrow => true,
                ConsoleKey.D or ConsoleKey.RightArrow => true,
                _ => false
            };
        }
        
        /// <summary>
        /// Get direction from movement key
        /// </summary>
        public Direction GetDirection(ConsoleKeyInfo keyInfo)
        {
            return keyInfo.Key switch
            {
                ConsoleKey.W or ConsoleKey.UpArrow => Direction.Up,
                ConsoleKey.A or ConsoleKey.LeftArrow => Direction.Left,
                ConsoleKey.S or ConsoleKey.DownArrow => Direction.Down,
                ConsoleKey.D or ConsoleKey.RightArrow => Direction.Right,
                _ => Direction.None
            };
        }
        
        /// <summary>
        /// Get delta coordinates for a direction
        /// </summary>
        public (int deltaX, int deltaY) GetDirectionDelta(Direction direction)
        {
            return direction switch
            {
                Direction.Up => (0, 1),
                Direction.Down => (0, -1),
                Direction.Left => (-1, 0),
                Direction.Right => (1, 0),
                _ => (0, 0)
            };
        }
        
        /// <summary>
        /// Check if input is a defense choice
        /// </summary>
        public bool IsDefenseChoice(string input)
        {
            var normalized = input.ToLower().Trim();
            return normalized == "defend" || normalized == "def" || 
                   normalized == "move" || normalized == "mov" || 
                   normalized == "take";
        }
        
        /// <summary>
        /// Parse defense choice from input
        /// </summary>
        public RPGGame.Combat.DefenseChoice? ParseDefenseChoice(string input)
        {
            return input.ToLower().Trim() switch
            {
                "defend" or "def" => RPGGame.Combat.DefenseChoice.Defend,
                "move" or "mov" => RPGGame.Combat.DefenseChoice.Move,
                "take" => RPGGame.Combat.DefenseChoice.TakeDamage,
                _ => null
            };
        }
    }
    
    /// <summary>
    /// Represents a processed input command for movement mode
    /// </summary>
    public class InputCommand
    {
        public InputType Type { get; set; }
        public Direction Direction { get; set; } = Direction.None;
        public ConsoleKey Key { get; set; }
        public string RawInput { get; set; }
        
        public bool IsMovement => Type == InputType.Movement;
        public bool IsControl => Type == InputType.Confirm || Type == InputType.Cancel || Type == InputType.Reset;
        
        public override string ToString()
        {
            return $"{Type}" + (Direction != Direction.None ? $"({Direction})" : "");
        }
    }
    
    /// <summary>
    /// Represents a processed game action command
    /// </summary>
    public class GameActionCommand
    {
        public GameActionType Type { get; set; }
        public string Target { get; set; }
        public Position TargetPosition { get; set; }
        public Direction Direction { get; set; } = Direction.None;
        public string RawInput { get; set; }
        
        public override string ToString()
        {
            string result = Type.ToString();
            if (!string.IsNullOrEmpty(Target)) result += $"({Target})";
            if (TargetPosition != null) result += $"({TargetPosition})";
            if (Direction != Direction.None) result += $"({Direction})";
            return result;
        }
    }
    
    /// <summary>
    /// Types of input commands for movement mode
    /// </summary>
    public enum InputType
    {
        Movement,    // WASD or arrow keys
        Confirm,     // Enter or Space
        Cancel,      // Escape
        Reset,       // R key
        Help,        // H key
        Invalid      // Unrecognized input
    }
    
    /// <summary>
    /// Types of game action commands
    /// </summary>
    public enum GameActionType
    {
        // Combat actions
        Attack,
        Defend,
        Rest,
        
        // Movement actions
        EnterMovementMode,     // "move" - enter WASD mode
        EnterDashMode,         // "dash" - enter WASD dash mode
        MoveToPosition,        // "move x y" - direct move
        DashToPosition,        // "dash x y" - direct dash
        MovementStep,          // WASD step in movement mode
        
        // Movement controls
        Confirm,               // Confirm path
        Cancel,                // Cancel movement
        Reset,                 // Reset path
        Skip,                  // Skip optional action
        
        // Defense choices
        DefenseTakeDamage,     // "take" defense choice
        
        // Utility
        Help,                  // Show help
        Quit,                  // Exit game
        Coordinates,           // Raw "x y" coordinates
        Invalid                // Unrecognized input
    }
    
    /// <summary>
    /// Movement directions
    /// </summary>
    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right
    }
}