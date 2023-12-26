using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectMediaPlayer
{
    public class PlayList : INotifyPropertyChanged
    {
        private List<string> listFileName;
        private string name;
        private int id;
        public PlayList()
        {
            listFileName = new List<string>();
            name = "New PlayList";
            id = DataManager.Instance.GetMaxID() + 1;
        }

        public PlayList(List<string> listFileName)
        {
            this.listFileName = listFileName;
            name = "New PlayList";
            id = DataManager.Instance.GetMaxID() + 1;
        }

        public PlayList(List<string> listFileName, string name)
        {
            this.listFileName = listFileName;
            this.name = name;
            id = DataManager.Instance.GetMaxID() + 1;
        }

        public List<string> ListFileName
        {
            get { return listFileName; }
            set { listFileName = value; }
        }

        public string ID { get => id.ToString(); set => id = int.Parse(value); }
        public string Name { get => name; set => name = value; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Add(string fileName)
        {
            listFileName.Add(fileName);
        }

        public void Remove(string fileName)
        {
            listFileName.Remove(fileName);
        }

        public void RemoveAt(int index)
        {
            listFileName.RemoveAt(index);
        }

    }
}
