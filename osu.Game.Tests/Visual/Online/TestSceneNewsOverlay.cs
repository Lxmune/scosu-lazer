// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.News;
using osu.Game.Overlays.News.Displays;

namespace osu.Game.Tests.Visual.Online
{
    public partial class TestSceneNewsOverlay : OsuTestScene
    {
        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        private NewsOverlay overlay;

        [SetUp]
        public void SetUp() => Schedule(() => Child = overlay = new NewsOverlay());

        [Test]
        public void TestRequest()
        {
            setUpNewsResponse(responseExample);
            AddStep("Show", () => overlay.Show());
            AddStep("Show article", () => overlay.ShowArticle("article"));
        }

        [Test]
        public void TestArticleShownInGame()
        {
            AddStep("set up responses", () => dummyAPI.HandleRequest = request =>
            {
                switch (request)
                {
                    case GetNewsRequest list:
                        list.TriggerSuccess(new GetNewsResponse
                        {
                            NewsPosts = new[] { new APINewsPost { Title = "Welcome to scosu", Slug = "welcome", PublishedAt = DateTimeOffset.Now } },
                        });
                        return true;

                    case GetNewsPostRequest post:
                        post.TriggerSuccess(new GetNewsPostResponse
                        {
                            Title = "Welcome to scosu",
                            Slug = "welcome",
                            Author = "scosu",
                            PublishedAt = DateTimeOffset.Now,
                            Markdown = "Hello **world**, here is where to start.",
                        });
                        return true;
                }

                return false;
            });

            AddStep("show", () => overlay.Show());
            AddUntilStep("card shown", () => overlay.ChildrenOfType<NewsCard>().Any());

            // The card opens the post in the game, not in the browser.
            AddStep("click the card", () => overlay.ChildrenOfType<NewsCard>().First().TriggerClick());
            AddUntilStep("post shown", () => overlay.ChildrenOfType<ArticleDisplay>().Any());
            // The Markdown is drawn a word per sprite.
            AddUntilStep("its text rendered", () => string.Join(" ", overlay.ChildrenOfType<ArticleDisplay>().Single().ChildrenOfType<SpriteText>().Select(t => t.Text.ToString().Trim())).Contains("here is where to start"));
        }

        [Test]
        public void TestMissingArticleGoesToFrontPage()
        {
            AddStep("set up responses", () => dummyAPI.HandleRequest = request =>
            {
                switch (request)
                {
                    case GetNewsRequest list:
                        list.TriggerSuccess(responseExample);
                        return true;

                    case GetNewsPostRequest post:
                        post.TriggerFailure(new InvalidOperationException("not found"));
                        return true;
                }

                return false;
            });
            AddStep("show missing article", () => overlay.ShowArticle("missing"));
            AddUntilStep("front page shown", () => overlay.ChildrenOfType<NewsCard>().Any());
        }

        [Test]
        public void TestCursorRequest()
        {
            setUpNewsResponse(responseWithCursor, "Set up cursor response");
            AddStep("Show", () => overlay.Show());
            AddUntilStep("Show More button is visible", () => showMoreButton?.Alpha == 1);
            setUpNewsResponse(responseWithNoCursor, "Set up no cursor response");
            AddStep("Click Show More", () => showMoreButton?.TriggerClick());
            AddUntilStep("Show More button is hidden", () => showMoreButton?.Alpha == 0);
        }

        private ShowMoreButton showMoreButton => overlay.ChildrenOfType<ShowMoreButton>().FirstOrDefault();

        private void setUpNewsResponse(GetNewsResponse r, string testName = "Set up response")
            => AddStep(testName, () =>
            {
                dummyAPI.HandleRequest = request =>
                {
                    if (!(request is GetNewsRequest getNewsRequest))
                        return false;

                    getNewsRequest.TriggerSuccess(r);
                    return true;
                };
            });

        private static GetNewsResponse responseExample => new GetNewsResponse
        {
            NewsPosts = new[]
            {
                new APINewsPost
                {
                    Title = "This post has an image which starts with \"/\" and has many authors!",
                    Preview = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
                    Author = "someone, someone1, someone2, someone3, someone4",
                    FirstImage = "/help/wiki/shared/news/banners/monthly-beatmapping-contest.png",
                    PublishedAt = DateTimeOffset.Now
                },
                new APINewsPost
                {
                    Title = "This post has a full-url image! (HTML entity: &amp;)",
                    Preview = "boom (HTML entity: &amp;)",
                    Author = "user (HTML entity: &amp;)",
                    FirstImage = "https://assets.ppy.sh/artists/88/header.jpg",
                    PublishedAt = DateTimeOffset.Now
                }
            }
        };

        private static GetNewsResponse responseWithCursor => new GetNewsResponse
        {
            NewsPosts = new[]
            {
                new APINewsPost
                {
                    Title = "This post has an image which starts with \"/\" and has many authors!",
                    Preview = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
                    Author = "someone, someone1, someone2, someone3, someone4",
                    FirstImage = "/help/wiki/shared/news/banners/monthly-beatmapping-contest.png",
                    PublishedAt = DateTimeOffset.Now
                }
            },
            Cursor = new Cursor()
        };

        private static GetNewsResponse responseWithNoCursor => new GetNewsResponse
        {
            NewsPosts = new[]
            {
                new APINewsPost
                {
                    Title = "This post has a full-url image! (HTML entity: &amp;)",
                    Preview = "boom (HTML entity: &amp;)",
                    Author = "user (HTML entity: &amp;)",
                    FirstImage = "https://assets.ppy.sh/artists/88/header.jpg",
                    PublishedAt = DateTimeOffset.Now
                }
            },
            Cursor = null
        };
    }
}
