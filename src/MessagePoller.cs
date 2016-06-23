using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PSNBot.Telegram;
using System.Net;
using System.IO;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using PsnLib.Entities;
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
                new DeleteCommand(_psnService, client, _accounts)
            };
        }

        private async Task Poll()
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

        private async void Handle(Message message)
        {
            var account = _accounts.GetById(message.From.Id);
            if (account != null && account.Status != Status.Ok)
            {
                await _registrationProcess.HandleCurrentStep(account, message);
                return;
            }

            //if (acc != null && acc.TelegramName != message.From.Username)
            //{
            //    acc.TelegramName = message.From.Username;
            //    _accounts.Persist();
            //}

            if (message.NewChatMember != null)
            {
                await _client.SendMessage(new SendMessageQuery()
                {
                    ChatId = message.Chat.Id,
                    ReplyToMessageId = message.MessageId,
                    Text = Messages.GetWelcomeMessage(message.NewChatMember)
                });
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
