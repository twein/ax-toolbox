using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Scorer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(CloseTab));
        }

        public void AddTab(UserControl control, string header)
        {

            // make sure the passed in arguments are good
            Debug.Assert(control != null, "UserControl control is null");
            Debug.Assert(header != null, "string header is null");

            // locate the TabControl that the tab will be added to
            var itemsTab = this.FindName("ItemsTab") as TabControl;
            Debug.Assert(itemsTab != null, "can't find ItemsTab");

            //recycle if already open
            //find a tab with the same header
            CloseableTabItem tab = null;
            foreach (CloseableTabItem t in itemsTab.Items)
            {
                if ((string)t.Header == header)
                {
                    tab = t;
                    break;
                }
            }

            if (tab == null)
            {
                // not found
                // create and populate a new tab and add it to the tab control
                tab = new CloseableTabItem();
                tab.Content = control;
                tab.Header = header;
                itemsTab.Items.Add(tab);
            }

            // display the new tab to the user; if this line is missing
            // you get a blank tab
            itemsTab.SelectedItem = tab;
        }
        private void CloseTab(object source, RoutedEventArgs args)
        {
            var tabItem = args.Source as TabItem;
            if (tabItem != null)
            {
                var tabControl = tabItem.Parent as TabControl;
                if (tabControl != null)
                    tabControl.Items.Remove(tabItem);
            }
        }

        private void menuLoadEvent_Click(object sender, RoutedEventArgs e)
        {
            if (Database.Instance.IsDirty)
            {
                var response = MessageBox.Show(
                    "The event database has not been saved. Are you sure you want to load other data?",
                    "Warning!",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (response == MessageBoxResult.No)
                    return;
            }

            var dlg = new OpenFileDialog();
            dlg.Filter = ".AXevt files (*.AXevt)|*.AXEvt";
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == true)
                Database.Instance.Load(dlg.FileName);
        }
        private void menuSaveEvent_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = ".AXevt files (*.AXevt)|*.AXEvt";
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == true)
                Database.Instance.Save(dlg.FileName);
        }
        private void menuSaveXmlEvent_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = ".xml files (*.xml)|*.xml";
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == true)
                Database.Instance.Save(dlg.FileName, AXToolbox.Common.IO.SerializationFormat.XML);
        }
        private void menuCompetitions_Click(object sender, RoutedEventArgs e)
        {
            var editOptions = EditOptions.CanEdit;
            if (Database.Instance.Pilots.Count == 0)
                editOptions |= EditOptions.CanAdd | EditOptions.CanDelete;

            AddTab(new EditCompetitions(Database.Instance.Competitions, editOptions), "Competitions");
        }
        private void menuPilots_Click(object sender, RoutedEventArgs e)
        {
            var editOptions = EditOptions.CanEdit;
            if (Database.Instance.Competitions.Count > 0 && Database.Instance.Tasks.Count == 0)
                editOptions |= EditOptions.CanAdd | EditOptions.CanDelete;

            AddTab(new EditPilots(Database.Instance.Pilots, editOptions), "Pilots");
        }
        private void menuTasks_Click(object sender, RoutedEventArgs e)
        {
            var editOptions = EditOptions.CanEdit;
            if (Database.Instance.Competitions.Count > 0 && Database.Instance.Pilots.Count > 0)
                editOptions |= EditOptions.CanAdd | EditOptions.CanDelete;

            AddTab(new EditTasks(Database.Instance.Tasks, editOptions), "Tasks");
        }

        private void menuCompetitionPilots_Click(object sender, RoutedEventArgs e)
        {
            var competition = ((MenuItem)sender).Tag as Competition;
            var editOptions = EditOptions.CanDelete;

            AddTab(new EditPilots(competition.Pilots, editOptions), competition.Name + " pilots");
        }
        private void menuResetCompetitionPilots_Click(object sender, RoutedEventArgs e)
        {
            var competition = ((MenuItem)sender).Tag as Competition;
            competition.ResetPilots();
        }
        private void menuCompetitionTasks_Click(object sender, RoutedEventArgs e)
        {
            var competition = ((MenuItem)sender).Tag as Competition;
            var editOptions = EditOptions.CanDelete;

            AddTab(new EditTasks(competition.Tasks, editOptions), competition.Name + " tasks");
        }
        private void menuResetCompetitionTasks_Click(object sender, RoutedEventArgs e)
        {
            var competition = ((MenuItem)sender).Tag as Competition;
            competition.ResetTasks();
        }
        private void menuCompetitionTaskScores_Click(object sender, RoutedEventArgs e)
        {
            var competition = ((MenuItem)sender).Tag as Competition;

            var dlg = new SaveFileDialog();
            dlg.Filter = "pdf files (*.pdf)|*.pdf";
            dlg.FileName = string.Format("{0}-Task scores", competition.Name);
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == true)
                competition.PdfTaskScores(dlg.FileName);
        }
        private void menuCompetitionGeneralScore_Click(object sender, RoutedEventArgs e)
        {
            var competition = ((MenuItem)sender).Tag as Competition;

            var dlg = new SaveFileDialog();
            dlg.Filter = "pdf files (*.pdf)|*.pdf";
            dlg.FileName = string.Format("{0}-General score", competition.Name);
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == true)
                competition.PdfGeneralScore(dlg.FileName);
        }

        private void menuEditTaskResults_Click(object sender, RoutedEventArgs e)
        {
            var task = ((MenuItem)sender).Tag as Task;
            var editOptions = EditOptions.CanEdit;

            AddTab(new EditTaskResults(task.PilotResults, editOptions), string.Format("Task {0}", task.ToString()));
        }
        private void menuShowTaskResults_Click(object sender, RoutedEventArgs e)
        {
            var task = ((MenuItem)sender).Tag as Task;

            var dlg = new SaveFileDialog();
            dlg.Filter = "pdf files (*.pdf)|*.pdf";
            dlg.FileName = string.Format("{0}-Results", task.UltraShortDescription);
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == true)
                task.ResultsToPdf(dlg.FileName);
        }
        private void menuComputeScores_Click(object sender, RoutedEventArgs e)
        {
            var task = ((MenuItem)sender).Tag as Task;
            foreach (var c in Database.Instance.Competitions)
            {
                var ts = c.TaskScores.First(s => s.Task == task);
                ts.Compute();
            }
        }
        private void menuTaskScores_Click(object sender, RoutedEventArgs e)
        {
            var task = ((MenuItem)sender).Tag as Task;

            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.Description = "Choose a folder to save the scores";
            dlg.SelectedPath = Environment.CurrentDirectory;
            dlg.ShowNewFolderButton = true;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var path = dlg.SelectedPath;
                foreach (var c in Database.Instance.Competitions)
                {
                    var ts = c.TaskScores.First(s => s.Task == task);
                    var pdfName = string.Format("{0}-{1}-v{3:00}{4}-{2:MMdd HHmmss}",
                        c.Name, task.UltraShortDescription, ts.RevisionDate, ts.Version, ts.Status.ToString().Substring(0, 1));
                    var pdfPath = Path.Combine(path, pdfName);
                    ts.PdfScores(pdfPath);
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (Database.Instance.IsDirty)
            {
                var response = MessageBox.Show(
                    "The event database has not been saved. Are you sure you want to close the application?",
                    "Warning!",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (response == MessageBoxResult.No)
                    e.Cancel = true;
            }
        }
    }
}
