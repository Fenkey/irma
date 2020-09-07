using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Odbc;
using System.Data.Common;
using System.Data.SqlClient;
using System.Collections.Generic;
using IRMAKit.Log;

namespace IRMAKit.Store
{
	public sealed class OdbcStore : IDBStore
	{
		private string connectionString;
		public string ConnectionString
		{
			get { return this.connectionString; }
		}

		private OdbcConnection connection;
		public DbConnection Connection
		{
			get { return this.connection; }
		}

		private OdbcTransaction transaction = null;
		/*
		public DbTransaction Transaction
		{
			get { return this.transaction; }
		}
		*/

		private int refCount = 0;

		private bool rollbacked = false;
		public bool TransHasRollbacked
		{
			get { return this.rollbacked; }
		}

		// refer to: https://www.connectionstrings.com
		public OdbcStore(string connectionString)
		{
			this.connectionString = connectionString;
			this.connection = new OdbcConnection(this.connectionString);
			// FIX: It's best to do it later. 
			//try { connection.Open(); } catch {}
		}

		public OdbcStore(string driver, string host, int port, string user, string password, string db, string charset)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("Driver=").Append(driver)
				.Append(";Server=").Append(host)
				.Append(";Port=").Append(port)
				.Append(";Database=").Append(db)
				.Append(";UID=").Append(user)
				.Append(";PWD=").Append(password)
				.Append(";Charset=").Append(string.IsNullOrEmpty(charset) ? "utf8" : charset);
			this.connectionString = sb.ToString();
			this.connection = new OdbcConnection(this.connectionString);
			// FIX: It's best to do it later. 
			//try { connection.Open(); } catch {}
		}

