using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;


namespace RO_Project {
    public partial class Form1 : Form {

        //папка, куда сохраняются эталонные бинаризованные изображения(png)
        public string pngPath;

        //папка, куда сохраняются преобразованные в текстовый формат 
        //эталонные матрицы
        public string txtPath;

        //папка с правилами
        public string rulesPath;

        //объект для удобного сохранения картинки в файл
        MyImageSaver imageSaver;

       


        //конструктор формы
        public Form1() {

            InitializeComponent();

            imageToRecognize = null;

            string str = @"C:\Users\1\Documents\GitHub\RO_Project\RO_Project\bin\Debug\etalons\png\upperLetters";
            for (char letter = 'A'; letter <= 'Z'; ++letter) {
                Directory.CreateDirectory(str + "\\" + letter);
            }

            Console.WriteLine("Месторасположение эталонов: " + Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\etalons");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)+"\\etalons");
                pngPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\etalons" + "\\png";
                txtPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\etalons" + "\\txt";
                rulesPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\rules";
                Directory.CreateDirectory(pngPath);
                Directory.CreateDirectory(txtPath);
                Directory.CreateDirectory(rulesPath);
                
                imageSaver = new MyImageSaver(pngPath);
                //инициирую объект-распознаватель. Из него потом полочу результат
            }
            catch(Exception e)
            {
                Console.WriteLine("НЕОПРЕДЕЛЕНО МЕСТОПОЛОЖЕНИЕ ЭТАЛОНОВ И ПРАВИЛ!");
                Console.WriteLine(e.Message);
            }
            
        }

        //изображение для распознавания
        Bitmap imageToRecognize;

        //запуск распознавания
        private void StartRecognizing(object sender, EventArgs e) {

            if (imageToRecognize != null) {

                MyTextRecognizer recognizer = new MyTextRecognizer(txtPath, rulesPath);
                recognizer.ClearResult();
                //начинаю распознавание
                recognizer.Start(imageToRecognize);
                //результат распознавания. Получаю из объекта-распознавателя
                recognitionResultTextbox.Text = recognizer.GetResult();
            }
            else
                recognitionResultTextbox.Text = "ДОБАВЬТЕ ИЗОБРАЖЕНИЕ ДЛЯ РАСПОЗНАВАНИЯ: ВЫБЕРИТЕ ЧЕРЕЗ ПРОВОДНИК, ЛИБО ВСТАВЬТЕ КОМАНДОЙ CTRL+V";
        }

        //выбрать из проводника изображение для распознавания
        private void chooseImageFromExplorer(object sender, EventArgs e) {

            //настроика диалога
            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "png files (*.png)|*.png|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK) {

                //путь к файлу
                String filePath = openFileDialog.FileName;

                //считываем изображение из файла
                imageToRecognize = new Bitmap(filePath);

                pictureBox.Image = imageToRecognize;
                pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            }
        }

        //получить txt файлы для картинок с конкретными символами в папке
        private void CreateEtalonMatrix(object sender, EventArgs e) {

            //настроика диалога
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.SelectedPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            DialogResult result = folderBrowserDialog.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath)) {

                string[] symbolTypeDirectories = Directory.GetDirectories(folderBrowserDialog.SelectedPath);
                foreach(var symbolTypeDirectory in symbolTypeDirectories)
                {
                    string symbolTypeDirectoryName = Path.GetFileName(symbolTypeDirectory);
                    string[] directories = Directory.GetDirectories(symbolTypeDirectory);
                    foreach (var directory in directories)
                    {
                        //имя директории - имя символа, т.е. каждому символу своя директория
                        string directoryName = Path.GetFileName(directory);
                        //нужно получить список эталоных матриц 16x16 одного символа
                        List<double[,]> etalonsArrays = new List<double[,]>();
                        //по всем вариантам эталона данного символа проходимся...
                        string[] files = Directory.GetFiles(directory);
                        foreach (string file in files)
                        {
                            //считываем изображение из файла
                            System.Drawing.Bitmap image = new Bitmap(file);

                            //матрица эталонная для символа
                            var temp = MyEtalonLoader.CreateDoubleMatrixForEtalon(image);
                            etalonsArrays.Add(temp);
                           
                        }
                        Directory.CreateDirectory(txtPath + "\\" + symbolTypeDirectoryName);
                        MyArraySerializer.SerializeDoubleArray(MyEtalonLoader.GetAverageArrayForEtalon(etalonsArrays), MyTextRecognizer.ResizeWidth, MyTextRecognizer.ResizeHeight, new StreamWriter(txtPath + "\\" + symbolTypeDirectoryName + "\\" + directoryName + ".txt"));
                    }
                }
            }
        }
        
        //вычленить конкретные символы на выбранной(из проводника) картинке и получить их в виде отдельных картинок
        private void CreateEtalonImages(object sender, EventArgs e) {
            //настройка диалога
            openFileDialog.InitialDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            openFileDialog.Filter = "png files (*.png)|*.png|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK) {

                //путь к файлу
                String filePath = openFileDialog.FileName;

                //считываем изображение из файла
                System.Drawing.Bitmap image = new Bitmap(filePath);

                //сегментация картинки
                List<Bitmap> symbols = MyEtalonLoader.GetEtalonBitmaps(image);

                int count = 0;
                foreach (Bitmap symbol in symbols) {

                    imageSaver.Save(symbol, "symbol" + DateTime.Now.Millisecond+count);

                    count += 1;
                }
            }
            
        }

       

        private void handleKeyPress(object sender, KeyEventArgs e) {

            Console.WriteLine("keypress");

            if (e.Control == true && e.KeyCode == Keys.V) {

                Console.WriteLine("contrlv");

                if (Clipboard.ContainsImage()) {
                    imageToRecognize = new Bitmap(Clipboard.GetImage());
                    pictureBox.Image = imageToRecognize;
                    pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                }
            }
        }
    }
}
