namespace Taskora.Модели
{
    public class User
    {
        public int Id { get; set; }
        public string Имя { get; set; } = "";
        public string Логин { get; set; } = "";
        public string Роль { get; set; } = "";
    }
}