		public OdbcStore(string driver, string host, int port, string user, string password, string db, string charset, bool pooling, int maxPoolSize, int connectionLifetime, bool allowUserVariables)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("Driver=").Append(driver)
				.Append(";Server=").Append(host)
				.Append(";Port=").Append(port)
				.Append(";Database=").Append(db)
				.Append(";UID=").Append(user)
				.Append(";PWD=").Append(password)
				.Append(";Charset=").Append(string.IsNullOrEmpty(charset) ? "utf8" : charset)
				.Append(";Pooling=").Append(pooling.ToString().ToLower())
				.Append(";Max Pool Size=").Append(maxPoolSize < 1 ? "1" : maxPoolSize.ToString());
			if (connectionLifetime > 0)
				sb.Append(";Connection Lifetime=").Append(connectionLifetime.ToString());
			if (allowUserVariables)
				sb.Append(";Allow User Variables=true");
			this.connectionString = sb.ToString();
			this.connection = new OdbcConnection(this.connectionString);
			// FIX: It's best to do it later. 
			//try { connection.Open(); } catch {}
		}

		~OdbcStore()
		{
			Close();
		}

		public void Open()
		{
			if (connection.State != ConnectionState.Open) {
				/*
				 * Refer to: https://docs.microsoft.com/zh-cn/dotnet/api/system.data.connectionstate?view=netcore-3.1
				 * Don't touch & rollback the transaction and refCount even if the transaction object is not null.
				 */
				//Logger.ERROR("Kit - Connection state is '{0}' before doing Open()", connection.State);
				if (connection.State == ConnectionState.Broken) {
					connection.Close();
					Logger.WARN("Kit - DB connection has broken while calling Open()");
				}
				connection.Open();
			}
			if (connection.State != ConnectionState.Open && connection.State != ConnectionState.Connecting) {
				Logger.DEBUG("Kit - Connect DB failed. refCount={0}", refCount);
				throw new InvalidOperationException("Connect DB failed");
			}
		}

		public void Close()
		{
			if (connection.State != ConnectionState.Closed && transaction == null)
				connection.Close();
		}

		private void InvalidTransaction(string msg)
		{
			if (transaction != null) {
				transaction.Rollback();
				transaction.Dispose();
				transaction = null;
			}
			Close();
			refCount = 0;
			throw new InvalidOperationException(msg);
		}

		public DbTransaction BeginTransaction(out int rc)
		{
			if (refCount < 0) {
				Logger.WARN("Kit - Begin transaction with invalid reference count. refCound={0}", refCount);
				InvalidTransaction("Invalid reference count");
			}

			Open();

			if (refCount == 0 && transaction == null) {
				transaction = connection.BeginTransaction();
				rollbacked = false;
			}

			if (transaction == null) {
				Logger.WARN("Kit - Begin transaction failed");
				InvalidTransaction("Begin transaction failed");
			}

			rc = ++refCount;
			Logger.DEBUG("Kit - Begin transaction. refCound={0}, rc={1}", refCount, rc);
			/*
			 * We don't want to expose the internal transaction object for avoiding the errors
			 * of artificial multi-transaction nesting in application layer. BTW, you don't
			 * worry about the statement of: using (DbTransaction t = null) { ... }
			 *
			 * return transaction;
			 */
			return null;
		}

		public bool CommitTransaction(int rc, bool force=false)
		{
			if (transaction == null) {
				Logger.WARN("Kit - Commit invalid transaction. refCound={0}, rc={1}", refCount, rc);
				InvalidTransaction("Invalid transaction");
			}
			/*
			 * refCount < rc: It indicates that there are more than one times COMMIT / ROLLBACK
			 * since BeginTransaction(), adjust refCount.
			 */
			if (refCount < rc)
				refCount = rc;

			if (refCount <= 0) {
				Logger.WARN("Kit - Commit invalid reference count. refCound={0}, rc={1}", refCount, rc);
				InvalidTransaction("Invalid reference count");
			}

			bool ok = false;
			if (--refCount == 0) {
				if (!rollbacked || force) {
					transaction.Commit();
					ok = true;
				} else
					transaction.Rollback();
				transaction.Dispose();
				transaction = null;
				Close();
			}
			Logger.DEBUG("Kit - Commit transaction. commitReally={0}, refCount={1}, rc={2}", ok, refCount, rc);
			return ok;
		}

		public bool RollbackTransaction(int rc)
		{
			/*
			 * refCount < rc: It indicates that there are more than one times COMMIT / ROLLBACK
			 * since BeginTransaction(), adjust refCount. At the same time, we regard it as an
			 * effective rolled back only when refCount is not necessary to adjust.
			 */
			if (refCount < rc)
				refCount = rc;
			else
				rollbacked = true;

			if (refCount <= 0) {
				Logger.WARN("Kit - Rollback invalid reference count. refCound={0}, rc={1}", refCount, rc);
				InvalidTransaction("Invalid reference count");
			}

			bool ok = false;
			if (--refCount == 0) {
				if (transaction != null) {
					transaction.Rollback();
					transaction.Dispose();
					transaction = null;
					Close();
				}
				ok = true;
			}
			Logger.DEBUG("Kit - Rollback transaction. rollbackReally={0}, refCount={1}, rc={2}", ok, refCount, rc);
			return ok;
		}

		/*
		 * Usage reference:
		 * using (DbDataReader reader = dbs.Query("SELECT * FROM foo;", 10)) {
		 *	 while (reader.Read()) { ... }
		 * }
		 */
		public DbDataReader Query(string sql, int timeout)
		{
			Open();
			DbDataReader dr = null;
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = sql;
				if (timeout > 0)
					cmd.CommandTimeout = timeout;
				dr = cmd.ExecuteReader();
			} catch (Exception e) {
				throw;
			} finally {
				/*
				 * It's a bit different from the case of using _Fill_, you have
				 * to close it after _DbDataReader_ be called in application.
				 */
				//Close();
			}
			return dr;
		}

		public DbDataReader Query(string sql)
		{
			return Query(sql, 0);
		}

		public DbDataReader Query(string sql, Dictionary<string, object> parameters, int timeout)
		{
			Open();
			DbDataReader dr = null;
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = sql;
				if (timeout > 0)
					cmd.CommandTimeout = timeout;
				if (parameters != null) {
					foreach (KeyValuePair<string, object> p in parameters)
						((OdbcParameterCollection)cmd.Parameters).AddWithValue(p.Key, p.Value);
				}
				dr = (DbDataReader)cmd.ExecuteReader();
			} catch (Exception e) {
				throw;
			} finally {
				/*
				 * It's a bit different from the case of using _Fill_, you have
				 * to close it after _DbDataReader_ be called in application.
				 */
				//Close();
			}
			return dr;
		}

		public DbDataReader Query(string sql, Dictionary<string, object> parameters)
		{
			return Query(sql, parameters, 0);
		}

		public DbDataReader Query(string sql, int timeout, params DbParameter[] parameters)
		{
			Open();
			DbDataReader dr = null;
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = sql;
				if (timeout > 0)
					cmd.CommandTimeout = timeout;
				if (parameters != null && parameters.Length > 0) {
					foreach (DbParameter p in parameters) {
						if ((p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Input) && p.Value == null)
							p.Value = DBNull.Value;
					}
					cmd.Parameters.AddRange(parameters);
				}
				dr = (DbDataReader)cmd.ExecuteReader();
			} catch (Exception e) {
				throw;
			} finally {
				/*
				 * It's a bit different from the case of using _Fill_, you have
				 * to close it after _DbDataReader_ be called in application.
				 */
				//Close();
			}
			return dr;
		}

		public DbDataReader Query(string sql, int timeout, params OdbcParameter[] parameters)
		{
			return Query(sql, timeout, (DbParameter[])parameters);
		}

		public DbDataReader Query(string sql, params DbParameter[] parameters)
		{
			return Query(sql, 0, (DbParameter[])parameters);
		}

		public DbDataReader Query(string sql, params OdbcParameter[] parameters)
		{
			return Query(sql, 0, (DbParameter[])parameters);
		}

		public object QuerySingle(string sql, int timeout)
		{
			Open();
			object obj = null;
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = sql;
				if (timeout > 0)
					cmd.CommandTimeout = timeout;
				obj = cmd.ExecuteScalar();
			} catch (Exception e) {
				throw;
			} finally {
				Close();
			}
			return obj;
		}

		public object QuerySingle(string sql)
		{
			return QuerySingle(sql, 0);
		}

		public object QuerySingle(string sql, Dictionary<string, object> parameters, int timeout)
		{
			Open();
			object obj = null;
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = sql;
				if (timeout > 0)
					cmd.CommandTimeout = timeout;
				if (parameters != null) {
					foreach (KeyValuePair<string, object> p in parameters)
						((OdbcParameterCollection)cmd.Parameters).AddWithValue(p.Key, p.Value);
				}
				obj = cmd.ExecuteScalar();
			} catch (Exception e) {
				throw;
			} finally {
				Close();
			}
			return obj;
		}

		public object QuerySingle(string sql, Dictionary<string, object> parameters)
		{
			return QuerySingle(sql, parameters, 0);
		}

		public object QuerySingle(string sql, int timeout, params DbParameter[] parameters)
		{
			Open();
			object obj = null;
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = sql;
				if (timeout > 0)
					cmd.CommandTimeout = timeout;
				if (parameters != null && parameters.Length > 0) {
					foreach (DbParameter p in parameters) {
						if ((p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Input) && p.Value == null)
							p.Value = DBNull.Value;
					}
					cmd.Parameters.AddRange(parameters);
				}
				obj = cmd.ExecuteScalar();
			} catch (Exception e) {
				throw;
			} finally {
				Close();
			}
			return obj;
		}

		public object QuerySingle(string sql, int timeout, params OdbcParameter[] parameters)
		{
			return QuerySingle(sql, timeout, (DbParameter[])parameters);
		}

		public object QuerySingle(string sql, params DbParameter[] parameters)
		{
			return QuerySingle(sql, 0, (DbParameter[])parameters);
		}

		public object QuerySingle(string sql, params OdbcParameter[] parameters)
		{
			return QuerySingle(sql, 0, (DbParameter[])parameters);
		}

		public DataSet QueryDataSet(string sql, int timeout)
		{
			Open();
			DataSet ds = null;
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = sql;
				if (timeout > 0)
					cmd.CommandTimeout = timeout;

				ds = new DataSet();
				OdbcDataAdapter dap = new OdbcDataAdapter();
				dap.SelectCommand = cmd;
				dap.Fill(ds);
			} catch (Exception e) {
				throw;
			} finally {
				Close();
			}
			return ds;
		}

		public DataSet QueryDataSet(string sql)
		{
			return QueryDataSet(sql, 0);
		}

		public DataSet QueryDataSet(string sql, Dictionary<string, object> parameters, int timeout)
		{
			Open();
			DataSet ds = null;
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = sql;
				if (timeout > 0)
					cmd.CommandTimeout = timeout;
				if (parameters != null) {
					foreach (KeyValuePair<string, object> p in parameters)
						((OdbcParameterCollection)cmd.Parameters).AddWithValue(p.Key, p.Value);
				}

				ds = new DataSet();
				OdbcDataAdapter dap = new OdbcDataAdapter();
				dap.SelectCommand = cmd;
				dap.Fill(ds);
			} catch (Exception e) {
				throw;
			} finally {
				Close();
			}
			return ds;
		}

		public DataSet QueryDataSet(string sql, Dictionary<string, object> parameters)
		{
			return QueryDataSet(sql, parameters, 0);
		}

		public DataSet QueryDataSet(string sql, int timeout, params DbParameter[] parameters)
		{
			Open();
			DataSet ds = null;
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = sql;
				if (timeout > 0)
					cmd.CommandTimeout = timeout;
				if (parameters != null && parameters.Length > 0) {
					foreach (DbParameter p in parameters) {
						if ((p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Input) && p.Value == null)
							p.Value = DBNull.Value;
					}
					cmd.Parameters.AddRange(parameters);
				}

				ds = new DataSet();
				OdbcDataAdapter dap = new OdbcDataAdapter();
				dap.SelectCommand = cmd;
				dap.Fill(ds);
			} catch (Exception e) {
				throw;
			} finally {
				Close();
			}
			return ds;
		}

		public DataSet QueryDataSet(string sql, int timeout, params OdbcParameter[] parameters)
		{
			return QueryDataSet(sql, timeout, (DbParameter[])parameters);
		}

		public DataSet QueryDataSet(string sql, params DbParameter[] parameters)
		{
			return QueryDataSet(sql, 0, (DbParameter[])parameters);
		}

		public DataSet QueryDataSet(string sql, params OdbcParameter[] parameters)
		{
			return QueryDataSet(sql, 0, (DbParameter[])parameters);
		}

		public int Execute(string sql, Dictionary<string, object> parameters, int timeout)
		{
			Open();
			int n = 0;
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = sql;
				if (timeout > 0)
					cmd.CommandTimeout = timeout;
				if (transaction != null)
					cmd.Transaction = transaction;
				if (parameters != null) {
					foreach (KeyValuePair<string, object> p in parameters)
						((OdbcParameterCollection)cmd.Parameters).AddWithValue(p.Key, p.Value);
				}
				n = cmd.ExecuteNonQuery();
			} catch (Exception e) {
				throw;
			} finally {
				Close();
			}
			return n;
		}

		public int Execute(string sql, int timeout)
		{
			return Execute(sql, (Dictionary<string, object>)null, timeout);
		}

		public int Execute(string sql)
		{
			return Execute(sql, 0);
		}

		public int Execute(string sql, Dictionary<string, object> parameters)
		{
			return Execute(sql, parameters, 0);
		}

		public int Execute(string sql, int timeout, params DbParameter[] parameters)
		{
			Open();
			int n = 0;
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = sql;
				if (timeout > 0)
					cmd.CommandTimeout = timeout;
				if (transaction != null)
					cmd.Transaction = transaction;
				if (parameters != null && parameters.Length > 0) {
					foreach (DbParameter p in parameters) {
						if ((p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Input) && p.Value == null)
							p.Value = DBNull.Value;
					}
					cmd.Parameters.AddRange(parameters);
				}
				n = cmd.ExecuteNonQuery();
			} catch (Exception e) {
				throw;
			} finally {
				Close();
			}
			return n;
		}

		public int Execute(string sql, int timeout, params OdbcParameter[] parameters)
		{
			return Execute(sql, timeout, (DbParameter[])parameters);
		}

		public int Execute(string sql, params DbParameter[] parameters)
		{
			return Execute(sql, 0, (DbParameter[])parameters);
		}

		public int Execute(string sql, params OdbcParameter[] parameters)
		{
			return Execute(sql, 0, (DbParameter[])parameters);
		}

		public int Execute(ref DbCommand cmd)
		{
			Open();
			int n = 0;
			try {
				n = cmd.ExecuteNonQuery();
			} catch (Exception e) {
				throw;
			} finally {
				Close();
			}
			return n;
		}

		public bool BulkInsert(string tableName, DataTable dt, int timeout)
		{
			bool ret = false;
			if (dt == null || dt.Rows.Count <= 0)
				return ret;

			Open();
			DataTable dtInsert = new DataTable();
			string cols = string.Join(",", dt.Columns.Cast<DataColumn>().Select(r => r.ColumnName));
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = string.Format("SELECT {0} FROM {1} LIMIT 0", cols, tableName);
				OdbcDataAdapter dap = new OdbcDataAdapter(cmd);
				OdbcCommandBuilder cb = new OdbcCommandBuilder(dap);
				cmd = cb.GetInsertCommand();
				if (timeout > 0)
					cmd.CommandTimeout = timeout;
				if (transaction != null)
					cmd.Transaction = transaction;
				dap.InsertCommand = cmd;
				dap.Fill(dtInsert);
				foreach (DataRow dr in dt.Rows)
					dtInsert.Rows.Add(dr.ItemArray);
				dap.Update(dtInsert);
				ret = true;
			} catch (Exception e) {
				throw;
			} finally {
				dtInsert = null;
				Close();
			}
			return ret;
		}

		public bool BulkInsert(string tableName, DataTable dt)
		{
			return BulkInsert(tableName, dt, 0);
		}

		/*
		 * Usage reference:
		 * try {
		 *	 DataSet kids;
		 *	 Dictionary<string, object> op = new Dictionary<string, object>() {{"@age", null}};
		 *	 dbs.Procedure("myproc", new Dictionary<string, object>() {{"@name", "Jack"}}, ref op, out kids);
		 *	 int age = (int)op["@age"];
		 *	 ...
		 * }
		 */
		public bool Procedure(string proc, Dictionary<string, object> inParams, ref Dictionary<string, object> outParams, out DataSet ds, int timeout)
		{
			Open();
			bool ret = false;
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = proc;
				cmd.CommandType = System.Data.CommandType.StoredProcedure;
				if (timeout > 0)
					cmd.CommandTimeout = timeout;
				if (transaction != null)
					cmd.Transaction = transaction;
				if (inParams != null) {
					foreach (KeyValuePair<string, object> p in inParams)
						((OdbcParameterCollection)cmd.Parameters).AddWithValue(p.Key, p.Value);
				}
				if (outParams != null) {
					foreach (KeyValuePair<string, object> p in outParams) {
						OdbcParameter mp = ((OdbcParameterCollection)cmd.Parameters).AddWithValue(p.Key, p.Value);
						mp.Direction = ParameterDirection.Output;
					}
				}
				ds = new DataSet();
				OdbcDataAdapter dap = new OdbcDataAdapter();
				dap.SelectCommand = cmd;
				dap.Fill(ds);

				if (outParams != null) {
					// Clone a new collection object to return.
					Dictionary<string, object> outNew = new Dictionary<string, object>();
					foreach (KeyValuePair<string, object> p in outParams)
						outNew[p.Key] = cmd.Parameters[p.Key].Value;
					outParams = outNew;
				}
				ret = true;
			} catch (Exception e) {
				throw;
			} finally {
				Close();
			}
			return ret;
		}

		public bool Procedure(string proc, Dictionary<string, object> inParams, ref Dictionary<string, object> outParams, out DataSet ds)
		{
			return Procedure(proc, inParams, ref outParams, out ds, 0);
		}

		public bool Procedure(string proc, Dictionary<string, object> inParams, out DataSet ds)
		{
			Dictionary<string, object> outParams = null;
			return Procedure(proc, inParams, ref outParams, out ds);
		}

		public bool Procedure(string proc, DbParameter[] parameters, out DataSet ds)
		{
			Open();
			bool ret = false;
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = proc;
				cmd.CommandType = System.Data.CommandType.StoredProcedure;
				if (transaction != null)
					cmd.Transaction = transaction;
				if (parameters != null && parameters.Length > 0) {
					foreach (DbParameter p in parameters) {
						if ((p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Input) && p.Value == null)
							p.Value = DBNull.Value;
					}
					cmd.Parameters.AddRange(parameters);
				}
				ds = new DataSet();
				OdbcDataAdapter dap = new OdbcDataAdapter();
				dap.SelectCommand = cmd;
				dap.Fill(ds);
				ret = true;
			} catch (Exception e) {
				throw;
			} finally {
				Close();
			}
			return ret;
		}

		public bool Procedure(string proc, Dictionary<string, object> inParams, ref Dictionary<string, object> outParams)
		{
			Open();
			bool ret = false;
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = proc;
				cmd.CommandType = System.Data.CommandType.StoredProcedure;
				if (transaction != null)
					cmd.Transaction = transaction;
				if (inParams != null) {
					foreach (KeyValuePair<string, object> p in inParams)
						((OdbcParameterCollection)cmd.Parameters).AddWithValue(p.Key, p.Value);
				}
				if (outParams != null) {
					foreach (KeyValuePair<string, object> p in outParams) {
						OdbcParameter mp = ((OdbcParameterCollection)cmd.Parameters).AddWithValue(p.Key, p.Value);
						mp.Direction = ParameterDirection.Output;
					}
				}
				cmd.ExecuteScalar();
				if (outParams != null) {
					// Clone a new collection object to return.
					Dictionary<string, object> outNew = new Dictionary<string, object>();
					foreach (KeyValuePair<string, object> p in outParams)
						outNew[p.Key] = cmd.Parameters[p.Key].Value;
					outParams = outNew;
				}
				ret = true;
			} catch (Exception e) {
				throw;
			} finally {
				Close();
			}
			return ret;
		}

		public bool Procedure(string proc, Dictionary<string, object> inParams)
		{
			Dictionary<string, object> outParams = null;
			return Procedure(proc, inParams, ref outParams);
		}

		public bool Procedure(string proc, params DbParameter[] parameters)
		{
			Open();
			bool ret = false;
			try {
				OdbcCommand cmd = connection.CreateCommand();
				cmd.CommandText = proc;
				cmd.CommandType = System.Data.CommandType.StoredProcedure;
				if (transaction != null)
					cmd.Transaction = transaction;
				if (parameters != null && parameters.Length > 0) {
					foreach (DbParameter p in parameters) {
						if ((p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Input) && p.Value == null)
							p.Value = DBNull.Value;
					}
					cmd.Parameters.AddRange(parameters);
				}
				cmd.ExecuteScalar();
				ret = true;
			} catch (Exception e) {
				throw;
			} finally {
				Close();
			}
			return ret;
		}

		public bool Procedure(string proc, params OdbcParameter[] parameters)
		{
			return Procedure(proc, (DbParameter[])parameters);
		}

		public void Reset()
		{
			try {
				if (transaction != null) {
					transaction.Rollback();
					transaction.Dispose();
				}
				Close();
			} finally {
				refCount = 0;
				transaction = null;
				rollbacked = false;
			}
		}
	}
}
