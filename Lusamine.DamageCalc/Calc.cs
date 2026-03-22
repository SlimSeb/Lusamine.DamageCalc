using System;
using Lusamine.DamageCalc.Data;
using Lusamine.DamageCalc.Mechanics;

namespace Lusamine.DamageCalc {
  public static class Calc {
    private static readonly Func<IGeneration, Pokemon, Pokemon, Move, Field, Result>[] MECHANICS = {
      (g, a, d, m, f) => throw new InvalidOperationException("Invalid generation"),
      Gen12.CalculateRBYGSC,
      Gen12.CalculateRBYGSC,
      Gen3.CalculateADV,
      Gen4.CalculateDPP,
      Gen56.CalculateBWXY,
      Gen56.CalculateBWXY,
      Gen789.CalculateSMSSSV,
      Gen789.CalculateSMSSSV,
      Gen789.CalculateSMSSSV,
    };

    public static Result Calculate(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field? field = null) {
      var f = field?.Clone() ?? new Field();
      return MECHANICS[gen.Num](gen, attacker.Clone(), defender.Clone(), move.Clone(), f);
    }
  }
}
