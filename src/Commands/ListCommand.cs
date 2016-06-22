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
    public class ListCommand : Command
    {
        private Regex _regex;
        private PSNService _psnService;
        private TelegramClient _telegramClient;
        private AccountManager _accounts;

        public ListCommand(PSNService psnService, TelegramClient telegramClient, AccountManager accounts)
        {
            _psnService = psnService;
            _telegramClient = telegramClient;
            _accounts = accounts;
            _regex = new Regex("/list@clankbot", RegexOptions.IgnoreCase);
        }

        public override bool IsApplicable(Message message)
        {
            return _regex.IsMatch(message.Text.Trim());
        }

        public override async Task<bool> Handle(Message message)
        {
            StringBuilder sb = new StringBuilder();
            var lines = await Task.WhenAll(_accounts.GetAll().AsParallel().Select(async account =>
            {
                var builder = new StringBuilder();
                builder.AppendLine(string.Format("Telegram: <b>{0}</b>\nPSN: <b>{1}</b>", account.TelegramName, account.PSNName));

                var userEntry = await _psnService.GetUser(account.PSNName);

                var status = userEntry.GetStatus();
                if (!string.IsNullOrEmpty(status))
                {
                    builder.AppendLine(string.Format("{0}", status));
                }

                if (!string.IsNullOrEmpty(account.Interests))
                {
                    builder.AppendLine(string.Format("{0}", account.Interests));
                }
                builder.AppendLine();
                return builder.ToString();
            }).ToArray());

            foreach (var line in lines)
            {
                if (sb.Length + line.Length < 4096)
                {
                    sb.Append(line);
                }
                else
                {
                    await _telegramClient.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.From.Id,
                        Text = sb.ToString(),
                        ParseMode = "HTML",
                    });
                    sb.Clear();
                    sb.Append(line);
                }
            }

            await _telegramClient.SendMessage(new SendMessageQuery()
            {
                ChatId = message.From.Id,
                Text = sb.ToString(),
                ParseMode = "HTML",
            });

            return true;
        }
    }
}

