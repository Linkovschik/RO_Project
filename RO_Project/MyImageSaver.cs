using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RO_Project {
    class MyImageSaver {

        System.Drawing.Imaging.Encoder myEncoder;
        EncoderParameter myEncoderParameter;
        EncoderParameters myEncoderParameters;
        ImageCodecInfo myImageCodecInfo;

        private string pngPath;
        //конструктор
        public MyImageSaver(string _pngPath) {

            myEncoder = System.Drawing.Imaging.Encoder.Quality;
            myImageCodecInfo = GetEncoderInfo("image/png");
            myEncoderParameters = new EncoderParameters(1);
            pngPath = _pngPath;
        }

        public void Save(Bitmap image, string name) {
            myEncoderParameter = new EncoderParameter(myEncoder, 75L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            image.Save(pngPath + "\\" + name + ".png", myImageCodecInfo, myEncoderParameters);
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType) {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j) {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
    }
}
