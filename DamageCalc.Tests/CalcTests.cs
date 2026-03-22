using System.Collections.Generic;
using DamageCalc.Data;
using NUnit.Framework;

namespace DamageCalc.Tests {
  public sealed class CalcTests {
    private static Pokemon P(int gen, string name, State.Pokemon? options = null) => TestHelper.Pokemon(gen, name, options);
    private static Move M(int gen, string name, State.Move? options = null) => TestHelper.Move(gen, name, options);
    private static Field F(State.Field? field = null) => TestHelper.Field(field);
    private static Result C(int gen, Pokemon attacker, Pokemon defender, Move move, Field? field = null) =>
      TestHelper.Calculate(gen, attacker, defender, move, field);

    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void GrassKnot(int gen) {
      var result = C(gen, P(gen, "Groudon"), P(gen, "Groudon"), M(gen, "Grass Knot"));
      Assert.That(result.Range(), Is.EqualTo((190, 224)));
    }

    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void ArceusPlate(int gen) {
      var result = C(
        gen,
        P(gen, "Arceus", new State.Pokemon { Item = "Meadow Plate" }),
        P(gen, "Blastoise"),
        M(gen, "Judgment")
      );
      Assert.That(result.Range(), Is.EqualTo((194, 230)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 SpA Meadow Plate Arceus Judgment vs. 0 HP / 0 SpD Blastoise: 194-230 (64.8 - 76.9%) -- guaranteed 2HKO"
      ));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void NightShadeSeismicToss(int gen) {
      var mew = P(gen, "Mew", new State.Pokemon { Level = 50 });
      var vulpix = P(gen, "Vulpix");
      foreach (var move in new[] { M(gen, "Seismic Toss"), M(gen, "Night Shade") }) {
        var result = C(gen, mew, vulpix, move);
        Assert.That(result.Damage, Is.EqualTo(50));
        var expected = gen < 3
          ? $"Lvl 50 Mew {move.Name} vs. Vulpix: 50-50 (17.9 - 17.9%) -- guaranteed 6HKO"
          : $"Lvl 50 Mew {move.Name} vs. 0 HP Vulpix: 50-50 (23 - 23%) -- guaranteed 5HKO";
        Assert.That(result.Desc(), Is.EqualTo(expected));
      }
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void CometPunch(int gen) {
      var result = C(gen, P(gen, "Snorlax"), P(gen, "Vulpix"), M(gen, "Comet Punch"));
      TestHelper.AssertMatch(result, gen, new Dictionary<int, ResultBreakdown> {
        { 1, new ResultBreakdown { Range = (108, 129), Desc = "Snorlax Comet Punch (3 hits) vs. Vulpix", Result = "(38.7 - 46.2%) -- guaranteed 3HKO" } },
        { 3, new ResultBreakdown { Range = (132, 156), Desc = "0 Atk Snorlax Comet Punch (3 hits) vs. 0 HP / 0 Def Vulpix", Result = "(60.8 - 71.8%) -- guaranteed 2HKO" } },
        { 4, new ResultBreakdown { Range = (129, 156), Result = "(59.4 - 71.8%) -- guaranteed 2HKO" } },
      });
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void Immunity(int gen) {
      var result = C(gen, P(gen, "Snorlax"), P(gen, "Gengar"), M(gen, "Hyper Beam"));
      Assert.That(result.Damage, Is.EqualTo(0));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void NonDamaging(int gen) {
      var result = C(gen, P(gen, "Snorlax"), P(gen, "Vulpix"), M(gen, "Barrier"));
      Assert.That(result.Damage, Is.EqualTo(0));
      Assert.That(result.Desc(), Is.EqualTo("Snorlax Barrier vs. Vulpix: 0-0 (0 - 0%)"));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void Protect(int gen) {
      var field = F(new State.Field { DefenderSide = new State.Side { IsProtected = true } });
      var snorlax = P(gen, "Snorlax");
      var chansey = P(gen, "Chansey");
      var result = C(gen, snorlax, chansey, M(gen, "Hyper Beam"), field);
      Assert.That(result.Damage, Is.EqualTo(0));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void CriticalHitsIgnoreAttackDecreases(int gen) {
      var field = F(new State.Field { DefenderSide = new State.Side { IsReflect = true } });

      var mew = P(gen, "Mew", new State.Pokemon { Status = "brn" });
      var vulpix = P(gen, "Vulpix");
      var explosion = M(gen, "Explosion", new State.Move { IsCrit = true });
      var result = C(gen, mew, vulpix, explosion, field);
      mew.Boosts.Atk = 2;
      vulpix.Boosts.Def = 2;
      if (gen < 2) {
        Assert.That(result.Range(), Is.EqualTo((799, 939)));
        Assert.That(result.Desc(), Is.EqualTo(
          "Mew Explosion vs. Vulpix on a critical hit: 799-939 (286.3 - 336.5%) -- guaranteed OHKO"
        ));
      } else if (gen < 5 && gen > 2) {
        Assert.That(result.Range(), Is.EqualTo((729, 858)));
        Assert.That(result.Desc(), Is.EqualTo(
          "0 Atk burned Mew Explosion vs. 0 HP / 0 Def Vulpix on a critical hit: 729-858 (335.9 - 395.3%) -- guaranteed OHKO"
        ));
      } else if (gen == 5) {
        Assert.That(result.Range(), Is.EqualTo((364, 429)));
        Assert.That(result.Desc(), Is.EqualTo(
          "0 Atk burned Mew Explosion vs. 0 HP / 0 Def Vulpix on a critical hit: 364-429 (167.7 - 197.6%) -- guaranteed OHKO"
        ));
      } else if (gen >= 6) {
        Assert.That(result.Range(), Is.EqualTo((273, 321)));
        Assert.That(result.Desc(), Is.EqualTo(
          "0 Atk burned Mew Explosion vs. 0 HP / 0 Def Vulpix on a critical hit: 273-321 (125.8 - 147.9%) -- guaranteed OHKO"
        ));
      }
      explosion.IsCrit = false;
      result = C(gen, mew, vulpix, explosion, field);
      if (gen == 1) {
        Assert.That(result.Range(), Is.EqualTo((102, 120)));
      } else if (gen == 2) {
        Assert.That(result.Range(), Is.EqualTo((149, 176)));
      } else if (gen > 2 && gen < 5) {
        Assert.That(result.Range(), Is.EqualTo((182, 215)));
      } else {
        Assert.That(result.Range(), Is.EqualTo((91, 107)));
      }
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void StruggleVsGhost(int gen) {
      var result = C(gen, P(gen, "Mew"), P(gen, "Gengar"), M(gen, "Struggle"));
      if (gen < 2) {
        Assert.That(result.Range().max, Is.EqualTo(0));
      } else {
        Assert.That(result.Range().max, Is.GreaterThan(0));
      }
    }

    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void WeatherBall_ChangesType(int gen) {
      var weathers = new[] {
        new {
          Weather = "Sun",
          Type = "Fire",
          Adv = new { Range = (346, 408), Desc = "(149.7 - 176.6%) -- guaranteed OHKO" },
          Dpp = new { Range = (342, 404), Desc = "(148 - 174.8%) -- guaranteed OHKO" },
          Modern = new { Range = (344, 408), Desc = "(148.9 - 176.6%) -- guaranteed OHKO" },
        },
        new {
          Weather = "Rain",
          Type = "Water",
          Adv = new { Range = (86, 102), Desc = "(37.2 - 44.1%) -- guaranteed 3HKO" },
          Dpp = new { Range = (85, 101), Desc = "(36.7 - 43.7%) -- guaranteed 3HKO" },
          Modern = new { Range = (86, 102), Desc = "(37.2 - 44.1%) -- guaranteed 3HKO" },
        },
        new {
          Weather = "Sand",
          Type = "Rock",
          Adv = new { Range = (96, 114), Desc = "(41.5 - 49.3%) -- 82.4% chance to 2HKO after sandstorm damage" },
          Dpp = new { Range = (77, 91), Desc = "(33.3 - 39.3%) -- guaranteed 3HKO after sandstorm damage" },
          Modern = new { Range = (77, 91), Desc = "(33.3 - 39.3%) -- guaranteed 3HKO after sandstorm damage" },
        },
        new {
          Weather = "Hail",
          Type = "Ice",
          Adv = new { Range = (234, 276), Desc = "(101.2 - 119.4%) -- guaranteed OHKO" },
          Dpp = new { Range = (230, 272), Desc = "(99.5 - 117.7%) -- 93.8% chance to OHKO (guaranteed OHKO after hail damage)" },
          Modern = new { Range = (230, 272), Desc = "(99.5 - 117.7%) -- 93.8% chance to OHKO (guaranteed OHKO after hail damage)" },
        },
      };

      foreach (var w in weathers) {
        var dmg = gen == 3 ? w.Adv : gen == 4 ? w.Dpp : w.Modern;
        var atk = (gen == 3 && w.Type == "Rock") ? "Atk" : "SpA";
        var def = (gen == 3 && w.Type == "Rock") ? "Def" : "SpD";

        var result = C(
          gen,
          P(gen, "Castform"),
          P(gen, "Bulbasaur"),
          M(gen, "Weather Ball"),
          F(new State.Field { Weather = w.Weather })
        );
        Assert.That(result.Range(), Is.EqualTo(dmg.Range));
        Assert.That(result.Desc(), Is.EqualTo(
          $"0 {atk} Castform Weather Ball (100 BP {w.Type}) vs. 0 HP / 0 {def} Bulbasaur in {w.Weather}: {dmg.Range.Item1}-{dmg.Range.Item2} {dmg.Desc}"
        ));
      }
    }

    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void FlyingPress(int gen) {
      var attacker = P(gen, "Hawlucha");
      var flyingPress = M(gen, "Flying Press");
      var result = C(gen, attacker, P(gen, "Cacturne"), flyingPress);
      if (gen == 6) {
        Assert.That(result.Range(), Is.EqualTo((484, 576)));
        Assert.That(result.Desc(), Is.EqualTo(
          "0 Atk Hawlucha Flying Press vs. 0 HP / 0 Def Cacturne: 484-576 (172.2 - 204.9%) -- guaranteed OHKO"
        ));
      } else {
        Assert.That(result.Range(), Is.EqualTo((612, 720)));
        Assert.That(result.Desc(), Is.EqualTo(
          "0 Atk Hawlucha Flying Press vs. 0 HP / 0 Def Cacturne: 612-720 (217.7 - 256.2%) -- guaranteed OHKO"
        ));
      }

      var result2 = C(gen, attacker, P(gen, "Spiritomb"), flyingPress);
      Assert.That(result2.Range(), Is.EqualTo((0, 0)));

      var scrappyAttacker = P(gen, "Hawlucha", new State.Pokemon { Ability = "Scrappy" });
      var ringTargetSpiritomb = P(gen, "Spiritomb", new State.Pokemon { Item = "Ring Target" });
      var result3 = C(gen, attacker, ringTargetSpiritomb, flyingPress);
      var result4 = C(gen, scrappyAttacker, P(gen, "Spiritomb"), flyingPress);
      if (gen == 6) {
        Assert.That(result3.Range(), Is.EqualTo((152, 180)));
        Assert.That(result4.Range(), Is.EqualTo((152, 180)));
      } else {
        Assert.That(result3.Range(), Is.EqualTo((188, 224)));
        Assert.That(result4.Range(), Is.EqualTo((188, 224)));
      }
    }

    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void ThousandArrows_RingTarget(int gen) {
      var result = C(gen, P(gen, "Zygarde"), P(gen, "Swellow"), M(gen, "Thousand Arrows"));
      Assert.That(result.Range(), Is.EqualTo((147, 174)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Zygarde Thousand Arrows vs. 0 HP / 0 Def Swellow: 147-174 (56.3 - 66.6%) -- guaranteed 2HKO"
      ));
    }

    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void RingTarget_Nullifies(int gen) {
      var attacker = P(gen, "Mew");
      var defender = P(gen, "Skarmory", new State.Pokemon { Item = "Ring Target" });
      var result = C(gen, attacker, defender, M(gen, "Sludge Bomb"));
      Assert.That(result.Range(), Is.EqualTo((87, 103)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 SpA Mew Sludge Bomb vs. 0 HP / 0 SpD Skarmory: 87-103 (32.1 - 38%) -- 94.6% chance to 3HKO"
      ));
      var result2 = C(gen, attacker, defender, M(gen, "Earth Power"));
      Assert.That(result2.Range(), Is.EqualTo((174, 206)));
      Assert.That(result2.Desc(), Is.EqualTo(
        "0 SpA Mew Earth Power vs. 0 HP / 0 SpD Skarmory: 174-206 (64.2 - 76%) -- guaranteed 2HKO"
      ));
    }

    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void IvsShownIfApplicable(int gen) {
      var ivs = new StatsTableInput { Spa = 9, Spd = 9, Hp = 9 };
      var evs = new StatsTableInput { Spa = 9, Spd = 9, Hp = 9 };
      var result = C(gen, P(gen, "Mew", new State.Pokemon { Ivs = ivs }), P(gen, "Mew", new State.Pokemon { Evs = evs }), M(gen, "Psychic"));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 SpA 9 IVs Mew Psychic vs. 9 HP / 9 SpD Mew: 43-51 (12.5 - 14.8%) -- possible 7HKO"
      ));
      result = C(gen, P(gen, "Mew", new State.Pokemon { Evs = evs }), P(gen, "Mew", new State.Pokemon { Ivs = ivs }), M(gen, "Psychic"));
      Assert.That(result.Desc(), Is.EqualTo(
        "9 SpA Mew Psychic vs. 0 HP 9 IVs / 0 SpD 9 IVs Mew: 54-64 (16.9 - 20%) -- possible 5HKO"
      ));
    }

    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void IronBall_NegatesImmunity(int gen) {
      var zapdos = P(gen, "Zapdos", new State.Pokemon { Item = "Iron Ball" });
      if (gen == 4) {
        var result = C(gen, P(gen, "Vibrava"), zapdos, M(gen, "Earthquake"));
        Assert.That(result.Range(), Is.EqualTo((186, 218)));
        Assert.That(result.Desc(), Is.EqualTo(
          "0 Atk Vibrava Earthquake vs. 0 HP / 0 Def Zapdos: 186-218 (57.9 - 67.9%) -- guaranteed 2HKO"
        ));
      } else {
        var result = C(gen, P(gen, "Vibrava"), zapdos, M(gen, "Earthquake"));
        Assert.That(result.Range(), Is.EqualTo((93, 109)));
        Assert.That(result.Desc(), Is.EqualTo(
          "0 Atk Vibrava Earthquake vs. 0 HP / 0 Def Zapdos: 93-109 (28.9 - 33.9%) -- 1.2% chance to 3HKO"
        ));
      }
    }

    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void IronBall_NegatesLevitate(int gen) {
      var result = C(gen, P(gen, "Poliwrath"), P(gen, "Mismagius", new State.Pokemon { Item = "Iron Ball" }), M(gen, "Mud Shot"));
      Assert.That(result.Range(), Is.EqualTo((29, 35)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 SpA Poliwrath Mud Shot vs. 0 HP / 0 SpD Mismagius: 29-35 (11.1 - 13.4%) -- possible 8HKO"
      ));
    }

    [TestCase(8)]
    [TestCase(9)]
    public void Multiscale_ShadowShieldHalvesDamageWithHeavyDutyBoots(int gen) {
      var dragonite2 = P(gen, "Dragonite", new State.Pokemon { Ability = "Shadow Shield", Item = "Heavy-Duty Boots" });
      var field = F(new State.Field { DefenderSide = new State.Side { IsSR = true } });
      var result = C(gen, P(gen, "Abomasnow"), dragonite2, M(gen, "Blizzard"), field);
      Assert.That(result.Range(), Is.EqualTo((222, 264)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 SpA Abomasnow Blizzard vs. 0 HP / 0 SpD Shadow Shield Dragonite: 222-264 (68.7 - 81.7%) -- guaranteed 2HKO"
      ));
    }

    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void Multiscale_ShadowShieldNotHalvesBelowFullHp(int gen) {
      var dragonite1 = P(gen, "Dragonite", new State.Pokemon { Ability = "Multiscale", CurHP = 69 });
      var result = C(gen, P(gen, "Abomasnow"), dragonite1, M(gen, "Ice Shard"));
      Assert.That(result.Range(), Is.EqualTo((168, 204)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Abomasnow Ice Shard vs. 0 HP / 0 Def Dragonite: 168-204 (52 - 63.1%) -- guaranteed OHKO"
      ));
    }

    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void Multiscale_ShadowShieldHalvesDamage(int gen) {
      var dragonite = P(gen, "Dragonite", new State.Pokemon { Ability = "Multiscale" });
      var result = C(gen, P(gen, "Abomasnow"), dragonite, M(gen, "Ice Shard"));
      Assert.That(result.Range(), Is.EqualTo((84, 102)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Abomasnow Ice Shard vs. 0 HP / 0 Def Multiscale Dragonite: 84-102 (26 - 31.5%) -- guaranteed 4HKO"
      ));
    }

    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void Weight_HeavyMetalDoubles(int gen) {
      var result = C(gen, P(gen, "Simisage", new State.Pokemon { Ability = "Gluttony" }), P(gen, "Simisear", new State.Pokemon { Ability = "Heavy Metal" }), M(gen, "Grass Knot"));
      Assert.That(result.RawDesc.MoveBP, Is.EqualTo(80));
    }

    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void Weight_HeavyMetalNegatedByMoldBreaker(int gen) {
      var result = C(gen, P(gen, "Simisage", new State.Pokemon { Ability = "Mold Breaker" }), P(gen, "Simisear", new State.Pokemon { Ability = "Heavy Metal" }), M(gen, "Grass Knot"));
      Assert.That(result.RawDesc.MoveBP, Is.EqualTo(60));
    }

    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void Weight_LightMetalHalves(int gen) {
      var result = C(gen, P(gen, "Simisage", new State.Pokemon { Ability = "Gluttony" }), P(gen, "Registeel", new State.Pokemon { Ability = "Light Metal" }), M(gen, "Grass Knot"));
      Assert.That(result.RawDesc.MoveBP, Is.EqualTo(100));
    }

    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void Weight_LightMetalNegatedByMoldBreaker(int gen) {
      var result = C(gen, P(gen, "Simisage", new State.Pokemon { Ability = "Mold Breaker" }), P(gen, "Registeel", new State.Pokemon { Ability = "Light Metal" }), M(gen, "Grass Knot"));
      Assert.That(result.RawDesc.MoveBP, Is.EqualTo(120));
    }

    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void Weight_FloatStoneHalves(int gen) {
      var result = C(gen, P(gen, "Simisage", new State.Pokemon { Ability = "Gluttony" }), P(gen, "Registeel", new State.Pokemon { Item = "Float Stone" }), M(gen, "Grass Knot"));
      Assert.That(result.RawDesc.MoveBP, Is.EqualTo(100));
    }

    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void Weight_FloatStoneStacksWithLightMetal(int gen) {
      var result = C(gen, P(gen, "Simisage", new State.Pokemon { Ability = "Gluttony" }), P(gen, "Registeel", new State.Pokemon { Ability = "Light Metal", Item = "Float Stone" }), M(gen, "Grass Knot"));
      Assert.That(result.RawDesc.MoveBP, Is.EqualTo(80));
    }

    [Test]
    public void DynamaxHp_Gen8() {
      var munchlax = P(8, "Munchlax", new State.Pokemon { IsDynamaxed = true });
      Assert.That(munchlax.CurHP(), Is.EqualTo(822));
    }

    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void PsychicTerrain(int gen) {
      var field = F(new State.Field { Terrain = "Psychic" });
      var mewtwo = P(gen, "Mewtwo", new State.Pokemon {
        Nature = "Timid",
        Evs = new StatsTableInput { Spa = 252 },
        Boosts = new StatsTableInput { Spa = 2 },
      });
      var milotic = P(gen, "Milotic", new State.Pokemon {
        Item = "Flame Orb",
        Nature = "Bold",
        Ability = "Marvel Scale",
        Evs = new StatsTableInput { Hp = 248, Def = 184 },
        Status = "brn",
        Boosts = new StatsTableInput { Spd = 1 },
      });
      var psystrike = M(gen, "Psystrike");
      var sPunch = M(gen, "Sucker Punch");
      var result = C(gen, mewtwo, milotic, psystrike, field);
      if (gen < 8) {
        Assert.That(result.Range(), Is.EqualTo((331, 391)));
        Assert.That(result.Desc(), Is.EqualTo(
          "+2 252 SpA Mewtwo Psystrike vs. 248 HP / 184+ Def Marvel Scale Milotic in Psychic Terrain: 331-391 (84.2 - 99.4%) -- 37.5% chance to OHKO after burn damage"
        ));
      } else {
        Assert.That(result.Range(), Is.EqualTo((288, 339)));
        Assert.That(result.Desc(), Is.EqualTo(
          "+2 252 SpA Mewtwo Psystrike vs. 248 HP / 184+ Def Marvel Scale Milotic in Psychic Terrain: 288-339 (73.2 - 86.2%) -- guaranteed 2HKO after burn damage"
        ));
      }
      result = C(gen, mewtwo, milotic, sPunch, field);
      Assert.That(result.Range(), Is.EqualTo((0, 0)));
    }

    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void ParentalBond(int gen) {
      var result = C(
        gen,
        P(gen, "Kangaskhan-Mega", new State.Pokemon { Evs = new StatsTableInput { Atk = 152 } }),
        P(gen, "Amoonguss", new State.Pokemon { Nature = "Bold", Evs = new StatsTableInput { Hp = 252, Def = 152 } }),
        M(gen, "Frustration")
      );

      if (gen == 6) {
        Assert.That(result.Damage, Is.EqualTo(new[] {
          new[] { 153, 154, 156, 157, 159, 162, 163, 165, 166, 168, 171, 172, 174, 175, 177, 180 },
          new[] { 76, 76, 78, 78, 79, 81, 81, 82, 82, 84, 85, 85, 87, 87, 88, 90 },
        }));
        Assert.That(result.Desc(), Is.EqualTo(
          "152 Atk Parental Bond Kangaskhan-Mega Frustration vs. 252 HP / 152+ Def Amoonguss: 229-270 (53 - 62.5%) -- guaranteed 2HKO"
        ));
      } else {
        Assert.That(result.Damage, Is.EqualTo(new[] {
          new[] { 153, 154, 156, 157, 159, 162, 163, 165, 166, 168, 171, 172, 174, 175, 177, 180 },
          new[] { 37, 37, 39, 39, 39, 40, 40, 40, 40, 42, 42, 42, 43, 43, 43, 45 },
        }));
        Assert.That(result.Desc(), Is.EqualTo(
          "152 Atk Parental Bond Kangaskhan-Mega Frustration vs. 252 HP / 152+ Def Amoonguss: 190-225 (43.9 - 52%) -- 6.6% chance to 2HKO"
        ));
      }

      result = C(
        gen,
        P(gen, "Kangaskhan-Mega", new State.Pokemon { Level = 88 }),
        P(gen, "Amoonguss"),
        M(gen, "Seismic Toss")
      );
      Assert.That(result.Damage, Is.EqualTo(new[] { 88, 88 }));
      Assert.That(result.Desc(), Is.EqualTo(
        "Lvl 88 Parental Bond Kangaskhan-Mega Seismic Toss vs. 0 HP Amoonguss: 176-176 (47.6 - 47.6%) -- guaranteed 3HKO"
      ));

      result = C(
        gen,
        P(gen, "Kangaskhan-Mega", new State.Pokemon { Evs = new StatsTableInput { Atk = 252 } }),
        P(gen, "Aggron", new State.Pokemon { Level = 72 }),
        M(gen, "Power-Up Punch")
      );
      if (gen == 6) {
        Assert.That(result.Desc(), Is.EqualTo(
          "252 Atk Parental Bond Kangaskhan-Mega Power-Up Punch vs. Lvl 72 0 HP / 0 Def Aggron: 248-296 (120.9 - 144.3%) -- guaranteed OHKO"
        ));
      } else {
        Assert.That(result.Desc(), Is.EqualTo(
          "252 Atk Parental Bond Kangaskhan-Mega Power-Up Punch vs. Lvl 72 0 HP / 0 Def Aggron: 196-236 (95.6 - 115.1%) -- 78.9% chance to OHKO"
        ));
      }

      if (gen == 6) return;

      result = C(
        gen,
        P(gen, "Kangaskhan-Mega", new State.Pokemon { Evs = new StatsTableInput { Atk = 252 } }),
        P(gen, "Lunala"),
        M(gen, "Crunch")
      );

      Assert.That(result.Damage, Is.EqualTo(new[] {
        new[] { 188, 190, 192, 194, 196, 198, 202, 204, 206, 208, 210, 212, 214, 216, 218, 222 },
        new[] { 92, 96, 96, 96, 96, 100, 100, 100, 104, 104, 104, 104, 108, 108, 108, 112 },
      }));
      Assert.That(result.Desc(), Is.EqualTo(
        "252 Atk Parental Bond Kangaskhan-Mega Crunch vs. 0 HP / 0 Def Shadow Shield Lunala: 280-334 (67.4 - 80.4%) -- guaranteed 2HKO"
      ));
    }

    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void KnockOffVsKlutz(int gen) {
      var weavile = P(gen, "Weavile");
      var audino = P(gen, "Audino", new State.Pokemon { Ability = "Klutz", Item = "Leftovers" });
      var audinoMega = P(gen, "Audino", new State.Pokemon { Ability = "Klutz", Item = "Audinite" });
      var knockoff = M(gen, "Knock Off");
      var result = C(gen, weavile, audino, knockoff);
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Weavile Knock Off (97.5 BP) vs. 0 HP / 0 Def Audino: 139-165 (40 - 47.5%) -- guaranteed 3HKO"
      ));
      var result2 = C(gen, weavile, audinoMega, knockoff);
      Assert.That(result2.Desc(), Is.EqualTo(
        "0 Atk Weavile Knock Off vs. 0 HP / 0 Def Audino: 93-111 (26.8 - 31.9%) -- guaranteed 4HKO"
      ));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void MultiHitPercentageKill(int gen) {
      if (gen < 3) {
        var result = C(
          gen,
          P(gen, "Persian", new State.Pokemon { Boosts = new StatsTableInput { Atk = 4 } }),
          P(gen, "Abra"),
          M(gen, "Fury Swipes", new State.Move { Hits = 2 })
        );
        Assert.That(result.Range(), Is.EqualTo((218, 258)));
        Assert.That(result.Desc(), Is.EqualTo(
          "+4 Persian Fury Swipes (2 hits) vs. Abra: 218-258 (86.1 - 101.9%) -- 2.9% chance to OHKO"
        ));
      } else if (gen == 3) {
        var result = C(
          gen,
          P(gen, "Persian", new State.Pokemon { Boosts = new StatsTableInput { Atk = 3 } }),
          P(gen, "Abra", new State.Pokemon { Boosts = new StatsTableInput { Def = 1 } }),
          M(gen, "Fury Swipes", new State.Move { Hits = 2 })
        );
        Assert.That(result.Range(), Is.EqualTo((174, 206)));
        Assert.That(result.Desc(), Is.EqualTo(
          "+3 0 Atk Persian Fury Swipes (2 hits) vs. +1 0 HP / 0 Def Abra: 174-206 (91 - 107.8%) -- 41.8% chance to OHKO"
        ));
      } else {
        var result = C(
          gen,
          P(gen, "Persian", new State.Pokemon { Boosts = new StatsTableInput { Atk = 3 } }),
          P(gen, "Abra", new State.Pokemon { Boosts = new StatsTableInput { Def = 1 } }),
          M(gen, "Fury Swipes", new State.Move { Hits = 2 })
        );
        Assert.That(result.Range(), Is.EqualTo((174, 206)));
        Assert.That(result.Desc(), Is.EqualTo(
          "+3 0 Atk Persian Fury Swipes (2 hits) vs. +1 0 HP / 0 Def Abra: 174-206 (91 - 107.8%) -- 43.8% chance to OHKO"
        ));
      }
    }

    [TestCase(8)]
    [TestCase(9)]
    public void KnockOffVsZacianCrowned(int gen) {
      var weavile = P(gen, "Weavile");
      var zacian = P(gen, "Zacian-Crowned", new State.Pokemon { Ability = "Intrepid Sword", Item = "Rusted Sword" });
      var knockoff = M(gen, "Knock Off");
      var result = C(gen, weavile, zacian, knockoff);
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Weavile Knock Off vs. 0 HP / 0 Def Zacian-Crowned: 36-43 (11 - 13.2%) -- possible 8HKO"
      ));
    }

    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void MultiHitWithMultiscale(int gen) {
      var result = C(
        gen,
        P(gen, "Mamoswine"),
        P(gen, "Dragonite", new State.Pokemon { Ability = "Multiscale" }),
        M(gen, "Icicle Spear")
      );
      Assert.That(result.Range(), Is.EqualTo((360, 430)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Mamoswine Icicle Spear (3 hits) vs. 0 HP / 0 Def Multiscale Dragonite: 360-430 (111.4 - 133.1%) -- guaranteed OHKO"
      ));
    }

    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void MultiHitWithWeakArmor(int gen) {
      var result = C(
        gen,
        P(gen, "Mamoswine"),
        P(gen, "Skarmory", new State.Pokemon { Ability = "Weak Armor" }),
        M(gen, "Icicle Spear")
      );
      Assert.That(result.Range(), Is.EqualTo((115, 138)));

      result = C(
        gen,
        P(gen, "Mamoswine"),
        P(gen, "Skarmory", new State.Pokemon { Ability = "Weak Armor", Item = "White Herb" }),
        M(gen, "Icicle Spear")
      );
      Assert.That(result.Range(), Is.EqualTo((89, 108)));

      result = C(
        gen,
        P(gen, "Mamoswine"),
        P(gen, "Skarmory", new State.Pokemon { Ability = "Weak Armor", Item = "White Herb", Boosts = new StatsTableInput { Def = 2 } }),
        M(gen, "Icicle Spear")
      );
      Assert.That(result.Range(), Is.EqualTo((56, 69)));

      result = C(
        gen,
        P(gen, "Mamoswine", new State.Pokemon { Ability = "Unaware" }),
        P(gen, "Skarmory", new State.Pokemon { Ability = "Weak Armor", Item = "White Herb", Boosts = new StatsTableInput { Def = 2 } }),
        M(gen, "Icicle Spear")
      );
      Assert.That(result.Range(), Is.EqualTo((75, 93)));
    }

    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void MultiHitWithMummy(int gen) {
      var result = C(
        gen,
        P(gen, "Pinsir-Mega"),
        P(gen, "Cofagrigus", new State.Pokemon { Ability = "Mummy" }),
        M(gen, "Double Hit")
      );
      if (gen == 6) {
        Assert.That(result.Range(), Is.EqualTo((96, 113)));
        Assert.That(result.Desc(), Is.EqualTo(
          "0 Atk Aerilate Pinsir-Mega Double Hit (2 hits) vs. 0 HP / 0 Def Mummy Cofagrigus: 96-113 (37.3 - 43.9%) -- guaranteed 3HKO"
        ));
      } else {
        Assert.That(result.Range(), Is.EqualTo((91, 107)));
        Assert.That(result.Desc(), Is.EqualTo(
          "0 Atk Aerilate Pinsir-Mega Double Hit (2 hits) vs. 0 HP / 0 Def Mummy Cofagrigus: 91-107 (35.4 - 41.6%) -- guaranteed 3HKO"
        ));
      }
    }

    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void MultiHitWithItems(int gen) {
      var result = C(
        gen,
        P(gen, "Greninja"),
        P(gen, "Gliscor", new State.Pokemon { Item = "Luminous Moss" }),
        M(gen, "Water Shuriken")
      );
      Assert.That(result.Range(), Is.EqualTo((104, 126)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 SpA Greninja Water Shuriken (15 BP) (3 hits) vs. 0 HP / 0 SpD Luminous Moss Gliscor: 104-126 (35.7 - 43.2%) -- guaranteed 3HKO"
      ));

      result = C(
        gen,
        P(gen, "Greninja"),
        P(gen, "Gliscor", new State.Pokemon { Ability = "Simple", Item = "Luminous Moss" }),
        M(gen, "Water Shuriken")
      );
      Assert.That(result.Range(), Is.EqualTo((92, 114)));

      result = C(
        gen,
        P(gen, "Greninja"),
        P(gen, "Gliscor", new State.Pokemon { Ability = "Contrary", Item = "Luminous Moss" }),
        M(gen, "Water Shuriken")
      );
      Assert.That(result.Range(), Is.EqualTo((176, 210)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 SpA Greninja Water Shuriken (15 BP) (3 hits) vs. 0 HP / 0 SpD Luminous Moss Contrary Gliscor: 176-210 (60.4 - 72.1%) -- guaranteed 2HKO"
      ));
    }

    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void KoedPokemon_NoRecovery_5PlusTurns(int gen) {
      var chansey = P(gen, "Chansey", new State.Pokemon { Level = 25 });
      var mew = P(gen, "Mew", new State.Pokemon { Level = 30, Item = "Leftovers", Ivs = new StatsTableInput { Hp = 0 } });
      var result = C(gen, chansey, mew, M(gen, "Seismic Toss"));
      Assert.That(result.Damage, Is.EqualTo(25));
      Assert.That(result.Desc(), Is.EqualTo(
        "Lvl 25 Chansey Seismic Toss vs. Lvl 30 0 HP 0 IVs Mew: 25-25 (25 - 25%) -- guaranteed 5HKO after Leftovers recovery"
      ));
    }

    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void KoedPokemon_NoRecovery_1To4Turns(int gen) {
      var chansey = P(gen, "Chansey", new State.Pokemon { Level = 55 });
      var mew = P(gen, "Mew", new State.Pokemon { Level = 30, Item = "Leftovers", Ivs = new StatsTableInput { Hp = 0 } });
      var result = C(gen, chansey, mew, M(gen, "Seismic Toss"));
      Assert.That(result.Damage, Is.EqualTo(55));
      Assert.That(result.Desc(), Is.EqualTo(
        "Lvl 55 Chansey Seismic Toss vs. Lvl 30 0 HP 0 IVs Mew: 55-55 (55 - 55%) -- guaranteed 2HKO after Leftovers recovery"
      ));
    }

    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void EndOfTurnDamage_5PlusTurns(int gen) {
      var chansey = P(gen, "Chansey", new State.Pokemon { Level = 1 });
      var mew = P(gen, "Mew", new State.Pokemon { Level = 30, Status = "tox", ToxicCounter = 1, Ivs = new StatsTableInput { Hp = 0 } });
      var result = C(gen, chansey, mew, M(gen, "Seismic Toss"));
      Assert.That(result.Damage, Is.EqualTo(1));
      Assert.That(result.Desc(), Is.EqualTo(
        "Lvl 1 Chansey Seismic Toss vs. Lvl 30 0 HP 0 IVs Mew: 1-1 (1 - 1%) -- guaranteed 6HKO after toxic damage"
      ));
    }

    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void EndOfTurnDamage_1To4Turns(int gen) {
      var field = F(new State.Field {
        Weather = "Sand",
        DefenderSide = new State.Side { IsSeeded = true },
      });
      var chansey = P(gen, "Chansey", new State.Pokemon { Level = 1 });
      var mew = P(gen, "Mew", new State.Pokemon { Level = 30, Status = "tox", ToxicCounter = 1, Ivs = new StatsTableInput { Hp = 0 } });
      var result = C(gen, chansey, mew, M(gen, "Seismic Toss"), field);
      Assert.That(result.Damage, Is.EqualTo(1));
      Assert.That(result.Desc(), Is.EqualTo(
        "Lvl 1 Chansey Seismic Toss vs. Lvl 30 0 HP 0 IVs Mew: 1-1 (1 - 1%) -- guaranteed 4HKO after sandstorm damage, Leech Seed damage, and toxic damage"
      ));
    }

    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void EndOfTurnDamage_FirstTurn(int gen) {
      var field = F(new State.Field { Weather = "Sand" });
      var chansey = P(gen, "Chansey", new State.Pokemon { Level = 90 });
      var mew = P(gen, "Mew", new State.Pokemon { Level = 30, Status = "brn", Ivs = new StatsTableInput { Hp = 0 } });
      var result = C(gen, chansey, mew, M(gen, "Seismic Toss"), field);
      Assert.That(result.Damage, Is.EqualTo(90));
      Assert.That(result.Desc(), Is.EqualTo(
        "Lvl 90 Chansey Seismic Toss vs. Lvl 30 0 HP 0 IVs Mew: 90-90 (90 - 90%) -- guaranteed OHKO after sandstorm damage and burn damage"
      ));
    }

    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void MoldBreakerDoesNotDisable_NonDamageAbilities(int gen) {
      var attacker = P(gen, "Rampardos", new State.Pokemon { Ability = "Mold Breaker" });
      var defender = P(gen, "Blastoise", new State.Pokemon { Ability = "Rain Dish" });
      var field = F(new State.Field { Weather = "Rain" });
      var move = M(gen, "Stone Edge");

      var result = C(gen, attacker, defender, move, field);
      Assert.That(result.Defender.Ability, Is.EqualTo("Rain Dish"));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Rampardos Stone Edge vs. 0 HP / 0 Def Blastoise: 168-198 (56.1 - 66.2%) -- guaranteed 2HKO after Rain Dish recovery"
      ));
    }

