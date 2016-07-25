using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using PSNBot.Services;

namespace PSNBot
{
    public class Startup
    {
        private PSNService _client;
        public Startup ()
        {
            _client = new PSNService();
            var task = _client.Login("retran@tolkien.ru", "");
            task.Wait();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Run(context =>
            {
                try
                {
                    var path = context.Request.Path;

                    var html = "\"Ламберт, Ламберт - Хер моржовый, Ламберт, Ламберт - вредный хуй!\" (с) Геральт из Ривии, ведьмак.";

                    if (path.StartsWithSegments(new PathString("/trophy"), StringComparison.OrdinalIgnoreCase))
                    {
                        var segments = path.Value.Split(new [] { '/' }, StringSplitOptions.None);
                        var id = int.Parse(WebUtility.UrlDecode(segments[2]));
                        var npComm = WebUtility.UrlDecode(segments[3]);

                        var trophy = _client.GetTrophy(npComm, id);
                        trophy.Wait();

                        if (trophy.Result != null)
                        {
                            html = string.Format(@" <!DOCTYPE html>
                                                        <html 	xmlns=""http://www.w3.org/1999/xhtml""
                                    xmlns:cc=""http://creativecommons.org/ns#""
                                    xmlns:fb=""http://ogp.me/ns/fb#"">
                                                            <head prefix=""og: http://ogp.me/ns# fb: http://ogp.me/ns/fb#"">
                                                                <meta content=""text/html; charset=utf-8"" http-equiv=""Content-Type"">
                                                                <meta name=""title"" property=""og:title"" content=""{0}"" />
                                                                <meta name=""description"" property=""og:description"" content=""{1}"" />
                                                                <meta property=""og:image"" content=""{2}"" />
                                                            </head>
                                                        <body>
                                                            Никак вы, блядь, не научитесь.
                                                        </body>
                                                    </html>", trophy.Result.Name, trophy.Result.Detail, trophy.Result.Image);
                        }
                    }

                    context.Response.Headers.Add("Content-Length", System.Text.Encoding.UTF8.GetBytes(html).Length.ToString());
                    context.Response.Headers.Add("Cache-Control", "max-age=0, no-store");
                    context.Response.Headers.Add("Content-Type", "text/html; charset=UTF-8");

                    return context.Response.WriteAsync(html);
                }
                catch (Exception e)
                {
                    //
                    return context.Response.WriteAsync("");
                }
            });
        }
    }
}
