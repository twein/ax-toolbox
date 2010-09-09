using System;
using System.Windows;
using AXToolbox.Common;
using System.Globalization;
using AXToolbox.Common.IO;

namespace FlightAnalyzer
{
    /// <summary>
    /// Interaction logic for Input.xaml
    /// </summary>
    public partial class InputWindow : Window
    {
        private string prompt;
        public string Prompt
        {
            get { return prompt; }
        }

        private string value = "";
        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }

        private Func<string, string> validate;


        public InputWindow(string prompt, string value, Func<string, string> validate)
        {
            InitializeComponent();
            this.prompt = prompt;
            this.validate = validate;
            DataContext = this;
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            textBlockError.Text = validate(value);
            if (textBlockError.Text == "")
                DialogResult = true;
            else
                textBoxValue.Focus();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
