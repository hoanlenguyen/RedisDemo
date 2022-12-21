using HtmlAgilityPack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RedisDemo.Data;
using RedisDemo.Models;
using System.Globalization;
using System.Net;
using System.Xml.Linq;

namespace RedisDemo.Services
{
    public static class AnalyticService
    {
        private static readonly CultureInfo culture = new("en-US");
        private static readonly object _lockObj = new();

        public static void AddAdminUserService(this WebApplication app)
        {
            app.MapPost("CreatePosts", [AllowAnonymous] 
            async Task<IResult> (
            [FromServices] MyDbContext db,
            [FromServices] IConfiguration config,
            string authorId) =>
            {
                try
                {
                    XDocument doc = XDocument.Load("https://www.c-sharpcorner.com/members/" + authorId + "/rss");
                    if (doc == null || doc.Root == null)
                    {
                        return Results.NotFound();
                    }
                    var entries = from item in doc.Root.Descendants().First(i => i.Name.LocalName == "channel").Elements().Where(i => i.Name.LocalName == "item")
                                  select new Feed
                                  {
                                      Content = item.Elements().First(i => i.Name.LocalName == "description").Value,
                                      Link = (item.Elements().First(i => i.Name.LocalName == "link").Value).StartsWith("/") ? "https://www.c-sharpcorner.com" + item.Elements().First(i => i.Name.LocalName == "link").Value : item.Elements().First(i => i.Name.LocalName == "link").Value,
                                      PubDate = Convert.ToDateTime(item.Elements().First(i => i.Name.LocalName == "pubDate").Value, culture),
                                      Title = item.Elements().First(i => i.Name.LocalName == "title").Value,
                                      FeedType = (item.Elements().First(i => i.Name.LocalName == "link").Value).ToLowerInvariant().Contains("blog") ? "Blog" : (item.Elements().First(i => i.Name.LocalName == "link").Value).ToLowerInvariant().Contains("news") ? "News" : "Article",
                                      Author = item.Elements().First(i => i.Name.LocalName == "author").Value
                                  };

                    List<Feed> feeds = entries.OrderByDescending(o => o.PubDate).ToList();
                    string urlAddress = string.Empty;
                    List<ArticleMatrix> articleMatrices = new();
                    _ = int.TryParse(config["ParallelTasksCount"], out int parallelTasksCount);

                    Parallel.ForEach(feeds, new ParallelOptions { MaxDegreeOfParallelism = parallelTasksCount }, feed =>
                    {
                        urlAddress = feed.Link;

                        var httpClient = new HttpClient
                        {
                            BaseAddress = new Uri(urlAddress)
                        };
                        var result = httpClient.GetAsync("").Result;

                        string strData = "";

                        if (result.StatusCode == HttpStatusCode.OK)
                        {
                            strData = result.Content.ReadAsStringAsync().Result;

                            HtmlDocument htmlDocument = new();
                            htmlDocument.LoadHtml(strData);

                            ArticleMatrix articleMatrix = new()
                            {
                                AuthorId = authorId,
                                Author = feed.Author,
                                Type = feed.FeedType,
                                Link = feed.Link,
                                Title = feed.Title,
                                PubDate = feed.PubDate
                            };

                            string category = "Videos";
                            if (htmlDocument.GetElementbyId("ImgCategory") != null)
                            {
                                category = htmlDocument.GetElementbyId("ImgCategory").GetAttributeValue("title", "");
                            }

                            articleMatrix.Category = category;

                            var view = htmlDocument.DocumentNode.SelectSingleNode("//span[@id='ViewCounts']");
                            if (view != null)
                            {
                                articleMatrix.Views = view.InnerText;

                                if (articleMatrix.Views.Contains('m'))
                                {
                                    articleMatrix.ViewsCount = decimal.Parse(articleMatrix.Views[0..^1]) * 1000000;
                                }
                                else if (articleMatrix.Views.Contains('k'))
                                {
                                    articleMatrix.ViewsCount = decimal.Parse(articleMatrix.Views[0..^1]) * 1000;
                                }
                                else
                                {
                                    _ = decimal.TryParse(articleMatrix.Views, out decimal viewCount);
                                    articleMatrix.ViewsCount = viewCount;
                                }
                            }
                            else
                            {
                                var newsView = htmlDocument.DocumentNode.SelectSingleNode("//span[@id='spanNewsViews']");
                                if (newsView != null)
                                {
                                    articleMatrix.Views = newsView.InnerText;

                                    if (articleMatrix.Views.Contains('m'))
                                    {
                                        articleMatrix.ViewsCount = decimal.Parse(articleMatrix.Views[0..^1]) * 1000000;
                                    }
                                    else if (articleMatrix.Views.Contains('k'))
                                    {
                                        articleMatrix.ViewsCount = decimal.Parse(articleMatrix.Views[0..^1]) * 1000;
                                    }
                                    else
                                    {
                                        _ = decimal.TryParse(articleMatrix.Views, out decimal viewCount);
                                        articleMatrix.ViewsCount = viewCount;
                                    }
                                }
                                else
                                {
                                    articleMatrix.ViewsCount = 0;
                                }
                            }
                            var like = htmlDocument.DocumentNode.SelectSingleNode("//span[@id='LabelLikeCount']");
                            if (like != null)
                            {
                                _ = int.TryParse(like.InnerText, out int likes);
                                articleMatrix.Likes = likes;
                            }

                            lock (_lockObj)
                            {
                                articleMatrices.Add(articleMatrix);
                            }
                        }
                    });

                    db.ArticleMatrices.RemoveRange(db.ArticleMatrices.Where(x => x.AuthorId == authorId));

                    foreach (ArticleMatrix articleMatrix in articleMatrices)
                    {
                        if (articleMatrix.Category == "Videos")
                        {
                            articleMatrix.Type = "Video";
                        }
                        articleMatrix.Category = articleMatrix.Category.Replace("&amp;", "&");
                        await db.ArticleMatrices.AddAsync(articleMatrix);
                    }

                    await db.SaveChangesAsync();
                    await cac.RemoveAsync(authorId);
                    return true;
                }
                catch
                {
                    return false;
                }

                //if (result.Succeeded)
                //{
                //    return Results.Ok();
                //}
                //return Results.BadRequest();
            });
        }
    }
}
