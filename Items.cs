using System.Collections.Generic;
using DamageCalc.Data;

namespace DamageCalc {
  public static class Items {
    public static readonly Dictionary<string, StatId> SeedBoostedStat = new Dictionary<string, StatId> {
      { "Electric Seed", StatId.Def },
      { "Grassy Seed", StatId.Def },
      { "Misty Seed", StatId.Spd },
      { "Psychic Seed", StatId.Spd },
    };

    public static string? GetItemBoostType(string? item) {
      switch (item) {
        case "Draco Plate":
        case "Dragon Fang":
          return "Dragon";
        case "Dread Plate":
        case "Black Glasses":
          return "Dark";
        case "Earth Plate":
        case "Soft Sand":
          return "Ground";
        case "Fist Plate":
        case "Black Belt":
          return "Fighting";
        case "Flame Plate":
        case "Charcoal":
          return "Fire";
        case "Icicle Plate":
        case "Never-Melt Ice":
          return "Ice";
        case "Insect Plate":
        case "Silver Powder":
          return "Bug";
        case "Iron Plate":
        case "Metal Coat":
          return "Steel";
        case "Meadow Plate":
        case "Rose Incense":
        case "Miracle Seed":
          return "Grass";
        case "Mind Plate":
        case "Odd Incense":
        case "Twisted Spoon":
          return "Psychic";
        case "Fairy Feather":
        case "Pixie Plate":
          return "Fairy";
        case "Sky Plate":
        case "Sharp Beak":
          return "Flying";
        case "Splash Plate":
        case "Sea Incense":
        case "Wave Incense":
        case "Mystic Water":
          return "Water";
        case "Spooky Plate":
        case "Spell Tag":
          return "Ghost";
        case "Stone Plate":
        case "Rock Incense":
        case "Hard Stone":
          return "Rock";
        case "Toxic Plate":
        case "Poison Barb":
          return "Poison";
        case "Zap Plate":
        case "Magnet":
          return "Electric";
        case "Silk Scarf":
        case "Pink Bow":
        case "Polkadot Bow":
          return "Normal";
        default:
          return null;
      }
    }

    public static string? GetBerryResistType(string? berry) {
      switch (berry) {
        case "Chilan Berry": return "Normal";
        case "Occa Berry": return "Fire";
        case "Passho Berry": return "Water";
        case "Wacan Berry": return "Electric";
        case "Rindo Berry": return "Grass";
        case "Yache Berry": return "Ice";
        case "Chople Berry": return "Fighting";
        case "Kebia Berry": return "Poison";
        case "Shuca Berry": return "Ground";
        case "Coba Berry": return "Flying";
        case "Payapa Berry": return "Psychic";
        case "Tanga Berry": return "Bug";
        case "Charti Berry": return "Rock";
        case "Kasib Berry": return "Ghost";
        case "Haban Berry": return "Dragon";
        case "Colbur Berry": return "Dark";
        case "Babiri Berry": return "Steel";
        case "Roseli Berry": return "Fairy";
        default: return null;
      }
    }

    private static readonly HashSet<string> FLING_120 = new HashSet<string> {
      "TR24", "TR28", "TR34", "TR39", "TR53", "TR55", "TR64", "TR66", "TR72", "TR73",
    };

    private static readonly HashSet<string> FLING_100 = new HashSet<string> {
      "Hard Stone", "Room Service", "Claw Fossil", "Dome Fossil", "Helix Fossil", "Old Amber",
      "Root Fossil", "Armor Fossil", "Old Amber", "Fossilized Bird", "Fossilized Dino",
      "Fossilized Drake", "Fossilized Fish", "Plume Fossil", "Jaw Fossil", "Cover Fossil",
      "Sail Fossil", "Rare Bone", "Skull Fossil", "TR10", "TR31", "TR75",
    };

    private static readonly HashSet<string> FLING_90 = new HashSet<string> {
      "Deep Sea Tooth", "Thick Club", "TR02", "TR04", "TR05", "TR08", "TR11", "TR22",
      "TR35", "TR42", "TR45", "TR50", "TR61", "TR65", "TR67", "TR86", "TR90", "TR96",
    };

    private static readonly HashSet<string> FLING_85 = new HashSet<string> { "TR01", "TR41", "TR62", "TR93", "TR97", "TR98" };

    private static readonly HashSet<string> FLING_80 = new HashSet<string> {
      "Assault Vest", "Blunder Policy", "Chipped Pot", "Cracked Pot", "Heavy-Duty Boots",
      "Weakness Policy", "Quick Claw", "Dawn Stone", "Dusk Stone", "Electirizer", "Magmarizer",
      "Oval Stone", "Protector", "Sachet", "Whipped Dream", "Razor Claw", "Shiny Stone",
      "TR16", "TR18", "TR19", "TR25", "TR32", "TR33", "TR47", "TR56", "TR57", "TR58",
      "TR59", "TR60", "TR63", "TR69", "TR70", "TR74", "TR84", "TR87", "TR92", "TR95",
      "TR99",
    };

