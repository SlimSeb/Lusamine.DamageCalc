using System.Collections.Generic;
using DamageCalc;
using DamageCalc.Data;
using NUnit;
using NUnit.Framework;

namespace DamageCalc.Tests {
  public sealed class DamageCalcTests {
    private static IGeneration CreateGen1() {
      IDataTable<IAbility> abilities = new DataTable<IAbility>(new Dictionary<string, IAbility>());
      var items = new DataTable<Item>(new Dictionary<string, Item>());

      var moves = new DataTable<MoveData>(new Dictionary<string, MoveData> {
        {
          Util.ToId("Seismic Toss"),
          new MoveData {
            Id = Util.ToId("Seismic Toss"),
            Name = "Seismic Toss",
            Kind = DataKinds.Move,
            BasePower = 0,
            Type = "Fighting",
            Category = MoveCategories.Physical,
            Flags = new MoveFlags(),
          }
        },
        {
	        Util.ToId("Tackle"),
	        new MoveData {
		        Id = Util.ToId("Tackle"),
		        Name = "Tackle",
		        Kind = DataKinds.Move,
		        BasePower = 35,
		        Type = "Normal",
		        Category = MoveCategories.Physical,
		        Flags = new MoveFlags(),
	        }
        },
        {
	        Util.ToId("Psychic"),
	        new MoveData {
		        Id = Util.ToId("Psychic"),
		        Name = "Psychic",
		        Kind = DataKinds.Move,
		        BasePower = 90,
		        Type = "Psychic",
		        Category = MoveCategories.Special,
		        Flags = new MoveFlags(),
	        }
        }
      });

      var species = new DataTable<Specie>(new Dictionary<string, Specie> {
        {
          Util.ToId("Mew"),
          new Specie {
            Id = Util.ToId("Mew"),
            Name = "Mew",
            Kind = DataKinds.Species,
            Types = new[] { "Psychic" },
            WeightKg = 4.0,
            BaseStats = new StatsTable { Hp = 100, Atk = 100, Def = 100, Spa = 100, Spd = 100, Spe = 100 },
            Abilities = new Dictionary<int, string> { { 0, "" } },
          }
        },
        {
          Util.ToId("Vulpix"),
          new Specie {
            Id = Util.ToId("Vulpix"),
            Name = "Vulpix",
            Kind = DataKinds.Species,
            Types = new[] { "Fire" },
            WeightKg = 9.9,
            BaseStats = new StatsTable { Hp = 38, Atk = 41, Def = 40, Spa = 50, Spd = 65, Spe = 65 },
            Abilities = new Dictionary<int, string> { { 0, "" } },
          }
        }
      });

      var types = new DataTable<TypeData>(new Dictionary<string, TypeData> {
        { Util.ToId("Fighting"), new TypeData { Id = Util.ToId("Fighting"), Name = "Fighting", Kind = DataKinds.Type, Effectiveness = new Dictionary<string, double>() } },
        { Util.ToId("Psychic"), new TypeData { Id = Util.ToId("Psychic"), Name = "Psychic", Kind = DataKinds.Type, Effectiveness = new Dictionary<string, double>{ ["Fire"] = 1.0 } }},
        { Util.ToId("Fire"), new TypeData { Id = Util.ToId("Fire"), Name = "Fire", Kind = DataKinds.Type, Effectiveness = new Dictionary<string, double>() } },
      });

      var natures = new DataTable<Nature>(new Dictionary<string, Nature>());

      return new Generation(1, abilities, items, moves, species, types, natures);
    }

    [Test]
    public void SeismicToss_IsFixedDamage_Gen1() {
      var gen = CreateGen1();
      var mew = new Pokemon(gen, "Mew", new State.Pokemon { Level = 50 });
      var vulpix = new Pokemon(gen, "Vulpix");
      var move = new Move(gen, "Seismic Toss");
      //var move = new Move(gen, "Tackle");
      //var move = new Move(gen, "Psychic");

      var result = Calc.Calculate(gen, mew, vulpix, move);
      Assert.That(result.Damage, Is.EqualTo(50));
      Assert.That(result.Range(), Is.EqualTo((50, 50)));
    }
  }
}
