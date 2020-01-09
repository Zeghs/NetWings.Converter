using System;
using System.Collections.Generic;
using PowerLanguage;
using Zeghs.Rules;
using Zeghs.Events;
using Zeghs.Products;
using Zeghs.Services;
using Zeghs.Managers;

namespace Zeghs.Data {
	/// <summary>
	///   商品資料類別(存放開高低收量資訊)
	/// </summary>
	public sealed class SeriesSymbolData : ISeriesSymbolDataRand, IDisposable {
		private static void MergeSeries(SeriesSymbolData target, DateTime period, DateTime time, double open, double high, double low, double close, double volume, bool isNewBars, bool isRealtime) {
			if (isNewBars) {
				target.AddSeries(period, open, high, low, close, volume, isRealtime);
			} else {
				target.SetSeries(open, high, low, close, volume, isRealtime);
			}
		}

		private double __dOVolume = 0;
		private int __iRealtimeCount = 0;
		private bool __bDisposed = false;
		private DateTime __cUpdateTime;
		private Series<double> __cLows = null;
		private Series<double> __cHighs = null;
		private Series<double> __cOpens = null;
		private Series<double> __cCloses = null;
		private Series<double> __cVolumes = null;
		private Series<DateTime> __cTimes = null;
		private Queue<DateTime> __cTimeQueue = null;
		private InstrumentSettings __cSettings = null;
		private InstrumentDataRequest __cDataRequest;

		public int Current {
			get {
				return 0;
			}
		}

		/// <summary>
		///   [取得] 資料總個數
		/// </summary>
		public int Count {
			get {
				return Indexer.RealtimeIndex - Indexer.HistoryIndex + 1;
			}
		}

		/// <summary>
		///   [取得] 收盤價陣列資訊
		/// </summary>
		public ISeries<double> Close {
			get {
				return __cCloses;
			}
		}

		/// <summary>
		///   [取得] 最高價陣列資訊
		/// </summary>
		public ISeries<double> High {
			get {
				return __cHighs;
			}
		}

		/// <summary>
		///   [取得] 最低價陣列資訊
		/// </summary>
		public ISeries<double> Low {
			get {
				return __cLows;
			}
		}

		/// <summary>
		///   [取得] 開盤價陣列資訊
		/// </summary>
		public ISeries<double> Open {
			get {
				return __cOpens;
			}
		}

		/// <summary>
		///   [取得] 日期時間陣列資訊
		/// </summary>
		public ISeries<DateTime> Time {
			get {
				return __cTimes;
			}
		}

		/// <summary>
		///   [取得] 成交量陣列資訊
		/// </summary>
		public ISeries<double> Volume {
			get {
				return __cVolumes;
			}
		}

		internal InstrumentDataRequest DataRequest {
			get {
				return __cDataRequest;
			}
		}

		internal SeriesIndexer Indexer {
			get;
			private set;
		}

		internal DateTime LastBarTime {
			get {
				return __cTimes[Indexer.RealtimeIndex];
			}
		}

		internal IInstrumentSettings Settings {
			get {
				return __cSettings;
			}
		}

		internal SeriesSymbolData(InstrumentDataRequest dataRequest, InstrumentSettings settings = null) {
			this.Indexer = new SeriesIndexer();

			__cDataRequest = dataRequest;
			__cSettings = ((settings == null) ? new InstrumentSettings(ref __cDataRequest) : settings.Create(ref __cDataRequest));
			__cUpdateTime = DateTime.UtcNow.AddHours(__cSettings.TimeZone);
			
			SessionObject cSession = __cSettings.GetSessionFromToday();
			__iRealtimeCount = (int) ((cSession.EndTime - cSession.StartTime).TotalSeconds / dataRequest.Resolution.TotalSeconds + 1);

			Indexer.HistoryIndex = 0;
			Indexer.RealtimeIndex = -1;

			__cOpens = new Series<double>(__iRealtimeCount);
			__cHighs = new Series<double>(__iRealtimeCount);
			__cLows = new Series<double>(__iRealtimeCount);
			__cCloses = new Series<double>(__iRealtimeCount);
			__cTimes = new Series<DateTime>(__iRealtimeCount);
			__cVolumes = new Series<double>(__iRealtimeCount);

			__cDataRequest.Range.Count = 0;  //將資料筆數設定為0(因為一開始沒有請求任何資訊)
		}

		/// <summary>
		///   釋放腳本資源
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		internal void AddSeries(DateTime time, double open, double high, double low, double close, double volume, bool isRealtime = true) {
			int iCount = __cTimes.Count;
			int iIndex = (isRealtime) ? ++Indexer.RealtimeIndex : --Indexer.HistoryIndex;
			if (iIndex < 0 || iIndex == iCount) {
				iIndex = AdjustSize(4, !isRealtime);
			}

			__cTimes.SetData(iIndex, time);
			__cOpens.SetData(iIndex, open);
			__cHighs.SetData(iIndex, high);
			__cLows.SetData(iIndex, low);
			__cCloses.SetData(iIndex, close);
			__cVolumes.SetData(iIndex, volume);
		}

