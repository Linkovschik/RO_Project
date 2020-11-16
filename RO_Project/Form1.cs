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
        //эталонные матрицы (16*16)
        public string txtPath;

        //папка с правилами
        public string rulesPath;

        //объект для удобного сохранения картинки в файл
        MyImageSaver imageSaver;

        //объект - распознаватель
        MyTextRecognizer recognizer;


        //конструктор формы
        public Form1() {

            InitializeComponent();
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
                recognizer = new MyTextRecognizer(txtPath, rulesPath);
            }
            catch(Exception e)
            {
                Console.WriteLine("НЕОПРЕДЕЛЕНО МЕСТОПОЛОЖЕНИЕ ЭТАЛОНОВ И ПРАВИЛ!");
                Console.WriteLine(e.Message);
            }
            
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
                System.Drawing.Bitmap image = new Bitmap(filePath);
                recognizer.ClearResult();

                //начинаю распознавание
                recognizer.Start(image);

                //результат распознавания. Получаю из объекта-распознавателя
                recognitionResultTextbox.Text = recognizer.GetResult();
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
                            var temp = recognizer.CreateDoubleMatrix_16X16(image);
                            etalonsArrays.Add(temp);
                            for (int i = 0; i < temp.GetLength(0); ++i)
                            {
                                for (int j = 0; j < temp.GetLength(1); ++j)
                                {
                                    
                                    Console.Write(temp[i, j] + " ");
                                }
                                Console.Write("\n");
                            }
                        }
                        Directory.CreateDirectory(txtPath + "\\" + symbolTypeDirectoryName);
                        MyArraySerializer.SerializeDoubleArray(SumByteArrays(etalonsArrays), recognizer.ResizeWidth, recognizer.ResizeHeight, new StreamWriter(txtPath + "\\" + symbolTypeDirectoryName + "\\" + directoryName + ".txt"));
                    }
                }
               
            }
        }
        //функция для суммирования матриц эталонных изображений одного символа (используется только для этого пока что)
        private double[,] SumByteArrays(List<double[,]> arrays)
        {
            double[,] result = new double[arrays[0].GetLength(0), arrays[0].GetLength(1)];

            //записываем сумму в результат
            foreach (var array in arrays)
            {

                for (int i = 0; i < array.GetLength(0); ++i)
                {
                    for (int j = 0; j < array.GetLength(1); ++j)
                    {
                        result[i, j] += array[i, j];
                        //Console.Write(array[i, j] + " ");
                    }
                    //Console.Write("\n");
                }
                    
            }

            //делим на количество
            for (int i = 0; i < result.GetLength(0); ++i)
                for (int j = 0; j < result.GetLength(1); ++j)
                {
                    result[i, j] /= (double)arrays.Count;
                }

            return result;
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
                List<Symbol> symbols = recognizer.GetSymbols(image);

                int count = 0;
                foreach (Symbol symbol in symbols) {

                    imageSaver.Save(symbol.GetBitMap(), "symbol" + count);

                    count += 1;
                }
            }
            
        }

        
    }
}
