using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;
using log4net;
using PowerLanguage;
using Mitake.Sockets.Data;
using Mitake.Stock.Data;
using Mitake.Stock.Decode;
using Zeghs.Data;
using Zeghs.Utils;
using Zeghs.Products;
using Zeghs.Managers;

namespace Zeghs.IO {
	internal sealed class MitakeSourceAdapter : IDisposable {
		private static readonly ILog logger = LogManager.GetLogger(typeof(MitakeSourceAdapter));

		private bool __bDisposed = false;
		private ZBuffer __cSymbolBuffer = null;
		private AbstractExchange __cExchange = null;

		internal MitakeSourceAdapter() {
			__cSymbolBuffer = new ZBuffer(1048576 * 5);
			__cExchange = ProductManager.Manager.GetExchange("TWSE");
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		internal void Load(DateTime date) {
			if (logger.IsInfoEnabled) logger.InfoFormat("讀取 {0} 股票資料並轉換...", date.ToString("yyyy-MM-dd"));
			if (logger.IsInfoEnabled) logger.InfoFormat("{0} 下載股票即時資料中...", date.ToString("yyyy-MM-dd"));
			if (GetFiles(date)) {
				string[] sFiles = File.ReadAllLines("mitake\\files.txt");
				foreach (string sFile in sFiles) {
					int iIndex = sFile.LastIndexOf(" ");
					System.Console.WriteLine();

					string sFilename = sFile.Substring(iIndex + 1);
					System.Console.WriteLine("Searching... {0}", sFilename);

					try {
						FtpWebRequest request = WebRequest.Create(string.Format("ftp://202.39.79.145/{0}/{1}/{2}/{3}", date.Year, date.Month.ToString("00"), date.Day.ToString("00"), sFilename)) as FtpWebRequest;
						request.Method = WebRequestMethods.Ftp.DownloadFile;
						request.Credentials = new NetworkCredential("zentc", "3979076");

						FtpWebResponse response = request.GetResponse() as FtpWebResponse;
						Stream responseStream = response.GetResponseStream();

						int iSize = 0;
						long lTotals = 0;
						byte[] bBuffer = new byte[1048576];
						using (FileStream cStream = new FileStream("mitake\\" + sFilename, FileMode.Create, FileAccess.Write, FileShare.Read)) {
							while ((iSize = responseStream.Read(bBuffer, 0, bBuffer.Length)) > 0) {
								cStream.Write(bBuffer, 0, iSize);

								lTotals += iSize;
								System.Console.Write("Downloading... {0} kb", (lTotals / 1000).ToString("N0"));
								System.Console.CursorLeft = 0;
							}
						}

						responseStream.Close();
						response.Close();
					} catch {
						System.Console.WriteLine("無 {0} 股票即時資料...", sFilename);
					}
				}
			} else {
				return;
			}

			try {
				StockDecoder.Reset(date);  //重置股票解碼器(並設定要解碼的資料日期, 解碼器會以當天為開盤日做解碼動作)

				__cSymbolBuffer.Length = 0;

				if (logger.IsInfoEnabled) logger.InfoFormat("讀取 {0} 股票基本資料中...", date.ToString("yyyy-MM-dd"));
				ConvertSymbol("mitake\\Load.0");  //讀取基本資料

				string[] sFiles = Directory.GetFiles("mitake\\");
				foreach (string sFile in sFiles) {
					string sName = sFile.Substring(sFile.LastIndexOf("\\") + 1);
					if (sName[0] < 'A' || sName.IndexOf("Load") > -1) {
						System.Console.Write("解碼檔案中... {0}        ", sFile);
						System.Console.CursorLeft = 0;

						Decode(sFile);
					} else {
						File.Delete(sFile);
					}
				}

				//CreateFutures(date);
				//CreateIndexs(date);
				CreateIndexOptions(date);
				//CreateSpreads(date);
				//CreateStocks(date);

				if (logger.IsInfoEnabled) logger.InfoFormat("{0} 股票資料轉換完畢...", date.ToString("yyyy-MM-dd"));
			} catch (Exception __errExcep) {
				File.AppendAllText("lose.txt", string.Format("{0}\r\n", date.ToString("yyyy-MM-dd")), System.Text.Encoding.UTF8);
				if (logger.IsErrorEnabled) logger.ErrorFormat("{0}\r\n{1}", __errExcep.Message, __errExcep.StackTrace);
			}
		}

		private void ConvertSymbol(string file) {
			if (File.Exists(file)) {
				StockDecoder.StockProc += StockDecoder_onStock;

				int iSize = 0;
				SocketToken cToken = new SocketToken();
				using (FileStream cStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					while ((iSize = cStream.Read(cToken.ReceiveBuffer.Data, 0, 8192)) > 0) {
						cToken.ReceiveBuffer.Length = iSize;
						StockDecoder.Decode(null, cToken, false);
					}
				}

				StockDecoder.StockProc -= StockDecoder_onStock;

				File.Delete(file);
				
				using (FileStream cStream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read)) {
					cStream.Write(__cSymbolBuffer.Data, 0, __cSymbolBuffer.Length);
				}
				Decode(file);
			}
		}

