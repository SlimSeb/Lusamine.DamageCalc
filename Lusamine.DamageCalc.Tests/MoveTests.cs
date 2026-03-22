using NUnit.Framework;

namespace Lusamine.DamageCalc.Tests {
  public sealed class MoveTests {
    [Test]
    public void Clone() {
      var move = new Move(TestHelper.Gen(7), "Blizzard", new State.Move { UseZ = true });
      Assert.That(move.Name, Is.EqualTo("Subzero Slammer"));
      Assert.That(move.Bp, Is.EqualTo(185));

      var clone = move.Clone();
      Assert.That(clone.Name, Is.EqualTo("Subzero Slammer"));
      Assert.That(clone.Bp, Is.EqualTo(185));
    }
  }
}
