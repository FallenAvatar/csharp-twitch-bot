using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lib.Data
{
	public class User : Framework.ActiveRecord
	{
		public static IList<User> FindAll()
		{
			return FindAll<User>("Users");
		}

		protected override string Table { get { return "Users"; } }

		public string Username;

		protected override void LoadData(System.Data.IDataRecord row)
		{
			this.Username = Framework.Database.RecordHelper.Value<string>(row, "Username");
		}

		protected override void GetData(ref Dictionary<string, object> data)
		{
			data["Username"] = this.Username;
		}
	}
}
