using BFDecoder;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NAudio.Dsp;

namespace BFDecoder
{
    public partial class Form1 : Form
    {
		LoopbackRecorder Recorder;
		readonly WaveFormat WaveFormat = new WaveFormat(44100, 1);
		WaveFormatConversionStream Wave;
		Wave16ToFloatProvider WaveToFloat;
		
		public Form1()
        {
            InitializeComponent();
        }

		/// <summary>
		/// Load a pre-recorded WAV
		/// </summary>
		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetupChart();
			OpenFileDialog open = new OpenFileDialog();
			open.InitialDirectory = @"c:\users\chris\appdata\local\temp";
			open.Filter = "Wave File (*.wav)|*.wav;";

			if (open.ShowDialog() != DialogResult.OK)
				return;

			// This will almost certainly go south! ;)
			WaveFileReader waveFileReader = new WaveFileReader(open.FileName);
			Wave = new WaveFormatConversionStream(WaveFormat, WaveFormatConversionStream.CreatePcmStream(waveFileReader));
			WaveToFloat = new Wave16ToFloatProvider(Wave);

			processToolStripMenuItem.Enabled = true;
			clearToolStripMenuItem.Enabled = true;
			startToolStripMenuItem.Enabled = false;
			stopToolStripMenuItem.Enabled = false;
		}

		private void startToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetupChart();
			Recorder = new LoopbackRecorder();
			Recorder.StartRecording();
			stopToolStripMenuItem.Enabled = true;
			processToolStripMenuItem.Enabled = true;
			startToolStripMenuItem.Enabled = false;
			openToolStripMenuItem.Enabled = false;
		}

		private void stopToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Recorder.StopRecording();
			stopToolStripMenuItem.Enabled = false;
			clearToolStripMenuItem.Enabled = true;
			saveToolStripMenuItem.Enabled = true;
		}

		private void processToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Wave == null)
			{
				Stream audioStream = Recorder.GetRecordingStream();
				audioStream.Position = 0;
				Wave = new WaveFormatConversionStream(WaveFormat, new Wave32To16Stream(new WaveFileReader(audioStream)));
				WaveToFloat = new Wave16ToFloatProvider(Wave);
			}
			ProcessAudio();
		}

		private void ProcessAudio()
		{
			int bufSize = 8 * 1024;
			byte[] audioData = new byte[bufSize];
			int read = 0;
			int sizeOfSample = sizeof(Single);
			SampleAggregator sampleAgg = new SampleAggregator(bufSize / 4);
			sampleAgg.PerformFFT = true;
			sampleAgg.FftCalculated += new EventHandler<FftEventArgs>(func);

			while (chart1.Series.Count > 0) { chart1.Series.RemoveAt(0); }
			chart1.Series.Add("X");
			chart1.Series.Add("Y");

			while (Wave.Position < Wave.Length)
			{
				read = WaveToFloat.Read(audioData, 0, bufSize);

				// Might need this to check we haven't run out of stuff to read
				if (read == 0)
					break;

				for (int i = 0; i < read / sizeOfSample; i++)
				{
					Single sample = BitConverter.ToSingle(audioData, i * sizeOfSample);
					sampleAgg.Add(sample);
				}
			}
		}

		private void func(object sender, FftEventArgs e)
		{
			foreach (var item in e.Result)
			{
				//Console.WriteLine(item.X);
				//Console.WriteLine(item.Y);

				chart1.Series["X"].Points.Add(item.X);
				chart1.Series["Y"].Points.Add(item.Y);
			}
		}

		private void SetupChart()
		{
			while (chart1.Series.Count > 0) { chart1.Series.RemoveAt(0); }
			chart1.Series.Add("Wave");
			chart1.Series["Wave"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
			chart1.Series["Wave"].ChartArea = "ChartArea1";
		}

		private void clearToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Wave != null)
			{
				Wave.Close();
				Wave.Dispose();
				Wave = null;
				
			}

			if (Recorder != null)
			{
				Recorder.StopRecording();
				Recorder = null;
			}

			SetupChart();

			processToolStripMenuItem.Enabled = false;
			startToolStripMenuItem.Enabled = true;
			stopToolStripMenuItem.Enabled = false;
			openToolStripMenuItem.Enabled = true;
			clearToolStripMenuItem.Enabled = false;
			saveToolStripMenuItem.Enabled = false;
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Wave == null)
			{
				Stream audioStream = Recorder.GetRecordingStream();
				audioStream.Position = 0;
				Wave = new WaveFormatConversionStream(WaveFormat, new Wave32To16Stream(new WaveFileReader(audioStream)));
			}

			using (WaveFileWriter waveWriter = new WaveFileWriter(@"c:\users\chris\appdata\local\temp\recording.wav", Wave.WaveFormat))
			{

				byte[] buffer = new byte[16 * 1024];
				Wave.Position = 0;

				while (Wave.Position < Wave.Length)
				{
					int read = Wave.Read(buffer, 0, 16 * 1024);
					if (read > 0)
						waveWriter.Write(buffer, 0, read);
				}
			}
		}
	}
}
