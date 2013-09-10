using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Scorer
{
    public partial class EditEvent : EditCollection<Event>
    {
        public EditEvent(ObservableCollection<Event> eventCollection, EditOptions editOptions)
            : base(eventCollection, editOptions)
        {
            InitializeComponent();
        }
    }
}
