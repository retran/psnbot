using System.Threading.Tasks;
using PSNBot.Telegram;
using PSNBot.Services;
using System.Text.RegularExpressions;

namespace PSNBot.Commands
{
    class RulesCommand : Command
    {
        private TelegramClient _telegramClient;
        private PSNService _psnService;
        private AccountService _accounts;
        private Regex _regex;

        public override async Task<bool> Handle(Message message)
        {
            await _telegramClient.SendMessage(new SendMessageQuery()
            {
                ChatId = message.Chat.Id,
                ReplyToMessageId = message.MessageId,
                Text = Messages.Rules,
                ParseMode = "HTML",
            });
            return true;
        }

        public RulesCommand(PSNService psnService, TelegramClient telegramClient, AccountService accounts)
        {
            _psnService = psnService;
            _telegramClient = telegramClient;
            _accounts = accounts;
            _regex = new Regex("^/rules", RegexOptions.IgnoreCase);
        }

        public override bool IsApplicable(Message message)
        {
            return _regex.IsMatch(message.Text.Trim());
        }
    }
}
