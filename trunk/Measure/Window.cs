/*
	AX-Measure - A program to perform the measures from a GPS logger 
                 in a hot air balloon competition.
	Copyright (c) 2005-2010 info@balloonerds.com
    Developers: Toni Martínez, Marcos Mezo, Dani Gallegos

	This program is free software; you can redistribute it and/or modify it
	under the terms of the GNU General Public License as published by the Free
	Software Foundation; either Version 2 of the License, or (at your option)
	any later Version.

	This program is distributed in the hope that it will be useful, but WITHOUT
	ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
	FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
	more details.

	You should have received a copy of the GNU General Public License along
	with this program (license.txt); if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

using System;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace Balloonerds.Measure
{
    public partial class Window : Form
    {
        public Window()
        {
            InitializeComponent();
            Text = Measure.Title;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            openFileDialog.Filter = "Definition files (*.def)|*.def|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                btnProcess.Enabled = true;
                btnResults.Enabled = true;
                txtFilename.Text = openFileDialog.FileName;
            }
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {
            if (txtFilename.Text != "" && File.Exists(txtFilename.Text))
            {
				btnBrowse.Enabled = false;
				btnProcess.Enabled = false;
                Flight.Reset();
				txtLog.Clear();
				txtLog.Refresh();
                Flight.Instance.SetLogger(txtLog);
                Directory.SetCurrentDirectory(Path.GetDirectoryName(txtFilename.Text));
                Flight.Instance.Process(Path.GetFileName(txtFilename.Text));
                System.GC.Collect();
				btnBrowse.Enabled = true;
				btnProcess.Enabled = true;
			}
        }

        private void btnResults_Click(object sender, EventArgs e)
        {
            if (txtFilename.Text != "")
            {
                string fileName = Path.ChangeExtension(txtFilename.Text, ".html");
                if (File.Exists(fileName))
                    Process.Start(fileName);
            }
        }

        private void linkAbout_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show(Measure.Version, Measure.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
