using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;

namespace IRMAKit.Store
{
	public class DBStoreInvalidException : Exception
	{
		public DBStoreInvalidException() : base("DB Store is invalid") {}

		public DBStoreInvalidException(string msg) : base(msg) {}
	}

	public interface IDBStore
	{
		/// <summary>
		/// Connection string
		/// </summary>
		string ConnectionString { get; }

		/// <summary>
		/// Connection object
		/// </summary>
		DbConnection Connection { get; }

		/// <summary>
		/// Has transaction ever rollbacked ?
		/// </summary>
		bool TransHasRollbacked { get; }

		/// <summary>
		/// Open DB
		/// </summary>
		void Open();

		/// <summary>
		/// Close DB
		/// </summary>
		void Close();

		/// <summary>
		/// Transaction begin
		/// </summary>
		/// <param name="rc">Reference count</param>
		DbTransaction BeginTransaction(out int rc);

		/// <summary>
		/// Transaction commit
		/// </summary>
		/// <param name="rc">Reference count</param>
		/// <param name="force">Force to commit</param>
		bool CommitTransaction(int rc, bool force=false);

		/// <summary>
		/// Transaction rollback
		/// </summary>
		/// <param name="rc">Reference count</param>
		bool RollbackTransaction(int rc);

		/// <summary>
		/// SQL query
		/// </summary>
		/// <param name="sql">SQL</param>
		DbDataReader Query(string sql);

		/// <summary>
		/// SQL query
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="timeout">Query timeout</param>
		DbDataReader Query(string sql, int timeout);

		/// <summary>
		/// SQL query
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="parameters">Query parameters</param>
		DbDataReader Query(string sql, Dictionary<string, object> parameters);

		/// <summary>
		/// SQL query
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="parameters">Query parameters</param>
		/// <param name="timeout">Query timeout</param>
		DbDataReader Query(string sql, Dictionary<string, object> parameters, int timeout);

		/// <summary>
		/// SQL query
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="parameters">Query parameters</param>
		DbDataReader Query(string sql, params DbParameter[] parameters);

		/// <summary>
		/// SQL query
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="timeout">Query timeout</param>
		/// <param name="parameters">Query parameters</param>
		DbDataReader Query(string sql, int timeout, params DbParameter[] parameters);

		/// <summary>
		/// Query single
		/// </summary>
		/// <param name="sql">SQL</param>
		object QuerySingle(string sql);

		/// <summary>
		/// Query single
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="timeout">Query timeout</param>
		object QuerySingle(string sql, int timeout);

		/// <summary>
		/// Query single
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="parameters">Query parameters</param>
		object QuerySingle(string sql, Dictionary<string, object> parameters);

		/// <summary>
		/// Query single
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="timeout">Query timeout</param>
		/// <param name="parameters">Query parameters</param>
		object QuerySingle(string sql, Dictionary<string, object> parameters, int timeout);

		/// <summary>
		/// Query single
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="parameters">Query parameters</param>
		object QuerySingle(string sql, params DbParameter[] parameters);

		/// <summary>
		/// Query single
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="timeout">Query timeout</param>
		/// <param name="parameters">Query parameters</param>
		object QuerySingle(string sql, int timeout, params DbParameter[] parameters);

		/// <summary>
		/// Query dataset
		/// </summary>
		/// <param name="sql">SQL</param>
		DataSet QueryDataSet(string sql);

		/// <summary>
		/// Query dataset
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="timeout">Query timeout</param>
		DataSet QueryDataSet(string sql, int timeout);

		/// <summary>
		/// Query dataset
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="parameters">Query parameters</param>
		DataSet QueryDataSet(string sql, Dictionary<string, object> parameters);

		/// <summary>
		/// Query dataset
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="parameters">Query parameters</param>
		/// <param name="timeout">Query timeout</param>
		DataSet QueryDataSet(string sql, Dictionary<string, object> parameters, int timeout);

		/// <summary>
		/// Query dataset
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="parameters">Query parameters</param>
		DataSet QueryDataSet(string sql, params DbParameter[] parameters);

