using NUnit.Framework;
using System.Text.Json; // Or Newtonsoft.Json if you prefer
using RPGGame.Combat;

namespace RPGDnDGame.Tests.Unit.Combat
{
    [TestFixture]
    public class CounterGaugeSerializationTests
    {
        [Test]
        [Category("Unit")]
        public void Should_RestoreState_When_Deserialized()
        {
            // Arrange
            var original = new CounterGauge();
            original.AddCounter(4);

            // Act
            var json = JsonSerializer.Serialize(original);
            var restored = JsonSerializer.Deserialize<CounterGauge>(json);

            // Assert
            Assert.NotNull(restored, "Deserialized CounterGauge should not be null");
            Assert.AreEqual(4, restored.Current, "Counter should be restored to correct value");
            Assert.AreEqual(original.Maximum, restored.Maximum, "Maximum should persist");
            Assert.AreEqual(original.IsReady, restored.IsReady, "Ready state should persist");
            Assert.AreEqual(original.FillPercentage, restored.FillPercentage, "Fill percentage should persist");
        }
    }
}