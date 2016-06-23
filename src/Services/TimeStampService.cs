using PSNBot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSNBot.Services
{
    public class TimeStampService
    {
        private DatabaseService _databaseService;

        public TimeStampService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public void Set(string id, DateTime timestamp)
        {
            _databaseService.Upsert(new TimeStamp()
            {
                Id = id,
                Stamp = timestamp
            });
        }

        public DateTime Get(string id)
        {
            var timestamp = _databaseService.Select<TimeStamp>("id", id).FirstOrDefault();
            if (timestamp != null)
            {
                return timestamp.Stamp.ToUniversalTime();
            }

            return DateTime.UtcNow;
        }

    }
}
