using System;
using System.Collections.Generic;

namespace DamageCalc.Data {
  public static class DataIndex {
    public static IGeneration Create(
      int gen,
      IDataTable<IMove> moves,
      IDataTable<ISpecie> species
    ) {
      if (gen < 1 || gen > 9) throw new ArgumentOutOfRangeException(nameof(gen));
      return new Generation(
        gen,
        AbilitiesData.GetTable(gen),
        ItemsData.GetTable(gen),
        moves,
        species,
        TypesData.GetTable(gen),
        NaturesData.Table
      );
    }

    public static IGeneration Create(int gen) {
      return Create(gen, MovesData.GetTable(gen), SpeciesData.GetTable(gen));
    }
  }
}
