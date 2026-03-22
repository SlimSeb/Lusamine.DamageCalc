using System;

namespace Lusamine.DamageCalc {
  /// <summary>
  /// Describes the battle field conditions for a damage calculation:
  /// game type, weather, terrain, and per-side modifiers.
  /// </summary>
  public sealed class Field {
    /// <summary>
    /// Battle format: <c>"Singles"</c> (default) or <c>"Doubles"</c>.
    /// Use <see cref="Data.GameTypes"/> constants.
    /// </summary>
    public string GameType { get; set; }
    /// <summary>Active weather: <c>"Sun"</c>, <c>"Rain"</c>, <c>"Sand"</c>, <c>"Hail"</c>, <c>"Snow"</c>, or <c>null</c>.</summary>
    public string? Weather { get; set; }
    /// <summary>Active terrain: <c>"Electric"</c>, <c>"Grassy"</c>, <c>"Misty"</c>, <c>"Psychic"</c>, or <c>null</c>.</summary>
    public string? Terrain { get; set; }
    /// <summary>Whether Magic Room is active (suppresses held items).</summary>
    public bool IsMagicRoom { get; set; }
    /// <summary>Whether Wonder Room is active (swaps Def and SpD).</summary>
    public bool IsWonderRoom { get; set; }
    /// <summary>Whether Gravity is active (grounds all Pokémon).</summary>
    public bool IsGravity { get; set; }
    /// <summary>Whether Aura Break is active (reverses Fairy/Dark Aura effects).</summary>
    public bool IsAuraBreak { get; set; }
    /// <summary>Whether Fairy Aura is active (boosts Fairy-type moves by 4/3).</summary>
    public bool IsFairyAura { get; set; }
    /// <summary>Whether Dark Aura is active (boosts Dark-type moves by 4/3).</summary>
    public bool IsDarkAura { get; set; }
    /// <summary>Whether Beads of Ruin is active (lowers Sp. Def of non-holders by 25%).</summary>
    public bool IsBeadsOfRuin { get; set; }
    /// <summary>Whether Sword of Ruin is active (lowers Defense of non-holders by 25%).</summary>
    public bool IsSwordOfRuin { get; set; }
    /// <summary>Whether Tablets of Ruin is active (lowers Attack of non-holders by 25%).</summary>
    public bool IsTabletsOfRuin { get; set; }
    /// <summary>Whether Vessel of Ruin is active (lowers Sp. Atk of non-holders by 25%).</summary>
    public bool IsVesselOfRuin { get; set; }
    /// <summary>Conditions on the attacker's side of the field.</summary>
    public Side AttackerSide { get; set; }
    /// <summary>Conditions on the defender's side of the field.</summary>
    public Side DefenderSide { get; set; }

    /// <summary>
    /// Constructs a Field from an optional <see cref="State.Field"/> descriptor.
    /// Defaults to Singles with no weather, terrain, or side conditions.
    /// </summary>
    public Field(State.Field? field = null) {
      field ??= new State.Field();
      GameType = field.GameType ?? Data.GameTypes.Singles;
      Terrain = field.Terrain;
      Weather = field.Weather;
      IsMagicRoom = field.IsMagicRoom ?? false;
      IsWonderRoom = field.IsWonderRoom ?? false;
      IsGravity = field.IsGravity ?? false;
      IsAuraBreak = field.IsAuraBreak ?? false;
      IsFairyAura = field.IsFairyAura ?? false;
      IsDarkAura = field.IsDarkAura ?? false;
      IsBeadsOfRuin = field.IsBeadsOfRuin ?? false;
      IsSwordOfRuin = field.IsSwordOfRuin ?? false;
      IsTabletsOfRuin = field.IsTabletsOfRuin ?? false;
      IsVesselOfRuin = field.IsVesselOfRuin ?? false;

      AttackerSide = new Side(field.AttackerSide);
      DefenderSide = new Side(field.DefenderSide);
    }

    /// <summary>Returns <c>true</c> if the active weather matches any of the given weather names.</summary>
    public bool HasWeather(params string[] weathers) {
      return Weather != null && Array.Exists(weathers, w => w == Weather);
    }

    /// <summary>Returns <c>true</c> if the active terrain matches any of the given terrain names.</summary>
    public bool HasTerrain(params string[] terrains) {
      return Terrain != null && Array.Exists(terrains, t => t == Terrain);
    }

    public Field Swap() {
      var temp = AttackerSide;
      AttackerSide = DefenderSide;
      DefenderSide = temp;
      return this;
    }

    public Field Clone() {
      return new Field(new State.Field {
        GameType = GameType,
        Weather = Weather,
        Terrain = Terrain,
        IsMagicRoom = IsMagicRoom,
        IsWonderRoom = IsWonderRoom,
        IsGravity = IsGravity,
        AttackerSide = AttackerSide.ToState(),
        DefenderSide = DefenderSide.ToState(),
        IsAuraBreak = IsAuraBreak,
        IsDarkAura = IsDarkAura,
        IsFairyAura = IsFairyAura,
        IsBeadsOfRuin = IsBeadsOfRuin,
        IsSwordOfRuin = IsSwordOfRuin,
        IsTabletsOfRuin = IsTabletsOfRuin,
        IsVesselOfRuin = IsVesselOfRuin,
      });
    }
  }

