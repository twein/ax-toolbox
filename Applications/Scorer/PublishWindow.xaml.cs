using System;
using System.Windows;
using System.Windows.Forms;
using System.ComponentModel;

namespace Scorer
{
    public partial class PublishWindow : Window, INotifyPropertyChanged
    {
        private ScoreStatus status;
        public ScoreStatus Status
        {
            get { return status; }
            set
            {
                status = value;
                RaisePropertyChanged("Status");
            }
        }
        private int version;
        public int Version
        {
            get { return version; }
            set
            {
                version = value;
                RaisePropertyChanged("Version");
            }
        }
        private DateTime revisionDate;
        public DateTime RevisionDate
        {
            get { return revisionDate; }
            set
            {
                revisionDate = value;
                RaisePropertyChanged("RevisionDate");
            }
        }

        private DialogResult response;
        public DialogResult Response
        {
            get { return response; }
            protected set
            {
                response = value;
                RaisePropertyChanged("Response");
            }
        }

        public PublishWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            Response = System.Windows.Forms.DialogResult.OK;
            Close();
        }
        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Response = System.Windows.Forms.DialogResult.Cancel;
            Close();
        }
        private void buttonNow_Click(object sender, RoutedEventArgs e)
        {
            RevisionDate = DateTime.Now;
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
