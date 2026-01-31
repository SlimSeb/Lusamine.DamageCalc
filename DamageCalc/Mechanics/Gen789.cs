using System;
using System.Collections.Generic;
using DamageCalc;
using DamageCalc.Data;

namespace DamageCalc.Mechanics {
  public static class Gen789 {
    private static readonly Dictionary<string, StatId> SEED_BOOSTED_STAT = new Dictionary<string, StatId> {
      { "Electric Seed", StatId.Def },
      { "Grassy Seed", StatId.Def },
      { "Misty Seed", StatId.Spd },
      { "Psychic Seed", StatId.Spd },
    };

    public static Result CalculateSMSSSV(IGeneration gen, Pokemon attacker, Pokemon defender, Move move, Field field) {
      MechanicsUtil.CheckAirLock(attacker, field);
      MechanicsUtil.CheckAirLock(defender, field);
      MechanicsUtil.CheckTeraformZero(attacker, field);
      MechanicsUtil.CheckTeraformZero(defender, field);
      MechanicsUtil.CheckForecast(attacker, field.Weather);
      MechanicsUtil.CheckForecast(defender, field.Weather);
      MechanicsUtil.CheckItem(attacker, field.IsMagicRoom);
      MechanicsUtil.CheckItem(defender, field.IsMagicRoom);
      MechanicsUtil.CheckWonderRoom(attacker, field.IsWonderRoom);
      MechanicsUtil.CheckWonderRoom(defender, field.IsWonderRoom);
      MechanicsUtil.CheckSeedBoost(attacker, field);
      MechanicsUtil.CheckSeedBoost(defender, field);
      MechanicsUtil.CheckDauntlessShield(attacker, gen);
      MechanicsUtil.CheckDauntlessShield(defender, gen);
      MechanicsUtil.CheckEmbody(attacker, gen);
      MechanicsUtil.CheckEmbody(defender, gen);

      MechanicsUtil.ComputeFinalStats(gen, attacker, defender, field, StatId.Def, StatId.Spd, StatId.Spe);

      MechanicsUtil.CheckIntimidate(gen, attacker, defender);
      MechanicsUtil.CheckIntimidate(gen, defender, attacker);
      MechanicsUtil.CheckDownload(attacker, defender, field.IsWonderRoom);
      MechanicsUtil.CheckDownload(defender, attacker, field.IsWonderRoom);
      MechanicsUtil.CheckIntrepidSword(attacker, gen);
      MechanicsUtil.CheckIntrepidSword(defender, gen);

      MechanicsUtil.CheckWindRider(attacker, field.AttackerSide);
      MechanicsUtil.CheckWindRider(defender, field.DefenderSide);

      if (move.Named("Meteor Beam", "Electro Shot")) {
        var delta = attacker.HasAbility("Simple") ? 2 : attacker.HasAbility("Contrary") ? -1 : 1;
        attacker.Boosts.Spa += delta;
        attacker.Boosts.Spa = Math.Min(6, Math.Max(-6, attacker.Boosts.Spa));
      }

      MechanicsUtil.ComputeFinalStats(gen, attacker, defender, field, StatId.Atk, StatId.Spa);

      MechanicsUtil.CheckInfiltrator(attacker, field.DefenderSide);
      MechanicsUtil.CheckInfiltrator(defender, field.AttackerSide);

      var desc = new RawDesc {
        AttackerName = attacker.Name,
        MoveName = move.Name,
        DefenderName = defender.Name,
        IsDefenderDynamaxed = defender.IsDynamaxed,
        IsWonderRoom = field.IsWonderRoom,
      };

      if (attacker.TeraType != "Stellar" || move.Name == "Tera Blast" || move.IsStellarFirstUse) {
        desc.IsStellarFirstUse = attacker.Name != "Terapagos-Stellar" && move.Name == "Tera Blast" &&
          attacker.TeraType == "Stellar" && move.IsStellarFirstUse;
        desc.AttackerTera = attacker.TeraType;
      }
      if (defender.TeraType != "Stellar") desc.DefenderTera = defender.TeraType;

      if (move.Named("Photon Geyser", "Light That Burns the Sky") ||
          (move.Named("Tera Blast") && !string.IsNullOrEmpty(attacker.TeraType)) ||
          (move.Named("Tera Starstorm") && !string.IsNullOrEmpty(attacker.TeraType) && attacker.Named("Terapagos-Stellar"))) {
        move.Category = attacker.Stats.Atk > attacker.Stats.Spa ? MoveCategories.Physical : MoveCategories.Special;
      }

      var result = new Result(gen, attacker, defender, move, field, 0, desc);

      if (move.Category == MoveCategories.Status && !move.Named("Nature Power")) return result;

      if (move.Flags.Punch && attacker.HasItem("Punching Glove")) {
        desc.AttackerItem = attacker.Item;
        move.Flags.Contact = false;
      }

      if (move.Named("Shell Side Arm") &&
          MechanicsUtil.GetShellSideArmCategory(attacker, defender) == MoveCategories.Physical) {
        move.Category = MoveCategories.Physical;
        move.Flags.Contact = true;
      }

      var breaksProtect = move.BreaksProtect || move.IsZ || attacker.IsDynamaxed ||
        (attacker.HasAbility("Unseen Fist") && move.Flags.Contact);

      if (field.DefenderSide.IsProtected && !breaksProtect) {
        desc.IsProtected = true;
        return result;
      }

      if (move.Name == "Pain Split") {
        var average = (int)Math.Floor((attacker.CurHP() + defender.CurHP()) / 2.0);
        var dmg = Math.Max(0, defender.CurHP() - average);
        result.Damage = dmg;
        return result;
      }

      var defenderAbilityIgnored = defender.HasAbility(
        "Armor Tail", "Aroma Veil", "Aura Break", "Battle Armor",
        "Big Pecks", "Bulletproof", "Clear Body", "Contrary",
        "Damp", "Dazzling", "Disguise", "Dry Skin",
        "Earth Eater", "Filter", "Flash Fire", "Flower Gift",
        "Flower Veil", "Fluffy", "Friend Guard", "Fur Coat",
        "Good as Gold", "Grass Pelt", "Guard Dog", "Heatproof",
        "Heavy Metal", "Hyper Cutter", "Ice Face", "Ice Scales",
        "Illuminate", "Immunity", "Inner Focus", "Insomnia",
        "Keen Eye", "Leaf Guard", "Levitate", "Light Metal",
        "Lightning Rod", "Limber", "Magic Bounce", "Magma Armor",
        "Marvel Scale", "Mind's Eye", "Mirror Armor", "Motor Drive",
        "Multiscale", "Oblivious", "Overcoat", "Own Tempo",
        "Pastel Veil", "Punk Rock", "Purifying Salt", "Queenly Majesty",
        "Sand Veil", "Sap Sipper", "Shell Armor", "Shield Dust",
        "Simple", "Snow Cloak", "Solid Rock", "Soundproof",
        "Sticky Hold", "Storm Drain", "Sturdy", "Suction Cups",
        "Sweet Veil", "Tangled Feet", "Telepathy", "Tera Shell",
        "Thermal Exchange", "Thick Fat", "Unaware", "Vital Spirit",
        "Volt Absorb", "Water Absorb", "Water Bubble", "Water Veil",
        "Well-Baked Body", "White Smoke", "Wind Rider", "Wonder Guard",
        "Wonder Skin"
      );

      var attackerIgnoresAbility = attacker.HasAbility("Mold Breaker", "Teravolt", "Turboblaze");
      var moveIgnoresAbility = move.Named(
        "G-Max Drum Solo",
        "G-Max Fire Ball",
        "G-Max Hydrosnipe",
        "Light That Burns the Sky",
        "Menacing Moonraze Maelstrom",
        "Moongeist Beam",
        "Photon Geyser",
        "Searing Sunraze Smash",
        "Sunsteel Strike"
      );

      if (defenderAbilityIgnored && (attackerIgnoresAbility || moveIgnoresAbility)) {
        if (attackerIgnoresAbility) desc.AttackerAbility = attacker.Ability;
        if (defender.HasItem("Ability Shield")) {
          desc.DefenderItem = defender.Item;
        } else {
          defender.Ability = "";
        }
      }

      var ignoresNeutralizingGas = new HashSet<string> {
        "As One (Glastrier)", "As One (Spectrier)", "Battle Bond", "Comatose",
        "Disguise", "Gulp Missile", "Ice Face", "Multitype", "Neutralizing Gas",
        "Power Construct", "RKS System", "Schooling", "Shields Down",
        "Stance Change", "Tera Shift", "Zen Mode", "Zero to Hero",
      };

      if (attacker.HasAbility("Neutralizing Gas") &&
          !ignoresNeutralizingGas.Contains(defender.Ability ?? "")) {
        desc.AttackerAbility = attacker.Ability;
        if (defender.HasItem("Ability Shield")) {
          desc.DefenderItem = defender.Item;
        } else {
          defender.Ability = "";
        }
      }

      if (defender.HasAbility("Neutralizing Gas") &&
          !ignoresNeutralizingGas.Contains(attacker.Ability ?? "")) {
        desc.DefenderAbility = defender.Ability;
        if (attacker.HasItem("Ability Shield")) {
          desc.AttackerItem = attacker.Item;
        } else {
          attacker.Ability = "";
        }
      }

      var isCritical = !defender.HasAbility("Battle Armor", "Shell Armor") &&
        (move.IsCrit || (attacker.HasAbility("Merciless") && defender.HasStatus("psn", "tox"))) &&
        (move.TimesUsed ?? 1) == 1;

      var type = move.Type;
      if (move.OriginalName == "Weather Ball") {
        var holdingUmbrella = attacker.HasItem("Utility Umbrella");
        type =
          field.HasWeather("Sun", "Harsh Sunshine") && !holdingUmbrella ? "Fire"
          : field.HasWeather("Rain", "Heavy Rain") && !holdingUmbrella ? "Water"
          : field.HasWeather("Sand") ? "Rock"
          : field.HasWeather("Hail", "Snow") ? "Ice"
          : "Normal";
        desc.Weather = field.Weather;
        desc.MoveType = type;
      } else if (move.Named("Judgment") && !string.IsNullOrEmpty(attacker.Item) && attacker.Item.Contains("Plate")) {
        type = Items.GetItemBoostType(attacker.Item) ?? type;
      } else if (move.OriginalName == "Techno Blast" &&
          !string.IsNullOrEmpty(attacker.Item) && attacker.Item.Contains("Drive")) {
        type = Items.GetTechnoBlast(attacker.Item) ?? type;
        desc.MoveType = type;
      } else if (move.OriginalName == "Multi-Attack" &&
          !string.IsNullOrEmpty(attacker.Item) && attacker.Item.Contains("Memory")) {
        type = Items.GetMultiAttack(attacker.Item) ?? type;
        desc.MoveType = type;
      } else if (move.Named("Natural Gift") && attacker.Item != null && attacker.Item.EndsWith("Berry")) {
        var gift = Items.GetNaturalGift(gen, attacker.Item);
        type = gift.type;
        desc.MoveType = type;
        desc.AttackerItem = attacker.Item;
      } else if (
        move.Named("Nature Power") ||
        (move.OriginalName == "Terrain Pulse" && MechanicsUtil.IsGrounded(attacker, field))
      ) {
        type =
          field.HasTerrain("Electric") ? "Electric"
          : field.HasTerrain("Grassy") ? "Grass"
          : field.HasTerrain("Misty") ? "Fairy"
          : field.HasTerrain("Psychic") ? "Psychic"
          : "Normal";
        desc.Terrain = field.Terrain;

        if (move.IsMax) {
          desc.MoveType = type;
        }

        if (!(move.Named("Nature Power") && attacker.HasAbility("Prankster")) &&
            ((Array.Exists(defender.Types, t => t == "Dark") ||
            (field.HasTerrain("Psychic") && MechanicsUtil.IsGrounded(defender, field))))) {
          desc.MoveType = type;
        }
      } else if (move.OriginalName == "Revelation Dance") {
        type = !string.IsNullOrEmpty(attacker.TeraType) ? attacker.TeraType : attacker.Types[0];
      } else if (move.Named("Aura Wheel")) {
        if (attacker.Named("Morpeko")) {
          type = "Electric";
        } else if (attacker.Named("Morpeko-Hangry")) {
          type = "Dark";
        }
      } else if (move.Named("Raging Bull")) {
        if (attacker.Named("Tauros-Paldea-Combat")) {
          type = "Fighting";
        } else if (attacker.Named("Tauros-Paldea-Blaze")) {
          type = "Fire";
        } else if (attacker.Named("Tauros-Paldea-Aqua")) {
          type = "Water";
        }

        field.DefenderSide.IsReflect = false;
        field.DefenderSide.IsLightScreen = false;
        field.DefenderSide.IsAuroraVeil = false;
      } else if (move.Named("Ivy Cudgel")) {
        if (attacker.Name.Contains("Ogerpon-Cornerstone")) {
          type = "Rock";
        } else if (attacker.Name.Contains("Ogerpon-Hearthflame")) {
          type = "Fire";
        } else if (attacker.Name.Contains("Ogerpon-Wellspring")) {
          type = "Water";
        }
      } else if (move.Named("Tera Starstorm") && attacker.Name == "Terapagos-Stellar") {
        move.Target = "allAdjacentFoes";
        type = "Stellar";
      } else if (move.Named("Brick Break", "Psychic Fangs")) {
        field.DefenderSide.IsReflect = false;
        field.DefenderSide.IsLightScreen = false;
        field.DefenderSide.IsAuroraVeil = false;
      }

      var hasAteAbilityTypeChange = false;
      var isAerilate = false;
      var isPixilate = false;
      var isRefrigerate = false;
      var isGalvanize = false;
      var isLiquidVoice = false;
      var isNormalize = false;
      var noTypeChange = move.Named(
        "Revelation Dance",
        "Judgment",
        "Nature Power",
        "Techno Blast",
        "Multi-Attack",
        "Natural Gift",
        "Weather Ball",
        "Terrain Pulse",
        "Struggle"
      ) || (move.Named("Tera Blast") && !string.IsNullOrEmpty(attacker.TeraType));

      if (!move.IsZ && !noTypeChange) {
        var normal = type == "Normal";
        if ((isAerilate = attacker.HasAbility("Aerilate") && normal)) {
          type = "Flying";
        } else if ((isGalvanize = attacker.HasAbility("Galvanize") && normal)) {
          type = "Electric";
        } else if ((isLiquidVoice = attacker.HasAbility("Liquid Voice") && move.Flags.Sound)) {
          type = "Water";
        } else if ((isPixilate = attacker.HasAbility("Pixilate") && normal)) {
          type = "Fairy";
        } else if ((isRefrigerate = attacker.HasAbility("Refrigerate") && normal)) {
          type = "Ice";
        } else if ((isNormalize = attacker.HasAbility("Normalize"))) {
          type = "Normal";
        }
        if (isGalvanize || isPixilate || isRefrigerate || isAerilate || isNormalize) {
          desc.AttackerAbility = attacker.Ability;
          hasAteAbilityTypeChange = true;
        } else if (isLiquidVoice) {
          desc.AttackerAbility = attacker.Ability;
        }
      }

      if (move.Named("Tera Blast") && !string.IsNullOrEmpty(attacker.TeraType)) {
        type = attacker.TeraType;
      }

      move.Type = type;

      var isGhostRevealed =
        attacker.HasAbility("Scrappy") || attacker.HasAbility("Mind's Eye") ||
          field.DefenderSide.IsForesight;
      var isRingTarget =
        defender.HasItem("Ring Target") && !defender.HasAbility("Klutz");
      var type1Effectiveness = MechanicsUtil.GetMoveEffectiveness(
        gen,
        move,
        defender.Types[0],
        isGhostRevealed,
        field.IsGravity,
        isRingTarget
      );
      var type2Effectiveness = defender.Types.Length > 1
        ? MechanicsUtil.GetMoveEffectiveness(
          gen,
          move,
          defender.Types[1],
          isGhostRevealed,
          field.IsGravity,
          isRingTarget
        )
        : 1;

      var typeEffectiveness = type1Effectiveness * type2Effectiveness;

      if (!string.IsNullOrEmpty(defender.TeraType) && defender.TeraType != "Stellar") {
        typeEffectiveness = MechanicsUtil.GetMoveEffectiveness(
          gen,
          move,
          defender.TeraType,
          isGhostRevealed,
          field.IsGravity,
          isRingTarget
        );
      }

      if (typeEffectiveness == 0 && move.HasType("Ground") &&
          defender.HasItem("Iron Ball") && !defender.HasAbility("Klutz")) {
        typeEffectiveness = 1;
      }

      if (typeEffectiveness == 0 && move.Named("Thousand Arrows")) {
        typeEffectiveness = 1;
      }

      if (typeEffectiveness == 0) {
        return result;
      }

      if ((move.Named("Sky Drop") &&
          (defender.HasType("Flying") || defender.WeightKg >= 200 || field.IsGravity)) ||
          (move.Named("Synchronoise") && !defender.HasType(attacker.Types[0]) &&
            (attacker.Types.Length < 2 || !defender.HasType(attacker.Types[1]))) ||
          (move.Named("Dream Eater") &&
            (!(defender.HasStatus("slp") || defender.HasAbility("Comatose")))) ||
          (move.Named("Steel Roller") && string.IsNullOrEmpty(field.Terrain)) ||
          (move.Named("Poltergeist") &&
            (string.IsNullOrEmpty(defender.Item) || (MechanicsUtil.IsQPActive(defender, field) && defender.HasItem("Booster Energy"))))
      ) {
        return result;
      }

      if (
        (field.HasWeather("Harsh Sunshine") && move.HasType("Water")) ||
        (field.HasWeather("Heavy Rain") && move.HasType("Fire"))
      ) {
        desc.Weather = field.Weather;
        return result;
      }

      if (field.HasWeather("Strong Winds") && defender.HasType("Flying") &&
          (gen.Types.Get(Util.ToId(move.Type))?.Effectiveness["Flying"] ?? 1) > 1) {
        typeEffectiveness /= 2;
        desc.Weather = field.Weather;
      }

      if (move.Type == "Stellar") {
        desc.DefenderTera = defender.TeraType;
        typeEffectiveness = string.IsNullOrEmpty(defender.TeraType) ? 1 : 2;
      }

      var turn2TypeEffectiveness = typeEffectiveness;

      if (defender.HasAbility("Tera Shell") &&
          defender.CurHP() == defender.MaxHP() &&
          (!field.DefenderSide.IsSR && (field.DefenderSide.Spikes == 0 || defender.HasType("Flying")) ||
          defender.HasItem("Heavy-Duty Boots"))
      ) {
        typeEffectiveness = 0.5;
        desc.DefenderAbility = defender.Ability;
      }

      if ((defender.HasAbility("Wonder Guard") && typeEffectiveness <= 1) ||
          (move.HasType("Grass") && defender.HasAbility("Sap Sipper")) ||
          (move.HasType("Fire") && defender.HasAbility("Flash Fire", "Well-Baked Body")) ||
          (move.HasType("Water") && defender.HasAbility("Dry Skin", "Storm Drain", "Water Absorb")) ||
          (move.HasType("Electric") &&
            defender.HasAbility("Lightning Rod", "Motor Drive", "Volt Absorb")) ||
          (move.HasType("Ground") &&
            !field.IsGravity && !move.Named("Thousand Arrows") &&
            !defender.HasItem("Iron Ball") && defender.HasAbility("Levitate")) ||
          (move.Flags.Bullet && defender.HasAbility("Bulletproof")) ||
          (move.Flags.Sound && !move.Named("Clangorous Soul") && defender.HasAbility("Soundproof")) ||
          (move.Priority > 0 && defender.HasAbility("Queenly Majesty", "Dazzling", "Armor Tail")) ||
          (move.HasType("Ground") && defender.HasAbility("Earth Eater")) ||
          (move.Flags.Wind && defender.HasAbility("Wind Rider"))
      ) {
        desc.DefenderAbility = defender.Ability;
        return result;
      }

      if (move.HasType("Ground") && !move.Named("Thousand Arrows") &&
          !field.IsGravity && defender.HasItem("Air Balloon")) {
        desc.DefenderItem = defender.Item;
        return result;
      }

      if (move.Priority > 0 && field.HasTerrain("Psychic") && MechanicsUtil.IsGrounded(defender, field)) {
        desc.Terrain = field.Terrain;
        return result;
      }

      var weightBasedMove = move.Named("Heat Crash", "Heavy Slam", "Low Kick", "Grass Knot");
      if (defender.IsDynamaxed && weightBasedMove) {
        return result;
      }

      desc.HPEVs = MechanicsUtil.GetStatDescriptionText(gen, defender, StatId.Hp, null);

      var fixedDamage = MechanicsUtil.HandleFixedDamageMoves(attacker, move);
      if (fixedDamage != 0) {
        if (attacker.HasAbility("Parental Bond")) {
          result.Damage = new[] { fixedDamage, fixedDamage };
          desc.AttackerAbility = attacker.Ability;
        } else {
          result.Damage = fixedDamage;
        }
        return result;
      }

      if (move.Named("Final Gambit")) {
        result.Damage = attacker.CurHP();
        return result;
      }

      if (move.Named("Guardian of Alola")) {
        var zLostHP = (int)Math.Floor((defender.CurHP() * 3) / 4.0);
        if (field.DefenderSide.IsProtected && attacker.Item != null && attacker.Item.Contains(" Z")) {
          zLostHP = (int)Math.Ceiling(zLostHP / 4.0 - 0.5);
        }
        result.Damage = zLostHP;
        return result;
      }

      if (move.Named("Nature's Madness")) {
        var lostHP = field.DefenderSide.IsProtected ? 0 : (int)Math.Floor(defender.CurHP() / 2.0);
        result.Damage = lostHP;
        return result;
      }

      if (move.Named("Spectral Thief")) {
        var stats = new[] { StatId.Atk, StatId.Def, StatId.Spa, StatId.Spd, StatId.Spe };
        foreach (var stat in stats) {
          if (defender.Boosts[stat] > 0) {
            attacker.Boosts[stat] += attacker.HasAbility("Contrary") ? -defender.Boosts[stat] : defender.Boosts[stat];
            attacker.Boosts[stat] = Math.Min(6, Math.Max(-6, attacker.Boosts[stat]));
            attacker.Stats[stat] = MechanicsUtil.GetModifiedStat(attacker.RawStats[stat], attacker.Boosts[stat]);
            defender.Boosts[stat] = 0;
            defender.Stats[stat] = defender.RawStats[stat];
          }
        }
      }

      if (move.Hits > 1) {
        desc.Hits = move.Hits;
      }

      var basePower = CalculateBasePowerSMSSSV(
        gen,
        attacker,
        defender,
        move,
        field,
        hasAteAbilityTypeChange,
        desc
      );
      if (basePower == 0) {
        return result;
      }

      var attack = CalculateAttackSMSSSV(gen, attacker, defender, move, field, desc, isCritical);
      var attackStat = move.Named("Body Press") ? StatId.Def : move.Category == MoveCategories.Special ? StatId.Spa : StatId.Atk;

      var defense = CalculateDefenseSMSSSV(gen, attacker, defender, move, field, desc, isCritical);
      var hitsPhysical = move.OverrideDefensiveStat == StatId.Def || move.Category == MoveCategories.Physical;
      var defenseStat = hitsPhysical ? StatId.Def : StatId.Spd;

      var baseDamage = CalculateBaseDamageSMSSSV(
        gen,
        attacker,
        defender,
        basePower,
        attack,
        defense,
        move,
        field,
        desc,
        isCritical
      );

      if ((attacker.HasAbility("Triage") && move.Drain != null) ||
          (attacker.HasAbility("Gale Wings") &&
           move.HasType("Flying") &&
           attacker.CurHP() == attacker.MaxHP())) {
        move.Priority = 1;
        desc.AttackerAbility = attacker.Ability;
      }

      if (HasTerrainSeed(defender) &&
          field.HasTerrain(defender.Item!.Substring(0, defender.Item!.IndexOf(' '))) &&
          SEED_BOOSTED_STAT[defender.Item!] == defenseStat) {
        desc.DefenderItem = defender.Item;
      }

      var preStellarStabMod = MechanicsUtil.GetStabMod(attacker, move, desc);
      var stabMod = MechanicsUtil.GetStellarStabMod(attacker, move, preStellarStabMod);

      var applyBurn =
        attacker.HasStatus("brn") &&
        move.Category == MoveCategories.Physical &&
        !attacker.HasAbility("Guts") &&
        !move.Named("Facade");
      desc.IsBurned = applyBurn;
      var finalMods = CalculateFinalModsSMSSSV(
        gen,
        attacker,
        defender,
        move,
        field,
        desc,
        isCritical,
        typeEffectiveness
      );

      var protect = false;
      if (field.DefenderSide.IsProtected &&
          (attacker.IsDynamaxed || (move.IsZ && attacker.Item != null && attacker.Item.Contains(" Z")))) {
        protect = true;
        desc.IsProtected = true;
      }

      var finalMod = MechanicsUtil.ChainMods(finalMods, 41, 131072);

      var isSpread = field.GameType != GameTypes.Singles &&
         (move.Target == "allAdjacent" || move.Target == "allAdjacentFoes");

      int[]? childDamage = null;
      if (attacker.HasAbility("Parental Bond") && move.Hits == 1 && !isSpread) {
        var child = attacker.Clone();
        child.Ability = "Parental Bond (Child)";
        MechanicsUtil.CheckMultihitBoost(gen, child, defender, move, field, desc);
        childDamage = (int[])CalculateSMSSSV(gen, child, defender, move, field).Damage;
        desc.AttackerAbility = attacker.Ability;
      }

      var damage = new int[16];
      for (var i = 0; i < 16; i++) {
        damage[i] = MechanicsUtil.GetFinalDamage(baseDamage, i, typeEffectiveness, applyBurn, stabMod, finalMod, protect);
      }
      result.Damage = childDamage != null ? new object[] { damage, childDamage } : damage;

      desc.AttackBoost = move.Named("Foul Play") ? defender.Boosts[attackStat] : attacker.Boosts[attackStat];

      if ((move.TimesUsed ?? 1) > 1 || move.Hits > 1) {
        var origDefBoost = desc.DefenseBoost;
        var origAtkBoost = desc.AttackBoost;

        var numAttacks = 1;
        if ((move.TimesUsed ?? 1) > 1) {
          desc.MoveTurns = $"over {move.TimesUsed} turns";
          numAttacks = move.TimesUsed ?? 1;
        } else {
          numAttacks = move.Hits;
        }
        var usedItems = (attackerUsed: false, defenderUsed: false);
        var damageMatrix = new int[numAttacks][];
        damageMatrix[0] = damage;
        for (var times = 1; times < numAttacks; times++) {
          usedItems = MechanicsUtil.CheckMultihitBoost(gen, attacker, defender, move,
            field, desc, usedItems.attackerUsed, usedItems.defenderUsed);
          var newAttack = CalculateAttackSMSSSV(gen, attacker, defender, move,
            field, desc, isCritical);
          var newDefense = CalculateDefenseSMSSSV(gen, attacker, defender, move,
            field, desc, isCritical);
          hasAteAbilityTypeChange = hasAteAbilityTypeChange &&
            attacker.HasAbility("Aerilate", "Galvanize", "Pixilate", "Refrigerate", "Normalize");

          if ((move.TimesUsed ?? 1) > 1) {
            preStellarStabMod = MechanicsUtil.GetStabMod(attacker, move, desc);
            typeEffectiveness = turn2TypeEffectiveness;
            stabMod = MechanicsUtil.GetStellarStabMod(attacker, move, preStellarStabMod, times);
          }

          var newBasePower = CalculateBasePowerSMSSSV(
            gen,
            attacker,
            defender,
            move,
            field,
            hasAteAbilityTypeChange,
            desc,
            times + 1
          );
          var newBaseDamage = CalculateBaseDamageSMSSSV(
            gen,
            attacker,
            defender,
            newBasePower,
            newAttack,
            newDefense,
            move,
            field,
            desc,
            isCritical
          );
          var newFinalMods = CalculateFinalModsSMSSSV(
            gen,
            attacker,
            defender,
            move,
            field,
            desc,
            isCritical,
            typeEffectiveness,
            times
          );
          var newFinalMod = MechanicsUtil.ChainMods(newFinalMods, 41, 131072);

          var damageArray = new int[16];
          for (var i = 0; i < 16; i++) {
            var newFinalDamage = MechanicsUtil.GetFinalDamage(
              newBaseDamage,
              i,
              typeEffectiveness,
              applyBurn,
              stabMod,
              newFinalMod,
              protect
            );
            damageArray[i] = newFinalDamage;
          }
          damageMatrix[times] = damageArray;
        }
        result.Damage = damageMatrix;
        desc.DefenseBoost = origDefBoost;
        desc.AttackBoost = origAtkBoost;
      }

      return result;
    }

