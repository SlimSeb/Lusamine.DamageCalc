using DamageCalc.Data;

namespace DamageCalc.Mechanics {
  public static class Gen789 {
    public static Result CalculateSMSSSV(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field) {
      // TODO: Port full Gen 7/8/9 mechanics from calc/src/mechanics/gen789.ts
      var desc = new RawDesc { AttackerName = attacker.Name, DefenderName = defender.Name, MoveName = move.Name };
      return new Result(gen, attacker, defender, move, field, 0, desc);
    }
  }
}
