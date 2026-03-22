using System;

namespace Lusamine.DamageCalc {
  public sealed class Field {
    public string GameType { get; set; }
    public string? Weather { get; set; }
    public string? Terrain { get; set; }
    public bool IsMagicRoom { get; set; }
    public bool IsWonderRoom { get; set; }
    public bool IsGravity { get; set; }
    public bool IsAuraBreak { get; set; }
    public bool IsFairyAura { get; set; }
    public bool IsDarkAura { get; set; }
    public bool IsBeadsOfRuin { get; set; }
    public bool IsSwordOfRuin { get; set; }
    public bool IsTabletsOfRuin { get; set; }
    public bool IsVesselOfRuin { get; set; }
    public Side AttackerSide { get; set; }
    public Side DefenderSide { get; set; }

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

    public bool HasWeather(params string[] weathers) {
      return Weather != null && Array.Exists(weathers, w => w == Weather);
    }

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

  public sealed class Side {
    public int Spikes { get; set; }
    public bool Steelsurge { get; set; }
    public bool Vinelash { get; set; }
    public bool Wildfire { get; set; }
    public bool Cannonade { get; set; }
    public bool Volcalith { get; set; }
    public bool IsSR { get; set; }
    public bool IsReflect { get; set; }
    public bool IsLightScreen { get; set; }
    public bool IsProtected { get; set; }
    public bool IsSeeded { get; set; }
    public bool IsSaltCured { get; set; }
    public bool IsForesight { get; set; }
    public bool IsTailwind { get; set; }
    public bool IsHelpingHand { get; set; }
    public bool IsFlowerGift { get; set; }
    public bool? IsPowerTrick { get; set; }
    public bool IsFriendGuard { get; set; }
    public bool IsAuroraVeil { get; set; }
    public bool IsBattery { get; set; }
    public bool IsPowerSpot { get; set; }
    public bool IsSteelySpirit { get; set; }
    public string? IsSwitching { get; set; }

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
