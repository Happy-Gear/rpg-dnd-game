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
            
            // Alice: high DEF (Endurance = 6), low everything else
            // Bob: balanced attacker
            var alice = CreateCharacter("Alice", str: 1, end: 6, cha: 0, intel: 0, agi: 1, wis: 0);
            var bob   = CreateCharacter("Bob",   str: 3, end: 1, cha: 0, intel: 0, agi: 1, wis: 0);
                        
            var game = new GameManager(8, 8);
            Console.WriteLine(game.StartGame(alice, bob));
            
            while (game.GameActive)
            {
                try
                {
                    if (game.InMovementMode)
                        HandleMovementMode(game);
                    else
                        HandleNormalMode(game);
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
        
        static void HandleMovementMode(GameManager game)
        {
            Console.WriteLine("\n🎮 MOVEMENT MODE - Use WASD keys (no Enter needed)");
            Console.Write("Press key: ");
            var keyInfo = Console.ReadKey(true);
            Console.WriteLine($"[{keyInfo.Key}]");
            if (keyInfo.Key == ConsoleKey.Q) Environment.Exit(0);
            Console.WriteLine(game.ProcessKeyInput(keyInfo));
        }
        
        static void HandleNormalMode(GameManager game)
        {
            Console.Write("\n> ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) return;
            if (input.ToLower() is "quit" or "exit") Environment.Exit(0);
            if (input.ToLower() == "reload")
            {
                Console.WriteLine(GameConfig.Reload() ? "✓ Config reloaded!" : "✗ Reload failed");
                return;
            }
            Console.WriteLine(game.ProcessAction(input));
        }
        
        static Character CreateCharacter(string name, int str, int end, int cha, int intel, int agi, int wis)
        {
            var stats = new CharacterStats(str, end, cha, intel, agi, wis);
            return new Character(name, stats, health: 10, stamina: 10);
        }
    }
}