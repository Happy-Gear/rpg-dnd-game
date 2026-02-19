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

        /// <summary>
        /// Create a GridDisplay driven by balance.json values.
        /// </summary>
        public GridDisplay()
        {
            var cfg = GameConfig.Current.Grid;
            _arenaWidth  = cfg.Width;
            _arenaHeight = cfg.Height;
            _vpWidth     = cfg.Viewport.Width;
            _vpHeight    = cfg.Viewport.Height;
            _vpHalfW     = cfg.ViewportHalfW;
            _vpHalfH     = cfg.ViewportHalfH;
            _characters  = new List<Character>();
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
                return DrawFullGrid(); // Fallback: draw from (0,0) if no focus

            int focusX = focusCharacter.Position.X;
            int focusY = focusCharacter.Position.Y;

            // World coords of the top-left corner of the viewport
            int startX = focusX - _vpHalfW;
            int startY = focusY + _vpHalfH; // Y increases upward; top row = highest Y

            var sb = new StringBuilder();

            // Header: world position of focus + viewport info
            sb.AppendLine($"  Arena {_arenaWidth}×{_arenaHeight} | " +
                          $"View {_vpWidth}×{_vpHeight} | " +
                          $"Focus: {focusCharacter.Name} @ ({focusX},{focusY})");
            sb.AppendLine();

            // Top Y axis label
            sb.AppendLine($"  y={startY,4} ┌" + new string('─', _vpWidth * 4 - 1) + "┐");

            // Grid rows — top of viewport is highest Y (so we iterate downward)
            for (int row = 0; row < _vpHeight; row++)
            {
                int worldY = startY - row;
                bool isMiddleRow = (row == _vpHalfH);

                // Left axis label
                if (isMiddleRow)
                    sb.Append($"  y={worldY,4} ┤ ");
                else
                    sb.Append($"       │ ");

                // Columns
                for (int col = 0; col < _vpWidth; col++)
                {
                    int worldX = startX + col;
                    bool isCenter = (col == _vpHalfW && isMiddleRow);

                    char cell = GetCellChar(worldX, worldY, focusCharacter, isCenter);
                    sb.Append(cell);

                    if (col < _vpWidth - 1)
                        sb.Append("   "); // 3 spaces between tiles
                }

                sb.AppendLine(" │");

                if (row < _vpHeight - 1)
                    sb.AppendLine("       │" + new string(' ', _vpWidth * 4) + "│");
            }

            // Bottom border
            int bottomY = startY - (_vpHeight - 1);
            sb.AppendLine($"  y={bottomY,4} └" + new string('─', _vpWidth * 4 - 1) + "┘");

            // X axis labels (start, center, end)
            int endX = startX + _vpWidth - 1;
            sb.AppendLine($"         x={startX,-6}  x={focusX}  (center)  x={endX}");

            // Off-screen character indicators
            var offScreen = GetOffScreenIndicators(focusCharacter, startX, startY, endX, bottomY);
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

                    if (IsInArenaBounds(newPos) && !_characters.Any(c => c.Position.Equals(newPos)))
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

            // For preview purposes use the final path position (or current pos) as focus
            var previewPos = path != null && path.Count > 0 ? path.Last() : movingCharacter.Position;
            int focusX = movingCharacter.Position.X; // Keep focus on character's actual position
            int focusY = movingCharacter.Position.Y;

            int startX = focusX - _vpHalfW;
            int startY = focusY + _vpHalfH;

            var sb = new StringBuilder();
            sb.AppendLine($"  Arena {_arenaWidth}×{_arenaHeight} | " +
                          $"View {_vpWidth}×{_vpHeight} | " +
                          $"Moving: {movingCharacter.Name} @ ({focusX},{focusY})");
            sb.AppendLine();
            sb.AppendLine($"  y={startY,4} ┌" + new string('─', _vpWidth * 4 - 1) + "┐");

            for (int row = 0; row < _vpHeight; row++)
            {
                int worldY = startY - row;
                bool isMiddleRow = (row == _vpHalfH);

                if (isMiddleRow)
                    sb.Append($"  y={worldY,4} ┤ ");
                else
                    sb.Append($"       │ ");

                for (int col = 0; col < _vpWidth; col++)
                {
                    int worldX = startX + col;
                    char cell = GetCellCharWithPath(worldX, worldY, movingCharacter, startPosition, path);
                    sb.Append(cell);
                    if (col < _vpWidth - 1)
                        sb.Append("   ");
                }

                sb.AppendLine(" │");
                if (row < _vpHeight - 1)
                    sb.AppendLine("       │" + new string(' ', _vpWidth * 4) + "│");
            }

            int bottomY = startY - (_vpHeight - 1);
            sb.AppendLine($"  y={bottomY,4} └" + new string('─', _vpWidth * 4 - 1) + "┘");
            sb.AppendLine($"         x={startX,-6}  x={focusX}  (center)  x={startX + _vpWidth - 1}");

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
            if (!IsInArenaBounds(worldX, worldY))
                return '#';

            // Check if any character occupies this tile
            var occupant = _characters.FirstOrDefault(c => c.Position.X == worldX && c.Position.Y == worldY);
            if (occupant != null)
            {
                // Focus character shown in uppercase (always)
                // Other characters also uppercase — distinguishable by position context
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
            if (!IsInArenaBounds(worldX, worldY))
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
            int vpLeft, int vpTop, int vpRight, int vpBottom)
        {
            var result = new List<string>();

            foreach (var c in _characters.Where(c => c != focusCharacter))
            {
                int cx = c.Position.X;
                int cy = c.Position.Y;

                // Is this character inside the current viewport?
                if (cx >= vpLeft && cx <= vpRight && cy <= vpTop && cy >= vpBottom)
                    continue; // Visible — no indicator needed

                int dx = cx - focusCharacter.Position.X;
                int dy = cy - focusCharacter.Position.Y;
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

        private bool IsInArenaBounds(int x, int y)
            => x >= 0 && x < _arenaWidth && y >= 0 && y < _arenaHeight;

        private bool IsInArenaBounds(Position pos)
            => IsInArenaBounds(pos.X, pos.Y);
    }
}