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
    public partial class Input : Window
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
        private string errorMessage = "";
        public string ErrorMessage
        {
            get { return errorMessage; }
        }

        private Func<string, string> validate;


        public Input(string prompt, string value, Func<string, string> validate)
        {
            InitializeComponent();
            this.prompt = prompt;
            this.validate = validate;
            DataContext = this;
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            errorMessage = validate(value);
            DataContext = null;
            DataContext = this;
            if (errorMessage == "")
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
