using CSScriptLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SpiderHost
{
	class Program
	{
		static string baseurl = "http://opp.hegc1024.com/pw/";

		static Regex pagetotalReg = new Regex(@"Pages:\s*\(\s*1/(<?total>\d?)\s*total\s*\)");
		static Regex linkRegex = new Regex(@"htm_data/\d{1,2}/\d{4}/\d{5,}.html");
		//static Regex torlinkRegex = new Regex(@"http://www1\.newstorrentsspace\.info/freeone/file\.php/.{7}\.html");

		static void Main(string[] args)
		{
			List<string> lsgurl = new List<string>();

			if (Directory.Exists("spider")) Directory.CreateDirectory("spider");
			Directory.GetFiles("spider","*.cs").ToList().ForEach(s =>
				{
					dynamic script = new AsmHelper(CSScript.Load(s)).CreateObject("SpiderHost.script.spider");
					lsgurl.AddRange(script.getUrls());
				});
			

			//Parallel.For(1, 2, (i =>
			//{
			//	lsgurl.Add("http://opp.hegc1024.com/pw/thread.php?fid=5&page=" + i);
			//}));

			//Parallel.For(1,2, ( i=>{
			//	lsgurl.Add("http://opp.hegc1024.com/pw/thread.php?fid=22&page=" + i);
			//}));


			dowload_av(lsgurl.ToArray());

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("Done!");
			Console.ForegroundColor = ConsoleColor.White;
			Console.ReadLine();


		}

		private static void dowload_av(string[] urls)
		{
			HttpMethod m = new HttpMethod("GET");
			HttpClient httpClient = new HttpClient();

			List<string> lstpageHtml = new List<string>();
			List<string> lstcontenturl = new List<string>();

			string folderpath = string.Format("{0}/download", Environment.CurrentDirectory);
			if (!Directory.Exists(folderpath))
			{
				Directory.CreateDirectory(folderpath);
			}

			urls.ToList().ForEach(url =>
			{
				if (string.IsNullOrEmpty(url)) return;
				
				try
				{
					var taskget = httpClient.GetStreamAsync(url);
					var stream = taskget.Result;
					using (StreamReader sr = new StreamReader(stream))
					{
						lstpageHtml.Add(sr.ReadToEnd());
					}
					Console.WriteLine("get page " + url);
				}
				catch (Exception ex)
				{
					Console.WriteLine("get page list error: " + ex.Message);
				}
			});

			
			lstpageHtml.AsParallel().ForAll(
			x =>
			{

				//if (pagetotalReg.IsMatch(x))
				//{
				//	var pageTotalMatch = pagetotalReg.Match(x);
				//	int page = int.Parse(pageTotalMatch.Groups[1].Value.Trim());
				//}
				if (linkRegex.IsMatch(x))
				{
					foreach (Match link in linkRegex.Matches(x))
					{
						lstcontenturl.Add(link.Value);

					}

				}

			});
			HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
			List<string> lstImgurl = new List<string>();
			StringBuilder sb = new StringBuilder();
			lstcontenturl.AsParallel().ForAll(link =>
			{
				try { 
					parsecontent(httpClient, folderpath, htmlDoc, lstImgurl, link);
					}
				catch(Exception e)
				{
					Console.WriteLine(e.Message);
				}
				



				//if (torlinkRegex.IsMatch(content))
				//{
				//	var match = torlinkRegex.Match(content);
				//	sb.AppendFormat("{0} - {1}", picName, match.Value);
				//	sb.AppendLine();
				//	sb.AppendFormat("{0} - {1}", picName, contentNode.InnerText.Trim());
				//	sb.AppendLine();
				//}
			});

		}

		private static void parsecontent(HttpClient httpClient, string folderpath, HtmlAgilityPack.HtmlDocument htmlDoc, List<string> lstImgurl, string link)
		{
			int a = link.LastIndexOf("/");
			var name = link.Substring(a + 1);
			var b = name.IndexOf(".");
			name = name.Substring(0, b);

			string urlPageName = name;
			string resname = Path.Combine(folderpath, urlPageName);
			string contenturl = baseurl + link;

			string txtfullname = resname + ".txt";
			if (File.Exists(txtfullname))
			{
				Console.WriteLine("page {0} has download =>{1}", link, urlPageName);
				return;
			}

			try
			{
				var taskget = httpClient.GetStreamAsync(contenturl);
				htmlDoc.Load(taskget.Result, Encoding.UTF8);

				Console.WriteLine("load html " + contenturl);
			}
			catch (Exception ex)
			{
				Console.WriteLine("load html error: " + ex.Message);
				return;
			}
			var contentNode = htmlDoc.GetElementbyId("read_tpc");


			var content = contentNode.InnerHtml;
			try
			{
				FileStream fs = File.OpenWrite(txtfullname);
				byte[] torbytes = Encoding.UTF8.GetBytes(content);
				fs.Write(torbytes, 0, torbytes.Count());
				fs.Flush();
				fs.Close();
				fs.Dispose();
			}
			catch (Exception ex)
			{
				Console.WriteLine("get torrent failed! " + ex.Message);
			}

			int imgIndex = 0;
			contentNode.Elements("img").ToList().ForEach(e =>
			{
				try
				{
					var imgurl = e.Attributes["src"].Value;
					var imgstream = httpClient.GetStreamAsync(imgurl);



					var downImgname = resname + "-" + (++imgIndex) + ".jpg";

					lstImgurl.Add(imgurl);
					FileStream fsimg = File.OpenWrite(downImgname);


					imgstream.Result.CopyTo(fsimg);

					if (fsimg.Length < 100)
					{
						return;

					}

					fsimg.Flush();
					fsimg.Close();
					fsimg.Dispose();

					Console.WriteLine("save img => " + downImgname);
				}
				catch (Exception ex)
				{
					Console.WriteLine("get img {0} failed! {1} ", lstImgurl, ex.Message);

				}
			});
		}
	}
}
