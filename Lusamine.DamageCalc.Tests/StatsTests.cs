using System.Collections.Generic;
using DamageCalc;
using DamageCalc.Data;
using DamageCalc.Mechanics;
using NUnit.Framework;

namespace DamageCalc.Tests {
  public sealed class StatsTests {
    [Test]
    public void DisplayStat() {
      Assert.That(Stats.DisplayStat(StatId.Hp), Is.EqualTo("HP"));
      Assert.That(Stats.DisplayStat(StatId.Atk), Is.EqualTo("Atk"));
      Assert.That(Stats.DisplayStat(StatId.Def), Is.EqualTo("Def"));
      Assert.That(Stats.DisplayStat(StatId.Spa), Is.EqualTo("SpA"));
      Assert.That(Stats.DisplayStat(StatId.Spd), Is.EqualTo("SpD"));
      Assert.That(Stats.DisplayStat(StatId.Spe), Is.EqualTo("Spe"));
      Assert.That(Stats.DisplayStat(StatId.Spc), Is.EqualTo("Spc"));
    }

    [Test]
    public void CalcStat() {
      var rby = new Dictionary<StatId, int> {
        { StatId.Hp, 403 },
        { StatId.Atk, 298 },
        { StatId.Def, 298 },
        { StatId.Spa, 298 },
        { StatId.Spd, 298 },
        { StatId.Spe, 298 },
      };
      var adv = new Dictionary<StatId, int> {
        { StatId.Hp, 404 },
        { StatId.Atk, 328 },
        { StatId.Def, 299 },
        { StatId.Spa, 269 },
        { StatId.Spd, 299 },
        { StatId.Spe, 299 },
      };

      for (var gen = 1; gen <= 9; gen++) {
        var g = DataIndex.Create(gen);
        foreach (var stat in adv.Keys) {
          var val = Stats.CalcStat(g, stat, 100, 31, 252, 100, "Adamant");
          Assert.That(val, Is.EqualTo(gen < 3 ? rby[stat] : adv[stat]));
        }
      }

      var gen8 = DataIndex.Create(8);
      Assert.That(Stats.CalcStat(gen8, StatId.Hp, 1, 31, 252, 100, "Jolly"), Is.EqualTo(1));
      Assert.That(Stats.CalcStat(gen8, StatId.Atk, 100, 31, 252, 100, "Seriou"), Is.EqualTo(299));
    }

    [Test]
    public void Dvs() {
      for (var dv = 0; dv <= 15; dv++) {
        Assert.That(Stats.IVToDV(Stats.DVToIV(dv)), Is.EqualTo(dv));
      }

      Assert.That(Stats.GetHPDV(new StatsTable {
        Atk = Stats.DVToIV(15),
        Def = Stats.DVToIV(15),
        Spc = Stats.DVToIV(15),
        Spe = Stats.DVToIV(15),
      }), Is.EqualTo(15));

      Assert.That(Stats.GetHPDV(new StatsTable {
        Atk = Stats.DVToIV(5),
        Def = Stats.DVToIV(15),
        Spc = Stats.DVToIV(13),
        Spe = Stats.DVToIV(13),
      }), Is.EqualTo(15));

      Assert.That(Stats.GetHPDV(new StatsTable {
        Atk = Stats.DVToIV(15),
        Def = Stats.DVToIV(3),
        Spc = Stats.DVToIV(11),
        Spe = Stats.DVToIV(10),
      }), Is.EqualTo(13));
    }

    [Test]
    public void Gen2Modifications() {
      var gen2 = DataIndex.Create(2);
      Assert.That(MechanicsUtil.GetModifiedStat(158, -1, gen2), Is.EqualTo(104));
      Assert.That(MechanicsUtil.GetModifiedStat(238, -1, gen2), Is.EqualTo(157));
    }
  }
}
