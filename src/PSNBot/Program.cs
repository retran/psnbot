using PsnLib.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PsnLib.Entities;

namespace PSNBot
{
    class Program
    {
        static async void Do()
        {
            var authManager = new AuthenticationManager();
            var userAccountEntity = await authManager.Authenticate("", "");

            var user = await authManager.GetUserEntity(userAccountEntity);

            var manager = new PsnLib.Managers.RecentActivityManager();


            //PsnLib.Managers.
            var activity = await manager.GetActivityFeed("RetranDeLarten", 0, false, true, userAccountEntity);

            foreach (var entry in activity.feed)
            {
                if (entry.CondensedStories != null && entry.CondensedStories.Any())
                { 
                    foreach (var story in entry.CondensedStories)
                    {
                        Console.WriteLine();
                        Console.WriteLine(story.Date);
                        Console.WriteLine(story.Caption);
                        Console.WriteLine(GetAchievmentLine(story.Targets));
                    }
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine(entry.Date);
                    Console.WriteLine(entry.Caption);
                    Console.WriteLine(GetAchievmentLine(entry.Targets));
                }
            }
        }

        private static string GetAchievmentLine(List<RecentActivityEntity.Target> targets)
        {
            var name = targets.FirstOrDefault(t => t.Type == "TROPHY_NAME");
            var detail = targets.FirstOrDefault(t => t.Type == "TROPHY_DETAIL");
            var url = targets.FirstOrDefault(t => t.Type == "TROPHY_IMAGE_URL");

            var sb = new StringBuilder();
            if (name != null)
            {
                sb.Append(name.Meta);
                sb.Append("\n");
                sb.Append(detail.Meta);
                sb.Append("\n");
                sb.Append(url.Meta);
            }
            return sb.ToString();
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            Do();
            Console.ReadKey();
        }
    }
}
