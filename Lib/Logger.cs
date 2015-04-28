using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lib
{
	public enum LogLevel
	{
		Debug = 1,
		Info,
		Warning,
		Error,
		Critical
	}

	public class Logger : IDisposable
	{
		
		private static Logger _inst = null;
		private static object _instLock = new object();
		public static Logger Instance
		{
			get
			{
				if( _inst == null )
				{
					lock( _instLock )
					{
						if( _inst == null )
						{
							_inst = new Logger();
							_inst.Level = LogLevel.Info;
						}
					}
				}

				return _inst;
			}
		}

		private StreamWriter writer = null;
		public LogLevel Level { get; set; }

		private Logger()
		{
			writer = new StreamWriter("ModBotNext.log");
			writer.AutoFlush = true;
		}

		private void WriteLine(string message)
		{
			var dt = DateTime.Now.ToUniversalTime();

			var sb = new StringBuilder();
			sb.Append("[");
			sb.Append(dt.Year.ToString("0000"));
			sb.Append("-");
			sb.Append(dt.Month.ToString("00"));
			sb.Append("-");
			sb.Append(dt.Day.ToString("00"));
			sb.Append("T");
			sb.Append(dt.Hour.ToString("00"));
			sb.Append(":");
			sb.Append(dt.Minute.ToString("00"));
			sb.Append(":");
			sb.Append(dt.Second.ToString("00"));
			sb.Append("Z] ");
			sb.Append(message);

			lock( _instLock )
			{
				if( writer != null )
				{
					writer.WriteLine(sb.ToString());
				}
				else
				{
					Console.WriteLine("Could not log message, logger was disposed!");
				}
			}
		}

		public void Debug(string message)
		{
			if( this.Level > LogLevel.Debug )
				return;

			WriteLine("[DEBUG] "+message);
		}

		public void Info(string message)
		{
			if( this.Level > LogLevel.Info )
				return;

			WriteLine("[INFO ] " + message);
		}

		public void Warn(string message)
		{
			if( this.Level > LogLevel.Warning )
				return;

			WriteLine("[WARN ] " + message);
		}

		public void Error(string message)
		{
			WriteLine("[ERROR] " + message);
		}

		public void Critical(string message)
		{
			WriteLine("[CRIT ] " + message);
		}

		public void Dispose()
		{
			lock( _instLock )
			{
				writer.Flush();
				writer.Close();
				writer.Dispose();
				writer = null;
			}

			_instLock = null;
		}
	}
}
