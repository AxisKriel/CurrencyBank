using System;
using System.IO;
using Newtonsoft.Json;

namespace CurrencyBank
{
	public class Config
	{
		public bool UseSEconomy = false;

		public bool UseShortName = false;

		public string CurrencyNameShort = "c";

		public string CurrencyName = "coin";

		public string CurrencyNamePlural = "coins";

		public float SEconomyConversionRate = 1.0f;

		public string StorageType = "sqlite";

		public string MySqlHost = "localhost:3306";

		public string MySqlDbName = "";

		public string MySqlUsername = "";

		public string MySqlPassword = "";

		public static Config Read(string path)
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				if (!File.Exists(path))
				{
					Config config = new Config();
					File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
					return config;
				}
				else
					return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
			}
			catch (Exception)
			{
				return new Config();
			}
		}
	}
}
