using System;
using AXToolbox.Common;

namespace Scorer
{
    [Serializable]
    public class Pilot : BindableObject
    {
        private int number;
        public int Number
        {
            get { return number; }
            set
            {
                number = value;
                RaisePropertyChanged("Number");
            }
        }
        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                RaisePropertyChanged("Name");
            }
        }
        private string country;
        public string Country
        {
            get { return country; }
            set
            {
                country = value;
                RaisePropertyChanged("Country");
            }
        }
        private string balloon;
        public string Balloon
        {
            get { return balloon; }
            set
            {
                balloon = value;
                RaisePropertyChanged("Balloon");
            }
        }
        private bool isDisqualified;
        public bool IsDisqualified
        {
            get { return isDisqualified; }
            set
            {
                isDisqualified = value;
                Database.Instance.IsDirty = true;
            }
        }

        public Pilot() { }

        protected override void AfterPropertyChanged(string propertyName)
        {
            Database.Instance.IsDirty = true;
        }

        public override string ToString()
        {
            return string.Format("{0:000}: {1}", Number, Name);
        }
    }
}
