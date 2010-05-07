﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FlightAnalyzer.Properties;

namespace FlightAnalyzer
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		public SettingsWindow()
		{
			InitializeComponent();
		}


		private void buttonSave_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.Save();
			DialogResult = true;
			Close();
		}

		private void buttonReset_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.Reset();
		}

		private void buttonCancel_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.Reload();
			DialogResult = false;
			Close();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			Settings.Default.Reload();
			DialogResult = false;
		}
	}
}