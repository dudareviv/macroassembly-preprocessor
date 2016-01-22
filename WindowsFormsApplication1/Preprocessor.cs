using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Application
{
    class Preprocessor
    {
        private string sourceFilename;
        private string destFilename;

        private StreamReader sourceFileSR;
        private StreamWriter destFileSW;

        private char[] commentChars = new char[] { ';', '#' };

        /// <summary>
        /// Номер метки.
        /// </summary>
        private int labelsCount = 0;

        /// <summary>
        /// Номер текущей строки
        /// </summary>
        private long position = 0;

        /// <summary>
        /// Текущая строка
        /// </summary>
        private string line = "";

        /// <summary>
        /// Совпадения с шаблоном.
        /// </summary>
        private MatchCollection matches;

        private List<string> operands = new List<string>();

        private List<String> names = new List<string>();

        /// <summary>
        /// Список макро команд.
        /// </summary>
        private List<MacroCommand> commands = new List<MacroCommand>();

        /// <summary>
        /// Стек определений макрокоманд.
        /// </summary>
        private Stack<MacroCommand> defineStack = new Stack<MacroCommand>();

        /// <summary>
        /// Стек вызовов макрокоманд.
        /// </summary>
        private Stack<MacroCommand> expandStack = new Stack<MacroCommand>();

        public Preprocessor(string filename)
        {
            if (File.Exists(filename))
            {
                sourceFilename = filename;
                destFilename = sourceFilename.Substring(0, sourceFilename.LastIndexOf('.')) + ".asm";
            }
            else
            {
                throw new FileNotFoundException(String.Format("Файл {0} не найден", sourceFilename));
            }
        }

        public void Run()
        {
            sourceFileSR = new StreamReader(sourceFilename);
            destFileSW = new StreamWriter(destFilename);

            while (sourceFileSR.Peek() >= 0)
            {
                GetLine();
                ProcessLine();
            }

            sourceFileSR.Close();
            destFileSW.Close();
        }

        /// <summary>
        /// Осуществляет переход к определенной строке. zero-based.
        /// </summary>
        /// <param name="position">Номер строки, на которую нужно поставить указатель.</param>
        private void SetLine(long position)
        {
            this.position = position - 1;

            sourceFileSR.BaseStream.Position = 0;
            sourceFileSR.DiscardBufferedData();

            for (int i = 0; i < this.position; i++)
            {
                sourceFileSR.ReadLine();
            }
        }

        /// <summary>
        /// Читает следующую строку из файла.
        /// </summary>
        private void GetLine()
        {
            line = sourceFileSR.ReadLine();

            position++;

            line = Regex.Replace(line, @"[\t\s]+", " ");
            line = line.Trim();

            if (expandStack.Count > 0)
            {
                string pattern;
                var command = expandStack.Peek();

                foreach (KeyValuePair<string,int> label in command.Labels)
                {
                    pattern = String.Format(@"\b{0}\b", label.Key);
                    line = Regex.Replace(line, @pattern, label.Value.ToString("??0000"));
                }

                for (int j = 0; j < command.FactParams.Count; j++)
                {
                    pattern = String.Format(@"\b{0}\b", command.FormalParams[j]);
                    line = Regex.Replace(line, @pattern, command.FactParams[j]);
                }
            }

            matches = Regex.Matches(line, @"[\w\d\[\]]+:?");

            operands.Clear();
            foreach (Match match in matches)
            {
                operands.Add(match.Value);
            }
        }

        /// <summary>
        /// Обрабатывает текущую строку.
        /// </summary>
        private void ProcessLine()
        {
            if (!IsCorrectLine())
            {
                return;
            }

            if (IsDefineLabel())
            {
                DefineLabel();
            }

            if (IsEndExpand())
            {
                EndExpand();
            }
            else if (IsStartExpand())
            {
                StartExpand();
            }
            else if (IsEndDefine())
            {
                EndDefind();
            }
            else if (IsStartDefine())
            {
                StartDefine();
            }
            else if (defineStack.Count == 0)
            {
                WriteLine();
            }
        }

        /// <summary>
        /// Добавляет метку в команду.
        /// </summary>
        private void DefineLabel()
        {
            defineStack.Peek().Labels[operands[0].TrimEnd(':')] = 0;
        }

        /// <summary>
        /// Проверяет является ли первый параметр меткой.
        /// Вернет истину, если в данный момент описывается макрос.
        /// </summary>
        /// <returns></returns>
        private bool IsDefineLabel()
        {
            return defineStack.Count > 0 && operands[0][operands[0].Length - 1] == ':';
        }

        /// <summary>
        /// Разворачивает вызов макро команды.
        /// </summary>
        private void StartExpand()
        {
            var command = (MacroCommand)commands.Find(x => x.Name == operands[0]).Clone();

            // Простая проверка на рекурсию
            foreach (MacroCommand _command in expandStack)
            {
                if (_command.Name == command.Name)
                {
                    throw new Exception(String.Format("Бесконечная рекурсия на строке {0}.", position));
                }
            }

            command.CallPosition = position;

            for (int i = 1; i < operands.Count; i++)
            {
                command.FactParams.Add(operands[i]);
            }

            for (int i = 0; i < command.Labels.Count; i++)
            {
                command.Labels[command.Labels.ElementAt(i).Key] = labelsCount++;
            }
            
            if (command.FactParams.Count != command.FormalParams.Count)
            {
                throw new Exception(String.Format("Неверное количество параметров на строке {0}\r\nГлубина вызова: {1}", position, expandStack.Count));
            }

            SetLine(command.StartPosition);
            expandStack.Push(command);
        }

        /// <summary>
        /// Убирает команду из стека и возвращает указатель на место откуда был совершен вызов.
        /// </summary>
        private void EndExpand()
        {
            var command = expandStack.Pop();
            SetLine(command.CallPosition + 1);
        }

        /// <summary>
        /// Создает экземпляр команды и помещает в стек команд.
        /// </summary>
        private void StartDefine()
        {
            var command = new MacroCommand();
            command.StartPosition = position + 1;
            command.Name = operands[0];

            for(int i = 2; i < operands.Count; i++)
            {
                command.FormalParams.Add(operands[i]);
            }

            defineStack.Push(command);
        }

        /// <summary>
        /// Достает команду из стека, указывает последнюю строку и помещает в список команд.
        /// </summary>
        private void EndDefind()
        {
            var command = defineStack.Pop();

            if (command == null)
            {
                throw new ApplicationException("Ошибка на строке " + position);
            }

            command.EndPosition = position - 1;

            commands.Add(command);
        }

        /// <summary>
        /// Вернет истину, в случае, если строка длиннее нуля и комментарий не в начале строки.
        /// </summary>
        /// <returns></returns>
        private bool IsCorrectLine()
        {
            return line.Length > 0 && line.IndexOfAny(commentChars) != 0;
        }

        /// <summary>
        /// Возвращает истину, если второй оператор = MACRO - начало макроопределения.
        /// </summary>
        /// <returns></returns>
        private bool IsStartDefine()
        {
            return operands.Count > 1 && operands[1] == "MACRO" && !commands.Exists(x => x.Name == operands[0]);
        }

        /// <summary>
        /// Возвращает истику, если первый оператор = MEND - окончание макроопределения.
        /// </summary>
        /// <returns></returns>
        private bool IsEndDefine()
        {
            return operands.Contains("MEND");
        }

        /// <summary>
        /// Возвращает истину, если название команды уже есть в списке команд.
        /// </summary>
        /// <returns></returns>
        private bool IsStartExpand()
        {
            return defineStack.Count == 0 && operands.Count > 0 && commands.Exists(x => x.Name == operands[0]);
        }

        /// <summary>
        /// Возвращает истину, если номер строки больше номера последней строки МК.
        /// </summary>
        /// <returns></returns>
        private bool IsEndExpand()
        {
            return expandStack.Count > 0 && position > expandStack.Peek().EndPosition;
        }

        /// <summary>
        /// Записывает текущую строку в выходной файл.
        /// </summary>
        private void WriteLine()
        {
            destFileSW.WriteLine(line);
        }
    }

    /// <summary>
    /// Класс макро комманды.
    /// </summary>
    public class MacroCommand
    {
        public string Name;

        /// <summary>
        /// Формальные параметры.
        /// </summary>
        public List<string> FormalParams = new List<string>();

        /// <summary>
        /// Фактические параметры.
        /// </summary>
        public List<string> FactParams = new List<string>();

        /// <summary>
        /// Метки внутри макроса.
        /// </summary>
        public Dictionary<string, int> Labels = new Dictionary<string, int>();

        public long StartPosition;
        public long EndPosition;
        public long CallPosition;

        public MacroCommand Clone()
        {
            var command = new MacroCommand();
            command.Name = this.Name;
            command.FormalParams = new List<string>(this.FormalParams);
            command.FactParams = new List<string>(this.FactParams);
            command.Labels = new Dictionary<string, int>(this.Labels);
            command.StartPosition = this.StartPosition;
            command.EndPosition = this.EndPosition;
            command.CallPosition = this.CallPosition;

            return command;
        }
    }
}