		private bool GetFiles(DateTime date) {
			try {
				FtpWebRequest request = WebRequest.Create(string.Format("ftp://202.39.79.145/{0}/{1}/{2}/", date.Year, date.Month.ToString("00"), date.Day.ToString("00"))) as FtpWebRequest;
				request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
				request.Credentials = new NetworkCredential("zentc", "3979076");

				FtpWebResponse response = request.GetResponse() as FtpWebResponse;
				Stream responseStream = response.GetResponseStream();

				int iSize = 0;
				long lTotals = 0;
				byte[] bBuffer = new byte[1048576];
				using (FileStream cStream = new FileStream("mitake\\files.txt", FileMode.Create, FileAccess.Write, FileShare.Read)) {
					while ((iSize = responseStream.Read(bBuffer, 0, bBuffer.Length)) > 0) {
						cStream.Write(bBuffer, 0, iSize);

						lTotals += iSize;
						System.Console.Write("Get ListDirectoryDetails data... {0} kb", (lTotals / 1000).ToString("N0"));
						System.Console.CursorLeft = 0;
					}
				}

				responseStream.Close();
				response.Close();
			} catch {
				System.Console.WriteLine("無 {0} 股票即時資料檔案清單...", date.ToString("yyyy-MM-dd"));
				return false;
			}
			return true;
		}

		private void StockDecoder_onStock(object sender, Mitake.Events.StockEvent e) {
			if (e.Header == 0x53) {
				if (e.Type == 0x38) {
					EncodeUtil.EncodeSymbol(e.Source);
					__cSymbolBuffer.Add(e.Source.Data, 0, e.Source.Length);
				}
			}
		}

		private void Decode(string file) {
			if (File.Exists(file)) {
				int iSize = 0;
				SocketToken cToken = new SocketToken();
				using (FileStream cStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					while ((iSize = cStream.Read(cToken.ReceiveBuffer.Data, 0, 8192)) > 0) {
						cToken.ReceiveBuffer.Length = iSize;
						StockDecoder.Decode(null, cToken, true);
					}
				}
				File.Delete(file);
			}
		}

		private void Dispose(bool disposing) {
			if (!this.__bDisposed) {
				__bDisposed = true;

				if (disposing) {
					__cExchange = null;
					__cSymbolBuffer = null;
				}
			}
		}

		private void CreateFutures(DateTime date) {
			if (logger.IsInfoEnabled) logger.Info("開始轉換期貨資料...");

			List<string> cStocks = __cExchange.GetProductClassify(ESymbolCategory.Future);
			foreach (string sStock in cStocks) {
				int iIndex = sStock.LastIndexOf(".");
				if (iIndex > -1) {
					iIndex -= 2;
					if (sStock[3] >= 'A') {
						continue;  //沒有轉換代號的期貨全部都不要轉換
					}
				}

				AbstractProductProperty cProperty = __cExchange.GetProperty(sStock, "Mitake");
				if (cProperty != null) {
					IQuote cQuote = MitakeStorage.Storage.GetQuote(sStock);
					if (cQuote != null) {
						SeriesSymbolData cMSeries = CreateSeries(sStock, EResolution.Minute, date);
						SeriesSymbolData cDSeries = CreateSeries(sStock, EResolution.Day, date);

						int iLast = cQuote.TickCount - 1;
						if (iLast >= 0) {
							for (int i = iLast; i >= 0; i--) {
								ITick cTick = cQuote.GetTick(i);
								if (cTick != null) {
									cMSeries.Merge(cTick);
									cDSeries.Merge(cTick);
								}
							}

							FileAdapter cAdapter = new FileAdapter(Settings.GlobalSettings.Settings.DataPath, false);
							cAdapter.Write(cMSeries);
							cAdapter.Write(cDSeries);

							System.Console.Write("Convert... {0}        ", sStock);
							System.Console.CursorLeft = 0;
						}
					}
				}
			}
		}

		private void CreateIndexs(DateTime date) {
			if (logger.IsInfoEnabled) logger.Info("開始轉換指數資料...");

			List<string> cStocks = __cExchange.GetProductClassify(ESymbolCategory.Index);
			foreach (string sStock in cStocks) {
				AbstractProductProperty cProperty = __cExchange.GetProperty(sStock, "Mitake");
				if (cProperty != null) {
					IQuote cQuote = MitakeStorage.Storage.GetQuote(sStock);
					if (cQuote != null) {
						SeriesSymbolData cMSeries = CreateSeries(sStock, EResolution.Minute, date);
						SeriesSymbolData cDSeries = CreateSeries(sStock, EResolution.Day, date);

						int iLast = cQuote.TickCount - 1;
						if (iLast >= 0) {
							for (int i = iLast; i >= 0; i--) {
								ITick cTick = cQuote.GetTick(i);
								if (cTick != null && cTick.Price > 0) {
									cMSeries.Merge(cTick);
									cDSeries.Merge(cTick);
								}
							}

							FileAdapter cAdapter = new FileAdapter(Settings.GlobalSettings.Settings.DataPath, false);
							cAdapter.Write(cMSeries);
							cAdapter.Write(cDSeries);

							System.Console.Write("Convert... {0}        ", sStock);
							System.Console.CursorLeft = 0;
						}
					}
				}
			}
		}

