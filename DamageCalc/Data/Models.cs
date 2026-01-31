using System;
using System.Collections.Generic;

namespace DamageCalc.Data {
  public sealed class Ability : IAbility {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = DataKinds.Ability;

    public Ability Clone() {
      return new Ability { Id = Id, Name = Name, Kind = Kind };
    }
  }

  public sealed class Item : IItem {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = DataKinds.Item;
    public IReadOnlyDictionary<string, string>? MegaStone { get; set; }
    public bool IsBerry { get; set; }
    public NaturalGiftData? NaturalGift { get; set; }

    public Item Clone() {
      return new Item {
        Id = Id,
        Name = Name,
        Kind = Kind,
        MegaStone = MegaStone == null ? null : new Dictionary<string, string>(MegaStone),
        IsBerry = IsBerry,
        NaturalGift = NaturalGift == null ? null : new NaturalGiftData {
          BasePower = NaturalGift.BasePower,
          Type = NaturalGift.Type,
        },
      };
    }

    public void ApplyOverrides(Item? overrides) {
      if (overrides == null) return;
      Id = overrides.Id ?? Id;
      Name = overrides.Name ?? Name;
      Kind = overrides.Kind ?? Kind;
      if (overrides.MegaStone != null) MegaStone = overrides.MegaStone;
      IsBerry = overrides.IsBerry;
      if (overrides.NaturalGift != null) NaturalGift = overrides.NaturalGift;
    }
  }

  public sealed class MoveData : IMove {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = DataKinds.Move;
    public int BasePower { get; set; }
    public string Type { get; set; } = "";
    public string? Category { get; set; }
    public MoveFlags Flags { get; set; } = new MoveFlags();
    public object? Secondaries { get; set; }
    public string? Target { get; set; }
    public int[]? Recoil { get; set; }
    public bool HasCrashDamage { get; set; }
    public bool MindBlownRecoil { get; set; }
    public bool StruggleRecoil { get; set; }
    public bool WillCrit { get; set; }
    public int[]? Drain { get; set; }
    public int? Priority { get; set; }
    public SelfOrSecondaryEffect? Self { get; set; }
    public bool IgnoreDefensive { get; set; }
    public StatId? OverrideOffensiveStat { get; set; }
    public StatId? OverrideDefensiveStat { get; set; }
    public string? OverrideOffensivePokemon { get; set; }
    public string? OverrideDefensivePokemon { get; set; }
    public bool BreaksProtect { get; set; }
    public bool IsZ { get; set; }
    public ZMoveData? ZMove { get; set; }
    public bool IsMax { get; set; }
    public MaxMoveData? MaxMove { get; set; }
    public object? Multihit { get; set; }
    public bool Multiaccuracy { get; set; }

    public MoveData Clone() {
      return new MoveData {
        Id = Id,
        Name = Name,
        Kind = Kind,
        BasePower = BasePower,
        Type = Type,
        Category = Category,
        Flags = new MoveFlags {
          Contact = Flags.Contact,
          Bite = Flags.Bite,
          Sound = Flags.Sound,
          Punch = Flags.Punch,
          Bullet = Flags.Bullet,
          Pulse = Flags.Pulse,
          Slicing = Flags.Slicing,
          Wind = Flags.Wind,
        },
        Secondaries = Secondaries,
        Target = Target,
        Recoil = Recoil == null ? null : (int[])Recoil.Clone(),
        HasCrashDamage = HasCrashDamage,
        MindBlownRecoil = MindBlownRecoil,
        StruggleRecoil = StruggleRecoil,
        WillCrit = WillCrit,
        Drain = Drain == null ? null : (int[])Drain.Clone(),
        Priority = Priority,
        Self = Self == null ? null : new SelfOrSecondaryEffect {
          Boosts = Self.Boosts == null ? null : new StatsTableInput {
            Hp = Self.Boosts.Hp,
            Atk = Self.Boosts.Atk,
            Def = Self.Boosts.Def,
            Spa = Self.Boosts.Spa,
            Spd = Self.Boosts.Spd,
            Spe = Self.Boosts.Spe,
            Spc = Self.Boosts.Spc,
          }
        },
        IgnoreDefensive = IgnoreDefensive,
        OverrideOffensiveStat = OverrideOffensiveStat,
        OverrideDefensiveStat = OverrideDefensiveStat,
        OverrideOffensivePokemon = OverrideOffensivePokemon,
        OverrideDefensivePokemon = OverrideDefensivePokemon,
        BreaksProtect = BreaksProtect,
        IsZ = IsZ,
        ZMove = ZMove == null ? null : new ZMoveData { BasePower = ZMove.BasePower },
        IsMax = IsMax,
        MaxMove = MaxMove == null ? null : new MaxMoveData { BasePower = MaxMove.BasePower },
        Multihit = Multihit,
        Multiaccuracy = Multiaccuracy,
      };
    }

