using System;
using System.Drawing;
using ZXing;
using ZXing.Common;

namespace Shove.InformationCode
{
    /// <summary>
    /// 基于 com.google.zxing 的二维码封装
    /// </summary>
    public class QrCode
    {
        #region Create

        /// <summary>
        /// 生成二维码，保存为图片文件
        /// </summary>
        /// <param name="input">输入的需要编码的信息字符串</param>
        /// <param name="fromat">二维码格式</param>
        /// <param name="canvasWidth">画布宽度</param>
        /// <param name="canvasHeight">画布高度</param>
        /// <param name="outputFileName">保存图片的文件名</param>
        /// <param name="imageFormat">图像格式</param>
        public static void CreateCode(string input, BarcodeFormat fromat, int canvasWidth, int canvasHeight, string outputFileName, System.Drawing.Imaging.ImageFormat imageFormat)
        {
            CreateCode(input, fromat, canvasWidth, canvasHeight, outputFileName, imageFormat, "");
        }

        /// <summary>
        /// 生成二维码，保存为图片文件
        /// </summary>
        /// <param name="input">输入的需要编码的信息字符串</param>
        /// <param name="fromat">二维码格式</param>
        /// <param name="canvasWidth">画布宽度</param>
        /// <param name="canvasHeight">画布高度</param>
        /// <param name="outputFileName">保存图片的文件名</param>
        /// <param name="imageFormat">图像格式</param>
        /// <param name="logoImageFileName">中间嵌入的 Logo 图片</param>
        public static void CreateCode(string input, BarcodeFormat fromat, int canvasWidth, int canvasHeight, string outputFileName, System.Drawing.Imaging.ImageFormat imageFormat, string logoImageFileName)
        {
            Bitmap bmap = CreateCode(input, fromat, canvasWidth, canvasHeight, imageFormat, logoImageFileName);
            bmap.Save(outputFileName, imageFormat);
        }

        /// <summary>
        /// 生成二维码，返回位图
        /// </summary>
        /// <param name="input">输入的需要编码的信息字符串</param>
        /// <param name="fromat">二维码格式</param>
        /// <param name="canvasWidth">画布宽度</param>
        /// <param name="canvasHeight">画布高度</param>
        /// <param name="imageFormat">图像格式</param>
        /// <returns></returns>
        public static Bitmap CreateCode(string input, BarcodeFormat fromat, int canvasWidth, int canvasHeight, System.Drawing.Imaging.ImageFormat imageFormat)
        {
            return CreateCode(input, fromat, canvasWidth, canvasHeight, imageFormat, "");
        }

        /// <summary>
        /// 生成二维码，返回位图
        /// </summary>
        /// <param name="input">输入的需要编码的信息字符串</param>
        /// <param name="fromat">二维码格式</param>
        /// <param name="canvasWidth">画布宽度</param>
        /// <param name="canvasHeight">画布高度</param>
        /// <param name="imageFormat">图像格式</param>
        /// <param name="logoImageFileName">中间嵌入的 Logo 图片</param>
        /// <returns></returns>
        public static Bitmap CreateCode(string input, BarcodeFormat fromat, int canvasWidth, int canvasHeight, System.Drawing.Imaging.ImageFormat imageFormat, string logoImageFileName)
        {
            System.Collections.Hashtable hints = new System.Collections.Hashtable();
            hints.Add(EncodeHintType.CHARACTER_SET, "utf-8");

            BitMatrix byteMatrix = new MultiFormatWriter().encode(input, fromat, canvasWidth, canvasHeight, (System.Collections.Generic.IDictionary<ZXing.EncodeHintType, object>)hints);

            int x, y;
            int width = byteMatrix.Width;
            int height = byteMatrix.Height;

            Bitmap bmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (x = 0; x < width; x++)
            {
                for (y = 0; y < height; y++)
                {
                    //[shove] bmap.SetPixel(x, y, byteMatrix.get_Renamed(x, y) != -1 ? ColorTranslator.FromHtml("0xFF000000") : ColorTranslator.FromHtml("0xFFFFFFFF"));
                }
            }

            if (string.IsNullOrEmpty(logoImageFileName.Trim()) || (!System.IO.File.Exists(logoImageFileName)))
            {
                return bmap;
            }

            // 加图片水印
            System.Drawing.Image logo = System.Drawing.Image.FromFile(logoImageFileName);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmap);

            // 设置高质量插值法   
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            // 设置高质量,低速度呈现平滑程度   
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            // 清空画布并以透明背景色填充   
            //g.Clear(System.Drawing.Color.Transparent);

            // 计算 Logo 的范围
            x = (width - logo.Width) / 2;
            y = (height - logo.Height) / 2;

            //在指定位置并且按指定大小绘制原图片的指定部分
            g.DrawImage(logo, new System.Drawing.Rectangle(x, y, logo.Width, logo.Height), 0, 0, logo.Width, logo.Height, System.Drawing.GraphicsUnit.Pixel);

            logo.Dispose();
            g.Dispose();

            return bmap;
        }

        #endregion

        #region Read

        /// <summary>
        /// 从二维码图片读取二维码信息
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string ReadCode(string fileName)
        {
            Image img = Image.FromFile(fileName);

            return ReadCode(img);
        }

        /// <summary>
        /// 从二维码图片读取二维码信息
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static string ReadCode(Image img)
        {
            Bitmap bmap = new Bitmap(img);

            return ReadCode(bmap);
        }

        /// <summary>
        /// 从二维码图片读取二维码信息
        /// </summary>
        /// <param name="bmap"></param>
        /// <returns></returns>
        public static string ReadCode(Bitmap bmap)
        {
            if (bmap == null)
            {
                throw new Exception("Invalid code pictures.");
            }

            LuminanceSource source = null;//[shove] new RGBLuminanceSource(bmap, bmap.Width, bmap.Height);
            BinaryBitmap bitmap = new BinaryBitmap(new HybridBinarizer(source));
            Result result;

            result = new MultiFormatReader().decode(bitmap);
            // 如果要捕获异常，用 catch (ReaderException re)， re.ToString();

            return result.Text;
        }

        #endregion
    }
}
