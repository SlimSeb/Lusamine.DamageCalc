using System.Collections.Generic;
using Lusamine.DamageCalc.Data;

namespace Lusamine.DamageCalc {
  public static class State {
    public sealed class Pokemon {
      public string Name { get; set; } = "";
      public int? Level { get; set; }
      public string? Ability { get; set; }
      public bool? AbilityOn { get; set; }
      public bool? IsDynamaxed { get; set; }
      public int? DynamaxLevel { get; set; }
      public int? AlliesFainted { get; set; }
      public string? BoostedStat { get; set; }
      public string? Item { get; set; }
      public string? Gender { get; set; }
      public string? Nature { get; set; }
      public StatsTableInput? Ivs { get; set; }
      public StatsTableInput? Evs { get; set; }
      public StatsTableInput? Boosts { get; set; }
      public int? CurHP { get; set; }
      public int? OriginalCurHP { get; set; }
      public string? Status { get; set; }
      public string? TeraType { get; set; }
      public int? ToxicCounter { get; set; }
      public List<string>? Moves { get; set; }
      public Data.Specie? Overrides { get; set; }
    }

    public sealed class Move {
      public string Name { get; set; } = "";
      public bool? UseZ { get; set; }
      public bool? UseMax { get; set; }
      public bool? IsCrit { get; set; }
      public bool? IsStellarFirstUse { get; set; }
      public int? Hits { get; set; }
      public int? TimesUsed { get; set; }
      public int? TimesUsedWithMetronome { get; set; }
      public Data.MoveData? Overrides { get; set; }
    }

    public sealed class Field {
      public string GameType { get; set; } = GameTypes.Singles;
      public string? Weather { get; set; }
      public string? Terrain { get; set; }
      public bool? IsMagicRoom { get; set; }
      public bool? IsWonderRoom { get; set; }
      public bool? IsGravity { get; set; }
      public bool? IsAuraBreak { get; set; }
      public bool? IsFairyAura { get; set; }
      public bool? IsDarkAura { get; set; }
      public bool? IsBeadsOfRuin { get; set; }
      public bool? IsSwordOfRuin { get; set; }
      public bool? IsTabletsOfRuin { get; set; }
      public bool? IsVesselOfRuin { get; set; }
      public Side? AttackerSide { get; set; }
      public Side? DefenderSide { get; set; }
    }

    public sealed class Side {
      public int? Spikes { get; set; }
      public bool? Steelsurge { get; set; }
      public bool? Vinelash { get; set; }
      public bool? Wildfire { get; set; }
      public bool? Cannonade { get; set; }
      public bool? Volcalith { get; set; }
      public bool? IsSR { get; set; }
      public bool? IsReflect { get; set; }
      public bool? IsLightScreen { get; set; }
      public bool? IsProtected { get; set; }
      public bool? IsSeeded { get; set; }
      public bool? IsSaltCured { get; set; }
      public bool? IsForesight { get; set; }
      public bool? IsTailwind { get; set; }
      public bool? IsHelpingHand { get; set; }
      public bool? IsFlowerGift { get; set; }
      public bool? IsPowerTrick { get; set; }
      public bool? IsFriendGuard { get; set; }
      public bool? IsAuroraVeil { get; set; }
      public bool? IsBattery { get; set; }
      public bool? IsPowerSpot { get; set; }
      public bool? IsSteelySpirit { get; set; }
      public string? IsSwitching { get; set; }
    }
  }
}
