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

        private void loadCompetitionsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void loadTaskResultsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuPilots_Click(object sender, RoutedEventArgs e)
        {
            AddTab(new EditPilots(), "Pilots");
        }
    }
}
