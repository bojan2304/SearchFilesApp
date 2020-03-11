using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SearchFiles
{
    public partial class MainForm : Form
    {
        private bool _closing = false;

        // Synchronizing Delegates

        private delegate void FoundInfoHandler(FoundInfoEvent e);
        private FoundInfoHandler FoundInfo;

        private delegate void ThreadEndedHandler(ThreadEndedEvent e);
        private ThreadEndedHandler ThreadEnded;


        // Constructor

        public MainForm()
        {
            InitializeComponent();
        }

        private void SearchDirButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                SelectedPath = dirTextBox.Text
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                dirTextBox.Text = dialog.SelectedPath;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            dirTextBox.Text = Config.Data.SearchDir;
            subDirsCheckBox.Checked = Config.Data.SubDirsChecked;
            searchTextBox.Text = Config.Data.SearchText;

            // Subscribe delegates
            this.FoundInfo += new FoundInfoHandler(This_FoundInfo);
            this.ThreadEnded += new ThreadEndedHandler(This_ThreadEnded);

            // Subscribe for Searcher's events
            Search.FoundInfo += new Search.FoundInfoEventHandler(Searcher_FoundInfo);
            Search.ThreadEnded += new Search.ThreadEndedEventHandler(Searcher_ThreadEnded);

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _closing = true;

            Search.Stop();
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            // Clear the results list
            resultsList.Items.Clear();

            Encoding encoding = Encoding.ASCII;

            // Get the parameters for search and adjusting .txt only
            string fileExtension = "*.txt*";
            string[] fileNames = fileExtension.Split(new char[] { ';' });
            List<string> resultingFileNames = new List<string>();
            foreach (string file in fileNames)
            {
                string trimmedFileName = file.Trim();
                if (trimmedFileName != "")
                {
                    resultingFileNames.Add(trimmedFileName);
                }
            }

            SearchParams pars = new SearchParams( dirTextBox.Text.Trim(), subDirsCheckBox.Checked, resultingFileNames, searchTextBox.Text.Trim(), encoding);

            Search.Start(pars);
            DisableAllExpStop();
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            // Stop the search thread if it is running:
            Search.Stop();
        }

        private void ResultsList_DoubleClick(object sender, EventArgs e)
        {
            // Get the path from the selected item
            if (resultsList.SelectedItems.Count > 0)
            {
                string path = resultsList.SelectedItems[0].Text;

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = ""
                };
                Process process = new Process
                {
                    StartInfo = startInfo
                };

                process.Start();
            }
        }

        private void ResultsList_Resize(object sender, EventArgs e)
        {
            resultsList.Columns[0].Width = resultsList.Width - 150;
        }

        private void Searcher_FoundInfo(FoundInfoEvent e)
        {
            // if app closing
            if (!_closing)
            {
                // Invoke the method "this_FoundInfo" through a delegate, so it is executed in the same thread as MainWindow
                this.Invoke(FoundInfo, new object[] { e });
            }
        }

        private void This_FoundInfo(FoundInfoEvent e)
        {
            // Create a new item in the results list
            CreateResultsListItem(e.Info);
        }

        private void Searcher_ThreadEnded(ThreadEndedEvent e)
        {
            // if app closing
            if (!_closing)
            {
                // Invoke the method "this_ThreadEnded" through a delegate, so it is executed in the same thread as MainWindow
                this.Invoke(ThreadEnded, new object[] { e });
            }
        }

        private void This_ThreadEnded(ThreadEndedEvent e)
        {
            EnableAllExpStop();
        }


        // Methods

        // Enable all fields and buttons except stopButton
        private void EnableAllExpStop()
        {
            dirTextBox.Enabled = true;
            selectDirButton.Enabled = true;
            subDirsCheckBox.Enabled = true;
            searchTextBox.Enabled = true;
            startButton.Enabled = true;
            stopButton.Enabled = false;
        }

        // Disable all fields and buttons except stopButton
        private void DisableAllExpStop()
        {
            dirTextBox.Enabled = false;
            selectDirButton.Enabled = false;
            subDirsCheckBox.Enabled = false;
            searchTextBox.Enabled = false;
            startButton.Enabled = false;
            stopButton.Enabled = true;
        }

        private void CreateResultsListItem(FileSystemInfo info)
        {
            // Create new item and set text
            ListViewItem listViewItem = new ListViewItem
            {
                Text = info.FullName,

                ToolTipText = info.FullName
            };

            ListViewItem.ListViewSubItem listViewSubItem = new ListViewItem.ListViewSubItem();
            listViewSubItem.Text = info.FullName;

            //repetition of string
            string str = File.ReadAllText( info.FullName, Encoding.Default);
            string[] subStr = { searchTextBox.Text };
            string[] arr = str.Split(subStr, StringSplitOptions.None);

            resultsList.Items.Add(listViewItem);
            listViewItem.SubItems.Add((arr.Length - 1).ToString());
        }
    }

    class ThreadEndedEvent
    {
        private readonly bool _success;
        private readonly string _error;

        // Constructor

        public ThreadEndedEvent(bool success, string errorMsg)
        {
            _success = success;
            _error = errorMsg;
        }


        // Properties

        public bool Success
        {
            get { return _success; }
        }

        public string ErrorMsg
        {
            get { return _error; }
        }
    }
}