    private static int CalculateBasePowerSMSSSV(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      Field field,
      bool hasAteAbilityTypeChange,
      RawDesc desc,
      int hit = 1
    ) {
      var turnOrder = attacker.Stats.Spe > defender.Stats.Spe ? "first" : "last";
      int basePower;

      switch (move.Name) {
        case "Payback":
          basePower = move.Bp * (turnOrder == "last" ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        case "Bolt Beak":
        case "Fishious Rend":
          basePower = move.Bp * (turnOrder != "last" ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        case "Pursuit":
          var switching = field.DefenderSide.IsSwitching == "out";
          basePower = move.Bp * (switching ? 2 : 1);
          if (switching) desc.IsSwitching = "out";
          desc.MoveBP = basePower;
          break;
        case "Electro Ball":
          var r = defender.Stats.Spe == 0 ? 0 : (int)Math.Floor(attacker.Stats.Spe / (double)defender.Stats.Spe);
          basePower = r >= 4 ? 150 : r >= 3 ? 120 : r >= 2 ? 80 : r >= 1 ? 60 : 40;
          if (defender.Stats.Spe == 0) basePower = 40;
          desc.MoveBP = basePower;
          break;
        case "Gyro Ball":
          if (attacker.Stats.Spe == 0) {
            basePower = 1;
          } else {
            basePower = Math.Min(150, (int)Math.Floor((25 * defender.Stats.Spe) / (double)attacker.Stats.Spe) + 1);
          }
          desc.MoveBP = basePower;
          break;
        case "Punishment":
          basePower = Math.Min(200, 60 + 20 * MechanicsUtil.CountBoosts(gen, defender.Boosts));
          desc.MoveBP = basePower;
          break;
        case "Low Kick":
        case "Grass Knot":
          var w = MechanicsUtil.GetWeight(defender, desc, "defender");
          basePower = w >= 200 ? 120 : w >= 100 ? 100 : w >= 50 ? 80 : w >= 25 ? 60 : w >= 10 ? 40 : 20;
          desc.MoveBP = basePower;
          break;
        case "Hex":
        case "Infernal Parade":
          basePower = move.Bp * (!string.IsNullOrEmpty(defender.Status) || defender.HasAbility("Comatose") ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        case "Barb Barrage":
          basePower = move.Bp * (defender.HasStatus("psn", "tox") ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        case "Heavy Slam":
        case "Heat Crash":
          var wr = MechanicsUtil.GetWeight(attacker, desc, "attacker") /
            MechanicsUtil.GetWeight(defender, desc, "defender");
          basePower = wr >= 5 ? 120 : wr >= 4 ? 100 : wr >= 3 ? 80 : wr >= 2 ? 60 : 40;
          desc.MoveBP = basePower;
          break;
        case "Stored Power":
        case "Power Trip":
          basePower = 20 + 20 * MechanicsUtil.CountBoosts(gen, attacker.Boosts);
          desc.MoveBP = basePower;
          break;
        case "Acrobatics":
          basePower = move.Bp * (attacker.HasItem("Flying Gem") ||
            string.IsNullOrEmpty(attacker.Item) ||
            (MechanicsUtil.IsQPActive(attacker, field) && attacker.HasItem("Booster Energy")) ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        case "Assurance":
          basePower = move.Bp * (defender.HasAbility("Parental Bond (Child)") ? 2 : 1);
          break;
        case "Wake-Up Slap":
          basePower = move.Bp * (defender.HasStatus("slp") || defender.HasAbility("Comatose") ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        case "Smelling Salts":
          basePower = move.Bp * (defender.HasStatus("par") ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        case "Weather Ball":
          basePower = move.Bp * (!string.IsNullOrEmpty(field.Weather) && !field.HasWeather("Strong Winds") ? 2 : 1);
          if (field.HasWeather("Sun", "Harsh Sunshine", "Rain", "Heavy Rain") &&
              attacker.HasItem("Utility Umbrella")) {
            basePower = move.Bp;
          }
          desc.MoveBP = basePower;
          break;
        case "Terrain Pulse":
          basePower = move.Bp * (MechanicsUtil.IsGrounded(attacker, field) && !string.IsNullOrEmpty(field.Terrain) ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        case "Rising Voltage":
          basePower = move.Bp * ((MechanicsUtil.IsGrounded(defender, field) && field.HasTerrain("Electric")) ? 2 : 1);
          desc.MoveBP = basePower;
          break;
        case "Psyblade":
          basePower = (int)Math.Floor(move.Bp * (field.HasTerrain("Electric") ? 1.5 : 1));
          if (field.HasTerrain("Electric")) {
            desc.MoveBP = basePower;
            desc.Terrain = field.Terrain;
          }
          break;
        case "Fling":
          basePower = Items.GetFlingPower(attacker.Item, gen.Num);
          desc.MoveBP = basePower;
          desc.AttackerItem = attacker.Item;
          break;
        case "Dragon Energy":
        case "Eruption":
        case "Water Spout":
          basePower = Math.Max(1, (int)Math.Floor((150 * attacker.CurHP()) / (double)attacker.MaxHP()));
          desc.MoveBP = basePower;
          break;
        case "Flail":
        case "Reversal":
          var p = (int)Math.Floor((48 * attacker.CurHP()) / (double)attacker.MaxHP());
          basePower = p <= 1 ? 200 : p <= 4 ? 150 : p <= 9 ? 100 : p <= 16 ? 80 : p <= 32 ? 40 : 20;
          desc.MoveBP = basePower;
          break;
        case "Natural Gift":
          if (attacker.Item != null && attacker.Item.EndsWith("Berry")) {
            var gift = Items.GetNaturalGift(gen, attacker.Item);
            basePower = gift.power;
            desc.AttackerItem = attacker.Item;
            desc.MoveBP = move.Bp;
          } else {
            basePower = move.Bp;
          }
          break;
        case "Nature Power":
          move.Category = MoveCategories.Special;
          move.Secondaries = true;

          if (attacker.HasAbility("Prankster") && Array.Exists(defender.Types, t => t == "Dark")) {
            basePower = 0;
            desc.MoveName = "Nature Power";
            desc.AttackerAbility = "Prankster";
            break;
          }
          switch (field.Terrain) {
            case "Electric":
              basePower = 90;
              desc.MoveName = "Thunderbolt";
              break;
            case "Grassy":
              basePower = 90;
              desc.MoveName = "Energy Ball";
              break;
            case "Misty":
              basePower = 95;
              desc.MoveName = "Moonblast";
              break;
            case "Psychic":
              if (attacker.HasAbility("Prankster") && MechanicsUtil.IsGrounded(defender, field)) {
                basePower = 0;
                desc.AttackerAbility = "Prankster";
              } else {
                basePower = 90;
                desc.MoveName = "Psychic";
              }
              break;
            default:
              basePower = 80;
              desc.MoveName = "Tri Attack";
              break;
          }
          break;
        case "Water Shuriken":
          basePower = attacker.Named("Greninja-Ash") && attacker.HasAbility("Battle Bond") ? 20 : 15;
          desc.MoveBP = basePower;
          break;
        case "Triple Axel":
          basePower = hit * 20;
          desc.MoveBP = move.Hits == 2 ? 60 : move.Hits == 3 ? 120 : 20;
          break;
        case "Triple Kick":
          basePower = hit * 10;
          desc.MoveBP = move.Hits == 2 ? 30 : move.Hits == 3 ? 60 : 10;
          break;
        case "Crush Grip":
        case "Wring Out":
          basePower = 100 * (int)Math.Floor((defender.CurHP() * 4096.0) / defender.MaxHP());
          basePower = (int)Math.Floor(Math.Floor((120 * basePower + 2048 - 1) / 4096.0) / 100.0);
          basePower = basePower == 0 ? 1 : basePower;
          desc.MoveBP = basePower;
          break;
        case "Hard Press":
          basePower = 100 * (int)Math.Floor((defender.CurHP() * 4096.0) / defender.MaxHP());
          basePower = (int)Math.Floor(Math.Floor((100 * basePower + 2048 - 1) / 4096.0) / 100.0);
          basePower = basePower == 0 ? 1 : basePower;
          desc.MoveBP = basePower;
          break;
        case "Tera Blast":
          basePower = attacker.TeraType == "Stellar" ? 100 : 80;
          desc.MoveBP = basePower;
          break;
        default:
          basePower = move.Bp;
          break;
      }

      if (basePower == 0) return 0;

      if (move.Named(
        "Breakneck Blitz", "Bloom Doom", "Inferno Overdrive", "Hydro Vortex", "Gigavolt Havoc",
        "Subzero Slammer", "Supersonic Skystrike", "Savage Spin-Out", "Acid Downpour", "Tectonic Rage",
        "Continental Crush", "All-Out Pummeling", "Shattered Psyche", "Never-Ending Nightmare",
        "Devastating Drake", "Black Hole Eclipse", "Corkscrew Crash", "Twinkle Tackle"
      ) || move.IsMax) {
        desc.MoveBP = move.Bp;
      }

      var bpMods = CalculateBPModsSMSSSV(
        gen,
        attacker,
        defender,
        move,
        field,
        desc,
        basePower,
        hasAteAbilityTypeChange,
        turnOrder,
        hit
      );
      basePower = MechanicsUtil.OF16(Math.Max(1, MechanicsUtil.PokeRound((basePower * MechanicsUtil.ChainMods(bpMods, 41, 2097152)) / 4096.0)));

      if (!string.IsNullOrEmpty(attacker.TeraType) &&
          ((move.Type == attacker.TeraType && attacker.HasType(attacker.TeraType)) ||
          (attacker.TeraType == "Stellar" && move.IsStellarFirstUse)) &&
          move.Hits == 1 && !move.Multiaccuracy &&
          move.Priority <= 0 && move.Bp > 0 &&
          !move.Named("Dragon Energy", "Eruption", "Water Spout") &&
          basePower < 60 && gen.Num >= 9
      ) {
        basePower = 60;
        desc.MoveBP = 60;
      }
      return basePower;
    }

    private static List<int> CalculateBPModsSMSSSV(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      Field field,
      RawDesc desc,
      int basePower,
      bool hasAteAbilityTypeChange,
      string turnOrder,
      int hit
    ) {
      var bpMods = new List<int>();

      var defenderItem = (!string.IsNullOrEmpty(defender.Item) ? defender.Item : defender.DisabledItem) ?? "";
      var resistedKnockOffDamage =
        string.IsNullOrEmpty(defenderItem) ||
        (MechanicsUtil.IsQPActive(defender, field) && defenderItem == "Booster Energy") ||
        (defender.Named("Dialga-Origin") && defenderItem == "Adamant Crystal") ||
        (defender.Named("Palkia-Origin") && defenderItem == "Lustrous Globe") ||
        (defender.Name.Contains("Giratina-Origin") && defenderItem.Contains("Griseous")) ||
        (defender.Name.Contains("Arceus") && defenderItem.Contains("Plate")) ||
        (defender.Name.Contains("Genesect") && defenderItem.Contains("Drive")) ||
        (defender.Named("Groudon", "Groudon-Primal") && defenderItem == "Red Orb") ||
        (defender.Named("Kyogre", "Kyogre-Primal") && defenderItem == "Blue Orb") ||
        (defender.Name.Contains("Silvally") && defenderItem.Contains("Memory")) ||
        defenderItem.Contains(" Z") ||
        (defender.Name.Contains("Zacian") && defenderItem == "Rusted Sword") ||
        (defender.Name.Contains("Zamazenta") && defenderItem == "Rusted Shield") ||
        (defender.Name.Contains("Ogerpon-Cornerstone") && defenderItem == "Cornerstone Mask") ||
        (defender.Name.Contains("Ogerpon-Hearthflame") && defenderItem == "Hearthflame Mask") ||
        (defender.Name.Contains("Ogerpon-Wellspring") && defenderItem == "Wellspring Mask") ||
        (defender.Named("Venomicon-Epilogue") && defenderItem == "Vile Vial");

      if (!resistedKnockOffDamage && !string.IsNullOrEmpty(defenderItem)) {
        var item = gen.Items.Get(Util.ToId(defenderItem)) as Item;
        if (item?.MegaStone != null) {
          if (item.MegaStone.ContainsKey(defender.Name) || new List<string>(item.MegaStone.Values).Contains(defender.Name)) {
            resistedKnockOffDamage = true;
          }
        }
      }

      if (!resistedKnockOffDamage && hit > 1 && !defender.HasAbility("Sticky Hold")) {
        resistedKnockOffDamage = true;
      }

      if ((move.Named("Facade") && attacker.HasStatus("brn", "par", "psn", "tox")) ||
        (move.Named("Brine") && defender.CurHP() <= defender.MaxHP() / 2) ||
        (move.Named("Venoshock") && defender.HasStatus("psn", "tox")) ||
        (move.Named("Lash Out") && (MechanicsUtil.CountBoosts(gen, attacker.Boosts) < 0))
      ) {
        bpMods.Add(8192);
        desc.MoveBP = basePower * 2;
      } else if (
        move.Named("Expanding Force") && MechanicsUtil.IsGrounded(attacker, field) && field.HasTerrain("Psychic")
      ) {
        move.Target = "allAdjacentFoes";
        bpMods.Add(6144);
        desc.MoveBP = (int)Math.Floor(basePower * 1.5);
      } else if ((move.Named("Knock Off") && !resistedKnockOffDamage) ||
        (move.Named("Misty Explosion") && MechanicsUtil.IsGrounded(attacker, field) && field.HasTerrain("Misty")) ||
        (move.Named("Grav Apple") && field.IsGravity)
      ) {
        bpMods.Add(6144);
        desc.MoveBP = (int)Math.Floor(basePower * 1.5);
      } else if (move.Named("Solar Beam", "Solar Blade") &&
          field.HasWeather("Rain", "Heavy Rain", "Sand", "Hail", "Snow")) {
        bpMods.Add(2048);
        desc.MoveBP = basePower / 2;
        desc.Weather = field.Weather;
      } else if (move.Named("Collision Course", "Electro Drift")) {
        var isGhostRevealed =
          attacker.HasAbility("Scrappy") || attacker.HasAbility("Mind's Eye") ||
          field.DefenderSide.IsForesight;
        var isRingTarget =
          defender.HasItem("Ring Target") && !defender.HasAbility("Klutz");
        var types = !string.IsNullOrEmpty(defender.TeraType) && defender.TeraType != "Stellar"
          ? new[] { defender.TeraType }
          : defender.Types;
        var type1Effectiveness = MechanicsUtil.GetMoveEffectiveness(
          gen,
          move,
          types[0],
          isGhostRevealed,
          field.IsGravity,
          isRingTarget
        );
        var type2Effectiveness = types.Length > 1 ? MechanicsUtil.GetMoveEffectiveness(
          gen,
          move,
          types[1],
          isGhostRevealed,
          field.IsGravity,
          isRingTarget
        ) : 1;
        if (type1Effectiveness * type2Effectiveness >= 2) {
          bpMods.Add(5461);
          desc.MoveBP = (int)Math.Floor(basePower * (5461 / 4096.0));
        }
      }

      if (field.AttackerSide.IsHelpingHand) {
        bpMods.Add(6144);
        desc.IsHelpingHand = true;
      }

      var terrainMultiplier = gen.Num > 7 ? 5325 : 6144;
      if (MechanicsUtil.IsGrounded(attacker, field)) {
        if ((field.HasTerrain("Electric") && move.HasType("Electric")) ||
            (field.HasTerrain("Grassy") && move.HasType("Grass")) ||
            (field.HasTerrain("Psychic") && move.HasType("Psychic"))
        ) {
          bpMods.Add(terrainMultiplier);
          desc.Terrain = field.Terrain;
        }
      }
      if (MechanicsUtil.IsGrounded(defender, field)) {
        if ((field.HasTerrain("Misty") && move.HasType("Dragon")) ||
            (field.HasTerrain("Grassy") && move.Named("Bulldoze", "Earthquake"))
        ) {
          bpMods.Add(2048);
          desc.Terrain = field.Terrain;
        }
      }

      if ((attacker.HasAbility("Technician") && basePower <= 60) ||
        (attacker.HasAbility("Flare Boost") &&
          attacker.HasStatus("brn") && move.Category == MoveCategories.Special) ||
        (attacker.HasAbility("Toxic Boost") &&
          attacker.HasStatus("psn", "tox") && move.Category == MoveCategories.Physical) ||
        (attacker.HasAbility("Mega Launcher") && move.Flags.Pulse) ||
        (attacker.HasAbility("Strong Jaw") && move.Flags.Bite) ||
        (attacker.HasAbility("Steely Spirit") && move.HasType("Steel")) ||
        (attacker.HasAbility("Sharpness") && move.Flags.Slicing)
      ) {
        bpMods.Add(6144);
        desc.AttackerAbility = attacker.Ability;
      }

      var aura = $"{move.Type} Aura";
      var isAttackerAura = attacker.HasAbility(aura);
      var isDefenderAura = defender.HasAbility(aura);
      var isUserAuraBreak = attacker.HasAbility("Aura Break") || defender.HasAbility("Aura Break");
      var isFieldAuraBreak = field.IsAuraBreak;
      var isFieldFairyAura = field.IsFairyAura && move.Type == "Fairy";
      var isFieldDarkAura = field.IsDarkAura && move.Type == "Dark";
      var auraActive = isAttackerAura || isDefenderAura || isFieldFairyAura || isFieldDarkAura;
      var auraBreak = isFieldAuraBreak || isUserAuraBreak;
      if (auraActive) {
        if (auraBreak) {
          bpMods.Add(3072);
          desc.AttackerAbility = attacker.Ability;
          desc.DefenderAbility = defender.Ability;
        } else {
          bpMods.Add(5448);
          if (isAttackerAura) desc.AttackerAbility = attacker.Ability;
          if (isDefenderAura) desc.DefenderAbility = defender.Ability;
        }
      }

      if (
        (attacker.HasAbility("Sheer Force") &&
          (move.Secondaries != null || move.Named("Electro Shot", "Order Up")) && !move.IsMax) ||
        (attacker.HasAbility("Sand Force") &&
          field.HasWeather("Sand") && move.HasType("Rock", "Ground", "Steel")) ||
        (attacker.HasAbility("Analytic") &&
          (turnOrder != "first" || field.DefenderSide.IsSwitching == "out")) ||
        (attacker.HasAbility("Tough Claws") && move.Flags.Contact) ||
        (attacker.HasAbility("Punk Rock") && move.Flags.Sound)
      ) {
        bpMods.Add(5325);
        desc.AttackerAbility = attacker.Ability;
      }

      if (field.AttackerSide.IsBattery && move.Category == MoveCategories.Special) {
        bpMods.Add(5325);
        desc.IsBattery = true;
      }

      if (field.AttackerSide.IsPowerSpot) {
        bpMods.Add(5325);
        desc.IsPowerSpot = true;
      }

      if (attacker.HasAbility("Rivalry") && attacker.Gender != "N" && defender.Gender != "N") {
        if (attacker.Gender == defender.Gender) {
          bpMods.Add(5120);
          desc.Rivalry = "buffed";
        } else {
          bpMods.Add(3072);
          desc.Rivalry = "nerfed";
        }
        desc.AttackerAbility = attacker.Ability;
      }

      if (!move.IsMax && hasAteAbilityTypeChange) {
        bpMods.Add(4915);
      }

      if ((attacker.HasAbility("Reckless") && (move.Recoil != null || move.HasCrashDamage)) ||
          (attacker.HasAbility("Iron Fist") && move.Flags.Punch)
      ) {
        bpMods.Add(4915);
        desc.AttackerAbility = attacker.Ability;
      }

      if (gen.Num <= 8 && defender.HasAbility("Heatproof") && move.HasType("Fire")) {
        bpMods.Add(2048);
        desc.DefenderAbility = defender.Ability;
      } else if (defender.HasAbility("Dry Skin") && move.HasType("Fire")) {
        bpMods.Add(5120);
        desc.DefenderAbility = defender.Ability;
      }

      if (attacker.HasAbility("Supreme Overlord") && attacker.AlliesFainted.HasValue) {
        var powMod = new[] { 4096, 4506, 4915, 5325, 5734, 6144 };
        bpMods.Add(powMod[Math.Min(5, attacker.AlliesFainted.Value)]);
        desc.AttackerAbility = attacker.Ability;
        desc.AlliesFainted = attacker.AlliesFainted.Value;
      }

      if (attacker.HasItem($"{move.Type} Gem")) {
        bpMods.Add(5325);
        desc.AttackerItem = attacker.Item;
      } else if (
        (((attacker.HasItem("Adamant Crystal") && attacker.Named("Dialga-Origin")) ||
          (attacker.HasItem("Adamant Orb") && attacker.Named("Dialga"))) &&
         move.HasType("Steel", "Dragon")) ||
        (((attacker.HasItem("Lustrous Orb") &&
         attacker.Named("Palkia")) ||
          (attacker.HasItem("Lustrous Globe") && attacker.Named("Palkia-Origin"))) &&
         move.HasType("Water", "Dragon")) ||
        (((attacker.HasItem("Griseous Orb") || attacker.HasItem("Griseous Core")) &&
         (attacker.Named("Giratina-Origin") || attacker.Named("Giratina"))) &&
         move.HasType("Ghost", "Dragon")) ||
        (attacker.HasItem("Vile Vial") &&
         attacker.Named("Venomicon-Epilogue") &&
         move.HasType("Poison", "Flying")) ||
        (attacker.HasItem("Soul Dew") &&
         attacker.Named("Latios", "Latias", "Latios-Mega", "Latias-Mega") &&
         move.HasType("Psychic", "Dragon")) ||
         (!string.IsNullOrEmpty(attacker.Item) && move.HasType(Items.GetItemBoostType(attacker.Item))) ||
        (attacker.Name.Contains("Ogerpon-Cornerstone") && attacker.HasItem("Cornerstone Mask")) ||
        (attacker.Name.Contains("Ogerpon-Hearthflame") && attacker.HasItem("Hearthflame Mask")) ||
        (attacker.Name.Contains("Ogerpon-Wellspring") && attacker.HasItem("Wellspring Mask"))
      ) {
        bpMods.Add(4915);
        desc.AttackerItem = attacker.Item;
      } else if (
        (attacker.HasItem("Muscle Band") && move.Category == MoveCategories.Physical) ||
        (attacker.HasItem("Wise Glasses") && move.Category == MoveCategories.Special)
      ) {
        bpMods.Add(4505);
        desc.AttackerItem = attacker.Item;
      } else if (attacker.HasItem("Punching Glove") && move.Flags.Punch) {
        bpMods.Add(4506);
      }

      return bpMods;
    }

    private static int CalculateAttackSMSSSV(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      Field field,
      RawDesc desc,
      bool isCritical = false
    ) {
      var attackStat = move.Named("Body Press") ? StatId.Def : move.Category == MoveCategories.Special ? StatId.Spa : StatId.Atk;
      desc.AttackEVs =
        move.Named("Foul Play")
          ? MechanicsUtil.GetStatDescriptionText(gen, defender, attackStat, defender.Nature)
          : MechanicsUtil.GetStatDescriptionText(gen, attacker, attackStat, attacker.Nature);
      var attackSource = move.Named("Foul Play") ? defender : attacker;

      if (field.AttackerSide.IsPowerTrick == true && !move.Named("Foul Play") &&
      move.Category == MoveCategories.Physical) {
        desc.IsPowerTrickAttacker = true;
        attackSource.RawStats[attackStat] = move.Named("Body Press")
          ? attacker.RawStats.Atk : attacker.RawStats.Def;
      }

      int attack;
      if (attackSource.Boosts[attackStat] == 0 ||
          (isCritical && attackSource.Boosts[attackStat] < 0)) {
        attack = attackSource.RawStats[attackStat];
      } else if (defender.HasAbility("Unaware")) {
        attack = attackSource.RawStats[attackStat];
        desc.DefenderAbility = defender.Ability;
      } else {
        attack = MechanicsUtil.GetModifiedStat(attackSource.RawStats[attackStat], attackSource.Boosts[attackStat]);
        desc.AttackBoost = attackSource.Boosts[attackStat];
      }

      if (attacker.HasAbility("Hustle") && move.Category == MoveCategories.Physical) {
        attack = MechanicsUtil.PokeRound((attack * 3.0) / 2.0);
        desc.AttackerAbility = attacker.Ability;
      }
      var atMods = CalculateAtModsSMSSSV(gen, attacker, defender, move, field, desc);
      attack = MechanicsUtil.OF16(Math.Max(1, MechanicsUtil.PokeRound((attack * MechanicsUtil.ChainMods(atMods, 410, 131072)) / 4096.0)));
      return attack;
    }

    private static List<int> CalculateAtModsSMSSSV(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      Field field,
      RawDesc desc
    ) {
      var atMods = new List<int>();

      if ((attacker.HasAbility("Slow Start") && attacker.AbilityOn &&
          (move.Category == MoveCategories.Physical || (move.Category == MoveCategories.Special && move.IsZ))) ||
        (attacker.HasAbility("Defeatist") && attacker.CurHP() <= attacker.MaxHP() / 2)
      ) {
        atMods.Add(2048);
        desc.AttackerAbility = attacker.Ability;
      } else if (
        (attacker.HasAbility("Solar Power") &&
         field.HasWeather("Sun", "Harsh Sunshine") &&
         move.Category == MoveCategories.Special) ||
        (attacker.Named("Cherrim") &&
         attacker.HasAbility("Flower Gift") &&
         field.HasWeather("Sun", "Harsh Sunshine") &&
         move.Category == MoveCategories.Physical)) {
        atMods.Add(6144);
        desc.AttackerAbility = attacker.Ability;
        desc.Weather = field.Weather;
      } else if (
        (attacker.HasAbility("Gorilla Tactics") && move.Category == MoveCategories.Physical &&
         !attacker.IsDynamaxed)) {
        atMods.Add(6144);
        desc.AttackerAbility = attacker.Ability;
      } else if (
        (attacker.HasAbility("Guts") && !string.IsNullOrEmpty(attacker.Status) && move.Category == MoveCategories.Physical) ||
        (attacker.CurHP() <= attacker.MaxHP() / 3 &&
          ((attacker.HasAbility("Overgrow") && move.HasType("Grass")) ||
           (attacker.HasAbility("Blaze") && move.HasType("Fire")) ||
           (attacker.HasAbility("Torrent") && move.HasType("Water")) ||
           (attacker.HasAbility("Swarm") && move.HasType("Bug")))) ||
        (move.Category == MoveCategories.Special && attacker.AbilityOn && attacker.HasAbility("Plus", "Minus"))
      ) {
        atMods.Add(6144);
        desc.AttackerAbility = attacker.Ability;
      } else if (attacker.HasAbility("Flash Fire") && attacker.AbilityOn && move.HasType("Fire")) {
        atMods.Add(6144);
        desc.AttackerAbility = "Flash Fire";
      } else if (
        (attacker.HasAbility("Steelworker") && move.HasType("Steel")) ||
        (attacker.HasAbility("Dragon's Maw") && move.HasType("Dragon")) ||
        (attacker.HasAbility("Rocky Payload") && move.HasType("Rock"))
      ) {
        atMods.Add(6144);
        desc.AttackerAbility = attacker.Ability;
      } else if (attacker.HasAbility("Transistor") && move.HasType("Electric")) {
        atMods.Add(gen.Num >= 9 ? 5325 : 6144);
        desc.AttackerAbility = attacker.Ability;
      } else if (attacker.HasAbility("Stakeout") && attacker.AbilityOn) {
        atMods.Add(8192);
        desc.AttackerAbility = attacker.Ability;
      } else if (
        (attacker.HasAbility("Water Bubble") && move.HasType("Water")) ||
        (attacker.HasAbility("Huge Power", "Pure Power") && move.Category == MoveCategories.Physical)
      ) {
        atMods.Add(8192);
        desc.AttackerAbility = attacker.Ability;
      }

      if (
        field.AttackerSide.IsFlowerGift &&
        !attacker.HasAbility("Flower Gift") &&
        field.HasWeather("Sun", "Harsh Sunshine") &&
        move.Category == MoveCategories.Physical) {
        atMods.Add(6144);
        desc.Weather = field.Weather;
        desc.IsFlowerGiftAttacker = true;
      }

      if (field.AttackerSide.IsSteelySpirit && move.HasType("Steel")) {
        atMods.Add(6144);
        desc.IsSteelySpiritAttacker = true;
      }

      if ((defender.HasAbility("Thick Fat") && move.HasType("Fire", "Ice")) ||
          (defender.HasAbility("Water Bubble") && move.HasType("Fire")) ||
         (defender.HasAbility("Purifying Salt") && move.HasType("Ghost"))) {
        atMods.Add(2048);
        desc.DefenderAbility = defender.Ability;
      }

      if (gen.Num >= 9 && defender.HasAbility("Heatproof") && move.HasType("Fire")) {
        atMods.Add(2048);
        desc.DefenderAbility = defender.Ability;
      }

      var isTabletsOfRuinActive = (defender.HasAbility("Tablets of Ruin") || field.IsTabletsOfRuin) &&
        !attacker.HasAbility("Tablets of Ruin");
      var isVesselOfRuinActive = (defender.HasAbility("Vessel of Ruin") || field.IsVesselOfRuin) &&
        !attacker.HasAbility("Vessel of Ruin");
      if (
        (isTabletsOfRuinActive && move.Category == MoveCategories.Physical) ||
        (isVesselOfRuinActive && move.Category == MoveCategories.Special)
      ) {
        if (defender.HasAbility("Tablets of Ruin") || defender.HasAbility("Vessel of Ruin")) {
          desc.DefenderAbility = defender.Ability;
        } else {
          if (move.Category == MoveCategories.Special) desc.IsVesselOfRuin = true;
          else desc.IsTabletsOfRuin = true;
        }
        atMods.Add(3072);
      }

      if (MechanicsUtil.IsQPActive(attacker, field)) {
        if (
          (move.Category == MoveCategories.Physical && MechanicsUtil.GetQPBoostedStat(attacker) == StatId.Atk) ||
          (move.Category == MoveCategories.Special && MechanicsUtil.GetQPBoostedStat(attacker) == StatId.Spa)
        ) {
          atMods.Add(5325);
          desc.AttackerAbility = attacker.Ability;
        }
      }

      if (
        (attacker.HasAbility("Hadron Engine") && move.Category == MoveCategories.Special &&
          field.HasTerrain("Electric")) ||
        (attacker.HasAbility("Orichalcum Pulse") && move.Category == MoveCategories.Physical &&
          field.HasWeather("Sun", "Harsh Sunshine") && !attacker.HasItem("Utility Umbrella"))
      ) {
        atMods.Add(5461);
        desc.AttackerAbility = attacker.Ability;
      }

      if ((attacker.HasItem("Thick Club") &&
           attacker.Named("Cubone", "Marowak", "Marowak-Alola", "Marowak-Alola-Totem") &&
           move.Category == MoveCategories.Physical) ||
          (attacker.HasItem("Deep Sea Tooth") &&
           attacker.Named("Clamperl") &&
           move.Category == MoveCategories.Special) ||
          (attacker.HasItem("Light Ball") && attacker.Name.Contains("Pikachu") && !move.IsZ)
      ) {
        atMods.Add(8192);
        desc.AttackerItem = attacker.Item;
      } else if (!move.IsZ && !move.IsMax &&
        ((attacker.HasItem("Choice Band") && move.Category == MoveCategories.Physical) ||
          (attacker.HasItem("Choice Specs") && move.Category == MoveCategories.Special))
      ) {
        atMods.Add(6144);
        desc.AttackerItem = attacker.Item;
      }
      return atMods;
    }

    private static int CalculateDefenseSMSSSV(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      Field field,
      RawDesc desc,
      bool isCritical = false
    ) {
      var hitsPhysical = move.OverrideDefensiveStat == StatId.Def || move.Category == MoveCategories.Physical;
      var defenseStat = hitsPhysical ? StatId.Def : StatId.Spd;
      var boosts = defender.Boosts[field.IsWonderRoom ? (hitsPhysical ? StatId.Spd : StatId.Def) : defenseStat];
      desc.DefenseEVs = MechanicsUtil.GetStatDescriptionText(gen, defender, defenseStat, defender.Nature);

      if (field.DefenderSide.IsPowerTrick == true && hitsPhysical) {
        desc.IsPowerTrickDefender = true;
        defender.RawStats[defenseStat] = defender.RawStats.Atk;
      }

      int defense;
      if (boosts == 0 ||
          (isCritical && boosts > 0) ||
          move.IgnoreDefensive) {
        defense = defender.RawStats[defenseStat];
      } else if (attacker.HasAbility("Unaware") || move.Name == "Nihil Light") {
        defense = defender.RawStats[defenseStat];
        desc.AttackerAbility = attacker.Ability;
      } else {
        defense = MechanicsUtil.GetModifiedStat(defender.RawStats[defenseStat], boosts);
        desc.DefenseBoost = boosts;
      }

      if (field.HasWeather("Sand") && defender.HasType("Rock") && !hitsPhysical) {
        defense = MechanicsUtil.PokeRound((defense * 3.0) / 2.0);
        desc.Weather = field.Weather;
      }
      if (field.HasWeather("Snow") && defender.HasType("Ice") && hitsPhysical) {
        defense = MechanicsUtil.PokeRound((defense * 3.0) / 2.0);
        desc.Weather = field.Weather;
      }

      var dfMods = CalculateDfModsSMSSSV(
        gen,
        attacker,
        defender,
        move,
        field,
        desc,
        isCritical,
        hitsPhysical
      );

      return MechanicsUtil.OF16(Math.Max(1, MechanicsUtil.PokeRound((defense * MechanicsUtil.ChainMods(dfMods, 410, 131072)) / 4096.0)));
    }

    private static List<int> CalculateDfModsSMSSSV(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      Field field,
      RawDesc desc,
      bool isCritical = false,
      bool hitsPhysical = false
    ) {
      var dfMods = new List<int>();
      if (defender.HasAbility("Marvel Scale") && !string.IsNullOrEmpty(defender.Status) && hitsPhysical) {
        dfMods.Add(6144);
        desc.DefenderAbility = defender.Ability;
      } else if (
        defender.Named("Cherrim") &&
        defender.HasAbility("Flower Gift") &&
        field.HasWeather("Sun", "Harsh Sunshine") &&
        !hitsPhysical
      ) {
        dfMods.Add(6144);
        desc.DefenderAbility = defender.Ability;
        desc.Weather = field.Weather;
      } else if (
        field.DefenderSide.IsFlowerGift &&
        field.HasWeather("Sun", "Harsh Sunshine") &&
        !hitsPhysical) {
        dfMods.Add(6144);
        desc.Weather = field.Weather;
        desc.IsFlowerGiftDefender = true;
      } else if (
        defender.HasAbility("Grass Pelt") &&
        field.HasTerrain("Grassy") &&
        hitsPhysical
      ) {
        dfMods.Add(6144);
        desc.DefenderAbility = defender.Ability;
      } else if (defender.HasAbility("Fur Coat") && hitsPhysical) {
        dfMods.Add(8192);
        desc.DefenderAbility = defender.Ability;
      }

      var isSwordOfRuinActive = (attacker.HasAbility("Sword of Ruin") || field.IsSwordOfRuin) &&
        !defender.HasAbility("Sword of Ruin");
      var isBeadsOfRuinActive = (attacker.HasAbility("Beads of Ruin") || field.IsBeadsOfRuin) &&
        !defender.HasAbility("Beads of Ruin");
      if (
        (isSwordOfRuinActive && hitsPhysical) ||
        (isBeadsOfRuinActive && !hitsPhysical)
      ) {
        if (attacker.HasAbility("Sword of Ruin") || attacker.HasAbility("Beads of Ruin")) {
          desc.AttackerAbility = attacker.Ability;
        } else {
          if (hitsPhysical) desc.IsSwordOfRuin = true;
          else desc.IsBeadsOfRuin = true;
        }
        dfMods.Add(3072);
      }

      if (MechanicsUtil.IsQPActive(defender, field)) {
        if (
          (hitsPhysical && MechanicsUtil.GetQPBoostedStat(defender) == StatId.Def) ||
          (!hitsPhysical && MechanicsUtil.GetQPBoostedStat(defender) == StatId.Spd)
        ) {
          desc.DefenderAbility = defender.Ability;
          dfMods.Add(5324);
        }
      }

      var species = gen.Species.Get(Util.ToId(defender.Name)) as Specie;
      if ((defender.HasItem("Eviolite") &&
          (defender.Name == "Dipplin" || species?.Nfe == true)) ||
          (!hitsPhysical && defender.HasItem("Assault Vest"))) {
        dfMods.Add(6144);
        desc.DefenderItem = defender.Item;
      } else if (
        (defender.HasItem("Metal Powder") && defender.Named("Ditto") && hitsPhysical) ||
        (defender.HasItem("Deep Sea Scale") && defender.Named("Clamperl") && !hitsPhysical)
      ) {
        dfMods.Add(8192);
        desc.DefenderItem = defender.Item;
      }
      return dfMods;
    }

    private static int CalculateBaseDamageSMSSSV(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      int basePower,
      int attack,
      int defense,
      Move move,
      Field field,
      RawDesc desc,
      bool isCritical = false
    ) {
      var baseDamage = MechanicsUtil.GetBaseDamage(attacker.Level, basePower, attack, defense);
      var isSpread = field.GameType != GameTypes.Singles &&
         (move.Target == "allAdjacent" || move.Target == "allAdjacentFoes");
      if (isSpread) {
        baseDamage = MechanicsUtil.PokeRound(MechanicsUtil.OF32(baseDamage * 3072) / 4096.0);
      }

      if (attacker.HasAbility("Parental Bond (Child)")) {
        baseDamage = MechanicsUtil.PokeRound(MechanicsUtil.OF32(baseDamage * 1024) / 4096.0);
      }

      if (
        field.HasWeather("Sun") && move.Named("Hydro Steam") && !attacker.HasItem("Utility Umbrella")
      ) {
        baseDamage = MechanicsUtil.PokeRound(MechanicsUtil.OF32(baseDamage * 6144) / 4096.0);
        desc.Weather = field.Weather;
      } else if (!defender.HasItem("Utility Umbrella")) {
        if (
          (field.HasWeather("Sun", "Harsh Sunshine") && move.HasType("Fire")) ||
          (field.HasWeather("Rain", "Heavy Rain") && move.HasType("Water"))
        ) {
          baseDamage = MechanicsUtil.PokeRound(MechanicsUtil.OF32(baseDamage * 6144) / 4096.0);
          desc.Weather = field.Weather;
        } else if (
          (field.HasWeather("Sun") && move.HasType("Water")) ||
          (field.HasWeather("Rain") && move.HasType("Fire"))
        ) {
          baseDamage = MechanicsUtil.PokeRound(MechanicsUtil.OF32(baseDamage * 2048) / 4096.0);
          desc.Weather = field.Weather;
        }
      }

      if (isCritical) {
        baseDamage = (int)Math.Floor((double)MechanicsUtil.OF32(baseDamage * 1.5));
        desc.IsCritical = isCritical;
      }

      return baseDamage;
    }

    private static List<int> CalculateFinalModsSMSSSV(
      IGeneration gen,
      Pokemon attacker,
      Pokemon defender,
      Move move,
      Field field,
      RawDesc desc,
      bool isCritical = false,
      double typeEffectiveness = 1,
      int hitCount = 0
    ) {
      var finalMods = new List<int>();

      if (field.DefenderSide.IsReflect && move.Category == MoveCategories.Physical &&
          !isCritical && !field.DefenderSide.IsAuroraVeil) {
        finalMods.Add(field.GameType != GameTypes.Singles ? 2732 : 2048);
        desc.IsReflect = true;
      } else if (
        field.DefenderSide.IsLightScreen && move.Category == MoveCategories.Special &&
        !isCritical && !field.DefenderSide.IsAuroraVeil
      ) {
        finalMods.Add(field.GameType != GameTypes.Singles ? 2732 : 2048);
        desc.IsLightScreen = true;
      }
      if (field.DefenderSide.IsAuroraVeil && !isCritical) {
        finalMods.Add(field.GameType != GameTypes.Singles ? 2732 : 2048);
        desc.IsAuroraVeil = true;
      }

      if (attacker.HasAbility("Neuroforce") && typeEffectiveness > 1) {
        finalMods.Add(5120);
        desc.AttackerAbility = attacker.Ability;
      } else if (attacker.HasAbility("Sniper") && isCritical) {
        finalMods.Add(6144);
        desc.AttackerAbility = attacker.Ability;
      } else if (attacker.HasAbility("Tinted Lens") && typeEffectiveness < 1) {
        finalMods.Add(8192);
        desc.AttackerAbility = attacker.Ability;
      }

      if (defender.IsDynamaxed && move.Named("Dynamax Cannon", "Behemoth Blade", "Behemoth Bash")) {
        finalMods.Add(8192);
      }

      if (defender.HasAbility("Multiscale", "Shadow Shield") &&
          defender.CurHP() == defender.MaxHP() &&
          hitCount == 0 &&
          (!field.DefenderSide.IsSR && (field.DefenderSide.Spikes == 0 || defender.HasType("Flying")) ||
          defender.HasItem("Heavy-Duty Boots")) && !attacker.HasAbility("Parental Bond (Child)")
      ) {
        finalMods.Add(2048);
        desc.DefenderAbility = defender.Ability;
      }

      if (defender.HasAbility("Fluffy") && move.Flags.Contact && !attacker.HasAbility("Long Reach")) {
        finalMods.Add(2048);
        desc.DefenderAbility = defender.Ability;
      } else if (
        (defender.HasAbility("Punk Rock") && move.Flags.Sound) ||
        (defender.HasAbility("Ice Scales") && move.Category == MoveCategories.Special)
      ) {
        finalMods.Add(2048);
        desc.DefenderAbility = defender.Ability;
      }

      if (defender.HasAbility("Solid Rock", "Filter", "Prism Armor") && typeEffectiveness > 1) {
        finalMods.Add(3072);
        desc.DefenderAbility = defender.Ability;
      }

      if (field.DefenderSide.IsFriendGuard) {
        finalMods.Add(3072);
        desc.IsFriendGuard = true;
      }

      if (defender.HasAbility("Fluffy") && move.HasType("Fire")) {
        finalMods.Add(8192);
        desc.DefenderAbility = defender.Ability;
      }

      if (attacker.HasItem("Expert Belt") && typeEffectiveness > 1 && !move.IsZ) {
        finalMods.Add(4915);
        desc.AttackerItem = attacker.Item;
      } else if (attacker.HasItem("Life Orb")) {
        finalMods.Add(5324);
        desc.AttackerItem = attacker.Item;
      } else if (attacker.HasItem("Metronome") && move.TimesUsedWithMetronome.GetValueOrDefault() >= 1) {
        var timesUsedWithMetronome = (int)Math.Floor((double)move.TimesUsedWithMetronome.GetValueOrDefault());
        if (timesUsedWithMetronome <= 4) {
          finalMods.Add(4096 + timesUsedWithMetronome * 819);
        } else {
          finalMods.Add(8192);
        }
        desc.AttackerItem = attacker.Item;
      }

      if (move.HasType(Items.GetBerryResistType(defender.Item)) &&
          (typeEffectiveness > 1 || move.HasType("Normal")) &&
          hitCount == 0 &&
          !attacker.HasAbility("Unnerve", "As One (Glastrier)", "As One (Spectrier)")) {
        if (defender.HasAbility("Ripen")) {
          finalMods.Add(1024);
        } else {
          finalMods.Add(2048);
        }
        desc.DefenderItem = defender.Item;
      }

      return finalMods;
    }

    private static bool HasTerrainSeed(Pokemon pokemon) {
      return pokemon.HasItem("Electric Seed", "Misty Seed", "Grassy Seed", "Psychic Seed");
    }
  }
}
