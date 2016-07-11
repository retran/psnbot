using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace PSNBot
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.Run(context =>
            {
                try
                {
                    var path = context.Request.Path;

                    if (path.StartsWithSegments(new PathString("/trophy"), StringComparison.OrdinalIgnoreCase))
                    {
                        var title = context.Request.Query["title"];
                        var content = context.Request.Query["content"];
                        var image = context.Request.Query["image"];

                        var html = string.Format(@" <!DOCTYPE html>
                                                    <html xmlns:cc='http://creativecommons.org/ns#'>
                                                        <head prefix='og: http://ogp.me/ns# fb: http://ogp.me/ns/fb#'>
                                                            <meta content='text/html; charset=utf-8' http-equiv='Content-Type'>
                                                            <meta content='{0}' name='title'>
                                                            <meta content='{1}' name='description'>
                                                            <meta content='{2}' property='og:image'>
                                                        </head>
                                                    <body>
                                                        Никак вы, блядь, не научитесь.
                                                    </body>
                                                </html>", title, content, image);
                        return context.Response.WriteAsync(html);
                    }

                    return context.Response.WriteAsync("\"Ламберт, Ламберт - Хер моржовый, Ламберт, Ламберт - вредный хуй!\" (с) Геральт из Ривии, ведьмак.");
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