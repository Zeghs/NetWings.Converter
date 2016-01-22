using System;
using System.IO;
using System.Text;
using log4net;
using Zeghs.IO;

namespace Zeghs.Utils {
	internal sealed class DumpDataUtil {
		private static readonly ILog logger = LogManager.GetLogger(typeof(FileAdapter));

		internal static void Load(string symbolId, bool isMinute) {
			Load(symbolId, isMinute, DateTime.MinValue, DateTime.MaxValue);
		}

		internal static void Load(string symbolId, bool isMinute, DateTime startDate, DateTime endDate) {
			try {
				string sFile = string.Format("{0}\\{1}\\{2}", Settings.GlobalSettings.Settings.DataPath, (isMinute) ? "mins" : "days", symbolId);
				if (File.Exists(sFile)) {
					using (FileStream cStream = new FileStream(sFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
						ZBuffer cBuffer = new ZBuffer(64);

						while (cStream.Read(cBuffer.Data, 0, FileAdapter.MAX_BLOCK_SIZE) > 0) {
							cBuffer.Position = 0;
							cBuffer.Length = FileAdapter.MAX_BLOCK_SIZE;
							DateTime cTime = cBuffer.GetDateTime();
							if (cTime >= startDate && cTime <= endDate) {
								double dOpen = cBuffer.GetDouble();
								double dHigh = cBuffer.GetDouble();
								double dLow = cBuffer.GetDouble();
								double dClose = cBuffer.GetDouble();
								double dVolume = cBuffer.GetDouble();

								System.Console.WriteLine("{0} {1,8:0.00} {2,8:0.00} {3,8:0.00} {4,8:0.00} {5,7}", cTime.ToString("yyyyMMdd HHmmss"), dOpen, dHigh, dLow, dClose, dVolume);
								if (cTime == endDate) {
									break;
								}
							}
						}
					}
				}
			} catch (Exception __errExcep) {
				if (logger.IsErrorEnabled) logger.ErrorFormat("{0}/r/n{1}", __errExcep.Message, __errExcep.StackTrace);
			}
		}
		
		internal static void Save(string symbolId, bool isMinute, string file, DateTime targetDate) {
			try {
				string sFile = string.Format("{0}\\{1}\\{2}", Settings.GlobalSettings.Settings.DataPath, (isMinute) ? "mins" : "days", symbolId);
				if (File.Exists(sFile)) {
					DateTime cEndDate = targetDate.AddSeconds(86400);
					using (FileStream cStream = new FileStream(sFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
						ZBuffer cBuffer = new ZBuffer(64);

						while (cStream.Read(cBuffer.Data, 0, FileAdapter.MAX_BLOCK_SIZE) > 0) {
							cBuffer.Position = 0;
							cBuffer.Length = FileAdapter.MAX_BLOCK_SIZE;
							DateTime cTime = cBuffer.GetDateTime();
							if (cTime >= targetDate && cTime <= cEndDate) {
								double dOpen = cBuffer.GetDouble();
								double dHigh = cBuffer.GetDouble();
								double dLow = cBuffer.GetDouble();
								double dClose = cBuffer.GetDouble();
								double dVolume = cBuffer.GetDouble();

								File.AppendAllText(file, string.Format("{0},{1},{2},{3},{4},{5}\r\n", cTime.ToString("yyyy-MM-dd HH:mm:ss"), dOpen, dHigh, dLow, dClose, dVolume), Encoding.UTF8);
								System.Console.WriteLine("{0} {1,8:0.00} {2,8:0.00} {3,8:0.00} {4,8:0.00} {5,7}", cTime.ToString("yyyyMMdd HHmmss"), dOpen, dHigh, dLow, dClose, dVolume);

								if (cTime == cEndDate) {
									break;
								}
							}
						}
					}
				}
			} catch (Exception __errExcep) {
				if (logger.IsErrorEnabled) logger.ErrorFormat("{0}/r/n{1}", __errExcep.Message, __errExcep.StackTrace);
			}
		}
	}
}