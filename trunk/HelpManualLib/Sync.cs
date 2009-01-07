using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Xml;
using DotNetWikiBot;

namespace HelpManualLib
{
	public class HelpManualProject
	{
		public XmlDocument xmlDoc;
		public readonly TopicList topicList;
		public TopicList topicHeaderList;
		private readonly string prefix;
		private readonly string category;
		private readonly string directory;

		public string Prefix
		{
			get { return prefix; }
		}

		public string Category
		{
			get { return category; }
		}

		private string ImagePrefix
		{
			get { return prefix.Replace(":", "-").Replace(" ", "_");  }
		}

		public HelpManualProject(string filename, string category, string prefix)
		{
			xmlDoc = new XmlDocument();
			xmlDoc.Load(filename);
			topicList = new TopicList();
			topicHeaderList = new TopicList();
			this.prefix = prefix;
			this.category = category;
			directory = Path.GetDirectoryName(filename);

			addTOCTopic();
			fillTopicList();
		}

		private void addTOCTopic()
		{
			XmlNode node = xmlDoc.SelectSingleNode("/helpproject/map");
			Topic topic = new TOCTopic(this, node);
			topicList.Add(topic.ID, topic);
			topicHeaderList.Add(topic.Header, topic);
		}

		private void fillTopicList()
		{
			XmlNodeList nodes = xmlDoc.SelectNodes("/helpproject/topics/topic");
			foreach (XmlNode node in nodes)
			{
				Topic topic = new Topic(this, node);
				topicList.Add(topic.ID, topic);
				topicHeaderList.Add(topic.Header, topic);
			}
		}

		public void Sync(Site site)
		{
			Sync(site, true, true);
		}

		public void Sync(Site site, bool doImages, bool doPages)
		{
			if (!doImages && !doPages)
			{
				doImages = true;
				doPages = true;
			}
			PageList pages = WikiPubLib.Sync.GetCategoryList(site, category);
			if (doPages)
			{
				syncNewPages(site, pages);
				syncDeletedPages(pages);
				syncExistingPages(site, pages);
			}
			if (doImages)
			{
				syncImages(site);
			}
		}

		private void syncImages(Site site)
		{
			string publicTempFile = Path.GetTempFileName();
			foreach (Topic topic  in topicList.Values)
			{
				string[] images = topic.GetImages();
				foreach (string imageName in images)
				{
					string localFileName = GetImageFileName(imageName);
					if (!File.Exists(localFileName))
						continue;

					Page publicImage = new Page(site, AddImagePrefix(imageName));
					if (publicImage.TryDownloadImage(publicTempFile))
					{
						if (WikiPubLib.Sync.FilesAreIdentical(localFileName, publicTempFile))
							continue;
					}
					publicImage.UploadImage(localFileName, "", "", "", "");
				}
			}
			File.Delete(publicTempFile);
		}

		public string GetImageFileName(string name)
		{
			string filename = null;
			if (isBmpImage(name))
				filename = makePngFromBmp(name);
			else
				filename = Path.GetFullPath(Path.Combine(directory + "/Images", name));

			string nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
			if (nameWithoutExtension.Length < 3)
			{
				string path = Path.GetDirectoryName(filename);
				string extention = Path.GetExtension(filename);
				string newfilename =
					Path.Combine(path, nameWithoutExtension + nameWithoutExtension + nameWithoutExtension + extention);
				File.Copy(filename, newfilename, true);
				filename = newfilename;
			}
			return filename;
		}

		private bool isBmpImage(string name)
		{
			return name.EndsWith(".bmp", true, CultureInfo.CurrentCulture);
		}



		private string makePngFromBmp(string bmpName)
		{
			string pngName = makePngName(bmpName);
			string bmpFullName = Path.GetFullPath(Path.Combine(directory + "/Images", bmpName));
			string pngFullName = Path.GetFullPath(Path.Combine(directory + "/Images", pngName));

			if (File.Exists(bmpFullName))
			{
				Bitmap bmpImage = new Bitmap(bmpFullName);
				bmpImage.Save(pngFullName, ImageFormat.Png);
			}
			return pngFullName;
		}

		private string makePngName(string bmpName)
		{
			return bmpName.Substring(0, bmpName.Length - 3) + "png";
		}

		private void syncExistingPages(Site site, PageList pages)
		{
			foreach (Page page in pages)
			{
				string topicName = RemovePrefix(page.title);
				if (!topicHeaderList.ContainsKey(topicName))
					continue;
				Topic topic = topicHeaderList[topicName];
				page.LoadSilent();
				if (page.text != topic.ToWiki())
					copyTopic(topic, site);
			}
		}

		private void syncDeletedPages(PageList pages)
		{
			foreach (Page page in pages)
			{
				if (topicHeaderList.ContainsKey(RemovePrefix(page.title)))
					continue;
				page.Delete("wiki synchronization");
			}
		}

		private void syncNewPages(Site site, PageList pages)
		{
			foreach (Topic topic in topicList.Values)
			{
				if (pages.Contains(topic.Name))
					continue;
				copyTopic(topic, site);
			}
		}


