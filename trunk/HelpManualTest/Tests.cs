using System;
using System.IO;
using System.Xml;
using DotNetWikiBot;
using HelpManualLib;
using NUnit.Framework;

namespace HelpManualTest
{
	public class Utils
	{
		public const string localwiki = "http://vangog/wiki/";
		public const string localwikiName = "KintWiki";
		public const string localwikiUser = "Rebot";
		public const string localwikiPass = "152369876208566";
		public const string TestDataDir = "../../TestData";

		public static void ClearCategory(Site site, string category, string reason)
		{
			PageList publist = new PageList(site);
			publist.FillAllFromCategory(category);
			foreach (Page page in publist)
			{
				page.Delete(reason);
			}
		}

		public static void AddPage(Site site, string category, string title, string text)
		{
			Page newpage = new Page(site);
			newpage.title = title;
			newpage.text = String.Format("[[Категория: {0}]]\n{1}\n", category, text);
			newpage.Save();
		}

		public static void AddNewPage(Site site, string category, string title, string text)
		{
			Page page = new Page(site, title);
			if (page.LoadTry())
				page.Delete("test setup");
			AddPage(site, category, title, text);
		}

		public static void AddImage(Site site, string title, string imageFileName)
		{
			Page newpage = new Page(site);
			newpage.title = title;
			newpage.UploadImage(imageFileName, "", "", "", "");
		}

		public static void PrintXmlTree(XmlNode xmlDoc, TextWriter outStream)
		{
			printXmlNode(xmlDoc, outStream, 0);
		}

		private static void printXmlNode(XmlNode rootNode, TextWriter outStream, int level)
		{
			levelWriteLine(level, outStream, "{0}: Name:<{1}> Attributes:<{2}> Value:<{3}>", rootNode.ToString()
						   , rootNode.Name, attributesString(rootNode.Attributes), rootNode.Value);

			foreach (XmlNode node in rootNode.ChildNodes)
			{
				printXmlNode(node, outStream, level + 1);
			}
		}

		private static void levelWriteLine(int level, TextWriter outStream, string format, params object[] args)
		{
			for (int i = 0; i < level; i++)
				outStream.Write("-");
			outStream.WriteLine(format, args);
		}

		private static string attributesString(XmlAttributeCollection attributes)
		{
			if (attributes == null)
				return "";
			StringWriter writer = new StringWriter();
			foreach (XmlAttribute attribute in attributes)
				writer.Write("{0}={1}", attribute.Name, attribute.Value);
			return writer.ToString();
		}

	
	}

	[TestFixture]
	public class ReadProjectTests
	{
		public string filename = Path.Combine(Utils.TestDataDir, "OneTopicProject/Xml/200_ObjektSystem.xml");
		const string category = "Тесты синхронизации Help&Manual";
		private const string prefix = "Тест HM: ";
		private HelpManualProject project;

		[SetUp]
		public void SetUp()
		{
			project = new HelpManualProject(filename, category, prefix);
		}

		[Test]
		public void ReadOneTopicProject()
		{
			XmlNode topic = project.xmlDoc.SelectSingleNode("/helpproject/topics/topic");
			Assert.IsNotNull(topic);
		}

		[Test]
		public void ReadTopicList()
		{
			TopicList topicList = project.topicList;
			Assert.IsNotNull(topicList);
			Assert.Greater(topicList.Count, 0);
		}

		[Test]
		public void TopicID()
		{
			TopicList topicList = project.topicList;

			string id = "Объектысистемы";
			Topic topic = topicList[id];
			Assert.IsNotNull(topic);
			Assert.AreEqual(id, topic.ID);
		}

		[Test]
		public void TopicHeader()
		{
			TopicList topicList = project.topicList;

			string id = "Объектысистемы";
			string header = "Объекты системы";
			Topic topic = topicList[id];
			//Utils.PrintXmlTree(topic.Node, Console.Out);
			Assert.AreEqual(header, topic.Header);
		}

		[Test]
		public void TopicNode()
		{
			TopicList topicList = project.topicList;

			string id = "Объектысистемы";
			Topic topic = topicList[id];
			Assert.IsInstanceOfType(typeof(XmlNode), topic.Node);
		}

		[Test]
		public void SimpleText2Wiki()
		{
			Topic topic = project.topicList["SimpleText"];
			string wikiText = topic.rawToWiki();
			string sample = "Вот он, простой текст.";
			Assert.AreEqual(sample, wikiText);
			
		}

