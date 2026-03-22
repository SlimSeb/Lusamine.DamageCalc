using System.Collections.Generic;
using DamageCalc.Data;
using NUnit.Framework;

namespace DamageCalc.Tests {
  public sealed class PokemonTests {
    [Test]
    public void Defaults() {
      var gen = TestHelper.Gen(7);
      var p = new Pokemon(gen, "Gengar");

      Assert.That(p.Name, Is.EqualTo("Gengar"));
      Assert.That(p.Types, Is.EqualTo(new[] { "Ghost", "Poison" }));
      Assert.That(p.WeightKg, Is.EqualTo(40.5));
      Assert.That(p.Level, Is.EqualTo(100));
      Assert.That(p.Gender, Is.EqualTo("M"));
      Assert.That(p.Item, Is.Null);
      Assert.That(p.Ability, Is.EqualTo("Cursed Body"));
      Assert.That(p.Nature, Is.EqualTo("Serious"));
      Assert.That(p.Status, Is.EqualTo(""));
      Assert.That(p.HasStatus(), Is.False);
      Assert.That(p.ToxicCounter, Is.EqualTo(0));
      Assert.That(p.CurHP(), Is.EqualTo(p.Stats.Hp));

      foreach (var stat in StatIds.Standard) {
        Assert.That(p.Ivs[stat], Is.EqualTo(31));
        Assert.That(p.Evs[stat], Is.EqualTo(0));
        Assert.That(p.Boosts[stat], Is.EqualTo(0));
      }

      Assert.That(p.Stats.Hp, Is.EqualTo(261));
      Assert.That(p.Stats.Atk, Is.EqualTo(166));
      Assert.That(p.Stats.Def, Is.EqualTo(156));
      Assert.That(p.Stats.Spa, Is.EqualTo(296));
      Assert.That(p.Stats.Spd, Is.EqualTo(186));
      Assert.That(p.Stats.Spe, Is.EqualTo(256));
    }

    [Test]
    public void PokemonFull() {
      var gen = TestHelper.Gen(7);
      var p = new Pokemon(gen, "Suicune", new State.Pokemon {
        Level = 50,
        Ability = "Inner Focus",
        Item = "Leftovers",
        Nature = "Bold",
        Ivs = new StatsTableInput { Spa = 30 },
        Evs = new StatsTableInput { Spd = 4, Def = 252, Hp = 252 },
        Boosts = new StatsTableInput { Atk = -1, Spa = 2, Spd = 1 },
        CurHP = 60,
        Status = "tox",
        ToxicCounter = 2,
        Moves = new List<string> { "Surf", "Rest", "Curse", "Sleep Talk" },
      });

      Assert.That(p.Name, Is.EqualTo("Suicune"));
      Assert.That(p.Types, Is.EqualTo(new[] { "Water" }));
      Assert.That(p.WeightKg, Is.EqualTo(187.0));
      Assert.That(p.Level, Is.EqualTo(50));
      Assert.That(p.Gender, Is.EqualTo("N"));
      Assert.That(p.Item, Is.EqualTo("Leftovers"));
      Assert.That(p.Ability, Is.EqualTo("Inner Focus"));
      Assert.That(p.Nature, Is.EqualTo("Bold"));
      Assert.That(p.Status, Is.EqualTo("tox"));
      Assert.That(p.ToxicCounter, Is.EqualTo(2));
      Assert.That(p.CurHP(), Is.EqualTo(60));

      Assert.That(p.Ivs.Hp, Is.EqualTo(31));
      Assert.That(p.Ivs.Atk, Is.EqualTo(31));
      Assert.That(p.Ivs.Def, Is.EqualTo(31));
      Assert.That(p.Ivs.Spa, Is.EqualTo(30));
      Assert.That(p.Ivs.Spd, Is.EqualTo(31));
      Assert.That(p.Ivs.Spe, Is.EqualTo(31));

      Assert.That(p.Evs.Hp, Is.EqualTo(252));
      Assert.That(p.Evs.Atk, Is.EqualTo(0));
      Assert.That(p.Evs.Def, Is.EqualTo(252));
      Assert.That(p.Evs.Spa, Is.EqualTo(0));
      Assert.That(p.Evs.Spd, Is.EqualTo(4));
      Assert.That(p.Evs.Spe, Is.EqualTo(0));

      Assert.That(p.Boosts.Hp, Is.EqualTo(0));
      Assert.That(p.Boosts.Atk, Is.EqualTo(-1));
      Assert.That(p.Boosts.Def, Is.EqualTo(0));
      Assert.That(p.Boosts.Spa, Is.EqualTo(2));
      Assert.That(p.Boosts.Spd, Is.EqualTo(1));
      Assert.That(p.Boosts.Spe, Is.EqualTo(0));

      Assert.That(p.Stats.Hp, Is.EqualTo(207));
      Assert.That(p.Stats.Atk, Is.EqualTo(85));
      Assert.That(p.Stats.Def, Is.EqualTo(183));
      Assert.That(p.Stats.Spa, Is.EqualTo(110));
      Assert.That(p.Stats.Spd, Is.EqualTo(136));
      Assert.That(p.Stats.Spe, Is.EqualTo(105));

      Assert.That(p.Moves, Is.EqualTo(new List<string> { "Surf", "Rest", "Curse", "Sleep Talk" }));
    }

