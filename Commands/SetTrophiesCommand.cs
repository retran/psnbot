using PSNBot.Model;
using PSNBot.Process;
using PSNBot.Services;
using PSNBot.Telegram;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PSNBot.Commands
{
    class SetTrophiesCommand : Command
    {
        private Regex _regex;
        private AccountService _accounts;
        private TelegramClient _telegramClient;
        private PSNService _psnService;
        private RegistrationProcess _registrationProcess;

        public SetTrophiesCommand(PSNService psnService, TelegramClient telegramClient, AccountService accounts, RegistrationProcess registrationProcess)
        {
            _psnService = psnService;
            _telegramClient = telegramClient;
            _accounts = accounts;
            _regex = new Regex("^/settrophies", RegexOptions.IgnoreCase);
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
                account.Status = Status.AwaitingNewTrophies;
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