		public string AddPrefix(string name)
		{
			return Prefix + name;
		}

		public string AddImagePrefix(string name)
		{
			if (isBmpImage(name))
				name = makePngName(name);
			return ImagePrefix + name;
		}

		public string RemovePrefix(string name)
		{
			if (name.StartsWith(prefix))
				return name.Substring(prefix.Length);
			else
				return "";
		}

		private void copyTopic(Topic topic, Site site)
		{
			Page page = new Page(site);
			page.title = topic.Name;
			page.text = topic.ToWiki();
			page.Save();
		}

	}

	public class TOCTopic : Topic
	{
		public TOCTopic(HelpManualProject project, XmlNode map): base(project)
		{
			topicNode = map;
			IDString = "TOC";
			header = "Содержание";
		}

		public override string rawToWiki()
		{
			StringWriter buffer = new StringWriter();
			XmlNodeList nodes = Node.SelectNodes("topicref");
			foreach(XmlNode node in nodes)
				topicrefNodeToWiki(node, buffer, 1);
			string result = buffer.ToString();
			return result.EndsWith("\n") ? result.Substring(0, result.Length - 1) : result;
		}

		private void topicrefNodeToWiki(XmlNode node, StringWriter buffer, int level)
		{
			if (node.Name != "topicref")
				return;

			string href = node.Attributes["href"].Value;
			for(int i=0; i < level; i++)
				buffer.Write("*");

			writeTopicLink(node, href, buffer);
			buffer.Write("\n");

			foreach (XmlNode chieldNode in node.ChildNodes)
				topicrefNodeToWiki(chieldNode, buffer, level + 1);

		}
	}

	public class TopicList: Dictionary<string, Topic>
	{
		public TopicList():base(StringComparer.CurrentCultureIgnoreCase )
		{
		}
	}

	public class Topic
	{
		protected string IDString;
		protected string header;
		protected XmlNode topicNode;
		public readonly HelpManualProject Project;
		
		protected Topic(HelpManualProject project)
		{
			Project = project;
		}

		public string ID
		{
			get { return IDString; }
		}

		public XmlNode Node
		{
			get { return topicNode; }
		}


		public Topic(HelpManualProject project, XmlNode node)
		{
			Project = project;
			topicNode = node;
			IDString = node.Attributes["id"].Value;
			makeHeader();
		}

		private void makeHeader()
		{
			XmlNode headerNode = Node.SelectSingleNode("body/header/para/text");

			if (headerNode == null)
				header = ID;
			else
				header = headerNode.InnerText;

			char[] invalidChars = {'<', '>', '{', '}', '[', ']'};

			foreach(char ch in invalidChars)
				header = header.Replace(ch, '"');

			while (Project.topicHeaderList.ContainsKey(header))
				header += "*";
		}

		public string Header
		{
			get { return header; }
		}

		public string Name
		{
			get
			{
				return Project.AddPrefix(Header);
			}
		}

		public string ToWiki()
		{
			return String.Format("[[Категория:{0}]]\n{1}", Project.Category, rawToWiki());
		}


		public virtual string rawToWiki()
		{
			StringWriter outBuffer = new StringWriter();
			XmlNode body = Node.SelectSingleNode("body");
			bodyNodeToWiki(body, outBuffer);
			string result = outBuffer.ToString();
			return result.EndsWith("\n") ? result.Substring(0, result.Length - 1) : result;
		}

		public string[] GetImages()
		{
			XmlNodeList nodes = Node.SelectNodes("descendant::image");
			List<string> imageList = new List<string>();
			foreach (XmlNode node in nodes)
				imageList.Add(node.Attributes["src"].Value);
			return imageList.ToArray();
		}

		private void bodyNodeToWiki(XmlNode body, TextWriter outBuffer)
		{
			bool needNewLine = false;
			foreach (XmlNode node in body.ChildNodes)
			{
				switch (node.Name)
				{
					case "para":
						break;
					default:
						needNewLine = false;
						break;
				}

				switch(node.Name)
				{
					case "header":
						break;
					case "para":
						if (needNewLine)
							outBuffer.Write("\n");
						paraNodeToWiki(node, outBuffer);
						needNewLine = true;
						break;
					case "list":
						listNodeToWiki(node, outBuffer, "");
						break;
				}
			}
		}

		private void tableNodeToWiki(XmlNode table, TextWriter buffer)
		{
			XmlNodeList trNodes = table.SelectNodes("tr");
			buffer.Write("{|border=1\n");
			foreach (XmlNode trNode in trNodes)
			{
				buffer.Write("|-\n");
				foreach (XmlNode tdNode in trNode.ChildNodes)
				{
					tdNodeToWiki(tdNode, buffer);
				}
			}
			buffer.Write("|}");
		}

		private void tdNodeToWiki(XmlNode tdNode, TextWriter buffer)
		{
			buffer.Write("|");
			XmlNode paraNode = tdNode.SelectSingleNode("para");
			if (paraNode != null)
			{
				string align = getAlignStyle(paraNode);
				if (align != null)
					buffer.Write("align=\"{0}\"|", align);
			}
			buffer.Write("\n");
			bodyNodeToWiki(tdNode, buffer);
		}