    public void ApplyOverrides(MoveData? overrides) {
      if (overrides == null) return;
      if (!string.IsNullOrEmpty(overrides.Id)) Id = overrides.Id;
      if (!string.IsNullOrEmpty(overrides.Name)) Name = overrides.Name;
      if (!string.IsNullOrEmpty(overrides.Kind)) Kind = overrides.Kind;
      if (overrides.BasePower != 0) BasePower = overrides.BasePower;
      if (!string.IsNullOrEmpty(overrides.Type)) Type = overrides.Type;
      if (overrides.Category != null) Category = overrides.Category;
      if (overrides.Flags != null) Flags = overrides.Flags;
      if (overrides.Secondaries != null) Secondaries = overrides.Secondaries;
      if (overrides.Target != null) Target = overrides.Target;
      if (overrides.Recoil != null) Recoil = overrides.Recoil;
      if (overrides.HasCrashDamage) HasCrashDamage = true;
      if (overrides.MindBlownRecoil) MindBlownRecoil = true;
      if (overrides.StruggleRecoil) StruggleRecoil = true;
      if (overrides.WillCrit) WillCrit = true;
      if (overrides.Drain != null) Drain = overrides.Drain;
      if (overrides.Priority.HasValue) Priority = overrides.Priority;
      if (overrides.Self != null) Self = overrides.Self;
      if (overrides.IgnoreDefensive) IgnoreDefensive = true;
      if (overrides.OverrideOffensiveStat.HasValue) OverrideOffensiveStat = overrides.OverrideOffensiveStat;
      if (overrides.OverrideDefensiveStat.HasValue) OverrideDefensiveStat = overrides.OverrideDefensiveStat;
      if (overrides.OverrideOffensivePokemon != null) OverrideOffensivePokemon = overrides.OverrideOffensivePokemon;
      if (overrides.OverrideDefensivePokemon != null) OverrideDefensivePokemon = overrides.OverrideDefensivePokemon;
      if (overrides.BreaksProtect) BreaksProtect = true;
      if (overrides.IsZ) IsZ = true;
      if (overrides.ZMove != null) ZMove = overrides.ZMove;
      if (overrides.IsMax) IsMax = true;
      if (overrides.MaxMove != null) MaxMove = overrides.MaxMove;
      if (overrides.Multihit != null) Multihit = overrides.Multihit;
      if (overrides.Multiaccuracy) Multiaccuracy = true;
    }
  }

  public sealed class Specie : ISpecie {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = DataKinds.Species;
    public string[] Types { get; set; } = Array.Empty<string>();
    public StatsTable BaseStats { get; set; } = new StatsTable();
    public double WeightKg { get; set; }
    public bool Nfe { get; set; }
    public string? Gender { get; set; }
    public string[]? OtherFormes { get; set; }
    public string? BaseSpecies { get; set; }
    public IReadOnlyDictionary<int, string>? Abilities { get; set; }

    public Specie Clone() {
      return new Specie {
        Id = Id,
        Name = Name,
        Kind = Kind,
        Types = (string[])Types.Clone(),
        BaseStats = BaseStats.Clone(),
        WeightKg = WeightKg,
        Nfe = Nfe,
        Gender = Gender,
        OtherFormes = OtherFormes == null ? null : (string[])OtherFormes.Clone(),
        BaseSpecies = BaseSpecies,
        Abilities = Abilities == null ? null : new Dictionary<int, string>(Abilities),
      };
    }

    public void ApplyOverrides(Specie? overrides) {
      if (overrides == null) return;
      if (!string.IsNullOrEmpty(overrides.Id)) Id = overrides.Id;
      if (!string.IsNullOrEmpty(overrides.Name)) Name = overrides.Name;
      if (!string.IsNullOrEmpty(overrides.Kind)) Kind = overrides.Kind;
      if (overrides.Types.Length > 0) Types = overrides.Types;
      if (overrides.BaseStats != null) BaseStats = overrides.BaseStats;
      if (overrides.WeightKg != 0) WeightKg = overrides.WeightKg;
      if (overrides.Nfe) Nfe = true;
      if (overrides.Gender != null) Gender = overrides.Gender;
      if (overrides.OtherFormes != null) OtherFormes = overrides.OtherFormes;
      if (overrides.BaseSpecies != null) BaseSpecies = overrides.BaseSpecies;
      if (overrides.Abilities != null) Abilities = overrides.Abilities;
    }
  }

  public sealed class TypeData : IType {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = DataKinds.Type;
    public IReadOnlyDictionary<string, double> Effectiveness { get; set; } = new Dictionary<string, double>();

    public TypeData Clone() {
      return new TypeData {
        Id = Id,
        Name = Name,
        Kind = Kind,
        Effectiveness = new Dictionary<string, double>(Effectiveness),
      };
    }
  }

  public sealed class Nature : INature {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = DataKinds.Nature;
    public StatId? Plus { get; set; }
    public StatId? Minus { get; set; }

    public Nature Clone() {
      return new Nature { Id = Id, Name = Name, Kind = Kind, Plus = Plus, Minus = Minus };
    }
  }
}
