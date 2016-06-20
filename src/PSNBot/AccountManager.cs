using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSNBot
{
    public class Account
    {
        public long TelegramId { get; set; }
        public string TelegramName { get; set; }
        public string PSNName { get; set; }
        public HashSet<long> Chats { get; set; }
        public string Interests { get; set; }

        public Account()
        {
            Chats = new HashSet<long>();
        }
    }

    public class AccountManager
    {
        private List<Account> _accounts;

        public AccountManager()
        {
            _accounts = new List<Account>();
            Load();
        }

        public void Register(long telegramId, string telegramName, string psnName)
        {
            var account = new Account()
            {
                TelegramId = telegramId,
                TelegramName = telegramName,
                PSNName = psnName
            };
            _accounts.Add(account);
            Persist();
        }

        public void Start(long telegramId, long chatId)
        {
            var account = _accounts.First(a => a.TelegramId == telegramId);
            account.Chats.Add(chatId);
            Persist();
        }

        public void SetInterests(long telegramId, string interests)
        {
            var account = _accounts.First(a => a.TelegramId == telegramId);
            account.Interests = interests;
            Persist();
        }

        public void Stop(long telegramId, long chatId)
        {
            var account = _accounts.First(a => a.TelegramId == telegramId);
            account.Chats.Remove(chatId);
            Persist();
        }

        public Account GetById(long id)
        {
            return _accounts.FirstOrDefault(a => a.TelegramId == id);
        }

        public Account GetByPSN(string name)
        {
            return _accounts.FirstOrDefault(a => string.Equals(a.PSNName, name, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<Account> GetAllActive()
        {
            return _accounts.Where(a => a.Chats != null && a.Chats.Any());
        }

        public IEnumerable<Account> GetAll()
        {
            return _accounts;
        }

        private void Load()
        {
            if (File.Exists(".accounts"))
            {
                using (var sr = new StreamReader(".accounts"))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        var splitted = line.Split(' ');
                        var account = new Account();
                        account.TelegramId = long.Parse(splitted[0]);
                        account.TelegramName = splitted[1];
                        account.PSNName = splitted[2];
                        account.Chats = splitted.Length > 3 && !string.IsNullOrEmpty(splitted[3])
                            ? new HashSet<long>(splitted[3].Split(',').Select(v => long.Parse(v)))
                            : new HashSet<long>();
                        _accounts.Add(account);
                    }
                }
            }

            if (File.Exists(".interests"))
            {
                using (var sr = new StreamReader(".interests"))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            var splitted = line.Split((char)0);
                            var id = long.Parse(splitted[0]);
                            var account = _accounts.FirstOrDefault(a => a.TelegramId == id);
                            if (account != null && splitted.Length > 1)
                            {
                                account.Interests = splitted[1];
                            }
                        }
                    }
                }
            }
        }

        public void Persist()
        {
            using (var sw = new StreamWriter(".accounts"))
            {
                foreach (var account in _accounts)
                {
                    sw.WriteLine(string.Format("{0} {1} {2} {3}", account.TelegramId, account.TelegramName, account.PSNName, string.Join(",", account.Chats)));
                }
                sw.Flush();
            }

            using (var sw = new StreamWriter(".interests"))
            {
                foreach (var account in _accounts)
                {
                    sw.WriteLine(account.TelegramId.ToString() + (char)0 + account.Interests);
                }
                sw.Flush();
            }
        }

    }
}
