using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Linq;

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
            {
                Database.Instance.Load(dlg.FileName);
                DataContext = null;
                DataContext = this;
            }
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
        private void menuCompetitions_Click(object sender, RoutedEventArgs e)
        {
            AddTab(new EditCompetitions(), "Competitions");

        }
        private void menuPilots_Click(object sender, RoutedEventArgs e)
        {
            AddTab(new EditPilots(), "Pilots");
        }
        private void menuTasks_Click(object sender, RoutedEventArgs e)
        {
            AddTab(new EditTasks(), "Tasks");
        }

        private void menuTaskScores_Click(object sender, RoutedEventArgs e)
        {
            var competition = ((MenuItem)sender).Tag as Competition;

            var dlg = new SaveFileDialog();
            dlg.Filter = "pdf files (*.pdf)|*.pdf";
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == true)
                competition.PdfTaskScores(dlg.FileName);
        }
        private void menuGeneralScore_Click(object sender, RoutedEventArgs e)
        {
            var competition = ((MenuItem)sender).Tag as Competition;

            var dlg = new SaveFileDialog();
            dlg.Filter = "pdf files (*.pdf)|*.pdf";
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == true)
                competition.PdfGeneralScore(dlg.FileName);
        }
        private void menuTaskResults_Click(object sender, RoutedEventArgs e)
        {
            var task = ((MenuItem)sender).Tag as Task;
            if (task != null)
                AddTab(new EditTaskResults(task), string.Format("Task {0}", task.ToString()));
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
