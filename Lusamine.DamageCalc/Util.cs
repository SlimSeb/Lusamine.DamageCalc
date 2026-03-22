using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Lusamine.DamageCalc {
  public static class Util {
    private static readonly Regex IdRegex = new Regex("[^a-z0-9]+", RegexOptions.Compiled);

    public static string ToId(object? text) {
      var lcase = ("" + text).ToLowerInvariant();
      if (lcase == "flabébé") return "flabebe";
      return IdRegex.Replace(lcase, "");
    }

    public static Dictionary<string, object?> Extend(
      bool deep,
      Dictionary<string, object?> target,
      params Dictionary<string, object?>[] sources
    ) {
      foreach (var source in sources) {
        MergeInto(target, source, deep);
      }
      return target;
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

    private static void MergeInto(Dictionary<string, object?> target, Dictionary<string, object?> source, bool deep) {
      foreach (var kvp in source) {
        if (deep && kvp.Value is Dictionary<string, object?> sourceDict) {
          if (target.TryGetValue(kvp.Key, out var existing) && existing is Dictionary<string, object?> existingDict) {
            target[kvp.Key] = Extend(true, new Dictionary<string, object?>(existingDict), sourceDict);
          } else {
            target[kvp.Key] = Extend(true, new Dictionary<string, object?>(), sourceDict);
          }
        } else {
          target[kvp.Key] = kvp.Value;
        }
      }
    }
  }
}
