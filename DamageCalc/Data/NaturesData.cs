using System.Collections.Generic;

namespace DamageCalc.Data {
  public static class NaturesData {
    public static readonly Dictionary<string, (StatId plus, StatId minus)> Natures =
      new Dictionary<string, (StatId plus, StatId minus)> {
        { "Adamant", (StatId.Atk, StatId.Spa) },
        { "Bashful", (StatId.Spa, StatId.Spa) },
        { "Bold", (StatId.Def, StatId.Atk) },
        { "Brave", (StatId.Atk, StatId.Spe) },
        { "Calm", (StatId.Spd, StatId.Atk) },
        { "Careful", (StatId.Spd, StatId.Spa) },
        { "Docile", (StatId.Def, StatId.Def) },
        { "Gentle", (StatId.Spd, StatId.Def) },
        { "Hardy", (StatId.Atk, StatId.Atk) },
        { "Hasty", (StatId.Spe, StatId.Def) },
        { "Impish", (StatId.Def, StatId.Spa) },
        { "Jolly", (StatId.Spe, StatId.Spa) },
        { "Lax", (StatId.Def, StatId.Spd) },
        { "Lonely", (StatId.Atk, StatId.Def) },
        { "Mild", (StatId.Spa, StatId.Def) },
        { "Modest", (StatId.Spa, StatId.Atk) },
        { "Naive", (StatId.Spe, StatId.Spd) },
        { "Naughty", (StatId.Atk, StatId.Spd) },
        { "Quiet", (StatId.Spa, StatId.Spe) },
        { "Quirky", (StatId.Spd, StatId.Spd) },
        { "Rash", (StatId.Spa, StatId.Spd) },
        { "Relaxed", (StatId.Def, StatId.Spe) },
        { "Sassy", (StatId.Spd, StatId.Spe) },
        { "Serious", (StatId.Spe, StatId.Spe) },
        { "Timid", (StatId.Spe, StatId.Atk) },
      };

    private static readonly Dictionary<string, INature> NaturesById = BuildNaturesById();

    public static IDataTable<INature> Table { get; } = new DataTable<INature>(NaturesById);

    private static Dictionary<string, INature> BuildNaturesById() {
      var map = new Dictionary<string, INature>();
      foreach (var kvp in Natures) {
        var name = kvp.Key;
        var (plus, minus) = kvp.Value;
        var nature = new Nature {
          Id = Util.ToId(name),
          Name = name,
          Kind = DataKinds.Nature,
          Plus = plus,
          Minus = minus,
        };
        map[nature.Id] = nature;
      }
      return map;
    }
  }
}
