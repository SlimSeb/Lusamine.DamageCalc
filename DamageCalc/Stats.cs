using System;
using System.Collections.Generic;
using DamageCalc.Data;

namespace DamageCalc {
  public static class Stats {
    private static readonly StatId[] RBY = { StatId.Hp, StatId.Atk, StatId.Def, StatId.Spc, StatId.Spe };
    private static readonly StatId[] GSC = { StatId.Hp, StatId.Atk, StatId.Def, StatId.Spa, StatId.Spd, StatId.Spe };

    public static readonly List<StatId[]> STATS = new List<StatId[]> {
      Array.Empty<StatId>(),
      RBY,
      GSC,
      GSC,
      GSC,
      GSC,
      GSC,
      GSC,
      GSC,
      GSC,
    };

    private static readonly string[] HP_TYPES = {
      "Fighting", "Flying", "Poison", "Ground", "Rock", "Bug", "Ghost", "Steel",
      "Fire", "Water", "Grass", "Electric", "Psychic", "Ice", "Dragon", "Dark",
    };

    private static readonly Dictionary<string, (StatsTable ivs, StatsTable dvs)> HP =
      new Dictionary<string, (StatsTable ivs, StatsTable dvs)> {
        { "Bug", (new StatsTable { Atk = 30, Def = 30, Spd = 30 }, new StatsTable { Atk = 13, Def = 13 }) },
        { "Dark", (new StatsTable(), new StatsTable()) },
        { "Dragon", (new StatsTable { Atk = 30 }, new StatsTable { Def = 14 }) },
        { "Electric", (new StatsTable { Spa = 30 }, new StatsTable { Atk = 14 }) },
        { "Fighting", (new StatsTable { Def = 30, Spa = 30, Spd = 30, Spe = 30 }, new StatsTable { Atk = 12, Def = 12 }) },
        { "Fire", (new StatsTable { Atk = 30, Spa = 30, Spe = 30 }, new StatsTable { Atk = 14, Def = 12 }) },
        { "Flying", (new StatsTable { Hp = 30, Atk = 30, Def = 30, Spa = 30, Spd = 30 }, new StatsTable { Atk = 12, Def = 13 }) },
        { "Ghost", (new StatsTable { Def = 30, Spd = 30 }, new StatsTable { Atk = 13, Def = 14 }) },
        { "Grass", (new StatsTable { Atk = 30, Spa = 30 }, new StatsTable { Atk = 14, Def = 14 }) },
        { "Ground", (new StatsTable { Spa = 30, Spd = 30 }, new StatsTable { Atk = 12 }) },
        { "Ice", (new StatsTable { Atk = 30, Def = 30 }, new StatsTable { Def = 13 }) },
        { "Poison", (new StatsTable { Def = 30, Spa = 30, Spd = 30 }, new StatsTable { Atk = 12, Def = 14 }) },
        { "Psychic", (new StatsTable { Atk = 30, Spe = 30 }, new StatsTable { Def = 12 }) },
        { "Rock", (new StatsTable { Def = 30, Spd = 30, Spe = 30 }, new StatsTable { Atk = 13, Def = 12 }) },
        { "Steel", (new StatsTable { Spd = 30 }, new StatsTable { Atk = 13 }) },
        { "Water", (new StatsTable { Atk = 30, Def = 30, Spa = 30 }, new StatsTable { Atk = 14, Def = 13 }) },
      };

    public static string DisplayStat(StatId stat) {
      switch (stat) {
        case StatId.Hp: return "HP";
        case StatId.Atk: return "Atk";
        case StatId.Def: return "Def";
        case StatId.Spa: return "SpA";
        case StatId.Spd: return "SpD";
        case StatId.Spe: return "Spe";
        case StatId.Spc: return "Spc";
        default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
      }
    }

    public static string ShortForm(StatId stat) {
      switch (stat) {
        case StatId.Hp: return "hp";
        case StatId.Atk: return "at";
        case StatId.Def: return "df";
        case StatId.Spa: return "sa";
        case StatId.Spd: return "sd";
        case StatId.Spe: return "sp";
        case StatId.Spc: return "sl";
        default: return "";
      }
    }

    public static int GetHPDV(StatsTable ivs) {
      return (IVToDV(ivs.Atk) % 2) * 8 +
        (IVToDV(ivs.Def) % 2) * 4 +
        (IVToDV(ivs.Spe) % 2) * 2 +
        (IVToDV(ivs.Spc) % 2);
    }