		internal int AdjustSize(int count, bool isInsert = false) {
			__cTimes.AdjustSize(count, isInsert);
			__cOpens.AdjustSize(count, isInsert);
			__cHighs.AdjustSize(count, isInsert);
			__cLows.AdjustSize(count, isInsert);
			__cCloses.AdjustSize(count, isInsert);
			__cVolumes.AdjustSize(count, isInsert);

			if (isInsert) {  //如果是往前插入空間需要調整索引直
				Indexer.AdjustIndex(count);
			}
			return (isInsert) ? Indexer.HistoryIndex : Indexer.RealtimeIndex;
		}

		internal SeriesSymbolData CreateSeries(InstrumentDataRequest dataRequest) {
			return new SeriesSymbolData(dataRequest, __cSettings);
		}

		internal void Merge(SeriesSymbolData target) {
			int iTargetCount = target.Count;
			int iFirstIndex = target.Indexer.GetBaseIndex(__cDataRequest.Range.Count);

			DateTime cFrom = __cTimes[Indexer.HistoryIndex];
			DateTime cTo = __cTimes[iFirstIndex];
			List<DateTime> cPeriods = target.__cDataRequest.Resolution.CalculatePeriods(cFrom, cTo);

			for (int i = iFirstIndex; i >= Indexer.HistoryIndex; i--) {
				DateTime cBaseTime = __cTimes[i];
				bool bNewBars = Resolution.GetNearestPeriod(cPeriods, ref cBaseTime);
				MergeSeries(target, cBaseTime, __cTimes[i], __cOpens[i], __cHighs[i], __cLows[i], __cCloses[i], __cVolumes[i], bNewBars, false);
				if (bNewBars) {
					target.Indexer.SetBaseIndex(i);
				}
			}
		}

		internal void Merge(ITick tick) {
			double dVolume = tick.Volume;
			if (dVolume > __dOVolume) {
				double dPrice = tick.Price;
				double dSingle = dVolume - __dOVolume;  //重新計算準確的單量(即時系統送來的單量並不準確, 所以以總量為標準依據)
				if (dPrice > 0 && dSingle > 0) {
					DateTime cBaseTime = tick.Time;
					if (__cTimeQueue != null) {
						bool bNewBars = Resolution.GetNearestPeriod(__cTimeQueue, ref cBaseTime);
						MergeSeries(this, cBaseTime, tick.Time, dPrice, dPrice, dPrice, dPrice, dSingle, bNewBars, true);
					} else {
						MergeSeries(this, cBaseTime, tick.Time, dPrice, dPrice, dPrice, dPrice, dSingle, false, true);
					}
				}
				
				__cUpdateTime = DateTime.UtcNow.AddHours(__cSettings.TimeZone);
				__dOVolume = dVolume;
			}
		}

		internal Queue<DateTime> CreateRealtimePeriods(DateTime today) {
			DateTime cFrom = today;

			AbstractExchange cExchange = ProductManager.Manager.GetExchange(__cDataRequest.Exchange);
			AbstractProductProperty cProperty = cExchange.GetProperty(__cDataRequest.Symbol);
			IContractTime cContractTime = cProperty.ContractRule as IContractTime;

			DateTime cExpiration = DateTime.MinValue;
			if (cContractTime == null) {
				cExpiration = today.AddDays(2);
			} else {
				ContractTime cContract = cContractTime.GetContractTime(today);
				cExpiration = cContract.MaturityDate;
			}

			ESymbolCategory cCategory = __cSettings.ASymbolInfo2.Category;
			if ((cCategory == ESymbolCategory.Future || cCategory == ESymbolCategory.IndexOption || cCategory == ESymbolCategory.StockOption || cCategory == ESymbolCategory.FutureOption || cCategory == ESymbolCategory.FutureRolover) && ConvertParameter.強制今日為期權到期日) {
				cExpiration = new DateTime(today.Year, today.Month, today.Day, cExpiration.Hour, cExpiration.Minute, cExpiration.Second);
			}

			if (__cDataRequest.Resolution.TotalSeconds == Resolution.MAX_BASE_TOTALSECONDS) {
				cFrom = today.AddDays(-1);
			}
			__cTimeQueue = __cDataRequest.Resolution.CalculateRealtimePeriods(this.LastBarTime, today, cExpiration);
			return __cTimeQueue;
		}

		internal void SetRange(int count) {
			__cDataRequest.Range.Count = count;
		}

		/// <summary>
		///   釋放腳本資源
		/// </summary>
		/// <param name="disposing">是否正在處理資源中</param>
		private void Dispose(bool disposing) {
			if (!this.__bDisposed) {
				__bDisposed = true;
				
				if (disposing) {
					__cTimes.Dispose();
					__cOpens.Dispose();
					__cHighs.Dispose();
					__cLows.Dispose();
					__cCloses.Dispose();
					__cVolumes.Dispose();
				}
			}
		}

		private void SetSeries(double open, double high, double low, double close, double volume, bool isRealtime = true) {
			int iIndex = (isRealtime) ? Indexer.RealtimeIndex : Indexer.HistoryIndex;
			if (isRealtime) {
				__cCloses.SetData(iIndex, close);
			} else {
				__cOpens.SetData(iIndex, open);
			}

			if (high > __cHighs[iIndex]) {
				__cHighs.SetData(iIndex, high);
			}

			if (low < __cLows[iIndex]) {
				__cLows.SetData(iIndex, low);
			}
			__cVolumes.SetData(iIndex, __cVolumes[iIndex] + volume);
		}
	}
}