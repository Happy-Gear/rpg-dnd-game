using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RPGGame.Characters;
using RPGGame.Core;
using RPGGame.Grid;

namespace RPGGame.Display
{
    /// <summary>
    /// ASCII grid display with a camera/viewport system.
    /// The active character is always centered in the terminal view.
    /// The full arena can be much larger than what is visible at any time.
    /// </summary>
    public class GridDisplay
    {
        // Arena dimensions (full world size)
        private readonly int _arenaWidth;
        private readonly int _arenaHeight;

        // Viewport dimensions (terminal window size, should be odd)
        private readonly int _vpWidth;
        private readonly int _vpHeight;
        private readonly int _vpHalfW;
        private readonly int _vpHalfH;

        private List<Character> _characters;

        // Camera / coordinate transform system
        private readonly ViewportSystem _viewport;
        private readonly CameraController _camera;

        /// <summary>Exposes camera so GameManager can change modes (follow, midpoint, lock-on, etc.)</summary>
        public CameraController Camera => _camera;

        /// <summary>Exposes viewport for direct coordinate queries.</summary>
        public ViewportSystem Viewport => _viewport;

        /// <summary>
        /// Create a GridDisplay driven by balance.json values.
        /// </summary>
        public GridDisplay()
        {
            var cfg      = GameConfig.Current.Grid;
            _arenaWidth  = cfg.Width;
            _arenaHeight = cfg.Height;
            _vpWidth     = cfg.Viewport.Width;
            _vpHeight    = cfg.Viewport.Height;
            _vpHalfW     = cfg.ViewportHalfW;
            _vpHalfH     = cfg.ViewportHalfH;
            _characters  = new List<Character>();
            _viewport    = new ViewportSystem();
            _camera      = new CameraController(_characters);
        }

        /// <summary>
        /// Legacy constructor — still accepted but dimensions now come from config.
        /// The width/height params are ignored; left in so existing call sites compile.
        /// </summary>
        public GridDisplay(int ignoredWidth, int ignoredHeight) : this() { }

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        public void UpdateCharacters(List<Character> characters)
        {
            _characters = characters?.Where(c => c.IsAlive).ToList() ?? new List<Character>();
            _camera.UpdateCharacters(_characters);
        }

        /// <summary>
        /// Create a full display frame: viewport grid + status panel + info text.
        /// The viewport is centered on the first living character (or the current actor
        /// if you pass one explicitly via CreateFullDisplay overload).
        /// </summary>
        public string CreateFullDisplay(string additionalInfo = "")
        {
            var focus = _characters.FirstOrDefault();
            return CreateFullDisplay(focus, additionalInfo);
        }

        /// <summary>
        /// Create a full display frame centered on a specific character.
        /// </summary>
        public string CreateFullDisplay(Character focusCharacter, string additionalInfo = "")
        {
            var sb = new StringBuilder();
            sb.AppendLine("╔═══════════════════ RPG COMBAT GRID ═══════════════════╗");
            sb.AppendLine();
            sb.Append(DrawViewport(focusCharacter));
            sb.AppendLine();
            sb.Append(DrawCombatStatus());

            if (!string.IsNullOrEmpty(additionalInfo))
            {
                sb.AppendLine();
                sb.AppendLine("=== GAME INFO ===");
                sb.AppendLine(additionalInfo);
            }

            sb.AppendLine("╚══════════════════════════════════════════════════════╝");
            return sb.ToString();
        }