		private string getAlignStyle(XmlNode node)
		{
			XmlAttribute styleAttribute = node.Attributes["style"];
			if (styleAttribute == null)
				return null;
			string[] styles = styleAttribute.Value.Split(';');
			foreach (string s in styles)
			{
				string style = s.Trim();
				if (style.StartsWith("text-align:"))
					return style.Substring("text-align:".Length);
			}
			return null;
		}

		private void listNodeToWiki(XmlNode list, TextWriter buffer, string upWikiType)
		{
			string type = list.Attributes["type"].Value;
			string wikiType = upWikiType + (type == "ol" ? "#": "*");

			foreach (XmlNode node in list.ChildNodes)
			{
				listElementToWiki(node, buffer, wikiType);
			}
		}

		private void listElementToWiki(XmlNode node, TextWriter buffer, string wikiType)
		{
			switch(node.Name)
			{
				case "li":
					buffer.Write("{0}", wikiType);
					paraNodeToWiki(node, buffer);
					break;
				case "list":
					listNodeToWiki(node, buffer, wikiType);
					break;
			}
		}

		private void imageNodeToWiki(XmlNode body, TextWriter buffer)
		{
			string name = body.Attributes["src"].Value;

			buffer.Write("[[Изображение:{0}]]", Project.AddImagePrefix(name));
		}

		private void paraNodeToWiki(XmlNode body, TextWriter buffer)
		{
			int headerLevel = paraHeaderLevel(body);
			wikiHeader(headerLevel, buffer);

			foreach (XmlNode node in body.ChildNodes)
					paraElementToWiki(node, buffer);

			wikiHeader(headerLevel, buffer);
			
			buffer.Write("\n");
		}

		private int paraHeaderLevel(XmlNode node)
		{
			XmlAttribute attribute = node.Attributes["styleclass"];
			bool isParaHeader = false;
			bool isTextHeader = false;
			
			if (attribute != null && attribute.Value != "Normal")
				isParaHeader = true;

			XmlNode textNode = node.SelectSingleNode("text");
			if (textNode != null)
			{
				attribute = textNode.Attributes["styleclass"];
				if (attribute != null && attribute.Value != "Normal")
					isTextHeader = true;
			}

			if (!isParaHeader && !isTextHeader)
				return 0;
			else if (isParaHeader && isTextHeader)
				return 2;
			else
				return 3;
		}

		private void wikiHeader(int headerLevel, TextWriter buffer)
		{
			for (int i = 0; i < headerLevel; i++ )
				buffer.Write("=");
		}

		private void paraElementToWiki(XmlNode node, TextWriter buffer)
		{
			switch (node.Name)
			{
				case "text":
					textNodeToWiki(node, buffer);
					break;
				case "image":
					imageNodeToWiki(node, buffer);
					break;
				case "link":
					linkNodeToWiki(node, buffer);
					break;
				case "table":
					tableNodeToWiki(node, buffer);
					break;
			}
		}

		private void linkNodeToWiki(XmlNode node, TextWriter buffer)
		{
			string href = node.Attributes["href"].Value;
			string type = node.Attributes["type"].Value;
			switch(type)
			{
				case "topiclink":
					writeTopicLink(node, href, buffer);
					break;
				case "weblink":
					buffer.Write("[{0} {1}]", href, node.InnerText);
					break;
				default:
					buffer.Write("{0}", node.InnerText);
					break;
			}

		}

		protected void writeTopicLink(XmlNode node, string href, TextWriter buffer)
		{
			if (Project.topicList.ContainsKey(href))
			{
				Topic topic = Project.topicList[href];
				buffer.Write("[[{0}|{1}]]", topic.Name, topic.Header);
			}
			else
				buffer.Write("[[{0}{1}]]", Project.Prefix, node.InnerText);
		}

		private void textNodeToWiki(XmlNode node, TextWriter buffer)
		{
			XmlAttribute attribute = node.Attributes["style"];
			if (attribute != null)
			{
				string[] styles = attribute.Value.Split(';');
				string text = buildStyleText(node.InnerText, styles, 0);
				buffer.Write("{0}", text);
				return;
			}
			buffer.Write("{0}", node.InnerText);
		}

		private string buildStyleText(string text, string[] styles, int pos)
		{
			if (pos >= styles.Length)
				return text;
			string style = styles[pos];
			StringWriter buffer = new StringWriter();
			switch (style.Trim())
			{
				case "font-style:italic":
					buffer.Write("''{0}''", text);
					break;
				case "font-weight:bold":
					buffer.Write("'''{0}'''", text);
					break;
				case "text-decoration:underline":
					buffer.Write("<u>{0}</u>", text);
					break;
				default:
					buffer.Write("{0}", text);
					break;
			}
			return buildStyleText(buffer.ToString(), styles, pos + 1);
		}



	}
}