		[Test]
		public void ДваПараграфа2Wiki()
		{
			Topic topic = project.topicList["ДваПараграфа"];
			string wikiText = topic.rawToWiki();
			string sample = "Первый параграф\n\nВторой параграф";
			Assert.AreEqual(sample, wikiText);

		}

		[Test]
		public void ImagePng()
		{
			Topic topic = project.topicList["Изображение_PNG"];
			string wikiText = topic.rawToWiki();
			string sample = "[[Изображение:Тест_HM-_VistaRunAsAdmin.png]]simple text.";
			Assert.AreEqual(sample, wikiText);

		}

		[Test]
		public void ImageBMP()
		{
			Topic topic = project.topicList["ИзображениеBMP"];
			string wikiText = topic.rawToWiki();
			string sample = "[[Изображение:Тест_HM-_0001.png]]";
			Assert.AreEqual(sample, wikiText);

		}

		[Test]
		public void GetImageFileName()
		{
			string imageName = "VistaRunAsAdmin.png";
			
			string fileImageName = project.GetImageFileName(imageName);

			string etalonFileName = Path.GetFullPath(Path.Combine(Utils.TestDataDir, "OneTopicProject/Xml/Images/VistaRunAsAdmin.png"));
			Assert.AreEqual(etalonFileName, fileImageName);
		}

		[Test]
		public void GetImageFileNameBMP()
		{
			string imageName = "0001.bmp";
			string etalonFileName = Path.GetFullPath(Path.Combine(Utils.TestDataDir, "OneTopicProject/Xml/Images/0001.png"));
			if (File.Exists(etalonFileName))
				File.Delete(etalonFileName);

			string fileImageName = project.GetImageFileName(imageName);

			Assert.AreEqual(etalonFileName, fileImageName);
			Assert.IsTrue(File.Exists(fileImageName));
		}

		[Test]
		public void GetImageFileName_ShortFilename()
		{
			string imageName = "10.png";
			string etalonFileName = Path.GetFullPath(Path.Combine(Utils.TestDataDir, "OneTopicProject/Xml/Images/101010.png"));
			if (File.Exists(etalonFileName))
				File.Delete(etalonFileName);

			string fileImageName = project.GetImageFileName(imageName);

			Assert.AreEqual(etalonFileName, fileImageName);
			Assert.IsTrue(File.Exists(fileImageName));
		}

		[Test]
		public void GetImages()
		{
			Topic topic = project.topicList["Изображение_PNG"];
			string[] images = topic.GetImages();
			string[] etalon = {"VistaRunAsAdmin.png"};
			Assert.AreEqual(etalon, images);
		}

		[Test]
		public void GetImagesTwoImages()
		{
			Topic topic = project.topicList["Два_изображения_PNG"];
			string[] images = topic.GetImages();
			string[] etalon = { "button_main.gif", "button_next.gif" };
			Assert.AreEqual(etalon, images);
		}

		[Test]
		public void GetImagesBMP()
		{
			Topic topic = project.topicList["ИзображениеBMP"];
			string[] images = topic.GetImages();
			string[] etalon = { "0001.bmp" };
			Assert.AreEqual(etalon, images);
		}

		[Test]
		public void AddImagePrefixBMP()
		{
			string wikiImageName = project.AddImagePrefix("0001.bmp");
			Assert.AreEqual("Тест_HM-_0001.png", wikiImageName);
		}

		[Test]
		public void AddImagePrefixPNG()
		{
			string wikiImageName = project.AddImagePrefix("0001.png");
			Assert.AreEqual("Тест_HM-_0001.png", wikiImageName);
		}
	}


	[Explicit]
	[TestFixture]
	public class WikiSyncTests
	{
		private Site localSite;
		const string category = "Тесты синхронизации Help&Manual";
		private const string prefix = "Тест HM: ";
		public string filename = Path.Combine(Utils.TestDataDir, "OneTopicProject/Xml/200_ObjektSystem.xml");

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			if (Directory.Exists("Cache"))
				Directory.Delete("Cache", true);

			localSite = new Site(Utils.localwiki, Utils.localwikiUser, Utils.localwikiPass);
		}

		[SetUp]
		public void SetUp()
		{
			Utils.ClearCategory(localSite, category, "WikiSync test");
		}

		[Test]
		public void SimpleProject()
		{
			HelpManualProject project = new HelpManualProject(filename, category, prefix);
			project.Sync(localSite);

			PageList list = WikiPubLib.Sync.GetCategoryList(localSite, category);
			Assert.AreEqual(project.topicList.Count, list.Count());
		}

