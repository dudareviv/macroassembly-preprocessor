using System;
using System.IO;

namespace WindowsFormsApplication1
{
    public class MacroAssemblerPreprocessor
    {
        private string sourceFilename;
        private string destFilename;

        public MacroAssemblerPreprocessor(string filename)
        {
            if (File.Exists(filename))
            {
                sourceFilename = filename;
            }
        }

        /// <summary>
        /// Основная логика выполнения программы.
        /// </summary>
        /// <returns>В случае ошибки возвращает ложь.</returns>
        public bool Run()
        {
            if (!File.Exists(sourceFilename))
            {
                return false;
            }

            destFilename = sourceFilename.Split('.')[0] + ".asm";

            StreamReader sourceFileSR = new StreamReader(sourceFilename);
            StreamWriter destFileSW = new StreamWriter(destFilename);

            

            sourceFileSR.Close();
            destFileSW.Close();

            return true;
        }
    }
}
