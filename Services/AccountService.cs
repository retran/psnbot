using PSNBot.Model;
using System.Collections.Generic;
using System.Linq;
using System;

namespace PSNBot.Services
{
    public class AccountService
    {
        private DatabaseService _databaseService;

        public AccountService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public Account GetById(long id)
        {
            return _databaseService.Select<Account>("Id", id).FirstOrDefault();
        }

        public Account GetByPSN(string name)
        {
            return _databaseService.Select<Account>("PSNName", name).FirstOrDefault();
        }

        public IEnumerable<Account> GetAllWithShowTrophies()
        {
            return _databaseService.Select<Account>("ShowTrophies", true).ToArray();
        }

        public IEnumerable<Account> GetAll()
        {
            return _databaseService.Select<Account>().ToArray();
        }

        public IEnumerable<Account> Search(string text)
        {
            return _databaseService.Search<Account>(text).ToArray();
        }

        public void Delete(Account account)
        {
            _databaseService.Delete(account);
        }

        public Account Create(long id, string username)
        {
            var account = new Account()
            {
                Id = id,
                TelegramName = username ?? string.Empty,
                ShowTrophies = false,
                Status = Status.AwaitingPSNName,
                Interests = string.Empty,
                PSNName = string.Empty,
                RegisteredAt = DateTime.Now.ToUniversalTime(),
                LastPolledTrophy = DateTime.Now.ToUniversalTime()
            };

            _databaseService.Insert(account);

            return account;
        }

        public IEnumerable<Account> GetAllAwaitingFriendRequest()
        {
            return _databaseService.Select<Account>("Status", Status.AwaitingFriendRequest).ToArray();
        }

        public void Update(Account account)
        {
            _databaseService.Update(account);
        }
    }
}
