using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System;
using Microsoft.Win32;

namespace Scorer
{
    public partial class MainWindow : Window
    {
        public Database db{get;set;}

        public MainWindow()
        {
            InitializeComponent();

            AddHandler(CloseableTabItem.CloseTabEvent, new RoutedEventHandler(CloseTab));

            db = Database.Instance;
            db.Pilots.Add(new Pilot() { Number = 1, Name = "PETIT, Joan" });
            db.Pilots.Add(new Pilot() { Number = 2, Name = "PALOTES, Perico" });
            db.Pilots.Add(new Pilot() { Number = 3, Name = "DOE, John" });

            AddTab(new EditTaskScores(), "Prova1");
            AddTab(new EditTaskScores(), "Prova2");
            AddTab(new EditTaskScores(), "Prova3");
            AddTab(new EditTaskScores(), "Prova4");

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

        private void loadPilotsButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "CSV files (*.csv)|*.csv";
            dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog(this) == true)
            {
                db.LoadPilots(dlg.FileName);
            }
        }

        private void loadCompetitionsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void loadTaskResultsButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
