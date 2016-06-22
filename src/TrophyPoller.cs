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

namespace PSNBot
{
    public class TrophyPoller : IDisposable
    {
        private TelegramClient _telegramClient;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;
        private PSNService _psnService;
        private AccountManager _accounts;
        private DateTime _lastCheckDateTime = DateTime.Now;
        private long _chatId;

        public TrophyPoller(TelegramClient telegramClient, PSNService psnService, AccountManager accounts, long chatId)
        {
            _chatId = chatId;
            _telegramClient = telegramClient;
            _psnService = psnService;
            _accounts = accounts;
        }

        private async Task Poll()
        {
            try
            {
                var dt = DateTime.Now;
                if ((dt - _lastCheckDateTime).TotalSeconds > 60)
                {
                    DateTime lastTimeStamp = LoadTimeStamp(".timestamp");
                    _lastCheckDateTime = dt;
                    var trophies = await _psnService.GetTrophies(_accounts.GetAllActive());
                    foreach (var ach in trophies.Where(a => a.TimeStamp > lastTimeStamp).OrderBy(a => a.TimeStamp))
                    {
                        lastTimeStamp = ach.TimeStamp;

                        var account = _accounts.GetByPSN(ach.Source);
                        if (account == null)
                        {
                            continue;
                        }

                        if (!account.Chats.Any())
                        {
                            continue;
                        }

                        byte[] image = null;

                        if (!string.IsNullOrEmpty(ach.Image))
                        {
                            WebClient myWebClient = new WebClient();
                            image = myWebClient.DownloadData(ach.Image);
                        }

                        foreach (var id in account.Chats)
                        {
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
                    }
                    SaveTimeStamp(lastTimeStamp, ".timestamp");
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(string.Format("{0}\t{1}", DateTime.Now, e.Message));
            }
        }

        private void SaveTimeStamp(DateTime stamp, string filename)
        {
            using (var sw = new StreamWriter(filename))
            {
                sw.Write(stamp.ToString(CultureInfo.InvariantCulture));
                sw.Flush();
            }
        }

        private DateTime LoadTimeStamp(string filename)
        {
            if (File.Exists(filename))
            {
                using (var sr = new StreamReader(filename))
                {
                    var line = sr.ReadLine();
                    return DateTime.Parse(line, CultureInfo.InvariantCulture);
                }
            }

            return DateTime.UtcNow;
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