        /// <summary>
        /// Draw the viewport grid centered on focusCharacter.
        /// Characters outside the viewport are shown as off-screen indicators below.
        /// </summary>
        public string DrawViewport(Character focusCharacter)
        {
            if (focusCharacter == null)
                return DrawFullGrid();

            // 1. Ask CameraController where to focus
            var (focusX, focusY) = _camera.GetFocusPoint(focusCharacter);

            // 2. Tell ViewportSystem to center there
            _viewport.UpdatePlayerPosition(focusX, focusY);

            // 3. Get bounds from ViewportSystem (it owns the math now)
            var (minX, maxX, minY, maxY) = _viewport.GetViewportBoundsWorldCoords();

            var sb = new StringBuilder();

            sb.AppendLine($"  Arena {_arenaWidth}×{_arenaHeight} | " +
                          $"View {_vpWidth}×{_vpHeight} | " +
                          $"{_camera.GetModeDescription()} @ ({focusX},{focusY})");
            sb.AppendLine();

            // Top border
            sb.AppendLine($"  {maxY,4} ┌" + new string('─', _vpWidth * 2 - 1) + "┐");

            // Rows from top (maxY) to bottom (minY)
            for (int worldY = maxY; worldY >= minY; worldY--)
            {
                sb.Append($"  {worldY,4} │ ");

                for (int worldX = minX; worldX <= maxX; worldX++)
                {
                    bool isCenter = (worldX == focusX && worldY == focusY);
                    char cell = GetCellChar(worldX, worldY, focusCharacter, isCenter);
                    sb.Append(cell);
                    if (worldX < maxX) sb.Append(" ");
                }

                sb.AppendLine(" │");
            }

            // Bottom border
            sb.AppendLine($"  {minY,4} └" + new string('─', _vpWidth * 2 - 1) + "┘");
            sb.AppendLine($"       x={minX,-6} x={focusX}(center) x={maxX}");

            // Off-screen indicators
            var offScreen = GetOffScreenIndicators(focusCharacter, focusX, focusY, minX, maxX, minY, maxY);
            if (offScreen.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("  Off-screen:");
                foreach (var line in offScreen)
                    sb.AppendLine($"    {line}");
            }

            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Attack range helpers (unchanged API, arena-aware)
        // ─────────────────────────────────────────────────────────────────────

        public string DrawAttackRange(Character attacker)
        {
            if (attacker == null) return "";

            var targets = GetCharactersInAttackRange(attacker);

            if (!targets.Any())
                return $"{attacker.Name} has no valid targets in attack range (adjacent positions).\n";

            var sb = new StringBuilder();
            sb.AppendLine($"{attacker.Name} can attack:");
            foreach (var target in targets)
            {
                char symbol = char.ToUpper(target.Name[0]);
                double distance = attacker.Position.DistanceTo(target.Position);
                sb.AppendLine($"  [{symbol}] {target.Name} at ({target.Position.X},{target.Position.Y}) - Distance: {distance:F1}");
            }
            return sb.ToString();
        }

        public List<Character> GetCharactersInAttackRange(Character attacker)
        {
            if (attacker == null) return new List<Character>();
            return _characters
                .Where(c => c != attacker && c.IsAlive && IsInAttackRange(attacker.Position, c.Position))
                .ToList();
        }

        public bool IsInAttackRange(Position attackerPos, Position targetPos)
        {
            int dx = Math.Abs(attackerPos.X - targetPos.X);
            int dy = Math.Abs(attackerPos.Y - targetPos.Y);
            return dx <= 1 && dy <= 1 && !(dx == 0 && dy == 0);
        }

        public List<Position> GetValidMovePositions(Character character)
        {
            var positions = new List<Position>();
            var cur = character.Position;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var newPos = new Position(cur.X + dx, cur.Y + dy);

                    if (_viewport.IsValidArenaPosition(newPos.X, newPos.Y) &&
                        !_characters.Any(c => c.Position.Equals(newPos)))
                        positions.Add(newPos);
                }
            }
            return positions;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Combat status panel
        // ─────────────────────────────────────────────────────────────────────

        public string DrawCombatStatus()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== COMBAT STATUS ===");

