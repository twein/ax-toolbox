using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace Scorer
{
    public partial class PublishWindow : Window, INotifyPropertyChanged
    {
        private Task Task;

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

        public PublishWindow(Task task)
        {
            InitializeComponent();

            Task = task;

            //TODO: fix [0]
            var taskScoresTmp = from c in Event.Instance.Competitions
                                from ts in c.TaskScores
                                select ts;
            var taskScore = taskScoresTmp.First(s => s.Task == Task);

            Title = "Task " + Task.Description;
            Status = taskScore.Status;
            Version = taskScore.Version;
            RevisionDate = taskScore.RevisionDate;

            DataContext = this;
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            if (Status != ScoreStatus.Provisional)
            {
                Task.Phases |= CompletedPhases.Published;

                foreach (var c in Event.Instance.Competitions)
                {
                    var taskScore = c.TaskScores.FirstOrDefault(s => s.Task == Task);
                    if (taskScore != null)
                    {
                        taskScore.Status = Status;
                        taskScore.Version = Version;
                        taskScore.RevisionDate = RevisionDate;

                        try
                        {
                            taskScore.ScoresToPdf(false);
                        }
                        catch { }
                    }
                }

            }

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
