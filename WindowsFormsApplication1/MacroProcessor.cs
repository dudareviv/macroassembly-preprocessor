using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    class MacroProcessor
    {
        private string sourceFilename;
        private string destFilename;

        private List<Command> commands = new List<Command>();
        private Stack<Command> commandsStack = new Stack<Command>();

        public MacroProcessor(string filename)
        {
            if (File.Exists(filename))
            {
                sourceFilename = filename;
            }
        }
    }
}
