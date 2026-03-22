using System;
using System.Collections.Generic;

namespace Lusamine.DamageCalc.Data {
  public static class DataIndex {
    public static IGeneration Create(
      int gen,
      IDataTable<IMove> moves,
      IDataTable<ISpecie> species
    ) {
      if (gen < 1 || gen > 9) throw new ArgumentOutOfRangeException(nameof(gen));
      return new Generation(
        gen,
        JsonDataLoader.GetAbilitiesTable(gen),
        JsonDataLoader.GetItemsTable(gen),
        moves,
        species,
        JsonDataLoader.GetTypesTable(gen),
        JsonDataLoader.GetNaturesTable()
      );
    }

    public static IGeneration Create(int gen) {
      return Create(gen, JsonDataLoader.GetMovesTable(gen), JsonDataLoader.GetSpeciesTable(gen));
    }
  }
}
