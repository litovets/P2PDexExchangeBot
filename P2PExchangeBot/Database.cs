using System;
using System.Linq;
using System.IO;
using System.Data.SQLite;
using System.Collections.Generic;

namespace P2PExchangeBot
{
    enum RequestType
    {
        Buy,
        Sell
    }

    static class Database
    {
        const string DBFileName = "db.sqlite";

        const int MaxVotes = 5;

        const string RequestResultStringTemplate = @"<b>({0})</b>
{1} <i>хочет {2}</i> <b>{3} {4}</b> <i>с комиссией</i> <b>{5}%</b>.
<b>Банк</b> - {6}. 
<b>Начало:</b> {7}, <b>Окончание:</b> {8}";

        const string EscrowListTemplate = @"@{0} - <b>{1}</b>";

        static SQLiteConnection _dbConnection;

        static Database()
        {
            if (!File.Exists(DBFileName))
            {
                CreateDB();
            }
            else
            {
                OpenDB();
            }
        }

        static void CreateDB()
        {
            SQLiteConnection.CreateFile(DBFileName);

            OpenDB();

            CreateTables();
        }

        static void OpenDB()
        {
            _dbConnection = new SQLiteConnection(string.Format("Data Source={0};Version=3;", DBFileName));
            _dbConnection.Open();

            CreateTables();
        }

        static void CreateTables()
        {
            string sql =
                @"CREATE TABLE IF NOT EXISTS requests (id INTEGER PRIMARY KEY AUTOINCREMENT, username TEXT, requestType INTEGER, quantity INTEGER, currency TEXT, bankName TEXT, fee REAL, startDate TEXT, endDate TEXT);
                  CREATE TABLE IF NOT EXISTS notifications (username TEXT, chatId INTEGER);
                  CREATE TABLE IF NOT EXISTS masterchat (chatId INTEGER);
                  CREATE TABLE IF NOT EXISTS users (username TEXT);
                  CREATE TABLE IF NOT EXISTS users_votes (username TEXT, votedUser TEXT);";

            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            command.ExecuteNonQuery();
        }

        public static bool IsNotificationsRowExistForUser(string username)
        {
            string sql = "SELECT count(*) FROM notifications WHERE username=\"" + username + "\"";
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            int count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }

        public static void AddUserForNotifications(string username, long chatId)
        {
            string sql = string.Format("INSERT INTO notifications(username, chatId) VALUES(\"{0}\",\"{1}\")", username, chatId);
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            command.ExecuteNonQuery();
        }

        public static void DeleteUserFromNotifications(string username)
        {
            string sql = string.Format("DELETE FROM notifications WHERE username=\"{0}\"", username);
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            command.ExecuteNonQuery();
        }

        public static List<long> GetUserlistForNotifications(string excludeUser)
        {
            List<long> result = new List<long>();

            string sql = "SELECT chatId FROM notifications WHERE username != \"" + excludeUser + "\"";
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);

            try
            {
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    result.Add((long)reader["chatId"]);
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return result;
        }
        
        public static int AddRequest(string username, RequestType reqType, int quantity, string currency, string bankName, float fee, DateTime startDate, DateTime endDate)
        {
            string sql = string.Format("INSERT INTO requests(username, requestType, quantity, currency, bankName, fee, startDate, endDate) VALUES(\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\")",
                username, (int)reqType, quantity, currency, bankName, fee.ToString("F2"), startDate.ToShortDateString(), endDate.ToShortDateString());

            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            command.ExecuteNonQuery();

            sql = "SELECT last_insert_rowid() FROM requests";
            command = new SQLiteCommand(sql, _dbConnection);
            int id = Convert.ToInt32(command.ExecuteScalar());

            return id;
        }

        public static string GetRequest(int id)
        {
            string sql = "SELECT * FROM requests WHERE id=" + id;

            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);

            string result = string.Empty;

            try
            {
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var num = reader["id"].ToString();
                    string user = "@" + reader["username"].ToString();
                    string reqType = GetLocalizedString((RequestType)(int.Parse(reader["requestType"].ToString())));
                    var quantity = reader["quantity"].ToString();
                    string currency = reader["currency"].ToString();
                    string fee = reader["fee"].ToString();
                    string bankName = reader["bankName"].ToString();
                    string startDate = reader["startDate"].ToString();
                    string endDate = reader["endDate"].ToString();

                    result = string.Format(RequestResultStringTemplate, num, user, reqType, quantity, currency, fee, bankName, startDate, endDate);
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return result;
        }

        public static List<string> GetRequestsFor(string username)
        {
            string sql = "SELECT * FROM requests WHERE username = \"" + username + "\"";

            return GetResultsForSql(sql);
        }

        public static List<string> GetAllRequests()
        {
            string sql = "SELECT * FROM requests";

            return GetResultsForSql(sql);
        }

        public static void DeleteReqWithId(string username, int id)
        {
            string sql = string.Format("DELETE FROM requests WHERE id={0} AND username=\"{1}\"", id, username);
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            command.ExecuteNonQuery();
        }

