using PSNBot.Telegram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSNBot
{
    public class Messages
    {
        public static readonly string FriendRequestMessage = "Привет! Это Кланк из чата PS4RUS!";

        public static string GetWelcomeMessage(User user)
        {
            var sb = new StringBuilder();

            sb.Append("@");
            sb.Append(user.Username);
            sb.Append(" Привет");

            var name = new StringBuilder();
            if (!string.IsNullOrEmpty(user.FirstName))
            {
                name.Append(user.FirstName);
            }
            if (!string.IsNullOrEmpty(user.LastName))
            {
                name.Append(" ");
                name.Append(user.LastName);
            }

            if (!string.IsNullOrEmpty(name.ToString()))
            {
                sb.Append(", " + name.ToString());
            }
            sb.Append("! Добро пожаловать в наш уютный чат.Пожалуйста, напиши \"/start\" мне в личные сообщения, чтобы зарегистрироваться и прочесть правила сообщества. С любовью, твой @ClankBot.");
            return sb.ToString();
        }
    }
}
