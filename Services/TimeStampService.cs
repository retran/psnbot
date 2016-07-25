using PSNBot.Model;
using System;
using System.Linq;

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
            var timestamp = _databaseService.Select<TimeStamp>("Id", id).FirstOrDefault();
            if (timestamp != null)
            {
                return timestamp.Stamp;
            }

            return DateTime.UtcNow;
        }

    }
}
