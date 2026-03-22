using System;
using Lusamine.DamageCalc.Data;

namespace Lusamine.DamageCalc {
  public static class Api {
    public static IGenerations? DefaultGenerations { get; set; }

    public static Result Calculate(int gen, Pokemon attacker, Pokemon defender, Move move, Field? field = null) {
      if (DefaultGenerations == null) throw new InvalidOperationException("DefaultGenerations is not set");
      return Calc.Calculate(DefaultGenerations.Get(gen), attacker, defender, move, field);
    }

    public static Result Calculate(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field? field = null) {
      return Calc.Calculate(gen, attacker, defender, move, field);
    }

    public static int CalcStat(int gen, StatId stat, int @base, int iv, int ev, int level, string? nature = null) {
      if (DefaultGenerations == null) throw new InvalidOperationException("DefaultGenerations is not set");
      return Stats.CalcStat(DefaultGenerations.Get(gen), stat, @base, iv, ev, level, nature);
    }

    public static int CalcStat(IGeneration gen, StatId stat, int @base, int iv, int ev, int level, string? nature = null) {
      return Stats.CalcStat(gen, stat, @base, iv, ev, level, nature);
    }
  }
}
