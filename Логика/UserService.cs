using System;
using System.Data;
using Microsoft.Data.Sqlite;
using Taskora.БазаДанных;
using Taskora.Модели;

namespace Taskora.Логика
{
    public static class UserService
    {
        public const string RoleAdmin = "администратор";
        public const string RoleUser = "пользователь";

        public static User Authenticate(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                return null;

            using var connection = DatabaseHelper.OpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT id, имя, роль
                FROM пользователи
                WHERE логин = @login AND пароль_хеш = @hash;";

            command.Parameters.AddWithValue("@login", login.Trim());
            command.Parameters.AddWithValue("@hash", DatabaseHelper.HashPassword(password));

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new User
            {
                Id = Convert.ToInt32(reader["id"]),
                Имя = reader["имя"].ToString(),
                Роль = reader["роль"].ToString()
            };
        }

        public static void Register(string name, string login, string password)
        {
            Validator.Required(name, "Имя");
            Validator.ValidateLogin(login);
            Validator.ValidatePassword(password);

            login = login.Trim();

            if (LoginExists(login))
                throw new InvalidOperationException("Пользователь с таким логином уже существует.");

            using var connection = DatabaseHelper.OpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO пользователи (имя, логин, пароль_хеш, роль)
                VALUES (@name, @login, @hash, @role);";

            command.Parameters.AddWithValue("@name", name.Trim());
            command.Parameters.AddWithValue("@login", login);
            command.Parameters.AddWithValue("@hash", DatabaseHelper.HashPassword(password));
            command.Parameters.AddWithValue("@role", RoleUser);

            command.ExecuteNonQuery();
        }

        public static DataTable GetAll()
        {
            using var connection = DatabaseHelper.OpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT id, имя, логин, роль
                FROM пользователи
                ORDER BY роль, имя;";

            DataTable table = new DataTable();

            using (var reader = command.ExecuteReader())
            {
                table.Load(reader);
            }

            return table;
        }

        public static User GetById(int id)
        {
            using var connection = DatabaseHelper.OpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT id, имя, логин, роль
                FROM пользователи
                WHERE id = @id;";

            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new User
            {
                Id = Convert.ToInt32(reader["id"]),
                Имя = reader["имя"].ToString(),
                Логин = reader["логин"].ToString(),
                Роль = reader["роль"].ToString()
            };
        }

        public static void Create(string name, string login, string password, string role)
        {
            Validator.Required(name, "Имя");
            Validator.ValidateLogin(login);
            Validator.ValidatePassword(password);

            login = login.Trim();
            role = NormalizeRole(role);

            if (LoginExists(login))
                throw new InvalidOperationException("Пользователь с таким логином уже существует.");

            using var connection = DatabaseHelper.OpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO пользователи (имя, логин, пароль_хеш, роль)
                VALUES (@name, @login, @hash, @role);";

            command.Parameters.AddWithValue("@name", name.Trim());
            command.Parameters.AddWithValue("@login", login);
            command.Parameters.AddWithValue("@hash", DatabaseHelper.HashPassword(password));
            command.Parameters.AddWithValue("@role", role);

            command.ExecuteNonQuery();
        }

        public static void Update(int id, string name, string login, string password, string role, int currentUserId)
        {
            Validator.Required(name, "Имя");
            Validator.ValidateLogin(login);

            User oldUser = GetById(id);

            if (oldUser == null)
                throw new InvalidOperationException("Пользователь не найден.");

            login = login.Trim();
            role = NormalizeRole(role);

            if (id == currentUserId && role != RoleAdmin)
                throw new InvalidOperationException("Нельзя изменить свою роль администратора.");

            if (oldUser.Роль == RoleAdmin && role != RoleAdmin && CountAdmins() <= 1)
                throw new InvalidOperationException("Нельзя оставить систему без администратора.");

            if (LoginExists(login, id))
                throw new InvalidOperationException("Пользователь с таким логином уже существует.");

            using var connection = DatabaseHelper.OpenConnection();
            using var command = connection.CreateCommand();

            string sql = "UPDATE пользователи SET имя = @name, логин = @login, роль = @role";

            if (!string.IsNullOrWhiteSpace(password))
            {
                Validator.ValidatePassword(password);
                sql += ", пароль_хеш = @hash";
            }

            sql += " WHERE id = @id;";

            command.CommandText = sql;

            command.Parameters.AddWithValue("@name", name.Trim());
            command.Parameters.AddWithValue("@login", login);
            command.Parameters.AddWithValue("@role", role);
            command.Parameters.AddWithValue("@id", id);

            if (!string.IsNullOrWhiteSpace(password))
                command.Parameters.AddWithValue("@hash", DatabaseHelper.HashPassword(password));

            command.ExecuteNonQuery();
        }

        public static void Delete(int id, int currentUserId)
        {
            if (id == currentUserId)
                throw new InvalidOperationException("Нельзя удалить собственную учётную запись.");

            User user = GetById(id);

            if (user == null)
                throw new InvalidOperationException("Пользователь не найден.");

            using var connection = DatabaseHelper.OpenConnection();

            if (user.Роль == RoleAdmin && CountAdmins(connection) <= 1)
                throw new InvalidOperationException("Нельзя удалить последнего администратора.");

            using var transaction = connection.BeginTransaction();

            using var deleteTasks = connection.CreateCommand();
            deleteTasks.Transaction = transaction;
            deleteTasks.CommandText = "DELETE FROM задачи WHERE пользователь_id = @userId;";
            deleteTasks.Parameters.AddWithValue("@userId", id);
            deleteTasks.ExecuteNonQuery();

            using var deleteUser = connection.CreateCommand();
            deleteUser.Transaction = transaction;
            deleteUser.CommandText = "DELETE FROM пользователи WHERE id = @id;";
            deleteUser.Parameters.AddWithValue("@id", id);
            deleteUser.ExecuteNonQuery();

            transaction.Commit();
        }

        private static bool LoginExists(string login, int? excludeId = null)
        {
            using var connection = DatabaseHelper.OpenConnection();
            using var command = connection.CreateCommand();

            string sql = "SELECT COUNT(*) FROM пользователи WHERE логин = @login";

            if (excludeId.HasValue)
                sql += " AND id != @excludeId";

            command.CommandText = sql;
            command.Parameters.AddWithValue("@login", login);

            if (excludeId.HasValue)
                command.Parameters.AddWithValue("@excludeId", excludeId.Value);

            long count = Convert.ToInt64(command.ExecuteScalar());

            return count > 0;
        }

        private static int CountAdmins()
        {
            using var connection = DatabaseHelper.OpenConnection();
            return CountAdmins(connection);
        }

        private static int CountAdmins(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT COUNT(*) FROM пользователи WHERE роль = @role;";
            command.Parameters.AddWithValue("@role", RoleAdmin);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static string NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return RoleUser;

            if (role.Trim().Equals(RoleAdmin, StringComparison.OrdinalIgnoreCase))
                return RoleAdmin;

            return RoleUser;
        }
    }
}