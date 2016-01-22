using System;
using System.IO;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;

namespace Zeghs.Settings {
	public sealed class GlobalSettings {
		private static GlobalSettings __cSettings = null;
		
		public static GlobalSettings Settings {
			get {
				return __cSettings;
			}
		}

		public static void Load() {
			string sLocation = Assembly.GetExecutingAssembly().Location;
			string sPath = Path.GetDirectoryName(sLocation);
			string sTargetName = Path.GetFileNameWithoutExtension(sLocation) + ".set";
			string sFileName = Path.Combine(sPath, sTargetName);

			string sSettings = File.ReadAllText(sFileName, Encoding.UTF8);

			__cSettings = JsonConvert.DeserializeObject<GlobalSettings>(sSettings);
		}

		public static void Save() {
			string sLocation = Assembly.GetExecutingAssembly().Location;
			string sPath = Path.GetDirectoryName(sLocation);
			string sTargetName = Path.GetFileNameWithoutExtension(sLocation) + ".set";
			string sFileName = Path.Combine(sPath, sTargetName);

			string sSettings = JsonConvert.SerializeObject(__cSettings, Formatting.Indented);
			File.WriteAllText(sFileName, sSettings, Encoding.UTF8);
		}

		/// <summary>
		///   [取得/設定] 股票資訊路徑
		/// </summary>
		public string DataPath {
			get;
			set;
		}

		/// <summary>
		///   [取得/設定] 7Zip安裝路徑
		/// </summary>
		public string SevenZipPath {
			get;
			set;
		}
	}
}