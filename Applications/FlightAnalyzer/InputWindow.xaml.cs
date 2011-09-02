using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using AXToolbox.GpsLoggers;

namespace FlightAnalyzer
{
    public partial class InputWindow : Window, INotifyPropertyChanged
    {
        public string Text { get; set; }
        public DialogResult Response { get; set; }
        private Func<string, bool> Validate;
        private Visibility CancelButtonVisibility { get; set; }

        public InputWindow(Func<string, bool> validateFunction)
        {
            InitializeComponent();
            DataContext = this;
            Validate = validateFunction;

            Response = System.Windows.Forms.DialogResult.Cancel;

            textBox.Focus();
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            Ok();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
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

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //TODO: fix: it does not fire!
            switch (e.Key)
            {
                case System.Windows.Input.Key.Return:
                    Ok();
                    break;
            }
        }
        private void Ok()
        {
            try
            {
                if (Validate(Text))
                {
                    Response = System.Windows.Forms.DialogResult.OK;
                    Close();
                }
            }
            catch
            {
                textBox.Focus();
            }
        }
    }
}
