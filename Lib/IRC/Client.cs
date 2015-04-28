using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Lib.IRC
{
	public class Client : IDisposable
	{
		protected TcpClient ircClient;
		protected StreamReader reader;
		protected StreamWriter writer;

		protected Thread ReadThread;
		protected Thread KeepAliveThread;

		protected string Nick;
		protected string HostName;
		protected UserInfo ServerID;

		protected Dictionary<string, Channel> channels = null;

		public delegate void ConnectedDelegate();
		public event ConnectedDelegate Connected;

		public Client()
		{
			channels = new Dictionary<string, Channel>();
		}

		public void Dispose()
		{
			if( ReadThread != null )
			{
				if( ReadThread.IsAlive )
					ReadThread.Abort();
				ReadThread = null;
			}

			if( KeepAliveThread != null )
			{
				if( KeepAliveThread.IsAlive )
					KeepAliveThread.Abort();
				KeepAliveThread = null;
			}

			foreach( var k in channels.Keys )
			{
				var item = channels[k];
				item.Dispose();
				item = null;
				channels.Remove( k );
			}
			channels = null;

			if( ircClient != null )
			{
				if( ircClient.Connected )
				{
					Write( "QUIT" );
					ircClient.Close();
				}

				ircClient = null;
			}
		}

		public void Connect(string server, int port, string user, string password)
		{
			int count = 1;

			ircClient = new TcpClient();
			while( !ircClient.Connected )
			{
				try
				{
					ircClient.Connect(server, port);

					var ns = ircClient.GetStream();

					reader = new StreamReader(ns);
					
					writer = new StreamWriter(ns);
					writer.AutoFlush = true;

					break;
				}
				catch(SocketException)
				{
					Logger.Instance.Error("Could not connect to IRC Server. Retrying in 5 seconds...");
				}
				catch(Exception ex)
				{
					Logger.Instance.Error("Error connecting to IRC Server.\nException: "+ex.Message+"\nStack Trace:\n"+ex.StackTrace+"\n\nRetrying in 5 seconds...");
				}

				count++;
				Thread.Sleep(5000);
			}

			this.Nick = user;

			this.Write( "PASS " + password );
			this.Write( "NICK " + user );
			this.Write( "USER " + user + " * * :" + user );

			ReadThread = new Thread( Read_Thread );
			ReadThread.Start();

			// Start Keep Alive Thread
			KeepAliveThread = new Thread( () => {
				while( true )
				{
					Thread.Sleep( 30 * 1000 );
					Write( "PING :"+user );
				}
			} );
			//KeepAliveThread.Start();
		}

		public Channel JoinChannel( string channel )
		{
			if( !channel.StartsWith( "#" ) )
				channel = "#" + channel;

			this.channels[channel] = new Channel( this, channel );
			Write( "JOIN " + channel );

			return this.channels[channel];
		}

		public void Write(string s)
		{
			try
			{
				lock( writer )
				{
					Logger.Instance.Debug( "--> " + s );
					writer.WriteLine( s );
				}
			}
			catch(Exception ex)
			{
				Logger.Instance.Error("Error sending data to IRC Server.\nException: " + ex.Message + "\nStack Trace:\n" + ex.StackTrace + "");
			}
		}

		private void Read_Thread()
		{
			while( true )
			{
				string line = reader.ReadLine();

				HandleMessage( line );
			}
		}

		private void HandleMessage( string message )
		{
			if( string.IsNullOrEmpty( message ) )
				return;

			Logger.Instance.Debug( "<-- " + message );

			if( message.StartsWith("PING") )
			{
				int idx = message.IndexOf(' ');
				string to = "";
				if( idx > 0 )
					to = " " + message.Substring(idx+1);
				Write( "PONG " + this.Nick + " " + to );
				return;
			}
			else if( message[0] == ':' )
			{
				int idx = message.IndexOf(' ');
				string from = message.Substring(1, idx-1); // Skip leading ':'
				var ui = ParseUserInfo( from );

				if( this.ServerID == null && !from.Contains( this.Nick ) )
					this.ServerID = ui;

				message = message.Substring( idx + 1 );
				idx = message.IndexOf( ' ' );

				var sCmd = message.Substring( 0, idx );
				message = message.Substring( idx + 1 );

				int cmd;

				if( int.TryParse( sCmd, out cmd ) )
				{
					HandleCommand( ui, cmd, message );
					return;
				}

				switch( sCmd )
				{
				case "JOIN":
					if( !channels.ContainsKey( message ) )
						break;
					channels[message].HandleMessage( ui, "JOIN", null );
					break;
				case "PART":
					if( !channels.ContainsKey( message ) )
						break;
					channels[message].HandleMessage( ui, "PART", null );
					break;
				case "PRIVMSG":
					idx = message.IndexOf( ' ' );
					if( idx < 0 )
						return;

					string to = message.Substring( 0, idx );
					message = message.Substring( idx + 1 );
					if( channels.ContainsKey( to ) )
					{
						channels[to].HandleMessage( ui, "PRIVMSG", message );
					}
					else if( to == this.Nick )
					{
						// Hmmm....
					}
					break;
				case "MODE":
					idx = message.IndexOf( ' ' );
					if( idx < 0 )
						return;

					string in_channel = message.Substring( 0, idx );
					message = message.Substring( idx + 1 );
					if( channels.ContainsKey( in_channel ) )
					{
						channels[in_channel].HandleMessage( ui, "MODE", message );
					}
					break;
				}
			}
		}

		private void HandleCommand( UserInfo ui, int cmdNumber, string data )
		{
			int idx = data.IndexOf( ' ' );
			if( idx < 0 )
				return;

			string to = data.Substring( 0, idx );
			data = data.Substring( idx + 1 );

			if( to != this.Nick )
				return;

			switch( cmdNumber )
			{
			case 353:
				if( data.StartsWith( "=" ) )
					data = data.Substring( 1 );
				data = data.Trim();
				idx = data.IndexOf( ' ' );
				if( idx < 0 )
					break;
				string channel = data.Substring( 0, idx ).Trim();
				data = data.Substring( idx + 1 ).Trim();

				if( !channels.ContainsKey( channel ) )
					break;

				channels[channel].HandleCommand( ui, 353, data );
				break;
			case 366:
				data = data.Trim();
				idx = data.IndexOf( ' ' );
				if( idx < 0 )
					break;
				string channel2 = data.Substring( 0, idx ).Trim();
				data = data.Substring( idx + 1 ).Trim();

				if( !channels.ContainsKey( channel2 ) )
					break;

				channels[channel2].HandleCommand( ui, 366, data );
				break;
			case 375:			// Start MOTD
				//MOTD = "";
				break;
			case 372:			// MOTD Line
				//MOTD += data;
				break;
			case 376:			// End MOTD
				//Console.WriteLine( "Server MOTD:\n" + MOTD );
				if( Connected != null )
					Connected();
				break;
			}
		}

		public UserInfo ParseUserInfo( string ui )
		{
			var ret = new UserInfo(this);

			if( ui.StartsWith( ":" ) )
				ui = ui.Substring( 1 );

			int idx = ui.IndexOfAny( new char[] { '!', '@' } );

			if( idx < 0 )
			{
				ret.Nick = ui;
			}
			else
			{
				ret.Nick = ui.Substring( 0, idx );
				ui = ui.Substring( idx );

				if( ui[0] == '!' )		// User name is next
				{
					ui = ui.Substring( 1 );
					idx = ui.IndexOf( '@' );
					if( idx < 0 )
					{
						ret.User = ui;
					}
					else
					{
						ret.User = ui.Substring( 0, idx );
						ret.Hostname = ui.Substring( idx + 1 );
					}
				}
				else					// Hostname is left
				{
					ret.User = ret.Nick;
					ui = ui.Substring( 1 );
					ret.Hostname = ui;
				}
			}

			return ret;
		}
	}
}
