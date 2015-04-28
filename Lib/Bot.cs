using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;

namespace Lib
{
	public class Bot
	{
		protected IRC.Client ircClient;
		public IRC.Channel Channel;
		protected string ChannelName;

		public event Action Connected;

		public Bot()
		{
			Logger.Instance.Level = LogLevel.Debug;
		}

		public void Start(string channel_name)
		{
			this.ChannelName = channel_name;
			if( !this.ChannelName.StartsWith( "#" ) )
				this.ChannelName = '#' + this.ChannelName;

			var appSettings = ConfigurationManager.AppSettings;

			ircClient = new IRC.Client();
			ircClient.Connected += ircClient_Connected;
			ircClient.Connect( appSettings["irc-server"], int.Parse( appSettings["irc-port"] ), appSettings["irc-user"], appSettings["irc-password"] );
		}

		public void SendMessage( string message )
		{
			Channel.SendMessage( message );
		}

		protected void ircClient_Connected()
		{
			Channel = ircClient.JoinChannel( this.ChannelName );

			Channel.ChatMessageRecieved += channel_ChatMessageRecieved;
			Channel.UserJoined += channel_UserJoined;
			Channel.UserParted += channel_UserParted;

			if( this.Connected != null )
				this.Connected();
		}

		protected void channel_ChatMessageRecieved( IRC.Channel chan, IRC.UserInfo user, string message )
		{
			var dt = DateTime.Now;
			Console.WriteLine( "[" + dt.Hour.ToString( "00" ) + ":" + dt.Minute.ToString( "00" ) + "] " + user.Nick + ": " + message );

			if( message.StartsWith( "!" ) )
			{
				int i=0;
				// TODO: Command Support
			}
		}

		protected void channel_UserJoined( IRC.Channel chan, IRC.UserInfo ui )
		{
			var dt = DateTime.Now;
			Console.WriteLine( "[" + dt.Hour.ToString( "00" ) + ":" + dt.Minute.ToString( "00" ) + "] User " + ui.Nick + " joined." );

			// TODO: Greeting Support
		}

		protected void channel_UserParted( IRC.Channel chan, IRC.UserInfo ui )
		{
			var dt = DateTime.Now;
			Console.WriteLine( "[" + dt.Hour.ToString( "00" ) + ":" + dt.Minute.ToString( "00" ) + "] User " + ui.Nick + " left." );
		}

		public void Stop()
		{
			ircClient.Dispose();
			ircClient = null;
		}
	}
}
