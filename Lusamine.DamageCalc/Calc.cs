using System;
using Lusamine.DamageCalc.Data;
using Lusamine.DamageCalc.Mechanics;

namespace Lusamine.DamageCalc {
  /// <summary>
  /// Core entry point for damage calculation.
  /// Delegates to the correct generation-specific mechanics engine.
  /// </summary>
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

    /// <summary>
    /// Performs a damage calculation for the given generation and participants.
    /// The attacker, defender, and field are cloned internally so the originals
    /// are not mutated.
    /// </summary>
    /// <param name="gen">Generation data context (e.g. from <see cref="DataIndex.Create"/>).</param>
    /// <param name="attacker">The attacking Pokémon.</param>
    /// <param name="defender">The defending Pokémon.</param>
    /// <param name="move">The move being used.</param>
    /// <param name="field">Optional field conditions (weather, terrain, sides). Defaults to an empty field.</param>
    /// <returns>
    /// A <see cref="Result"/> containing the damage roll array, description, and KO-chance helpers.
    /// </returns>
    public static Result Calculate(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field? field = null) {
      var f = field?.Clone() ?? new Field();
      return MECHANICS[gen.Num](gen, attacker.Clone(), defender.Clone(), move.Clone(), f);
    }
  }
}
