using System.Collections.Generic;

namespace YanK
{
	public static class LocalizationParser
	{
		public static Dictionary<string, string> Parse(string json)
		{
			var dict = new Dictionary<string, string>();
			json = json.Trim();
			if (json.StartsWith("{")) json = json.Substring(1);
			if (json.EndsWith("}")) json = json.Substring(0, json.Length - 1);

			foreach (var entry in json.Split('\n'))
			{
				var line = entry.Trim().TrimEnd(',');
				if (string.IsNullOrEmpty(line)) continue;
				int colonIdx = line.IndexOf(':');
				if (colonIdx < 0) continue;
				string key = line.Substring(0, colonIdx).Trim().Trim('"');
				string val = line.Substring(colonIdx + 1).Trim().Trim('"');
				if (!string.IsNullOrEmpty(key))
					dict[key] = val;
			}
			return dict;
		}
	}
}
