using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Taskora.БазаДанных
{
    public static class DatabaseHelper
    {
        private static readonly string DbPath = Path.Combine(AppContext.BaseDirectory, "taskora.db");

        public static string ConnectionString => $"Data Source={DbPath}";

        public static SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys=ON;";
            pragma.ExecuteNonQuery();

            return connection;
        }

        public static void InitializeDatabase()
        {
            using var connection = OpenConnection();

            string createUsersTable = @"
                CREATE TABLE IF NOT EXISTS пользователи (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    имя TEXT NOT NULL,
                    логин TEXT UNIQUE NOT NULL,
                    пароль_хеш TEXT NOT NULL,
                    роль TEXT NOT NULL DEFAULT 'пользователь'
                );";

            string createTasksTable = @"
                CREATE TABLE IF NOT EXISTS задачи (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    пользователь_id INTEGER NOT NULL,
                    название TEXT NOT NULL,
                    описание TEXT,
                    категория TEXT NOT NULL DEFAULT 'Без категории',
                    приоритет TEXT NOT NULL DEFAULT 'Средний',
                    срок TEXT,
                    статус TEXT NOT NULL DEFAULT 'Новая',
                    дата_создания DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (пользователь_id) REFERENCES пользователи(id)
                );";

            ExecuteNonQuery(connection, createUsersTable);
            ExecuteNonQuery(connection, createTasksTable);

            long version = ExecuteScalarLong(connection, "PRAGMA user_version;");

            if (version == 0)
            {
                CreateUserIfNotExists(connection, "Администратор", "admin", "admin123", "администратор");
                CreateUserIfNotExists(connection, "Иван Иванов", "user1", "user1234", "пользователь");

                SeedDemoTasks(connection);

                ExecuteNonQuery(connection, "PRAGMA user_version = 1;");
            }
        }

        private static void CreateUserIfNotExists(
            SqliteConnection connection,
            string name,
            string login,
            string password,
            string role)
        {
            using var check = connection.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM пользователи WHERE логин = @login;";
            check.Parameters.AddWithValue("@login", login);

            long count = Convert.ToInt64(check.ExecuteScalar());

            if (count > 0)
                return;

            using var insert = connection.CreateCommand();
            insert.CommandText = @"
                INSERT INTO пользователи (имя, логин, пароль_хеш, роль)
                VALUES (@name, @login, @hash, @role);";

            insert.Parameters.AddWithValue("@name", name);
            insert.Parameters.AddWithValue("@login", login);
            insert.Parameters.AddWithValue("@hash", HashPassword(password));
            insert.Parameters.AddWithValue("@role", role);

            insert.ExecuteNonQuery();
        }

        private static void SeedDemoTasks(SqliteConnection connection)
        {
            long count = ExecuteScalarLong(connection, "SELECT COUNT(*) FROM задачи;");

            if (count > 0)
                return;

            int user1Id = GetUserId(connection, "user1");
            int adminId = GetUserId(connection, "admin");

            string today = DateTime.Today.ToString("yyyy-MM-dd");
            string tomorrow = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
            string afterTomorrow = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd");

            if (user1Id > 0)
            {
                InsertTask(
                    connection,
                    user1Id,
                    "Посмотреть материал по C#",
                    "Повторить тему работы с базами данных.",
                    "Учёба",
                    "Средний",
                    today,
                    "Новая");

                InsertTask(
                    connection,
                    user1Id,
                    "Подготовить презентацию проекта",
                    "Сделать слайды по системе Taskora.",
                    "Учёба",
                    "Высокий",
                    afterTomorrow,
                    "В работе");

                InsertTask(
                    connection,
                    user1Id,
                    "Купить продукты",
                    "Молоко, хлеб, сыр.",
                    "Дом",
                    "Низкий",
                    null,
                    "Выполнена");
            }

            if (adminId > 0)
            {
                InsertTask(
                    connection,
                    adminId,
                    "Проверить список пользователей",
                    "Просмотреть демо-пользователей в системе.",
                    "Администрирование",
                    "Средний",
                    tomorrow,
                    "Новая");
            }
        }

        private static void InsertTask(
            SqliteConnection connection,
            int userId,
            string title,
            string description,
            string category,
            string priority,
            string dueDate,
            string status)
        {
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO задачи (
                    пользователь_id,
                    название,
                    описание,
                    категория,
                    приоритет,
                    срок,
                    статус
                )
                VALUES (
                    @userId,
                    @title,
                    @description,
                    @category,
                    @priority,
                    @dueDate,
                    @status
                );";

            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@title", title);
            command.Parameters.AddWithValue("@description", string.IsNullOrWhiteSpace(description) ? DBNull.Value : description);
            command.Parameters.AddWithValue("@category", category);
            command.Parameters.AddWithValue("@priority", priority);
            command.Parameters.AddWithValue("@dueDate", string.IsNullOrWhiteSpace(dueDate) ? DBNull.Value : dueDate);
            command.Parameters.AddWithValue("@status", status);

            command.ExecuteNonQuery();
        }

        private static int GetUserId(SqliteConnection connection, string login)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id FROM пользователи WHERE логин = @login;";
            command.Parameters.AddWithValue("@login", login);

            object result = command.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToInt32(result);
        }

        public static string HashPassword(string password)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password ?? ""));

            StringBuilder builder = new StringBuilder();

            foreach (byte b in bytes)
                builder.Append(b.ToString("x2"));

            return builder.ToString();
        }

        private static void ExecuteNonQuery(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private static long ExecuteScalarLong(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            object result = command.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToInt64(result);
        }
    }
}