using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Characters;
using RPGGame.Grid;

namespace RPGGame.Core
{
    /// <summary>
    /// Controls where the camera/viewport is focused.
    /// Sits above ViewportSystem — decides WHAT to focus on,
    /// ViewportSystem handles the coordinate math from that focus point.
    /// 
    /// Designed to support:
    /// - Single player: follow current actor each turn
    /// - Spectator: lock to a specific character or switch freely
    /// - Multiplayer: each client only sees their own character's view
    /// - Tactical: midpoint between two characters (relative view)
    /// - Lock-on: camera sits between you and your target
    /// </summary>
    public class CameraController
    {
        private List<Character> _characters;
        private Character _lockedTarget;      // Used by LockOnTarget mode
        private Character _focusCharacter;    // Used by FollowCharacter mode
        private CameraMode _mode;
        private string _observingPlayerId;    // Multiplayer: which player owns this view

        // For smoothing future lerp support (positions stored as floats)
        private float _currentFocusX;
        private float _currentFocusY;

        public CameraMode Mode => _mode;
        public Character FocusCharacter => _focusCharacter;
        public Character LockedTarget => _lockedTarget;

        public CameraController(List<Character> characters)
        {
            _characters = characters;
            _mode = CameraMode.FollowCurrentActor;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Mode setters
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Follow whichever character's turn it currently is.
        /// Default mode for single player hotseat.
        /// </summary>
        public void SetFollowCurrentActor()
        {
            _mode = CameraMode.FollowCurrentActor;
            _focusCharacter = null;
            _lockedTarget = null;
        }

        /// <summary>
        /// Lock camera to a specific character regardless of whose turn it is.
        /// Useful for spectators wanting to watch a specific player,
        /// or multiplayer clients who always follow their own character.
        /// </summary>
        public void SetFollowCharacter(Character character)
        {
            _mode = CameraMode.FollowCharacter;
            _focusCharacter = character ?? throw new ArgumentNullException(nameof(character));
            _lockedTarget = null;
        }

        /// <summary>
        /// Camera sits at the midpoint between two characters.
        /// Useful for 1v1 duels — both fighters stay visible.
        /// As they approach each other the view tightens naturally.
        /// </summary>
        public void SetMidpoint(Character characterA, Character characterB)
        {
            _mode = CameraMode.Midpoint;
            _focusCharacter = characterA ?? throw new ArgumentNullException(nameof(characterA));
            _lockedTarget = characterB ?? throw new ArgumentNullException(nameof(characterB));
        }

        /// <summary>
        /// Camera sits between the focus character and their locked target.
        /// The bias controls how close to the focus vs the target the camera sits.
        /// bias 0.5 = exact midpoint, bias 0.0 = follow focus only, bias 1.0 = follow target only.
        /// </summary>
        public void SetLockOn(Character focus, Character target, float bias = 0.5f)
        {
            _mode = CameraMode.LockOnTarget;
            _focusCharacter = focus ?? throw new ArgumentNullException(nameof(focus));
            _lockedTarget = target ?? throw new ArgumentNullException(nameof(target));
            LockOnBias = Math.Clamp(bias, 0f, 1f);
        }

        /// <summary>
        /// Multiplayer private view: this client can only see from their character's perspective.
        /// Other characters outside their viewport are hidden (shown as '?' instead of letter).
        /// </summary>
        public void SetPrivateView(Character ownCharacter, string playerId)
        {
            _mode = CameraMode.PrivateView;
            _focusCharacter = ownCharacter ?? throw new ArgumentNullException(nameof(ownCharacter));
            _lockedTarget = null;
            _observingPlayerId = playerId;
        }

        /// <summary>
        /// Fixed camera position — does not move.
        /// Useful for overview/spectator mode or small arenas.
        /// </summary>
        public void SetFixed(int worldX, int worldY)
        {
            _mode = CameraMode.Fixed;
            _currentFocusX = worldX;
            _currentFocusY = worldY;
            _focusCharacter = null;
            _lockedTarget = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Focus point calculation
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Bias for LockOn mode. 0.5 = exact midpoint between focus and target.
        /// </summary>
        public float LockOnBias { get; private set; } = 0.5f;

        /// <summary>
        /// Get the current world-coordinate focus point for the viewport.
        /// This is what ViewportSystem.UpdatePlayerPosition() should receive each frame.
        /// </summary>
        public (int x, int y) GetFocusPoint(Character currentActor = null)
        {
            switch (_mode)
            {
                case CameraMode.FollowCurrentActor:
                    var actor = currentActor ?? _characters.FirstOrDefault(c => c.IsAlive);
                    return actor != null ? (actor.Position.X, actor.Position.Y) : (0, 0);

                case CameraMode.FollowCharacter:
                case CameraMode.PrivateView:
                    if (_focusCharacter == null || !_focusCharacter.IsAlive)
                        goto case CameraMode.FollowCurrentActor;
                    return (_focusCharacter.Position.X, _focusCharacter.Position.Y);

                case CameraMode.Midpoint:
                    return GetMidpoint(_focusCharacter, _lockedTarget);

                case CameraMode.LockOnTarget:
                    return GetBiasedPoint(_focusCharacter, _lockedTarget, LockOnBias);

                case CameraMode.Fixed:
                    return ((int)_currentFocusX, (int)_currentFocusY);

                default:
                    return (0, 0);
            }
        }

        /// <summary>
        /// In PrivateView mode, should this character's information be hidden?
        /// Other players' exact positions are obscured if they're not in visible range.
        /// </summary>
        public bool ShouldHideCharacter(Character character, ViewportSystem viewport)
        {
            if (_mode != CameraMode.PrivateView) return false;
            if (character == _focusCharacter) return false;

            // Hide characters not in viewport (their position is unknown to this player)
            return !viewport.IsPositionInViewport(character.Position.X, character.Position.Y);
        }

        /// <summary>
        /// Get display character for a position, respecting PrivateView hiding rules.
        /// </summary>
        public char? GetDisplayChar(
            int worldX, int worldY,
            List<Character> characters,
            ViewportSystem viewport)
        {
            var baseChar = viewport.GetDisplayCharacter(worldX, worldY, characters);

            if (_mode == CameraMode.PrivateView && baseChar.HasValue
                && char.IsLetter(baseChar.Value))
            {
                // Find which character this is
                var ch = characters?.Find(c =>
                    c.Position.X == worldX && c.Position.Y == worldY);

                if (ch != null && ShouldHideCharacter(ch, viewport))
                    return '?'; // Hidden — player can't see them from here
            }

            return baseChar;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Status / description helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Human-readable description of the current camera mode (for UI display).
        /// </summary>
        public string GetModeDescription()
        {
            return _mode switch
            {
                CameraMode.FollowCurrentActor => "Following current actor",
                CameraMode.FollowCharacter    => $"Following {_focusCharacter?.Name ?? "?"}",
                CameraMode.Midpoint           => $"Midpoint: {_focusCharacter?.Name} ↔ {_lockedTarget?.Name}",
                CameraMode.LockOnTarget       => $"Lock-on: {_focusCharacter?.Name} → {_lockedTarget?.Name} (bias {LockOnBias:F1})",
                CameraMode.PrivateView        => $"Private view: {_focusCharacter?.Name}",
                CameraMode.Fixed              => $"Fixed @ ({(int)_currentFocusX},{(int)_currentFocusY})",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Distance between the two tracked characters (useful for zoom scaling later).
        /// Returns -1 if not in a two-character mode.
        /// </summary>
        public double GetTrackedDistance()
        {
            if (_focusCharacter == null || _lockedTarget == null) return -1;
            return _focusCharacter.Position.DistanceTo(_lockedTarget.Position);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Update
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Call this when the character list changes (death, new players, etc.)
        /// </summary>
        public void UpdateCharacters(List<Character> characters)
        {
            _characters = characters;

            // If locked character died, fall back to following current actor
            if (_focusCharacter != null && !_focusCharacter.IsAlive)
            {
                SetFollowCurrentActor();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        private static (int x, int y) GetMidpoint(Character a, Character b)
        {
            if (a == null || b == null) return (0, 0);
            return (
                (a.Position.X + b.Position.X) / 2,
                (a.Position.Y + b.Position.Y) / 2
            );
        }

        private static (int x, int y) GetBiasedPoint(Character focus, Character target, float bias)
        {
            if (focus == null || target == null) return (0, 0);
            int x = (int)Math.Round(focus.Position.X + (target.Position.X - focus.Position.X) * bias);
            int y = (int)Math.Round(focus.Position.Y + (target.Position.Y - focus.Position.Y) * bias);
            return (x, y);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Camera mode enum
    // ─────────────────────────────────────────────────────────────────────────

    public enum CameraMode
    {
        /// <summary>Camera follows whoever's turn it currently is (default hotseat)</summary>
        FollowCurrentActor,

        /// <summary>Camera locked to a specific character regardless of turn order</summary>
        FollowCharacter,

        /// <summary>Camera sits at the midpoint between two characters</summary>
        Midpoint,

        /// <summary>Camera biased between a focus character and their target</summary>
        LockOnTarget,

        /// <summary>Multiplayer: camera follows own character, others hidden when off-screen</summary>
        PrivateView,

        /// <summary>Camera does not move — fixed world coordinate</summary>
        Fixed
    }
}