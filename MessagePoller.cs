using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PSNBot.Telegram;
using System.Collections.Generic;
using PSNBot.Services;
using PSNBot.Commands;
using PSNBot.Process;
using PSNBot.Model;

namespace PSNBot
{
    public class MessagePoller : IDisposable
    {
        private TelegramClient _client;
        private long _offset = 0;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;
        private PSNService _psnService;
        private AccountService _accounts;
        private DateTime _lastCheckDateTime = DateTime.Now;
        private IEnumerable<Command> _commands;
        private long _chatId;
        private DatabaseService _databaseService;
        private RegistrationProcess _registrationProcess;

        public MessagePoller(DatabaseService databaseService, TelegramClient client, PSNService psnService, AccountService accounts,
            RegistrationProcess registrationProcess, long chatId)
        {
            _chatId = chatId;
            _client = client;
            _psnService = psnService;
            _accounts = accounts;
            _databaseService = databaseService;
            _registrationProcess = registrationProcess;

            _commands = new Command[]
            {
                new TopCommand(_psnService, client, _accounts),
                new SearchCommand(_psnService, client, _accounts),
                new ListCommand(_psnService, client, _accounts),
                new StartCommand(_psnService, client, _accounts, _registrationProcess),
                new HelpCommand(_psnService, client, _accounts),
                new RulesCommand(_psnService, client, _accounts),
                new OnlineCommand(_psnService, client, _accounts),
                new DeleteCommand(_psnService, client, _accounts, _registrationProcess),
                new SetInterestsCommand(_psnService, client, _accounts, _registrationProcess),
                new SetTrophiesCommand(_psnService, client, _accounts, _registrationProcess),
            };
        }

        private async Task Poll()
        {
            try
            {
                var updates = await _client.GetUpdates(new GetUpdatesQuery()
                {
                    Offset = _offset
                });

                if (updates.Result.Any())
                {
                    _offset = updates.Result.Last().UpdateId + 1;
                }

                foreach (var update in updates.Result)
                {
                    if (update.Message != null && (!string.IsNullOrEmpty(update.Message.Text) || update.Message.NewChatMember != null) && (update.Message.Chat.Id == _chatId || update.Message.Chat.Type == "private"))
                    {
                        Handle(update.Message);
                    }
                }
            }
            catch (Exception e)
            {
                //Trace.WriteLine(string.Format("{0}\t{1}", DateTime.Now, e.Message));
            }
        }

        private async void Handle(Message message)
        {
            if (message.From != null)
            {
                Console.WriteLine(string.Format("{0} {1}: {2}", DateTime.Now, message.From.Username, message.Text));
            }

            var account = _accounts.GetById(message.From.Id);
            if (account != null && account.Status != Status.Ok)
            {
                await _registrationProcess.HandleCurrentStep(account, message);
                return;
            }

            if (account != null && account.TelegramName != (message.From.Username ?? string.Empty))
            {
                account.TelegramName = message.From.Username ?? string.Empty;
                _accounts.Update(account);
            }

            if (message.NewChatMember != null && account == null)
            {
                await _client.SendMessage(new SendMessageQuery()
                {
                    ChatId = message.Chat.Id,
                    ReplyToMessageId = message.MessageId,
                    Text = Messages.GetWelcomeMessage(message.NewChatMember)
                });
                return;
            }

            if (string.IsNullOrEmpty(message.Text))
            {
                return;
            }

            message.Text = message.Text.ToLower();

            if (message.Chat.Type != "private" && !message.Text.Contains("@clankbot"))
            {
                return;
            }

            message.Text = message.Text.Replace("@clankbot", "");

            var command = _commands.FirstOrDefault(c => c.IsApplicable(message));
            if (command != null)
            {
                if (command.IsPrivateOnly() && message.Chat.Type != "private")
                {
                    await _client.SendMessage(new SendMessageQuery()
                    {
                        ChatId = message.Chat.Id,
                        ReplyToMessageId = message.MessageId,
                        Text = Messages.PrivateOnlyCommand
                    });

                    return;
                }
                await command.Handle(message);
            }
        }

        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            Task.Factory.StartNew(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var task = Poll();
                    task.Wait();
                    Thread.Sleep(1000);
                }
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // ...
                }

                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _disposed = true;
            }
        }

        ~MessagePoller()
        {
            Dispose(false);
        }
    }
}