    private static readonly HashSet<string> FLING_70 = new HashSet<string> {
      "Poison Barb", "Dragon Fang", "Power Anklet", "Power Band", "Power Belt", "Power Bracer",
      "Power Lens", "Power Weight",
    };

    private static readonly HashSet<string> FLING_60 = new HashSet<string> {
      "Adamant Orb", "Damp Rock", "Heat Rock", "Leek", "Lustrous Orb", "Macho Brace",
      "Rocky Helmet", "Stick", "Utility Umbrella", "Terrain Extender",
    };

    private static readonly HashSet<string> FLING_30 = new HashSet<string> {
      "Absorb Bulb", "Black Belt", "Black Sludge", "Black Glasses", "Cell Battery", "Charcoal",
      "Deep Sea Scale", "Flame Orb", "King's Rock", "Life Orb", "Light Ball", "Light Clay",
      "Magnet", "Metal Coat", "Miracle Seed", "Mystic Water", "Never-Melt Ice", "Razor Fang",
      "Scope Lens", "Soul Dew", "Spell Tag", "Sweet Apple", "Tart Apple", "Throat Spray",
      "Toxic Orb", "Twisted Spoon", "Dragon Scale", "Energy Powder", "Fire Stone", "Leaf Stone",
      "Moon Stone", "Sun Stone", "Thunder Stone", "Up-Grade", "Water Stone", "Berry Juice",
      "Black Sludge", "Prism Scale", "Ice Stone", "Gold Bottle Cap", "Luminous Moss",
      "Eject Button", "Snowball", "Bottle Cap",
    };

    private static readonly HashSet<string> FLING_10 = new HashSet<string> {
      "Air Balloon", "Berry Sweet", "Choice Band", "Choice Scarf", "Choice Specs", "Clover Sweet",
      "Destiny Knot", "Electric Seed", "Expert Belt", "Flower Sweet", "Focus Band", "Focus Sash",
      "Full Incense", "Grassy Seed", "Lagging Tail", "Lax Incense", "Leftovers", "Love Sweet",
      "Mental Herb", "Metal Powder", "Mint Berry", "Miracle Berry", "Misty Seed", "Muscle Band",
      "Power Herb", "Psychic Seed", "Odd Incense", "Quick Powder", "Reaper Cloth", "Red Card",
      "Ribbon Sweet", "Ring Target", "Rock Incense", "Rose Incense", "Sea Incense", "Shed Shell",
      "Silk Scarf", "Silver Powder", "Smooth Rock", "Soft Sand", "Soothe Bell", "Star Sweet",
      "Strawberry Sweet", "Wave Incense", "White Herb", "Wide Lens", "Wise Glasses", "Zoom Lens",
      "Silver Powder", "Power Herb", "TR00", "TR07", "TR12", "TR13", "TR14", "TR17", "TR20",
      "TR21", "TR23", "TR26", "TR27", "TR29", "TR30", "TR37", "TR38", "TR40", "TR44",
      "TR46", "TR48", "TR49", "TR51", "TR52", "TR54", "TR68", "TR76", "TR77", "TR79",
      "TR80", "TR83", "TR85", "TR88", "TR91",
    };

    public static int GetFlingPower(string? item, int gen = 9) {
      if (string.IsNullOrEmpty(item)) return 0;
      if (item == "Big Nugget" && gen <= 7) return 30;
      if (item == "Big Nugget" || item == "Iron Ball" || item == "TR43" || item == "TR71") return 130;
      if (FLING_120.Contains(item)) return 85;
      if (item == "TR03" || item == "TR06" || item == "TR09" || item == "TR15" || item == "TR89") return 110;
      if (FLING_100.Contains(item)) return 100;
      if (item == "TR36" || item == "TR78" || item == "TR81" || item == "TR94") return 95;
      if (item.Contains("Plate") || FLING_90.Contains(item)) return 90;
      if (FLING_85.Contains(item)) return 85;
      if (FLING_80.Contains(item)) return 80;
      if (FLING_70.Contains(item)) return 70;
      if (FLING_60.Contains(item)) return 60;
      if (item == "Eject Pack" || item == "Sharp Beak" || item == "Dubious Disc") return 50;
      if (item == "Icy Rock" || item == "Eviolite" || item == "Lucky Punch") return 40;
      if (FLING_30.Contains(item)) return 30;
      if (item == "TR82" || item == "Pretty Feather") return 20;
      if (item.Contains("Berry") || FLING_10.Contains(item)) return 10;
      return 0;
    }

    public static (string type, int power) GetNaturalGift(IGeneration gen, string item) {
      var gift = gen.Items.Get(Util.ToId(item))?.NaturalGift;
      if (gift != null) return (gift.Type, gift.BasePower);
      return ("Normal", 1);
    }

    public static string? GetTechnoBlast(string item) {
      switch (item) {
        case "Burn Drive": return "Fire";
        case "Chill Drive": return "Ice";
        case "Douse Drive": return "Water";
        case "Shock Drive": return "Electric";
        default: return null;
      }
    }

    public static string? GetMultiAttack(string item) {
      if (item.Contains("Memory")) {
        return item.Substring(0, item.IndexOf(' '));
      }
      return null;
    }
  }
}
