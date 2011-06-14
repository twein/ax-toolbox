using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Scorer
{
    public partial class MainWindow : Window
    {
        public Database db { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(CloseTab));

            db = Database.Instance;

            DataContext = this;
        }

        public void AddTab(UserControl control, string name)
        {

            //    make sure the passed in arguments are good
            Debug.Assert(control != null, "UserControl control is null");
            Debug.Assert(name != null, "string name is null");

            //    locate the TabControl that the tab will be added to
            var itemsTab = this.FindName("ItemsTab") as TabControl;
            Debug.Assert(itemsTab != null, "can't find ItemsTab");

            //    create and populate the new tab and add it to the tab control
            var newTab = new CloseableTabItem();
            newTab.Content = control;
            newTab.Header = name;
            itemsTab.Items.Add(newTab);

            //    display the new tab to the user; if this line is missing
            //    you get a blank tab
            itemsTab.SelectedItem = newTab;
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
            if (db.IsDirty)
            {
                var response = MessageBox.Show(
                    "The event data has not been saved. Are you sure you want to load other data?",
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
        private void menuPilots_Click(object sender, RoutedEventArgs e)
        {
            AddTab(new EditPilots(), "Pilots");
        }
        private void menuTasks_Click(object sender, RoutedEventArgs e)
        {
            AddTab(new EditTasks(), "Tasks");
        }
        private void menuCompetitions_Click(object sender, RoutedEventArgs e)
        {
            AddTab(new EditCompetitions(), "Competitions");

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (db.IsDirty)
            {
                var response = MessageBox.Show(
                    "The event data has not been saved. Are you sure you want to close the application?",
                    "Warning!",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (response == MessageBoxResult.No)
                    e.Cancel = true;
            }
        }

        private void menuTaskResults_Click(object sender, RoutedEventArgs e)
        {
            var task = ((MenuItem)sender).Tag as Task;
            if (task != null)
                AddTab(new EditTaskResults(task), string.Format("Task {0}", task.ToString()));
        }

        private void menuTaskScores_Click(object sender, RoutedEventArgs e)
        {
            var competition = ((MenuItem)sender).Tag as Competition;
        }

        private void menuGeneralScore_Click(object sender, RoutedEventArgs e)
        {
            var competition = ((MenuItem)sender).Tag as Competition;
        }
    }
}
