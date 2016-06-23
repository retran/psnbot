using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PSNBot.Telegram;
using System.Net;
using PSNBot.Services;

namespace PSNBot
{
    public class TrophyPoller : IDisposable
    {
        private TelegramClient _telegramClient;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;
        private PSNService _psnService;
        private AccountService _accounts;
        private DateTime _lastCheckDateTime = DateTime.Now;
        private long _chatId;
        private TimeStampService _timestampService;

        public TrophyPoller(TelegramClient telegramClient, PSNService psnService, AccountService accounts, TimeStampService timestampService, long chatId)
        {
            _chatId = chatId;
            _telegramClient = telegramClient;
            _psnService = psnService;
            _accounts = accounts;
            _timestampService = timestampService;
        }

        private async Task Poll()
        {
            try
            {
                var dt = DateTime.Now;
                if ((dt - _lastCheckDateTime).TotalSeconds > 60)
                {
                    DateTime lastTimeStamp = _timestampService.Get(".timestamp");
                    _lastCheckDateTime = dt;
                    var trophies = await _psnService.GetTrophies(_accounts.GetAllWithShowTrophies());
                    foreach (var ach in trophies.Where(a => a.TimeStamp > lastTimeStamp).OrderBy(a => a.TimeStamp))
                    {
                        lastTimeStamp = ach.TimeStamp;

                        var account = _accounts.GetByPSN(ach.Source);
                        if (account == null)
                        {
                            continue;
                        }

                        byte[] image = null;

                        if (!string.IsNullOrEmpty(ach.Image))
                        {
                            WebClient myWebClient = new WebClient();
                            image = myWebClient.DownloadData(ach.Image);
                        }

                        if (!string.IsNullOrEmpty(ach.Image))
                        {
                            var message = await _telegramClient.SendPhoto(new Telegram.SendPhotoQuery()
                            {
                                ChatId = _chatId
                            }, image);
                        }

                        var msg = await _telegramClient.SendMessage(new Telegram.SendMessageQuery()
                        {
                            ChatId = _chatId,
                            Text = ach.GetTelegramMessage(),
                            ParseMode = "HTML",
                        });
                        Thread.Sleep(1000);
                    }
                    _timestampService.Set(".timestamp", lastTimeStamp);
                }
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

        ~TrophyPoller()
        {
            Dispose(false);
        }
    }
}
