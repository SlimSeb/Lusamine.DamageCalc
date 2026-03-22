using System;
using Lusamine.DamageCalc.Data;

namespace Lusamine.DamageCalc {
  /// <summary>
  /// Convenience facade over <see cref="Calc"/> and <see cref="Stats"/>.
  /// Lets you omit an explicit <see cref="IGeneration"/> by pointing
  /// <see cref="DefaultGenerations"/> at a pre-built generation registry.
  /// </summary>
  public static class Api {
    /// <summary>
    /// Optional generation registry used by the <c>int gen</c> overloads.
    /// Set this once at application startup (e.g. from <see cref="DataIndex"/>)
    /// so callers can pass a plain generation number instead of an
    /// <see cref="IGeneration"/> instance.
    /// </summary>
    public static IGenerations? DefaultGenerations { get; set; }

    /// <summary>
    /// Calculates damage using the generation number from
    /// <see cref="DefaultGenerations"/>.
    /// </summary>
    /// <param name="gen">Generation number (1–9).</param>
    /// <param name="attacker">The attacking Pokémon.</param>
    /// <param name="defender">The defending Pokémon.</param>
    /// <param name="move">The move being used.</param>
    /// <param name="field">Optional field conditions (weather, terrain, sides).</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="DefaultGenerations"/> is <c>null</c>.
    /// </exception>
    public static Result Calculate(int gen, Pokemon attacker, Pokemon defender, Move move, Field? field = null) {
      if (DefaultGenerations == null) throw new InvalidOperationException("DefaultGenerations is not set");
      return Calc.Calculate(DefaultGenerations.Get(gen), attacker, defender, move, field);
    }

    /// <summary>
    /// Calculates damage using an explicit <see cref="IGeneration"/> context.
    /// </summary>
    /// <param name="gen">Generation data context (e.g. from <see cref="DataIndex.Create"/>).</param>
    /// <param name="attacker">The attacking Pokémon.</param>
    /// <param name="defender">The defending Pokémon.</param>
    /// <param name="move">The move being used.</param>
    /// <param name="field">Optional field conditions.</param>
    public static Result Calculate(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field? field = null) {
      return Calc.Calculate(gen, attacker, defender, move, field);
    }

    /// <summary>
    /// Computes a single stat value using the generation number from
    /// <see cref="DefaultGenerations"/>.
    /// </summary>
    /// <param name="gen">Generation number (1–9).</param>
    /// <param name="stat">The stat to compute.</param>
    /// <param name="base">Base stat value.</param>
    /// <param name="iv">Individual value (0–31; converted to DVs for gen 1–2).</param>
    /// <param name="ev">Effort value (0–252 for gen 3+; ignored in gen 1–2).</param>
    /// <param name="level">Pokémon level (1–100).</param>
    /// <param name="nature">Nature name, or <c>null</c> for a neutral nature.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="DefaultGenerations"/> is <c>null</c>.
    /// </exception>
    public static int CalcStat(int gen, StatId stat, int @base, int iv, int ev, int level, string? nature = null) {
      if (DefaultGenerations == null) throw new InvalidOperationException("DefaultGenerations is not set");
      return Stats.CalcStat(DefaultGenerations.Get(gen), stat, @base, iv, ev, level, nature);
    }

    /// <summary>
    /// Computes a single stat value using an explicit <see cref="IGeneration"/> context.
    /// </summary>
    /// <param name="gen">Generation data context.</param>
    /// <param name="stat">The stat to compute.</param>
    /// <param name="base">Base stat value.</param>
    /// <param name="iv">Individual value (0–31; converted to DVs for gen 1–2).</param>
    /// <param name="ev">Effort value (0–252 for gen 3+; ignored in gen 1–2).</param>
    /// <param name="level">Pokémon level (1–100).</param>
    /// <param name="nature">Nature name, or <c>null</c> for a neutral nature.</param>
    public static int CalcStat(IGeneration gen, StatId stat, int @base, int iv, int ev, int level, string? nature = null) {
      return Stats.CalcStat(gen, stat, @base, iv, ev, level, nature);
    }
  }
}
