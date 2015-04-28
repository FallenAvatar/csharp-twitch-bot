using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ModBotNext
{
	public partial class frmStart : Form
	{
		Lib.Bot bot;

		public frmStart()
		{
			InitializeComponent();
		}

		private void frmStart_Load( object sender, EventArgs e )
		{
			bot = new Lib.Bot();
			bot.Connected += bot_Connected;
		}

		private void bot_Connected()
		{
			bot.Channel.ChatMessageRecieved += new Lib.IRC.Channel.ChatMessageRecievedDelegate( Channel_ChatMessageRecieved );
			bot.Channel.UserJoined += new Lib.IRC.Channel.UserJoinedDelegate( Channel_UserJoined );
			bot.Channel.UserParted += new Lib.IRC.Channel.UserPartedDelegate( Channel_UserParted );
			bot.Channel.UserChanged += new Lib.IRC.Channel.UserChangedDelegate( Channel_UserChanged );

			//lstUsers.DataSource = bot.Channel.Users;

			UpdateUsers();
		}

		void Channel_UserChanged( Lib.IRC.Channel chan, Lib.IRC.UserInfo ui )
		{
			UpdateUsers();
		}

		void Channel_UserParted( Lib.IRC.Channel chan, Lib.IRC.UserInfo ui )
		{
			if( this.InvokeRequired )
			{
				this.Invoke( new Lib.IRC.Channel.UserPartedDelegate( Channel_UserParted ), chan, ui );
				return;
			}

			UpdateUsers();
		}

		void Channel_UserJoined( Lib.IRC.Channel chan, Lib.IRC.UserInfo ui )
		{
			if( this.InvokeRequired )
			{
				this.Invoke( new Lib.IRC.Channel.UserJoinedDelegate( Channel_UserJoined ), chan, ui );
				return;
			}

			UpdateUsers();
		}

		private void frmStart_FormClosing( object sender, FormClosingEventArgs e )
		{
			bot.Stop();
		}

		private void btnConnect_Click( object sender, EventArgs e )
		{
			bot.Start(txtChannel.Text);
		}

		void Channel_ChatMessageRecieved( Lib.IRC.Channel chan, Lib.IRC.UserInfo user, string message )
		{
			if( this.InvokeRequired )
			{
				this.Invoke( new Lib.IRC.Channel.ChatMessageRecievedDelegate( Channel_ChatMessageRecieved ), chan, user, message );
				return;
			}

			bool scroll = false;

			int currTopIndex = lstMessages.TopIndex;
			int visibleItems = lstMessages.ClientSize.Height / lstMessages.ItemHeight;

			if( lstMessages.TopIndex >= Math.Max(lstMessages.Items.Count - visibleItems, 0) )
				scroll = true;
			var dt = DateTime.Now;
			lstMessages.Items.Add("[" + dt.Hour.ToString( "00" ) + ":" + dt.Minute.ToString( "00" ) + "] " + user.Nick + ": " + message );

			if( scroll )
				lstMessages.TopIndex = Math.Max( lstMessages.Items.Count - visibleItems + 1, 0 );
		}

		private void btnSend_Click( object sender, EventArgs e )
		{
			SendMessage();
		}

		private void txtMessage_KeyDown( object sender, KeyEventArgs e )
		{
			if( e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return )
				SendMessage();
		}

		private void SendMessage()
		{
			string msg = txtMessage.Text;
			txtMessage.Text = "";

			bot.SendMessage( msg );

			var dt = DateTime.Now;
			lstMessages.Items.Add( "[" + dt.Hour.ToString( "00" ) + ":" + dt.Minute.ToString( "00" ) + "] ME: " + msg );
		}

		private void UpdateUsers()
		{
			if( this.InvokeRequired )
			{
				this.Invoke( new Action(UpdateUsers) );
				return;
			}

			if( lstUsers.DataSource == null && bot != null && bot.Channel != null && bot.Channel.Users != null && bot.Channel.Users.Count > 0 )
				lstUsers.DataSource = bot.Channel.Users;
			else
			{
				lstUsers.Update();
				//CurrencyManager cm = (CurrencyManager)BindingContext[bot.Channel.Users];
				//cm.Refresh();
			}
		}

		private void lstUsers_Format( object sender, ListControlConvertEventArgs e )
		{
			var item = e.ListItem as Lib.IRC.UserInfo;

			if( item == null )
				return;

			e.Value = ((item.IsMod) ? "*" : " ")+item.Nick;
		}
	}
}
