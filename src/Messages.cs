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
        public static readonly string PrivateOnlyCommand = "Извини, эту команду можно использовать только в личных сообщениях.";
        public static readonly string AlreadyRegistered = "Привет! Ты уже зарегистрирован. Используй команду /rules, чтобы прочесть правила, и команду /help, чтобы получить справку по командам.";
        public static readonly string Rules = "RULES";
        public static readonly string Help = "HELP";
        public static readonly string StartAwaitingPSNName = "Пожалуйста, укажи свой идентификатор PSN.";
        public static readonly string StartAwaitingFriendRequest = "Я отправил тебе запрос на добавление в друзья в PSN. Добавь меня и мы сможем продолжить.";
        public static readonly string StartAwaitingInterests = "Пожалуйста, расскажи в двух словах в какие игры ты любишь играть и что тебя интересует.";
        public static readonly string SavedInterests = "Отлично, теперь другие пользователи с похожими интересами смогут тебя найти!";
        public static readonly string PSNNameFault = "Извини, я не смог найти тебя в PSN. Попробуй еще раз.";
        public static readonly string PSNNameSuccess = "Ура, есть такой!";
        public static readonly string AwaitingFriendRequest = "Извини, я жду что ты добавишь меня в друзья в PSN.";
        public static readonly string ConfirmedFriendRequest = "Спасибо! Теперь мы друзья в PSN!";
        public static readonly string EndRegistration = "Поздравляю! Регистрация окончена. Используй команду /rules, чтобы прочесть правила, и команду /help, чтобы получить справку по командам.";
        public static readonly string StartAwaitingTrophies = "Ты хочешь, чтобы твои призы появлялись в чате? Напиши \"да\" или \"нет\".";
        public static readonly string YesOrNo = "Напиши \"да\" или \"нет\".";
        public static readonly string ShowTrophiesYes = "Хорошо! Все увидят твои призы.";
        public static readonly string ShowTrophiesNo = "Ладно. Твои призы не будут появляться в чате.";

        private static string GetName(User user)
        {
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
            return name.ToString().Trim();
        }

        public static string GetWelcomeMessage(User user)
        {
            var sb = new StringBuilder();
            sb.Append("Привет");
            var name = GetName(user);
            if (!string.IsNullOrEmpty(name.ToString()))
            {
                sb.Append(", " + name.ToString());
            }
            sb.Append("! Добро пожаловать в наш уютный чат. Пожалуйста, напиши \"/start\" мне в личные сообщения, чтобы зарегистрироваться и прочесть правила сообщества. С любовью, твой @ClankBot.");
            return sb.ToString();
        }

        public static string GetWelcomePrivateMessage(User user)
        {
            var sb = new StringBuilder();
            sb.Append("Привет");
            var name = GetName(user);
            if (!string.IsNullOrEmpty(name.ToString()))
            {
                sb.Append(", " + name.ToString());
            }
            sb.Append("! Пожалуйста ознакомься с правилами сообщества перед тем как продолжить.");
            return sb.ToString();
        }
    }
}
