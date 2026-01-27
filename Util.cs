using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DamageCalc {
  public static class Util {
    private static readonly Regex IdRegex = new Regex("[^a-z0-9]+", RegexOptions.Compiled);

    public static string ToId(object? text) {
      var lcase = ("" + text).ToLowerInvariant();
      if (lcase == "flabébé") return "flabebe";
      return IdRegex.Replace(lcase, "");
    }

    public static void Error(bool err, string msg) {
      if (err) {
        throw new InvalidOperationException(msg);
      }
      Console.WriteLine(msg);
    }

    public static void AssignWithout(Dictionary<string, int> target, Dictionary<string, int> source, HashSet<string> exclude) {
      foreach (var kvp in source) {
        if (!exclude.Contains(kvp.Key)) target[kvp.Key] = kvp.Value;
      }
    }
  }
}