  /// <summary>
  /// Conditions active on one side of the battlefield (attacker's or defender's side),
  /// including entry hazards, screens, and ally-support effects.
  /// </summary>
  public sealed class Side {
    /// <summary>Number of Spikes layers (0–3).</summary>
    public int Spikes { get; set; }
    /// <summary>Whether G-Max Steelsurge steel-type hazard is active.</summary>
    public bool Steelsurge { get; set; }
    /// <summary>Whether G-Max Vine Lash grass-type hazard is active.</summary>
    public bool Vinelash { get; set; }
    /// <summary>Whether G-Max Wildfire fire-type hazard is active.</summary>
    public bool Wildfire { get; set; }
    /// <summary>Whether G-Max Cannonade water-type hazard is active.</summary>
    public bool Cannonade { get; set; }
    /// <summary>Whether G-Max Volcalith rock-type hazard is active.</summary>
    public bool Volcalith { get; set; }
    /// <summary>Whether Stealth Rock is active on this side.</summary>
    public bool IsSR { get; set; }
    /// <summary>Whether Reflect is active on this side (halves physical damage).</summary>
    public bool IsReflect { get; set; }
    /// <summary>Whether Light Screen is active on this side (halves special damage).</summary>
    public bool IsLightScreen { get; set; }
    /// <summary>Whether the Pokémon on this side is protected (Protect / Detect).</summary>
    public bool IsProtected { get; set; }
    /// <summary>Whether Leech Seed is active on the Pokémon on this side.</summary>
    public bool IsSeeded { get; set; }
    /// <summary>Whether Salt Cure is active on the Pokémon on this side.</summary>
    public bool IsSaltCured { get; set; }
    /// <summary>Whether Foresight / Odor Sleuth is active (removes Normal/Fighting immunity).</summary>
    public bool IsForesight { get; set; }
    /// <summary>Whether Tailwind is active on this side (doubles Speed).</summary>
    public bool IsTailwind { get; set; }
    /// <summary>Whether Helping Hand has been used on the attacker this turn (boosts damage by 1.5×).</summary>
    public bool IsHelpingHand { get; set; }
    /// <summary>Whether Flower Gift is active (boosts Attack and Sp. Def in sun).</summary>
    public bool IsFlowerGift { get; set; }
    /// <summary>Whether Power Trick is active (swaps Attack and Defense).</summary>
    public bool? IsPowerTrick { get; set; }
    /// <summary>Whether Friend Guard is active on an ally in Doubles (reduces damage taken by 25%).</summary>
    public bool IsFriendGuard { get; set; }
    /// <summary>Whether Aurora Veil is active on this side (halves both physical and special damage).</summary>
    public bool IsAuroraVeil { get; set; }
    /// <summary>Whether Battery is active on an ally in Doubles (boosts special moves by 1.3×).</summary>
    public bool IsBattery { get; set; }
    /// <summary>Whether Power Spot is active on an ally in Doubles (boosts all moves by 1.3×).</summary>
    public bool IsPowerSpot { get; set; }
    /// <summary>Whether Steely Spirit is active on an ally in Doubles (boosts Steel-type moves by 1.5×).</summary>
    public bool IsSteelySpirit { get; set; }
    /// <summary>Switching state override (<c>"in"</c> or <c>"out"</c>), used for entry/exit mechanics.</summary>
    public string? IsSwitching { get; set; }

    /// <summary>Constructs a Side from an optional <see cref="State.Side"/> descriptor. Defaults to all conditions off.</summary>
    public Side(State.Side? side = null) {
      side ??= new State.Side();
      Spikes = side.Spikes ?? 0;
      Steelsurge = side.Steelsurge ?? false;
      Vinelash = side.Vinelash ?? false;
      Wildfire = side.Wildfire ?? false;
      Cannonade = side.Cannonade ?? false;
      Volcalith = side.Volcalith ?? false;
      IsSR = side.IsSR ?? false;
      IsReflect = side.IsReflect ?? false;
      IsLightScreen = side.IsLightScreen ?? false;
      IsProtected = side.IsProtected ?? false;
      IsSeeded = side.IsSeeded ?? false;
      IsSaltCured = side.IsSaltCured ?? false;
      IsForesight = side.IsForesight ?? false;
      IsTailwind = side.IsTailwind ?? false;
      IsHelpingHand = side.IsHelpingHand ?? false;
      IsFlowerGift = side.IsFlowerGift ?? false;
      IsPowerTrick = side.IsPowerTrick ?? false;
      IsFriendGuard = side.IsFriendGuard ?? false;
      IsAuroraVeil = side.IsAuroraVeil ?? false;
      IsBattery = side.IsBattery ?? false;
      IsPowerSpot = side.IsPowerSpot ?? false;
      IsSteelySpirit = side.IsSteelySpirit ?? false;
      IsSwitching = side.IsSwitching;
    }

    public State.Side ToState() {
      return new State.Side {
        Spikes = Spikes,
        Steelsurge = Steelsurge,
        Vinelash = Vinelash,
        Wildfire = Wildfire,
        Cannonade = Cannonade,
        Volcalith = Volcalith,
        IsSR = IsSR,
        IsReflect = IsReflect,
        IsLightScreen = IsLightScreen,
        IsProtected = IsProtected,
        IsSeeded = IsSeeded,
        IsSaltCured = IsSaltCured,
        IsForesight = IsForesight,
        IsTailwind = IsTailwind,
        IsHelpingHand = IsHelpingHand,
        IsFlowerGift = IsFlowerGift,
        IsPowerTrick = IsPowerTrick,
        IsFriendGuard = IsFriendGuard,
        IsAuroraVeil = IsAuroraVeil,
        IsBattery = IsBattery,
        IsPowerSpot = IsPowerSpot,
        IsSteelySpirit = IsSteelySpirit,
        IsSwitching = IsSwitching,
      };
    }

    public Side Clone() {
      return new Side(ToState());
    }
  }
}
