﻿using System;
using System.Data;
using System.Linq;
using Assets.Scripts.DataPersistence.MissionStatusDetail;
using Assets.Scripts.Helper;
using Mono.Data.Sqlite;

namespace Assets.Scripts.DataPersistence.SQLComponent
{
    public class SQLService: ISQLService
    {
        private string[] _bannedWords = { "create", "update", "delete", "insert", "drop", "alter", "truncate", "grant", "revoke", "commit", "rollback", "savepoint" };
        /// <summary>
        /// Get result from executing SQL.
        /// First column must be Images' name if puzzle type is Float image.
        /// </summary>
        /// <param name="dbConn">Must be full path for connecting to Database example "URI=file:folder/database.db"</param>
        /// <param name="sql">SQL command</param>
        /// <param name="visualType">Type of puzzle that want to execute.</param>
        /// <exception cref="SqliteException">If sql have banned word, it will throw exception</exception>
        /// <exception cref="ArgumentException">If sql command is null</exception>
        /// <returns>Result after execute SQL and first row is attribute. If puzzle type is "A" then first column must be image column</returns>
        public string[][] GetTableResult(string dbConn, string sql, VisualType visualType)
        {
            switch (visualType)
            {
                case VisualType.Dynamic:
                    // 1) Validate sql before insert 'Image' column.
                    _ValidateQuery(dbConn, sql);
                    // 2) Insert 'Image' column to sql command.
                    string sqlWithImg = _InsertImgColumn(sql);
                    try
                    {
                        // 3) If after insert 'Image' column have an exception, then return with original sql result.
                        _ValidateQuery(dbConn, sqlWithImg);
                        return _GetQueryResult(dbConn, sqlWithImg);
                    }
                    catch (Exception)
                    {
                        return _GetQueryResult(dbConn, sql);
                    }
                case VisualType.Static:
                    return _GetQueryResult(dbConn, sql);
                default:
                    return null;
            }
        }

        #region For validate method
        private bool _HaveBannedWord(string sql)
        {
            string[] sqlWords = sql.ToLower().Split(' ', ';');

            foreach (string word in sqlWords)
            {
                if (_bannedWords.Contains(word))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Will throw exception if query is invalid.
        /// </summary>
        /// <param name="dbConn"></param>
        /// <param name="sql"></param>
        /// <exception cref="SqliteException">If sql have banned word, it will throw exception</exception>
        /// <exception cref="ArgumentException">If sql command is null</exception>
        private void _ValidateQuery(string dbConn, string sql)
        {
            if (_HaveBannedWord(sql))
            {
                throw new SqliteException(_GetWarningWord_BannedWord());
            }
            else
            {
                using (SqliteConnection connection = new SqliteConnection(dbConn))
                {
                    connection.Open();
                    // Query to database
                    using (SqliteCommand command = new SqliteCommand(sql, connection))
                    {
                        // Read data from query
                        using (IDataReader reader = command.ExecuteReader())
                        {
                        
                        }
                    }
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Warning word for query that use banned word.
        /// </summary>
        /// <returns>Warning message for query that use banned word.</returns>
        private string _GetWarningWord_BannedWord()
        {
            string warningWord = "You don't have permission to use this command:";

            for (int i = 0; i < _bannedWords.Length; i++)
            {
                warningWord += " \"" + _bannedWords[i] + "\"";
                if (i < _bannedWords.Length - 1)
                {
                    warningWord += ",";
                }
            }

            return warningWord;
        }
        #endregion

        /// <summary>
        /// Get result from executing SQL.
        /// </summary>
        /// <param name="dbConn">Must be full path for connecting to Database example "URI=file:folder/database.db"</param>
        /// <param name="sql">SQL command</param>
        /// <returns>Result from execute sql command and first row is attributes.</returns>
        /// <exception cref="ArgumentException">If sql command is null</exception>
        private string[][] _GetQueryResult(string dbConn, string sql)
        {
            string[][] queryResult;
            int numOfRecord = 0;

            // Connect to database
            using (SqliteConnection connection = new SqliteConnection(dbConn))
            {
                connection.Open();
                // Query to database
                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    // Count number of record
                    using (IDataReader forCountReader = command.ExecuteReader())
                    {
                        while (forCountReader.Read())
                        {
                            numOfRecord += 1;
                        }
                    }
                    // Read data from query
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        queryResult = new string[reader.FieldCount][];

                        // set attribute in result
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            queryResult[i] = new string[numOfRecord + 1];
                            queryResult[i][0] = reader.GetName(i);
                        }
                        // fill value for each header from each row in table
                        int record_index = 1;
                        while (reader.Read())
                        {
                            for (int j = 0; j < reader.FieldCount; j++)
                            {
                                queryResult[j][record_index] = reader.GetValue(j).ToString();
                            }
                            record_index++;
                        }
                    }
                }
                connection.Close();
            }

            return queryResult;
        }

        /// <summary>
        /// Insert image column to query command.
        /// </summary>
        /// <param name="sql"></param>
        /// <returns>Query command that have image column.</returns>
        private string _InsertImgColumn(string sql)
        {
            const string selectWord = "select ";

            int selectIndex = sql.ToLower().IndexOf(selectWord);
            //If query is not select all (SELECT *) then insert image column.
            if (selectIndex >= 0 && sql.ElementAt(selectIndex + selectWord.Length) != '*')
            {
                int imgIndex = selectIndex + selectWord.Length;

                return sql.Insert(imgIndex, $"{EnvironmentData.Instance.ImageColumn},");
            }
            else
            {
                return sql;
            }
        }

        public Schema[] GetSchemas(string dbConn, string[] tables, bool keepImgCol)
        {
            Schema[] schemas = new Schema[tables.Length];

            // Connect to database
            using (SqliteConnection connection = new SqliteConnection(dbConn))
            {
                connection.Open();

                for (int i = 0; i < tables.Length; i++)
                {
                    string table = tables[i];
                    string sql = $"SELECT * FROM {table} LIMIT 1";
                    // Query to database
                    using (SqliteCommand command = new SqliteCommand(sql, connection))
                    {
                        // Read data from query
                        using (IDataReader reader = command.ExecuteReader())
                        {
                            string[] attributes = new string[reader.FieldCount];

                            // get all attribute
                            for (int j = 0; j < reader.FieldCount; j++)
                            {
                                attributes[j] = reader.GetName(j);
                            }

                            // Check if want to keep image column.
                            if (!keepImgCol && attributes[0].Equals(EnvironmentData.Instance.ImageColumn))
                            {
                                attributes = attributes.Skip(1).ToArray();
                            }
                            schemas[i] = new Schema(table, attributes);
                        }
                    }
                }
                connection.Close();
            }
            return schemas;
        }

        public string[] GetAllTable(string dbConn)
        {
            string[] tables;

            // Connect to database
            using (SqliteConnection connection = new SqliteConnection(dbConn))
            {
                connection.Open();

                string sql = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY 1";
                // Query to database
                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    // Read data from query
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        tables = new string[reader.FieldCount];

                        // get all table
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            tables[i] = reader.GetName(i);
                        }
                    }
                }
                
                connection.Close();
            }

            return tables;
        }
    }
}