		/// <summary>
		/// Query dataset
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="timeout">Query timeout</param>
		/// <param name="parameters">Query parameters</param>
		DataSet QueryDataSet(string sql, int timeout, params DbParameter[] parameters);

		/// <summary>
		/// SQL execute
		/// </summary>
		/// <param name="sql">SQL</param>
		int Execute(string sql);

		/// <summary>
		/// SQL execute
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="timeout">Execute timeout</param>
		int Execute(string sql, int timeout);

		/// <summary>
		/// SQL execute
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="parameters">Query parameters</param>
		int Execute(string sql, Dictionary<string, object> parameters);

		/// <summary>
		/// SQL execute
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="parameters">Query parameters</param>
		/// <param name="timeout">Execute timeout</param>
		int Execute(string sql, Dictionary<string, object> parameters, int timeout);

		/// <summary>
		/// SQL execute
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="parameters">Query parameters</param>
		int Execute(string sql, params DbParameter[] parameters);

		/// <summary>
		/// SQL execute
		/// </summary>
		/// <param name="sql">SQL</param>
		/// <param name="timeout">Execute timeout</param>
		/// <param name="parameters">Query parameters</param>
		int Execute(string sql, int timeout, params DbParameter[] parameters);

		/// <summary>
		/// SQL execute
		/// </summary>
		/// <param name="cmd">DB command</param>
		int Execute(ref DbCommand cmd);

		/// <summary>
		/// Bulk insert
		/// </summary>
		/// <param name="tableName">Table name</param>
		/// <param name="dt">Datatable being inserted</param>
		bool BulkInsert(string tableName, DataTable dt);

		/// <summary>
		/// Bulk insert
		/// </summary>
		/// <param name="tableName">Table name</param>
		/// <param name="dt">Datatable being inserted</param>
		/// <param name="timeout">Insert timeout</param>
		bool BulkInsert(string tableName, DataTable dt, int timeout);

		/// <summary>
		/// Procedure
		/// </summary>
		/// <param name="proc">Procedure name</param>
		/// <param name="inParams">Parameters inputted</param>
		/// <param name="outParams">Parameters outputted</param>
		/// <param name="ds">Dataset outputted</param>
		/// <param name="timeout">Insert timeout</param>
		bool Procedure(string proc, Dictionary<string, object> inParams, ref Dictionary<string, object> outParams, out DataSet ds, int timeout);

		/// <summary>
		/// Procedure
		/// </summary>
		/// <param name="proc">Procedure name</param>
		/// <param name="inParams">Parameters inputted</param>
		/// <param name="outParams">Parameters outputted</param>
		/// <param name="ds">Dataset outputted</param>
		bool Procedure(string proc, Dictionary<string, object> inParams, ref Dictionary<string, object> outParams, out DataSet ds);

		/// <summary>
		/// Procedure
		/// </summary>
		/// <param name="proc">Procedure name</param>
		/// <param name="inParams">Parameters inputted</param>
		/// <param name="outParams">Parameters outputted</param>
		bool Procedure(string proc, Dictionary<string, object> inParamrs, ref Dictionary<string, object> outParams);

		/// <summary>
		/// Procedure
		/// </summary>
		/// <param name="proc">Procedure name</param>
		/// <param name="parameter">Parameters inputted</param>
		/// <param name="ds">Dataset outputted</param>
		bool Procedure(string proc, DbParameter[] parameters, out DataSet ds);

		/// <summary>
		/// Procedure
		/// </summary>
		/// <param name="proc">Procedure name</param>
		/// <param name="inParams">Parameters inputted</param>
		/// <param name="ds">Dataset outputted</param>
		bool Procedure(string proc, Dictionary<string, object> inParams, out DataSet ds);

		/// <summary>
		/// Procedure
		/// </summary>
		/// <param name="proc">Procedure name</param>
		/// <param name="inParams">Parameters inputted</param>
		bool Procedure(string proc, Dictionary<string, object> inParams);

		/// <summary>
		/// Procedure
		/// </summary>
		/// <param name="proc">Procedure name</param>
		/// <param name="parameter">Parameters inputted</param>
		bool Procedure(string proc, params DbParameter[] parameters);

		/// <summary>
		/// Reset
		/// </summary>
		void Reset();
	}
}
