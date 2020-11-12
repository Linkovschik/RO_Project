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
        public static String pngPath = "C:\\Users\\1\\Desktop\\etalons\\png\\";

        //папка, куда сохраняются преобразованные в текстовый формат 
        //эталонные матрицы (16*16)
        public static String txtPath = "C:\\Users\\1\\Desktop\\etalons\\txt\\";

        //объект для удобного сохранения картинки в файл
        MyImageSaver imageSaver;

        //конструктор формы
        public Form1() {

            InitializeComponent();
            imageSaver = new MyImageSaver();
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

                //очистим полотно
                //canvas.Clear(Color.FromArgb(110, 110, 110));
                //отрисуем картинку на полотне
                //canvas.DrawImage(image, new PointF(20, 20));

                //бинаризация изображения
                BinarizedImage binarizedImage = new BinarizedImage(image);

                //отрисуем бинаризованную картинку на полотне
                //canvas.DrawImage(binarizedImageBitmap, new PointF(20, binarizedImageBitmap.Size.Height+40));
                //imageSaver.Save(binarizedImage.GetBitmap(), "binarizedImage.png");

                //моздаем объект - распознаватель
                MyTextRecognizer recognizer = new MyTextRecognizer();

                //разделение изображения на отдельные символы
                List<Symbol> symbols = MyTextRecognizer.GetSymbols(binarizedImage);

                //результат распознавания
                recognitionResultTextbox.Text = "";

                int count = 0;
                foreach (Symbol symbol in symbols) {

                    symbol.Print();
                    Console.WriteLine();

                    recognizer.RecognizeSymbol(symbol);

                    recognitionResultTextbox.Text = recognizer.;
                    count += 1;
                }
                        //

                //recognitionResultTextbox.Text = MyTextRecognizer.RecognizeSymbol(MyTextRecognizer.CreateMatrix_16X16(binarizedImage));
            }
        }

        //МЕТОДЫ, ИСПОЛЬЗУЕМЫЕ ДЛЯ СОЗДАНИЯ ЭТАЛОНОВ
        //----------------------------------------------------------------
        //получить txt файлы для картинок с конкретными символами в папке 
        private void CreateEtalonMatrix(object sender, EventArgs e) {

            //настроика диалога
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath)) {

                string[] files = Directory.GetFiles(folderBrowserDialog.SelectedPath);

                foreach (string file in files) {

                    //считываем изображение из файла
                    System.Drawing.Bitmap image = new Bitmap(file);

                    //произведем бинаризацию изображения
                    BinarizedImage binarizedImage = new BinarizedImage(image);
                    
                    //матрица эталонная для символа
                    byte[,] etalonArray = MyTextRecognizer.CreateMatrix_16X16(binarizedImage);

                    string fileName = Path.GetFileName(file);

                    fileName = fileName.Replace(".png","");

                    MyArraySerializer.SerializeArray(etalonArray, new StreamWriter(txtPath+fileName + ".txt"));
                }
            }
        }

        //вычленить конкретные символы на выбранной(из проводника) картинке
        //и получить их в виде отдельных картинок
        private void CreateEtalonImages(object sender, EventArgs e) {

            /*
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

                //сегментация картинки
                List<BinarizedImage> images = MyTextRecognizer.GetSymbolImages(binarizedImage);


                int count = 0;
                foreach (BinarizedImage symbolImage in images) {

                    imageSaver.Save(symbolImage.GetBitmap(), "symbol" + count);

                    count += 1;
                }
            }
            */
        }
        //----------------------------------------------------------------
    }
}
