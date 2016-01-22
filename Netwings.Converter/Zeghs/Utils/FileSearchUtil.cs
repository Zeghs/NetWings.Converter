using System;
using System.IO;

namespace Zeghs.Utils {
	internal sealed class FileSearchUtil {
		/// <summary>
		///   二分搜尋法
		/// </summary>
		/// <param name="stream">檔案串流類別</param>
		/// <param name="count">資料最大 Block 區塊個數(以 blockSize 為一個單位區塊)</param>
		/// <param name="blockSize">Block 區塊大小</param>
		/// <param name="time">要搜尋的時間</param>
		/// <returns>返回值:true=有找到, false=沒找到</returns>
		internal static bool BinarySearch(FileStream stream, long count, int blockSize, DateTime time) {
			bool bRet = false;
			if (count == 0) {
				return bRet;
			}

			byte[] bDate = new byte[4];
			long lLeft = 0, lMiddle = 0, lRight = count;
			ZBuffer cBuffer = new ZBuffer(16);

			while (lLeft <= lRight) {
				lMiddle = (lLeft + lRight) / 2;
				cBuffer.Position = 0;

				stream.Position = lMiddle * blockSize;
				stream.Read(cBuffer.Data, 0, 8);
				DateTime cTime = cBuffer.GetDateTime();

				int iCompare = cTime.CompareTo(time);
				if (iCompare == 0) {
					stream.Seek(-8, SeekOrigin.Current);  //因為已經讀取的時間日期, 所以 Position 會被移動 8bytes 要在移動回去至日期處, 才是資料 Block 的起點
					bRet = true;
					break;
				} else if (iCompare < 0) {
					lLeft = lMiddle + 1;
				} else {
					lRight = lMiddle - 1;
				}
			}
			return bRet;
		}

		/// <summary>
		///   二分搜尋法(取逼近值)
		/// </summary>
		/// <param name="stream">檔案串流類別</param>
		/// <param name="count">資料最大 Block 區塊個數(以 blockSize 為一個單位區塊)</param>
		/// <param name="blockSize">Block 區塊大小</param>
		/// <param name="time">要搜尋的時間</param>
		/// <returns>返回值:true=逼近, false=已經有目標</returns>
		internal static bool BinaryNearSearch(FileStream stream, long count, int blockSize, DateTime time) {
			if (count == 0) {
				return false;
			}

			byte[] bDate = new byte[4];
			long lLeft = 0, lMiddle = 0, lRight = count;
			ZBuffer cBuffer = new ZBuffer(16);

			while (lLeft <= lRight) {
				lMiddle = (lLeft + lRight) / 2;
				cBuffer.Position = 0;

				stream.Position = lMiddle * blockSize;
				stream.Read(cBuffer.Data, 0, 8);
				DateTime cTime = cBuffer.GetDateTime();

				int iCompare = cTime.CompareTo(time);
				if (iCompare == 0) {
					stream.Seek(-8, SeekOrigin.Current);  //因為已經讀取的時間日期, 所以 Position 會被移動 8bytes 要在移動回去至日期處, 才是資料 Block 的起點
					return false;
				} else if (iCompare < 0) {
					lLeft = lMiddle + 1;
				} else {
					lRight = lMiddle - 1;
				}
			}
			stream.Position = lLeft * blockSize;
			return true;
		}

		internal static void SearchNextDate(FileStream stream, int blockSize, DateTime time) {
			byte[] bDate = new byte[4];
			ZBuffer cBuffer = new ZBuffer(16);

			int iSize = 0;
			long lCurrent = stream.Position / blockSize;
			while ((iSize = stream.Read(cBuffer.Data, 0, 8)) > 0) {
				cBuffer.Position = 0;
				cBuffer.Length = iSize;
				DateTime cTime = cBuffer.GetDateTime();
				if ((cTime - time).TotalSeconds >= 86400) {
					stream.Seek(-8, SeekOrigin.Current);  //因為已經讀取的時間日期, 所以 Position 會被移動 8bytes 要在移動回去至日期處, 才是資料 Block 的起點
					break;
				}

				++lCurrent;
				stream.Position = lCurrent * blockSize;
			}
		}
	}
}