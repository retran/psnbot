using System;
using System.Threading.Tasks;
using PSNBot.Telegram;
using PSNBot.Services;
using System.Text.RegularExpressions;
using PSNBot.Process;

namespace PSNBot.Commands
{
    public class StartCommand : Command
    {
        private Regex _regex;
        private AccountService _accounts;
        private TelegramClient _telegramClient;
        private PSNService _psnService;
        private RegistrationProcess _registrationProcess;

        public StartCommand(PSNService psnService, TelegramClient telegramClient, AccountService accounts, RegistrationProcess registrationProcess)
        {
            _psnService = psnService;
            _telegramClient = telegramClient;
            _accounts = accounts;
            _registrationProcess = registrationProcess;
            _regex = new Regex("/start", RegexOptions.IgnoreCase);
        }

        public override bool IsPrivateOnly()
        {
            return true;
        }

        public override async Task<bool> Handle(Message message)
        {
            try
            {
                var account = _accounts.GetById(message.From.Id);
                if (account != null)
                {
                    await _telegramClient.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        Text = Messages.AlreadyRegistered,
                        ParseMode = "HTML",
                    });

                    return true;
                }

                account = _accounts.Create(message.From.Id, message.From.Username);

                await _telegramClient.SendMessage(new SendMessageQuery()
                {
                    ChatId = message.Chat.Id,
                    Text = Messages.GetWelcomePrivateMessage(message.From),
                    ParseMode = "HTML",
                });

                await _telegramClient.SendMessage(new SendMessageQuery()
                {
                    ChatId = message.Chat.Id,
                    Text = Messages.Rules,
                    ParseMode = "HTML",
                });

                await _registrationProcess.SendCurrentStep(account);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool IsApplicable(Message message)
        {
            return _regex.IsMatch(message.Text.Trim());
        }
    }
}
