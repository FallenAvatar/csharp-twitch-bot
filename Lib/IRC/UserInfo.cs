using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lib.IRC
{
	public class UserInfo : IEquatable<UserInfo>, IComparable<UserInfo>
	{
		public string Nick;
		public string User;
		public string Hostname;
		public bool IsMod;

		private Client client;

		public UserInfo( Client client )
		{
			this.client = client;
			this.IsMod = false;
		}

		public bool Equals( UserInfo other )
		{
			return this.Nick.Equals( other.Nick );
		}

		public int CompareTo( UserInfo other )
		{
			if( other == null )
				return 1;
			else if( this.IsMod && !other.IsMod )
				return 1;
			else if( !this.IsMod && other.IsMod )
				return -1;
			else
				return this.Nick.CompareTo( other.Nick );
		}
	}
}
