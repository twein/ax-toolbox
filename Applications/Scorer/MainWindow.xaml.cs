using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using AXToolbox.PdfHelpers;

namespace Scorer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(CloseTab));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //var fakeCollection = new ObservableCollection<Event>();
            //fakeCollection.Add(Event.Instance);

            //var editOptions = EditOptions.CanEdit;

            //AddTab(new EditEvent(fakeCollection, editOptions), "Event");
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (Event.Instance.IsDirty)
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

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (var fileName in e.Data.GetData(DataFormats.FileDrop, true) as string[])
                    Event.Instance.ImportFile(fileName);
            }
        }

        private void menuEventEdit_Click(object sender, RoutedEventArgs e)
        {
            var fakeCollection = new ObservableCollection<Event>();
            fakeCollection.Add(Event.Instance);

            var editOptions = EditOptions.CanEdit;

            AddTab(new EditEvent(fakeCollection, editOptions), "Event");
        }
        private void menuEventLoad_Click(object sender, RoutedEventArgs e)
        {
            if (Event.Instance.IsDirty)
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

            var fileName = GetOpenFileName(".AXevt files (*.AXevt)|*.AXEvt");
            if (!string.IsNullOrEmpty(fileName))
                Event.Instance.Load(fileName);
        }
        private void menuEventLoadDef_Click(object sender, RoutedEventArgs e)
        {
            var fileName = GetOpenFileName(".csv files (*.csv)|*.csv");
            if (!string.IsNullOrEmpty(fileName))
                Event.Instance.ImportFile(fileName);
        }
        private void menuEventSave_Click(object sender, RoutedEventArgs e)
        {
            var fileName = GetSaveFileName(".AXevt files (*.AXevt)|*.AXEvt", Event.Instance.ShortName);
            if (!string.IsNullOrEmpty(fileName))
                Event.Instance.Save(fileName);
        }
        private void menuEventSaveXml_Click(object sender, RoutedEventArgs e)
        {
            var fileName = GetSaveFileName(".xml files (*.xml)|*.xml", Event.Instance.ShortName);
            if (!string.IsNullOrEmpty(fileName))
                Event.Instance.Save(fileName, AXToolbox.Common.IO.SerializationFormat.XML);
        }
        private void menuEventExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void menuCompetitionsEdit_Click(object sender, RoutedEventArgs e)
        {
            var editOptions = EditOptions.CanEdit;
            if (Event.Instance.Pilots.Count == 0)
                editOptions |= EditOptions.CanAdd | EditOptions.CanDelete;

            AddTab(new EditCompetitions(Event.Instance.Competitions, editOptions), "Competitions");
        }

        private void menuPilotsEdit_Click(object sender, RoutedEventArgs e)
        {
            var editOptions = EditOptions.CanEdit;
            if (Event.Instance.Competitions.Count > 0 && Event.Instance.Tasks.Count == 0)
                editOptions |= EditOptions.CanAdd | EditOptions.CanDelete;

            AddTab(new EditPilots(Event.Instance.Pilots, editOptions), "Pilots");
        }

        private void menuTasksEdit_Click(object sender, RoutedEventArgs e)
        {
            var editOptions = EditOptions.CanEdit;
            if (Event.Instance.Competitions.Count > 0 && Event.Instance.Pilots.Count > 0)
                editOptions |= EditOptions.CanAdd | EditOptions.CanDelete;

            AddTab(new EditTasks(Event.Instance.Tasks, editOptions), "Tasks");
        }

        private void menuOutputPilotsListToPdf_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var folder = GetFolderName();
            if (!string.IsNullOrEmpty(folder))
                Event.Instance.PilotListToPdf(folder, true);
        }
        private void menuOutputPilotsWorkListToPdf_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var folder = GetFolderName();
            if (!string.IsNullOrEmpty(folder))
                Event.Instance.WorkListToPdf(folder, true);
        }
        private void menuOutputPilotsListByCompetitionToPdf_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var folder = GetFolderName();
            if (!string.IsNullOrEmpty(folder))
                foreach (var competition in Event.Instance.Competitions)
                    competition.PilotListToPdf(folder, true);
        }
        private void menuOutputTaskScoresTo1Pdf_Click(object sender, RoutedEventArgs e)
        {
            var folder = GetFolderName();
            if (!string.IsNullOrEmpty(folder))
                foreach (var competition in Event.Instance.Competitions)
                    competition.TaskScoresTo1Pdf(folder, true);
        }
        private void menuOutputTaskScoresToNPdf_Click(object sender, RoutedEventArgs e)
        {
            var folder = GetFolderName();
            if (!string.IsNullOrEmpty(folder))
                foreach (var competition in Event.Instance.Competitions)
                    competition.TaskScoresToNPdf(folder);
        }
        private void menuOutputTotalScoresToPdf_Click(object sender, RoutedEventArgs e)
        {
            var folder = GetFolderName();
            if (!string.IsNullOrEmpty(folder))
                foreach (var competition in Event.Instance.Competitions)
                    competition.TotalScoreToPdf(folder, true);
        }

        private void menuCompetitionPilotsEdit_Click(object sender, RoutedEventArgs e)
        {
            var competition = ((MenuItem)sender).Tag as Competition;
            var editOptions = EditOptions.CanDelete;

            AddTab(new EditPilots(competition.Pilots, editOptions), competition.Name + " pilots");
        }
        private void menuCompetitionPilotsReset_Click(object sender, RoutedEventArgs e)
        {
            var competition = ((MenuItem)sender).Tag as Competition;
            competition.ResetPilots();
        }
        private void menuCompetitionTasksEdit_Click(object sender, RoutedEventArgs e)
        {
            var competition = ((MenuItem)sender).Tag as Competition;
            var editOptions = EditOptions.CanDelete;

            AddTab(new EditTasks(competition.Tasks, editOptions), competition.Name + " tasks");
        }
        private void menuCompetitionTasksReset_Click(object sender, RoutedEventArgs e)
        {
            var competition = ((MenuItem)sender).Tag as Competition;
            competition.ResetTasks();
        }

        private void listBoxTask_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var task = ((TextBlock)sender).Tag as Task;
            textDescription.Text = task.ExtendedStatus;
        }
        private void listBoxTask_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            textDescription.Text = "";
        }
        private void menuTaskEditResults_Click(object sender, RoutedEventArgs e)
        {
            var task = ((MenuItem)sender).Tag as Task;
            var editOptions = EditOptions.CanEdit;

            var results = new ObservableCollection<ResultInfo>();
            foreach (var pr in task.PilotResults)
                results.Add(pr.ManualResultInfo);

            AddTab(new EditTaskResults(results, editOptions), string.Format("Task {0}", task.ShortDescription));
        }
        private void menuTaskResultsToPdf_Click(object sender, RoutedEventArgs e)
        {
            var task = ((MenuItem)sender).Tag as Task;

            var folder = GetFolderName();
            if (!string.IsNullOrEmpty(folder))
                task.ResultsToPdf(folder, true);
        }
        private void menuTaskComputeScores_Click(object sender, RoutedEventArgs e)
        {
            var task = ((MenuItem)sender).Tag as Task;
            foreach (var c in Event.Instance.Competitions)
            {
                var ts = c.TaskScores.First(s => s.Task == task);
                ts.ComputeScores();
            }
        }
        private void menuTaskSetStatus_Click(object sender, RoutedEventArgs e)
        {
            var task = ((MenuItem)sender).Tag as Task;

            //TODO: fix [0]
            var ts = Event.Instance.Competitions[0].TaskScores.First(s => s.Task == task);
            var dlg = new ScoreStatusWindow()
            {
                Title = "Task " + task.Description,
                Status = ts.Status,
                Version = ts.Version,
                RevisionDate = ts.RevisionDate,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
            };
            dlg.ShowDialog();
            if (dlg.Response == System.Windows.Forms.DialogResult.OK)
                foreach (var c in Event.Instance.Competitions)
                {
                    var taskScore = c.TaskScores.First(s => s.Task == task);
                    taskScore.Status = dlg.Status;
                    taskScore.Version = dlg.Version;
                    taskScore.RevisionDate = dlg.RevisionDate;
                }
        }
        private void menuTaskScoresToPdf_Click(object sender, RoutedEventArgs e)
        {
            var task = ((MenuItem)sender).Tag as Task;

            var folder = GetFolderName();

            if (!string.IsNullOrEmpty(folder))
            {
                foreach (var c in Event.Instance.Competitions)
                {
                    var ts = c.TaskScores.First(s => s.Task == task);
                    ts.ScoresToPdf(folder, true);
                }
            }
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

        private string GetOpenFileName(string filter)
        {
            string fileName = null;
            var dlg = new OpenFileDialog();
            dlg.Filter = filter;
            if (string.IsNullOrEmpty(Event.Instance.FilePath))
                dlg.InitialDirectory = Environment.CurrentDirectory;
            else
                dlg.InitialDirectory = Path.GetDirectoryName(Event.Instance.FilePath);
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == true)
                fileName = dlg.FileName;

            return fileName;
        }
        private string GetSaveFileName(string filter, string proposedName = null)
        {
            string fileName = null;
            var dlg = new SaveFileDialog();
            dlg.Filter = filter;
            dlg.FileName = proposedName;
            if (string.IsNullOrEmpty(Event.Instance.FilePath))
                dlg.InitialDirectory = Environment.CurrentDirectory;
            else
                dlg.InitialDirectory = Path.GetDirectoryName(Event.Instance.FilePath);
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == true)
                fileName = dlg.FileName;

            return fileName;
        }
        private string GetFolderName()
        {
            string folder = null;
            if (!string.IsNullOrEmpty(Event.Instance.FilePath))
                folder = Path.GetDirectoryName(Event.Instance.FilePath);
            else
            {
                var dlg = new System.Windows.Forms.FolderBrowserDialog();
                dlg.Description = "Choose a folder to save the scores";
                dlg.SelectedPath = Environment.CurrentDirectory;
                dlg.ShowNewFolderButton = true;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    folder = dlg.SelectedPath;
            }

            return folder;
        }
    }
}
