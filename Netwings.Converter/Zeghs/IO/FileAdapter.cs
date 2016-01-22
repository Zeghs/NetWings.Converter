using System;
using System.IO;
using System.Text;
using log4net;
using PowerLanguage;
using Zeghs.Data;
using Zeghs.Utils;

namespace Zeghs.IO {
	internal sealed class FileAdapter {
		private static readonly ILog logger = LogManager.GetLogger(typeof(FileAdapter));
		internal const int MAX_BLOCK_SIZE = 48;

		private bool __bCreate = false;
		private string __sPath = null;
		
		internal FileAdapter(string path, bool isCreate) {
			__sPath = path;
			__bCreate = isCreate;
		}

		/// <summary>
		///   插入資訊
		/// </summary>
		/// <param name="series">SeriesSymbolData 類別</param>
		/// <param name="date">欲插入的日期(資料會被插入至該指定的日期前)</param>
		internal void Insert(SeriesSymbolData series, DateTime date) {
			try {
				string sFile = string.Format("{0}\\{1}\\{2}", __sPath, (series.DataRequest.Resolution.TotalSeconds < Resolution.MAX_BASE_TOTALSECONDS) ? "mins" : "days", series.DataRequest.Symbol);
				using (FileStream cStream = new FileStream(sFile, (__bCreate) ? FileMode.Create : FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite)) {
					bool bFind = false;
					if (!__bCreate) {
						long lCount = cStream.Length / MAX_BLOCK_SIZE;
						bFind = FileSearchUtil.BinaryNearSearch(cStream, lCount, MAX_BLOCK_SIZE, date);
					}

					if (bFind) {
						long lCurrentP = cStream.Position;

						//先保留後面的資料以便插入時不會被覆蓋
						long lRevSize = cStream.Length - lCurrentP;
						ZBuffer cTemp = new ZBuffer((int) lRevSize);
						cStream.Read(cTemp.Data, 0, (int) lRevSize);
						cTemp.Length = (int) lRevSize;

						cStream.SetLength(lCurrentP + 1);
						cStream.Position = lCurrentP; //移動置固定位置

						ZBuffer cBuffer = new ZBuffer(64);

						int iHistoryIndex = series.Indexer.HistoryIndex;
						int iCount = series.Count;
						for (int i = 0; i < iCount; i++) {
							int iIndex = iHistoryIndex + i;
							cBuffer.Length = 0;
							cBuffer.Add(series.Time[iIndex]);
							cBuffer.Add(series.Open[iIndex]);
							cBuffer.Add(series.High[iIndex]);
							cBuffer.Add(series.Low[iIndex]);
							cBuffer.Add(series.Close[iIndex]);
							cBuffer.Add(series.Volume[iIndex]);

							cStream.Write(cBuffer.Data, 0, cBuffer.Length);
						}
						cStream.Write(cTemp.Data, 0, cTemp.Length);  //再將後面的資料合併
					}
				}
				if (logger.IsInfoEnabled) logger.InfoFormat("[FileWrite] {0} insert completed...  count={1}", sFile, series.Count);
			} catch (Exception __errExcep) {
				if (logger.IsErrorEnabled) logger.ErrorFormat("[FileWrite] '{0}' insert error...", series.DataRequest.Symbol, series.Count);
				if (logger.IsErrorEnabled) logger.ErrorFormat("{0}/r/n{1}", __errExcep.Message, __errExcep.StackTrace);
			}
		}

