using PSNBot.Model;
using PSNBot.Process;
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
    class SetInterestsCommand : Command
    {
        private Regex _regex;
        private AccountService _accounts;
        private TelegramClient _telegramClient;
        private PSNService _psnService;
        private RegistrationProcess _registrationProcess;

        public SetInterestsCommand(PSNService psnService, TelegramClient telegramClient, AccountService accounts, RegistrationProcess registrationProcess)
        {
            _psnService = psnService;
            _telegramClient = telegramClient;
            _accounts = accounts;
            _regex = new Regex("^/setinterests", RegexOptions.IgnoreCase);
            _registrationProcess = registrationProcess;
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
                account.Status = Status.AwaitingNewInterests;
                _accounts.Update(account);
                await _registrationProcess.SendCurrentStep(account);
            }
            else
            {
                await _telegramClient.SendMessage(new SendMessageQuery()
                {
                    ChatId = message.Chat.Id,
                    ReplyToMessageId = message.MessageId,
                    Text = Messages.NeedRegister,
                    ParseMode = "HTML",
                });
                return true;
            }
            return true;
        }

        public override bool IsApplicable(Message message)
        {
            return _regex.IsMatch(message.Text.Trim());
        }
    }
}
