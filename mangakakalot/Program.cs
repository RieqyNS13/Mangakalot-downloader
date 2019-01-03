using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace mangakakalot
{
	class Program
	{
		static string contentType;
		static Stream get(string url, ref string error)
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			HttpWebRequest req;
			Stream stream;
			HttpWebResponse resp = null;
			try
			{
				req = HttpWebRequest.Create(url) as HttpWebRequest;
				req.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
				req.AllowAutoRedirect = true;
				req.UserAgent = "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36";
				resp = req.GetResponse() as HttpWebResponse;
				responCode = resp.StatusCode;
				contentType = resp.ContentType;
				stream = resp.GetResponseStream();
				return stream;
			}
			catch (WebException Wex)
			{
				try
				{
					resp = Wex.Response as HttpWebResponse;
					responCode = resp.StatusCode;
					contentType = null;
					return resp.GetResponseStream();
				}
				catch (Exception ex1)
				{
					responCode = 0;
					contentType = null;
					return null;
				}
			}
			catch (Exception ex)
			{
				
				error = ex.ToString();
				responCode = 0;
				contentType = null;
				return null;
			}
		}
		static HttpStatusCode responCode;
		static string getTextFromSteam(Stream stream)
		{
			try
			{
				StreamReader reader = new StreamReader(stream, ASCIIEncoding.ASCII);
				string asu = reader.ReadToEnd();
				reader.Close();
				return asu;
			}
			catch
			{
				return null;
			}
		}
		static List<string> getListChapter(string curl, string asu)
		{
			List<string> list = new List<string>();
			MatchCollection matches = Regex.Matches(curl, "<span><a href=\"(.*?)\"", RegexOptions.IgnoreCase);
			string[] jembut = new string[matches.Count];
			for (int i = 0; i < matches.Count; i++)
			{
				jembut[i] = matches[matches.Count - 1 - i].Groups[1].Value;
			}
			return jembut.ToList();
		}
		static void Main(string[] args)
		{
			Console.Title = "Mangakakalot Downloader by RieqyNS13";
			string url;
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write("[+] Masukkan url (contoh: http://mangakakalot.com/manga/aho_girl/) = ");
			url = Console.ReadLine();
			string error = null;
			string curl = getTextFromSteam(get(url.Trim(), ref error));
		
			if (curl == null)
			{
				if (responCode == HttpStatusCode.NotFound) Console.WriteLine("[+] Not Found anjing");
				else Console.WriteLine("[+] Koneksi cacat njing. respon code "+responCode.ToString());
				Console.Read();
				return;
			}
			List<string> listChapter = getListChapter(curl, url.Trim().ToLower());
			List<string> listChaptertext = new List<string>();
			Console.WriteLine("[+] Jumlah chapter : " + listChapter.Count);
			Console.ForegroundColor = ConsoleColor.Cyan;
			if (listChapter.Count == 0)
			{
				Console.WriteLine("[+] Kosong cuk");
				Console.Read();
				return;
			}
			else
			{
				Console.WriteLine();
				Match x = Regex.Match(curl, "<div class=\"chapter-list\">(.+?)<div class=\"comment-info\">", RegexOptions.Singleline);

				Regex r = new Regex("\">(.+?)</a>");
				MatchCollection m = r.Matches(x.Groups[1].Value);
				for (int i = m.Count - 1; i >= 0; i--)
				{
					Console.WriteLine("[" + (m.Count - 1 - i) + "] " + m[i].Groups[1].Value);
					listChaptertext.Add(m[i].Groups[1].Value);
				}
				Console.WriteLine();
			}
			Console.ForegroundColor = ConsoleColor.Green;
			string range;
			int begin = 0, end = 0;
			do
			{
				Console.Write("[+] Range chapter yang didownload (contoh: 0-11) = ");
				range = Console.ReadLine();
			} while (!cekInputRange(listChapter, range, ref begin, ref end));

			do
			{
				Console.Write("[+] Tulis folder untuk menyimpan manga (contoh: f:\\manga) = ");
				foldersimpan = Console.ReadLine().Trim();
			} while (!Directory.Exists(foldersimpan.Trim()));
			string timpa;
			do
			{
				Console.Write("[+] Timpa file yang sudah ada ? [y/n] = ");
				timpa = Console.ReadLine().Trim().ToLower();
			} while (timpa != "y" && timpa != "n");
			proses(listChapter, listChaptertext, begin, end, timpa == "y" ? true : false);
			Console.Read();

		}
		static string foldersimpan;
		static void downloadGambar(string url, int nomer, string pathfolder)
		{
			try
			{
				int i = 0; bool sukses = false; string error = null;
				while (!sukses && i < 10)
				{
					string file;
					Stream streamImg = get(url, ref error);
					if (responCode == HttpStatusCode.OK)
					{
						switch (contentType)
						{
							case "image/jpeg":
								file = pathfolder + "/" + nomer + ".jpg";
								htmlCok += "<img src=\"" + nomer + ".jpg\"><br>";
								break;
							default:
								file = pathfolder + "/" + nomer + ".png";
								htmlCok += "<img src=\"" + nomer + ".png\"><br>";
								break;
						}
						using (var fs = File.Create(file))
						{
							streamImg.CopyTo(fs);
							sukses = true;
							Console.WriteLine("Completed");
						}
					}
					else if (streamImg != null)
					{
						Console.WriteLine("Invalid");
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("Gak konek internet cuk");
						Console.ForegroundColor = ConsoleColor.Green;
						Console.Read();
					}
					i++;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

		}
		static string removeIllegal(string asu)
		{
			string[] cok = new string[] { "<", ">", ":", "\"", "/", "\\", "|", "?", "*" };
			foreach (string q in cok)
			{
				asu = Regex.Replace(asu, Regex.Escape(q), string.Empty);
			}
			return asu;
		}
		static string htmlCok;
		static List<string> getGambarPerChapter(string urlChapter)
		{
			List<string> asu = new List<string>();
			try
			{
				string awal = "1.html";
				bool jalan = true;
				Console.ForegroundColor = ConsoleColor.Yellow;
				do
				{
					string error = null;
					string curl = null;
					do
					{
						curl = getTextFromSteam(get(urlChapter, ref error));
						if (responCode == HttpStatusCode.ServiceUnavailable)
						{
							Thread.Sleep(1000);
							Console.WriteLine("..");
						}
						else if (curl == null)
						{
							Console.ForegroundColor = ConsoleColor.Red;
							Console.WriteLine("Koneksi bermasalah njing !, Cek maneh cok");
							Console.Read();
						}
					} while (curl == null);
					Console.ForegroundColor = ConsoleColor.Yellow;
					Match m = Regex.Match(curl, "<img src=\"(.+?)\"");
					if (m.Groups.Count == 2)
					{
						asu.Add(m.Groups[1].Value);
						Console.WriteLine(urlChapter + " > " + m.Groups[1].Value);
					}
					Regex r = new Regex("<a href=\"(\\d+\\.html)\" class=\"btn next_page\"", RegexOptions.IgnoreCase);
					Match m2 = r.Match(curl);
					if (m2.Groups.Count == 2)
					{
						urlChapter = urlChapter.Replace(awal, m2.Groups[1].Value);
						awal = m2.Groups[1].Value;
					}
					else jalan = false;

				} while (jalan);
				Console.ForegroundColor = ConsoleColor.Green;
				return asu;
			}
			catch (Exception e)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine(e.ToString());
				return asu;
			}
		}
		static void proses(List<string> list, List<string> text, int begin, int end, bool timpa)
		{
			try
			{

				for (int i = begin; i <= end; i++)
				{
					string error = null;
					Console.WriteLine("[" + i + "] " + text[i]);
					string curl = getTextFromSteam(get(list[i], ref error));
					Match x = Regex.Match(curl, "<div class=\"vung-doc\" id=\"vungdoc\">(.+?)</div>", RegexOptions.Singleline);
					Regex r = new Regex("<img src=\"(.+?)\"", RegexOptions.IgnoreCase);
					MatchCollection matches = r.Matches(x.Groups[1].Value);
					int j = 0;
					string dir = removeIllegal(text[i]);
					if (!Directory.Exists(foldersimpan + "/" + dir)) Directory.CreateDirectory(foldersimpan + "/" + dir);
					htmlCok = "<html><head><title>" + dir + "</title></head><body bgcolor=\"#000000\"><center>";
					string pathfolder = foldersimpan + "/" + dir;
					foreach (Match m in matches)
					{

						string url = m.Groups[1].Value;
						Console.Write("[+] " + url + " -> ");
						if ((File.Exists(pathfolder + "/" + j + ".jpg") || File.Exists(pathfolder + "/" + j + ".png")) && !timpa)
						{
							string z = "jpg"; ;
							if (File.Exists(pathfolder + "/" + j + ".jpg")) z = "jpg";
							else if (File.Exists(pathfolder + "/" + j + ".png")) z = "png";
							htmlCok += "<img src=\"" + j + "." + z + "\"><br>";
							Console.WriteLine(" Sudah ada");
							j++;
							continue;
						}
						else
						{
							if (!Regex.IsMatch(url, "^(http:|https:)", RegexOptions.IgnoreCase)) url = "http:" + url;
							downloadGambar(url, j, pathfolder);
							j++;
						}

					}
					htmlCok += "</body></center></html>";
					File.WriteAllText(pathfolder + "/" + dir + ".html", htmlCok);

				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
			finally
			{
				Console.WriteLine("\n[+] Proses completed");
			}
		}
		static bool cekInputRange(List<string> list, string range, ref int begin, ref int end)
		{
			Regex r = new Regex("^(\\d+)\\-(\\d+)$");
			Match m = r.Match(range);
			int x = 0, y = 0;
			if (m.Success)
			{
				x = Convert.ToInt16(m.Groups[1].Value);
				y = Convert.ToInt16(m.Groups[2].Value);
				if (x > y)
				{
					Console.WriteLine("Angka pertama harus lebih kecil atau sama dengan angka terakhir");
					return false;
				}
				else if (x < 0 || y > list.Count - 1)
				{
					Console.WriteLine("Range harus diantara 0 sampai Range maksimal");
					return false;
				}
			}
			else
			{
				Console.WriteLine("Format salah njing");
				return false;
			}
			begin = x;
			end = y;
			return true;
		}
	}
}
