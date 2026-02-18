using System;
using System.IO;
using NUnit.Framework;
using RPGGame.Core;

namespace RPGGame.Tests.Unit.Core
{
    [TestFixture]
    [Category("Unit")]
    public class GameConfigTests
    {
        private const string ValidJson = @"{
            ""combat"": {
                ""staminaCosts"": { ""attack"": 3, ""defend"": 2, ""move"": 1 },
                ""restStaminaRestore"": 5,
                ""attackRange"": 1,
                ""counterGauge"": { ""maximum"": 6 }
            },
            ""characters"": {
                ""defaults"": { ""health"": 100, ""stamina"": 20, ""statValue"": 10 }
            },
            ""grid"": {
                ""width"": 8,
                ""height"": 8,
                ""startingPositions"": [
                    { ""x"": 2, ""y"": 2 },
                    { ""x"": 5, ""y"": 5 }
                ]
            },
            ""turns"": {
                ""maxActionsBase"": 1,
                ""maxActionsAfterMove"": 2,
                ""forcedRestRestore"": 5,
                ""maxSkipAttempts"": 10
            },
            ""movement"": {
                ""simpleMove"": { ""diceCount"": 1, ""diceSides"": 6 },
                ""dashMove"": { ""diceCount"": 2, ""diceSides"": 6 }
            }
        }";

        [SetUp]
        public void SetUp()
        {
            GameConfig.ResetToDefaults();
        }

        // === Loading ===

        [Test]
        public void Should_LoadFromJsonString_When_ValidJson()
        {
            var config = GameConfig.Load(ValidJson);

            Assert.That(config, Is.Not.Null);
            Assert.That(config.Combat.StaminaCosts.Attack, Is.EqualTo(3));
            Assert.That(config.Grid.Width, Is.EqualTo(8));
        }

        [Test]
        public void Should_LoadFromFile_When_FileExists()
        {
            var tempPath = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempPath, ValidJson);
                var config = GameConfig.LoadFromFile(tempPath);

