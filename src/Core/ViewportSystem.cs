using System;
using RPGGame.Core;
using RPGGame.Grid;

namespace RPGGame.Display
{
    /// <summary>
    /// Viewport camera system that centers on the player and handles coordinate transformations.
    /// The player is always at the center of the 64x64 viewport, with the world behind it.
    /// Invalid positions (outside the circular arena) are marked differently in display.
    /// </summary>
    public class ViewportSystem
    {
        private int _playerX;
        private int _playerY;
        private int _viewportWidth;
        private int _viewportHeight;

        /// <summary>
        /// Initialize viewport with player position
        /// </summary>
        public ViewportSystem(int playerX = 64, int playerY = 64)
        {
            _playerX = playerX;
            _playerY = playerY;
            _viewportWidth = GameConfig.Current.Grid.Viewport.Width;
            _viewportHeight = GameConfig.Current.Grid.Viewport.Height;
        }

        /// <summary>
        /// Update player position (camera follows player)
        /// </summary>
        public void UpdatePlayerPosition(int worldX, int worldY)
        {
            _playerX = worldX;
            _playerY = worldY;
        }

        /// <summary>
        /// Get current player position (viewport center in world coords)
        /// </summary>
        public (int x, int y) GetPlayerWorldPosition() => (_playerX, _playerY);

        /// <summary>
        /// Get viewport center position in viewport coordinates (always middle of screen)
        /// </summary>
        public (int x, int y) GetPlayerViewportPosition()
        {
            return (_viewportWidth / 2, _viewportHeight / 2);
        }

        /// <summary>
        /// Convert world coordinates to viewport coordinates.
        /// Returns null if the world position is outside the viewport bounds.
        /// </summary>
        public (int viewX, int viewY)? WorldToViewport(int worldX, int worldY)
        {
            var (minX, maxX, minY, maxY) = GameConfig.Current.Grid.GetViewportBounds(_playerX, _playerY);

            // Check if position is within viewport bounds
            if (worldX < minX || worldX > maxX || worldY < minY || worldY > maxY)
                return null;

            // Convert to viewport coordinates
            int viewX = worldX - minX;
            int viewY = worldY - minY;
            return (viewX, viewY);
        }

        /// <summary>
        /// Convert viewport coordinates back to world coordinates
        /// </summary>
        public (int worldX, int worldY) ViewportToWorld(int viewX, int viewY)
        {
            var (minX, _, minY, _) = GameConfig.Current.Grid.GetViewportBounds(_playerX, _playerY);
            int worldX = minX + viewX;
            int worldY = minY + viewY;
            return (worldX, worldY);
        }

        /// <summary>
        /// Check if a world position is visible in the current viewport
        /// </summary>
        public bool IsPositionInViewport(int worldX, int worldY)
        {
            return WorldToViewport(worldX, worldY) != null;
        }

        /// <summary>
        /// Check if a position is valid (inside the circular arena)
        /// </summary>
        public bool IsValidArenaPosition(int worldX, int worldY)
        {
            return GameConfig.Current.Grid.IsValidArenaPosition(worldX, worldY);
        }

        /// <summary>
        /// Get the display character for a position in the viewport.
        /// Returns:
        /// - Character letter if a character occupies the space
        /// - '.' if empty and valid (inside arena)
        /// - '~' if empty and invalid (outside arena / in water)
        /// - null if position is outside viewport bounds
        /// </summary>
        public char? GetDisplayCharacter(int worldX, int worldY, System.Collections.Generic.List<Characters.Character> characters)
        {
            // First check if position is in viewport
            if (!IsPositionInViewport(worldX, worldY))
                return null;

            // Check if any character is at this position
            var character = characters?.Find(c => c.Position.X == worldX && c.Position.Y == worldY);
            if (character != null)
                return char.ToUpper(character.Name[0]);

            // Check if position is valid arena position
            if (IsValidArenaPosition(worldX, worldY))
                return '.'; // Valid empty tile
            else
                return '~'; // Invalid tile (outside arena / water)
        }

        /// <summary>
        /// Get viewport bounds in world coordinates
        /// </summary>
        public (int minX, int maxX, int minY, int maxY) GetViewportBoundsWorldCoords()
        {
            return GameConfig.Current.Grid.GetViewportBounds(_playerX, _playerY);
        }

        /// <summary>
        /// Calculate distance from player to a world position
        /// </summary>
        public int GetDistanceToPlayer(int worldX, int worldY)
        {
            int dx = worldX - _playerX;
            int dy = worldY - _playerY;
            // Chebyshev distance (max of absolute differences - typical for grid-based games)
            return Math.Max(Math.Abs(dx), Math.Abs(dy));
        }

        /// <summary>
        /// Check if a position is at the edge of the arena
        /// (useful for movement validation - prevent moves that would go outside)
        /// </summary>
        public bool IsAtArenaEdge(int worldX, int worldY, int checkRadius = 1)
        {
            // Check if position is valid but surrounding positions might not be
            if (!IsValidArenaPosition(worldX, worldY))
                return false;

            // Check 8 directions
            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            for (int i = 0; i < 8; i++)
            {
                int checkX = worldX + (dx[i] * checkRadius);
                int checkY = worldY + (dy[i] * checkRadius);
                
                if (!IsValidArenaPosition(checkX, checkY))
                    return true; // Found an invalid position nearby
            }

            return false; // All surrounding positions are valid
        }
    }
}