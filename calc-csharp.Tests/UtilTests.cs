using NUnit.Framework;

namespace DamageCalc.Tests {
  public sealed class UtilTests {
    [Test]
    public void ToId_ConvertsFlabebe() {
      var id = Util.ToId("Flab\u00e9b\u00e9");
      Assert.That(id, Is.EqualTo("flabebe"));
    }
  }
}
