using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Lib.Framework
{
	public abstract class ActiveRecord
	{
		protected static IList<T> FindAll<T>(string table) where T : ActiveRecord, new()
		{
			string sql = "SELECT * FROM ["+table+"]";
			var rows = Database.Instance.ExecuteQuery(sql);
			var ret = new List<T>();

			foreach( var row in rows )
			{
				var temp = new T();
				temp.Load(row);
				ret.Add(temp);
			}

			return ret;
		}

		protected abstract string Table { get; }
		public long? ID { get; protected set; }
		
		protected ActiveRecord(long? id = null)
		{
			if( id != null )
				Load(id.Value);
		}

		protected ActiveRecord(IDataRecord row)
		{
			Load(row);
		}

		protected void Load(long id)
		{
			var sql = "SELECT * FROM ["+this.Table+"] WHERE [ID] = @id";
			var ps = new Dictionary<string, object> {
				{ "@id", id }
			};

			var rows = Database.Instance.ExecuteQuery(sql, ps);

			if( rows.Count > 1 )
				throw new DataException("Multiple rows found with primary id ["+id.ToString()+"].");
			else if( rows.Count <= 0 )
				throw new ArgumentOutOfRangeException("id", "No rows found with primary id [" + id.ToString() + "].");

			Load(rows[0]);
		}

		protected void Load(IDataRecord row)
		{
			if( row == null )
				throw new ArgumentNullException("row");

			this.ID = Database.RecordHelper.Value<long>(row, "ID");
			LoadData(row);
		}

		protected abstract void LoadData(IDataRecord row);

		public void Save()
		{
			var data = new Dictionary<string, object>();

			this.GetData(ref data);

			string sql = null;
			var ps = new Dictionary<string,object>();
			if( this.ID == null ) // Insert
			{
				sql = "INSERT INTO ["+this.Table+"](";
				var sql2 = ") VALUES (";

				bool first = true;
				int count = 0;
				foreach( string k in data.Keys )
				{
					var v = data[k];

					if( !first )
					{
						sql += ", ";
						sql2 += ", ";
					}

					first = false;

					sql += "["+k+"]";
					var pname = "@col"+count.ToString();
					sql2 += pname;

					ps[pname] = v;
					count++;
				}

				sql = sql + sql2 + "); SELECT last_insert_rowid();";

				this.ID = Database.Instance.ExecuteScalar<long>(sql, ps);
			}
			else // Update
			{
				sql = "UPDATE ["+this.Table+"] SET ";

				bool first = true;
				int count = 0;
				foreach( string k in data.Keys )
				{
					var v = data[k];

					if( !first )
						sql += ", ";

					first = false;

					var pname = "@col"+count.ToString();
					sql += "["+k+"] = "+pname;

					ps[pname] = v;
					count++;
				}

				sql += " WHERE [ID] = @id";
				ps["@id"] = this.ID.Value;

				Database.Instance.ExecuteNonQuery(sql, ps);
			}
		}

		protected abstract void GetData(ref Dictionary<string, object> data);

		public override string ToString()
		{
			return "ActiveRecord ["+this.Table+"] ("+((this.ID == null) ? "NEW" : this.ID.Value.ToString())+")";
		}
	}
}
