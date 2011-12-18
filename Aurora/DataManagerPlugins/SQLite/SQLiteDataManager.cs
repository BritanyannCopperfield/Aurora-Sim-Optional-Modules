/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

#define Experimental
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Aurora.DataManager.Migration;
using Aurora.Framework;
using System.Data.SQLite;
using OpenMetaverse;
using OpenSim.Framework;
using log4net;

namespace Aurora.DataManager.SQLite
{
    public class SQLiteLoader : DataManagerBase
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private SQLiteConnection m_Connection;

        protected Dictionary<string, FieldInfo> m_Fields = new Dictionary<string, FieldInfo>();

//        private static bool m_spammedmessage = false;
        public SQLiteLoader()
        {
            try
            {
                if (System.IO.File.Exists("System.Data.SQLite.dll"))
                   System.IO.File.Delete("System.Data.SQLite.dll");
                string fileName = System.IntPtr.Size == 4 ? "System.Data.SQLitex86.dll" : "System.Data.SQLitex64.dll";
                System.IO.File.Copy(fileName, "System.Data.SQLite.dll",true);
            }
            catch
//            catch (Exception ex)
            {
//                if(!m_spammedmessage)
//                    OpenSim.Framework.MainConsole.Instance.Output("[SQLite]: Failed to copy SQLite dll file, may have issues with SQLite! (Can be caused by running multiple instances in the same bin, if so, ignore this warning) " + ex.ToString(), log4net.Core.Level.Emergency);
//                m_spammedmessage = true;
            }
        }

        public override string Identifier
        {
            get { return "SQLiteConnector"; }
        }

        public override void ConnectToDatabase(string connectionString, string migratorName, bool validateTables)
        {
            string[] s1 = connectionString.Split(new[] { "Data Source=", "," }, StringSplitOptions.RemoveEmptyEntries);
            if (Path.GetFileName(s1[0]) == s1[0]) //Only add this if we arn't an absolute path already
                connectionString = connectionString.Replace("Data Source=", "Data Source=" + Util.BasePathCombine("") + "\\");
            m_Connection = new SQLiteConnection(connectionString);
            m_Connection.Open();
            var migrationManager = new MigrationManager(this, migratorName, validateTables);
            migrationManager.DetermineOperation();
            migrationManager.ExecuteOperation();
        }

        protected IDataReader ExecuteReader(SQLiteCommand cmd)
        {
            try
            {
                var newConnection =
                    (SQLiteConnection) (m_Connection).Clone();
                if (newConnection.State != ConnectionState.Open)
                    newConnection.Open();
                cmd.Connection = newConnection;
                SQLiteDataReader reader = cmd.ExecuteReader();
                return reader;
            }
            catch (SQLiteException ex)
            {
                m_log.Warn("[SQLiteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " +
                           ex);
                //throw ex;
            }
            catch (Exception ex)
            {
                m_log.Warn("[SQLiteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " +
                           ex);
                throw ex;
            }
            return null;
        }

        protected void PrepReader(ref SQLiteCommand cmd)
        {
            try
            {
#if Experimental
                var newConnection = m_Connection;
#else
                var newConnection =
                    (SQLiteConnection)((ICloneable)m_Connection).Clone();
#endif
                if (newConnection.State != ConnectionState.Open)
                    newConnection.Open();
                cmd.Connection = newConnection;
            }
            catch (SQLiteException ex)
            {
                m_log.Warn("[SQLiteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " +
                           ex);
                //throw ex;
            }
            catch (Exception ex)
            {
                m_log.Warn("[SQLiteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " +
                           ex);
                throw ex;
            }
        }

