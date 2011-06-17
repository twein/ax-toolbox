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

        protected EditOptions options;
        public bool ReadOnly
        {
            get { return (options & EditOptions.CanEdit) == 0; }
        }
        public Visibility DeleteVisibility
        {
            get { return (options & EditOptions.CanDelete) > 0 ? Visibility.Visible : Visibility.Hidden; }
        }
        public Visibility AddVisibility
        {
            get { return (options & EditOptions.CanAdd) > 0 ? Visibility.Visible : Visibility.Hidden; }
        }

        public EditCollection() { }

        public EditCollection(ObservableCollection<T> collection, EditOptions editOptions)
        {
            DataContext = this;

            options = editOptions;
            DataGridCollection = collection;
        }
    }
}