    [TestCase(8)]
    [TestCase(9)]
    public void SteelySpirit_FieldEffect(int gen) {
      var pokemon = P(gen, "Perrserker", new State.Pokemon { Ability = "Battle Armor" });
      var move = M(gen, "Iron Head");

      var result = C(gen, pokemon, pokemon, move);
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Perrserker Iron Head vs. 0 HP / 0 Def Perrserker: 46-55 (16.3 - 19.5%) -- possible 6HKO"
      ));

      var field = F(new State.Field { AttackerSide = new State.Side { IsSteelySpirit = true } });
      result = C(gen, pokemon, pokemon, move, field);
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Perrserker with an ally's Steely Spirit Iron Head vs. 0 HP / 0 Def Perrserker: 70-83 (24.9 - 29.5%) -- 99.9% chance to 4HKO"
      ));

      pokemon.Ability = "Steely Spirit";
      result = C(gen, pokemon, pokemon, move, field);
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Steely Spirit Perrserker with an ally's Steely Spirit Iron Head vs. 0 HP / 0 Def Perrserker: 105-124 (37.3 - 44.1%) -- guaranteed 3HKO"
      ));
    }

    [TestCase(8)]
    [TestCase(9)]
    public void ShellSideArm_SpecialNotFurCoatOrFluffy(int gen) {
      var attacker = P(gen, "Slowbro-Galar");
      var defender = P(gen, "Mew", new State.Pokemon { Ability = "Fluffy", Evs = new StatsTableInput { Def = 4 } });
      var result = C(gen, attacker, defender, M(gen, "Shell Side Arm"));
      Assert.That(result.Move.Category, Is.EqualTo(MoveCategories.Special));
      Assert.That(result.RawDesc.DefenderAbility, Is.Null);
      defender.Ability = "Fur Coat";
      result = C(gen, attacker, defender, M(gen, "Shell Side Arm"));
      Assert.That(result.Move.Category, Is.EqualTo(MoveCategories.Special));
      Assert.That(result.RawDesc.DefenderAbility, Is.Null);
    }

    [TestCase(8)]
    [TestCase(9)]
    public void ShellSideArm_PhysicalNotIceScales(int gen) {
      var attacker = P(gen, "Slowbro-Galar");
      var defender = P(gen, "Mew", new State.Pokemon { Ability = "Ice Scales", Evs = new StatsTableInput { Spd = 4 } });
      var result = C(gen, attacker, defender, M(gen, "Shell Side Arm"));
      Assert.That(result.Move.Category, Is.EqualTo(MoveCategories.Physical));
      Assert.That(result.RawDesc.DefenderAbility, Is.Null);
    }

    [TestCase(8)]
    [TestCase(9)]
    public void ShellSideArm_PhysicalMakesContact(int gen) {
      var attacker = P(gen, "Slowbro-Galar");
      var defender = P(gen, "Mew", new State.Pokemon { Ability = "Fluffy", Evs = new StatsTableInput { Spd = 4 } });
      var result = C(gen, attacker, defender, M(gen, "Shell Side Arm"));
      Assert.That(result.Move.Flags.Contact, Is.True);
    }

    [Test]
    public void Gen1_BasicGengarVsChansey() {
      var result = C(1, P(1, "Gengar"), P(1, "Chansey"), M(1, "Thunderbolt"));
      Assert.That(result.Range(), Is.EqualTo((79, 94)));
      Assert.That(result.Desc(), Is.EqualTo(
        "Gengar Thunderbolt vs. Chansey: 79-94 (11.2 - 13.3%) -- possible 8HKO"
      ));

      var field = F(new State.Field { DefenderSide = new State.Side { IsLightScreen = true } });
      result = C(1, P(1, "Gengar"), P(1, "Vulpix"), M(1, "Surf"), field);
      Assert.That(result.Desc(), Is.EqualTo(
        "Gengar Surf vs. Vulpix through Light Screen: 108-128 (38.7 - 45.8%) -- guaranteed 3HKO"
      ));
    }

    [Test]
    public void Gen2_BasicGengarVsChansey() {
      var result = C(
        2,
        P(2, "Gengar"),
        P(2, "Chansey", new State.Pokemon { Item = "Leftovers" }),
        M(2, "Dynamic Punch")
      );
      Assert.That(result.Range(), Is.EqualTo((304, 358)));
      Assert.That(result.Desc(), Is.EqualTo(
        "Gengar Dynamic Punch vs. Chansey: 304-358 (43.2 - 50.9%) -- guaranteed 3HKO after Leftovers recovery"
      ));
    }

    [Test]
    public void Gen2_Struggle() {
      var attacker = P(2, "Skarmory", new State.Pokemon { Boosts = new StatsTableInput { Atk = 6, Def = 6 } });
      var defender = P(2, "Skarmory", new State.Pokemon { Boosts = new StatsTableInput { Atk = 6, Def = 6 } });
      var result = C(2, attacker, defender, M(2, "Struggle"));
      Assert.That(result.Range(), Is.EqualTo((37, 44)));
      Assert.That(result.Desc(), Is.EqualTo(
        "+6 Skarmory Struggle vs. +6 Skarmory: 37-44 (11.1 - 13.2%) -- possible 8HKO"
      ));
    }

    [Test]
    public void Gen2_Present() {
      var attacker = P(2, "Togepi", new State.Pokemon { Level = 5, Boosts = new StatsTableInput { Atk = -6 }, Status = "brn" });
      var defender = P(2, "Umbreon", new State.Pokemon { Boosts = new StatsTableInput { Def = 6 } });
      var field = F(new State.Field { DefenderSide = new State.Side { IsReflect = true } });
      var result = C(2, attacker, defender, M(2, "Present"), field);
      Assert.That(result.Range(), Is.EqualTo((125, 147)));
      Assert.That(result.Desc(), Is.EqualTo(
        "-6 Lvl 5 burned Togepi Present vs. +6 Umbreon through Reflect: 125-147 (31.8 - 37.4%) -- 89.1% chance to 3HKO"
      ));
    }

    [Test]
    public void Gen2_DVs() {
      var aerodactyl = P(2, "Aerodactyl");
      var zapdos = P(2, "Zapdos", new State.Pokemon { Ivs = new StatsTableInput { Atk = 29, Def = 27 }, Item = "Leftovers" });
      Assert.That(zapdos.Ivs.Hp, Is.EqualTo(14));
      var result = C(2, aerodactyl, zapdos, M(2, "Ancient Power"));
      Assert.That(result.Range(), Is.EqualTo((153, 180)));
      Assert.That(result.Desc(), Is.EqualTo(
        "Aerodactyl Ancient Power vs. Zapdos: 153-180 (41.6 - 49%) -- guaranteed 3HKO after Leftovers recovery"
      ));
    }

    [Test]
    public void Gen3_BasicGengarVsChansey() {
      var result = C(
        3,
        P(3, "Gengar", new State.Pokemon { Nature = "Mild", Evs = new StatsTableInput { Atk = 100 } }),
        P(3, "Chansey", new State.Pokemon { Item = "Leftovers", Nature = "Bold", Evs = new StatsTableInput { Hp = 252, Def = 252 } }),
        M(3, "Focus Punch")
      );
      Assert.That(result.Range(), Is.EqualTo((346, 408)));
      Assert.That(result.Desc(), Is.EqualTo(
        "100 Atk Gengar Focus Punch vs. 252 HP / 252+ Def Chansey: 346-408 (49.1 - 57.9%) -- 59% chance to 2HKO after Leftovers recovery"
      ));
    }

    [Test]
    public void Gen3_WaterAbsorb() {
      var cacturne = P(3, "Cacturne", new State.Pokemon { Ability = "Sand Veil" });
      var blastoise = P(3, "Blastoise", new State.Pokemon { Evs = new StatsTableInput { Spa = 252 } });
      var surf = M(3, "Surf");
      var result = C(3, blastoise, cacturne, surf);
      Assert.That(result.Range(), Is.EqualTo((88, 104)));
      Assert.That(result.Desc(), Is.EqualTo(
        "252 SpA Blastoise Surf vs. 0 HP / 0 SpD Cacturne: 88-104 (31.3 - 37%) -- 76.6% chance to 3HKO"
      ));
      cacturne.Ability = "Water Absorb";
      result = C(3, blastoise, cacturne, surf);
      Assert.That(result.Damage, Is.EqualTo(0));
    }

    [Test]
    public void Gen3_SpreadAllAdjacent() {
      var gengar = P(3, "Gengar", new State.Pokemon { Nature = "Mild", Evs = new StatsTableInput { Atk = 100 } });
      var blissey = P(3, "Chansey", new State.Pokemon { Item = "Leftovers", Nature = "Bold", Evs = new StatsTableInput { Hp = 252, Def = 252 } });
      var field = F(new State.Field { GameType = GameTypes.Doubles });
      var result = C(3, gengar, blissey, M(3, "Explosion"), field);
      Assert.That(result.Range(), Is.EqualTo((578, 681)));
      Assert.That(result.Desc(), Is.EqualTo(
        "100 Atk Gengar Explosion vs. 252 HP / 252+ Def Chansey: 578-681 (82.1 - 96.7%) -- guaranteed 2HKO after Leftovers recovery"
      ));
    }

    [Test]
    public void Gen3_SpreadAllAdjacentFoes() {
      var gengar = P(3, "Gengar", new State.Pokemon { Nature = "Modest", Evs = new StatsTableInput { Spa = 252 } });
      var blissey = P(3, "Chansey", new State.Pokemon { Item = "Leftovers", Nature = "Bold", Evs = new StatsTableInput { Hp = 252, Def = 252 } });
      var field = F(new State.Field { GameType = GameTypes.Doubles });
      var result = C(3, gengar, blissey, M(3, "Blizzard"), field);
      Assert.That(result.Range(), Is.EqualTo((69, 82)));
      Assert.That(result.Desc(), Is.EqualTo(
        "252+ SpA Gengar Blizzard vs. 252 HP / 0 SpD Chansey: 69-82 (9.8 - 11.6%)"
      ));
    }

    [Test]
    public void Gen4_BasicGengarVsChansey() {
      var result = C(
        4,
        P(4, "Gengar", new State.Pokemon { Item = "Choice Specs", Nature = "Timid", Evs = new StatsTableInput { Spa = 252 }, Boosts = new StatsTableInput { Spa = 1 } }),
        P(4, "Chansey", new State.Pokemon { Item = "Leftovers", Nature = "Calm", Evs = new StatsTableInput { Hp = 252, Spd = 252 } }),
        M(4, "Focus Blast")
      );
      Assert.That(result.Range(), Is.EqualTo((408, 482)));
      Assert.That(result.Desc(), Is.EqualTo(
        "+1 252 SpA Choice Specs Gengar Focus Blast vs. 252 HP / 252+ SpD Chansey: 408-482 (57.9 - 68.4%) -- guaranteed 2HKO after Leftovers recovery"
      ));
    }

    [Test]
    public void Gen4_MoldBreaker() {
      var pinsir = P(4, "Pinsir", new State.Pokemon { Item = "Choice Band", Nature = "Adamant", Ability = "Hyper Cutter", Evs = new StatsTableInput { Atk = 252 } });
      var gengar = P(4, "Gengar", new State.Pokemon { Item = "Choice Specs", Nature = "Timid", Evs = new StatsTableInput { Spa = 252 }, Boosts = new StatsTableInput { Spa = 1 } });
      var earthquake = M(4, "Earthquake");

      var result = C(4, pinsir, gengar, earthquake);
      Assert.That(result.Damage, Is.EqualTo(0));

      pinsir.Ability = "Mold Breaker";
      result = C(4, pinsir, gengar, earthquake);
      Assert.That(result.Range(), Is.EqualTo((528, 622)));
      Assert.That(result.Desc(), Is.EqualTo(
        "252+ Atk Choice Band Mold Breaker Pinsir Earthquake vs. 0 HP / 0 Def Gengar: 528-622 (202.2 - 238.3%) -- guaranteed OHKO"
      ));

      pinsir.Boosts.Atk = 2;
      gengar.Ability = "Unaware";
      result = C(4, pinsir, gengar, earthquake);
      Assert.That(result.Range(), Is.EqualTo((1054, 1240)));
    }

    [Test]
    public void Gen4_Technician() {
      var scizor = P(4, "Scizor", new State.Pokemon { Item = "Insect Plate", Ability = "Technician" });
      var chansey = P(4, "Chansey");
      var bugbite = M(4, "Bug Bite");
      var result = C(4, scizor, chansey, bugbite);
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Insect Plate Technician Scizor Bug Bite vs. 0 HP / 0 Def Chansey: 745-877 (116.2 - 136.8%) -- guaranteed OHKO"
      ));
    }

    [Test]
    public void Gen5_BasicGengarVsChansey() {
      var result = C(
        5,
        P(5, "Gengar", new State.Pokemon { Item = "Choice Specs", Nature = "Timid", Evs = new StatsTableInput { Spa = 252 }, Boosts = new StatsTableInput { Spa = 1 } }),
        P(5, "Chansey", new State.Pokemon { Item = "Eviolite", Nature = "Calm", Evs = new StatsTableInput { Hp = 252, Spd = 252 } }),
        M(5, "Focus Blast")
      );
      Assert.That(result.Range(), Is.EqualTo((274, 324)));
      Assert.That(result.FullDesc("px"), Is.EqualTo(
        "+1 252 SpA Choice Specs Gengar Focus Blast vs. 252 HP / 252+ SpD Eviolite Chansey: 274-324 (18 - 22px) -- guaranteed 3HKO"
      ));
    }

    [Test]
    public void Gen5_TechnicianLowKick() {
      var ambipom = P(5, "Ambipom", new State.Pokemon { Level = 50, Ability = "Technician" });
      var blissey = P(5, "Blissey", new State.Pokemon { Level = 50, Evs = new StatsTableInput { Hp = 252 } });
      var result = C(5, ambipom, blissey, M(5, "Low Kick"));
      Assert.That(result.Range(), Is.EqualTo((272, 320)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Technician Ambipom Low Kick (60 BP) vs. 252 HP / 0 Def Blissey: 272-320 (75.1 - 88.3%) -- guaranteed 2HKO"
      ));

      var aggron = P(5, "Aggron", new State.Pokemon { Level = 50, Evs = new StatsTableInput { Hp = 252 } });
      result = C(5, ambipom, aggron, M(5, "Low Kick"));
      Assert.That(result.Range(), Is.EqualTo((112, 132)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Ambipom Low Kick (120 BP) vs. 252 HP / 0 Def Aggron: 112-132 (63.2 - 74.5%) -- guaranteed 2HKO"
      ));
    }

    [Test]
    public void Gen6_BasicGengarVsChansey() {
      var result = C(
        6,
        P(6, "Gengar", new State.Pokemon { Item = "Life Orb", Nature = "Modest", Evs = new StatsTableInput { Spa = 252 } }),
        P(6, "Chansey", new State.Pokemon { Item = "Eviolite", Nature = "Bold", Evs = new StatsTableInput { Hp = 252, Def = 252 } }),
        M(6, "Sludge Bomb")
      );
      Assert.That(result.Range(), Is.EqualTo((134, 160)));
      Assert.That(result.Desc(), Is.EqualTo(
        "252+ SpA Life Orb Gengar Sludge Bomb vs. 252 HP / 0 SpD Eviolite Chansey: 134-160 (19 - 22.7%) -- possible 5HKO"
      ));
    }

    [Test]
    public void Gen7_BasicGengarVsChansey() {
      var result = C(
        7,
        P(7, "Gengar", new State.Pokemon { Item = "Life Orb", Nature = "Modest", Evs = new StatsTableInput { Spa = 252 }, Boosts = new StatsTableInput { Spa = 3 } }),
        P(7, "Chansey", new State.Pokemon { Item = "Eviolite", Nature = "Bold", Evs = new StatsTableInput { Hp = 100, Spd = 100 }, Boosts = new StatsTableInput { Spd = 1 } }),
        M(7, "Sludge Bomb")
      );
      Assert.That(result.Range(), Is.EqualTo((204, 242)));
      Assert.That(result.Desc(), Is.EqualTo(
        "+3 252+ SpA Life Orb Gengar Sludge Bomb vs. +1 100 HP / 100 SpD Eviolite Chansey: 204-242 (30.6 - 36.3%) -- 52.9% chance to 3HKO"
      ));
    }

    [Test]
    public void Gen7_ZMoveCriticalHits() {
      var abomasnow = P(7, "Abomasnow", new State.Pokemon {
        Item = "Icy Rock",
        Ability = "Snow Warning",
        Nature = "Hasty",
        Evs = new StatsTableInput { Atk = 252, Spd = 4, Spe = 252 },
      });
      var hoopa = P(7, "Hoopa-Unbound", new State.Pokemon {
        Item = "Choice Band",
        Ability = "Magician",
        Nature = "Jolly",
        Evs = new StatsTableInput { Hp = 32, Atk = 224, Spe = 252 },
      });
      var zMove = M(7, "Wood Hammer", new State.Move { UseZ = true, IsCrit = true });
      var result = C(7, abomasnow, hoopa, zMove);
      Assert.That(result.Range(), Is.EqualTo((555, 654)));
      Assert.That(result.Desc(), Is.EqualTo(
        "252 Atk Abomasnow Bloom Doom (190 BP) vs. 32 HP / 0 Def Hoopa-Unbound on a critical hit: 555-654 (179.6 - 211.6%) -- guaranteed OHKO"
      ));
    }

    [Test]
    public void Gen7_RecoilAndRecovery() {
      var abomasnow = P(7, "Abomasnow", new State.Pokemon {
        Item = "Icy Rock",
        Ability = "Snow Warning",
        Nature = "Hasty",
        Evs = new StatsTableInput { Atk = 252, Spd = 4, Spe = 252 },
      });
      var hoopa = P(7, "Hoopa-Unbound", new State.Pokemon {
        Item = "Choice Band",
        Ability = "Magician",
        Nature = "Jolly",
        Evs = new StatsTableInput { Hp = 32, Atk = 224, Spe = 252 },
      });
      var result = C(7, abomasnow, hoopa, M(7, "Wood Hammer"));
      Assert.That(result.Range(), Is.EqualTo((234, 276)));
      Assert.That(result.Desc(), Is.EqualTo(
        "252 Atk Abomasnow Wood Hammer vs. 32 HP / 0 Def Hoopa-Unbound: 234-276 (75.7 - 89.3%) -- guaranteed 2HKO"
      ));
      var recoil = result.Recoil();
      Assert.That(recoil.recoil, Is.EqualTo(new[] { 24.0, 28.3 }));
      Assert.That(recoil.text, Is.EqualTo("24 - 28.3% recoil damage"));

      result = C(7, hoopa, abomasnow, M(7, "Drain Punch"));
      Assert.That(result.Range(), Is.EqualTo((398, 470)));
      Assert.That(result.Desc(), Is.EqualTo(
        "224 Atk Choice Band Hoopa-Unbound Drain Punch vs. 0 HP / 0- Def Abomasnow: 398-470 (123.9 - 146.4%) -- guaranteed OHKO"
      ));
      var recovery = result.Recovery();
      Assert.That(recovery.recovery, Is.EqualTo(new[] { 161.0, 161.0 }));
      Assert.That(recovery.text, Is.EqualTo("52.1 - 52.1% recovered"));
    }

    [Test]
    public void Gen7_BigRoot() {
      var abomasnow = P(7, "Abomasnow", new State.Pokemon {
        Item = "Icy Rock",
        Ability = "Snow Warning",
        Nature = "Hasty",
        Evs = new StatsTableInput { Atk = 252, Spd = 4, Spe = 252 },
      });
      var bigRoot = P(7, "Blissey", new State.Pokemon { Item = "Big Root" });
      var result = C(7, bigRoot, abomasnow, M(7, "Drain Punch"));
      Assert.That(result.Range(), Is.EqualTo((38, 46)));
      Assert.That(result.Recovery().recovery, Is.EqualTo(new[] { 24.0, 29.0 }));
    }

    [Test]
    public void Gen7_BigRootAppliestoOHKO() {
      var bigRoot = P(7, "Blissey", new State.Pokemon { Item = "Big Root" });
      var weak = P(7, "Abomasnow", new State.Pokemon {
        Item = "Icy Rock",
        Ability = "Snow Warning",
        Nature = "Hasty",
        Evs = new StatsTableInput { Atk = 252, Spd = 4, Spe = 252 },
        Level = 29,
      });
      var result = C(7, bigRoot, weak, M(7, "Drain Punch"));
      Assert.That(result.Range(), Is.EqualTo((120, 142)));
      Assert.That(result.Recovery().recovery, Is.EqualTo(new[] { 64.0, 64.0 }));
    }

    [Test]
    public void Gen7_LoadedField() {
      var abomasnow = P(7, "Abomasnow", new State.Pokemon {
        Item = "Icy Rock",
        Ability = "Snow Warning",
        Nature = "Hasty",
        Evs = new StatsTableInput { Atk = 252, Spd = 4, Spe = 252 },
      });
      var hoopa = P(7, "Hoopa-Unbound", new State.Pokemon {
        Item = "Choice Band",
        Ability = "Magician",
        Nature = "Jolly",
        Evs = new StatsTableInput { Hp = 32, Atk = 224, Spe = 252 },
      });
      var field = F(new State.Field {
        GameType = GameTypes.Doubles,
        Terrain = "Grassy",
        Weather = "Hail",
        DefenderSide = new State.Side {
          IsSR = true,
          Spikes = 1,
          IsLightScreen = true,
          IsSeeded = true,
          IsFriendGuard = true,
        },
        AttackerSide = new State.Side {
          IsHelpingHand = true,
          IsTailwind = true,
        },
      });
      var result = C(7, abomasnow, hoopa, M(7, "Blizzard"), field);
      Assert.That(result.Range(), Is.EqualTo((50, 59)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 SpA Abomasnow Helping Hand Blizzard vs. 32 HP / 0 SpD Hoopa-Unbound through Light Screen with an ally's Friend Guard: 50-59 (16.1 - 19%) -- guaranteed 3HKO after Stealth Rock, 1 layer of Spikes, hail damage, Leech Seed damage, and Grassy Terrain recovery"
      ));
    }

    [Test]
    public void Gen7_WringOut() {
      var smeargle = P(7, "Smeargle", new State.Pokemon { Level = 50, Ability = "Technician" });
      var blissey = P(7, "Blissey", new State.Pokemon { Level = 50, Evs = new StatsTableInput { Hp = 252 }, CurHP = 184 });
      var result = C(7, smeargle, blissey, M(7, "Wring Out"));
      Assert.That(result.Range(), Is.EqualTo((15, 18)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 SpA Technician Smeargle Wring Out (60 BP) vs. 252 HP / 0 SpD Blissey: 15-18 (4.1 - 4.9%)"
      ));
    }

    [Test]
    public void Gen7_MoldBreaker() {
      var pinsir = P(7, "Pinsir", new State.Pokemon { Item = "Choice Band", Nature = "Adamant", Ability = "Hyper Cutter", Evs = new StatsTableInput { Atk = 252 } });
      var gengar = P(7, "Gengar", new State.Pokemon { Item = "Choice Specs", Nature = "Timid", Ability = "Levitate", Evs = new StatsTableInput { Spa = 252 }, Boosts = new StatsTableInput { Spa = 1 } });
      var earthquake = M(7, "Earthquake");
      var result = C(7, pinsir, gengar, earthquake);
      Assert.That(result.Damage, Is.EqualTo(0));
      pinsir.Ability = "Mold Breaker";
      result = C(7, pinsir, gengar, earthquake);
      Assert.That(result.Range(), Is.EqualTo((528, 622)));
      Assert.That(result.Desc(), Is.EqualTo(
        "252+ Atk Choice Band Mold Breaker Pinsir Earthquake vs. 0 HP / 0 Def Gengar: 528-622 (202.2 - 238.3%) -- guaranteed OHKO"
      ));
      pinsir.Boosts.Atk = 2;
      gengar.Ability = "Unaware";
      result = C(7, pinsir, gengar, earthquake);
      Assert.That(result.Range(), Is.EqualTo((1054, 1240)));
    }

    [Test]
    public void Gen7_16BitOverflow() {
      var result = C(
        7,
        P(7, "Mewtwo-Mega-Y", new State.Pokemon { Evs = new StatsTableInput { Spa = 196 } }),
        P(7, "Wynaut", new State.Pokemon { Level = 1, Boosts = new StatsTableInput { Spd = -6 } }),
        M(7, "Fire Blast"),
        F(new State.Field { AttackerSide = new State.Side { IsHelpingHand = true } })
      );
      Assert.That(result.Damage, Is.EqualTo(new[] {
        55725, 56380, 57036, 57691,
        58347, 59003, 59658, 60314,
        60969, 61625, 62281, 62936,
        63592, 64247, 64903, 23,
      }));
    }

    [Test]
    public void Gen7_32BitOverflow() {
      var kyogre = P(7, "Kyogre", new State.Pokemon {
        Ability = "Water Bubble",
        Item = "Choice Specs",
        CurHP = 340,
        Ivs = new StatsTableInput { Spa = 6 },
        Boosts = new StatsTableInput { Spa = 6 },
      });
      var wynaut = P(7, "Wynaut", new State.Pokemon { Level = 1, Boosts = new StatsTableInput { Spd = -6 } });
      var waterSpout = M(7, "Water Spout");
      var field = F(new State.Field { Weather = "Rain", AttackerSide = new State.Side { IsHelpingHand = true } });
      Assert.That(C(7, kyogre, wynaut, waterSpout, field).Range(), Is.EqualTo((55, 66)));

      kyogre = P(7, "Kyogre", new State.Pokemon {
        Ability = "Water Bubble",
        Item = "Choice Specs",
        CurHP = 340,
        Ivs = new StatsTableInput { Spa = 6 },
        Boosts = new StatsTableInput { Spa = 6 },
        Overrides = new Specie { Types = new[] { "Normal" } },
      });
      Assert.That(C(7, kyogre, wynaut, waterSpout, field).Range(), Is.EqualTo((37, 44)));
    }

    [Test]
    public void Gen8_BasicGengarVsChansey() {
      var result = C(
        8,
        P(8, "Gengar", new State.Pokemon { Item = "Life Orb", Nature = "Modest", Evs = new StatsTableInput { Spa = 252 }, Boosts = new StatsTableInput { Spa = 3 } }),
        P(8, "Chansey", new State.Pokemon { Item = "Eviolite", Nature = "Bold", Evs = new StatsTableInput { Hp = 100, Spd = 100 }, Boosts = new StatsTableInput { Spd = 1 } }),
        M(8, "Sludge Bomb")
      );
      Assert.That(result.Range(), Is.EqualTo((204, 242)));
      Assert.That(result.Desc(), Is.EqualTo(
        "+3 252+ SpA Life Orb Gengar Sludge Bomb vs. +1 100 HP / 100 SpD Eviolite Chansey: 204-242 (30.6 - 36.3%) -- 52.9% chance to 3HKO"
      ));
    }

    [Test]
    public void Gen8_KnockOffVsSilvally() {
      var sawk = P(8, "Sawk", new State.Pokemon { Ability = "Mold Breaker", Evs = new StatsTableInput { Atk = 252 } });
      var silvally = P(8, "Silvally-Dark", new State.Pokemon { Item = "Dark Memory" });
      var knockoff = M(8, "Knock Off");
      var result = C(8, sawk, silvally, knockoff);
      Assert.That(result.Desc(), Is.EqualTo(
        "252 Atk Sawk Knock Off vs. 0 HP / 0 Def Silvally-Dark: 36-43 (10.8 - 12.9%) -- possible 8HKO"
      ));
    }

    [Test]
    public void Gen8_AteAbilities() {
      var sylveon = P(8, "Sylveon", new State.Pokemon { Ability = "Pixilate", Evs = new StatsTableInput { Spa = 252 } });
      var silvally = P(8, "Silvally");
      var hypervoice = M(8, "Hyper Voice");
      var result = C(8, sylveon, silvally, hypervoice);
      Assert.That(result.Desc(), Is.EqualTo(
        "252 SpA Pixilate Sylveon Hyper Voice vs. 0 HP / 0 SpD Silvally: 165-195 (49.8 - 58.9%) -- 99.6% chance to 2HKO"
      ));
    }

    [Test]
    public void Gen8_ChanceToOHKO() {
      var abomasnow = P(8, "Abomasnow", new State.Pokemon { Level = 55, Item = "Choice Specs", Evs = new StatsTableInput { Spa = 252 } });
      var deerling = P(8, "Deerling", new State.Pokemon { Evs = new StatsTableInput { Hp = 36 } });
      var blizzard = M(8, "Blizzard");
      var hail = F(new State.Field { Weather = "Hail" });
      var result = C(8, abomasnow, deerling, blizzard, hail);
      Assert.That(result.Desc(), Is.EqualTo(
        "Lvl 55 252 SpA Choice Specs Abomasnow Blizzard vs. 36 HP / 0 SpD Deerling: 236-278 (87.4 - 102.9%) -- 25% chance to OHKO (56.3% chance to OHKO after hail damage)"
      ));
    }

    [Test]
    public void Gen8_ChanceToOHKOWithLeftovers() {
      var kyurem = P(8, "Kyurem", new State.Pokemon { Level = 100, Item = "Choice Specs", Evs = new StatsTableInput { Spa = 252 } });
      var jirachi = P(8, "Jirachi", new State.Pokemon { Item = "Leftovers" });
      var earthpower = M(8, "Earth Power");
      var result = C(8, kyurem, jirachi, earthpower);
      Assert.That(result.Desc(), Is.EqualTo(
        "252 SpA Choice Specs Kyurem Earth Power vs. 0 HP / 0 SpD Jirachi: 294-348 (86.2 - 102%) -- 12.5% chance to OHKO"
      ));
    }

    [Test]
    public void Gen8_TechnicianLowKick() {
      var ambipom = P(8, "Ambipom", new State.Pokemon { Level = 50, Ability = "Technician" });
      var blissey = P(8, "Blissey", new State.Pokemon { Level = 50, Evs = new StatsTableInput { Hp = 252 } });
      var result = C(8, ambipom, blissey, M(8, "Low Kick"));
      Assert.That(result.Range(), Is.EqualTo((272, 320)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Technician Ambipom Low Kick (60 BP) vs. 252 HP / 0 Def Blissey: 272-320 (75.1 - 88.3%) -- guaranteed 2HKO"
      ));

      var aggron = P(8, "Aggron", new State.Pokemon { Level = 50, Evs = new StatsTableInput { Hp = 252 } });
      result = C(8, ambipom, aggron, M(8, "Low Kick"));
      Assert.That(result.Range(), Is.EqualTo((112, 132)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Ambipom Low Kick (120 BP) vs. 252 HP / 0 Def Aggron: 112-132 (63.2 - 74.5%) -- guaranteed 2HKO"
      ));
    }

    [Test]
    public void Gen9_SupremeOverlord() {
      var kingambit = P(9, "Kingambit", new State.Pokemon { Level = 100, Ability = "Supreme Overlord", AlliesFainted = 0 });
      var aggron = P(9, "Aggron", new State.Pokemon { Level = 100 });
      var result = C(9, kingambit, aggron, M(9, "Iron Head"));
      Assert.That(result.Range(), Is.EqualTo((67, 79)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Kingambit Iron Head vs. 0 HP / 0 Def Aggron: 67-79 (23.8 - 28.1%) -- 91.2% chance to 4HKO"
      ));
      kingambit.AlliesFainted = 5;
      result = C(9, kingambit, aggron, M(9, "Iron Head"));
      Assert.That(result.Range(), Is.EqualTo((100, 118)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Supreme Overlord 5 allies fainted Kingambit Iron Head vs. 0 HP / 0 Def Aggron: 100-118 (35.5 - 41.9%) -- guaranteed 3HKO"
      ));
      kingambit.AlliesFainted = 10;
      result = C(9, kingambit, aggron, M(9, "Iron Head"));
      Assert.That(result.Range(), Is.EqualTo((100, 118)));
      Assert.That(result.Desc(), Is.EqualTo(
        "0 Atk Supreme Overlord 5 allies fainted Kingambit Iron Head vs. 0 HP / 0 Def Aggron: 100-118 (35.5 - 41.9%) -- guaranteed 3HKO"
      ));
    }

    [Test]
    public void Gen9_ElectroDriftCollisionCourse() {
      var attacker = P(9, "Arceus");
      var defender = P(9, "Mew");
      (int min, int max) CalcRange(Move move) => C(9, attacker, defender, move).Range();
      var neutral = CalcRange(M(9, "Electro Drift"));
      var fusionBolt = M(9, "Fusion Bolt");
      Assert.That(CalcRange(fusionBolt), Is.EqualTo(neutral));
      defender = P(9, "Manaphy");
      var se = C(9, attacker, defender, M(9, "Electro Drift")).Range();
      Assert.That(CalcRange(fusionBolt), Is.Not.EqualTo(se));
      defender.TeraType = "Normal";
      Assert.That(C(9, attacker, defender, M(9, "Electro Drift")).Range(), Is.EqualTo(neutral));
      var cc = M(9, "Collision Course");
      defender = P(9, "Jirachi");
      Assert.That(C(9, attacker, defender, cc).Range(), Is.EqualTo(neutral));
      defender.TeraType = "Normal";
      Assert.That(C(9, attacker, defender, cc).Range(), Is.EqualTo(se));
    }

    [Test]
    public void Gen9_QuarkDrive() {
      void TestQP(string ability, State.Field? field = null) {
        var qpAttacker = P(9, "Iron Leaves", new State.Pokemon { Ability = ability, BoostedStat = "auto", Boosts = new StatsTableInput { Spa = 6 } });
        var qpDefender = P(9, "Iron Treads", new State.Pokemon { Ability = ability, BoostedStat = "auto", Boosts = new StatsTableInput { Spd = 6 } });
        var r = C(9, qpAttacker, qpDefender, M(9, "Leaf Storm"), F(field)).RawDesc;
        Assert.That(r.AttackerAbility, Is.EqualTo(ability));
        Assert.That(r.DefenderAbility, Is.EqualTo(ability));
        r = C(9, qpAttacker, qpDefender, M(9, "Psyblade"), F(field)).RawDesc;
        Assert.That(r.AttackerAbility, Is.Null);
        Assert.That(r.DefenderAbility, Is.Null);
      }
      TestQP("Quark Drive", new State.Field { Terrain = "Electric" });
    }

    [Test]
    public void Gen9_Protosynthesis() {
      void TestQP(string ability, State.Field? field = null) {
        var qpAttacker = P(9, "Iron Leaves", new State.Pokemon { Ability = ability, BoostedStat = "auto", Boosts = new StatsTableInput { Spa = 6 } });
        var qpDefender = P(9, "Iron Treads", new State.Pokemon { Ability = ability, BoostedStat = "auto", Boosts = new StatsTableInput { Spd = 6 } });
        var r = C(9, qpAttacker, qpDefender, M(9, "Leaf Storm"), F(field)).RawDesc;
        Assert.That(r.AttackerAbility, Is.EqualTo(ability));
        Assert.That(r.DefenderAbility, Is.EqualTo(ability));
        r = C(9, qpAttacker, qpDefender, M(9, "Psyblade"), F(field)).RawDesc;
        Assert.That(r.AttackerAbility, Is.Null);
        Assert.That(r.DefenderAbility, Is.Null);
      }
      TestQP("Protosynthesis", new State.Field { Weather = "Sun" });
    }

    [Test]
    public void Gen9_QuarkDriveOverride() {
      void TestQPOverride(string ability, State.Field? field = null) {
        var qpAttacker = P(9, "Flutter Mane", new State.Pokemon { Ability = ability, BoostedStat = "atk", Boosts = new StatsTableInput { Spa = 6 } });
        var qpDefender = P(9, "Walking Wake", new State.Pokemon { Ability = ability, BoostedStat = "def", Boosts = new StatsTableInput { Spd = 6 } });
        var r = C(9, qpAttacker, qpDefender, M(9, "Leaf Storm"), F(field)).RawDesc;
        Assert.That(r.AttackerAbility, Is.Null);
        Assert.That(r.DefenderAbility, Is.Null);
        r = C(9, qpAttacker, qpDefender, M(9, "Psyblade"), F(field)).RawDesc;
        Assert.That(r.AttackerAbility, Is.EqualTo(ability));
        Assert.That(r.DefenderAbility, Is.EqualTo(ability));
      }
      TestQPOverride("Quark Drive", new State.Field { Terrain = "Electric" });
    }

    [Test]
    public void Gen9_ProtosynthesisOverride() {
      void TestQPOverride(string ability, State.Field? field = null) {
        var qpAttacker = P(9, "Flutter Mane", new State.Pokemon { Ability = ability, BoostedStat = "atk", Boosts = new StatsTableInput { Spa = 6 } });
        var qpDefender = P(9, "Walking Wake", new State.Pokemon { Ability = ability, BoostedStat = "def", Boosts = new StatsTableInput { Spd = 6 } });
        var r = C(9, qpAttacker, qpDefender, M(9, "Leaf Storm"), F(field)).RawDesc;
        Assert.That(r.AttackerAbility, Is.Null);
        Assert.That(r.DefenderAbility, Is.Null);
        r = C(9, qpAttacker, qpDefender, M(9, "Psyblade"), F(field)).RawDesc;
        Assert.That(r.AttackerAbility, Is.EqualTo(ability));
        Assert.That(r.DefenderAbility, Is.EqualTo(ability));
      }
      TestQPOverride("Protosynthesis", new State.Field { Weather = "Sun" });
    }

    [Test]
    public void Gen9_MeteorBeamElectroShot() {
      void TestCase(State.Pokemon options, int expected) {
        var r = C(9, P(9, "Archaludon", options), P(9, "Arceus"), M(9, "Meteor Beam"));
        Assert.That(r.Attacker.Boosts.Spa, Is.EqualTo(expected));
        r = C(9, P(9, "Archaludon", options), P(9, "Arceus"), M(9, "Electro Shot"));
        Assert.That(r.Attacker.Boosts.Spa, Is.EqualTo(expected));
      }

      TestCase(new State.Pokemon(), 1);
      TestCase(new State.Pokemon { Boosts = new StatsTableInput { Spa = 6 } }, 6);
      TestCase(new State.Pokemon { Ability = "Simple" }, 2);
      TestCase(new State.Pokemon { Ability = "Contrary" }, -1);
    }

    [Test]
    public void Gen9_ProtosynthesisSunPoltergeistKnockOff() {
      var smeargle = P(9, "Smeargle");
      var defender2 = P(9, "Gouging Fire", new State.Pokemon { Ability = "Protosynthesis", Item = "Blunder Policy" });
      var field2 = F(new State.Field { Weather = "Sun" });
      var knockOff = C(9, smeargle, defender2, M(9, "Knock Off"), field2);
      Assert.That(knockOff.RawDesc.MoveBP, Is.EqualTo(97.5));
      var poltergeist = C(9, smeargle, defender2, M(9, "Poltergeist"), field2);
      Assert.That(poltergeist.Move.Bp, Is.EqualTo(110));
    }

    [Test]
    public void Gen9_QuarkDriveElectricTerrainPoltergeistKnockOff() {
      var smeargle = P(9, "Smeargle");
      var defender2 = P(9, "Iron Valiant", new State.Pokemon { Ability = "Quark Drive", Item = "Blunder Policy" });
      var field3 = F(new State.Field { Weather = "Sun" });
      var knockOff = C(9, smeargle, defender2, M(9, "Knock Off"), field3);
      Assert.That(knockOff.RawDesc.MoveBP, Is.EqualTo(97.5));
      var poltergeist = C(9, smeargle, defender2, M(9, "Poltergeist"), field3);
      Assert.That(poltergeist.Move.Bp, Is.EqualTo(110));
    }

    [Test]
    public void Gen9_RevelationDanceTeraType() {
      var attacker3 = P(9, "Oricorio-Pom-Pom");
      var defender3 = P(9, "Sandaconda");
      var result3 = C(9, attacker3, defender3, M(9, "Revelation Dance"));
      Assert.That(result3.Move.Type, Is.EqualTo("Electric"));
      attacker3.TeraType = "Water";
      result3 = C(9, attacker3, defender3, M(9, "Revelation Dance"));
      Assert.That(result3.Move.Type, Is.EqualTo("Water"));
    }

    [Test]
    public void Gen9_PsychicNoise() {
      var attacker4 = P(9, "Mewtwo");
      var defender4 = P(9, "Regigigas", new State.Pokemon { Ability = "Poison Heal", Item = "Leftovers", Status = "tox" });
      var result4 = C(9, attacker4, defender4, M(9, "Psychic Noise"), F(new State.Field { Terrain = "Grassy", AttackerSide = new State.Side { IsSeeded = true } }));
      Assert.That(result4.Desc(), Is.EqualTo(
        "0 SpA Mewtwo Psychic Noise vs. 0 HP / 0 SpD Regigigas: 109-129 (30.1 - 35.7%) -- 31.2% chance to 3HKO"
      ));
    }

    [Test]
    public void Gen9_FlowerGiftPowerSpotNoDoubleSpaces() {
      var attacker5 = P(9, "Weavile");
      var defender5 = P(9, "Vulpix");
      var field5 = F(new State.Field {
        Weather = "Sun",
        AttackerSide = new State.Side { IsFlowerGift = true, IsPowerSpot = true },
        DefenderSide = new State.Side { IsSwitching = "out" },
      });
      var result5 = C(9, attacker5, defender5, M(9, "Pursuit"), field5);
      Assert.That(result5.Desc(), Is.EqualTo(
        "0 Atk Weavile with an ally's Flower Gift Power Spot boosted switching boosted Pursuit (80 BP) vs. 0 HP / 0 Def Vulpix in Sun: 399-469 (183.8 - 216.1%) -- guaranteed OHKO"
      ));
    }

    [Test]
    public void Gen9_PowerTrick() {
      var attacker6 = P(9, "Bastiodon");
      var defender6 = P(9, "Glaceon");
      var result6 = C(9, attacker6, defender6, M(9, "Iron Head"), F(new State.Field { AttackerSide = new State.Side { IsPowerTrick = true } }));
      Assert.That(result6.Desc(), Is.EqualTo(
        "0 Atk Bastiodon with Power Trick Iron Head vs. 0 HP / 0 Def Glaceon: 252-296 (92.9 - 109.2%) -- 56.3% chance to OHKO"
      ));
    }

    [Test]
    public void Gen9_WindRider() {
      var attacker7 = P(9, "Brambleghast", new State.Pokemon { Ability = "Wind Rider" });
      var defender7 = P(9, "Brambleghast", new State.Pokemon { Ability = "Wind Rider" });
      var field7 = F(new State.Field { AttackerSide = new State.Side { IsTailwind = true } });
      var result7 = C(9, attacker7, defender7, M(9, "Power Whip"), field7);
      Assert.That(attacker7.Boosts.Atk, Is.EqualTo(0));
      Assert.That(result7.Attacker.Boosts.Atk, Is.EqualTo(1));
    }

    [Test]
    public void Gen9_TeraBasePowerUnder40() {
      var teraPokemon = P(9, "Arceus", new State.Pokemon { TeraType = "Normal" });
      Assert.That(C(9, teraPokemon, teraPokemon, M(9, "Scratch")).RawDesc.MoveBP, Is.EqualTo(60));
    }

    [Test]
    public void Gen9_TeraMultihitNoBPBoost() {
      var teraPokemon = P(9, "Arceus", new State.Pokemon { TeraType = "Normal" });
      Assert.That(C(9, teraPokemon, teraPokemon, M(9, "Spike Cannon")).RawDesc.MoveBP, Is.Null);
    }

    [Test]
    public void Gen9_TeraPriorityNoBPBoost() {
      var teraPokemon = P(9, "Arceus", new State.Pokemon { TeraType = "Normal" });
      Assert.That(C(9, teraPokemon, teraPokemon, M(9, "Quick Attack")).RawDesc.MoveBP, Is.Null);
    }

    [Test]
    public void Gen9_TriageTeraBPBoost() {
      var triage = P(9, "Comfey", new State.Pokemon { Ability = "Triage", TeraType = "Fairy" });
      Assert.That(C(9, triage, triage, M(9, "Draining Kiss")).RawDesc.MoveBP, Is.EqualTo(60));
      triage.TeraType = "Grass";
      Assert.That(C(9, triage, triage, M(9, "Absorb")).RawDesc.MoveBP, Is.EqualTo(60));
      triage.TeraType = "Stellar";
      Assert.That(C(9, triage, triage, M(9, "Draining Kiss", new State.Move { IsStellarFirstUse = true })).RawDesc.MoveBP, Is.EqualTo(60));
      Assert.That(C(9, triage, triage, M(9, "Absorb", new State.Move { IsStellarFirstUse = true })).RawDesc.MoveBP, Is.EqualTo(60));
    }

    [Test]
    public void Gen9_GaleWingsTeraBPBoost() {
      var gale = P(9, "Talonflame", new State.Pokemon { Ability = "Gale Wings", TeraType = "Flying" });
      Assert.That(C(9, gale, gale, M(9, "Peck")).RawDesc.MoveBP, Is.EqualTo(60));
      gale.TeraType = "Stellar";
      Assert.That(C(9, gale, gale, M(9, "Peck", new State.Move { IsStellarFirstUse = true })).RawDesc.MoveBP, Is.EqualTo(60));
    }

    [Test]
    public void Gen9_TeraStellarDefenderDisplay() {
      var terastal = P(9, "Arceus", new State.Pokemon { TeraType = "Stellar" });
      var control = P(9, "Arceus");
      Assert.That(C(9, control, terastal, M(9, "Tera Blast")).RawDesc.DefenderTera, Is.Null);
      Assert.That(C(9, terastal, terastal, M(9, "Tera Blast")).RawDesc.DefenderTera, Is.Not.Null);
      Assert.That(C(9, terastal, terastal, M(9, "Tera Blast", new State.Move { IsStellarFirstUse = true })).RawDesc.DefenderTera, Is.Not.Null);
      Assert.That(C(9, control, terastal, M(9, "Tera Blast", new State.Move { IsStellarFirstUse = true })).RawDesc.DefenderTera, Is.Null);
    }

    [Test]
    public void Gen9_TeraStellarNonBoostedDisplay() {
      var terastal = P(9, "Arceus", new State.Pokemon { TeraType = "Stellar" });
      var control = P(9, "Arceus");
      Assert.That(C(9, terastal, control, M(9, "Judgment", new State.Move { IsStellarFirstUse = false })).RawDesc.AttackerTera, Is.Null);
    }

    [Test]
    public void Gen9_TeraStellarFirstUseDistinction() {
      var terastal = P(9, "Arceus", new State.Pokemon { TeraType = "Stellar" });
      var control = P(9, "Arceus");
      var resultA = C(9, terastal, control, M(9, "Tera Blast", new State.Move { IsStellarFirstUse = true })).RawDesc.IsStellarFirstUse;
      var resultB = C(9, terastal, control, M(9, "Tera Blast", new State.Move { IsStellarFirstUse = false })).RawDesc.IsStellarFirstUse;
      Assert.That(resultA, Is.Not.EqualTo(resultB));
    }

    [Test]
    public void Gen9_TeraStellarBPBoostOnFirstUse() {
      var terastal = P(9, "Arceus", new State.Pokemon { TeraType = "Stellar" });
      var control = P(9, "Arceus");
      Assert.That(C(9, terastal, control, M(9, "Water Gun", new State.Move { IsStellarFirstUse = true })).RawDesc.MoveBP, Is.EqualTo(60));
      Assert.That(C(9, terastal, control, M(9, "Water Gun", new State.Move { IsStellarFirstUse = false })).RawDesc.MoveBP, Is.Null);
      Assert.That(C(9, terastal, control, M(9, "Scratch", new State.Move { IsStellarFirstUse = true })).RawDesc.MoveBP, Is.EqualTo(60));
      Assert.That(C(9, terastal, control, M(9, "Scratch", new State.Move { IsStellarFirstUse = false })).RawDesc.MoveBP, Is.Null);
    }

    [Test]
    public void Gen9_TeraStellarBoostInMistyTerrain() {
      var terrainPokemon = P(9, "Dracovish", new State.Pokemon { TeraType = "Stellar" });
      Assert.That(C(9, terrainPokemon, terrainPokemon, M(9, "Dragon Rush", new State.Move { IsStellarFirstUse = true }), F(new State.Field { Terrain = "Misty" })).RawDesc.MoveBP, Is.EqualTo(60));
    }

    [Test]
    public void Gen9_TeraStellarBoostInGrassyTerrain() {
      var terrainPokemon = P(9, "Dracovish", new State.Pokemon { TeraType = "Stellar" });
      Assert.That(C(9, terrainPokemon, terrainPokemon, M(9, "Earthquake", new State.Move { IsStellarFirstUse = true }), F(new State.Field { Terrain = "Grassy" })).RawDesc.MoveBP, Is.EqualTo(60));
    }

    [Test]
    public void Gen9_NihilLightNeutral() {
      var attacker8 = P(9, "Zygarde-Mega", new State.Pokemon { TeraType = "Electric" });
      var nihilLight = M(9, "Nihil Light");
      var otherMove = M(9, "Electro Drift");

      var defenderA = P(9, "Arceus-Fairy");
      var nihilResult = C(9, attacker8, defenderA, nihilLight);
      var otherResult = C(9, attacker8, defenderA, otherMove);
      Assert.That(nihilResult.Range(), Is.EqualTo(otherResult.Range()));
    }

    [Test]
    public void Gen9_NihilLightResistant() {
      var attacker8 = P(9, "Zygarde-Mega", new State.Pokemon { TeraType = "Electric" });
      var nihilLight = M(9, "Nihil Light");
      var otherMove = M(9, "Electro Drift");

      var defenderB = P(9, "Mawile");
      var nihilResult = C(9, attacker8, defenderB, nihilLight);
      var otherResult = C(9, attacker8, defenderB, otherMove);
      Assert.That(nihilResult.Range().min, Is.LessThan(otherResult.Range().min));
      Assert.That(nihilResult.Range().max, Is.LessThan(otherResult.Range().max));
    }

    [Test]
    public void Gen9_NihilLightWeak() {
      var attacker8 = P(9, "Zygarde-Mega", new State.Pokemon { TeraType = "Electric" });
      var nihilLight = M(9, "Nihil Light");
      var otherMove = M(9, "Electro Drift");

      var defenderC = P(9, "Altaria-Mega");
      var nihilResult = C(9, attacker8, defenderC, nihilLight);
      var otherResult = C(9, attacker8, defenderC, otherMove);
      Assert.That(nihilResult.Range().min, Is.GreaterThan(otherResult.Range().min));
      Assert.That(nihilResult.Range().max, Is.GreaterThan(otherResult.Range().max));
    }

    [Test]
    public void Descriptions_ChanceNotRoundTo100Percent() {
      var result = C(
        9,
        P(9, "Xerneas", new State.Pokemon { Item = "Choice Band", Nature = "Adamant", Evs = new StatsTableInput { Atk = 252 } }),
        P(9, "Necrozma-Dusk-Mane", new State.Pokemon { Nature = "Impish", Evs = new StatsTableInput { Hp = 252, Def = 252 } }),
        M(9, "Close Combat")
      );
      Assert.That(result.Kochance().chance, Is.GreaterThanOrEqualTo(0.9995));
      Assert.That(result.Kochance().text, Is.EqualTo("99.9% chance to 3HKO"));
    }

    [Test]
    public void Descriptions_ChanceNotRoundTo0Percent() {
      var result = C(
        9,
        P(9, "Deoxys-Attack", new State.Pokemon { Evs = new StatsTableInput { Spa = 44 } }),
        P(9, "Blissey", new State.Pokemon { Nature = "Calm", Evs = new StatsTableInput { Hp = 252, Spd = 252 } }),
        M(9, "Psycho Boost")
      );
      Assert.That(result.Kochance().chance, Is.LessThan(0.005));
      Assert.That(result.Kochance().text, Is.EqualTo("0.1% chance to 4HKO"));
    }

    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void BrickBreakBreaksScreens(int gen) {
      var pokemon = P(gen, "Mew");
      var brickBreak = M(gen, "Brick Break");
      var otherMove = M(gen, "Vital Throw", new State.Move { Overrides = new MoveData { BasePower = 75 } });
      var field = F(new State.Field { DefenderSide = new State.Side { IsReflect = true } });
      var brickBreakResult = C(gen, pokemon, pokemon, brickBreak, field);
      Assert.That(brickBreakResult.Field.DefenderSide.IsReflect, Is.False);
      var otherMoveResult = C(gen, pokemon, pokemon, otherMove, field);
      Assert.That(otherMoveResult.Field.DefenderSide.IsReflect, Is.True);
      Assert.That(brickBreakResult.Range().min, Is.GreaterThan(otherMoveResult.Range().min));
      Assert.That(brickBreakResult.Range().max, Is.GreaterThan(otherMoveResult.Range().max));
    }

    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void PsychicFangsBreaksScreens(int gen) {
      var pokemon = P(gen, "Mew");
      var psychicFangs = M(gen, "Psychic Fangs");
      var otherMove = M(gen, "Zen Headbutt", new State.Move { Overrides = new MoveData { BasePower = 75 } });
      var field = F(new State.Field { DefenderSide = new State.Side { IsReflect = true } });
      var psychicFangsResult = C(gen, pokemon, pokemon, psychicFangs, field);
      Assert.That(psychicFangsResult.Field.DefenderSide.IsReflect, Is.False);
      var otherMoveResult = C(gen, pokemon, pokemon, otherMove, field);
      Assert.That(otherMoveResult.Field.DefenderSide.IsReflect, Is.True);
      Assert.That(psychicFangsResult.Range().min, Is.GreaterThan(otherMoveResult.Range().min));
      Assert.That(psychicFangsResult.Range().max, Is.GreaterThan(otherMoveResult.Range().max));
    }

    [Test]
    public void RagingBullBreaksScreens() {
      var tauros = P(9, "Tauros-Paldea-Aqua");
      var ragingBull = M(9, "Raging Bull");
      var otherMove2 = M(9, "Waterfall", new State.Move { Overrides = new MoveData { BasePower = 90 } });
      var field2 = F(new State.Field { DefenderSide = new State.Side { IsReflect = true } });
      var ragingBullResult = C(9, tauros, tauros, ragingBull, field2);
      Assert.That(ragingBullResult.Field.DefenderSide.IsReflect, Is.False);
      var otherMoveResult2 = C(9, tauros, tauros, otherMove2, field2);
      Assert.That(otherMoveResult2.Field.DefenderSide.IsReflect, Is.True);
      Assert.That(ragingBullResult.Range().min, Is.GreaterThan(otherMoveResult2.Range().min));
      Assert.That(ragingBullResult.Range().max, Is.GreaterThan(otherMoveResult2.Range().max));
    }
  }
}
