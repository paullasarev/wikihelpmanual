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
			newpage.text = String.Format("[[���������: {0}]]\n{1}\n", category, text);
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
		const string category = "����� ������������� Help&Manual";
		private const string prefix = "���� HM: ";
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

			string id = "��������������";
			Topic topic = topicList[id];
			Assert.IsNotNull(topic);
			Assert.AreEqual(id, topic.ID);
		}

		[Test]
		public void TopicHeader()
		{
			TopicList topicList = project.topicList;

			string id = "��������������";
			string header = "������� �������";
			Topic topic = topicList[id];
			//Utils.PrintXmlTree(topic.Node, Console.Out);
			Assert.AreEqual(header, topic.Header);
		}

		[Test]
		public void TopicNode()
		{
			TopicList topicList = project.topicList;

			string id = "��������������";
			Topic topic = topicList[id];
			Assert.IsInstanceOfType(typeof(XmlNode), topic.Node);
		}

		[Test]
		public void SimpleText2Wiki()
		{
			Topic topic = project.topicList["SimpleText"];
			string wikiText = topic.rawToWiki();
			string sample = "��� ��, ������� �����.";
			Assert.AreEqual(sample, wikiText);
			
		}

		[Test]
		public void ������������2Wiki()
		{
			Topic topic = project.topicList["������������"];
			string wikiText = topic.rawToWiki();
			string sample = "������ ��������\n\n������ ��������";
			Assert.AreEqual(sample, wikiText);

		}

		[Test]
		public void ImagePng()
		{
			Topic topic = project.topicList["�����������_PNG"];
			string wikiText = topic.rawToWiki();
			string sample = "[[�����������:����_HM-_VistaRunAsAdmin.png]]simple text.";
			Assert.AreEqual(sample, wikiText);

		}

		[Test]
		public void ImageBMP()
		{
			Topic topic = project.topicList["�����������BMP"];
			string wikiText = topic.rawToWiki();
			string sample = "[[�����������:����_HM-_0001.png]]";
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
			Topic topic = project.topicList["�����������_PNG"];
			string[] images = topic.GetImages();
			string[] etalon = {"VistaRunAsAdmin.png"};
			Assert.AreEqual(etalon, images);
		}

		[Test]
		public void GetImagesTwoImages()
		{
			Topic topic = project.topicList["���_�����������_PNG"];
			string[] images = topic.GetImages();
			string[] etalon = { "button_main.gif", "button_next.gif" };
			Assert.AreEqual(etalon, images);
		}

		[Test]
		public void GetImagesBMP()
		{
			Topic topic = project.topicList["�����������BMP"];
			string[] images = topic.GetImages();
			string[] etalon = { "0001.bmp" };
			Assert.AreEqual(etalon, images);
		}

		[Test]
		public void AddImagePrefixBMP()
		{
			string wikiImageName = project.AddImagePrefix("0001.bmp");
			Assert.AreEqual("����_HM-_0001.png", wikiImageName);
		}

		[Test]
		public void AddImagePrefixPNG()
		{
			string wikiImageName = project.AddImagePrefix("0001.png");
			Assert.AreEqual("����_HM-_0001.png", wikiImageName);
		}
	}


	[Explicit]
	[TestFixture]
	public class WikiSyncTests
	{
		private Site localSite;
		const string category = "����� ������������� Help&Manual";
		private const string prefix = "���� HM: ";
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
			string pageName = "���� HM: SimpleText";
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
			string imageName = "�����������:����_HM-_VistaRunAsAdmin.png";

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
			string imageName = "�����������:����_HM-_0001.png";

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
		const string category = "����� ������������� Help&Manual - PageProperties";
		private const string prefix = "���� HM PageProperties: ";
		private HelpManualProject project;

		[SetUp]
		public void SetUp()
		{
			project = new HelpManualProject(filename, category, prefix);
		}


		[Test]
		public void ������()
		{
			Topic topic = project.topicList["������"];
			string wikiText = topic.rawToWiki();
			string sample = "��������� ������ ''��������''.";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ������()
		{
			Topic topic = project.topicList["������"];
			string wikiText = topic.rawToWiki();
			string sample = String.Format("����� ���� ������ ������.[[{0}������|������]] ��� ���.", prefix);
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void �������������()
		{
			Topic topic = project.topicList["�������������"];
			string wikiText = topic.rawToWiki();
			string sample = String.Format("��� ������� ������ [http://www.kint.ru ����]");
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ������()
		{
			Topic topic = project.topicList["������"];
			string wikiText = topic.rawToWiki();
			string sample = "��� '''������''' �����.";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void �������������()
		{
			Topic topic = project.topicList["�������������"];
			string wikiText = topic.rawToWiki();
			string sample = "��� <u>�������������</u>.";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ������������()
		{
			Topic topic = project.topicList["������������"];
			string wikiText = topic.rawToWiki();
			string sample = "��� '''''������ ������'''''";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void �������������()
		{
			Topic topic = project.topicList["�������������"];
			string wikiText = topic.rawToWiki();
			string sample = "��� ������� ������:\n*����� 1\n*����� 2\n*����� 3";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ������������������()
		{
			Topic topic = project.topicList["������������������"];
			string wikiText = topic.rawToWiki();
			string sample = "��� ������������ ������:\n#����� 1\n#����� 2\n#����� 3";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ���������������()
		{
			Topic topic = project.topicList["���������������"];
			string wikiText = topic.rawToWiki();
			string sample =
				"��������� ������:\n" +
				"*����� 1\n" +
				"**����� 1.1\n" +
				"**����� 1.2\n" +
				"*����� 2";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ���������������������������()
		{
			Topic topic = project.topicList["���������������������������"];
			string wikiText = topic.rawToWiki();
			string sample =
				"��������� ������������ ������:\n" +
				"#����� 1\n" +
				"##����� 1.1\n" +
				"##����� 1.2\n" +
				"#����� 2";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ����������������()
		{
			Topic topic = project.topicList["����������������"];
			string wikiText = topic.rawToWiki();
			string sample = "������:\n*����� � '''����������'''";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ��������������()
		{
			Topic topic = project.topicList["��������������"];
			string wikiText = topic.rawToWiki();
			string sample = "{|border=1\n" +
			"|-\n" +
			"|\n������ 1.1\n" +
			"|\n������ 1.2\n" +
			"|\n������ 1.3\n" +
			"|-\n" +
			"|\n������ 2.1\n" +
			"|\n������ 2.2\n" +
			"|\n������ 2.3\n" +
			"|}";
			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void �������������������������()
		{
			Topic topic = project.topicList["�������������������������"];
			string wikiText = topic.rawToWiki();
			string sample = "{|border=1\n" +
			"|-\n" +
			"|align=\"center\"|\n������ 1.1\n" +
			"|align=\"center\"|\n������ 1.2\n" +
			"|\n������ 1.3\n" +
			"|-\n" +
			"|align=\"right\"|\n������ 2.1\n" +
			"|align=\"right\"|\n������ 2.2\n" +
			"|\n������ 2.3\n" +
			"|}";
			Assert.AreEqual(sample, wikiText);
		}
		
		[Test]
		public void ��������������������������()
		{
			Topic topic = project.topicList["��������������������������"];
			string wikiText = topic.rawToWiki();
			string sample = "{|border=1\n" +
			"|-\n" +
			"|\n" +
			"������ 1\n" +
			"|\n" +
			"������ 2\n\n" +
			"������ 2\n\n" +
			"������ 2\n" +
			"|}";
			Assert.AreEqual(sample, wikiText);
		}
			
		[Test]
		public void �������C������()
		{
			Topic topic = project.topicList["��������������"];
			string wikiText = topic.rawToWiki();
			Console.Out.Write(wikiText);
		}

		[Test]
		public void ����������()
		{
			Topic topic = project.topicList["����������"];
			string wikiText = topic.rawToWiki();
			string sample =
				"��������.\n\n" +
				"==��������� 1==\n\n" +
				"����� ���������� 1.\n\n" +
				"===��������� 2===\n\n" +
				"����� ���������� 2.";

			Assert.AreEqual(sample, wikiText);
		}

		[Test]
		public void ���������������()
		{
			Topic topic = project.topicList["���������������"];
			topic.rawToWiki();
		}

		[Test]
		public void �����������������()
		{
			Topic topic = project.topicList["����������������"];
			Assert.AreEqual("������ \"� ���������\"", topic.Header);
		}
	
	}

	[TestFixture]
	public class TableOfContent
	{
		public string filename = Path.Combine(Utils.TestDataDir, "TableOfContents/Xml/TableOfContentst.xml");
		const string category = "����� ������������� Help&Manual - TableOfContents";
		private const string prefix = "���� HM TableOfContents: ";
		private HelpManualProject project;

		[SetUp]
		public void SetUp()
		{
			project = new HelpManualProject(filename, category, prefix);
		}


		[Test]
		public void ����������()
		{
			Topic topic = project.topicList["TOC"];
			Assert.AreEqual("����������", topic.Header);
			string wikiText = topic.rawToWiki();
			string sample =
				"*[[���� HM TableOfContents: Introduction|Introduction]]\n" +
				"**[[���� HM TableOfContents: Welcome topic|Welcome topic]]\n" +
				"**[[���� HM TableOfContents: Second topic|Second topic]]";
			Assert.AreEqual(sample, wikiText);
		}

	}
}