            foreach (var character in _characters.OrderBy(c => c.Name))
            {
                char symbol = char.ToUpper(character.Name[0]);
                string counter = character.Counter.IsReady
                    ? " [COUNTER READY!]"
                    : $" (Counter: {character.Counter.Current}/{character.Counter.Maximum})";

                sb.AppendLine($"[{symbol}] {character.Name}: " +
                              $"HP {character.CurrentHealth}/{character.MaxHealth}, " +
                              $"SP {character.CurrentStamina}/{character.MaxStamina}" +
                              $"{counter}");

                sb.AppendLine($"    Position: ({character.Position.X},{character.Position.Y}) " +
                              $"ATK:{character.AttackPoints} DEF:{character.DefensePoints} MOV:{character.MovementPoints}");
            }
            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Movement preview viewport (used during WASD movement mode)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Draw viewport with live movement path preview.
        /// movingCharacter is centered; path breadcrumbs and final position are shown.
        /// </summary>
        public string DrawViewportWithMovementPreview(
            Character movingCharacter,
            Position startPosition,
            List<Position> path)
        {
            if (movingCharacter == null) return "";

            // During movement, always follow the moving character directly
            _viewport.UpdatePlayerPosition(movingCharacter.Position.X, movingCharacter.Position.Y);

            var (minX, maxX, minY, maxY) = _viewport.GetViewportBoundsWorldCoords();
            int focusX = movingCharacter.Position.X;
            int focusY = movingCharacter.Position.Y;

            var sb = new StringBuilder();
            sb.AppendLine($"  Arena {_arenaWidth}×{_arenaHeight} | " +
                          $"View {_vpWidth}×{_vpHeight} | " +
                          $"Moving: {movingCharacter.Name} @ ({focusX},{focusY})");
            sb.AppendLine();
            sb.AppendLine($"  {maxY,4} ┌" + new string('─', _vpWidth * 2 - 1) + "┐");

            for (int worldY = maxY; worldY >= minY; worldY--)
            {
                sb.Append($"  {worldY,4} │ ");

                for (int worldX = minX; worldX <= maxX; worldX++)
                {
                    char cell = GetCellCharWithPath(worldX, worldY, movingCharacter, startPosition, path);
                    sb.Append(cell);
                    if (worldX < maxX) sb.Append(" ");
                }

                sb.AppendLine(" │");
            }

            sb.AppendLine($"  {minY,4} └" + new string('─', _vpWidth * 2 - 1) + "┘");
            sb.AppendLine($"       x={minX,-6} x={focusX}(center) x={maxX}");

            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Get the display character for a world tile, with the focus character at center.
        /// </summary>
        private char GetCellChar(int worldX, int worldY, Character focusCharacter, bool isExactCenter)
        {
            // Out of arena bounds — show wall
            if (!_viewport.IsValidArenaPosition(worldX, worldY))
                return '#';

            // Check if any character occupies this tile
            var occupant = _characters.FirstOrDefault(c => c.Position.X == worldX && c.Position.Y == worldY);
            if (occupant != null)
            {
                // In PrivateView mode, hide off-screen enemies as '?'
                if (_camera.Mode == CameraMode.PrivateView &&
                    _camera.ShouldHideCharacter(occupant, _viewport))
                    return '?';

                return char.ToUpper(occupant.Name[0]);
            }

            // Center crosshair (empty center tile)
            if (isExactCenter)
                return '+';

            return '.';
        }

        /// <summary>
        /// Get the display character for a world tile during movement preview.
        /// </summary>
        private char GetCellCharWithPath(
            int worldX, int worldY,
            Character movingCharacter,
            Position startPosition,
            List<Position> path)
        {
            if (!_viewport.IsValidArenaPosition(worldX, worldY))
                return '#';

            if (path != null && path.Count > 0)
            {
                var finalPos = path.Last();
                if (finalPos.X == worldX && finalPos.Y == worldY)
                    return char.ToUpper(movingCharacter.Name[0]); // Destination

                // Breadcrumb trail (all path steps except final)
                if (path.Take(path.Count - 1).Any(p => p.X == worldX && p.Y == worldY))
                    return '·';
            }
            else
            {
                // No path yet — show character at current position
                if (movingCharacter.Position.X == worldX && movingCharacter.Position.Y == worldY)
                    return char.ToUpper(movingCharacter.Name[0]);
            }

            // Show other characters
            var other = _characters.FirstOrDefault(c =>
                c != movingCharacter && c.IsAlive && c.Position.X == worldX && c.Position.Y == worldY);
            if (other != null)
                return char.ToUpper(other.Name[0]);

            // Show start position as empty circle if character has moved
            if (path != null && path.Count > 0 &&
                startPosition != null &&
                startPosition.X == worldX && startPosition.Y == worldY)
                return '○';

            return '.';
        }

        /// <summary>
        /// Build off-screen character indicators with compass direction and tile distance.
        /// </summary>
        private List<string> GetOffScreenIndicators(
            Character focusCharacter,
            int focusX, int focusY,
            int minX, int maxX, int minY, int maxY)
        {
            var result = new List<string>();

            foreach (var c in _characters.Where(c => c != focusCharacter))
            {
                int cx = c.Position.X;
                int cy = c.Position.Y;

                // Already visible — no indicator needed
                if (cx >= minX && cx <= maxX && cy >= minY && cy <= maxY)
                    continue;

                // PrivateView: hidden enemies show minimal info
                if (_camera.Mode == CameraMode.PrivateView &&
                    _camera.ShouldHideCharacter(c, _viewport))
                {
                    result.Add("[?] Unknown enemy detected nearby");
                    continue;
                }

                int dx   = cx - focusX;
                int dy   = cy - focusY;
                int dist = (int)Math.Round(Math.Sqrt(dx * dx + dy * dy));
                string compass = GetCompassDirection(dx, dy);

                result.Add($"[{char.ToUpper(c.Name[0])}] {c.Name}: {compass}  {dist} tiles  " +
                           $"({cx},{cy})  HP:{c.CurrentHealth}/{c.MaxHealth}");
            }

            return result;
        }

        /// <summary>
        /// Convert a dx/dy offset into a compass direction string (N, NE, E, etc.)
        /// </summary>
        private static string GetCompassDirection(int dx, int dy)
        {
            // dy positive = north (up), dy negative = south (down)
            double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
            // Convert to 0-360 where 0=East, going counter-clockwise
            if (angle < 0) angle += 360;

            // Map to 8 compass points
            return angle switch
            {
                >= 337.5 or < 22.5   => "E →",
                >= 22.5  and < 67.5  => "NE ↗",
                >= 67.5  and < 112.5 => "N ↑",
                >= 112.5 and < 157.5 => "NW ↖",
                >= 157.5 and < 202.5 => "W ←",
                >= 202.5 and < 247.5 => "SW ↙",
                >= 247.5 and < 292.5 => "S ↓",
                _                    => "SE ↘"
            };
        }

        /// <summary>
        /// Fallback: draw entire arena from (0,0) if no focus character is available.
        /// Only practical for tiny arenas; mostly used in tests.
        /// </summary>
        private string DrawFullGrid()
        {
            int drawW = Math.Min(_arenaWidth, _vpWidth);
            int drawH = Math.Min(_arenaHeight, _vpHeight);
            var sb = new StringBuilder();

            for (int y = drawH - 1; y >= 0; y--)
            {
                for (int x = 0; x < drawW; x++)
                {
                    var occupant = _characters.FirstOrDefault(c => c.Position.X == x && c.Position.Y == y);
                    sb.Append(occupant != null ? char.ToUpper(occupant.Name[0]) : '.');
                    if (x < drawW - 1) sb.Append("   ");
                }
                sb.AppendLine();
                if (y > 0) sb.AppendLine();
            }

            return sb.ToString();
        }

    }
}