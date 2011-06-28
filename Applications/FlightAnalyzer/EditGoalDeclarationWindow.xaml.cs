using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using AXToolbox.GpsLoggers;

namespace FlightAnalyzer
{
    public partial class EditGoalDeclarationWindow : Window, INotifyPropertyChanged
    {
        public DialogResult Response { get; set; }

        protected GoalDeclaration declaration;
        public GoalDeclaration Declaration
        {
            get { return declaration; }
            set
            {
                declaration = value;
                RaisePropertyChanged("Declaration");
            }
        }

        public EditGoalDeclarationWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var x = GoalDeclaration.Parse(textBox.Text);
                Response = System.Windows.Forms.DialogResult.OK;
                Close();
            }
            catch { }
        }
        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Response = System.Windows.Forms.DialogResult.Cancel;
            Close();
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
