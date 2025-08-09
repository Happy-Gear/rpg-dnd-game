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
			var alice = CreateCharacter("Alice", 1, 0, 5, 5, 1, 5); // ATK:1, DEF:0, MOV:1
			var bob = CreateCharacter("Bob", 2, 0, 5, 5, 0, 5);     // ATK:2, DEF:0, MOV:0
						
            // Initialize game
            var game = new GameManager(16, 16);
            
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
        
		static Character CreateCharacter(string name, int str, int end, int cha, int intel, int agi, int wis)
		{
			var stats = new CharacterStats(str, end, cha, intel, agi, wis);
			return new Character(name, stats, health: 10, stamina: 10);
		}
    }
}
