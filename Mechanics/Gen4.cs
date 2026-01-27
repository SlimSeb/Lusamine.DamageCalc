using DamageCalc.Data;

namespace DamageCalc.Mechanics {
  public static class Gen4 {
    public static Result CalculateDPP(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field) {
      // TODO: Port full Gen 4 mechanics from calc/src/mechanics/gen4.ts
      var desc = new RawDesc { AttackerName = attacker.Name, DefenderName = defender.Name, MoveName = move.Name };
      return new Result(gen, attacker, defender, move, field, 0, desc);
    }
  }
}
