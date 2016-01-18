using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    public class MacroAssemblerPreprocessor
    {
        private string sourceFilename;
        private string destFilename;

        private List<Command> commands = new List<Command>();
        private Stack<Command> commandsStack = new Stack<Command>();

        private StreamReader sourceFileSR;
        private StreamWriter destFileSW;

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

            sourceFileSR = new StreamReader(sourceFilename);
            destFileSW = new StreamWriter(destFilename);

            ParseMacroCommands();
            GenerateFinalFile();

            sourceFileSR.Close();
            destFileSW.Close();

            return true;
        }

        /// <summary>
        /// Собираем все макро процедуры в исходном файле в таблицу.
        /// </summary>
        private void ParseMacroCommands()
        {
            // Сбрасываем в начало файла.
            sourceFileSR.BaseStream.Position = 0;
            sourceFileSR.DiscardBufferedData();

            Match matches;

            // Храним номер текущей строки.
            int lineNumber = 0;

            while (sourceFileSR.Peek() >= 0)
            {
                string line = sourceFileSR.ReadLine();
                line = Regex.Replace(line, @"[\t\s]+", " ");
                line.Trim();

                if(line.Length == 0)
                {
                    continue;
                }

                matches = Regex.Match(line, @"([\w\d]+) MACRO ([\w\s\d,]+)");
                if (matches.Success)
                {
                    Command command = new Command();
                    command.StartPosition = lineNumber + 1;
                    command.Name = matches.Groups[1].Value;
                    command.FormalParams = matches.Groups[2].Value;

                    commandsStack.Push(command);
                }

                matches = Regex.Match(line, @"ENDM");
                if (matches.Success)
                {
                    Command command = commandsStack.Pop();

                    if (command == null)
                    {
                        throw new ApplicationException("Ошибка на строке " + lineNumber);
                    }

                    command.EndPosition = lineNumber - 1;
                    //distFileSW.WriteLine("{0}-{1} {2}: {3}", command.StartPosition, command.EndPosition, command.Name, command.FormalParams);

                    commands.Add(command);
                }

                // Увеличиваем кол-во строк на единицу.
                lineNumber++;
            }
        }

        private void GenerateFinalFile()
        {
            // Сбрасываем указатель в начало файла.
            sourceFileSR.BaseStream.Position = 0;
            sourceFileSR.DiscardBufferedData();


        }
    }

    public class Command
    {
        public string Name;
        public string FormalParams;
        public string LocalParams;
        public long StartPosition;
        public long EndPosition;
    }
}