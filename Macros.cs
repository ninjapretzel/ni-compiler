using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ni_compiler {
	public static class Macros {

		public static void FixSourceFiles(string inDirectory) {
			var files = GetAllFiles(inDirectory.ForwardSlashPath()).Select(s => s.ForwardSlashPath())
				.Where(it => it.EndsWith(".cs"));
			Console.WriteLine($"Checking {files.Count()} files in tree:\n{inDirectory}");
			foreach (var file in files) {
				string text = File.ReadAllText(file);
				if (text.Contains("\r\n")) {
					text = text.Replace("\r\n", "\n").Replace("\n\r", "\n").Replace("\r", "\n");
					File.WriteAllText(file, text, Encoding.UTF8);
				}
			}
		}

		private static string Filename(this string filepath) {
			return filepath.ForwardSlashPath().FromLast("/");
		}
		private static string Folder(this string filepath) {
			return filepath.UpToLast("/");
		}

		private static string ForwardSlashPath(this string path) { return path.Replace('\\', '/'); }
		private static string UpToLast(this string str, string search) {
			if (str.Contains(search)) {
				int ind = str.LastIndexOf(search);
				return str.Substring(0, ind);
			}
			return str;
		}
		private static string FromLast(this string str, string search) {
			if (str.Contains(search) && !str.EndsWith(search)) {
				int ind = str.LastIndexOf(search);

				return str.Substring(ind + search.Length);
			}
			return "";
		}

		private static string RelPath(this string filepath, string from) {
			return filepath.Replace(from, "").Replace(filepath.Filename(), "");
		}

		private static List<string> GetAllFiles(string dirPath, List<string> collector = null) {
			if (collector == null) { collector = new List<string>(); }

			collector.AddRange(Directory.GetFiles(dirPath));
			foreach (var subdir in Directory.GetDirectories(dirPath)) {
				GetAllFiles(subdir, collector);
			}

			return collector;
		}
	}
}