		[Test]
		public void DeletePage()
		{
			Utils.AddPage(localSite, category, "SimpleText", "test");
			HelpManualProject project = new HelpManualProject(filename, category, prefix);
			project.Sync(localSite);

			PageList list = WikiPubLib.Sync.GetCategoryList(localSite, category);
			Assert.AreEqual(project.topicList.Count, list.Count());
		}

		[Test]
		public void ExistingPage()
		{
			string pageName = "Тест HM: SimpleText";
			Utils.AddPage(localSite, category, pageName, "test");
			HelpManualProject project = new HelpManualProject(filename, category, prefix);
			project.Sync(localSite);

			PageList list = WikiPubLib.Sync.GetCategoryList(localSite, category);
			Assert.AreEqual(project.topicList.Count, list.Count());

			Page page = new Page(localSite, pageName);
			page.Load();

			Topic topic = project.topicHeaderList["SimpleText"];
			Assert.AreEqual(topic.ToWiki(), page.text);
		}


		[Test]
		public void ImagePage()
		{
			string imageName = "Изображение:Тест_HM-_VistaRunAsAdmin.png";

			Page page = new Page(localSite, imageName);
			try {page.Delete("test");} catch {}

			HelpManualProject project = new HelpManualProject(filename, category, prefix);
			project.Sync(localSite);

			string tempFile = Path.GetTempFileName();

			page = new Page(localSite, imageName);
			page.DownloadImage(tempFile);

			string imageFileName = Path.Combine(Utils.TestDataDir, "OneTopicProject/Xml/Images/VistaRunAsAdmin.png");
			FileAssert.AreEqual(imageFileName, tempFile);
			File.Delete(tempFile);
		}

