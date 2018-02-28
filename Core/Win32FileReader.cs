using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Decomp.Core
{
    public class Win32FileReader : StreamReader
    {
        public Win32FileReader(string s) : base(s, Encoding.GetEncoding("utf-8")) {}

        public static string[] ReadAllLines(string path)
        {
            var list = new List<string>();
            var f = new Win32FileReader(path);
            string item;
            while ((item = f.ReadLine()) != null) list.Add(item); 
            return list.ToArray();
        }
    }
}
