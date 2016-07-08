using PSNBot.Services;
using PSNBot.Telegram;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PSNBot.Commands
{
    public class HelpCommand : Command
    {
        private Regex _regex;
        private AccountService _accounts;
        private TelegramClient _telegramClient;
        private PSNService _psnService;

        public HelpCommand(PSNService psnService, TelegramClient telegramClient, AccountService accounts)
        {
            _psnService = psnService;
            _telegramClient = telegramClient;
            _accounts = accounts;
            _regex = new Regex("^/help", RegexOptions.IgnoreCase);
        }

        public override bool IsPrivateOnly()
        {
            return true;
        }

        public override async Task<bool> Handle(Message message)
        {
            await _telegramClient.SendMessage(new SendMessageQuery()
            {
                ChatId = message.Chat.Id,
                ReplyToMessageId = message.MessageId,
                Text = Messages.Help,
                ParseMode = "HTML",
            });
            return true;
        }

        public override bool IsApplicable(Message message)
        {
            return _regex.IsMatch(message.Text.Trim());
        }
    }
}
