using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace IISLogManager.Core {
	public static class Utils {
		/// <summary>
		/// Reads all lines of a file.
		/// </summary>
		/// <param name="file"></param>
		/// <returns>List<string></returns>
		public static List<string> ReadAllLines(string file) {
			List<string> lines = new List<string>();
			using (FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				using (StreamReader streamReader = new StreamReader(fileStream)) {
					while (streamReader.Peek() > -1) {
						lines.Add(streamReader.ReadLine());
					}
				}
			}

			return lines;
		}

		/// <summary>
		/// Compresses the string.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>string (base64)</returns>
		public static string CompressString(string text) {
			byte[] buffer = Encoding.UTF8.GetBytes(text);
			var memoryStream = new MemoryStream();
			using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true)) {
				gZipStream.Write(buffer, 0, buffer.Length);
			}

			memoryStream.Position = 0;

			var compressedData = new byte[memoryStream.Length];
			memoryStream.Read(compressedData, 0, compressedData.Length);

			var gZipBuffer = new byte[compressedData.Length + 4];
			Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
			Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
			return Convert.ToBase64String(gZipBuffer);
		}

		/// <summary>
		/// Decompresses the string.
		/// </summary>
		/// <param name="compressedText">The compressed text.</param>
		/// <returns> string </returns>
		public static string DecompressString(string compressedText) {
			byte[] gZipBuffer = Convert.FromBase64String(compressedText);
			using (var memoryStream = new MemoryStream()) {
				int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
				memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);
				var buffer = new byte[dataLength];
				memoryStream.Position = 0;
				using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress)) {
					gZipStream.Read(buffer, 0, buffer.Length);
				}

				return Encoding.UTF8.GetString(buffer);
			}
		}

		public static string MakeSafeFilename(string fileName, char replaceChar) {
			var invalidChars = Path.GetInvalidFileNameChars();
			return invalidChars.Aggregate(fileName, (current, c) => current.Replace(c, replaceChar));
		}

		/// <summary>
		/// Gets the number of lines from a given file
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns>int</returns>
		public static int? GetLineCount(string fileName) {
			var lineCount = 0;
			using var reader = File.OpenText(path: fileName);
			while (reader.ReadLine() != null) {
				lineCount++;
			}

			return lineCount;
		}

		/// <summary>
		/// Gets the number of logs from a given IIS Log file, excluding lines beginning with '#'
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns>int</returns>
		public static int? GetLogCount(string fileName) {
			var lineCount = 0;
			using var reader = File.OpenText(path: fileName);
			// var doContinue = true;
			// while (reader.ReadLine() != null) {
			while (true) {
				var line = reader.ReadLine();
				if ( line == null ) {
					break;
				}

				if ( !line.StartsWith("#") ) {
					lineCount++;
				}
			}

			return lineCount;
		}

		/// <summary>
		/// Gets a random integer by digit length
		/// </summary>
		/// <example>GetRandom(3) : 523</example>
		/// <example>GetRandom(3) : 872</example>
		/// <example>GetRandom(6) : 124015</example>
		/// <example>GetRandom(6) : 928341</example>
		/// <param name="length"></param>
		/// <returns></returns>
		public static int GetRandom(int length) {
			Random rand = new();
			var minLengthString = $"1{string.Join("", Enumerable.Repeat(0, length - 1))}";
			var maxLengthString = string.Join("", Enumerable.Repeat(9, length));
			return rand.Next(int.Parse(minLengthString), int.Parse(maxLengthString));
		}

		/// <summary>
		/// Parses the value from a string formatted like below, returning "Some More Text" from the example
		/// "Some Text (Some More Text)" 
		/// </summary>
		/// <param name="complexValue"></param>
		/// <returns></returns>
		public static string ExtractUrl(string complexValue) {
			var splitString = complexValue.Split('\t');
			if ( splitString[1] != "()" ) {
				return splitString[1].Replace('(', new char())
					.Replace(')', new char())
					.Trim();
			}

			return null;
		}
	}
}