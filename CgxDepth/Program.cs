using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StarFox.Interop.GFX;
using StarFox.Interop.GFX.DAT;
using StarFox.Interop.MISC;

namespace CgxDepth
{
	internal static class Program
	{
		private static readonly IDictionary<string, byte> s_dicKnown = InitKnownDepths();

		static async Task Main(string[] args)
		{
			var strWorkDir = WorkDirectory(args);
			Console.WriteLine("Directory: {0}", strWorkDir);
			var strarCgx = Directory.GetFiles(strWorkDir, "*.CGX", SearchOption.AllDirectories);
			Console.WriteLine("CGX files: {0}", strarCgx.Length);
			var lstCgx = new List<FXCGXFile>(strarCgx.Length);
			var intzzKnown = new List<int>[3];
			intzzKnown[0] = new List<int>();
			intzzKnown[1] = new List<int>();
			intzzKnown[2] = new List<int>();
			for (int i = 0; i < strarCgx.Length; i++) {
				var cgx = await CgxInfo(strarCgx[i], strWorkDir);
				if (cgx != null) {
					lstCgx.Add(cgx);
				}
				var fi = new FileInfo(strarCgx[i]);
				var d = KnownDepth(fi, strarCgx[i].Replace(strWorkDir + Path.DirectorySeparatorChar, ""));
				if ((d != null) && (fi.Length != 34048)) {
					intzzKnown[(int)(Math.Log(d.Value, 2) - 1)].Add((int)fi.Length);
				}
			}
			Console.WriteLine(CgxLoadingStatTable(lstCgx, intzzKnown));
		}

		private static async Task<FXCGXFile> CgxInfo(string cgxFile, string workDirectory)
		{
			var fi = new FileInfo(cgxFile);
			var strRelPath = cgxFile.Replace(workDirectory + Path.DirectorySeparatorChar, "");
			var t = Utility.TrimEnd(File.ReadAllBytes(cgxFile)).Length;
			var cgx = SFGFXInterface.OpenCGX(cgxFile) ?? await SFGFXInterface.TryImportFoxCGX(cgxFile);
			var d = KnownDepth(fi, strRelPath);
			if (cgx != null) {
				Console.WriteLine("{0,5} bytes\t{1,5} trimmed\t{2} bpp\t({3,1} bpp)\t{4,-10}\t{5}",
					fi.Length, t, cgx.GetBitDepth(), d, cgx.LoadingStrategy, strRelPath);
			} else {
				Console.WriteLine("{0,5} bytes\t{1,5} trimmed\t_ bpp\t({2,1} bpp)\tUnknown\t\t{3}",
					fi.Length, t, d, strRelPath);
			}
			return cgx;
		}



		private static string WorkDirectory(string[] args)
		{
			string directory = null;
			if ((args != null) && (args.Length > 0) && (!String.IsNullOrWhiteSpace(args[0])) && Directory.Exists(args[0])) {
				directory = args[0];
			} else if (File.Exists("recent.txt")) {
				var strArg = File.ReadAllText("recent.txt");
				if ((!String.IsNullOrWhiteSpace(strArg)) && Directory.Exists(strArg.Trim())) {
					directory = strArg.Trim();
				}
			}

			return directory ?? Environment.CurrentDirectory;
		}

		private static double StdDev(/*this*/ ICollection<int> collection)
		{
			if (collection == null) {
				return 0;
			} else {
				var n = collection.Count;
				if (n < 2) {
					return 0;
				} else {
					var mu = collection.Average();
					double s = 0;
					foreach (int xi in collection) {
						var delta = (xi - mu);
						s += delta * delta;
					}

					s /= (n - 1);
					return Math.Sqrt(s);
				}
			}
		}

