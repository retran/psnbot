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

                    var html = "\"Ламберт, Ламберт - Хер моржовый, Ламберт, Ламберт - вредный хуй!\" (с) Геральт из Ривии, ведьмак.";

                    if (path.StartsWithSegments(new PathString("/trophy"), StringComparison.OrdinalIgnoreCase))
                    {
                        var title = context.Request.Query["title"];
                        var content = context.Request.Query["content"];
                        var image = context.Request.Query["image"];

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
                                                </html>", title, content, image);
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
