using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Slack.Webhooks;

namespace SlackerXkcd
{
	public class XkcdService
	{
		private readonly TimeSpan _interval;
		private readonly CancellationTokenSource _cts = new CancellationTokenSource();
		private DateTimeOffset? _lastPostTime;
		private static string _srcRegex = "<img.+?src=[\"](.+?)[\"].*?>";
		private static string _titleRegex = "<img.+?title=[\"](.+?)[\"].*?>";

		private readonly SlackClient _client;

		public XkcdService()
		{
			var pollFrequency = ConfigurationManager.AppSettings["poll-frequency"];

			_interval = new TimeSpan(0, 15, 0);

			if (pollFrequency != null && TimeSpan.TryParse(pollFrequency, out _interval))
			{
				Console.WriteLine("using poll frequency " + pollFrequency);
			}
			_client = new SlackClient(ConfigurationManager.AppSettings["slack-webhook-url"]);
		}
		public void Start()
		{
			CheckFeed();
		}
		public void Stop() { _cts.Cancel(); }

		private void CheckFeed()
		{
			var atomUrl = ConfigurationManager.AppSettings["atom-url"];

			Console.WriteLine("Checking feed {0}", atomUrl);
			WebRequest atomRequest = WebRequest.Create(atomUrl);

			using (WebResponse atomResponse = atomRequest.GetResponse())
			using (XmlReader reader = XmlReader.Create(atomResponse.GetResponseStream()))
			{
				SyndicationFeed feed = SyndicationFeed.Load(reader);

				if (feed != null)
				{
					if (!_lastPostTime.HasValue) _lastPostTime = feed.Items.First().LastUpdatedTime;

					foreach (var item in feed.Items.Where(i => i.LastUpdatedTime > _lastPostTime))
					{
						Console.WriteLine("{0} is new posting", item.Id);
						var imgUrl = Regex.Match(item.Summary.Text, _srcRegex, RegexOptions.IgnoreCase).Groups[1].Value;
						var title = Regex.Match(item.Summary.Text, _titleRegex, RegexOptions.IgnoreCase).Groups[1].Value;
						Console.WriteLine("Image Url: {0}", imgUrl);
						Console.WriteLine("Image Title: {0}", title);
						var message = new SlackMessage
						{
							Username = "XKCD",
							IconUrl = new Uri("http://cdn.androidpolice.com/wp-content/uploads/2013/02/nexusae0_image59.png"),
							Attachments = new List<SlackAttachment>
							{
								new SlackAttachment
								{
									ImageUrl = imgUrl,
									Title = title
								}
							}
						};
						_client.Post(message);

					}
				}
			}
			if (!_cts.IsCancellationRequested)
			{
				Task.Delay(_interval, _cts.Token).ContinueWith(_ => CheckFeed(), _cts.Token);
			}
		}
	}
}
