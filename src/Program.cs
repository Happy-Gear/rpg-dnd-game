using System;
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
            
            // Create test characters
            var alice = CreateCharacter("Alice", 12, 10, 8, 9, 11);
            var bob = CreateCharacter("Bob", 14, 12, 6, 7, 8);
            
            // Initialize game
            var game = new GameManager(10, 8);
            
            // Start the game
            Console.WriteLine(game.StartGame(alice, bob));
            
            // Game loop
            while (game.GameActive)
            {
                Console.Write("\n> ");
                var input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input))
                    continue;
                    
                if (input.ToLower() == "quit" || input.ToLower() == "exit")
                    break;
                
                // Process game action
                try
                {
                    var result = game.ProcessAction(input);
                    Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            
            Console.WriteLine("\nThanks for playing!");
            Console.ReadKey();
        }
        
        static Character CreateCharacter(string name, int str, int end, int cha, int intel, int agi)
        {
            var stats = new CharacterStats(str, end, cha, intel, agi);
            return new Character(name, stats, health: 50, stamina: 15);
        }
    }
}
