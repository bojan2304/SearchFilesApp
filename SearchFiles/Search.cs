using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SearchFiles
{
    public class FoundInfoEvent
    {
        private readonly FileSystemInfo _info;

        // Constructor

        public FoundInfoEvent(FileSystemInfo info)
        {
            _info = info;
        }

        // Propertie

        public FileSystemInfo Info
        {
            get { return _info; }
        }
    }

    class SearchParams
    {
        private readonly string _searchDir;
        private readonly bool _subDirsChecked;
        private readonly List<string> _fileNames;
        private readonly string _searchText;
        private readonly Encoding _encoding;


        // Constructor

        public SearchParams(string searchDir, bool subDirsChecked, List<string> fileNames, string containingText, Encoding encoding)
        {
            _searchDir = searchDir;
            _subDirsChecked = subDirsChecked;
            _fileNames = fileNames;
            _searchText = containingText;
            _encoding = encoding;
        }


        // Properties

        public string SearchDir
        {
            get { return _searchDir; }
        }

        public bool SubDirsChecked
        {
            get { return _subDirsChecked; }
        }

        public List<string> FileNames
        {
            get { return _fileNames; }
        }

        public string ContainingText
        {
            get { return _searchText; }
        }

        public Encoding Encoding
        {
            get { return _encoding; }
        }
    }

    class Search
    {
        private static Thread _thread = null;
        private static bool _stop = false;
        private static SearchParams _pars = null;
        private static byte[] _containBytes = null;

        // Asynchronous Events
        public delegate void FoundInfoEventHandler(FoundInfoEvent e);
        public static event FoundInfoEventHandler FoundInfo;

        public delegate void ThreadEndedEventHandler(ThreadEndedEvent e);
        public static event ThreadEndedEventHandler ThreadEnded;

        // Methods
        public static bool Start(SearchParams pars)
        {
            bool success = false;

            if (_thread == null)
            {
                ResetVariables();

                // Remember parameters
                _pars = pars;

                // Start searching for FileSystemInfos that match the parameters
                _thread = new Thread(new ThreadStart(SearchThread));
                _thread.Start();

                success = true;
            }

            return success;
        }

        public static void Stop()
        {
            // Stop the thread by setting a flag
            _stop = true;
        }


        // Methods

        private static void ResetVariables()
        {
            _thread = null;
            _stop = false;
            _pars = null;
            _containBytes = null;
        }

        private static void SearchThread()
        {
            bool success = true;
            string errorMsg = "";

            // Search for FileSystemInfos that match the parameters
            if ((_pars.SearchDir.Length >= 3) && (Directory.Exists(_pars.SearchDir)))
            {
                // Convert string to search for into bytes if necessary
                if (_pars.ContainingText != "")
                {
                    _containBytes = _pars.Encoding.GetBytes(_pars.ContainingText);
                }
                else
                {
                    success = false;
                    errorMsg = "Search field can not be empty.";
                }

                if (success)
                {
                    // Get the directory info for the search directory
                    DirectoryInfo dirInfo = new DirectoryInfo(_pars.SearchDir);

                    SearchDirectory(dirInfo);
                }
            }
            else
            {
                success = false;
                errorMsg = "The directory\r\n" + _pars.SearchDir + "\r\ndoes not exist.";
            }

            // Remember the thread has ended
            _thread = null;

            // Raise an event
            if (ThreadEnded != null)
            {
                ThreadEnded(new ThreadEndedEvent(success, errorMsg));
            }
        }

        private static void SearchDirectory(DirectoryInfo dirInfo)
        {
            if (!_stop)
            {
                try
                {
                    foreach (string fileName in _pars.FileNames)
                    {
                        FileSystemInfo[] infos = dirInfo.GetFileSystemInfos(fileName);

                        foreach (FileSystemInfo info in infos)
                        {
                            if (_stop)
                            {
                                break;
                            }

                            if (MatchesRestrictions(info))
                            {
                                if (FoundInfo != null)
                                {
                                    FoundInfo(new FoundInfoEvent(info));
                                }
                            }
                        }
                    }

                    if (_pars.SubDirsChecked)
                    {
                        DirectoryInfo[] subDirInfos = dirInfo.GetDirectories();
                        foreach (DirectoryInfo info in subDirInfos)
                        {
                            if (_stop)
                            {
                                break;
                            }

                            SearchDirectory(info);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private static bool MatchesRestrictions(FileSystemInfo info)
        {
            bool matches = false;

            if (info is FileInfo)
            {
                matches = FileContainsBytes(info.FullName, _containBytes);
            }

            return matches;
        }

        private static bool FileContainsBytes(string path, byte[] compare)
        {
            bool contains = false;

            int blockSize = 4096;
            if ((compare.Length >= 1) && (compare.Length <= blockSize))
            {
                byte[] block = new byte[compare.Length - 1 + blockSize];

                try
                {
                    FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                    int bytesRead = fs.Read(block, 0, block.Length);

                    do
                    {
                        int endPos = bytesRead - compare.Length + 1;
                        for (int i = 0; i < endPos; i++)
                        {
                            int j;
                            for (j = 0; j < compare.Length; j++)
                            {
                                if (block[i + j] != compare[j])
                                {
                                    break;
                                }
                            }

                            if (j == compare.Length)
                            {
                                contains = true;
                                break;
                            }
                        }

                        if (contains || (fs.Position >= fs.Length))
                        {
                            break;
                        }
                        else
                        {
                            for (int i = 0; i < (compare.Length - 1); i++)
                            {
                                block[i] = block[blockSize + i];
                            }

                            bytesRead = compare.Length - 1 + fs.Read(block, compare.Length - 1, blockSize);
                        }
                    }
                    while (!_stop);

                    fs.Close();
                }
                catch (Exception)
                {
                }
            }

            return contains;
        }
    }
}