        public static void UpdateRequest(int id, string username, int quantity, string currency, string bankName, float fee, DateTime startDate, DateTime endDate)
        {
            if (quantity < 0 && string.IsNullOrEmpty(currency) && string.IsNullOrEmpty(bankName) && fee < 0f && endDate == DateTime.MinValue)
                return;

            List<string> updateValues = new List<string>();
            updateValues.Add(quantity > 0 ? "quantity=" + quantity : "");
            updateValues.Add(!string.IsNullOrEmpty(currency) ? "currency=\"" + currency + "\"" : "");
            updateValues.Add(!string.IsNullOrEmpty(bankName) ? "bankName=\"" + bankName + "\"" : "");
            updateValues.Add(fee >= 0f ? "fee=" + fee.ToString("F2") : "");
            updateValues.Add(endDate != DateTime.MinValue ? "startDate=\"" + startDate.ToShortDateString() + "\"" : "");
            updateValues.Add(endDate != DateTime.MinValue ? "endDate=\"" + endDate.ToShortDateString() + "\"" : "");
            var withoutEmpty = updateValues.Where(str => !string.IsNullOrEmpty(str));
            string agregated = withoutEmpty.Aggregate((first, second) => first + "," + second);

            string sql = "UPDATE requests SET " + agregated + " WHERE id=" + id + " AND username=\"" + username + "\"";

            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            command.ExecuteNonQuery();
        }

        public static void DeleteOldRequests()
        {
            string sql = "SELECT id, endDate FROM requests";
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            Dictionary<string, string> result = new Dictionary<string, string>();

            try
            {
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var num = reader["id"].ToString();
                    string endDate = reader["endDate"].ToString();
                    
                    result.Add(num, endDate);
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var forDelete = result.Where(pair => (DateTime.Now > DateTime.Parse(pair.Value))).Select(pair => pair.Key);

            foreach (var id in forDelete)
            {
                string sqlDel = "DELETE FROM requests WHERE id=" + id;
                command = new SQLiteCommand(sqlDel, _dbConnection);
                command.ExecuteNonQuery();
            }
        }

        public static long GetMasterChatId()
        {
            string sql = "SELECT chatId FROM masterchat";
            long chatId = 0;

            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);

            try
            {
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var num = reader["chatId"].ToString();
                    chatId = long.Parse(num);
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception during GetMasterChatId. Message: " + ex.Message);
            }

            return chatId;
        }

        public static void SetMasterChatId(long chatId)
        {
            string sql = "INSERT INTO masterchat(chatId) VALUES(" + chatId + ")";
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            command.ExecuteNonQuery();
        }

        public static bool IsUserRegistered(string username)
        {
            string sql = "SELECT count(*) FROM users WHERE username=\"" + username + "\"";
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            int count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }

        public static void AddUser(string username)
        {
            string sql = "INSERT INTO users(username) VALUES(\"" + username + "\")";
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            command.ExecuteNonQuery();
        }

        public static void DeleteUser(string username)
        {
            string sql = "DELETE FROM users WHERE username=\"" + username + "\"";
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            command.ExecuteNonQuery();
        }

        public static int GetVotesCount(string username)
        {
            string sql = "SELECT count(*) FROM users_votes WHERE username = \"" + username + "\"";
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            int count = Convert.ToInt32(command.ExecuteScalar());

            return MaxVotes - count;
        }

        public static bool IsAlreadyVotedByUser(string username, string votedUser)
        {
            string sql = "SELECT count(*) FROM users_votes WHERE username = \"" + username + "\" AND votedUser = \"" + votedUser + "\"";
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            int count = Convert.ToInt32(command.ExecuteScalar());

            return count > 0;
        }

        public static bool Vote(string username, string votedUser)
        {
            if (!IsUserRegistered(username) || IsAlreadyVotedByUser(username, votedUser))
            {
                return false;
            }

            string sql = string.Format("INSERT INTO users_votes(username, votedUser) VALUES(\"{0}\",\"{1}\")", username, votedUser);
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            command.ExecuteNonQuery();

            return true;
        }

        public static void Unvote(string username, string votedUser)
        {
            string sql = string.Format("DELETE FROM users_votes WHERE username = \"{0}\" AND votedUser = \"{1}\"", username, votedUser);
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            command.ExecuteNonQuery();
        }

        public static List<string> GetMyVotedUsers(string username)
        {
            List<string> result = new List<string>();

            string sql = "SELECT votedUser FROM users_votes WHERE username = \"" + username + "\"";

            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);

            try
            {
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var user = reader["votedUser"].ToString();
                    result.Add(user);
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception during GetEscrowList. Message: " + ex.Message);
            }

            return result;
        }

        public static List<string> GetEscrowList()
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            string sql = "SELECT votedUser FROM users_votes";

            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);

            try
            {
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var username = reader["votedUser"].ToString();
                    if (result.ContainsKey(username))
                    {
                        ++result[username];
                    }
                    else
                    {
                        result.Add(username, 1);
                    }
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception during GetEscrowList. Message: " + ex.Message);
            }

            var sortedResult = result.OrderByDescending(pair => pair.Value);

            List<string> escrowList = new List<string>();
            foreach (var pair in sortedResult)
            {
                escrowList.Add(string.Format(EscrowListTemplate, pair.Key, pair.Value.ToString()));
            }

            return escrowList;
        }

        private static List<string> GetResultsForSql(string sql)
        {
            List<string> result = new List<string>();
            
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);

            try
            {
                SQLiteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var num = reader["id"].ToString();
                    string user = "@" + reader["username"].ToString();
                    string reqType = GetLocalizedString((RequestType)(int.Parse(reader["requestType"].ToString())));
                    var quantity = reader["quantity"].ToString();
                    string currency = reader["currency"].ToString();
                    string fee = reader["fee"].ToString();
                    string bankName = reader["bankName"].ToString();
                    string startDate = reader["startDate"].ToString();
                    string endDate = reader["endDate"].ToString();

                    string line = string.Format(RequestResultStringTemplate, num, user, reqType, quantity, currency, fee, bankName, startDate, endDate);

                    result.Add(line);
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return result;
        }

        private static string GetLocalizedString(RequestType type)
        {
            switch (type)
            {
                case RequestType.Buy:
                    return "купить";
                case RequestType.Sell:
                    return "продать";
                default:
                    return "___";
            }
        }
    }
}
