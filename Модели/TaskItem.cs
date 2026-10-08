using System;

namespace Taskora.Модели
{
    public class TaskItem
    {
        public int Id { get; set; }
        public int ПользовательId { get; set; }

        public string Название { get; set; } = "";
        public string Описание { get; set; } = "";
        public string Категория { get; set; } = "";
        public string Приоритет { get; set; } = "";
        public string Статус { get; set; } = "";

        public DateTime? Срок { get; set; }
        public DateTime ДатаСоздания { get; set; }

        public string ИмяВладельца { get; set; } = "";
    }
}