    [Test]
    public void Gen1() {
      var gen = TestHelper.Gen(1);
      var p = new Pokemon(gen, "Tauros", new State.Pokemon {
        Level = 100,
        Ivs = new StatsTableInput { Spc = 20, Def = 16 },
        Evs = new StatsTableInput { Atk = 200 },
        CurHP = 500,
      });

      Assert.That(p.Ivs.Hp, Is.EqualTo(20));
      Assert.That(p.Ivs.Atk, Is.EqualTo(31));
      Assert.That(p.Ivs.Def, Is.EqualTo(16));
      Assert.That(p.Ivs.Spa, Is.EqualTo(20));
      Assert.That(p.Ivs.Spd, Is.EqualTo(20));
      Assert.That(p.Ivs.Spe, Is.EqualTo(31));

      Assert.That(p.Evs.Hp, Is.EqualTo(252));
      Assert.That(p.Evs.Atk, Is.EqualTo(200));
      Assert.That(p.Evs.Def, Is.EqualTo(252));
      Assert.That(p.Evs.Spa, Is.EqualTo(252));
      Assert.That(p.Evs.Spd, Is.EqualTo(252));
      Assert.That(p.Evs.Spe, Is.EqualTo(252));

      Assert.That(p.Stats.Hp, Is.EqualTo(343));
      Assert.That(p.Stats.Atk, Is.EqualTo(298));
      Assert.That(p.Stats.Def, Is.EqualTo(274));
      Assert.That(p.Stats.Spa, Is.EqualTo(228));
      Assert.That(p.Stats.Spd, Is.EqualTo(228));
      Assert.That(p.Stats.Spe, Is.EqualTo(318));

      Assert.That(p.CurHP(), Is.EqualTo(p.MaxHP()));
    }

    [Test]
    public void GetForme() {
      Assert.That(Pokemon.GetForme(TestHelper.Gen(1), "Gengar"), Is.EqualTo("Gengar"));

      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Gengar", "Black Sludge", "Hypnosis"), Is.EqualTo("Gengar"));
      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Gengar", "Gengarite", "Hypnosis"), Is.EqualTo("Gengar-Mega"));

      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Charizard"), Is.EqualTo("Charizard"));
      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Charizard", "Charizardite X"), Is.EqualTo("Charizard-Mega-X"));
      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Charizard", "Charizardite Y"), Is.EqualTo("Charizard-Mega-Y"));
      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Mewtwo", "Choice Specs", "Psystrike"), Is.EqualTo("Mewtwo"));
      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Mewtwo", "Mewtwonite X", "Psystrike"), Is.EqualTo("Mewtwo-Mega-X"));
      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Mewtwo", "Mewtwonite Y", "Psystrike"), Is.EqualTo("Mewtwo-Mega-Y"));

      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Groudon", "Choice Band", "Earthquake"), Is.EqualTo("Groudon"));
      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Groudon", "Red Orb", "Earthquake"), Is.EqualTo("Groudon-Primal"));
      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Kyogre", "Choice Specs", "Surf"), Is.EqualTo("Kyogre"));
      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Kyogre", "Blue Orb", "Surf"), Is.EqualTo("Kyogre-Primal"));

      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Meloetta", "Leftovers", "Psychic"), Is.EqualTo("Meloetta"));
      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Meloetta", "Leftovers", "Relic Song"), Is.EqualTo("Meloetta-Pirouette"));
      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Rayquaza", null, "Earthquake"), Is.EqualTo("Rayquaza"));
      Assert.That(Pokemon.GetForme(TestHelper.Gen(7), "Rayquaza", null, "Dragon Ascent"), Is.EqualTo("Rayquaza-Mega"));
    }

    [Test]
    public void HasType() {
      var p = new Pokemon(TestHelper.Gen(7), "Gengar");
      Assert.That(p.HasType("Ghost"), Is.True);
      Assert.That(p.HasType("Poison"), Is.True);
      Assert.That(p.HasType("Fire"), Is.False);
      Assert.That(p.HasType("Ice"), Is.False);
    }

    [Test]
    public void GigantamaxWeights() {
      var gen = TestHelper.Gen(8);
      Assert.That(new Pokemon(gen, "Venusaur-Gmax").WeightKg, Is.EqualTo(100));
      Assert.That(new Pokemon(gen, "Venusaur-Gmax", new State.Pokemon { IsDynamaxed = true }).WeightKg, Is.EqualTo(0));
      Assert.That(new Pokemon(gen, "Venusaur-Gmax", new State.Pokemon { Overrides = new Specie { WeightKg = 50 } }).WeightKg, Is.EqualTo(50));
    }
  }
}
