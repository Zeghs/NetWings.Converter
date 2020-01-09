using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using log4net;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using Zeghs.Settings;

namespace Zeghs.Utils {
	internal sealed class Compression {
		private static readonly ILog logger = LogManager.GetLogger(typeof(Compression));

		internal static bool ExtractFrom7Zip(string zipFile, string outFolder) {
			bool bRet = false;
			Process cProcess = null;

			try {
				cProcess = new Process();
				cProcess.StartInfo.FileName = GlobalSettings.Settings.SevenZipPath + "7z.exe";
				cProcess.StartInfo.Arguments = string.Format("x {0} -o{1} -y", zipFile, outFolder);
				cProcess.StartInfo.UseShellExecute = false;
				cProcess.StartInfo.CreateNoWindow = true;
				cProcess.Start();
				cProcess.WaitForExit();
				
				bRet = true;
			} catch (Exception __errExcep) {
				if (logger.IsErrorEnabled) logger.ErrorFormat("{0}/r/n{1}", __errExcep.Message, __errExcep.StackTrace);
			} finally {
				cProcess.Close();
			}
			return bRet;
		}

		internal static bool Extract(string zipFile, string password, string outFolder) {
			bool bRet = false;
			ZipFile cZipFile = null;
			
			try {
				FileStream cStream = File.OpenRead(zipFile);
				cZipFile = new ZipFile(cStream);
				
				if (!String.IsNullOrEmpty(password)) {
					cZipFile.Password = password;
				}

				byte[] arrBuffer = new byte[4096];
				foreach (ZipEntry zipEntry in cZipFile) {
					if (!zipEntry.IsFile) {
						continue;
					}
					
					string entryFileName = zipEntry.Name;
					string fullZipToPath = Path.Combine(outFolder, entryFileName);
					string directoryName = Path.GetDirectoryName(fullZipToPath);
					if (directoryName.Length > 0) {
						if (!Directory.Exists(directoryName)) {
							Directory.CreateDirectory(directoryName);
						}
					}

					Stream cZipStream = cZipFile.GetInputStream(zipEntry);
					using (FileStream streamWriter = File.Create(fullZipToPath)) {
						StreamUtils.Copy(cZipStream, streamWriter, arrBuffer);
					}
				}
				bRet = true;
			} catch (Exception __errExcep) {
				if (logger.IsErrorEnabled) logger.ErrorFormat("{0}/r/n{1}", __errExcep.Message, __errExcep.StackTrace);
			} finally {
				if (cZipFile != null) {
					cZipFile.IsStreamOwner = true;
					cZipFile.Close();
				}
			}
			return bRet;
		}
	}
}