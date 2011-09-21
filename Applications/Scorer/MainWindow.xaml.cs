using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //var fakeCollection = new ObservableCollection<Event>();
            //fakeCollection.Add(Event.Instance);

            //var editOptions = EditOptions.CanEdit;

            //AddTab(new EditEvent(fakeCollection, editOptions), "Event");
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!ConfirmPossibleDataLoss())
                e.Cancel = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    foreach (var fileName in e.Data.GetData(DataFormats.FileDrop, true) as string[])
                        Event.Instance.ImportFile(fileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void menuEventEdit_Click(object sender, RoutedEventArgs e)
        {
            var fakeCollection = new ObservableCollection<Event>();
            fakeCollection.Add(Event.Instance);

            var editOptions = EditOptions.CanEdit;

            AddTab(new EditEvent(fakeCollection, editOptions), "Event");
        }
        private void menuEventLoadCsv_Click(object sender, RoutedEventArgs e)
        {
            var fileName = GetOpenFileName(".csv files (*.csv)|*.csv");
            if (!string.IsNullOrEmpty(fileName))
            {
                try
                {
                    Event.Instance.ImportFile(fileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message);
                }
            }
        }
        private void menuEventSaveXml_Click(object sender, RoutedEventArgs e)
        {
            var fileName = GetSaveFileName(".xml files (*.xml)|*.xml");

            if (!string.IsNullOrEmpty(fileName))
                Event.Instance.Save(fileName, AXToolbox.Common.IO.SerializationFormat.XML);
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
            Event.Instance.PilotListToPdf(true);
        }
        private void menuOutputPilotsWorkListToPdf_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Event.Instance.WorkListToPdf(true);
        }
        private void menuOutputPilotsListByCompetitionToPdf_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (var competition in Event.Instance.Competitions)
                competition.PilotListToPdf(true);
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
            foreach (var competition in Event.Instance.Competitions)
                competition.TaskScoresToNPdf();
        }
        private void menuOutputTotalScoresPublicationToPdf_Click(object sender, RoutedEventArgs e)
        {
            foreach (var competition in Event.Instance.Competitions)
                competition.TotalScoreToPdf(true, true);
        }
        private void menuOutputTotalScoresToPdf_Click(object sender, RoutedEventArgs e)
        {
            foreach (var competition in Event.Instance.Competitions)
                competition.TotalScoreToPdf(false, true);
        }
        private void menuAbout_Click(object sender, RoutedEventArgs e)
        {
            var assembly = GetType().Assembly;
            var aName = assembly.GetName();
            var aTitle = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            var aCopyright = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            Debug.Assert(aTitle.Length > 0 && aCopyright.Length > 0, "Assembly information incomplete");

            var programInfo = string.Format("{0} v{1} {2}",
                ((AssemblyTitleAttribute)aTitle[0]).Title,
                aName.Version,
                ((AssemblyCopyrightAttribute)aCopyright[0]).Copyright);

            MessageBox.Show(this, programInfo, "About", MessageBoxButton.OK, MessageBoxImage.Information);
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

            AddTab(new EditTaskResults(task, results, editOptions), string.Format("Task {0} Manual", task.ShortDescription));
        }
        private void menuTaskEditAutoResults_Click(object sender, RoutedEventArgs e)
        {
            var task = ((MenuItem)sender).Tag as Task;
            var editOptions = EditOptions.CanEdit;

            var results = new ObservableCollection<ResultInfo>();
            foreach (var pr in task.PilotResults)
                results.Add(pr.AutoResultInfo);

            AddTab(new EditTaskResults(task, results, editOptions), string.Format("Task {0} Auto", task.ShortDescription));
        }
        private void menuTaskResultsToPdf_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Event.Instance.FilePath))
                MessageBox.Show("Please, save event before!");
            else
            {
                var task = ((MenuItem)sender).Tag as Task;
                task.ResultsToPdf(true);
            }
        }
        private void menuTaskPublishScore_Click(object sender, RoutedEventArgs e)
        {
            var task = ((MenuItem)sender).Tag as Task;
            var dlg = new PublishWindow(task);
            dlg.ShowDialog();
        }
        private void menuTaskScoresToPdf_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Event.Instance.FilePath))
                MessageBox.Show("Please, save event before!");
            else
            {
                var task = ((MenuItem)sender).Tag as Task;

                var folder = GetFolderName();

                if (!string.IsNullOrEmpty(folder))
                {
                    foreach (var c in Event.Instance.Competitions)
                    {
                        var ts = c.TaskScores.First(s => s.Task == task);
                        ts.ScoresToPdf(true);
                    }
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
            var dlg = new OpenFileDialog();
            dlg.Filter = filter;
            dlg.RestoreDirectory = true;
            if (string.IsNullOrEmpty(Event.Instance.FilePath))
                dlg.InitialDirectory = Environment.CurrentDirectory;
            else
                dlg.InitialDirectory = Path.GetDirectoryName(Event.Instance.FilePath);

            string fileName = null;
            if (dlg.ShowDialog() == true)
                fileName = dlg.FileName;

            return fileName;
        }
        private string GetSaveFileName(string filter)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = filter;
            dlg.RestoreDirectory = true;
            if (string.IsNullOrEmpty(Event.Instance.FilePath))
            {
                dlg.InitialDirectory = Environment.CurrentDirectory;
                dlg.FileName = Event.Instance.ShortName;
            }
            else
            {
                dlg.InitialDirectory = Path.GetDirectoryName(Event.Instance.FilePath);
                dlg.FileName = Event.Instance.FilePath;
            }

            string fileName = null;
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

        private void CommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //TODO: add cases
            e.CanExecute = true;
        }
        private void CommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command.Equals(ApplicationCommands.Open))
                OpenEvent();
            else if (e.Command.Equals(ApplicationCommands.Save))
                SaveEvent();
            else if (e.Command.Equals(ApplicationCommands.SaveAs))
                SaveEventAs();
            else if (e.Command.Equals(ApplicationCommands.Close))
                Close();
            else { }
        }

        private void OpenEvent()
        {
            if (ConfirmPossibleDataLoss())
            {
                var fileName = GetOpenFileName("AX-Scorer files (*.sco)|*.sco");
                if (!string.IsNullOrEmpty(fileName))
                    Event.Instance.Load(fileName);
            }
        }
        private void SaveEvent()
        {
            if (string.IsNullOrEmpty(Event.Instance.FilePath))
                SaveEventAs();
            else
                Event.Instance.Save(Event.Instance.FilePath);
        }
        private void SaveEventAs()
        {
            var fileName = GetSaveFileName("AX-Scorer files (*.sco)|*.sco");

            if (!string.IsNullOrEmpty(fileName))
                Event.Instance.Save(fileName);
        }

        private bool ConfirmPossibleDataLoss()
        {
            var confirm = false;

            if (!Event.Instance.IsDirty)
            {
                confirm = true;
            }
            else
            {
                var response = MessageBox.Show(this,
                    "The event database contains data that has not been saved. Are you sure you want to continue and lose this data?",
                    "Warning!",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (response == MessageBoxResult.Yes)
                    confirm = true;
            }

            return confirm;
        }
    }
}
