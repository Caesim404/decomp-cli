using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Decomp.Core
{
    public class Win32FileWriter : StreamWriter
    {
        public Win32FileWriter(string s) : base(s, false) {}

        public static void WriteAllText(string fileName, string data)
        {
            var writer = new Win32FileWriter(fileName);
            writer.Write(data);
            writer.Close();
        }
    }
}