    public static int IVToDV(int iv) {
      return (int)Math.Floor(iv / 2.0);
    }

    public static int DVToIV(int dv) {
      return dv * 2;
    }

    public static StatsTable DVsToIVs(StatsTable dvs) {
      return new StatsTable {
        Hp = DVToIV(dvs.Hp),
        Atk = DVToIV(dvs.Atk),
        Def = DVToIV(dvs.Def),
        Spa = DVToIV(dvs.Spa),
        Spd = DVToIV(dvs.Spd),
        Spe = DVToIV(dvs.Spe),
        Spc = DVToIV(dvs.Spc),
      };
    }

    public static int CalcStat(IGeneration gen, StatId stat, int @base, int iv, int ev, int level, string? nature) {
      if (gen.Num < 1 || gen.Num > 9) throw new InvalidOperationException($"Invalid generation {gen.Num}");
      if (gen.Num < 3) return CalcStatRBY(stat, @base, iv, level);
      return CalcStatADV(gen.Natures, stat, @base, iv, ev, level, nature);
    }

    public static int CalcStatADV(IDataTable<INature> natures, StatId stat, int @base, int iv, int ev, int level, string? nature) {
      if (stat == StatId.Hp) {
        return @base == 1
          ? @base
          : (int)Math.Floor(((@base * 2 + iv + Math.Floor(ev / 4.0)) * level) / 100.0) + level + 10;
      }

      StatId? plus = null;
      StatId? minus = null;
      if (!string.IsNullOrEmpty(nature)) {
        var nat = natures.Get(Util.ToId(nature));
        plus = nat?.Plus;
        minus = nat?.Minus;
      }

      double n = 1;
      if (plus == stat && minus == stat) {
        n = 1;
      } else if (plus == stat) {
        n = 1.1;
      } else if (minus == stat) {
        n = 0.9;
      }

      return (int)Math.Floor((Math.Floor(((@base * 2 + iv + Math.Floor(ev / 4.0)) * level) / 100.0) + 5) * n);
    }

    public static int CalcStatRBY(StatId stat, int @base, int iv, int level) {
      return CalcStatRBYFromDV(stat, @base, IVToDV(iv), level);
    }

    public static int CalcStatRBYFromDV(StatId stat, int @base, int dv, int level) {
      if (stat == StatId.Hp) {
        return (int)Math.Floor((((@base + dv) * 2 + 63) * level) / 100.0) + level + 10;
      }
      return (int)Math.Floor((((@base + dv) * 2 + 63) * level) / 100.0) + 5;
    }

    public static StatsTable? GetHiddenPowerIVs(IGeneration gen, string hpType) {
      if (!HP.TryGetValue(hpType, out var hp)) return null;
      return gen.Num == 2 ? DVsToIVs(hp.dvs) : hp.ivs;
    }

    public static (string type, int power) GetHiddenPower(IGeneration gen, StatsTable ivs) {
      int Tr(double num, int bits = 0) {
        if (bits != 0) return (int)((uint)num % (uint)(2 << (bits - 1)));
        return (int)((uint)num);
      }

      var stats = new StatsTable { Hp = 31, Atk = 31, Def = 31, Spe = 31, Spa = 31, Spd = 31 };
      if (gen.Num <= 2) {
        var atkDV = Tr(ivs.Atk / 2.0);
        var defDV = Tr(ivs.Def / 2.0);
        var speDV = Tr(ivs.Spe / 2.0);
        var spcDV = Tr(ivs.Spa / 2.0);
        var type = HP_TYPES[4 * (atkDV % 4) + (defDV % 4)];
        var power = Tr((5 * ((spcDV >> 3) + (2 * (speDV >> 3)) + (4 * (defDV >> 3)) + (8 * (atkDV >> 3))) + (spcDV % 4)) / 2.0 + 31);
        return (type, power);
      }

      int hpTypeX = 0;
      int hpPowerX = 0;
      int i = 1;
      foreach (var stat in new[] { StatId.Hp, StatId.Atk, StatId.Def, StatId.Spe, StatId.Spa, StatId.Spd }) {
        hpTypeX += i * (ivs[stat] % 2);
        hpPowerX += i * (Tr(ivs[stat] / 2.0) % 2);
        i *= 2;
      }
      var hpType = HP_TYPES[Tr(hpTypeX * 15.0 / 63.0)];
      var powerVal = gen.Num < 6 ? Tr(hpPowerX * 40.0 / 63.0) + 30 : 60;
      return (hpType, powerVal);
    }
  }
}
