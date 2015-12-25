using System;

namespace WindowsFormsApplication1
{
    public class MacroAssemblerPreprocessor
    {
        public MacroAssemblerPreprocessor(string filename)
        {
            System.IO.StreamReader sr = new System.IO.StreamReader(filename);
            MessageBox.Show(sr.ReadToEnd());
            sr.Close();
        }
    }
}
