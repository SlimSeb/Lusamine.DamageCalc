using System;
using System.Collections;
using System.Collections.Generic;

namespace Lusamine.DamageCalc.Data {
  public static class DataKinds {
    public const string Ability = "Ability";
    public const string Item = "Item";
    public const string Move = "Move";
    public const string Species = "Species";
    public const string Type = "Type";
    public const string Nature = "Nature";
  }

  public enum StatId {
    Hp,
    Atk,
    Def,
    Spa,
    Spd,
    Spe,
    Spc,
  }

  public static class StatIds {
    public static readonly StatId[] Standard = {
      StatId.Hp, StatId.Atk, StatId.Def, StatId.Spa, StatId.Spd, StatId.Spe,
    };
  }

  public sealed class StatsTable {
    public int Hp { get; set; }
    public int Atk { get; set; }
    public int Def { get; set; }
    public int Spa { get; set; }
    public int Spd { get; set; }
    public int Spe { get; set; }
    public int Spc { get; set; }

    public int this[StatId stat] {
      get {
        switch (stat) {
          case StatId.Hp: return Hp;
          case StatId.Atk: return Atk;
          case StatId.Def: return Def;
          case StatId.Spa: return Spa;
          case StatId.Spd: return Spd;
          case StatId.Spe: return Spe;
          case StatId.Spc: return Spc;
          default: return 0;
        }
      }
      set {
        switch (stat) {
          case StatId.Hp: Hp = value; break;
          case StatId.Atk: Atk = value; break;
          case StatId.Def: Def = value; break;
          case StatId.Spa: Spa = value; break;
          case StatId.Spd: Spd = value; break;
          case StatId.Spe: Spe = value; break;
          case StatId.Spc: Spc = value; break;
        }
      }
    }

    public StatsTable Clone() {
      return new StatsTable {
        Hp = Hp,
        Atk = Atk,
        Def = Def,
        Spa = Spa,
        Spd = Spd,
        Spe = Spe,
        Spc = Spc,
      };
    }
  }

  public sealed class StatsTableInput {
    public int? Hp { get; set; }
    public int? Atk { get; set; }
    public int? Def { get; set; }
    public int? Spa { get; set; }
    public int? Spd { get; set; }
    public int? Spe { get; set; }
    public int? Spc { get; set; }
  }

  public static class MoveCategories {
    public const string Physical = "Physical";
    public const string Special = "Special";
    public const string Status = "Status";
  }

  public static class GameTypes {
    public const string Singles = "Singles";
    public const string Doubles = "Doubles";
  }

  public static class Terrains {
    public const string Electric = "Electric";
    public const string Grassy = "Grassy";
    public const string Psychic = "Psychic";
    public const string Misty = "Misty";
  }

  public static class Weathers {
    public const string Sand = "Sand";
    public const string Sun = "Sun";
    public const string Rain = "Rain";
    public const string Hail = "Hail";
    public const string Snow = "Snow";
    public const string HarshSunshine = "Harsh Sunshine";
    public const string HeavyRain = "Heavy Rain";
    public const string StrongWinds = "Strong Winds";
  }

  public interface IData {
    string Id { get; }
    string Name { get; }
    string Kind { get; }
  }

  public interface IAbility : IData { }

  public interface IItem : IData {
    IReadOnlyDictionary<string, string>? MegaStone { get; }
    bool IsBerry { get; }
    NaturalGiftData? NaturalGift { get; }
  }

  public sealed class NaturalGiftData {
    public int BasePower { get; set; }
    public string Type { get; set; } = "";
  }

  public interface IMove : IData {
    int BasePower { get; }
    string Type { get; }
    string? Category { get; }
    MoveFlags Flags { get; }
    object? Secondaries { get; }
    string? Target { get; }
    int[]? Recoil { get; }
    bool HasCrashDamage { get; }
    bool MindBlownRecoil { get; }
    bool StruggleRecoil { get; }
    bool WillCrit { get; }
    int[]? Drain { get; }
    int? Priority { get; }
    SelfOrSecondaryEffect? Self { get; }
    bool IgnoreDefensive { get; }
    StatId? OverrideOffensiveStat { get; }
    StatId? OverrideDefensiveStat { get; }
    string? OverrideOffensivePokemon { get; }
    string? OverrideDefensivePokemon { get; }
    bool BreaksProtect { get; }
    bool IsZ { get; }
    ZMoveData? ZMove { get; }
    bool IsMax { get; }
    MaxMoveData? MaxMove { get; }
    object? Multihit { get; }
    bool Multiaccuracy { get; }
  }

  public sealed class MoveFlags {
    public bool Contact { get; set; }
    public bool Bite { get; set; }
    public bool Sound { get; set; }
    public bool Punch { get; set; }
    public bool Bullet { get; set; }
    public bool Pulse { get; set; }
    public bool Slicing { get; set; }
    public bool Wind { get; set; }
  }

  public sealed class SelfOrSecondaryEffect {
    public StatsTableInput? Boosts { get; set; }
  }

  public sealed class ZMoveData {
    public int? BasePower { get; set; }
  }

  public sealed class MaxMoveData {
    public int BasePower { get; set; }
  }

  public interface ISpecie : IData {
    string[] Types { get; }
    StatsTable BaseStats { get; }
    double WeightKg { get; }
    bool Nfe { get; }
    string? Gender { get; }
    string[]? OtherFormes { get; }
    string? BaseSpecies { get; }
    IReadOnlyDictionary<int, string>? Abilities { get; }
  }

  public interface IType : IData {
    IReadOnlyDictionary<string, double> Effectiveness { get; }
  }

  public interface INature : IData {
    StatId? Plus { get; }
    StatId? Minus { get; }
  }

  public interface IDataTable<out T> : IEnumerable<T> where T : class, IData {
    T? Get(string id);
  }

  public interface IGeneration {
    int Num { get; }
    IDataTable<IAbility> Abilities { get; }
    IDataTable<IItem> Items { get; }
    IDataTable<IMove> Moves { get; }
    IDataTable<ISpecie> Species { get; }
    IDataTable<IType> Types { get; }
    IDataTable<INature> Natures { get; }
  }

  public interface IGenerations {
    IGeneration Get(int gen);
  }

  public sealed class DataTable<T> : IDataTable<T> where T : class, IData {
    private readonly Dictionary<string, T> _data;

    public DataTable(Dictionary<string, T> data) {
      _data = data;
    }

    public T? Get(string id) {
      if (_data.TryGetValue(id, out var value)) return value;
      return null;
    }

    public IEnumerator<T> GetEnumerator() {
      return _data.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }
  }
}