                Assert.That(config, Is.Not.Null);
                Assert.That(config.Characters.Defaults.Health, Is.EqualTo(100));
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Test]
        public void Should_ThrowFileNotFound_When_FileMissing()
        {
            Assert.Throws<FileNotFoundException>(() =>
                GameConfig.LoadFromFile("/nonexistent/path/balance.json"));
        }

        [Test]
        public void Should_ThrowJsonException_When_MalformedJson()
        {
            Assert.Throws<System.Text.Json.JsonException>(() =>
                GameConfig.Load("{ invalid json }"));
        }

        [Test]
        public void Should_IgnoreUnknownProperties_When_FutureSectionsPresent()
        {
            var jsonWithFuture = ValidJson.TrimEnd('}') + @",
                ""_future"": { ""equipment"": {}, ""statusEffects"": {} }
            }";

            var config = GameConfig.Load(jsonWithFuture);
            Assert.That(config, Is.Not.Null);
            Assert.That(config.Combat.StaminaCosts.Attack, Is.EqualTo(3));
        }

        [Test]
        public void Should_AllowTrailingCommas_When_Loading()
        {
            var json = @"{ ""combat"": { ""staminaCosts"": { ""attack"": 3, ""defend"": 2, ""move"": 1, }, }, }";
            // Should not throw - trailing commas are allowed
            Assert.DoesNotThrow(() => GameConfig.Load(json));
        }

        // === Defaults ===

        [Test]
        public void Should_UseDefaults_When_NoJsonLoaded()
        {
            var config = GameConfig.Current;

            Assert.That(config.Combat.StaminaCosts.Attack, Is.EqualTo(3));
            Assert.That(config.Combat.StaminaCosts.Defend, Is.EqualTo(2));
            Assert.That(config.Combat.StaminaCosts.Move, Is.EqualTo(1));
            Assert.That(config.Combat.RestStaminaRestore, Is.EqualTo(5));
            Assert.That(config.Combat.AttackRange, Is.EqualTo(1));
            Assert.That(config.Combat.CounterGauge.Maximum, Is.EqualTo(6));
        }

        [Test]
        public void Should_UseCharacterDefaults_When_NoJsonLoaded()
        {
            var config = GameConfig.Current;

            Assert.That(config.Characters.Defaults.Health, Is.EqualTo(100));
            Assert.That(config.Characters.Defaults.Stamina, Is.EqualTo(20));
            Assert.That(config.Characters.Defaults.StatValue, Is.EqualTo(10));
        }

        [Test]
        public void Should_UseGridDefaults_When_NoJsonLoaded()
        {
            var config = GameConfig.Current;

            Assert.That(config.Grid.Width, Is.EqualTo(8));
            Assert.That(config.Grid.Height, Is.EqualTo(8));
            Assert.That(config.Grid.StartingPositions.Count, Is.EqualTo(4));
        }

        [Test]
        public void Should_UseTurnDefaults_When_NoJsonLoaded()
        {
            var config = GameConfig.Current;

            Assert.That(config.Turns.MaxActionsBase, Is.EqualTo(1));
            Assert.That(config.Turns.MaxActionsAfterMove, Is.EqualTo(2));
            Assert.That(config.Turns.ForcedRestRestore, Is.EqualTo(5));
            Assert.That(config.Turns.MaxSkipAttempts, Is.EqualTo(10));
        }

        [Test]
        public void Should_UseMovementDefaults_When_NoJsonLoaded()
        {
            var config = GameConfig.Current;

            Assert.That(config.Movement.SimpleMove.DiceCount, Is.EqualTo(1));
            Assert.That(config.Movement.SimpleMove.DiceSides, Is.EqualTo(6));
            Assert.That(config.Movement.DashMove.DiceCount, Is.EqualTo(2));
            Assert.That(config.Movement.DashMove.DiceSides, Is.EqualTo(6));
        }

        // === Singleton behavior ===

        [Test]
        public void Should_UpdateSingleton_When_LoadCalled()
        {
            var customJson = @"{
                ""combat"": { ""staminaCosts"": { ""attack"": 5, ""defend"": 2, ""move"": 1 } }
            }";

            GameConfig.Load(customJson);

            Assert.That(GameConfig.Current.Combat.StaminaCosts.Attack, Is.EqualTo(5));
        }

        [Test]
        public void Should_ResetToDefaults_When_ResetCalled()
        {
            var customJson = @"{ ""combat"": { ""attackRange"": 3 } }";
            GameConfig.Load(customJson);

            GameConfig.ResetToDefaults();

            Assert.That(GameConfig.Current.Combat.AttackRange, Is.EqualTo(1));
        }

        // === Validation ===

        [Test]
        public void Should_ThrowOnInvalidConfig_When_NegativeStaminaCost()
        {
            var json = @"{ ""combat"": { ""staminaCosts"": { ""attack"": -1, ""defend"": 2, ""move"": 1 } } }";

            Assert.Throws<InvalidOperationException>(() => GameConfig.Load(json));
        }

        [Test]
        public void Should_ThrowOnInvalidConfig_When_ZeroHealth()
        {
            var json = @"{ ""characters"": { ""defaults"": { ""health"": 0 } } }";

            Assert.Throws<InvalidOperationException>(() => GameConfig.Load(json));
        }

        [Test]
        public void Should_ThrowOnInvalidConfig_When_GridTooSmall()
        {
            var json = @"{ ""grid"": { ""width"": 1, ""height"": 1, ""startingPositions"": [{ ""x"": 0, ""y"": 0 }] } }";

            Assert.Throws<InvalidOperationException>(() => GameConfig.Load(json));
        }

        [Test]
        public void Should_ThrowOnInvalidConfig_When_StartingPositionOutOfBounds()
        {
            var json = @"{ ""grid"": { ""width"": 8, ""height"": 8, ""startingPositions"": [{ ""x"": 10, ""y"": 10 }] } }";

            Assert.Throws<InvalidOperationException>(() => GameConfig.Load(json));
        }

        [Test]
        public void Should_ThrowOnInvalidConfig_When_MaxActionsAfterMoveLessThanBase()
        {
            var json = @"{ ""turns"": { ""maxActionsBase"": 2, ""maxActionsAfterMove"": 1 } }";

            Assert.Throws<InvalidOperationException>(() => GameConfig.Load(json));
        }

        // === Grid helpers ===

        [Test]
        public void Should_ConvertStartingPositions_When_GetStartingPositionsCalled()
        {
            var config = GameConfig.Load(ValidJson);
            var positions = config.Grid.GetStartingPositions();

            Assert.That(positions.Count, Is.EqualTo(2));
            Assert.That(positions[0].X, Is.EqualTo(2));
            Assert.That(positions[0].Y, Is.EqualTo(2));
            Assert.That(positions[1].X, Is.EqualTo(5));
            Assert.That(positions[1].Y, Is.EqualTo(5));
        }

        // === Hot reload ===

        [Test]
        public void Should_Reload_When_FileChanged()
        {
            var tempPath = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempPath, ValidJson);
                GameConfig.LoadFromFile(tempPath);

                Assert.That(GameConfig.Current.Combat.AttackRange, Is.EqualTo(1));

                // "Change" the file
                var modifiedJson = ValidJson.Replace("\"attackRange\": 1", "\"attackRange\": 2");
                File.WriteAllText(tempPath, modifiedJson);

                var reloaded = GameConfig.Reload();

                Assert.That(reloaded, Is.True);
                Assert.That(GameConfig.Current.Combat.AttackRange, Is.EqualTo(2));
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Test]
        public void Should_ReturnFalse_When_ReloadWithNoSourcePath()
        {
            GameConfig.Load(ValidJson); // No file path

            Assert.That(GameConfig.Reload(), Is.False);
        }

        // === DiceConfig ===

        [Test]
        public void Should_FormatDiceString_When_ToStringCalled()
        {
            var config = GameConfig.Current;

            Assert.That(config.Movement.SimpleMove.ToString(), Is.EqualTo("1d6"));
            Assert.That(config.Movement.DashMove.ToString(), Is.EqualTo("2d6"));
        }

        // === Partial JSON (missing sections get defaults) ===

        [Test]
        public void Should_FillDefaults_When_PartialJsonLoaded()
        {
            var partialJson = @"{ ""combat"": { ""attackRange"": 2 } }";
            var config = GameConfig.Load(partialJson);

            // Overridden value
            Assert.That(config.Combat.AttackRange, Is.EqualTo(2));

            // Default values preserved
            Assert.That(config.Characters.Defaults.Health, Is.EqualTo(100));
            Assert.That(config.Grid.Width, Is.EqualTo(8));
            Assert.That(config.Turns.MaxSkipAttempts, Is.EqualTo(10));
        }
    }
}
