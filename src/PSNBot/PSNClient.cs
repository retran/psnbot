using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PsnLib.Managers;
using PsnLib.Entities;
using System.Threading;

namespace PSNBot
{
    public class PSNClient
    {
        private AuthenticationManager _authManager;
        private FriendManager _friendManager;
        private RecentActivityManager _recentActivityManager;
        private UserAccountEntity _userAccountEntity;

        public PSNClient()
        {
            _authManager = new AuthenticationManager();
            _recentActivityManager = new PsnLib.Managers.RecentActivityManager();
            _friendManager = new PsnLib.Managers.FriendManager();
        }

        public void Login(string username, string password)
        {
            var task = _authManager.Authenticate(username, password);
            task.Wait();
            _userAccountEntity = task.Result;
        }

        public bool SendFriendRequest(string name)
        {
            var task = _friendManager.SendFriendRequest(name, "Привет! Это Кланк из чата PS4RUS!", _userAccountEntity);
            task.Wait();
            return task.Result;
        }

        public async Task<IEnumerable<AchievementEntry>> GetAchievements(IEnumerable<Account> accounts)
        {
            var achievements = new List<AchievementEntry>();

            foreach (var account in accounts)
            {
                var activity = await _recentActivityManager.GetActivityFeed(account.PSNName, 0, false, false, _userAccountEntity);

                if (activity != null)
                {
                    achievements.AddRange(GetAchievementsImpl(activity.feed, account));
                }

                Thread.Sleep(100);
            }

            return achievements.OrderBy(a => a.TimeStamp);
        }

        private static IEnumerable<AchievementEntry> GetAchievementsImpl(List<RecentActivityEntity.Feed> feed, Account account)
        {
            if (feed != null)
            {
                foreach (var entry in feed.Where(e => e.StoryType == "TROPHY"))
                {
                    List<RecentActivityEntity.Target> targets = null;

                    if (entry.CondensedStories != null && entry.CondensedStories.Any())
                    {
                        foreach (var story in entry.CondensedStories)
                        {
                            AchievementEntry achievementEntry = new AchievementEntry();

                            achievementEntry.Account = account;
                            achievementEntry.TimeStamp = story.Date;
                            achievementEntry.Event = story.Caption;
                            achievementEntry.Source = story.Source.Meta;
                            
                            targets = story.Targets;

                            var name = targets.FirstOrDefault(t => t.Type == "TROPHY_NAME");
                            var detail = targets.FirstOrDefault(t => t.Type == "TROPHY_DETAIL");
                            var url = targets.FirstOrDefault(t => t.Type == "TROPHY_IMAGE_URL");

                            if (name != null)
                            {
                                achievementEntry.Name = name.Meta;
                            }

                            if (detail != null)
                            {
                                achievementEntry.Detail = detail.Meta;
                            }

                            if (url != null)
                            {
                                achievementEntry.Image = url.Meta;
                            }

                            yield return achievementEntry;
                        }
                    }
                    else
                    {
                        AchievementEntry achievementEntry = new AchievementEntry();

                        achievementEntry.Account = account;
                        achievementEntry.TimeStamp = entry.Date;
                        achievementEntry.Event = entry.Caption;
                        achievementEntry.Source = entry.Source.Meta;
                        targets = entry.Targets;

                        var name = targets.FirstOrDefault(t => t.Type == "TROPHY_NAME");
                        var detail = targets.FirstOrDefault(t => t.Type == "TROPHY_DETAIL");
                        var url = targets.FirstOrDefault(t => t.Type == "TROPHY_IMAGE_URL");

                        if (name != null)
                        {
                            achievementEntry.Name = name.Meta;
                        }

                        if (detail != null)
                        {
                            achievementEntry.Detail = detail.Meta;
                        }

                        if (url != null)
                        {
                            achievementEntry.Image = url.Meta;
                        }

                        yield return achievementEntry;
                    }
                }
            }
        }

    }
}
