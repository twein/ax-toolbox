using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Scorer
{
    public class EditCollection<T> : UserControl
    {
        protected ObservableCollection<T> saveCollection;
        public ObservableCollection<T> BufferCollection { get; protected set; }

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
            saveCollection = collection;

            BufferCollection = new ObservableCollection<T>();
            saveCollection.CopyTo(BufferCollection);
        }

        protected void Save()
        {
            BufferCollection.CopyTo(saveCollection);
            Database.Instance.IsDirty = true;
        }
    }
}
