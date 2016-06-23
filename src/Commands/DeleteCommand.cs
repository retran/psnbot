using PSNBot.Services;
using PSNBot.Telegram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PSNBot.Commands
{
    public class DeleteCommand : Command
    {
        private Regex _regex;
        private AccountService _accounts;
        private TelegramClient _telegramClient;
        private PSNService _psnService;

        public DeleteCommand(PSNService psnService, TelegramClient telegramClient, AccountService accounts)
        {
            _psnService = psnService;
            _telegramClient = telegramClient;
            _accounts = accounts;
            _regex = new Regex("/delete", RegexOptions.IgnoreCase);
        }

        public override bool IsPrivateOnly()
        {
            return true;
        }

        public override async Task<bool> Handle(Message message)
        {
            var account = _accounts.GetById(message.From.Id);

            if (account != null)
            {
                _accounts.Delete(account);
                await _telegramClient.SendMessage(new SendMessageQuery()
                {
                    ChatId = message.Chat.Id,
                    ReplyToMessageId = message.MessageId,
                    Text = "Ты удален из базы данных. До свидания :(",
                    ParseMode = "HTML",
                });
            }
            return true;
        }

        public override bool IsApplicable(Message message)
        {
            return _regex.IsMatch(message.Text.Trim());
        }
    }
}
