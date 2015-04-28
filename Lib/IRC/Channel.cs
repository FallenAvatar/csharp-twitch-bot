using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lib.IRC
{
	public class Channel : IDisposable
	{
		public string Name;
		private Client client;
		private Dictionary<string, UserInfo> users;

		public IList<UserInfo> Users { get { return users.Values.ToList(); } }

		public delegate void ChatMessageRecievedDelegate( Channel chan, UserInfo user, string message );
		public event ChatMessageRecievedDelegate ChatMessageRecieved;
		public event ChatMessageRecievedDelegate EmoteMessageRecieved;

		public delegate void UserJoinedDelegate(Channel chan, UserInfo ui);
		public event UserJoinedDelegate UserJoined;

		public delegate void UserPartedDelegate( Channel chan, UserInfo ui );
		public event UserPartedDelegate UserParted;

		public delegate void UserChangedDelegate( Channel chan, UserInfo ui );
		public event UserChangedDelegate UserChanged;

		public Channel( Client client, string name )
		{
			this.client = client;
			this.Name = name;
			this.users = new Dictionary<string, UserInfo>();
		}

		public void SendMessage( string message )
		{
			client.Write( "PRIVMSG " + this.Name + " :" + message );
		}

		public void AddUser( UserInfo ui, bool skip_event = false )
		{
			if( users.ContainsKey( ui.Nick ) && string.IsNullOrEmpty( users[ui.Nick].Hostname ) && !string.IsNullOrEmpty(ui.Hostname) )
			{
				var old = users[ui.Nick];
				users[ui.Nick] = ui;

				if( old.IsMod )
					users[ui.Nick].IsMod = true;

				if( UserChanged != null )
					UserChanged( this, users[ui.Nick] );

				return;
			}
			else if( !users.ContainsKey( ui.Nick ) )
				users[ui.Nick] = ui;
			else
				return;

			if( !skip_event && UserJoined != null )
				UserJoined( this, ui );
		}

		public void RemoveUser( UserInfo ui )
		{
			if( ui == null )
				return;

			if( string.IsNullOrEmpty( ui.Nick ) )
				return;

			if( !users.ContainsKey( ui.Nick ) )
				return;

			users.Remove( ui.Nick );

			if( UserParted != null )
				UserParted( this, ui );
		}

		public void HandleMessage( UserInfo ui, string command, string data )
		{
			AddUser(ui);

			int idx;
			switch( command )
			{
			// Handled by AddUser(ui); above
			/*case "JOIN":
				break;*/
			case "PART":
				RemoveUser( ui );
				break;
			case "PRIVMSG":
				if( data.StartsWith( ":" ) )
					data = data.Substring( 1 );

				data = data.Trim();
				string header="";
				string msg="";

				if( data.Contains( '\x01' ) )
				{
					int soh1 = data.IndexOf( '\x01' );
					int soh2 = data.IndexOf( '\x01', soh1 + 1 );

					header = data.Substring( soh1 + 1, soh2 - (soh1 + 1) ).Trim();
					if( header.StartsWith( "ACTION" ) )
					{
						header = header.Substring(7);	// ("ACTION ").Length
					}
				}
				else
				{
					msg = data;
				}

				if( !string.IsNullOrEmpty( msg ) && ChatMessageRecieved != null )
					ChatMessageRecieved( this, ui, msg );
				else if( !string.IsNullOrEmpty( header ) && EmoteMessageRecieved != null )
					EmoteMessageRecieved( this, ui, msg );

				break;
			case "MODE":
				idx = data.IndexOf( ' ' );
				if( idx < 0 )
					break;

				var opt = data.Substring( 0, idx );
				data = data.Substring( idx + 1 ).Trim();

				if( !users.ContainsKey( data ) )
					break;

				if( opt == "+o" )
					users[data].IsMod = true;
				else if( opt == "-o" )
					users[data].IsMod = false;

				if( UserChanged != null )
					UserChanged( this, ui );
				break;
			}
		}

		public void HandleCommand( UserInfo ui, int command, string data )
		{
			switch( command )
			{
			case 353:
				if( data.StartsWith( ":" ) )
					data = data.Substring( 1 );
				data = data.Trim();

				var us = data.Split( ' ' );
				foreach( var u in us )
				{
					if( string.IsNullOrEmpty( u ) )
						continue;

					AddUser( new UserInfo( this.client ) { Nick = u }, true );
				}
				break;
			case 366:
				// End of users lists.....do i care?
				break;
			}
		}

		public void Dispose()
		{
			users = null;
		}
	}
}
