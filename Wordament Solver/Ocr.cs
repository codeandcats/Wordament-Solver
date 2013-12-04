using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Permissions;
using System.Diagnostics;

namespace Wordament
{
	public static class Ocr
	{
		public static string ReadWord(Bitmap bmp)
		{
			string inputFileName = "";
			string outputFileName = "";
			string text = "";

			inputFileName = GetTempFileName("OcrInput", ".bmp");
			bmp.Save(inputFileName, ImageFormat.Bmp);

			outputFileName = GetTempFileName("OcrOutput", "txt");

			if (File.Exists(outputFileName))
				File.Delete(outputFileName);

			outputFileName = Path.ChangeExtension(outputFileName, "");

			string ocrToolFileName = Settings.TesseractFileName;

			if (!File.Exists(ocrToolFileName))
			{
				throw new Exception(string.Format(
					"Tesseract.exe not found on system.\nExpected under: {0}\nDownload from: {1}",
					ocrToolFileName,
					Settings.TesseractUrl));
			}

			string parameters = string.Format("\"{0}\" \"{1}\" -psm 8", inputFileName, outputFileName);

			try
			{
				var startInfo = new ProcessStartInfo(ocrToolFileName, parameters);
				//startInfo.CreateNoWindow = false;
				startInfo.WindowStyle = ProcessWindowStyle.Hidden;
				Process process = Process.Start(startInfo);
				process.WaitForExit();
			}
			catch (Exception error)
			{
				throw new Exception(
					"Error running: " + ocrToolFileName + " " + parameters + "\n\n" +
					error.Message + 
					"\nInput FileName: " + inputFileName + 
					"\nOutput FileName: " + outputFileName);
			}

			// Tesseract appends .txt to the output filename
			outputFileName += ".txt";

			text = File.ReadAllText(outputFileName).Trim();

			// Some tweaks to make up for common misreadings with tesseract ocr
			// Single "B" is often mistaken for "/"
			// I'm assuming that you can train tesseract and if so that would probably be
			// a better approach than hard-coding corrections but for now to get it
			// working I'm just doing this - Sue me!
			// (Don't sue me)
			if (text == "/")
				text = "B";

			text = text.Trim('/', '\\');

			// Clean up
			if (File.Exists(outputFileName))
				File.Delete(outputFileName);

			if (File.Exists(inputFileName))
				File.Delete(inputFileName);

			return text;
		}

		private static string GetTempFileName(string prefix, string extension)
		{
			string tempPath = Path.GetTempPath();
			new FileIOPermission(FileIOPermissionAccess.Write, tempPath).Demand();

			string fullPath = "";

			int index = 0;

			while (true)
			{
				string fileName = prefix + index.ToString("##0000") + extension;
				fullPath = Path.Combine(tempPath, fileName);
				if (File.Exists(fullPath))
				{
					index++;
				}
				else
				{
					using (var stream = File.Create(fullPath))
					{
						stream.Flush();
					}
					break;
				}
			}

			return fullPath;
		}
	}
}