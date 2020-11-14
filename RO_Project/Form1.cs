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

        //объект-распознаватель эталонов
        MyEtalonRecognizer etalonRecognizer;

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

                //бинаризация изображения
                BinarizedImage binarizedImage = new BinarizedImage(image);

                //инициирую объект-распознаватель. Из него потом полочу результат
                recognizer = new MyTextRecognizer(txtPath, rulesPath);

                //начинаю распознавание
                recognizer.Start(binarizedImage);

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

                string[] files = Directory.GetFiles(folderBrowserDialog.SelectedPath);

                foreach (string file in files) {

                    //считываем изображение из файла
                    System.Drawing.Bitmap image = new Bitmap(file);

                    //произведем бинаризацию изображения
                    BinarizedImage binarizedImage = new BinarizedImage(image);
                    
                    //матрица эталонная для символа
                    byte[,] etalonArray = MyEtalonRecognizer.CreateMatrix_16X16(binarizedImage);

                    string fileName = Path.GetFileName(file);

                    fileName = fileName.Replace(".png","");

                    MyArraySerializer.SerializeArray(etalonArray, new StreamWriter(txtPath+"\\"+fileName + ".txt"));
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

                //бинаризация изображения
                BinarizedImage binarizedImage = new BinarizedImage(image);

                etalonRecognizer = new MyEtalonRecognizer(pngPath, txtPath);

                //сегментация картинки
                List<BinarizedImage> images = etalonRecognizer.GetBinarizedImages(binarizedImage);


                int count = 0;
                foreach (BinarizedImage symbolImage in images) {

                    imageSaver.Save(symbolImage.GetBitmap(), "symbol" + count);

                    count += 1;
                }
            }
            
        }
    }
}