		private static string CgxLoadingStatTable(IEnumerable<FXCGXFile> allCGX)
		{
			var mtxBuckets = new List<int>[3, 3]; // i: strategy    j: bpp
			int i, j;
			foreach (var cgx in allCGX) {
				i = (int)cgx.LoadingStrategy;
				j = cgx.GetFormat();
				if (mtxBuckets[i, j] == null) {
					mtxBuckets[i, j] = new List<int>();
				}

				var fi = new FileInfo(cgx.OriginalFilePath);
				mtxBuckets[i, j].Add((int)fi.Length);
			}

			var dblarMean = new double[3, 3];
			var dblarStdDev = new double[3, 3];
			for (i = 0; i < 3; i++) {
				for (j = 0; j < 3; j++) {
					dblarMean[i, j] = (mtxBuckets[i, j] == null) ? Double.NaN : mtxBuckets[i, j].Average();
					dblarStdDev[i, j] = StdDev(mtxBuckets[i, j]);
				}
			}

			var stbStats = new StringBuilder(1024);
			stbStats.AppendLine(
@"CGX file loading statistics:

         |   2bpp   |   4bpp   |   8bpp   |
---------+----------+----------+----------|");
			AppendStrategyStats(stbStats, 0, "Standard", mtxBuckets, dblarMean, dblarStdDev);
			AppendStrategyStats(stbStats, 1, "Guessed", mtxBuckets, dblarMean, dblarStdDev);
			AppendStrategyStats(stbStats, 2, "Asked", mtxBuckets, dblarMean, dblarStdDev);
			return stbStats.ToString();
		}

		private static string CgxLoadingStatTable(IEnumerable<FXCGXFile> allCGX, List<int>[] knownSizeBuckets)
		{
			var strStats = CgxLoadingStatTable(allCGX);
			var stbStats = new StringBuilder(strStats, checked(strStats.Length * 5 / 4));
			var dblarMean = new double[3];
			var dblarStdDev = new double[3];
			for (var i = 0; i < 3; i++) {
				dblarMean[i] = (knownSizeBuckets[i].Count < 1) ? Double.NaN : knownSizeBuckets[i].Average();
				dblarStdDev[i] = StdDev(knownSizeBuckets[i]);
			}

			AppendStrategyStats(stbStats, "Known", knownSizeBuckets, dblarMean, dblarStdDev);
			return stbStats.ToString();
		}

		private static void AppendStrategyStats(StringBuilder message, int row, string name,
		List<int>[,] buckets, double[,] averages, double[,] stdDevs)
		{
			message.AppendFormat("         | n={0,6} | n={1,6} | n={2,6} |", buckets[row, 0]?.Count, buckets[row, 1]?.Count, buckets[row, 2]?.Count).AppendLine();
			message.AppendFormat("{0,-8} | µ={1,6:g5} | µ={2,6:g5} | µ={3,6:g5} |", name, averages[row, 0], averages[row, 1], averages[row, 2]).AppendLine();
			message.AppendFormat("         | s={0,6:g5} | s={1,6:g5} | s={2,6:g5} |", stdDevs[row, 0], stdDevs[row, 1], stdDevs[row, 2]).AppendLine();
			message.AppendLine("---------+----------+----------+----------|");
		}

		private static void AppendStrategyStats(StringBuilder message, string name,
		List<int>[] buckets, double[] averages, double[] stdDevs)
		{
			message.AppendFormat("         | n={0,6} | n={1,6} | n={2,6} |", buckets[0]?.Count, buckets[1]?.Count, buckets[2]?.Count).AppendLine();
			message.AppendFormat("{0,-8} | µ={1,6:g5} | µ={2,6:g5} | µ={3,6:g5} |", name, averages[0], averages[1], averages[2]).AppendLine();
			message.AppendFormat("         | s={0,6:g5} | s={1,6:g5} | s={2,6:g5} |", stdDevs[0], stdDevs[1], stdDevs[2]).AppendLine();
			message.AppendLine("---------+----------+----------+----------|");
		}

		private static IDictionary<string, byte> InitKnownDepths()
		{
			var dicDepths = new Dictionary<string, byte>();
			dicDepths.Add("DATA/CONT*.CGX", 4);
			dicDepths.Add("DATA/CP*.CGX", 4);
			dicDepths.Add("DATA/FOX*.CGX", 4);
			dicDepths.Add("DATA/OBJ-*.CGX", 4);
			dicDepths.Add("DATA/TI-3*.CGX", 2);
			dicDepths.Add("DATA/TI.CGX", 4);
			dicDepths.Add("DATA/STARS.CGX", 4);
			dicDepths.Add("DATA/OOPS.CGX", 4);
			dicDepths.Add("DATA/MAP*.CGX", 4);
			dicDepths.Add("DATA/M.CGX", 4);
			dicDepths.Add("DATA/LSB.CGX", 4);
			dicDepths.Add("DATA/HOLE-A.CGX", 4);
			dicDepths.Add("DATA/F-1.CGX", 4);
			dicDepths.Add("DATA/E-TEST2.CGX", 4);
			dicDepths.Add("DATA/E-TEST.CGX", 2);
			dicDepths.Add("DATA/DEMO.CGX", 4);
			dicDepths.Add("DATA/DOG.CGX", 4);
			dicDepths.Add("DATA/B.CGX", 2);
			dicDepths.Add("DATA/B-HOLE.CGX", 4);
			dicDepths.Add("DATA/AND.CGX", 4);
			dicDepths.Add("DATA/ST-P.CGX", 4);
			dicDepths.Add("DATA/1-3-B.CGX", 4);
			dicDepths.Add("DATA/1-4.CGX", 4);
			dicDepths.Add("DATA/2-2.CGX", 4);
			dicDepths.Add("DATA/2-3*.CGX", 4);
			dicDepths.Add("DATA/2-4.CGX", 4);
			dicDepths.Add("DATA/3-*.CGX", 4);
			dicDepths.Add("DATA/FONT/D*B*GF*.CGX", 2);
			return dicDepths;
		}

		private static Nullable<byte> KnownDepth(FileInfo fi, string relativePath)
		{
			if (fi.Length == 34048) {
				return 4;
			} else {
				var input = relativePath.Replace(Path.DirectorySeparatorChar, '/');
				foreach (var kvp in s_dicKnown) {
					var pattern = kvp.Key.Replace(".", "[.]").Replace("*", ".*");
					if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase)) {
						return kvp.Value;
					}
				}

				return null;
			}
		}
	}
}