		private void CreateIndexOptions(DateTime date) {
			if (logger.IsInfoEnabled) logger.Info("開始轉換指數型選擇權資料...");

			List<string> cStocks = __cExchange.GetProductClassify(ESymbolCategory.IndexOption);
			foreach (string sStock in cStocks) {
				int iIndex = sStock.LastIndexOf(".");
				if (iIndex > -1) {
					iIndex -= 2;
					if (sStock[iIndex] >= 'A') {
						continue;  //沒有轉換代號的選擇權全部都不要轉換
					}
				}

				AbstractProductProperty cProperty = __cExchange.GetProperty(sStock, "Mitake");
				if (cProperty != null) {
					IQuote cQuote = MitakeStorage.Storage.GetQuote(sStock);
					if (cQuote != null) {
						SeriesSymbolData cMSeries = CreateSeries(sStock, EResolution.Minute, date);
						SeriesSymbolData cDSeries = CreateSeries(sStock, EResolution.Day, date);

						int iLast = cQuote.TickCount - 1;
						if (iLast >= 0) {
							for (int i = iLast; i >= 0; i--) {
								ITick cTick = cQuote.GetTick(i);
								if (cTick != null) {
									cMSeries.Merge(cTick);
									cDSeries.Merge(cTick);
								}
							}

							FileAdapter cAdapter = new FileAdapter(Settings.GlobalSettings.Settings.DataPath, false);
							cAdapter.Write(cMSeries);
							cAdapter.Write(cDSeries);

							System.Console.Write("Convert... {0}        ", sStock);
							System.Console.CursorLeft = 0;
						}
					}
				}
			}
		}

		private void CreateSpreads(DateTime date) {
			if (logger.IsInfoEnabled) logger.Info("開始轉換展延類資料...");
			
			List<string> cStocks = __cExchange.GetProductClassify(ESymbolCategory.Spread);
			foreach (string sStock in cStocks) {
				AbstractProductProperty cProperty = __cExchange.GetProperty(sStock, "Mitake");
				if (cProperty != null) {
					IQuote cQuote = MitakeStorage.Storage.GetQuote(sStock);
					if (cQuote != null) {
						SeriesSymbolData cMSeries = CreateSeries(sStock, EResolution.Minute, date);
						SeriesSymbolData cDSeries = CreateSeries(sStock, EResolution.Day, date);

						int iLast = cQuote.TickCount - 1;
						if (iLast >= 0) {
							for (int i = iLast; i >= 0; i--) {
								ITick cTick = cQuote.GetTick(i);
								if (cTick != null) {
									cMSeries.Merge(cTick);
									cDSeries.Merge(cTick);
								}
							}

							FileAdapter cAdapter = new FileAdapter(Settings.GlobalSettings.Settings.DataPath, false);
							cAdapter.Write(cMSeries);
							cAdapter.Write(cDSeries);

							System.Console.Write("Convert... {0}        ", sStock);
							System.Console.CursorLeft = 0;
						}
					}
				}
			}
		}

		private void CreateStocks(DateTime date) {
			if (logger.IsInfoEnabled) logger.Info("開始轉換股票個股資料...");

			List<string> cStocks = __cExchange.GetProductClassify(ESymbolCategory.Stock);
			foreach (string sStock in cStocks) {
				AbstractProductProperty cProperty = __cExchange.GetProperty(sStock, "Mitake");
				if (cProperty != null) {
					IQuote cQuote = MitakeStorage.Storage.GetQuote(sStock);
					if (cQuote != null) {
						SeriesSymbolData cMSeries = CreateSeries(sStock, EResolution.Minute, date);
						SeriesSymbolData cDSeries = CreateSeries(sStock, EResolution.Day, date);

						int iLast = cQuote.TickCount - 1;
						if (iLast >= 0) {
							for (int i = iLast; i >= 0; i--) {
								ITick cTick = cQuote.GetTick(i);
								if (cTick != null) {
									cMSeries.Merge(cTick);
									cDSeries.Merge(cTick);
								}
							}

							FileAdapter cAdapter = new FileAdapter(Settings.GlobalSettings.Settings.DataPath, false);
							cAdapter.Write(cMSeries);
							cAdapter.Write(cDSeries);

							System.Console.Write("Convert... {0}        ", sStock);
							System.Console.CursorLeft = 0;
						}
					}
				}
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
	}
}