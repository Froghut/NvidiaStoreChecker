using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace NvidiaStoreChecker
{
	class Program
	{
		static void Main(string[] args)
		{
			HashSet<string> openedURLs = new HashSet<string>();

			Random r = new Random();

			bool sleeping = false;
			Console.WriteLine("Looking for Graphics Cards...");

			while (true)
			{
				try
				{
					if ((DateTime.Now.Hour < 9 || DateTime.Now.Hour >= 18) && !sleeping)
					{
						sleeping = true;
						Console.WriteLine("Sleeping...");
					}

					while (DateTime.Now.Hour < 9 || DateTime.Now.Hour >= 18)
					{
						Thread.Sleep(60 * 1000);
					}

					if (sleeping)
					{
						sleeping = false;
						Console.WriteLine("Looking for cards again!");
					}


					WebClient wc = new WebClient();
					wc.Headers.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.93 Safari/537.36 Edg/96.0.1054.43");
					wc.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
					wc.Headers.Add("Host","api.store.nvidia.com");
					wc.Headers.Add("Accept-Language","en-US,en;q=0.5");
					wc.Headers.Add("Accept-Encoding","deflate, br");
			
					wc.Headers.Add("Sec-Fetch-Dest","document");
					wc.Headers.Add("Sec-Fetch-Mode","navigate");
					wc.Headers.Add("Sec-Fetch-Site","none");
					wc.Headers.Add("Sec-Fetch-User","?1");

					wc.Headers.Add("DNT","1");

					wc.Headers.Add("Via","1.1 244.165.236.54");
					wc.Headers.Add("X-Forwarded-For","244.165.236.54");

					string ip = "244."+r.Next(1, 240)+".236." + r.Next(1, 240);
					wc.Headers["Via"] = "1.1 " + ip;
					wc.Headers["X-Forwarded-For"] = ip;
					string json = wc.DownloadString("https://api.store.nvidia.com/partner/v1/feinventory?skus=DE&locale=DE");
					NvidiaStoreJson nvidiaStoreJson = System.Text.Json.JsonSerializer.Deserialize<NvidiaStoreJson>(json);
					if (nvidiaStoreJson != null && nvidiaStoreJson.success)
					{
						#if DEBUG
						Console.WriteLine(DateTime.Now + " success");
						#endif
						foreach (ListMap map in nvidiaStoreJson.listMap)
						{
							if ((map.fe_sku.Contains("NVGFT070") || map.fe_sku.Contains("NVGFT080") || map.fe_sku.Contains("NVGFT060")) && map.is_active == "true")
							{
								if (!openedURLs.Contains(map.product_url))
								{
									openedURLs.Add(map.product_url);
									OpenUrl(map.product_url);
									RawSourceWaveStream importer = new RawSourceWaveStream(Resource1.alarm, new WaveFormat());
									WaveOut waveOut = new WaveOut();
									waveOut.Init(importer);
									waveOut.Play();
									waveOut.PlaybackStopped += (sender, eventArgs) =>
									                           {
										                           waveOut.Dispose();
										                           importer.Dispose();
									                           };
								}
							}
						}
					}
					else
					{
						Console.WriteLine(DateTime.Now + " Did not get good json!");
						Console.WriteLine("---------");
						Console.WriteLine(json);
						Console.WriteLine("---------");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(DateTime.Now + " ---------");
					Console.WriteLine(ex);
					Console.WriteLine("---------");
				}

				Thread.Sleep(5000);
			}

			
		}

		private static void OpenUrl(string url)
		{
			try
			{
				Process.Start(url);
			}
			catch
			{
				// hack because of this: https://github.com/dotnet/corefx/issues/10361.net 6 pack everything into one exe

				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					url = url.Replace("&", "^&");
					Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				{
					Process.Start("xdg-open", url);
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					Process.Start("open", url);
				}
				else
				{
					throw;
				}
			}
		}
	}

	public class ListMap
	{
		public string is_active { get; set; }
		public string product_url { get; set; }
		public string price { get; set; }
		public string fe_sku { get; set; }
		public string locale { get; set; }
	}

	public class NvidiaStoreJson
	{
		public bool success { get; set; }
		public IList<ListMap> listMap { get; set; }
	}
}
