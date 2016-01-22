using System;
using System.IO;
using System.Text;
using System.IO.Compression;
using System.Collections.Generic;
using log4net;
using PowerLanguage;
using Zeghs.Data;
using Zeghs.Utils;
using Zeghs.Settings;

namespace Zeghs.IO {
	internal sealed class FuturesCsvAdapter {
		private static readonly ILog logger = LogManager.GetLogger(typeof(FuturesCsvAdapter));

		private static Dictionary<string, string> cTargetSymbols = new Dictionary<string, string>() {
			{ "TX", "TXF0.tw" },
			{ "MTX", "MXF0.tw" },
			{ "TE", "EXF0.tw" },
			{ "TF", "FXF0.tw" }
		};

		internal static void Convert() {
			string[] sFiles = Directory.GetFiles("csv", "*.zip");
			foreach (string sFile in sFiles) {
				ConvertCSV(sFile);
			}
		}

		private static void ConvertCSV(string csvFile) {
			string[] sData = LoadCSV(csvFile);
			if (sData == null) {
				return;
			}

			bool bConvert = false;
			SeriesSymbolData cMSeries = null, cDSeries = null;

			if (logger.IsInfoEnabled) logger.Info("[Convert] 開始轉換 CSV 期貨檔案資訊...");

			string[] sItems = sData[0].Split(',');
			string sSymbolId = null;
			bConvert = cTargetSymbols.TryGetValue(sItems[0], out sSymbolId);

			if (bConvert) {
				int iLength = sData.Length;
				cMSeries = CreateSeries(sSymbolId, EResolution.Minute);
				cMSeries.SetRange(iLength);
				cMSeries.AdjustSize(iLength, true);

				for (int i = iLength - 1; i >=0; i--) {
					sItems = sData[i].Split(',');
					DateTime cTime = DateTime.Parse(string.Format("{0} {1}", sItems[2], sItems[3]));
					double dOpen = double.Parse(sItems[4]);
					double dHigh = double.Parse(sItems[5]);
					double dLow = double.Parse(sItems[6]);
					double dClose = double.Parse(sItems[7]);
					double dVolume = double.Parse(sItems[8]);
					cMSeries.AddSeries(cTime, dOpen, dHigh, dLow, dClose, dVolume, false);
				}

				cDSeries = CreateSeries(sSymbolId, EResolution.Day);
				cMSeries.Merge(cDSeries);

				FileAdapter cAdapter = new FileAdapter(Settings.GlobalSettings.Settings.DataPath, true);
				cAdapter.Write(cMSeries);
				cAdapter.Write(cDSeries);

				cMSeries.Dispose();
				cDSeries.Dispose();
			}
		}

		private static SeriesSymbolData CreateSeries(string symbolId, EResolution type) {
			InstrumentDataRequest cRequest = new InstrumentDataRequest() {
				Exchange = "TWSE",
				DataFeed = "Mitake",
				Range = DataRequest.CreateBarsBack(DateTime.Now, 1),
				Resolution = new Resolution(type, 1),
				Symbol = symbolId
			};

			return new SeriesSymbolData(cRequest);
		}

		private static string[] LoadCSV(string csvFile) {
			if (logger.IsInfoEnabled) logger.InfoFormat("Extracting... @{0}", csvFile);
			bool bOK = Compression.Extract(csvFile, null, "csv\\");
			if (bOK) {
				if (logger.IsInfoEnabled) logger.InfoFormat("[Extract] @{0} extract completed...", csvFile);

				string[] sFiles = Directory.GetFiles("csv", "*.txt");
				if (sFiles.Length > 0) {
					string[] sData = File.ReadAllLines(sFiles[0], Encoding.Default);
					File.Delete(sFiles[0]);
					return sData;
				}
			}
			return null;
		}
	}
}