        protected SQLiteCommand PrepReader(string query)
        {
            try
            {
/*#if Experimental
                var newConnection = m_Connection;
#else*/
                var newConnection =
                    (SQLiteConnection) (m_Connection).Clone();
//#endif
                if (newConnection.State != ConnectionState.Open)
                    newConnection.Open();
                var cmd = newConnection.CreateCommand();
                cmd.CommandText = query;
                return cmd;
            }
            catch (SQLiteException)
            {
                //throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

        protected int ExecuteNonQuery(SQLiteCommand cmd)
        {
            try
            {
                lock (m_Connection)
                {
/*#if Experimental
                    var newConnection = m_Connection;
#else*/
                    var newConnection =
                        (SQLiteConnection) (m_Connection).Clone();
//#endif
                    if (newConnection.State != ConnectionState.Open)
                        newConnection.Open();
                    cmd.Connection = newConnection;
                    UnescapeSQL(cmd);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (SQLiteException ex)
            {
                m_log.Warn("[SQLiteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " +
                           ex);
            }
            catch (Exception ex)
            {
                m_log.Warn("[SQLiteDataManager]: Exception processing command: " + cmd.CommandText + ", Exception: " +
                           ex);
                throw ex;
            }
            return 0;
        }

        private static void UnescapeSQL(SQLiteCommand cmd)
        {
            foreach (SQLiteParameter v in cmd.Parameters)
            {
                if (v.Value.ToString().Contains("\\'"))
                {
                    v.Value = v.Value.ToString().Replace("\\'", "\'");
                }
                if (v.Value.ToString().Contains("\\\""))
                {
                    v.Value = v.Value.ToString().Replace("\\\"", "\"");
                }
            }
        }

        protected IDataReader GetReader(SQLiteCommand cmd)
        {
            return ExecuteReader(cmd);
        }

        protected void CloseReaderCommand(SQLiteCommand cmd)
        {
            cmd.Connection.Close();
            cmd.Parameters.Clear();
            //cmd.Dispose ();
        }

        public override List<string> Query(string keyRow, object keyValue, string table, string wantedValue)
        {
            string query = "";
            Dictionary<string, object> ps = new Dictionary<string, object>();
            if (keyRow == "")
            {
                query = String.Format("select {0} from {1}",
                                      wantedValue, table);
            }
            else
            {
                ps[":" + keyRow.Replace("`", "")] = keyValue;
                query = String.Format("select {0} from {1} where {2} = :{3}",
                                      wantedValue, table, keyRow, keyRow.Replace("`", ""));
            }
            SQLiteCommand cmd = PrepReader(query);
            AddParams(ref cmd, ps);
            using (IDataReader reader = cmd.ExecuteReader())
            {
                var RetVal = new List<string>();
                try
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader[i] is byte[])
                                RetVal.Add(Utils.BytesToString((byte[]) reader[i]));
                            else
                                RetVal.Add(reader[i].ToString());
                        }
                    }
                    //reader.Close();
                    CloseReaderCommand(cmd);
                }
                catch
                {
                }
                return RetVal;
            }
        }

        private void AddParams(ref SQLiteCommand cmd, Dictionary<string, object> ps)
        {
            foreach (KeyValuePair<string, object> p in ps)
                cmd.Parameters.AddWithValue(p.Key, p.Value);
        }

        public override List<string> Query(string whereClause, string table, string wantedValue)
        {
            string query = "";
            query = String.Format("select {0} from {1} where {2}",
                                  wantedValue, table, whereClause);
            var cmd = PrepReader(query);
            using (IDataReader reader = cmd.ExecuteReader())
            {
                var RetVal = new List<string>();
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        RetVal.Add(reader[i].ToString());
                    }
                }
                //reader.Close();
                CloseReaderCommand(cmd);