		/// <summary>
		///   取代資訊
		/// </summary>
		/// <param name="series">SeriesSymbolData 類別</param>
		/// <param name="date">欲取代的日期(該指定的日期的資料會被取代掉)</param>
		internal void Replace(SeriesSymbolData series, DateTime date) {
			try {
				string sFile = string.Format("{0}\\{1}\\{2}", __sPath, (series.DataRequest.Resolution.TotalSeconds < Resolution.MAX_BASE_TOTALSECONDS) ? "mins" : "days", series.DataRequest.Symbol);
				using (FileStream cStream = new FileStream(sFile, (__bCreate) ? FileMode.Create : FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite)) {
					bool bFind = true;
					if (!__bCreate) {
						long lCount = cStream.Length / MAX_BLOCK_SIZE;
						bFind = FileSearchUtil.BinarySearch(cStream, lCount, MAX_BLOCK_SIZE, date);
					}

					if (bFind) {
						long lCurrentP = cStream.Position;

						//先保留後面的資料以便插入時不會被覆蓋
						ZBuffer cTemp = null;
						FileSearchUtil.SearchNextDate(cStream, MAX_BLOCK_SIZE, date);
						long lNextPos = cStream.Position;
						long lRevSize = cStream.Length - lNextPos;

						if (lRevSize > 0) {
							cTemp = new ZBuffer((int) lRevSize);
							cStream.Read(cTemp.Data, 0, (int) lRevSize);
							cTemp.Length = (int) lRevSize;
						}

						cStream.SetLength(lCurrentP + 1);
						cStream.Position = lCurrentP; //移動置固定位置

						ZBuffer cBuffer = new ZBuffer(64);

						int iHistoryIndex = series.Indexer.HistoryIndex;
						int iCount = series.Count;
						for (int i = 0; i < iCount; i++) {
							int iIndex = iHistoryIndex + i;
							cBuffer.Length = 0;
							cBuffer.Add(series.Time[iIndex]);
							cBuffer.Add(series.Open[iIndex]);
							cBuffer.Add(series.High[iIndex]);
							cBuffer.Add(series.Low[iIndex]);
							cBuffer.Add(series.Close[iIndex]);
							cBuffer.Add(series.Volume[iIndex]);

							cStream.Write(cBuffer.Data, 0, cBuffer.Length);
						}

						if (cTemp != null) {
							cStream.Write(cTemp.Data, 0, cTemp.Length);  //再將後面的資料合併
						}
					}
				}
				if (logger.IsInfoEnabled) logger.InfoFormat("[FileWrite] {0} replace completed...  count={1}", sFile, series.Count);
			} catch (Exception __errExcep) {
				if (logger.IsErrorEnabled) logger.ErrorFormat("[FileWrite] '{0}' replace error...", series.DataRequest.Symbol, series.Count);
				if (logger.IsErrorEnabled) logger.ErrorFormat("{0}/r/n{1}", __errExcep.Message, __errExcep.StackTrace);
			}
		}

		internal void Write(SeriesSymbolData series) {
			try {
				string sFile = string.Format("{0}\\{1}\\{2}", __sPath, (series.DataRequest.Resolution.TotalSeconds < Resolution.MAX_BASE_TOTALSECONDS) ? "mins" : "days", series.DataRequest.Symbol);
				using (FileStream cStream = new FileStream(sFile, (__bCreate) ? FileMode.Create : FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite)) {
					if (!__bCreate) {
						long lCount = cStream.Length / MAX_BLOCK_SIZE;
						if (lCount > 0) {
							FileSearchUtil.BinarySearch(cStream, lCount, MAX_BLOCK_SIZE, series.Time[0]);
						}
					}

					ZBuffer cBuffer = new ZBuffer(64);

					int iHistoryIndex = series.Indexer.HistoryIndex;
					int iCount = series.Count;
					for (int i = 0; i < iCount; i++) {
						int iIndex = iHistoryIndex + i;
						cBuffer.Length = 0;
						cBuffer.Add(series.Time[iIndex]);
						cBuffer.Add(series.Open[iIndex]);
						cBuffer.Add(series.High[iIndex]);
						cBuffer.Add(series.Low[iIndex]);
						cBuffer.Add(series.Close[iIndex]);
						cBuffer.Add(series.Volume[iIndex]);

						cStream.Write(cBuffer.Data, 0, cBuffer.Length);
					}
				}
				if (logger.IsInfoEnabled) logger.InfoFormat("[FileWrite] {0} write completed...  count={1}", sFile, series.Count);
			} catch (Exception __errExcep) {
				if (logger.IsErrorEnabled) logger.ErrorFormat("[FileWrite] '{0}' write error...", series.DataRequest.Symbol, series.Count);
				if (logger.IsErrorEnabled) logger.ErrorFormat("{0}/r/n{1}", __errExcep.Message, __errExcep.StackTrace);
			}
		}
	}
}