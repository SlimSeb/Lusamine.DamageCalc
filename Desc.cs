using System;
using DamageCalc.Data;

namespace DamageCalc {
  public sealed class RawDesc {
    public string? HPEVs { get; set; }
    public int? AttackBoost { get; set; }
    public string? AttackEVs { get; set; }
    public string? AttackerAbility { get; set; }
    public string? AttackerItem { get; set; }
    public string AttackerName { get; set; } = "";
    public string? AttackerTera { get; set; }
    public string? DefenderAbility { get; set; }
    public string? DefenderItem { get; set; }
    public string DefenderName { get; set; } = "";
    public string? DefenderTera { get; set; }
    public int? DefenseBoost { get; set; }
    public string? DefenseEVs { get; set; }
    public int? Hits { get; set; }
    public int? AlliesFainted { get; set; }
    public bool? IsStellarFirstUse { get; set; }
    public bool? IsBeadsOfRuin { get; set; }
    public bool? IsSwordOfRuin { get; set; }
    public bool? IsTabletsOfRuin { get; set; }
    public bool? IsVesselOfRuin { get; set; }
    public bool? IsAuroraVeil { get; set; }
    public bool? IsFlowerGiftAttacker { get; set; }
    public bool? IsFlowerGiftDefender { get; set; }
    public bool? IsPowerTrickAttacker { get; set; }
    public bool? IsPowerTrickDefender { get; set; }
    public bool? IsSteelySpiritAttacker { get; set; }
    public bool? IsFriendGuard { get; set; }
    public bool? IsHelpingHand { get; set; }
    public bool? IsCritical { get; set; }
    public bool? IsLightScreen { get; set; }
    public bool? IsBurned { get; set; }
    public bool? IsProtected { get; set; }
    public bool? IsReflect { get; set; }
    public bool? IsBattery { get; set; }
    public bool? IsPowerSpot { get; set; }
    public bool? IsWonderRoom { get; set; }
    public string? IsSwitching { get; set; }
    public int? MoveBP { get; set; }
    public string MoveName { get; set; } = "";
    public string? MoveTurns { get; set; }
    public string? MoveType { get; set; }
    public string? Rivalry { get; set; }
    public string? Terrain { get; set; }
    public string? Weather { get; set; }
    public bool? IsDefenderDynamaxed { get; set; }
  }

  public static class DescUtil {
    public static string Display(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field, object damage, RawDesc rawDesc, string notation = "%", bool err = true) {
      var range = DamageUtil.DamageRange(damage);
      var minDisplay = ToDisplay(notation, range.min, defender.MaxHP());
      var maxDisplay = ToDisplay(notation, range.max, defender.MaxHP());
      var desc = BuildDescription(rawDesc, attacker, defender);
      var damageText = $"{range.min}-{range.max} ({minDisplay} - {maxDisplay}{notation})";
      return $"{desc}: {damageText}";
    }

    public static string DisplayMove(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, object damage, string notation = "%") {
      var range = DamageUtil.DamageRange(damage);
      var minDisplay = ToDisplay(notation, range.min, defender.MaxHP());
      var maxDisplay = ToDisplay(notation, range.max, defender.MaxHP());
      return $"{minDisplay} - {maxDisplay}{notation}";
    }

    public static (int[] recovery, string text) GetRecovery(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, object damage, string notation = "%") {
      return (new[] { 0, 0 }, "");
    }

    public static (int[] recoil, string text) GetRecoil(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, object damage, string notation = "%") {
      return (new[] { 0, 0 }, "");
    }

    public static (string text, int chance) GetKOChance(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field, object damage, bool err = true) {
      return ("", 0);
    }

    private static string BuildDescription(RawDesc rawDesc, Pokemon attacker, Pokemon defender) {
      return $"{rawDesc.AttackerName} {rawDesc.MoveName} vs. {rawDesc.DefenderName}";
    }

    private static int ToDisplay(string notation, int num, int max, int maxDenominator = 100) {
      if (notation == "%") return (int)Math.Round(num * (maxDenominator / (double)max));
      return num;
    }
  }
}
