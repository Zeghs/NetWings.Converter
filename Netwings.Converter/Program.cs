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

namespace Converter {
	class Program {
		private static ManualResetEvent __cManualEvent = new ManualResetEvent(false);

		static void Main(string[] args) {
			XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo("log4net_config.xml"));

			ServicePointManager.DefaultConnectionLimit = 1024;
			
			ProductManager.Load("exchanges");
			QuoteManager.Manager.Refresh("plugins\\quotes");

			if (args.Length > 0 && args[0].Equals("/admin")) {
			        int iCommand = PrintMenu();
			        if (iCommand > 0) {
			                ExcuteCommand(iCommand);
			        }
			}

			QuoteServiceInformation[] cQuoteServiceInfos = QuoteManager.Manager.GetQuoteServiceInformations();
			QuoteManager.Manager.StartQuoteService(cQuoteServiceInfos[0]);
			AbstractQuoteService cService = QuoteManager.Manager.GetQuoteService(cQuoteServiceInfos[0].DataSource);
			cService.Load();
			cService.onLoginCompleted += Service_onLoginCompleted;
			
			Zeghs.Settings.GlobalSettings.Load();

			//DumpDataUtil.Load("TXW0C8250.tw", false, new DateTime(2012, 1, 2, 8, 45, 0), new DateTime(2014, 12, 31, 13, 45, 0));
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

			FuturesRptAdapter.Convert(DateTime.Now);

			__cManualEvent.Dispose();
			QuoteManager.Manager.CloseAll();
		}

		static void ExcuteCommand(int command) {
			switch (command) {
				case 1:
					RunCommand_01();
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

		static int PrintMenu() {
			Console.WriteLine("== [管理者功能選單]");
			Console.WriteLine("1. 修改期權商品到期日為今天");
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