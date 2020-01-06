using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using log4net.Config;
using PowerLanguage;
using Zeghs.IO;
using Zeghs.Data;
using Zeghs.Utils;
using Zeghs.Products;
using Zeghs.Services;
using Zeghs.Managers;
using Zeghs.Informations;
using System.IO;

namespace Converter {
	class Program {
		private static ManualResetEvent __cManualEvent = new ManualResetEvent(false);

		static void Main(string[] args) {
			XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo("log4net_config.xml"));

			ServicePointManager.DefaultConnectionLimit = 1024;

			ProductManager.Load("exchanges");
			QuoteManager.Manager.Refresh("plugins\\quotes");

			QuoteServiceInformation[] cQuoteServiceInfos = QuoteManager.Manager.GetQuoteServiceInformations();
			QuoteManager.Manager.StartQuoteService(cQuoteServiceInfos[0]);
			AbstractQuoteService cService = QuoteManager.Manager.GetQuoteService(cQuoteServiceInfos[0].DataSource);
			cService.Load();
			cService.onLoginCompleted += Service_onLoginCompleted;
			
			Zeghs.Settings.GlobalSettings.Load();

			if (args.Length > 0 && args[0].Equals("/admin")) {
				int iCommand = PrintMenu();
				if (iCommand > 0) {
					ExcuteCommand(iCommand);
				}
			} else {
				bool bEvent = __cManualEvent.WaitOne(60000);
				FuturesRptAdapter.Convert(DateTime.Now);
			}

			//DumpDataUtil.Load("TXF0.tw", true, new DateTime(2017, 12, 22, 8, 45, 0), new DateTime(2017, 12, 26, 13, 45, 0));
			//DumpDataUtil.Save("TXF0.tw", true, "abc.txt", new DateTime(2013,11,20,8,45,0));

			//FuturesCsvAdapter.Convert();  //轉換CSV使用

			//bool bEvent = __cManualEvent.WaitOne(60000);

			/*
			MitakeSourceAdapter cAdapter = new MitakeSourceAdapter();
			DateTime cStartDate = new DateTime(2012, 1, 1);
			DateTime cEndDate = new DateTime(2014, 12, 31);

			while (cStartDate <= cEndDate) {
				cAdapter.Load(cStartDate);
				cStartDate = cStartDate.AddSeconds(86400);
			}
			cAdapter.Dispose();

			//*/
			//System.Console.WriteLine("Completed...");
			//System.Console.ReadLine();
			//return;

			__cManualEvent.Dispose();
			QuoteManager.Manager.CloseAll();
		}

		static void ExcuteCommand(int command) {
			switch (command) {
				case 1:
					RunCommand_01();
					break;
				case 2:
					RunCommand_02();
					break;
				case 3:
					RunCommand_03();
					break;
			}
		}

		static void RunCommand_01() {
			ConvertParameter.強制今日為期權到期日 = true;

			Console.Write("請輸入到期的合約月份:");
			string sNumber = Console.ReadLine();
			int iMonth = 0;
			int.TryParse(sNumber, out iMonth);

			ConvertParameter.自訂期權合約月份 = iMonth;
		}

		static void RunCommand_02() {
			FuturesRptAdapter.Convert(DateTime.Now, false);
		}

		static void RunCommand_03() {
			Console.Write("請輸入指定日期(YYYY-MM-DD):");
			string sDate = Console.ReadLine();
			FuturesRptAdapter.Convert(DateTime.Parse(sDate), true);
		}

		static int PrintMenu() {
			Console.WriteLine("== [管理者功能選單]");
			Console.WriteLine("1. 修改期權商品到期日為今天");
			Console.WriteLine("2. 匯入今日RPT壓縮檔並轉檔資料");
			Console.WriteLine("3. 輸入指定日下載壓縮檔並轉檔資料");
			Console.WriteLine();
			Console.Write("請輸入選單號碼:");
			string sNumber = Console.ReadLine();

			int iNumber = 0;
			int.TryParse(sNumber, out iNumber);
			return iNumber;
		}

		static void Service_onLoginCompleted(object sender, EventArgs e) {
			__cManualEvent.Set();
		}
	}
}