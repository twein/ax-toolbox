using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace Scorer
{
    [Flags]
    public enum EditOptions
    {
        None = 0x0,
        CanAdd = 0x1,
        CanDelete = 0x2,
        CanEdit = 0x4,
        All = ~0x0
    }

    public class EditCollection<T> : UserControl
    {
        public ObservableCollection<T> DataGridCollection { get; protected set; }
        protected ObservableCollection<T> SaveCollection { get; set; }
        protected bool buffered { get; set; }

        protected EditOptions options;
        public bool ReadOnly
        {
            get { return (options & EditOptions.CanEdit) == 0; }
        }
        public Visibility DeleteVisibility
        {
            get { return (options & EditOptions.CanDelete) > 0 ? Visibility.Visible : Visibility.Collapsed; }
        }
        public Visibility AddVisibility
        {
            get { return (options & EditOptions.CanAdd) > 0 ? Visibility.Visible : Visibility.Collapsed; }
        }
        public Visibility SaveVisibility
        {
            get { return buffered ? Visibility.Visible : Visibility.Collapsed; }
        }

        public EditCollection() { }

        public EditCollection(ObservableCollection<T> collection, EditOptions editOptions, bool buffered = false)
        {
            DataContext = this;

            options = editOptions;
            this.buffered = buffered;

            if (buffered)
            {
                SaveCollection = collection;
                DataGridCollection = new ObservableCollection<T>();
                collection.CopyTo(DataGridCollection);
            }
            else
                DataGridCollection = collection;
        }

        public void Save()
        {
            if (buffered)
                DataGridCollection.CopyTo(SaveCollection);
        }
    }
}
