using PSNBot.Model;
using PSNBot.Process;
using PSNBot.Services;
using PSNBot.Telegram;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PSNBot
{
    public class FriendPoller : IDisposable
    {
        private TelegramClient _telegramClient;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;
        private PSNService _psnService;
        private AccountService _accounts;
        private DateTime _lastCheckDateTime = DateTime.Now;
        private RegistrationProcess _registrationProcess;

        public FriendPoller(TelegramClient telegramClient, PSNService psnService, AccountService accounts, RegistrationProcess registrationProcess)
        {
            _telegramClient = telegramClient;
            _psnService = psnService;
            _accounts = accounts;
            _registrationProcess = registrationProcess;
        }

        private async Task Poll()
        {
            try
            {
                var dt = DateTime.Now;
                if ((dt - _lastCheckDateTime).TotalSeconds > 60)
                {
                    _lastCheckDateTime = dt;
                    var accounts = _accounts.GetAllAwaitingFriendRequest();
                    foreach (var account in accounts)
                    {
                        if (await _psnService.CheckFriend(account.PSNName))
                        {
                            account.Status = Status.AwaitingInterests;
                            _accounts.Update(account);

                            await _telegramClient.SendMessage(new SendMessageQuery()
                            {
                                ChatId = account.Id,
                                Text = Messages.ConfirmedFriendRequest,
                                ParseMode = "HTML",
                            });

                            await _registrationProcess.SendCurrentStep(account);
                            continue;
                        };

                        if ((account.RegisteredAt + new TimeSpan(1, 0, 0)) < DateTime.Now.ToUniversalTime())
                        {
                            await _telegramClient.SendMessage(new SendMessageQuery()
                            {
                                ChatId = account.Id,
                                Text = Messages.AwaitingFriendRequestAborted,
                                ParseMode = "HTML",
                            });

                            await _psnService.RemoveFriend(account.PSNName);
                            _accounts.Delete(account);
                        }
                    }
                }
                Thread.Sleep(10000);
            }
            catch (Exception e)
            {
                Trace.WriteLine(string.Format("{0}\t{1}", DateTime.Now, e.Message));
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

        ~FriendPoller()
        {
            Dispose(false);
        }
    }
}
