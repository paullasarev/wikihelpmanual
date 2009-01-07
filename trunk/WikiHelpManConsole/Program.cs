using Commons.GetOptions;
using DotNetWikiBot;
using HelpManualLib;

namespace WikiHelpManConsole
{
	class WikiPubOptions : Options
	{
		[Option("Wiki URL", "wiki")]
		public string wiki = null;

		[Option("Wiki user", "user")]
		public string user = null;

		[Option("Wiki password", "password")]
		public string password = null;

		[Option("XML Help&Manual export project filename", "project")]
		public string project = null;

		[Option("Category to synchronize", "category")]
		public string category = null;

		[Option("Prefix for wiki pages", "prefix")]
		public string prefix = null;

		[Option("Sync only images", "images")]
		public bool images = false;

		[Option("Sync only pages", "pages")]
		public bool pages = false;

		public bool Validate()
		{
			return wiki != null && user != null && password != null && prefix != null && project != null;
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			WikiPubOptions options = new WikiPubOptions();
			options.ProcessArgs(args);
			if (!options.Validate())
			{
				options.DoHelp();
				return;
			}

			HelpManualProject project = new HelpManualProject(options.project, options.category, options.prefix);
			Site wiki = new Site(options.wiki, options.user, options.password);

			project.Sync(wiki, options.images, options.pages);
		}

	}
}
