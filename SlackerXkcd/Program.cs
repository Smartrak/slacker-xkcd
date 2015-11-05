using Topshelf;

namespace SlackerXkcd
{
	public class Program
	{
		static void Main(string[] args)
		{
			HostFactory.Run(x =>
			{
				x.Service<XkcdService>(s =>
				{
					s.ConstructUsing(name => new XkcdService());
					s.WhenStarted(tc => tc.Start());
					s.WhenStopped(tc => tc.Stop());
				});
				x.RunAsLocalSystem();

				x.SetDescription("Posts the latest XKCD when it comes out");
				x.SetDisplayName("Slacker XKCD");
				x.SetServiceName("SlackerXKCD");
			});
		}
	}
}
