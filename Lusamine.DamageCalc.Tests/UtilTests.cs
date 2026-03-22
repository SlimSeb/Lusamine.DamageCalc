using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;

namespace Lusamine.DamageCalc.Tests {
  public sealed class UtilTests {
    [Test]
    public void Extend_DeepMerges() {
      var obj1 = new Dictionary<string, object?> {
        ["a"] = 1,
        ["b"] = new Dictionary<string, object?> { ["c"] = 2 },
        ["d"] = new Dictionary<string, object?> { ["e"] = 3 },
        ["f"] = 4,
      };
      var obj2 = new Dictionary<string, object?> {
        ["a"] = 2,
        ["b"] = new Dictionary<string, object?> { ["c"] = 3 },
        ["d"] = 4,
        ["e"] = new Dictionary<string, object?> { ["f"] = 5 },
      };

      var merged1 = Util.Extend(true, new Dictionary<string, object?>(), obj1);
      Assert.That(JsonSerializer.Serialize(merged1), Is.EqualTo(JsonSerializer.Serialize(obj1)));

      var merged2 = Util.Extend(true, new Dictionary<string, object?>(), obj1, obj2);
      var expected2 = new Dictionary<string, object?> {
        ["a"] = 2,
        ["b"] = new Dictionary<string, object?> { ["c"] = 3 },
        ["d"] = 4,
        ["f"] = 4,
        ["e"] = new Dictionary<string, object?> { ["f"] = 5 },
      };
      Assert.That(JsonSerializer.Serialize(merged2), Is.EqualTo(JsonSerializer.Serialize(expected2)));

      var merged3 = Util.Extend(true, new Dictionary<string, object?>(), obj2, obj1);
      var expected3 = new Dictionary<string, object?> {
        ["a"] = 1,
        ["b"] = new Dictionary<string, object?> { ["c"] = 2 },
        ["d"] = new Dictionary<string, object?> { ["e"] = 3 },
        ["e"] = new Dictionary<string, object?> { ["f"] = 5 },
        ["f"] = 4,
      };
      Assert.That(JsonSerializer.Serialize(merged3), Is.EqualTo(JsonSerializer.Serialize(expected3)));
    }

    [Test]
    public void ToId_ConvertsFlabebe() {
      var id = Util.ToId("Flab\u00e9b\u00e9");
      Assert.That(id, Is.EqualTo("flabebe"));
    }
  }
}