		[Test]
		public void ImagePageBMP()
		{
			string imageName = "Изображение:Тест_HM-_0001.png";

			Page page = new Page(localSite, imageName);
			try { page.Delete("test"); }
			catch { }

			HelpManualProject project = new HelpManualProject(filename, category, prefix);
			project.Sync(localSite);

			string tempFile = Path.GetTempFileName();

			page = new Page(localSite, imageName);
			page.DownloadImage(tempFile);

			string imageFileName = Path.Combine(Utils.TestDataDir, "OneTopicProject/Xml/Images/0001.png");
			FileAssert.AreEqual(imageFileName, tempFile);
			File.Delete(tempFile);
		}


	}

	[TestFixture]
	public class PageProperties
	{
		public string filename = Path.Combine(Utils.TestDataDir, "PageProperties/Xml/PageProperties.xml");
		const string category = "Тесты синхронизации Help&Manual - PageProperties";
		private const string prefix = "Тест HM PageProperties: ";
		private HelpManualProject project;

		[SetUp]
		public void SetUp()
		{
			project = new HelpManualProject(filename, category, prefix);
		}


		[Test]
		public void Курсив()
		{
			Topic topic = project.topicList["Курсив"];
			string wikiText = topic.rawToWiki();
			string sample = "Выделение текста ''курсивом''.";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void Ссылка()
		{
			Topic topic = project.topicList["Ссылка"];
			string wikiText = topic.rawToWiki();
			string sample = String.Format("Далее идет ссылка курсив.[[{0}Курсив|Курсив]] Это все.", prefix);
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ВнешняяСсылка()
		{
			Topic topic = project.topicList["ВнешняяСсылка"];
			string wikiText = topic.rawToWiki();
			string sample = String.Format("Это внешняя ссылка [http://www.kint.ru КИНТ]");
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void Жирный()
		{
			Topic topic = project.topicList["Жирный"];
			string wikiText = topic.rawToWiki();
			string sample = "Это '''жирный''' текст.";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void Подчеркивание()
		{
			Topic topic = project.topicList["Подчеркивание"];
			string wikiText = topic.rawToWiki();
			string sample = "Это <u>подчеркивание</u>.";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ЖирныйКурсив()
		{
			Topic topic = project.topicList["ЖирныйКурсив"];
			string wikiText = topic.rawToWiki();
			string sample = "Это '''''жирный курсив'''''";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ПростойСписок()
		{
			Topic topic = project.topicList["ПростойСписок"];
			string wikiText = topic.rawToWiki();
			string sample = "Это простой список:\n*Пункт 1\n*Пункт 2\n*Пункт 3";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void НумерованныйСписок()
		{
			Topic topic = project.topicList["НумерованныйСписок"];
			string wikiText = topic.rawToWiki();
			string sample = "Это нумерованный список:\n#Пункт 1\n#Пункт 2\n#Пункт 3";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ВложенныйСписок()
		{
			Topic topic = project.topicList["ВложенныйСписок"];
			string wikiText = topic.rawToWiki();
			string sample =
				"Вложенный список:\n" +
				"*Пункт 1\n" +
				"**Пункт 1.1\n" +
				"**Пункт 1.2\n" +
				"*Пункт 2";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ВложенныйНумерованныйСписок()
		{
			Topic topic = project.topicList["ВложенныйНумерованныйСписок"];
			string wikiText = topic.rawToWiki();
			string sample =
				"Вложенный нумерованный список:\n" +
				"#Пункт 1\n" +
				"##Пункт 1.1\n" +
				"##Пункт 1.2\n" +
				"#Пункт 2";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ВыделениеВСписке()
		{
			Topic topic = project.topicList["ВыделениеВСписке"];
			string wikiText = topic.rawToWiki();
			string sample = "Список:\n*Пункт с '''выделением'''";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ПростаяТаблица()
		{
			Topic topic = project.topicList["ПростаяТаблица"];
			string wikiText = topic.rawToWiki();
			string sample = "{|border=1\n" +
			"|-\n" +
			"|\nЯчейка 1.1\n" +
			"|\nЯчейка 1.2\n" +
			"|\nЯчейка 1.3\n" +
			"|-\n" +
			"|\nЯчейка 2.1\n" +
			"|\nЯчейка 2.2\n" +
			"|\nЯчейка 2.3\n" +
			"|}";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ВыравниваниеЯчеекВТаблице()
		{
			Topic topic = project.topicList["ВыравниваниеЯчеекВТаблице"];
			string wikiText = topic.rawToWiki();
			string sample = "{|border=1\n" +
			"|-\n" +
			"|align=\"center\"|\nЯчейка 1.1\n" +
			"|align=\"center\"|\nЯчейка 1.2\n" +
			"|\nЯчейка 1.3\n" +
			"|-\n" +
			"|align=\"right\"|\nЯчейка 2.1\n" +
			"|align=\"right\"|\nЯчейка 2.2\n" +
			"|\nЯчейка 2.3\n" +
			"|}";
			Assert.AreEqual(sample, wikiText);
		}
		
		[Test]
		public void ТаблицаДваПараграфаВЯчейке()
		{
			Topic topic = project.topicList["ТаблицаДваПараграфаВЯчейке"];
			string wikiText = topic.rawToWiki();
			string sample = "{|border=1\n" +
			"|-\n" +
			"|\n" +
			"ячейка 1\n" +
			"|\n" +
			"ячейка 2\n\n" +
			"ячейка 2\n\n" +
			"ячейка 2\n" +
			"|}";
			Assert.AreEqual(sample, wikiText);
		}
			
		[Test]
		public void ОбъектыCистемы()
		{
			Topic topic = project.topicList["Объектысистемы"];
			string wikiText = topic.rawToWiki();
			Console.Out.Write(wikiText);
		}

		[Test]
		public void Подразделы()
		{
			Topic topic = project.topicList["Подразделы"];
			string wikiText = topic.rawToWiki();
			string sample =
				"Введение.\n\n" +
				"==Подраздел 1==\n\n" +
				"Текст подраздела 1.\n\n" +
				"===Подраздел 2===\n\n" +
				"Текст подраздела 2.";

			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ПустойЗаголовок()
		{
			Topic topic = project.topicList["ПустойЗаголовок"];
			topic.rawToWiki();
		}

		[Test]
		public void СкобкиВЗаголовоке()
		{
			Topic topic = project.topicList["СкобкиВЗаголовке"];
			Assert.AreEqual("Скобки \"в заголовке\"", topic.Header);
		}
	
	}

	[TestFixture]
	public class TableOfContent
	{
		public string filename = Path.Combine(Utils.TestDataDir, "TableOfContents/Xml/TableOfContentst.xml");
		const string category = "Тесты синхронизации Help&Manual - TableOfContents";
		private const string prefix = "Тест HM TableOfContents: ";
		private HelpManualProject project;

		[SetUp]
		public void SetUp()
		{
			project = new HelpManualProject(filename, category, prefix);
		}


		[Test]
		public void Содержание()
		{
			Topic topic = project.topicList["TOC"];
			Assert.AreEqual("Содержание", topic.Header);
			string wikiText = topic.rawToWiki();
			string sample =
				"*[[Тест HM TableOfContents: Introduction|Introduction]]\n" +
				"**[[Тест HM TableOfContents: Welcome topic|Welcome topic]]\n" +
				"**[[Тест HM TableOfContents: Second topic|Second topic]]";
			Assert.AreEqual(sample, wikiText);
		}

	}
}
