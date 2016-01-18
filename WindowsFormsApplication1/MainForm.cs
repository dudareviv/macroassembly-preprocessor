using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Application
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void openFileButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Файлы макроассемблера|*.masm|Все файлы (*.*)|*.*";
            fileDialog.Title = "Выберите файл макроассемблера";


            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                //System.IO.StreamReader sr = new System.IO.StreamReader(openFileDialog1.FileName);
                //fileNameField.Text = sr.ReadToEnd();
                //sr.Close();
                fileNameField.Text = fileDialog.FileName;
                this.runButton.Enabled = true;
            }
        }

        private void runButton_Click(object sender, EventArgs e)
        {
            try
            {
                var preprocessor = new Preprocessor(fileNameField.Text);
                preprocessor.Run();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }

            System.Windows.Forms.Application.Exit();
        }
    }
}
