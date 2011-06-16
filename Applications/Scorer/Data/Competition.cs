using System;
using System.Collections.ObjectModel;

namespace Scorer
{
    [Serializable]
    public class Competition
    {
        public string Name { get; set; }
        public string LocationDates { get; set; }
        public string Director { get; set; }

        public ObservableCollection<Pilot> Pilots { get; set; }
        public ObservableCollection<Task> Tasks { get; set; }

        public ObservableCollection<TaskScore> TaskScores { get; set; }

        public Competition()
        {
            Pilots = new ObservableCollection<Pilot>();
            Tasks = new ObservableCollection<Task>();
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>Generate a pdf general scores sheet
        /// </summary>
        /// <param header="fileName">desired pdf file path</param>
        public void PdfGeneralScore(string fileName)
        {
            throw new NotImplementedException();
        }

        /// <summary>Generate a pdf with all task scores
        /// </summary>
        /// <param header="fileName"></param>
        public void PdfTaskScores(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
