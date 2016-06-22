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
    public class SearchCommand : Command
    {
        private Regex _regex;
        private PSNService _psnService;
        private TelegramClient _telegramClient;
        private AccountManager _accounts;

        public SearchCommand(PSNService psnService, TelegramClient telegramClient, AccountManager accounts)
        {
            _psnService = psnService;
            _telegramClient = telegramClient;
            _accounts = accounts;
            _regex = new Regex("/search@clankbot\\s+(.+)", RegexOptions.IgnoreCase);
        }

        public override bool IsApplicable(Message message)
        {
            return _regex.IsMatch(message.Text.Trim());
        }

        public override async Task<bool> Handle(Message message)
        {
            var match = _regex.Match(message.Text);
            if (match.Groups.Count < 2)
            {
                return false; // send error message
            }

            var interests = match.Groups[1].Value;
            StringBuilder sb = new StringBuilder();

            var filtered = _accounts.GetAll().Where(a => string.IsNullOrEmpty(interests)
                || (!string.IsNullOrEmpty(a.Interests) && a.Interests.ToLower().Contains(interests.ToLower()))
                || (!string.IsNullOrEmpty(a.TelegramName) && a.TelegramName.ToLower().Contains(interests.ToLower()))
                || (!string.IsNullOrEmpty(a.PSNName) && a.PSNName.ToLower().Contains(interests.ToLower())));

            var lines = filtered.AsParallel().Select(async account =>
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
            }).ToArray();

            Task.WaitAll(lines);

            foreach (var line in lines)
            {
                if (sb.Length + line.Result.Length < 4096)
                {
                    sb.Append(line.Result);
                }
                else
                {
                    await _telegramClient.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = sb.ToString(),
                        ParseMode = "HTML",
                    });
                    sb.Clear();
                    sb.Append(line.Result);
                }
            }

            await _telegramClient.SendMessage(new SendMessageQuery()
            {
                ChatId = message.Chat.Id,
                ReplyToMessageId = message.MessageId,
                Text = sb.ToString(),
                ParseMode = "HTML",
            });

            return true;
        }
    }
}
