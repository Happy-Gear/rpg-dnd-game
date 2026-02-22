using System;
using System.IO;
using RPGGame.Characters;
using RPGGame.Core;

namespace RPGGame
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("🎮 RPG D&D Combat Game - ASCII Edition");
            Console.WriteLine("=====================================\n");
            
            // Load game configuration
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "balance.json");
            if (File.Exists(configPath))
            {
                GameConfig.LoadFromFile(configPath);
                Console.WriteLine("✓ Loaded balance.json");
            }
            else
            {
                Console.WriteLine("⚠ balance.json not found — using defaults");
            }
            
            // Create test characters
            var alice = CreateCharacter("Alice", 1, 0, 0, 0, 0, 5); // ATK:1, DEF:0, MOV:1
            var bob = CreateCharacter("Bob", 1, 0, 0, 0, 0, 5);     // ATK:2, DEF:0, MOV:0
			var crl= CreateCharacter("Crl", 1, 0, 0, 0, 0, 5); 
			var dee= CreateCharacter("Dee", 1, 0, 0, 0, 0, 5); 
                        
            // Initialize game
            var game = new GameManager(8,8);
            
            // Start the game
            Console.WriteLine(game.StartGame(alice, bob,crl,dee));
            
            // Enhanced game loop with responsive controls
            while (game.GameActive)
            {
                try
                {
                    if (game.InMovementMode)
                    {
                        // Movement mode: Use responsive key input
                        HandleMovementMode(game);
                    }
                    else
                    {
                        // Normal mode: Use text commands
                        HandleNormalMode(game);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
            
            Console.WriteLine("\nThanks for playing!");
            Console.ReadKey();
        }
        
        /// <summary>
        /// Handle responsive WASD controls during movement mode
        /// </summary>
        static void HandleMovementMode(GameManager game)
        {
            Console.WriteLine("\n🎮 MOVEMENT MODE - Use WASD keys (no Enter needed)");
            Console.Write("Press key: ");
            
            // Read single key without Enter
            var keyInfo = Console.ReadKey(true); // true = don't display the key
            Console.WriteLine($"[{keyInfo.Key}]"); // Show what key was pressed
            
            // Handle special cases
            if (keyInfo.Key == ConsoleKey.Q)
            {
                Console.WriteLine("Exiting game...");
                Environment.Exit(0);
            }
            
            // Process the key input
            var result = game.ProcessKeyInput(keyInfo);
            Console.WriteLine(result);
        }
        
        /// <summary>
        /// Handle text commands during normal game mode
        /// </summary>
        static void HandleNormalMode(GameManager game)
        {
            Console.Write("\n> ");
            var input = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input))
                return;
                
            if (input.ToLower() == "quit" || input.ToLower() == "exit")
            {
                Console.WriteLine("Exiting game...");
                Environment.Exit(0);
            }
            
            // Hot-reload config during gameplay
            if (input.ToLower() == "reload")
            {
                if (GameConfig.Reload())
                    Console.WriteLine("✓ Config reloaded!");
                else
                    Console.WriteLine("✗ Reload failed — check balance.json");
                return;
            }
            
            // Process text command
            var result = game.ProcessAction(input);
            Console.WriteLine(result);
        }
        
        /// <summary>
        /// Create a character with specified stats
        /// </summary>
        static Character CreateCharacter(string name, int str, int end, int cha, int intel, int agi, int wis)
        {
            var stats = new CharacterStats(str, end, cha, intel, agi, wis);
            return new Character(name, stats, health: 10, stamina: 10);
        }
    }
}