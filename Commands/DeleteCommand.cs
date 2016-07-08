using PSNBot.Model;
using PSNBot.Process;
using PSNBot.Services;
using PSNBot.Telegram;
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
        private RegistrationProcess _process;

        public DeleteCommand(PSNService psnService, TelegramClient telegramClient, AccountService accounts, RegistrationProcess process)
        {
            _psnService = psnService;
            _telegramClient = telegramClient;
            _accounts = accounts;
            _regex = new Regex("^/delete", RegexOptions.IgnoreCase);
            _process = process;
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
                account.Status = Status.AwaitingDeleteConfirmation;
                _accounts.Update(account);
                await _process.SendCurrentStep(account);
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
