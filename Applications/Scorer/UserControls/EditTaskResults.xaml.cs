using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace Scorer
{
    /// <summary>
    /// Interaction logic for EditTaskScores.xaml
    /// </summary>
    public partial class EditTaskResults : UserControl
    {
        public ObservableCollection<PilotResult> PilotResults { get; set; }

        public EditTaskResults(Task task)
        {
            InitializeComponent();

            //http://www.i-programmer.info/programming/wpf-workings/620-using-the-wpf-net-40-datagrid-.html
            PilotResults=new ObservableCollection<PilotResult>();
            task.PilotResults.CopyTo(PilotResults);
            dgMain.DataContext = PilotResults;
        }

        private void buttonSave_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
