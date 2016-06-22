using PSNBot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSNBot.Services
{
    public class AccountService
    {
        private List<Account> _accounts;
        private DatabaseService _databaseService;

        public AccountService(DatabaseService databaseService)
        {
            _accounts = new List<Account>();
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
            return _databaseService.Select<Account>("ShowTrophies", true);
        }

        public IEnumerable<Account> GetAll()
        {
            return _databaseService.Select<Account>();
        }

        public IEnumerable<Account> Search(string text)
        {
            return _databaseService.Search<Account>(text);
        }
    }
}
