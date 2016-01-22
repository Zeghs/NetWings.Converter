using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mitake.Sockets.Data;

namespace Zeghs.Utils {
	internal sealed class EncodeUtil {
		private static byte[] __bArray = new byte[128]; //暫存陣列(用來轉換UTF8格式)

		public static void EncodeSymbol(PacketBuffer Buffer) {
			if (Buffer.Data[3] < 20) { //以前的舊格式都忽略掉(只解碼新格式)
				Buffer.Length = 0; //舊格式不傳送
				return;
			}

			//取得股票名稱
			string sName = Encoding.GetEncoding("big5").GetString(Buffer.Data, 13, 6).Trim();

			//轉換成UTF8格式
			Array.Clear(__bArray, 0, 9);
			Encoding.UTF8.GetBytes(sName, 0, sName.Length, __bArray, 0);
			Array.Copy(Buffer.Data, 19, __bArray, 9, Buffer.Length - 19);
			Array.Copy(__bArray, 0, Buffer.Data, 13, Buffer.Length - 10);

			//修正封包長度(UTF8格式比Big5格式還要長[一個中文字佔三個bytes])
			Buffer.Data[3] += 3;
			Buffer.Length += 3;

			SetChecksum(Buffer);
		}
		
		/// <summary>
		///    設定檢查碼 
		/// </summary>
		/// <param name="item">封包資料</param>
		private static void SetChecksum(PacketBuffer item) {
			SetChecksum(item, 0);
		}

		/// <summary>
		///    設定檢查碼 
		/// </summary>
		/// <param name="item">封包資料</param>
		/// <param name="StartIndex">起始位置</param>
		private static void SetChecksum(PacketBuffer item, int StartIndex) {
			int iSum = 0;
			int iCheckSum = 0x100;
			int iBSize = item.Data[StartIndex + 3] - 1;   //取得封包的資料內容長度
			int iIndex = StartIndex + 3;

			for (int i = 0; i < iBSize; i++)         //計算CheckSum
				iSum += item.Data[iIndex + i];

			iCheckSum -= (iSum & 0xff);              //算出正確檢查碼
			item.Data[iIndex + iBSize] = (byte) iCheckSum; //填入檢查碼
		}
	}
}