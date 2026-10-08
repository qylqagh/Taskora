using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.Data.Sqlite;
using Taskora.БазаДанных;
using Taskora.Модели;

namespace Taskora.Логика
{
    public static class TaskService
    {
        public static readonly string[] Statuses =
        {
            "Новая",
            "В работе",
            "Выполнена",
            "Отменена"
        };

        public static readonly string[] Priorities =
        {
            "Низкий",
            "Средний",
            "Высокий"
        };

        public static DataTable GetTasks(
            int userId,
            bool isAdmin,
            string search,
            string status,
            string category)
        {
            using var connection = DatabaseHelper.OpenConnection();
            using var command = connection.CreateCommand();

            StringBuilder sql = new StringBuilder();

            sql.Append(@"
                SELECT
                    з.id,
                    з.название,
                    з.категория,
                    з.приоритет,
                    COALESCE(з.срок, '') AS срок,
                    з.статус,
                    з.описание,
                    date(з.дата_создания) AS дата_создания");

            if (isAdmin)
                sql.Append(", п.имя AS автор");

            sql.Append(" FROM задачи з");

            if (isAdmin)
                sql.Append(" LEFT JOIN пользователи п ON з.пользователь_id = п.id");

            sql.Append(" WHERE 1=1");

            if (!isAdmin)
            {
                sql.Append(" AND з.пользователь_id = @userId");
                command.Parameters.AddWithValue("@userId", userId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql.Append(@"
                    AND (
                        з.название LIKE @search
                        OR з.описание LIKE @search
                        OR з.категория LIKE @search
                    )");

                command.Parameters.AddWithValue("@search", $"%{search.Trim()}%");
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Все статусы")
            {
                sql.Append(" AND з.статус = @status");
                command.Parameters.AddWithValue("@status", status);
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "Все категории")
            {
                sql.Append(" AND з.категория = @category");
                command.Parameters.AddWithValue("@category", category);
            }

            sql.Append(@"
                ORDER BY
                    CASE з.приоритет
                        WHEN 'Высокий' THEN 1
                        WHEN 'Средний' THEN 2
                        WHEN 'Низкий' THEN 3
                        ELSE 4
                    END,
                    CASE WHEN з.срок IS NULL THEN 1 ELSE 0 END,
                    з.срок,
                    з.дата_создания DESC;");

            command.CommandText = sql.ToString();

            DataTable table = new DataTable();

            using (var reader = command.ExecuteReader())
            {
                table.Load(reader);
            }

            return table;
        }

        public static List<string> GetCategories(int userId, bool isAdmin)
        {
            using var connection = DatabaseHelper.OpenConnection();
            using var command = connection.CreateCommand();

            string sql = "SELECT DISTINCT категория FROM задачи";

            if (!isAdmin)
                sql += " WHERE пользователь_id = @userId";

            sql += " ORDER BY категория;";

            command.CommandText = sql;

            if (!isAdmin)
                command.Parameters.AddWithValue("@userId", userId);

            List<string> categories = new List<string>();

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                string category = reader["категория"]?.ToString();

                if (!string.IsNullOrWhiteSpace(category))
                    categories.Add(category);
            }

            return categories;
        }

        public static TaskItem GetById(int taskId)
        {
            using var connection = DatabaseHelper.OpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT
                    з.id,
                    з.пользователь_id,
                    з.название,
                    з.описание,
                    з.категория,
                    з.приоритет,
                    з.срок,
                    з.статус,
                    з.дата_создания,
                    п.имя AS владелец
                FROM задачи з
                LEFT JOIN пользователи п ON з.пользователь_id = п.id
                WHERE з.id = @id;";

            command.Parameters.AddWithValue("@id", taskId);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new TaskItem
            {
                Id = Convert.ToInt32(reader["id"]),
                ПользовательId = Convert.ToInt32(reader["пользователь_id"]),
                Название = reader["название"].ToString(),
                Описание = reader["описание"]?.ToString() ?? "",
                Категория = reader["категория"].ToString(),
                Приоритет = reader["приоритет"].ToString(),
                Статус = reader["статус"].ToString(),
                Срок = ReadDate(reader["срок"]),
                ДатаСоздания = ReadDate(reader["дата_создания"]) ?? DateTime.MinValue,
                ИмяВладельца = reader["владелец"]?.ToString() ?? ""
            };
        }

        public static void Create(TaskItem task, int ownerId)
        {
            Validator.Required(task.Название, "Название задачи");

            NormalizeTask(task);

            using var connection = DatabaseHelper.OpenConnection();
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
                    @ownerId,
                    @title,
                    @description,
                    @category,
                    @priority,
                    @dueDate,
                    @status
                );";

            command.Parameters.AddWithValue("@ownerId", ownerId);
            command.Parameters.AddWithValue("@title", task.Название.Trim());
            command.Parameters.AddWithValue("@description", string.IsNullOrWhiteSpace(task.Описание) ? DBNull.Value : task.Описание);
            command.Parameters.AddWithValue("@category", task.Категория);
            command.Parameters.AddWithValue("@priority", task.Приоритет);
            command.Parameters.AddWithValue("@dueDate", task.Срок.HasValue ? task.Срок.Value.ToString("yyyy-MM-dd") : DBNull.Value);
            command.Parameters.AddWithValue("@status", task.Статус);

            command.ExecuteNonQuery();
        }

        public static void Update(TaskItem task, int currentUserId, bool isAdmin)
        {
            Validator.Required(task.Название, "Название задачи");

            EnsureCanModify(task.Id, currentUserId, isAdmin);

            NormalizeTask(task);

            using var connection = DatabaseHelper.OpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE задачи
                SET
                    название = @title,
                    описание = @description,
                    категория = @category,
                    приоритет = @priority,
                    срок = @dueDate,
                    статус = @status
                WHERE id = @id;";

            command.Parameters.AddWithValue("@title", task.Название.Trim());
            command.Parameters.AddWithValue("@description", string.IsNullOrWhiteSpace(task.Описание) ? DBNull.Value : task.Описание);
            command.Parameters.AddWithValue("@category", task.Категория);
            command.Parameters.AddWithValue("@priority", task.Приоритет);
            command.Parameters.AddWithValue("@dueDate", task.Срок.HasValue ? task.Срок.Value.ToString("yyyy-MM-dd") : DBNull.Value);
            command.Parameters.AddWithValue("@status", task.Статус);
            command.Parameters.AddWithValue("@id", task.Id);

            command.ExecuteNonQuery();
        }

        public static void SetStatus(int taskId, int currentUserId, bool isAdmin, string status)
        {
            EnsureCanModify(taskId, currentUserId, isAdmin);

            using var connection = DatabaseHelper.OpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "UPDATE задачи SET статус = @status WHERE id = @id;";

            command.Parameters.AddWithValue("@status", status);
            command.Parameters.AddWithValue("@id", taskId);

            command.ExecuteNonQuery();
        }

        public static void Delete(int taskId, int currentUserId, bool isAdmin)
        {
            EnsureCanModify(taskId, currentUserId, isAdmin);

            using var connection = DatabaseHelper.OpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "DELETE FROM задачи WHERE id = @id;";
            command.Parameters.AddWithValue("@id", taskId);

            command.ExecuteNonQuery();
        }

        private static void EnsureCanModify(int taskId, int currentUserId, bool isAdmin)
        {
            if (taskId <= 0)
                throw new InvalidOperationException("Задача не выбрана.");

            using var connection = DatabaseHelper.OpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT пользователь_id FROM задачи WHERE id = @id;";
            command.Parameters.AddWithValue("@id", taskId);

            object result = command.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("Задача не найдена.");

            int ownerId = Convert.ToInt32(result);

            if (!isAdmin && ownerId != currentUserId)
                throw new InvalidOperationException("Вы можете работать только со своими задачами.");
        }

        private static void NormalizeTask(TaskItem task)
        {
            if (string.IsNullOrWhiteSpace(task.Категория))
                task.Категория = "Без категории";

            if (string.IsNullOrWhiteSpace(task.Приоритет))
                task.Приоритет = "Средний";

            if (string.IsNullOrWhiteSpace(task.Статус))
                task.Статус = "Новая";
        }

        private static DateTime? ReadDate(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            string text = value.ToString();

            if (DateTime.TryParse(text, out DateTime date))
                return date;

            return null;
        }
    }
}