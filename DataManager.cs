using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.Json;

namespace ProjectMediaPlayer
{
    public class DataManager
    {
        private DataManager() 
        { 
            CheckAndCreateDataFile();
        }
        private static DataManager instance;
        public static DataManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DataManager();
                }
                return instance;
            }
        }
        private const string DATA_FILE_NAME = "DataMediaPlayer.xml";
        private const string PREFIX_PLAYLIST = "PlayList_";
        public void CheckAndCreateDataFile()
        {
            if (!System.IO.File.Exists(DATA_FILE_NAME))
            {
                XDocument xDocument = new XDocument(
                                       new XDeclaration("1.0", "utf-8", "yes"), new XElement("Root",
                                                          new XElement("PlayLists"), new XElement("MaxID", 0)));
                xDocument.Save(DATA_FILE_NAME);
            }
            try {
                XDocument doc = XDocument.Load(DATA_FILE_NAME);
                doc.Root.Element("MaxID");
                doc.Root.Element("PlayLists");
            }
            catch(Exception e)
            {
                XDocument xDocument = new XDocument( new XDeclaration("1.0", "utf-8", "yes"), 
                    new XElement("Root", new XElement("PlayLists"), new XElement("MaxID", 0)));

                xDocument.Save(DATA_FILE_NAME);
            }
        }

        public List<PlayList> GetAllPlayList()
        {
            XDocument doc = XDocument.Load(DATA_FILE_NAME);
            List<PlayList> playLists = new List<PlayList>();
            if (doc == null)
            {
                XDocument xDocument = new XDocument(
                                       new XDeclaration("1.0", "utf-8", "yes"), new XElement("Root",
                                                          new XElement("PlayLists"), new XElement("MaxID", 0)));
               
                xDocument.Save(DATA_FILE_NAME);
                return playLists;
            }

            XElement data = doc.Root.Element("PlayLists");

            if(data == null)
            {
                doc.Root.Add(new XElement("PlayLists"));
                doc.Save(DATA_FILE_NAME);
                return playLists;
            }

            if (data.Elements().Count() == 0)
            {
                return playLists;
            }

            foreach (XElement element in data.Elements())
            {
                PlayList playList = JsonSerializer.Deserialize<PlayList>(element.Value);
                if(playList != null)
                {
                    playLists.Add(playList);
                }
            }

            return playLists;
        }

        public void AddPlayList(PlayList playList)
        {
            XDocument doc = XDocument.Load(DATA_FILE_NAME);
            XElement newPlayList = new XElement(PREFIX_PLAYLIST+playList.ID, JsonSerializer.Serialize(playList));
            if(doc == null || doc.Root.Element("PlayLists") == null)
            {
                XDocument xDocument = new XDocument( new XDeclaration("1.0", "utf-8", "yes"), 
                    new XElement("Root", new XElement("PlayLists"), new XElement("MaxID", 0)));
                xDocument.Root.Element("PlayLists").Add(newPlayList);
                xDocument.Save(DATA_FILE_NAME);
                doc = XDocument.Load(DATA_FILE_NAME);
            }
            
            doc.Root.Element("PlayLists").Add(newPlayList);
            doc.Root.Element("MaxID").Value = playList.ID;
            doc.Save(DATA_FILE_NAME);
        }

        public void RemovePlayList(PlayList playList)
        {
            XDocument doc = XDocument.Load(DATA_FILE_NAME);
            if (doc == null || doc.Root.Element("PlayLists") == null)
            {
                return;
            }

            XElement playLists = doc.Root.Element("PlayLists");
            if (playLists == null)
            {
                return;
            }

            XElement playListXml = playLists.Element(PREFIX_PLAYLIST + playList.ID);
            if (playListXml == null)
            {
                return;
            }

            playListXml.Remove();
            doc.Save(DATA_FILE_NAME);
        }

        public void UpdatePlayList(PlayList playList)
        {
            XDocument doc = XDocument.Load(DATA_FILE_NAME);
            if (doc == null || doc.Root.Element("PlayLists") == null)
            {
                return;
            }

            XElement playLists = doc.Root.Element("PlayLists");
            if (playLists == null)
            {
                return;
            }

            XElement playListElement = playLists.Element(PREFIX_PLAYLIST + playList.ID);
            if (playListElement == null)
            {
                return;
            }

            playListElement.Value = JsonSerializer.Serialize(playList);
            doc.Save(DATA_FILE_NAME);
        }

        public int GetMaxID()
        {
            XDocument doc = XDocument.Load(DATA_FILE_NAME);
            if (doc == null)
            {
                XDocument xDocument = new XDocument(
                                       new XDeclaration("1.0", "utf-8", "yes"), new XElement("Root",
                                                          new XElement("PlayLists"), new XElement("MaxID", 0)));
                xDocument.Save(DATA_FILE_NAME);
                return 0;
            }

            XElement maxID = doc.Root.Element("MaxID");
            if (maxID == null)
            {
                XElement xElement = new XElement("MaxID", 0);
                doc.Root.Add(xElement);
                return 0;
            }

            return int.Parse(maxID.Value);
        }

        public void UpdateMaxID(int maxID)
        {
            XDocument doc = XDocument.Load(DATA_FILE_NAME);
            if (doc == null)
            {
                XDocument xDocument = new XDocument(
                                       new XDeclaration("1.0", "utf-8", "yes"), new XElement("Root",
                                                          new XElement("PlayLists"), new XElement("MaxID", 0)));
                xDocument.Save(DATA_FILE_NAME);
            }

            XElement maxIDElement = doc.Root.Element("MaxID");
            if (maxIDElement == null)
            {
                XElement xElement = new XElement("MaxID", maxID);
                doc.Root.Add(xElement);
                return;
            }

            maxIDElement.Value = maxID.ToString();
            doc.Save(DATA_FILE_NAME);
        }

        public string LastPlayListID()
        {
            XDocument doc = XDocument.Load(DATA_FILE_NAME);
            if (doc == null)
            {
                XDocument xDocument = new XDocument( new XDeclaration("1.0", "utf-8", "yes"), new XElement("Root",
                    new XElement("PlayLists"), new XElement("MaxID", 0), new XElement("LastPlayListId", -1)));
                xDocument.Save(DATA_FILE_NAME);
                return "0";
            }

            XElement lastPlayListId = doc.Root.Element("LastPlayListId");
            if (lastPlayListId == null)
            {
                XElement xElement = new XElement("LastPlayListId", -1);
                doc.Root.Add(xElement);
                return "0";
            }
            
            return lastPlayListId.Value;
        }

        public void UpdateLastPlayListID(string lastPlayListId)
        {
            XDocument doc = XDocument.Load(DATA_FILE_NAME);
            if (doc == null)
            {
                XDocument xDocument = new XDocument( new XDeclaration("1.0", "utf-8", "yes"), new XElement("Root",
                                       new XElement("PlayLists"), new XElement("MaxID", 0), new XElement("LastPlayListId", -1)));
                xDocument.Save(DATA_FILE_NAME);
            }

            XElement lastPlayListIdElement = doc.Root.Element("LastPlayListId");
            if (lastPlayListIdElement == null)
            {
                XElement xElement = new XElement("LastPlayListId", lastPlayListId);
                doc.Root.Add(xElement);
                doc.Save(DATA_FILE_NAME);
                return;
            }

            lastPlayListIdElement.Value = lastPlayListId;
            doc.Save(DATA_FILE_NAME);
        }

        public string LastSongFileName()
        {
            XDocument doc = XDocument.Load(DATA_FILE_NAME);
            if (doc == null)
            {
                XDocument xDocument = new XDocument( new XDeclaration("1.0", "utf-8", "yes"), new XElement("Root",
                                       new XElement("PlayLists"), new XElement("MaxID", 0), new XElement("LastPlayListId", -1), new XElement("LastSongFileName", "")));
                xDocument.Save(DATA_FILE_NAME);
                return "";
            }

            XElement lastSongFileName = doc.Root.Element("LastSongFileName");
            if (lastSongFileName == null)
            {
                XElement xElement = new XElement("LastSongFileName", "");
                doc.Root.Add(xElement);
                return "";
            }
            
            return lastSongFileName.Value;
        }

        public void UpdateLastSongFileName(string lastSongFileName)
        {
            XDocument doc = XDocument.Load(DATA_FILE_NAME);
            if (doc == null)
            {
                XDocument xDocument = new XDocument( new XDeclaration("1.0", "utf-8", "yes"), new XElement("Root",
                                                          new XElement("PlayLists"), new XElement("MaxID", 0), new XElement("LastPlayListId", -1), new XElement("LastSongFileName", "")));
                xDocument.Save(DATA_FILE_NAME);
            }

            XElement lastSongFileNameElement = doc.Root.Element("LastSongFileName");
            if (lastSongFileNameElement == null)
            {
                XElement xElement = new XElement("LastSongFileName", lastSongFileName);
                doc.Root.Add(xElement);
                doc.Save(DATA_FILE_NAME);
                return;
            }

            lastSongFileNameElement.Value = lastSongFileName;
            doc.Save(DATA_FILE_NAME);
        }

        public double LastSongPosition()
        {
            XDocument doc = XDocument.Load(DATA_FILE_NAME);
            if (doc == null)
            {
                XDocument xDocument = new XDocument( new XDeclaration("1.0", "utf-8", "yes"), new XElement("Root",
                                                          new XElement("PlayLists"), new XElement("MaxID", 0), new XElement("LastPlayListId", -1), new XElement("LastSongFileName", ""), new XElement("LastSongPosition", 0)));
                xDocument.Save(DATA_FILE_NAME);
                return 0;
            }

            XElement lastSongPosition = doc.Root.Element("LastSongPosition");
            if (lastSongPosition == null)
            {
                XElement xElement = new XElement("LastSongPosition", 0);
                doc.Root.Add(xElement);
                return 0;
            }
            
            return double.Parse(lastSongPosition.Value);
        }

        public void UpdateLastSongPosition(double lastSongPosition)
        {
            XDocument doc = XDocument.Load(DATA_FILE_NAME);
            if (doc == null)
            {
                XDocument xDocument = new XDocument( new XDeclaration("1.0", "utf-8", "yes"), new XElement("Root",
                                                                             new XElement("PlayLists"), new XElement("MaxID", 0), new XElement("LastPlayListId", -1), new XElement("LastSongFileName", ""), new XElement("LastSongPosition", 0)));
                xDocument.Save(DATA_FILE_NAME);
            }

            XElement lastSongPositionElement = doc.Root.Element("LastSongPosition");
            if (lastSongPositionElement == null)
            {
                XElement xElement = new XElement("LastSongPosition", lastSongPosition);
                doc.Root.Add(xElement);
                doc.Save(DATA_FILE_NAME);
                return;
            }

            lastSongPositionElement.Value = lastSongPosition.ToString();
            doc.Save(DATA_FILE_NAME);
        }
    }
}
