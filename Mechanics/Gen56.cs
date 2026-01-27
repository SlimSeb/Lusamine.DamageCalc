using DamageCalc.Data;

namespace DamageCalc.Mechanics {
  public static class Gen56 {
    public static Result CalculateBWXY(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field) {
      // TODO: Port full Gen 5/6 mechanics from calc/src/mechanics/gen56.ts
      var desc = new RawDesc { AttackerName = attacker.Name, DefenderName = defender.Name, MoveName = move.Name };
      return new Result(gen, attacker, defender, move, field, 0, desc);
    }
  }
}
