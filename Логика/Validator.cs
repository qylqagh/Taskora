using System;

namespace Taskora.Логика
{
    public static class Validator
    {
        public static void Required(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{fieldName} обязательно для заполнения.");
            }
        }

        public static void ValidateLogin(string login)
        {
            Required(login, "Логин");

            login = login.Trim();

            if (login.Length < 3 || login.Length > 30)
            {
                throw new InvalidOperationException("Логин должен содержать от 3 до 30 символов.");
            }

            if (login.Contains(" "))
            {
                throw new InvalidOperationException("Логин не должен содержать пробелы.");
            }
        }

        public static void ValidatePassword(string password)
        {
            Required(password, "Пароль");

            if (password.Length < 6)
            {
                throw new InvalidOperationException("Пароль должен содержать минимум 6 символов.");
            }
        }
    }
}