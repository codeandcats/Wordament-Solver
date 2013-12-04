using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;

namespace Wordament
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void AddToLog(string text)
		{
			if ((chkAutoClearLog.IsChecked == true) && (txtLog.LineCount > txtLog.GetLastVisibleLineIndex() + 1))
				ClearLog();

			/*
			txtLog.Text += text + "\n";

			if (chkAutoClearLog.IsChecked == false)
				txtLog.SelectionStart = txtLog.Text.Length + "\n".Length;
			//txtLog.InvalidateVisual();
			//txtLog.UpdateLayout();

			TextRange range = new TextRange(txtRichLog.Document.ContentEnd, txtRichLog.Document.ContentEnd);
			range.Text = text + "\n";
			if (text.ToLower().Contains("invalid"))
				range.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.Red);
			else if (text.ToLower().Contains("valid"))
				range.ApplyPropertyValue(TextElement.ForegroundProperty, System.Windows.Media.Brushes.Green);
			
			//txtRichLog.AppendText(text);
			*/

			if ((lstLog.Items.Count > 400) && (chkAutoClearLog.IsChecked == true))
			{
				lstLog.Items.RemoveAt(0);
			}

			foreach (var line in text.Split(new[] { "\n" }, StringSplitOptions.None))
			{
				var item = new ListBoxItem();
				item.Content = line;

				if (text.ToLower().Contains("invalid"))
					item.Foreground = System.Windows.Media.Brushes.Red;
				else if (text.ToLower().Contains("valid"))
					item.Foreground = System.Windows.Media.Brushes.Green;

				lstLog.Items.Add(item);
			}

			lstLog.SelectedIndex = lstLog.Items.Count - 1;
			lstLog.ScrollIntoView(lstLog.Items[lstLog.Items.Count - 1]);
		}
		
		private void ClearLog()
		{
			//txtLog.Clear();
			//txtRichLog.Document.Blocks.Clear();
			lstLog.Items.Clear();
		}

		private WordamentService service = new WordamentService();

		private void btnStartStopService_Click(object sender, RoutedEventArgs e)
		{
			if (service.Running)
			{
				service.Stop();
				btnStartStopService.Content = "Start";
			}
			else
			{
				service.Start();
				btnStartStopService.Content = "Stop";
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			service.StatusChanged += ServiceStatusChanged;
			
			txtLog.Visibility = System.Windows.Visibility.Hidden;
			txtRichLog.Visibility = Visibility.Hidden;

			txtRichLog.IsReadOnly = true;
			txtLog.IsReadOnly = true;
			
			txtRichLog.Document = new FlowDocument(new Paragraph());

			ClearLog();
		}

		private void ServiceStatusChanged(object sender, StatusEventArgs args)
		{
			if (args.Status.ToLower() == "service stopped")
				btnStartStopService.Content = "Start";
			AddToLog(args.Status);
		}

		private void btnClearLog_Click(object sender, RoutedEventArgs e)
		{
			ClearLog();
		}

		private void btnTest_Click(object sender, RoutedEventArgs e)
		{
			string[] lines = System.IO.File.ReadAllLines("_Tile colors.txt");

			using (var bmp = new Bitmap(600, 2048))
			{
				using (var gfx = Graphics.FromImage(bmp))
				{
					int x = 5;
					int y = 5;

					gfx.FillRectangle(new SolidBrush(Color.White), 0, 0, bmp.Width, bmp.Height);

					Font font = new Font("Consolas", 9);

					foreach (var line in lines)
					{
						if (line.Trim() == "")
							continue;

						string[] values = line.Split(',');
						
						Color color = Color.FromArgb(int.Parse(values[4]), int.Parse(values[5]), int.Parse(values[6]));
						Brush brush = new SolidBrush(color);

						string word =
							values[0] + " (" +
							values[1] + ") - " +
							string.Format("{0},{1},{2}", values[4], values[5], values[6]);

						gfx.DrawString(word, font, brush, new PointF(x, y));

						y += (int)(gfx.MeasureString(word, font).Height * 1.5);
					}
				}

				bmp.Save("_Tile colors.png", System.Drawing.Imaging.ImageFormat.Png);
			}
		}

		private void btnLaunchSite_Click(object sender, RoutedEventArgs e)
		{
			var url = Settings.GameUrl;
			System.Diagnostics.Process.Start(url);
		}
	}
}