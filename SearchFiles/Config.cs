using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SearchFiles
{
    public class Config
    {
        private string _searchDir;
        private bool _subDirsChecked = true;
        private string _searchText = "";
        private static readonly Config _configData = new Config();


        // Properties

        public string SearchDir
        {
            get { return _searchDir; }
            set { _searchDir = value; }
        }

        public bool SubDirsChecked
        {
            get { return _subDirsChecked; }
            set { _subDirsChecked = value; }
        }

        public string SearchText
        {
            get { return _searchText; }
            set { _searchText = value; }
        }

        public static Config Data
        {
            get { return _configData; }
        }
    }
}
