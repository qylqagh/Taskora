using Taskora.Модели;

namespace Taskora.БазаДанных
{
	public static class Session
	{
		public static User CurrentUser { get; set; }

		public static bool IsAdmin => CurrentUser != null && CurrentUser.Роль == "администратор";
	}
}