                return RetVal;
            }
        }

        public override List<string> QueryFullData(string whereClause, string table, string wantedValue)
        {
            string query = "";
            query = String.Format("select {0} from {1} {2}",
                                  wantedValue, table, whereClause);
            var cmd = PrepReader(query);
            using (IDataReader reader = cmd.ExecuteReader())
            {
                var RetVal = new List<string>();
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        RetVal.Add(reader.GetValue(i).ToString());
                    }
                }
                //reader.Close();
                CloseReaderCommand(cmd);

                return RetVal;
            }
        }

        public override IDataReader QueryData(string whereClause, string table, string wantedValue)
        {
            string query = "";
            query = String.Format("select {0} from {1} {2}",
                                  wantedValue, table, whereClause);
            var cmd = PrepReader(query);
            return cmd.ExecuteReader();
        }

        public override List<string> Query(string keyRow, object keyValue, string table, string wantedValue,
                                           string Order)
        {
            Dictionary<string, object> ps = new Dictionary<string, object>();
            string query = "";
            if (keyRow == "")
            {
                query = String.Format("select {0} from {1}",
                                      wantedValue, table);
            }
            else
            {
                ps[":" + keyRow.Replace("`", "")] = keyValue;
                query = String.Format("select {0} from {1} where {2} = :{3}",
                                      wantedValue, table, keyRow, keyRow.Replace("`", ""));
            }
            var cmd = PrepReader(query);
            AddParams(ref cmd, ps);
            using (IDataReader reader = cmd.ExecuteReader())
            {
                var RetVal = new List<string>();
                try
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            Type r = reader[i].GetType();
                            RetVal.Add(r == typeof (DBNull) ? null : reader.GetString(i));
                        }
                    }
                }
                catch
                {
                }
                //reader.Close();
                CloseReaderCommand(cmd);

                return RetVal;
            }
        }

        public override List<string> Query(string[] keyRow, object[] keyValue, string table, string wantedValue)
        {
            Dictionary<string, object> ps = new Dictionary<string, object>();
            string query = String.Format("select {0} from {1} where ",
                                         wantedValue, table);
            int i = 0;
            foreach (object value in keyValue)
            {
                ps[":" + keyRow[i].Replace("`", "")] = value;
                query += String.Format("{0} = :{1} and ", keyRow[i], keyRow[i].Replace("`", ""));
                i++;
            }
            query = query.Remove(query.Length - 5);
            var cmd = PrepReader(query);
            AddParams(ref cmd, ps);
            using (IDataReader reader = cmd.ExecuteReader())
            {
                var RetVal = new List<string>();
                while (reader.Read())
                {
                    for (i = 0; i < reader.FieldCount; i++)
                    {
                        Type r = reader[i].GetType();
                        RetVal.Add(r == typeof (DBNull) ? null : reader[i].ToString());
                    }
                }
                //reader.Close();
                CloseReaderCommand(cmd);

                return RetVal;
            }
        }

        public override Dictionary<string, List<string>> QueryNames(string[] keyRow, object[] keyValue, string table,
                                                                    string wantedValue)
        {
            Dictionary<string, object> ps = new Dictionary<string, object>();
            string query = String.Format("select {0} from {1} where ",
                                         wantedValue, table);
            int i = 0;
            foreach (object value in keyValue)
            {
                ps[":" + keyRow[i].Replace("`", "")] = value;
                query += String.Format("{0} = :{1} and ", keyRow[i], keyRow[i].Replace("`", ""));
                i++;
            }
            query = query.Remove(query.Length - 5);
            var cmd = PrepReader(query);
            AddParams(ref cmd, ps);
            using (IDataReader reader = cmd.ExecuteReader())
            {
                var RetVal = new Dictionary<string, List<string>>();
                while (reader.Read())
                {
                    for (i = 0; i < reader.FieldCount; i++)
                    {
                        Type r = reader[i].GetType();
                        if (r == typeof (DBNull))
                            AddValueToList(ref RetVal, reader.GetName(i), null);
                        else
                            AddValueToList(ref RetVal, reader.GetName(i), reader[i].ToString());
                    }
                }
                //reader.Close();
                CloseReaderCommand(cmd);

                return RetVal;
            }
        }

        private void AddValueToList(ref Dictionary<string, List<string>> dic, string key, string value)
        {
            if (!dic.ContainsKey(key))
                dic.Add(key, new List<string>());

            dic[key].Add(value);
        }

        public override bool Insert(string table, object[] values)
        {
            var cmd = new SQLiteCommand();

            string query = "";
            query = String.Format("insert into {0} values(", table);
            string a = "a";
            foreach (object value in values)
            {
                object v = value;
                if (v is byte[])
                    v = Utils.BytesToString((byte[]) v);

                query += ":" + a + ",";
                cmd.Parameters.AddWithValue(a, v);
                a += "a"; //Nasty... but being lazy since SQLite doesn't like numbered params
            }
            query = query.Remove(query.Length - 1);
            query += ")";
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            return true;
        }

        public override bool Insert(string table, string[] keys, object[] values)
        {
            SQLiteCommand cmd = new SQLiteCommand();

            string query = "";
            query = String.Format("insert into {0} (", table);

            int i = 0;
            foreach (object key in keys)
            {
                cmd.Parameters.AddWithValue(":" + key.ToString().Replace("`", ""), values[i]);
                query += key + ",";
                i++;
            }

            query = query.Remove(query.Length - 1);
            query += ") values (";

            query = keys.Cast<object>().Aggregate(query, (current, key) => current + String.Format(":{0},", key.ToString().Replace("`", "")));
            query = query.Remove(query.Length - 1);
            query += ")";

            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            values = null;
            keys = null;
            return true;
        }

        public override bool DirectReplace(string table, string[] keys, object[] values)
        {
            return Replace(table, keys, values);
        }

        public override bool Replace(string table, string[] keys, object[] values)
        {
            var cmd = new SQLiteCommand();

            string query = "";
            query = String.Format("replace into {0} (", table);

            int i = 0;
            foreach (string key in keys)
            {
                string k = key;
                if (k.StartsWith("`"))
                {
                    k = k.Remove(0, 1);
                    k = k.Remove(k.Length - 1, 1);
                }
                cmd.Parameters.AddWithValue(":" + k, values[i]);
                query += key + ",";
                i++;
            }

            query = query.Remove(query.Length - 1);
            query += ") values (";

            foreach (string key in keys)
            {
                string k = key;
                if (k.StartsWith("`"))
                {
                    k = k.Remove(0, 1);
                    k = k.Remove(k.Length - 1, 1);
                }
                query += String.Format(":{0},", k);
            }

            query = query.Remove(query.Length - 1);
            query += ")";

            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            return true;
        }

        public override bool Delete(string table, string[] keys, object[] values)
        {
            var cmd = new SQLiteCommand();

            Dictionary<string, object> ps = new Dictionary<string, object>();
            string query = String.Format("delete from {0} " + (keys.Length > 0 ? "where " : ""), table);
            int i = 0;
            foreach (object value in values)
            {
                ps[":" + keys[i].Replace("`", "")] = value;
                query += keys[i] + " = :" + keys[i].Replace("`", "") + " and ";
                i++;
            }
            if (keys.Length > 0)
                query = query.Remove(query.Length - 4);
            cmd.CommandText = query;
            AddParams(ref cmd, ps);
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            return true;
        }

        public override bool Delete(string table, string whereclause)
        {
            var cmd = new SQLiteCommand();

            string query = String.Format("delete from {0} " + "where " + whereclause, table);
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            return true;
        }

        public override bool DeleteByTime(string table, string key)
        {
            var cmd = new SQLiteCommand();

            string query = String.Format("delete from {0} " + "where '" + key + "' < datetime('now', 'localtime')",
                                         table);
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            return true;
        }

        public override string FormatDateTimeString(int time)
        {
            if (time == 0)
                return "datetime('now', 'localtime')";
            return "datetime('now', 'localtime', '+" + time.ToString() + " minutes')";
        }

        public override string IsNull(string Field, string defaultValue)
        {
            return "IFNULL(" + Field + "," + defaultValue + ")";
        }

        public override string ConCat(string[] toConcat)
        {
            string returnValue = toConcat.Aggregate("", (current, s) => current + (s + " || "));
            return returnValue.Substring(0, returnValue.Length - 4);
        }

        public override bool Insert(string table, object[] values, string updateKey, object updateValue)
        {
            var cmd = new SQLiteCommand();
            Dictionary<string, object> ps = new Dictionary<string, object>();

            string query = "";
            query = String.Format("insert into {0} values (", table);
            string a = "a";
            foreach (object value in values)
            {
                ps[":" + a] = value;
                query = String.Format(query + ":{0},", a);
                a += "a";
            }
            query = query.Remove(query.Length - 1);
            query += ")";
            cmd.CommandText = query;
            AddParams(ref cmd, ps);
            try
            {
                ExecuteNonQuery(cmd);
                CloseReaderCommand(cmd);
            }
                //Execute the update then...
            catch (Exception)
            {
                cmd = new SQLiteCommand();
                query = String.Format("UPDATE {0} SET {1} = '{2}'", table, updateKey, updateValue);
                cmd.CommandText = query;
                ExecuteNonQuery(cmd);
                CloseReaderCommand(cmd);
            }
            return true;
        }

        public override bool DirectUpdate(string table, object[] setValues, string[] setRows, string[] keyRows,
                                          object[] keyValues)
        {
            return Update(table, setValues, setRows, keyRows, keyValues);
        }

        public override bool Update(string table, object[] setValues, string[] setRows, string[] keyRows,
                                    object[] keyValues)
        {
            var cmd = new SQLiteCommand();
            string query = String.Format("update {0} set ", table);
            int i = 0;

            foreach (object value in setValues)
            {
                query += string.Format("{0} = :{1},", setRows[i], setRows[i]);

                cmd.Parameters.AddWithValue(":" + setRows[i], value);
                i++;
            }
            i = 0;
            query = query.Remove(query.Length - 1);
            query += " where ";
            foreach (object value in keyValues)
            {
                query += String.Format("{0} = '{1}' and ", keyRows[i], value);
                i++;
            }
            query = query.Remove(query.Length - 5);
            cmd.CommandText = query;
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
            return true;
        }

        public override void CloseDatabase()
        {
            m_Connection.Close();
        }

        public override bool TableExists(string tableName)
        {
            var cmd = PrepReader("SELECT name FROM SQLite_master WHERE name='" + tableName + "'");
            using (IDataReader rdr = cmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    CloseReaderCommand(cmd);
                    return true;
                }
                else
                {
                    CloseReaderCommand(cmd);
                    return false;
                }
            }
        }

        public override void CreateTable(string table, ColumnDefinition[] columns)
        {
            if (TableExists(table))
            {
                throw new DataManagerException("Trying to create a table with name of one that already exists.");
            }

            string columnDefinition = string.Empty;
            var primaryColumns = (from cd in columns where cd.IsPrimary select cd);
            bool multiplePrimary = primaryColumns.Count() > 1;

            foreach (ColumnDefinition column in columns)
            {
                if (columnDefinition != string.Empty)
                {
                    columnDefinition += ", ";
                }
                columnDefinition += column.Name + " " + GetColumnTypeStringSymbol(column.Type) +
                                    ((column.IsPrimary && !multiplePrimary) ? " PRIMARY KEY" : string.Empty);
            }

            string multiplePrimaryString = string.Empty;
            if (multiplePrimary)
            {
                string listOfPrimaryNamesString = string.Empty;
                foreach (ColumnDefinition column in primaryColumns)
                {
                    if (listOfPrimaryNamesString != string.Empty)
                    {
                        listOfPrimaryNamesString += ", ";
                    }
                    listOfPrimaryNamesString += column.Name;
                }
                multiplePrimaryString = string.Format(", PRIMARY KEY ({0}) ", listOfPrimaryNamesString);
            }

            string query = string.Format("create table " + table + " ( {0} {1}) ", columnDefinition,
                                         multiplePrimaryString);

            var cmd = new SQLiteCommand {CommandText = query};
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        public override void UpdateTable(string table, ColumnDefinition[] columns,
                                         Dictionary<string, string> renameColumns)
        {
            if (!TableExists(table))
            {
                throw new DataManagerException("Trying to update a table with name of one that does not exist.");
            }

            List<ColumnDefinition> oldColumns = ExtractColumnsFromTable(table);

            Dictionary<string, ColumnDefinition> sameColumns = new Dictionary<string, ColumnDefinition>();
            foreach (ColumnDefinition column in oldColumns)
            {
                if (columns.Any(innercolumn => innercolumn.Name.ToLower() == column.Name.ToLower() ||
                                               renameColumns.ContainsKey(column.Name) &&
                                               renameColumns[column.Name].ToLower() == innercolumn.Name.ToLower()))
                {
                    sameColumns.Add(column.Name, column);
                }
            }

            string renamedTempTableColumnDefinition = string.Empty;
            string renamedTempTableColumn = string.Empty;

            foreach (ColumnDefinition column in oldColumns)
            {
                if (renamedTempTableColumnDefinition != string.Empty)
                {
                    renamedTempTableColumnDefinition += ", ";
                    renamedTempTableColumn += ", ";
                }
                renamedTempTableColumn += column.Name;
                renamedTempTableColumnDefinition += column.Name + " " + GetColumnTypeStringSymbol(column.Type);
            }
            string query = "CREATE TABLE " + table + "__temp(" + renamedTempTableColumnDefinition + ");";

            var cmd = new SQLiteCommand {CommandText = query};
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);

            query = "INSERT INTO " + table + "__temp SELECT " + renamedTempTableColumn + " from " + table + ";";
            cmd = new SQLiteCommand {CommandText = query};
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);

            query = "drop table " + table;
            cmd = new SQLiteCommand {CommandText = query};
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);

            string newTableColumnDefinition = string.Empty;
            List<ColumnDefinition> primaryColumns = columns.Where(column => column.IsPrimary).ToList();
            bool multiplePrimary = primaryColumns.Count > 1;

            foreach (ColumnDefinition column in columns)
            {
                if (newTableColumnDefinition != string.Empty)
                {
                    newTableColumnDefinition += ", ";
                }
                newTableColumnDefinition += column.Name + " " + GetColumnTypeStringSymbol(column.Type) +
                                            ((column.IsPrimary && !multiplePrimary) ? " PRIMARY KEY" : string.Empty);
            }
            string multiplePrimaryString = string.Empty;
            if (multiplePrimary)
            {
                string listOfPrimaryNamesString = string.Empty;
                foreach (ColumnDefinition column in primaryColumns)
                {
                    if (listOfPrimaryNamesString != string.Empty)
                    {
                        listOfPrimaryNamesString += ", ";
                    }
                    listOfPrimaryNamesString += column.Name;
                }
                multiplePrimaryString = string.Format(", PRIMARY KEY ({0}) ", listOfPrimaryNamesString);
            }

            query = string.Format("create table " + table + " ( {0} {1}) ", newTableColumnDefinition,
                                  multiplePrimaryString);
            cmd = new SQLiteCommand {CommandText = query};
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);

            string InsertFromTempTableColumnDefinition = string.Empty;
            string InsertIntoFromTempTableColumnDefinition = string.Empty;

            foreach (ColumnDefinition column in sameColumns.Values)
            {
                if (InsertFromTempTableColumnDefinition != string.Empty)
                {
                    InsertFromTempTableColumnDefinition += ", ";
                }
                if (InsertIntoFromTempTableColumnDefinition != string.Empty)
                {
                    InsertIntoFromTempTableColumnDefinition += ", ";
                }
                if (renameColumns.ContainsKey(column.Name))
                    InsertIntoFromTempTableColumnDefinition += renameColumns[column.Name];
                else
                    InsertIntoFromTempTableColumnDefinition += column.Name;
                InsertFromTempTableColumnDefinition += column.Name;
            }
            query = "INSERT INTO " + table + " (" + InsertIntoFromTempTableColumnDefinition + ") SELECT " +
                    InsertFromTempTableColumnDefinition + " from " + table + "__temp;";
            cmd = new SQLiteCommand {CommandText = query};
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);


            query = "drop table " + table + "__temp";
            cmd = new SQLiteCommand {CommandText = query};
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        public override string GetColumnTypeStringSymbol(ColumnTypes type)
        {
            switch (type)
            {
                case ColumnTypes.Double:
                    return "DOUBLE";
                case ColumnTypes.Integer11:
                    return "INT(11)";
                case ColumnTypes.Integer30:
                    return "INT(30)";
                case ColumnTypes.Char36:
                    return "CHAR(36)";
                case ColumnTypes.Char32:
                    return "CHAR(32)";
                case ColumnTypes.String:
                    return "TEXT";
                case ColumnTypes.String1:
                    return "VARCHAR(1)";
                case ColumnTypes.String2:
                    return "VARCHAR(2)";
                case ColumnTypes.String16:
                    return "VARCHAR(16)";
                case ColumnTypes.String32:
                    return "VARCHAR(32)";
                case ColumnTypes.String36:
                    return "VARCHAR(36)";
                case ColumnTypes.String45:
                    return "VARCHAR(45)";
                case ColumnTypes.String50:
                    return "VARCHAR(50)";
                case ColumnTypes.String64:
                    return "VARCHAR(64)";
                case ColumnTypes.String128:
                    return "VARCHAR(128)";
                case ColumnTypes.String100:
                    return "VARCHAR(100)";
                case ColumnTypes.String255:
                    return "VARCHAR(255)";
                case ColumnTypes.String512:
                    return "VARCHAR(512)";
                case ColumnTypes.String1024:
                    return "VARCHAR(1024)";
                case ColumnTypes.String8196:
                    return "VARCHAR(8196)";
                case ColumnTypes.Blob:
                    return "blob";
                case ColumnTypes.LongBlob:
                    return "blob";
                case ColumnTypes.Text:
                    return "VARCHAR(512)";
                case ColumnTypes.MediumText:
                    return "VARCHAR(512)";
                case ColumnTypes.LongText:
                    return "VARCHAR(512)";
                case ColumnTypes.Date:
                    return "DATE";
                case ColumnTypes.DateTime:
                    return "DATETIME";
                case ColumnTypes.Unknown:
                    return "";
                case ColumnTypes.TinyInt1:
                    return "TINYINT(1)";
                case ColumnTypes.TinyInt4:
                    return "TINYINT(4)";
                default:
                    throw new DataManagerException("Unknown column type.");
            }
        }

        protected override List<ColumnDefinition> ExtractColumnsFromTable(string tableName)
        {
            var defs = new List<ColumnDefinition>();

            var cmd = PrepReader(string.Format("PRAGMA table_info({0})", tableName));
            using (IDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    var name = rdr["name"];
                    var pk = rdr["pk"];
                    var type = rdr["type"];
                    defs.Add(new ColumnDefinition
                                 {
                                     Name = name.ToString(),
                                     IsPrimary = (int.Parse(pk.ToString()) > 0),
                                     Type = ConvertTypeToColumnType(type.ToString())
                                 });
                }
                rdr.Close();
            }
            CloseReaderCommand(cmd);

            return defs;
        }

        private ColumnTypes ConvertTypeToColumnType(string typeString)
        {
            string tStr = typeString.ToLower();
            //we'll base our names on lowercase
            switch (tStr)
            {
                case "double":
                    return ColumnTypes.Double;
                case "integer":
                    return ColumnTypes.Integer11;
                case "int(11)":
                    return ColumnTypes.Integer11;
                case "int(30)":
                    return ColumnTypes.Integer30;
                case "char(36)":
                    return ColumnTypes.Char36;
                case "char(32)":
                    return ColumnTypes.Char32;
                case "varchar(1)":
                    return ColumnTypes.String1;
                case "varchar(2)":
                    return ColumnTypes.String2;
                case "varchar(16)":
                    return ColumnTypes.String16;
                case "varchar(32)":
                    return ColumnTypes.String32;
                case "varchar(36)":
                    return ColumnTypes.String36;
                case "varchar(45)":
                    return ColumnTypes.String45;
                case "varchar(50)":
                    return ColumnTypes.String50;
                case "varchar(64)":
                    return ColumnTypes.String64;
                case "varchar(128)":
                    return ColumnTypes.String128;
                case "varchar(100)":
                    return ColumnTypes.String100;
                case "varchar(512)":
                    return ColumnTypes.String512;
                case "varchar(255)":
                    return ColumnTypes.String255;
                case "varchar(1024)":
                    return ColumnTypes.String1024;
                case "date":
                    return ColumnTypes.Date;
                case "datetime":
                    return ColumnTypes.DateTime;
                case "text":
                    return ColumnTypes.String512;
                case "varchar(8196)":
                    return ColumnTypes.String8196;
                case "blob":
                    return ColumnTypes.Blob;
                case "float":
                    return ColumnTypes.Unknown;
                case "tinyint(1)":
                    return ColumnTypes.TinyInt1;
                case "tinyint(4)":
                    return ColumnTypes.TinyInt4;
                case "int unsigned":
                    return ColumnTypes.Unknown;
                case "":
                    return ColumnTypes.Unknown;
                default:
                    throw new Exception("You've discovered some type in SQLite that's not reconized by Aurora (" +
                                        typeString +
                                        "), please place the correct conversion in ConvertTypeToColumnType.");
            }
        }

        public override void DropTable(string tableName)
        {
            var cmd = new SQLiteCommand {CommandText = string.Format("drop table {0}", tableName)};
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        public override void ForceRenameTable(string oldTableName, string newTableName)
        {
            var cmd = new SQLiteCommand
                          {
                              CommandText =
                                  string.Format("ALTER TABLE {0} RENAME TO {1}", oldTableName,
                                                newTableName + "_renametemp")
                          };
            ExecuteNonQuery(cmd);
            cmd.CommandText = string.Format("ALTER TABLE {0} RENAME TO {1}", newTableName + "_renametemp", newTableName);
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        protected override void CopyAllDataBetweenMatchingTables(string sourceTableName, string destinationTableName,
                                                                 ColumnDefinition[] columnDefinitions)
        {
            var cmd = new SQLiteCommand
                          {
                              CommandText =
                                  string.Format("insert into {0} select * from {1}", destinationTableName,
                                                sourceTableName)
                          };
            ExecuteNonQuery(cmd);
            CloseReaderCommand(cmd);
        }

        public override IGenericData Copy()
        {
            return new SQLiteLoader();
        }
    }
}