using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFDecoder
{
	/// <summary>
	/// A wrapper for the WasapiLoopbackCapture that will implement basic recording to a file that is overwrite only.
	/// </summary>
	public class LoopbackRecorder
	{
		private IWaveIn _waveIn;
		private WaveFileWriter _writer;
		private IgnoreDisposeStream _outputStream;
		private bool _isRecording = false;

		public LoopbackRecorder()
		{
			_outputStream = new IgnoreDisposeStream(new MemoryStream());
			_waveIn = new WasapiLoopbackCapture();
			_waveIn.DataAvailable += OnDataAvailable;
			_waveIn.RecordingStopped += OnRecordingStopped;
			_writer = new WaveFileWriter(_outputStream, _waveIn.WaveFormat);
		}

		public void StartRecording()
		{
			// If we are currently record then go ahead and exit out.
			if (_isRecording == true)
				return;

			_waveIn.StartRecording();
			_isRecording = true;
		}

		public void StopRecording()
		{
			if (_waveIn == null)
				return;

			_waveIn.StopRecording();
		}

		/// <summary>
		/// Event handled when recording is stopped.  We will clean up open objects here that are required to be 
		/// closed and/or disposed of.
		/// </summary>
		void OnRecordingStopped(object sender, StoppedEventArgs e)
		{
			// Writer Close() needs to come first otherwise NAudio will lock up.

			if (_writer != null)
			{
				_writer.Dispose();
				_writer = null;

			}
			if (_waveIn != null)
			{
				_waveIn.Dispose();
				_waveIn = null;
			}

			_isRecording = false;
			if (e.Exception != null)
			{
				throw e.Exception;
			}
		}

		/// <summary>
		/// Event handled when data becomes available.  The data will be written out to disk at this point.
		/// </summary>
		void OnDataAvailable(object sender, WaveInEventArgs e)
		{
			_writer.Write(e.Buffer, 0, e.BytesRecorded);
		}

		public Stream GetRecordingStream()
		{
			return _outputStream;
		}
	}
}
