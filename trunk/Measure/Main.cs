/*
	AX-Measure - A program to perform the measures from a GPS logger 
                 in a hot air balloon competition.
	Copyright (c) 2005-2010 info@balloonerds.com
    Developers: Toni Martínez, Marcos Mezo, Dani Gallegos

	This program is free software; you can redistribute it and/or modify it
	under the terms of the GNU General Public License as published by the Free
	Software Foundation; either version 2 of the License, or (at your option)
	any later version.

	This program is distributed in the hope that it will be useful, but WITHOUT
	ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
	FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
	more details.

	You should have received a copy of the GNU General Public License along
	with this program (license.txt); if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

using System;
using System.Reflection;
using System.Windows.Forms;

namespace Balloonerds.Measure
{
    public enum Magnitudes { Distance, Time, Angle, Area, Points, MinuteOrFraction };

    class Measure
    {
        static private string title;
        static private string version;

        static public string Title
        {
            get { return title; }
        }

        static public string Version
        {
            get { return version; }
        }

        [STAThread]
        static void Main(string[] args)
        {
            //AssemblyName aname = Assembly.GetAssembly(typeof(Measure)).GetName();
            //title = aname.Name;
            //version =
            //     aname.Name +
            //     " version " + aname.Version.Major + "." + aname.Version.Minor + "." + aname.Version.Build + "." + aname.Version.Revision +
            //     " (c) 2005-2010 info@balloonerds.com" + Environment.NewLine + Environment.NewLine +
            //     "This program comes with ABSOLUTELY NO WARRANTY. This is free software, and you are welcome to redistribute it under certain conditions. Read http://www.gnu.org/licenses/gpl.html for details.";

            title = "AX-Measure";
            version = "1.10.06.*" +
                " (c) 2005-2010 info@balloonerds.com" + Environment.NewLine + Environment.NewLine +
                "This program comes with ABSOLUTELY NO WARRANTY. This is free software, and you are welcome to redistribute it under certain conditions. Read http://www.gnu.org/licenses/gpl.html for details.";
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Window());
        }
    }
}
