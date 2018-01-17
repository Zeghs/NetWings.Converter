using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using log4net;
using PowerLanguage;
using Zeghs.Data;
using Zeghs.Utils;
using Zeghs.Settings;
using Zeghs.Managers;
using Zeghs.Products;
using Zeghs.Rules;

namespace Zeghs.IO {
	internal sealed class FuturesRptAdapter {
		private static readonly ILog logger = LogManager.GetLogger(typeof(FuturesRptAdapter));

		private static Dictionary<string, string> cTargetSymbols = new Dictionary<string, string>() {
			{ "TX", "TXF0.tw" },
			{ "MTX", "MXF0.tw" },
			{ "TE", "EXF0.tw" },
			{ "TF", "FXF0.tw" }
		};

		internal static void Convert(DateTime date, bool isDownload = true) {
			string[] sData = LoadRPT(date, isDownload);
			if (sData == null) {
				return;
			}

			double dVolume = 0;
			bool bConvert = false;
			string sEDate = string.Empty;
			string sOSymbolId = string.Empty;
			string sDate = date.ToString("yyyyMMdd");
			SeriesSymbolData cMSeries = null, cDSeries = null;

			if (logger.IsInfoEnabled) logger.Info("[Convert] 開始轉換期交所的期貨資訊...");
			
			int iLength = sData.Length;
			for (int i = 1; i < iLength; i++) {
				string[] sItems = sData[i].Split(',');
				if (sItems.Length == 9) {
					string sFutureDate = sItems[0].Trim();
					if (!sFutureDate.Equals(sDate)) {  //檢查日期是否為欲轉換的日期
						continue;
					}

					string sSymbolId = sItems[1].Trim();
					if (!sSymbolId.Equals(sOSymbolId)) {
						if (bConvert) {
							FileAdapter cAdapter = new FileAdapter(Settings.GlobalSettings.Settings.DataPath, false);
							cAdapter.Write(cMSeries);
							cAdapter.Write(cDSeries);
						}

						dVolume = 0;
						sOSymbolId = sSymbolId;

						string sTWSymbolId = null;
						bConvert = cTargetSymbols.TryGetValue(sSymbolId, out sTWSymbolId);
						if (bConvert) {
							cMSeries = CreateSeries(sTWSymbolId, EResolution.Minute, date);
							cDSeries = CreateSeries(sTWSymbolId, EResolution.Day, date);

							if (ConvertParameter.強制今日為期權到期日) {
								sEDate = DateTime.Now.Year.ToString() + ConvertParameter.自訂期權合約月份.ToString("0#");
							} else {
								AbstractExchange cExchange = ProductManager.Manager.GetExchange("TWSE");
								AbstractProductProperty cProperty = cExchange.GetProperty(sTWSymbolId);
								IContractTime cContractTime = cProperty.ContractRule as IContractTime;
								ContractTime cContract = cContractTime.GetContractTime(date);

								sEDate = cContract.MaturityDate.ToString("yyyyMM");
							}
						}
					}

					if (bConvert) {
						string sEndDate = sItems[2].Trim();
						if (sEndDate.Length == 6 && sEndDate.Equals(sEDate)) {
							Tick cTick = new Tick();
							cTick.Time = DateTimeParser.Parse(sItems[0], sItems[3]);
							cTick.Price = double.Parse(sItems[4]);
							cTick.Single = double.Parse(sItems[5]) / 2;  //Buy + Sell(需要除以2) 
							dVolume += cTick.Single;
							cTick.Volume = dVolume;

							cMSeries.Merge(cTick);
							cDSeries.Merge(cTick);
						}
					}
				}
			}

			if (bConvert) {
				FileAdapter cAdapter = new FileAdapter(Settings.GlobalSettings.Settings.DataPath, false);
				cAdapter.Write(cMSeries);
				cAdapter.Write(cDSeries);
			}
		}

		private static SeriesSymbolData CreateSeries(string symbolId, EResolution type, DateTime date) {
			InstrumentDataRequest cRequest = new InstrumentDataRequest() {
				Exchange = "TWSE",
				DataFeed = "Mitake",
				Range = DataRequest.CreateBarsBack(DateTime.Now, 1),
				Resolution = new Resolution(type, 1),
				Symbol = symbolId
			};

			SeriesSymbolData cSeries = new SeriesSymbolData(cRequest);
			cSeries.CreateRealtimePeriods(date);
			return cSeries;
		}

		private static string[] LoadRPT(DateTime date, bool isDownload = true) {
			string sDaily = string.Format("Daily_{0}_{1}_{2}", date.Year, date.Month.ToString("0#"), date.Day.ToString("0#"));
			if (isDownload) {
				using (WebClient cClient = new WebClient()) {
					string sUrl = string.Format("http://www.taifex.com.tw/DailyDownload/DailyDownload/{0}.zip", sDaily);
					try {
						cClient.DownloadFile(sUrl, sDaily + ".zip");
					} catch (Exception __errExcep) {
						if (File.Exists(sDaily + ".zip")) {
							File.Delete(sDaily + ".zip");
						}
						if (logger.IsErrorEnabled)
							logger.ErrorFormat("{0}/r/n{1}", __errExcep.Message, __errExcep.StackTrace);
						return null;
					}
				}
			}

			if (File.Exists(sDaily + ".zip")) {
				if (logger.IsInfoEnabled) logger.InfoFormat("[Download] @{0}.zip download completed...", sDaily);

				bool bOK = Compression.Extract(sDaily + ".zip", null, string.Empty);
				if (bOK) {
					if (logger.IsInfoEnabled) logger.InfoFormat("[Extract] @{0}.rpt extract completed...", sDaily);

					string[] sData = File.ReadAllLines(sDaily + ".rpt", Encoding.Default);
					File.Delete(sDaily + ".zip");
					File.Delete(sDaily + ".rpt");
					return sData;
				} else {
					File.Delete(sDaily + ".zip");
				}
			}
			return null;
		}
	}
}