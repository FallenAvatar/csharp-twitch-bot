using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace Lib.Framework
{
	public class Database
	{
		// In case we move it to a config file
		private const string connectionString = "Data Source=ModBotNext.sqlite;Version=3;";

		private static Database _inst = null;
		private static object _instLock = new object();
		public static Database Instance
		{
			get
			{
				if( _inst == null )
				{
					lock( _instLock )
					{
						if( _inst == null )
							_inst = new Database(connectionString);
					}
				}

				return _inst;
			}
		}

		private string ConnStr;

		private Database(string cs)
		{
			this.ConnStr = cs;
		}

		private SQLiteConnection GetConnection()
		{
			var ret = new System.Data.SQLite.SQLiteConnection(this.ConnStr);
			ret.Open();
			
			return ret;
		}

		public IList<IDataRecord> ExecuteQuery(string sql, Dictionary<string, object> parameters = null, CommandType type = CommandType.Text)
		{
			var conn = GetConnection();
			var cmd = new SQLiteCommand(sql, conn);

			if( parameters.Count > 0 )
			{
				foreach( var k in parameters.Keys )
				{
					object v = parameters[k];

					cmd.Parameters.AddWithValue(k,v);
				}
			}

			var i = cmd.ExecuteReader().GetEnumerator();
			var ret = new List<IDataRecord>();
			
			while(i.MoveNext())
			{
				ret.Add((IDataRecord)i.Current);
			}

			conn.Close();

			return ret;
		}

		public int ExecuteNonQuery(string sql, Dictionary<string, object> parameters = null, CommandType type = CommandType.Text)
		{
			var conn = GetConnection();
			var cmd = new SQLiteCommand(sql, conn);

			if (parameters.Count > 0)
			{
				foreach (var k in parameters.Keys)
				{
					object v = parameters[k];

					cmd.Parameters.AddWithValue(k, v);
				}
			}

			var ret = cmd.ExecuteNonQuery();

			conn.Close();

			return ret;
		}

		public object ExecuteScalar(string sql, Dictionary<string, object> parameters = null, CommandType type = CommandType.Text)
		{
			var conn = GetConnection();
			var cmd = new SQLiteCommand(sql, conn);

			if (parameters.Count > 0)
			{
				foreach (var k in parameters.Keys)
				{
					object v = parameters[k];

					cmd.Parameters.AddWithValue(k, v);
				}
			}

			var ret = cmd.ExecuteScalar();

			conn.Close();

			return ret;
		}

		public T ExecuteScalar<T>(string sql, Dictionary<string, object> parameters = null, CommandType type = CommandType.Text)
		{
			object o = ExecuteScalar(sql,parameters,type);

			if( o == DBNull.Value )
				return default(T);

			return (T)o;
		}

		public static class RecordHelper
		{
			public static T Value<T>(IDataRecord row, string col_name, T def = default(T))
			{
				object o = row[col_name];

				if( o == DBNull.Value )
					return def;

				return (T)o;
			}
		}
	}
}
