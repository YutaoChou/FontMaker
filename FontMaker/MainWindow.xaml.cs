using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Color = System.Drawing.Color;
using Path = System.IO.Path;
using Rectangle = System.Drawing.Rectangle;

namespace FontMaker
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private MyGenerateFonts myGenerateFonts = new MyGenerateFonts();

        private ObservableCollection<string> sysFonts = new ObservableCollection<string>();
        //打开文件;
        private Microsoft.Win32.OpenFileDialog openFileDialog;
        string font_modelPath = String.Empty;
        private List<Color> colors = new List<Color>();
        private Color BackGroundColor;
        private Color ForeGroundColor;
        private BackgroundWorker bgworker = null;

        public MainWindow()
        {
            InitializeComponent();
            InitSysFont();
            BackGroundColor = parseColor("ffffffff");
            ForeGroundColor = parseColor("ff000000");
        }

         private void InitSysFont()
        {
            System.Drawing.Text.InstalledFontCollection font = new System.Drawing.Text.InstalledFontCollection();
            System.Drawing.FontFamily[] array = font.Families;
            foreach (var v in array)
            {
                sysFonts.Add(v.Name);
            }
            fontType.ItemsSource = sysFonts;
            myGenerateFonts.isBold = false;
            myGenerateFonts.isAntiAliasing = false;
            myGenerateFonts.fontHeight = 16;
            myGenerateFonts.fontEncode = "GB2312";
            myGenerateFonts.fontRange = 1;
            myGenerateFonts.fontBpp = 1;
        }

        /// <summary>
        /// 字体大小修改
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fontHeightSel(object sender, SelectionChangedEventArgs e)
        {
            if (null != fontHeight.SelectedValue)
                myGenerateFonts.fontHeight = Convert.ToInt32(fontHeight.SelectedValue.ToString());
            reFreshFontPrewView();
        }

        private void fontEncodeSel(object sender, SelectionChangedEventArgs e)
        {
            if (null != fontEncode.SelectedValue)
            {
                myGenerateFonts.fontEncode = fontEncode.SelectedValue.ToString();
            }
            reFreshFontPrewView();
        }

        private void fontTypeSel(object sender, SelectionChangedEventArgs e)
        {
            if (null != fontType.SelectedValue)
                myGenerateFonts.fontName = fontType.SelectedValue.ToString();
            reFreshFontPrewView();
        }

        private void fontRangesel(object sender, SelectionChangedEventArgs e)
        {
            if (null != fontRange.SelectedValue && null != fonLabel && null != fontModelPath && null != fontModel)
            {
                if ("ASCII".Equals(fontRange.SelectedValue.ToString()))
                {
                    myGenerateFonts.fontRange = 0;
                    fontEncode.SelectedIndex = 0;
                    fonLabel.Visibility = Visibility.Hidden;
                    fontModelPath.Visibility = Visibility.Hidden;
                    fontModel.Visibility = Visibility.Hidden;

                }
                else if ("All".Equals(fontRange.SelectedValue.ToString()))
                {
                    myGenerateFonts.fontRange = 1;
                    fontEncode.SelectedIndex = 1;
                    fonLabel.Visibility = Visibility.Hidden;
                    fontModelPath.Visibility = Visibility.Hidden;
                    fontModel.Visibility = Visibility.Hidden;
                }
                else
                {
                    myGenerateFonts.fontRange = 2;
                    fonLabel.Visibility = Visibility.Visible;
                    fontModelPath.Visibility = Visibility.Visible;
                    fontModel.Visibility = Visibility.Visible;
                    fontEncode.SelectedValue = @"GB2312";
                }
                reFreshFontPrewView();
            }
        }

        private void cancle(object sender, RoutedEventArgs e)
        {
            //取消操作;
            //this.Hide();
            this.Close();
        }
        private void buildDIYGB2312()
        {
            Encoding encoding = FileEncode.GetType(font_modelPath);
            string fModles = System.IO.File.ReadAllText(font_modelPath, encoding);
            string[] font_Model = new string[fModles.Length];
            for (int i = 1; i < fModles.Length + 1; i++)
            {
                font_Model[i - 1] = fModles.Substring(i - 1, 1);
            }
            MyGenerateFonts tmp = myGenerateFonts.Clone();
            List<byte> data = drawGB2312BFont(font_Model, tmp);
            if (data.Count > 0)
            {
                //写出字库;
                BinaryWriter bw = null;
                try
                {
                    //获取Bin输出路径&写出Bin文件
                    bw = new BinaryWriter(new FileStream(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) 
                        + Path.DirectorySeparatorChar
                        + tmp.fontName + (2 == myGenerateFonts.fontRange ? @" GB2312" + Path.GetFileName(font_modelPath).Split('.')[0] + @" " : @" GB2312 ") + tmp.fontHeight.ToString()
                        + @" " + tmp.fontHeight + @" " + tmp.fontBpp + @".2font", FileMode.Create));
                    bw.Write(data.ToArray());
                }
                catch (IOException exe)
                {
                    Console.WriteLine(exe.StackTrace);
                }
                finally
                {
                    App.Current.Dispatcher.Invoke(new Action(delegate
                    {
                        ProgressBarInfo.Value = 100;
                        btnGenerateFont.IsEnabled = true;
                    }));
                    if (null != bw)
                        bw.Close();
                }
            }
        }

        private void buildGB2312()
        {
            MyGenerateFonts tmp = myGenerateFonts.Clone();
            List<byte> data = drawGB2312Font(BaseFontData.arrGB2312, tmp);
            if (data.Count > 0)
            {
                //写出字库;
                BinaryWriter bw = null;
                try
                {
                    //获取Bin输出路径&写出Bin文件
                    string sb = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                    bw = new BinaryWriter(new FileStream(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)
                        + Path.DirectorySeparatorChar
                        + Path.DirectorySeparatorChar + tmp.fontName + @" GB2312 " + tmp.fontHeight.ToString()
                        + @" " + tmp.fontHeight + @" " + tmp.fontBpp + @".2font", FileMode.Create));
                    bw.Write(data.ToArray());
                }
                catch (IOException exe)
                {
                    Console.WriteLine(exe.StackTrace);
                }
                finally
                {
                    App.Current.Dispatcher.Invoke(new Action(delegate
                    {
                        ProgressBarInfo.Value = 100;
                        btnGenerateFont.IsEnabled = true;
                    }));
                    if (null != bw)
                        bw.Close();
                }
            }
        }
        private void buildASCII()
        {
            MyGenerateFonts tmp = myGenerateFonts.Clone();
            List<byte> data = drawASCIIFont(BaseFontData.arrASCII, tmp);
            if (data.Count > 0)
            {
                //写出字库;
                BinaryWriter bw = null;
                try
                {
                    //获取Bin输出路径&写出Bin文件
                    bw = new BinaryWriter(new FileStream(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)
                        + Path.DirectorySeparatorChar
                        + tmp.fontName + @" ASCII " + tmp.fontHeight.ToString()
                        + @" " + tmp.fontHeight + @" " + tmp.fontBpp + @".2font", FileMode.Create));
                    bw.Write(data.ToArray());
                }
                catch (IOException exe)
                {
                    Console.WriteLine(exe.StackTrace);
                }
                finally
                {
                    App.Current.Dispatcher.Invoke(new Action(delegate
                    {
                        ProgressBarInfo.Value = 100;
                        btnGenerateFont.IsEnabled = true;
                    }));
                    if (null != bw)
                        bw.Close();
                }
            }
        }

        //画GB2312字并写入到文件;
        private List<byte> drawGB2312Font(string[,] text, MyGenerateFonts tmp)
        {
            using (Bitmap bmp = new Bitmap(tmp.fontHeight * 16, tmp.fontHeight * 522))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    return DrawChar2312(g, tmp, text);
                    //MainWindow.Instance.GetLog().Info("Bild");
                    /*
                    List<byte> d = DrawChar2312(g, tmp, text);
                    bmp.Save(@"C:\Users\YutaoZhou\Desktop\Bmp\GB2312.bmp");
                    return d;
                    */
                }

            }
        }

        //画GB2312字并写入到文件;
        private List<byte> drawGB2312BFont(string[] text, MyGenerateFonts tmp)
        {
            using (Bitmap bmp = new Bitmap(tmp.fontHeight * 16, tmp.fontHeight * (text.Length % 16 > 0 ? text.Length / 16 + 1 : text.Length / 16)))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    return DrawChar2312B(g, tmp, text);
                }
            }
        }

        //画ASCII字并写入到文件;
        private List<byte> drawASCIIFont(string[,] text, MyGenerateFonts tmp)
        {
            using (Bitmap bmp = new Bitmap(tmp.fontHeight * 16, tmp.fontHeight * 7))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    return DrawCharASCII(g, tmp, text);
                }
            }
        }

        private void generateFont(object sender, RoutedEventArgs e)
        {
            btnGenerateFont.IsEnabled = false;
            //生成字库;
            if ("ASCII".Equals(myGenerateFonts.fontEncode))
            {
                Task.Factory.StartNew(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    buildASCII();
                });
            }
            else
            {
                //自定义下2312位置;
                if (2 == myGenerateFonts.fontRange)
                {
                    Task.Factory.StartNew(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        buildDIYGB2312();
                    });
                }
                else
                {
                    Task.Factory.StartNew(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        buildGB2312();
                    });
                }
            }
        }
        /// <summary>
        ///改变进度条的值
        /// </summary>
        private void BgworkChange(object sender, ProgressChangedEventArgs e)
        {
            this.ProgressBarInfo.Value = e.ProgressPercentage;
        }

        private void reFreshFontPrewView()
        {
            if (null == myGenerateFonts.fontName || null == myGenerateFonts.fontEncode)
                return;
            //画字;
            using (Bitmap bmp = new Bitmap(myGenerateFonts.fontHeight, myGenerateFonts.fontHeight))
            {
                //图片抗锯齿处理;
                Rectangle szt;
                Rectangle rBase = new Rectangle(0, 0, myGenerateFonts.fontHeight, myGenerateFonts.fontHeight);
                Color[] color8 = new Color[8];
                if (2 == myGenerateFonts.fontBpp)
                {

                }
                else if (4 == myGenerateFonts.fontBpp)
                {

                }
                using (Graphics frame = Graphics.FromImage(bmp))
                {
                    DrawChar(frame, myGenerateFonts);
                }
                //测试------------------------------------------------------------------------------------------------;
                List<byte> data = new List<byte>();
                szt = GetFixedWidthRect(bmp, rBase, BackGroundColor);
                data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Width)));
                data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Height)));
                Array.Clear(color8, 0, color8.Length);
                for (int h = 0; h < myGenerateFonts.fontHeight; h++)
                {
                    for (int w = 0; w <= myGenerateFonts.fontHeight - 8; w += 8)
                    {
                        color8[0] = bmp.GetPixel(w, h);
                        color8[1] = bmp.GetPixel(w + 1, h);
                        color8[2] = bmp.GetPixel(w + 2, h);
                        color8[3] = bmp.GetPixel(w + 3, h);
                        color8[4] = bmp.GetPixel(w + 4, h);
                        color8[5] = bmp.GetPixel(w + 5, h);
                        color8[6] = bmp.GetPixel(w + 6, h);
                        color8[7] = bmp.GetPixel(w + 7, h);
                        if (1 == myGenerateFonts.fontBpp)
                            get0BitPix(data, color8);
                        else if (2 == myGenerateFonts.fontBpp)
                            get2BitPix(data, color8);
                        else
                            get4BitPix(data, color8);
                        Array.Clear(color8, 0, color8.Length);
                    }
                }
                /*
                BinaryWriter bw = null;
                try
                {
                    //获取Bin输出路径&写出Bin文件
                    bw = new BinaryWriter(new FileStream(@"C:\Users\YutaoZhou\Desktop\Bmp\Compress.DZK", FileMode.Create));
                    bw.Write(data.ToArray());
                }
                catch (IOException exe)
                {
                    Console.WriteLine(exe.StackTrace);
                }
                finally
                {
                    if (null != bw)
                        bw.Close();
                }
                */
                //-------------------------------------------------------------------------------------------------------
                //重画到图片上;
                Bitmap bmpZoom = new Bitmap(100, 100);
                Rectangle rect = new Rectangle(0, 0, myGenerateFonts.fontHeight, myGenerateFonts.fontHeight);
                Rectangle destRect;

                destRect = new Rectangle(new System.Drawing.Point(0, 0), new System.Drawing.Size(100, 100));

                using (Graphics g = Graphics.FromImage(bmpZoom))
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.DrawImage(ToGray(bmp), destRect, rect, GraphicsUnit.Pixel);
                }
                fontPreviewCV.Source = BitmapToBitmapImage(bmp);
                fontPreviewCV2.Source = BitmapToBitmapImage(Magnifier(bmp, bmp.Height > 40 ? 1 : 2));
            }
        }

        public Bitmap Magnifier(Bitmap srcbitmap, int multiple)
        {
            if (multiple <= 0) { multiple = 0; return srcbitmap; }
            Bitmap bitmap = new Bitmap(srcbitmap.Size.Width * multiple, srcbitmap.Size.Height * multiple);
            BitmapData srcbitmapdata = srcbitmap.LockBits(new Rectangle(new System.Drawing.Point(0, 0), srcbitmap.Size), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            BitmapData bitmapdata = bitmap.LockBits(new Rectangle(new System.Drawing.Point(0, 0), bitmap.Size), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* srcbyte = (byte*)(srcbitmapdata.Scan0.ToPointer());
                byte* sourcebyte = (byte*)(bitmapdata.Scan0.ToPointer());
                for (int y = 0; y < bitmapdata.Height; y++)
                {
                    for (int x = 0; x < bitmapdata.Width; x++)
                    {
                        long index = (x / multiple) * 4 + (y / multiple) * srcbitmapdata.Stride;
                        sourcebyte[0] = srcbyte[index];
                        sourcebyte[1] = srcbyte[index + 1];
                        sourcebyte[2] = srcbyte[index + 2];
                        sourcebyte[3] = srcbyte[index + 3];
                        sourcebyte += 4;
                    }
                }
            }
            srcbitmap.UnlockBits(srcbitmapdata);
            bitmap.UnlockBits(bitmapdata);
            return bitmap;
        }

        public static BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png); // 坑点：格式选Bmp时，不带透明度
                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }
        }

        private void DrawChar(Graphics g, MyGenerateFonts settings)
        {
            //美化，没软用;

            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = settings.isAntiAliasing ? TextRenderingHint.AntiAlias : TextRenderingHint.SingleBitPerPixel;
            g.Clear(BackGroundColor);
            Bitmap rect = new Bitmap(settings.fontHeight, settings.fontHeight);
            Rectangle rBase = new Rectangle(0, 0, settings.fontHeight, settings.fontHeight);
            Rectangle szt;
            Font font = new Font(new System.Drawing.FontFamily(settings.fontName), getPtbyPx(settings.fontHeight), settings.isBold ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);
            Font ord = font;
            //GB2312 对应位置;
            for (int x = 0; x < 45; x++)
            {
                TextRenderer.DrawText(g,
                settings.fontRange == 0 ? @"R" : @"富",
                font,
                rBase,
                ForeGroundColor,
                BackGroundColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping
                );
                rect = ToGray(rect);
                szt = GetFixedWidthRect(rect, rBase, BackGroundColor);
                //float f = font.Size;
                if ((szt.Width < settings.fontHeight && szt.Width >= settings.fontHeight - 1) || (szt.Height < settings.fontHeight && szt.Height >= settings.fontHeight - 1))
                {
                    break;
                }
                else if ((szt.Width >= settings.fontHeight - 1) || (szt.Height >= settings.fontHeight - 1))
                {
                    font = new Font(font.FontFamily, font.Size - 0.35f, font.Style, font.Unit);
                    TextRenderer.DrawText(g,
                        settings.fontRange == 0 ? @"R" : @"富",
                        font,
                        rBase,
                        ForeGroundColor,
                        BackGroundColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping
                    );
                    break;
                }
                else if (x == 44 && (szt.Width < settings.fontHeight - 1) || (szt.Height < settings.fontHeight - 1))
                {
                    font = new System.Drawing.Font(font.FontFamily, font.Size - 0.35f, font.Style, font.Unit);
                    TextRenderer.DrawText(g,
                        settings.fontRange == 0 ? @"R" : @"富",
                        font,
                        rBase,
                        ForeGroundColor,
                        BackGroundColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping
                    );
                    break;
                }
                else
                {
                    font = new Font(font.FontFamily, font.Size + 0.35f, font.Style, font.Unit);
                }
            }
        }

        public String ToSBC(String input)
        {
            // 半角转全角：
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 32)
                {
                    c[i] = (char)12288;
                    continue;
                }
                if (c[i] < 127)
                    c[i] = (char)(c[i] + 65248);
            }
            return new String(c);
        }

        //画GB2312B字库;
        private List<byte> DrawChar2312B(Graphics g, MyGenerateFonts settings, string[] text)
        {
            //美化，没软用;
            List<byte> data = new List<byte>();
            Color[] color8 = new Color[8];
            //data.Clear();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.TextRenderingHint = settings.isAntiAliasing ? TextRenderingHint.AntiAliasGridFit : TextRenderingHint.SingleBitPerPixel;
            g.Clear(Color.Transparent);
            System.Drawing.Font font = new System.Drawing.Font(new System.Drawing.FontFamily(settings.fontName), getPtbyPx(settings.fontHeight), settings.isBold ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);
            System.Drawing.Font fontOrdinal = font;

            //单个字;
            Bitmap rect = new Bitmap(settings.fontHeight, settings.fontHeight);

            using (Graphics t = Graphics.FromImage(rect))
            {
                t.SmoothingMode = SmoothingMode.AntiAlias;
                t.InterpolationMode = InterpolationMode.HighQualityBicubic;
                t.CompositingQuality = CompositingQuality.HighQuality;
                t.TextRenderingHint = settings.isAntiAliasing ? TextRenderingHint.AntiAliasGridFit : TextRenderingHint.SingleBitPerPixel;
                t.Clear(BackGroundColor);
                Rectangle szt;
                System.Drawing.Size ss;
                Rectangle rBase = new Rectangle(0, 0, settings.fontHeight, settings.fontHeight);
                for (int c = 0; c < text.Length; c++)
                {
                    //画字;
                    //Console.WriteLine(text[c]);
                    font = fontOrdinal;
                    //GB2312 对应位置;
                    if(c%2==0)
                        App.Current.Dispatcher.Invoke(new Action(delegate
                        {
                            ProgressBarInfo.Value = (int)Math.Ceiling(c / text.Length * 100.0);
                        }));

                    for (int x = 0; x < 45; x++)
                    {
                        TextRenderer.DrawText(t,
                        text[c],
                        font,
                        rBase,
                        ForeGroundColor,
                        BackGroundColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping
                        );
                        rect = ToGray(rect);
                        szt = GetFixedWidthRect(rect, rBase, BackGroundColor);
                        //float f = font.Size;
                        if ((szt.Width < settings.fontHeight && szt.Width >= settings.fontHeight - 1) || (szt.Height < settings.fontHeight && szt.Height >= settings.fontHeight - 1))
                        {
                            if (Encoding.GetEncoding("GB2312").GetBytes(text[c]).Length == 1)
                            {
                                data.AddRange(Encoding.GetEncoding("GB2312").GetBytes(ToSBC(text[c])).Reverse());
                            }
                            else
                                data.AddRange(Encoding.GetEncoding("GB2312").GetBytes(text[c]).Reverse());
                            data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Width)));
                            data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Height)));
                            break;
                        }
                        else if ((szt.Width >= settings.fontHeight - 1) || (szt.Height >= settings.fontHeight - 1))
                        {
                            font = new System.Drawing.Font(font.FontFamily, font.Size - 0.35f, font.Style, font.Unit);
                            TextRenderer.DrawText(t,
                            text[c],
                            font,
                            rBase,
                            ForeGroundColor,
                            BackGroundColor,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping
                            );
                            rect = ToGray(rect);
                            szt = GetFixedWidthRect(rect, rBase, BackGroundColor);
                            if (Encoding.GetEncoding("GB2312").GetBytes(text[c]).Length == 1)
                            {
                                data.AddRange(Encoding.GetEncoding("GB2312").GetBytes(ToSBC(text[c])).Reverse());
                            }
                            else
                                data.AddRange(Encoding.GetEncoding("GB2312").GetBytes(text[c]).Reverse());
                            data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Width)));
                            data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Height)));
                            break;
                        }
                        else if (x == 44 && ((szt.Width < settings.fontHeight - 1) || (szt.Height < settings.fontHeight - 1)))
                        {
                            TextRenderer.DrawText(t,
                            text[c],
                            font,
                            rBase,
                            ForeGroundColor,
                            BackGroundColor,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping
                            );
                            rect = ToGray(rect);
                            szt = GetFixedWidthRect(rect, rBase, BackGroundColor);
                            if (Encoding.GetEncoding("GB2312").GetBytes(text[c]).Length == 1)
                            {
                                data.AddRange(Encoding.GetEncoding("GB2312").GetBytes(ToSBC(text[c])).Reverse());
                            }
                            else
                                data.AddRange(Encoding.GetEncoding("GB2312").GetBytes(text[c]).Reverse());
                            data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Width)));
                            data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Height)));
                            break;
                        }
                        else
                        {
                            font = new System.Drawing.Font(font.FontFamily, font.Size + 0.35f, font.Style, font.Unit);
                        }
                    }
                    //低位在前;

                    //高位在前;
                    //data.AddRange(Encoding.GetEncoding("GB2312").GetBytes(text[c]));
                    //data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Width)));
                    //data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Height)));
                    Array.Clear(color8, 0, color8.Length);
                    for (int h = 0; h < settings.fontHeight; h++)
                    {
                        for (int w = 0; w <= settings.fontHeight - 8; w += 8)
                        {
                            if (1 == settings.fontBpp)
                            {
                                color8[0] = rect.GetPixel(w, h);
                                color8[1] = rect.GetPixel(w + 1, h);
                                color8[2] = rect.GetPixel(w + 2, h);
                                color8[3] = rect.GetPixel(w + 3, h);
                                color8[4] = rect.GetPixel(w + 4, h);
                                color8[5] = rect.GetPixel(w + 5, h);
                                color8[6] = rect.GetPixel(w + 6, h);
                                color8[7] = rect.GetPixel(w + 7, h);
                                get0BitPix(data, color8);
                            }
                            else if (2 == settings.fontBpp)
                            {
                                color8[0] = rect.GetPixel(w, h);
                                color8[1] = rect.GetPixel(w + 1, h);
                                color8[2] = rect.GetPixel(w + 2, h);
                                color8[3] = rect.GetPixel(w + 3, h);
                                color8[4] = rect.GetPixel(w + 4, h);
                                color8[5] = rect.GetPixel(w + 5, h);
                                color8[6] = rect.GetPixel(w + 6, h);
                                color8[7] = rect.GetPixel(w + 7, h);
                                get2BitPix(data, color8);
                            }
                            else
                            {
                                color8[0] = rect.GetPixel(w, h);
                                color8[1] = rect.GetPixel(w + 1, h);
                                color8[2] = rect.GetPixel(w + 2, h);
                                color8[3] = rect.GetPixel(w + 3, h);
                                color8[4] = rect.GetPixel(w + 4, h);
                                color8[5] = rect.GetPixel(w + 5, h);
                                color8[6] = rect.GetPixel(w + 6, h);
                                color8[7] = rect.GetPixel(w + 7, h);
                                get4BitPix(data, color8);
                            }
                            Array.Clear(color8, 0, color8.Length);
                        }
                    }
                    //画到图片;
                    g.DrawImage(rect, new Rectangle(settings.fontHeight * (c % 16), settings.fontHeight * (c / 16), settings.fontHeight, settings.fontHeight));
                    t.Clear(BackGroundColor);
                }
            }
            return data;
        }

        //画GB2312字库;
        private List<byte> DrawChar2312(Graphics g, MyGenerateFonts settings, string[,] text)
        {
            //美化，没软用;
            List<byte> data = new List<byte>();
            Color[] color8 = new Color[8];
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = settings.isAntiAliasing ? TextRenderingHint.AntiAliasGridFit : TextRenderingHint.SingleBitPerPixel;
            g.Clear(Color.Transparent);
            Font font = new Font(new System.Drawing.FontFamily(settings.fontName), getPtbyPx(settings.fontHeight), settings.isBold ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);
            Font fontOrdinal = font;

            //单个字;
            Bitmap rect = new Bitmap(settings.fontHeight, settings.fontHeight);

            using (Graphics t = Graphics.FromImage(rect))
            {
                t.SmoothingMode = SmoothingMode.HighQuality;
                t.InterpolationMode = InterpolationMode.NearestNeighbor;
                t.CompositingQuality = CompositingQuality.HighQuality;
                t.PixelOffsetMode = PixelOffsetMode.HighQuality;
                t.TextRenderingHint = settings.isAntiAliasing ? TextRenderingHint.AntiAliasGridFit : TextRenderingHint.SingleBitPerPixel;
                t.Clear(BackGroundColor);
                Rectangle szt;
                System.Drawing.Size ss;
                Rectangle rBase = new Rectangle(0, 0, settings.fontHeight, settings.fontHeight);
                for (int i = 0; i < 522; i++)
                {
                    if(i%6==0)
                        App.Current.Dispatcher.Invoke(new Action(delegate
                        {
                            ProgressBarInfo.Value = (int)Math.Ceiling(i / 522.0 * 100);
                        }));
                    for (int j = 0; j < 16; j++)
                    {
                        if (i % 6 == 0 && j == 0)
                            continue;
                        if ((i + 1) % 6 == 0 && j == 15 && i != 0)
                            continue;
                        //画字;
                        font = fontOrdinal;

                        for (int x = 0; x < 45; x++)
                        {
                            TextRenderer.DrawText(t,
                            text[i, j],
                            font,
                            rBase,
                            ForeGroundColor,
                            BackGroundColor,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping
                            );
                            rect = ToGray(rect);
                            szt = GetFixedWidthRect(rect, rBase, BackGroundColor);
                            //float f = font.Size;
                            if (i < 90 || (szt.Width < settings.fontHeight && szt.Width >= settings.fontHeight - 1) || (szt.Height < settings.fontHeight && szt.Height >= settings.fontHeight - 1))
                            {
                                data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Width)));
                                data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Height)));
                                break;
                            }
                            else if ((szt.Width >= settings.fontHeight - 1) || (szt.Height >= settings.fontHeight - 1))
                            {
                                font = new Font(font.FontFamily, font.Size - 0.35f, font.Style, font.Unit);
                                TextRenderer.DrawText(t,
                                text[i, j],
                                font,
                                rBase,
                                ForeGroundColor,
                                BackGroundColor,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping
                                );
                                rect = ToGray(rect);
                                szt = GetFixedWidthRect(rect, rBase, BackGroundColor);
                                data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Width)));
                                data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Height)));
                                break;
                            }
                            else if (x == 44 && ((szt.Width < settings.fontHeight - 1) || (szt.Height < settings.fontHeight - 1)))
                            {
                                TextRenderer.DrawText(t,
                                text[i, j],
                                font,
                                rBase,
                                ForeGroundColor,
                                BackGroundColor,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping
                                );
                                rect = ToGray(rect);
                                szt = GetFixedWidthRect(rect, rBase, BackGroundColor);
                                data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Width)));
                                data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Height)));
                                break;
                            }
                            else
                            {
                                font = new Font(font.FontFamily, font.Size + 0.35f, font.Style, font.Unit);
                            }
                        }
                        Array.Clear(color8, 0, color8.Length);
                        for (int h = 0; h < settings.fontHeight; h++)
                        {
                            for (int w = 0; w <= settings.fontHeight - 8; w += 8)
                            {
                                if (1 == settings.fontBpp)
                                {
                                    color8[0] = rect.GetPixel(w, h);
                                    color8[1] = rect.GetPixel(w + 1, h);
                                    color8[2] = rect.GetPixel(w + 2, h);
                                    color8[3] = rect.GetPixel(w + 3, h);
                                    color8[4] = rect.GetPixel(w + 4, h);
                                    color8[5] = rect.GetPixel(w + 5, h);
                                    color8[6] = rect.GetPixel(w + 6, h);
                                    color8[7] = rect.GetPixel(w + 7, h);
                                    get0BitPix(data, color8);
                                }
                                else if (2 == settings.fontBpp)
                                {
                                    color8[0] = rect.GetPixel(w, h);
                                    color8[1] = rect.GetPixel(w + 1, h);
                                    color8[2] = rect.GetPixel(w + 2, h);
                                    color8[3] = rect.GetPixel(w + 3, h);
                                    color8[4] = rect.GetPixel(w + 4, h);
                                    color8[5] = rect.GetPixel(w + 5, h);
                                    color8[6] = rect.GetPixel(w + 6, h);
                                    color8[7] = rect.GetPixel(w + 7, h);
                                    get2BitPix(data, color8);
                                }
                                else
                                {
                                    color8[0] = rect.GetPixel(w, h);
                                    color8[1] = rect.GetPixel(w + 1, h);
                                    color8[2] = rect.GetPixel(w + 2, h);
                                    color8[3] = rect.GetPixel(w + 3, h);
                                    color8[4] = rect.GetPixel(w + 4, h);
                                    color8[5] = rect.GetPixel(w + 5, h);
                                    color8[6] = rect.GetPixel(w + 6, h);
                                    color8[7] = rect.GetPixel(w + 7, h);
                                    get4BitPix(data, color8);
                                }
                                Array.Clear(color8, 0, color8.Length);
                            }
                        }
                        //画到图片;
                        g.DrawImage(rect, new Rectangle(settings.fontHeight * j, settings.fontHeight * i, settings.fontHeight, settings.fontHeight));
                        t.Clear(BackGroundColor);
                    }
                }
            }
            return data;
        }
        //字符串转颜色;
        public static Color parseColor(string value)
        {
            value = value.PadLeft(6, '0');
            if (value.Length == 7)
            {
                value = '0' + value;
            }
            else if (value.Length == 6)
            {
                value = "ff" + value;
            }
            return Color.FromArgb(
                Convert.ToInt16(value.Substring(0, 2), 16),
                Convert.ToInt16(value.Substring(2, 2), 16),
                Convert.ToInt16(value.Substring(4, 2), 16),
                Convert.ToUInt16(value.Substring(6), 16));
        }
        //计算大小;
        public static Rectangle GetFixedWidthRect(Bitmap bmp, Rectangle rect, Color backgroundColor)
        {
            Rectangle result = new Rectangle(rect.Location, rect.Size);
            int c, r, cc, rr, offset, offsety;
            bool isCatch = false;

            // right;
            offset = 0;
            for (c = rect.Right - 1; c > rect.Left; c--)
            {
                for (r = rect.Top; r <= rect.Bottom - 1; r++)
                {
                    if (!backgroundColor.Equals(bmp.GetPixel(c, r)))
                    {
                        isCatch = true;
                        break;
                    }
                }
                if (isCatch)
                {
                    break;
                }
                offset++;
            }
            offsety = 0;
            isCatch = false;
            //top;
            for (cc = rect.Top; cc < rect.Bottom - 1; cc++)
            {
                for (rr = rect.Left; rr <= rect.Right - 1; rr++)
                {
                    if (!backgroundColor.Equals(bmp.GetPixel(rr, cc)))
                    {
                        isCatch = true;
                        break;
                    }
                }
                if (isCatch)
                {
                    break;
                }
                offsety++;
            }
            //bottom;
            isCatch = false;
            for (cc = rect.Bottom - 1; cc > rect.Top; cc--)
            {
                for (rr = rect.Left; rr <= rect.Right - 1; rr++)
                {
                    if (!backgroundColor.Equals(bmp.GetPixel(rr, cc)))
                    {
                        isCatch = true;
                        break;
                    }
                }
                if (isCatch)
                {
                    break;
                }
                result.Offset(0, 1);
                offsety++;
            }

            // all blank
            if (result.Left == rect.Right || rect.Height < offsety)
            {
                return rect;
            }

            result.Size = new System.Drawing.Size(rect.Width - offset, rect.Height - offsety);

            return result;
        }

        private List<byte> DrawCharASCII(Graphics g, MyGenerateFonts settings, string[,] text)
        {
            //美化，没软用;
            List<byte> data = new List<byte>();
            Color[] color8 = new Color[8];
            //data.Clear();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.TextRenderingHint = settings.isAntiAliasing ? TextRenderingHint.AntiAliasGridFit : TextRenderingHint.SingleBitPerPixel;
            g.Clear(Color.Transparent);
            Font font = new Font(new System.Drawing.FontFamily(settings.fontName), getPtbyPx(settings.fontHeight), settings.isBold ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);
            Font fontOrdinal = font;
            //单个字;
            Bitmap rect = new Bitmap(myGenerateFonts.fontHeight, myGenerateFonts.fontHeight);

            using (Graphics t = Graphics.FromImage(rect))
            {
                t.SmoothingMode = SmoothingMode.AntiAlias;
                t.InterpolationMode = InterpolationMode.HighQualityBicubic;
                t.CompositingQuality = CompositingQuality.HighQuality;
                t.TextRenderingHint = settings.isAntiAliasing ? TextRenderingHint.AntiAliasGridFit : TextRenderingHint.SingleBitPerPixel;
                t.Clear(BackGroundColor);
                Rectangle szt;
                System.Drawing.Size ss;
                Rectangle rBase = new Rectangle(0, 0, settings.fontHeight, settings.fontHeight);
                for (int i = 0; i < 6; i++)
                {
                    App.Current.Dispatcher.Invoke(new Action(delegate
                    {
                        ProgressBarInfo.Value = (int)Math.Ceiling(i / 6.0 * 100);
                    }));
                    for (int j = 0; j < 16; j++)
                    {
                        font = fontOrdinal;
                        for (int x = 0; x < 10; x++)
                        {
                            ss = TextRenderer.MeasureText(text[i, j], font);
                            float f = font.Size;
                            if ((ss.Width <= settings.fontHeight && ss.Width >= settings.fontHeight - 1) || (ss.Height <= settings.fontHeight && ss.Height >= settings.fontHeight - 1))
                            {
                                break;
                            }
                            else
                            {
                                font = new Font(font.FontFamily, font.Size + 0.5f, font.Style, font.Unit);
                            }
                        }
                        //画字;
                        TextRenderer.DrawText(t,
                            text[i, j],
                            font,
                            rBase,
                            ForeGroundColor,
                            BackGroundColor,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping
                            );
                        rect = ToGray(rect);
                        szt = GetFixedWidthRect(rect, rBase, BackGroundColor);
                        data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Width)));
                        data.AddRange(BitConverter.GetBytes(Convert.ToUInt16(szt.Height)));
                        //灰度化,刷入颜色;
                        Array.Clear(color8, 0, color8.Length);
                        for (int h = 0; h < settings.fontHeight; h++)
                        {
                            for (int w = 0; w <= settings.fontHeight - 8; w += 8)
                            {
                                if (1 == settings.fontBpp)
                                {
                                    color8[0] = rect.GetPixel(w, h);
                                    color8[1] = rect.GetPixel(w + 1, h);
                                    color8[2] = rect.GetPixel(w + 2, h);
                                    color8[3] = rect.GetPixel(w + 3, h);
                                    color8[4] = rect.GetPixel(w + 4, h);
                                    color8[5] = rect.GetPixel(w + 5, h);
                                    color8[6] = rect.GetPixel(w + 6, h);
                                    color8[7] = rect.GetPixel(w + 7, h);
                                    get0BitPix(data, color8);
                                }
                                else if (2 == settings.fontBpp)
                                {
                                    color8[0] = rect.GetPixel(w, h);
                                    color8[1] = rect.GetPixel(w + 1, h);
                                    color8[2] = rect.GetPixel(w + 2, h);
                                    color8[3] = rect.GetPixel(w + 3, h);
                                    color8[4] = rect.GetPixel(w + 4, h);
                                    color8[5] = rect.GetPixel(w + 5, h);
                                    color8[6] = rect.GetPixel(w + 6, h);
                                    color8[7] = rect.GetPixel(w + 7, h);
                                    get2BitPix(data, color8);
                                }
                                else
                                {
                                    color8[0] = rect.GetPixel(w, h);
                                    color8[1] = rect.GetPixel(w + 1, h);
                                    color8[2] = rect.GetPixel(w + 2, h);
                                    color8[3] = rect.GetPixel(w + 3, h);
                                    color8[4] = rect.GetPixel(w + 4, h);
                                    color8[5] = rect.GetPixel(w + 5, h);
                                    color8[6] = rect.GetPixel(w + 6, h);
                                    color8[7] = rect.GetPixel(w + 7, h);
                                    get4BitPix(data, color8);
                                }
                                Array.Clear(color8, 0, color8.Length);
                            }
                        }
                        //画到图片;
                        t.Clear(BackGroundColor);
                    }
                }

            }
            return data;
        }

        #region 1BPP
        public static Bitmap GTo2Bit(Bitmap bmp)
        {
            if (bmp != null)
            {
                // 将源图像内存区域锁定
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

                // 获取图像参数
                int leng, offset_1bit = 0;
                int width = bmpData.Width;
                int height = bmpData.Height;
                int stride = bmpData.Stride;  // 扫描线的宽度,比实际图片要大
                int offset = stride - width;  // 显示宽度与扫描线宽度的间隙
                IntPtr ptr = bmpData.Scan0;   // 获取bmpData的内存起始位置的指针
                int scanBytesLength = stride * height;  // 用stride宽度，表示这是内存区域的大小
                if (width % 32 == 0)
                {
                    leng = width / 8;
                }
                else
                {
                    leng = width / 8 + (4 - (width / 8 % 4));
                    if (width % 8 != 0)
                    {
                        offset_1bit = leng - width / 8;
                    }
                    else
                    {
                        offset_1bit = leng - width / 8;
                    }
                }

                // 分别设置两个位置指针，指向源数组和目标数组
                int posScan = 0, posDst = 0;
                byte[] rgbValues = new byte[scanBytesLength];  // 为目标数组分配内存
                Marshal.Copy(ptr, rgbValues, 0, scanBytesLength);  // 将图像数据拷贝到rgbValues中
                // 分配二值数组
                byte[] grayValues = new byte[leng * height]; // 不含未用空间。
                // 计算二值数组
                int x, v, t = 0;
                for (int i = 0; i < height; i++)
                {
                    for (x = 0; x < width; x++)
                    {
                        v = rgbValues[posScan];
                        t = (t << 1) | (v > 100 ? 1 : 0);


                        if (x % 8 == 7)
                        {
                            grayValues[posDst] = (byte)t;
                            posDst++;
                            t = 0;
                        }
                        posScan++;
                    }

                    if ((x %= 8) != 7)
                    {
                        t <<= 8 - x;
                        grayValues[posDst] = (byte)t;
                    }
                    // 跳过图像数据每行未用空间的字节，length = stride - width * bytePerPixel
                    posScan += offset;
                    posDst += offset_1bit;
                }

                // 内存解锁
                Marshal.Copy(rgbValues, 0, ptr, scanBytesLength);
                bmp.UnlockBits(bmpData);  // 解锁内存区域

                // 构建1位二值位图
                Bitmap retBitmap = twoBit(grayValues, width, height);
                return retBitmap;
            }
            else
            {
                return null;
            }
        }
        //BPP1 位深度;
        private static Bitmap twoBit(byte[] rawValues, int width, int height)
        {
            // 新建一个1位二值位图，并锁定内存区域操作
            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format1bppIndexed);
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                 ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format1bppIndexed);

            // 计算图像参数
            int offset = bmpData.Stride - bmpData.Width / 8;        // 计算每行未用空间字节数
            IntPtr ptr = bmpData.Scan0;                         // 获取首地址
            int scanBytes = bmpData.Stride * bmpData.Height;    // 图像字节数 = 扫描字节数 * 高度
            byte[] grayValues = new byte[scanBytes];            // 为图像数据分配内存

            // 为图像数据赋值
            int posScan = 0;                        // rawValues和grayValues的索引
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < bmpData.Width / 8; j++)
                {
                    grayValues[posScan] = rawValues[posScan];
                    posScan++;
                }
                // 跳过图像数据每行未用空间的字节，length = stride - width * bytePerPixel
                posScan += offset;
            }

            // 内存解锁
            Marshal.Copy(grayValues, 0, ptr, scanBytes);
            bitmap.UnlockBits(bmpData);  // 解锁内存区域

            // 修改生成位图的索引表
            ColorPalette palette;
            // 获取一个Format8bppIndexed格式图像的Palette对象
            using (Bitmap bmp = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format1bppIndexed))
            {
                palette = bmp.Palette;
            }
            palette.Entries[0] = Color.FromArgb(255, 255, 255);
            palette.Entries[1] = Color.FromArgb(0, 0, 0);
            // 修改生成位图的索引表
            bitmap.Palette = palette;

            return bitmap;
        }
        #endregion

        //灰度化;
        public static Bitmap ToGray(Bitmap bmp)
        {
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    //获取该点的像素的RGB的颜色
                    Color color = bmp.GetPixel(i, j);
                    //利用公式计算灰度值
                    int gray = (int)(color.R * 0.3 + color.G * 0.59 + color.B * 0.11);
                    Color newColor = Color.FromArgb(gray, gray, gray);
                    bmp.SetPixel(i, j, newColor);
                }
            }
            return bmp;
        }
        //二值灰度;
        public static void get0BitPix(List<byte> data, Color[] colors)
        {
            var b = 0x00;
            foreach (Color c in colors)
            {
                float d = (float)c.B / 255;
                if (d <= 0.5)
                {
                    b += 0x01;
                }
                b = b << 1;
            }
            b = b >> 1;
            data.Add((byte)b);
        }
        public static void get2BitPix(List<byte> data, Color[] colors)
        {
            var d = 0x00000;
            foreach (Color color in colors)
            {
                float b = (float)color.B / 255;
                if (b >= 0 & b < 0.25)
                {
                    d += 0x03;
                    d = d << 2;
                }
                else if (b >= 0.25 && b < 0.5)
                {
                    d += 0x02;
                    d = d << 2;
                }
                else if (b >= 0.5 && b < 0.75)
                {
                    d += 0x01;
                    d = d << 2;
                }
                else if (b >= 0.75 && b <= 1)
                {
                    d = d << 2;
                }
            }
            d = d >> 2;
            d = d & 0xffff;
            data.Add(Convert.ToByte((d & 0xff00) >> 8));
            data.Add(Convert.ToByte((d & 0x00ff)));
        }

        public static void get4BitPix(List<byte> data, Color[] colors)
        {
            long d = 0x000000000;
            foreach (Color color in colors)
            {
                float b = (float)color.B / 255;
                if (b >= 0 & b < 0.065)
                {
                    d += 0x0f;
                    d = d << 4;
                }
                else if (b >= 0.065 && b < 0.125)
                {
                    d += 0x0e;
                    d = d << 4;
                }
                else if (b >= 0.125 && b < 0.1875)
                {
                    d += 0x0d;
                    d = d << 4;
                }
                else if (b >= 0.1875 && b <= 0.25)
                {
                    d += 0x0c;
                    d = d << 4;
                }
                if (b >= 0.25 & b < 0.3125)
                {
                    d += 0x0b;
                    d = d << 4;
                }
                else if (b >= 0.3125 && b < 0.375)
                {
                    d += 0x0a;
                    d = d << 4;
                }
                else if (b >= 0.375 && b < 0.4325)
                {
                    d += 0x09;
                    d = d << 4;
                }
                else if (b >= 0.4325 && b <= 0.5)
                {
                    d += 0x08;
                    d = d << 4;
                }
                if (b >= 0.5 & b < 0.5625)
                {
                    d += 0x07;
                    d = d << 4;
                }
                else if (b >= 0.5625 && b < 0.625)
                {
                    d += 0x06;
                    d = d << 4;
                }
                else if (b >= 0.625 && b < 0.6875)
                {
                    d += 0x05;
                    d = d << 4;
                }
                else if (b >= 0.6875 && b <= 0.75)
                {
                    d += 0x04;
                    d = d << 4;
                }
                if (b >= 0.75 & b < 0.8125)
                {
                    d += 0x03;
                    d = d << 4;
                }
                else if (b >= 0.8125 && b < 0.875)
                {
                    d += 0x02;
                    d = d << 4;
                }
                else if (b >= 0.875 && b < 0.9375)
                {
                    d += 0x01;
                    d = d << 4;
                }
                else if (b >= 0.9375 && b <= 1)
                {
                    d = d << 4;
                }
            }
            d = d >> 4;
            d = d & 0xffffffff;
            data.Add(Convert.ToByte((d & 0xff000000) >> 24));
            data.Add(Convert.ToByte((d & 0xff0000) >> 16));
            data.Add(Convert.ToByte((d & 0xff00) >> 8));
            data.Add(Convert.ToByte((d & 0x00ff)));
        }

        private int getPtbyPx(int px)
        {
            switch (px)
            {
                case 16:
                    return 12;
                case 24:
                    return 18;
                case 32:
                    return 24;
                case 40:
                    return 30;
                case 48:
                    return 36;
                case 56:
                    return 42;
                case 64:
                    return 48;
                case 72:
                    return 54;
                case 80:
                    return 60;
                case 88:
                    return 66;
                case 96:
                    return 72;
                default:
                    return 12;
            }
        }

        //浏览模板文件;
        private void bowserFont(object sender, RoutedEventArgs e)
        {
            if (openFileDialog == null)
            {
                openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Filter = "Font Model (*.txt)|*.txt";
            }
            if ((bool)openFileDialog.ShowDialog())
            {
                font_modelPath = openFileDialog.FileName;
                fontModelPath.Text = font_modelPath;
            }
        }

        private void antiCheck(object sender, RoutedEventArgs e)
        {
            //抗锯齿;
            if (isAntiAliasing.IsChecked == true)
                myGenerateFonts.isAntiAliasing = true;
            else
                myGenerateFonts.isAntiAliasing = false;
            reFreshFontPrewView();
        }

        private void boldCheck(object sender, RoutedEventArgs e)
        {
            if (isBold.IsChecked == true)
                myGenerateFonts.isBold = true;
            else
                myGenerateFonts.isBold = false;
            reFreshFontPrewView();
        }

        private void fontBppSel(object sender, SelectionChangedEventArgs e)
        {
            if ("2Bpp".Equals(fontBpp.SelectedValue.ToString()))
                myGenerateFonts.fontBpp = 2;
            else if ("4Bpp".Equals(fontBpp.SelectedValue.ToString()))
                myGenerateFonts.fontBpp = 4;
            else
                myGenerateFonts.fontBpp = 1;
            reFreshFontPrewView();
        }
    }

    class MyGenerateFonts
    {
        public string fontName { get; set; }
        public int fontHeight { get; set; }
        public string fontEncode { get; set; }
        public int fontBpp { get; set; }
        public bool isAntiAliasing { get; set; }
        public string fontType { get; set; }
        public int fontRange { get; set; }
        public bool isBold { get; set; }

        public MyGenerateFonts Clone()
        {
            MyGenerateFonts tmp = new MyGenerateFonts();
            tmp.fontName = this.fontName;
            tmp.fontHeight = this.fontHeight;
            tmp.fontEncode = this.fontEncode;
            tmp.fontBpp = this.fontBpp;
            tmp.isAntiAliasing = this.isAntiAliasing;
            tmp.fontType = this.fontType;
            tmp.fontRange = this.fontRange;
            tmp.isBold = this.isBold;
            return tmp;
        }
    }
}
