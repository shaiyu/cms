using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using SiteServer.CMS.Data;
using SiteServer.CMS.Model;
using SiteServer.Plugin;
using SiteServer.Utils;

namespace SiteServer.CMS.Provider
{
    public class ErrorLogDao : DataProviderBase
    {
        public override string TableName => "siteserver_ErrorLog";

        public override List<TableColumnInfo> TableColumns => new List<TableColumnInfo>
        {
            new TableColumnInfo
            {
                ColumnName = nameof(ErrorLogInfo.Id),
                DataType = DataType.Integer,
                IsIdentity = true,
                IsPrimaryKey = true
            },
            new TableColumnInfo
            {
                ColumnName = nameof(ErrorLogInfo.PluginId),
                DataType = DataType.VarChar,
                Length = 200
            },
            new TableColumnInfo
            {
                ColumnName = nameof(ErrorLogInfo.Message),
                DataType = DataType.VarChar,
                Length = 255
            },
            new TableColumnInfo
            {
                ColumnName = nameof(ErrorLogInfo.Stacktrace),
                DataType = DataType.Text
            },
            new TableColumnInfo
            {
                ColumnName = nameof(ErrorLogInfo.Summary),
                DataType = DataType.Text
            },
            new TableColumnInfo
            {
                ColumnName = nameof(ErrorLogInfo.AddDate),
                DataType = DataType.DateTime
            }
        };

        private const string ParmPluginId = "@PluginId";
        private const string ParmMessage = "@Message";
        private const string ParmStacktrace = "@Stacktrace";
        private const string ParmSummary = "@Summary";
        private const string ParmAddDate = "@AddDate";

        public int Insert(ErrorLogInfo logInfo)
        {
            var sqlString = $"INSERT INTO {TableName} (PluginId, Message, Stacktrace, Summary, AddDate) VALUES (@PluginId, @Message, @Stacktrace, @Summary, @AddDate)";

            var parms = new IDataParameter[]
            {
                GetParameter(ParmPluginId, DataType.VarChar, 200, logInfo.PluginId),
                GetParameter(ParmMessage, DataType.VarChar, 255, logInfo.Message),
                GetParameter(ParmStacktrace, DataType.Text, logInfo.Stacktrace),
                GetParameter(ParmSummary, DataType.Text, logInfo.Summary),
                GetParameter(ParmAddDate, DataType.DateTime, logInfo.AddDate),
            };

            return ExecuteNonQueryAndReturnId(TableName, nameof(ErrorLogInfo.Id), sqlString, parms);
        }

        public void Delete(List<int> idList)
        {
            if (idList == null || idList.Count <= 0) return;

            var sqlString =
                $"DELETE FROM {TableName} WHERE Id IN ({TranslateUtils.ToSqlInStringWithoutQuote(idList)})";

            ExecuteNonQuery(sqlString);
        }

        public void Delete(int days)
        {
            if (days <= 0) return;
            ExecuteNonQuery($@"DELETE FROM {TableName} WHERE AddDate < '{DateUtils.GetDateAndTimeString(DateTime.Now.AddDays(-days))}'");
        }

        public void DeleteAll()
        {
            var sqlString = $"DELETE FROM {TableName}";

            ExecuteNonQuery(sqlString);
        }

        public int GetCount()
        {
            var count = 0;
            var sqlString = $"SELECT Count(*) FROM {TableName}";

            using (var rdr = ExecuteReader(sqlString))
            {
                if (rdr.Read() && !rdr.IsDBNull(0))
                {
                    count = GetInt(rdr, 0);
                }
                rdr.Close();
            }

            return count;
        }

        public KeyValuePair<string, string> GetMessageAndStacktrace(int logId)
        {
            var pair = new KeyValuePair<string, string>();

            var sqlString = $"SELECT Message, Stacktrace FROM {TableName} WHERE Id = {logId}";

            using (var rdr = ExecuteReader(sqlString))
            {
                if (rdr.Read())
                {
                    pair = new KeyValuePair<string, string>(GetString(rdr, 0), GetString(rdr, 1));
                }
                rdr.Close();
            }

            return pair;
        }

        public string GetSelectCommend(string pluginId, string keyword, string dateFrom, string dateTo)
        {
            var whereString = new StringBuilder();

            if (!string.IsNullOrEmpty(pluginId))
            {
                whereString.Append($"PluginId = '{PageUtils.FilterSql(pluginId)}'");
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                if (whereString.Length > 0)
                {
                    whereString.Append(" AND ");
                }
                var filterKeyword = PageUtils.FilterSql(keyword);
                var keywordId = TranslateUtils.ToInt(keyword);
                whereString.Append(keywordId > 0
                    ? $"Id = {keywordId}"
                    : $"(Message LIKE '%{filterKeyword}%' OR Stacktrace LIKE '%{filterKeyword}%' OR Summary LIKE '%{filterKeyword}%')");
            }
            if (!string.IsNullOrEmpty(dateFrom))
            {
                if (whereString.Length > 0)
                {
                    whereString.Append(" AND ");
                }
                whereString.Append($"AddDate >= {SqlUtils.GetComparableDate(TranslateUtils.ToDateTime(dateFrom))}");
            }
            if (!string.IsNullOrEmpty(dateTo))
            {
                if (whereString.Length > 0)
                {
                    whereString.Append(" AND ");
                }
                whereString.Append($"AddDate <= {SqlUtils.GetComparableDate(TranslateUtils.ToDateTime(dateTo))}");
            }

            return whereString.Length > 0
                ? $"SELECT Id, PluginId, Message, Stacktrace, Summary, AddDate FROM {TableName} WHERE {whereString}"
                : $"SELECT Id, PluginId, Message, Stacktrace, Summary, AddDate FROM {TableName}";
        }
    }
}
