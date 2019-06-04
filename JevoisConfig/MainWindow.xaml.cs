using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;

//EMGU
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.IO;
using System.Drawing.Imaging;
using System.Windows.Threading;
using System.Windows.Interop;
using Emgu.CV.CvEnum;

namespace JevoisConfig
{
    public partial class MainWindow : Window
    {
        private static VideoCapture cap;
        public static Mat imgOriginal = new Mat();
        public static Mat imageHSV = new Mat(imgOriginal.Size, DepthType.Cv8U, 3);

        private BitmapSource screenimage;

        private byte[] pixels;

        private String script1 = "";
        private String script2 = "";
        private String script3 = "";

        public MainWindow()
        {
            InitializeComponent();
            //On initialise la capture

            TextRange textRange = new TextRange(RTB_script.Document.ContentStart, RTB_script.Document.ContentEnd);
            textRange.Text = "";

            cap = new VideoCapture(0);
            int Codec = FourCC.MRLE; //Latency 64ms en 640-360 (plus haut pas possible)
            cap.SetCaptureProperty(CapProp.FourCC, Codec);

            cap.SetCaptureProperty(CapProp.AutoExposure, 1);
            cap.SetCaptureProperty(CapProp.FrameWidth, 640);
            cap.SetCaptureProperty(CapProp.FrameHeight, 360);

            //On enregistre l'event image capturée
            cap.ImageGrabbed += ProcessFrame;

            if (cap != null)
            {
                try
                {
                    cap.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void ProcessFrame(object sender, EventArgs e)
        {
            //On utilise un Invoke pour contourner les cross thread operations
            //Définition de l'action à réaliser dans l'Invoke
            try
            {
                Action action = () =>
                {
                    if (cap != null && cap.Ptr != IntPtr.Zero)
                    {
                        cap.Retrieve(imgOriginal, 0);
                        /*if (imgOriginal.Bitmap == null)
                        {
                            imgOriginal = CvInvoke.Imread("E:\\Users\\Jessy\\Pictures\\Annotation 2019-05-30 223115.png");
                        }*/
                        CvInvoke.CvtColor(imgOriginal, imageHSV, ColorConversion.Bgr2Hsv);
                        inimg.Source = Convert(imgOriginal.Bitmap);
                        Mat imgThreshCalibr = ColorCalibration(imgOriginal, imageHSV);
                        outimg.Source = Convert(imgThreshCalibr.Bitmap);
                    }
                };

                //Appel de l'action via le dispatcher
                var dispatcher = inimg.Dispatcher;
                if (dispatcher.CheckAccess())
                    action();
                else
                    dispatcher.Invoke(action);
            }
            finally
            {
                //cap.Stop();
                //cap.Dispose();

                //cap = new VideoCapture(System.Convert.ToByte(CamIndex.Text));
                //int Codec = FourCC.MRLE; //Latency 64ms en 640-360 (plus haut pas possible)
                //cap.SetCaptureProperty(CapProp.FourCC, Codec);

                ////cap.SetCaptureProperty(CapProp.AutoExposure, 1);
                //cap.SetCaptureProperty(CapProp.FrameWidth, 640);
                //cap.SetCaptureProperty(CapProp.FrameHeight, 360);

                ////On enregistre l'event image capturée
                //cap.ImageGrabbed += ProcessFrame;

                //if (cap != null)
                //{
                //    try
                //    {
                //        cap.Start();
                //    }
                //    catch (Exception ex)
                //    {
                //        MessageBox.Show(ex.Message);
                //    }
                //}
            }
        }

        private Mat ColorCalibration(Mat imgOriginal, Mat imageHSV)
        {
            Mat imgThresh = new Mat(imgOriginal.Size, DepthType.Cv8U, 1);
            try
            {
                
                Mat structuringElement = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));

                //// Détection du rouge dans l'image HSV

                if ((bool)!CB_Manual.IsChecked)
                {
                    CvInvoke.InRange(imageHSV,
                        new ScalarArray(new MCvScalar((byte)Hue.Value - System.Convert.ToByte(Errorhue.Text), (byte)Value.Value - System.Convert.ToByte(Errorsat.Text), (byte)Value.Value - System.Convert.ToByte(Errorval.Text))),
                        new ScalarArray(new MCvScalar((byte)Hue.Value + System.Convert.ToByte(Errorhue.Text), (byte)Saturation.Value + System.Convert.ToByte(Errorsat.Text), (byte)Value.Value + System.Convert.ToByte(Errorval.Text))),
                        imgThresh);
                }
                else
                {
                    CvInvoke.InRange(imageHSV,
                        new ScalarArray(new MCvScalar((byte)System.Convert.ToByte(hlow.Text), (byte)System.Convert.ToByte(slow.Text), (byte)System.Convert.ToByte(vlow.Text))),
                        new ScalarArray(new MCvScalar((byte)System.Convert.ToByte(hhigh.Text), (byte)System.Convert.ToByte(shigh.Text), (byte)System.Convert.ToByte(vhigh.Text))),
                        imgThresh);
                }

                //CvInvoke.Erode(imgThresh, imgThresh, structuringElement, new System.Drawing.Point(-1, -1), ConfigSlider.ErodeIteration_value, BorderType.Default, new MCvScalar(0, 0, 0));
                //CvInvoke.Dilate(imgThresh, imgThresh, structuringElement, new System.Drawing.Point(-1, -1), ConfigSlider.DilateIteration_value, BorderType.Default, new MCvScalar(0, 0, 0));

                return imgThresh;
            }
            catch
            {
                return imgThresh;
            }
        }

        //private void ObjectLocalisation(IInputOutputArray imgThresh, Mat imgOriginal)
        //{
        //    double x = 0, y = 0;

        //    Emgu.CV.Util.VectorOfVectorOfPoint contours = new Emgu.CV.Util.VectorOfVectorOfPoint();
        //    Mat hierarchy = new Mat();
        //    CvInvoke.FindContours(imgThresh, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

        //    double refArea = 0;
        //    bool objectFound = false;

        //    if (hierarchy.Width > 0)
        //    {
        //        int numObject = hierarchy.Width;
        //        if (numObject < 15)
        //        {
        //            Moments moments = new Moments();
        //            for (int index = 0; index < hierarchy.Width; index++)
        //            {
        //                moments = CvInvoke.Moments(contours[index]);
        //                double area = moments.M00;
        //                if (area > 400 && area < imgOriginal.Width * imgOriginal.Height / 1.5 && area > refArea)
        //                {
        //                    x = moments.M10 / area;
        //                    y = moments.M01 / area;
        //                    objectFound = true;
        //                    refArea = area;
        //                }
        //                else if (objectFound == true) { }
        //                else objectFound = false;
        //            }
        //            if (objectFound == true)
        //            {
        //                CvInvoke.Circle(imgOriginal, new System.Drawing.Point((int)x, (int)y), 100, new MCvScalar(0, 0, 255), 8);
        //            }
        //        }
        //    }
        //    CvInvoke.DrawContours(imgOriginal, contours, -1, new MCvScalar(0, 0, 255));
        //    //Coord.Content = "objet trouver en x= " + x.ToString() + " y= " + y.ToString();
        //}

        public BitmapImage Convert(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        private void OnClick(object sender, MouseEventArgs e)
        {
            screenimage = InteropHelper.CaptureRegion(InteropHelper.GetDesktopWindow(),
                                                                       (int)SystemParameters.VirtualScreenLeft,
                                                                       (int)SystemParameters.VirtualScreenTop,
                                                                       (int)SystemParameters.PrimaryScreenWidth,
                                                                       (int)SystemParameters.PrimaryScreenHeight);
            System.Drawing.Point _point = System.Windows.Forms.Control.MousePosition;
            System.Windows.Point point = new System.Windows.Point(_point.X, _point.Y);
            int stride = (screenimage.PixelWidth * screenimage.Format.BitsPerPixel + 7) / 8;
            pixels = new byte[screenimage.PixelHeight * stride];
            Int32Rect rect = new Int32Rect((int)point.X, (int)point.Y, 1, 1);
            screenimage.CopyPixels(rect, pixels, stride, 0);
            colorRec.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(pixels[2], pixels[1], pixels[0]));
            float[] hsvList = RGBtoHSV(pixels[2], pixels[1], pixels[0]);
            Hue.Value = (byte)(hsvList[0] * 255 / 360);
            Saturation.Value = (byte)(hsvList[1] * 255);
            Value.Value = (byte)(hsvList[2] * 255);
        }

        private float[] RGBtoHSV(byte Red, byte Green, byte Blue)
        {
            float R = (float)(Red / 255.0);
            float G = (float)(Green / 255.0);
            float B = (float)(Blue / 255.0);
            float H = 0f, S = 0f, V = 0f;
            V = Math.Max(Math.Max(R, G), B);
            if(V != 0)
            {
                S = (V - Math.Min(Math.Min(R, G), B)) / V;
            } 

            if (V == R)
            {
                H = 60 * (G - B) / (V - Math.Min(Math.Min(R, G), B));
            }

            if (V == G)
            {
                H = 120 + 60 * (B - R) / (V - Math.Min(Math.Min(R, G), B));
            }

            if (V == B)
            {
                H = 240 + 60 * (R - G) / (V - Math.Min(Math.Min(R, G), B));
            }

            if (H < 0)
            {
                H = H + 360;
            }
            float[] hsvList = new float[3]{H,S,V};
            return hsvList;
        }

        private byte[] HSVtoRGB(float hue, float saturation, float value)
        {
            float C = value * saturation;
            float X = C * (1 - Math.Abs((hue / 60) % 2 - 1));
            float m = value - C;

            float Rp = 0, Gp = 0, Bp = 0;


            if (hue < 60)
            {
                Rp = C;
                Gp = X;
                Bp = 0;
            }
            else if (hue < 120)
            {
                Rp = X;
                Gp = C;
                Bp = 0;
            }
            else if(hue < 180)
            {
                Rp = 0;
                Gp = C;
                Bp = X;
            }
            else if (hue < 240)
            {
                Rp = 0;
                Gp = X;
                Bp = C;
            }
            else if (hue < 300)
            {
                Rp = X;
                Gp = 0;
                Bp = C;
            }
            else if (hue < 360)
            {
                Rp = C;
                Gp = 0;
                Bp = X;
            }
            byte[] rgbList = new byte[3] { (byte)((Rp + m) * 255), (byte)((Gp + m) * 255), (byte)((Bp + m) * 255) };
            return rgbList;
        }

        private void writeScript(byte colorNumber)
        {
            if ((bool)!CB_Manual.IsChecked)
            {
                if (colorNumber == 1)
                {
                    script1 = "\rself.HsvVertMin = np.array([" + Math.Max(((byte)Hue.Value - System.Convert.ToByte(Errorhue.Text)), 0).ToString() + ", " + Math.Max(((byte)Saturation.Value - System.Convert.ToByte(Errorsat.Text)), 0).ToString() + ", " + Math.Max(((byte)Value.Value - System.Convert.ToByte(Errorval.Text)), 0).ToString() + "], dtype=np.uint8)\r";
                    script1 += "self.HsvVertMax = np.array([" + Math.Min(((byte)Hue.Value + System.Convert.ToByte(Errorhue.Text)), 255).ToString() + ", " + Math.Min(((byte)Saturation.Value + System.Convert.ToByte(Errorsat.Text)), 255).ToString() + ", " + Math.Min(((byte)Value.Value + System.Convert.ToByte(Errorval.Text)), 255).ToString() + "], dtype=np.uint8)\r";
                }

                if (colorNumber == 2)
                {
                    script2 = "\rself.HsvRougeMin = np.array([" + Math.Max(((byte)Hue.Value - System.Convert.ToByte(Errorhue.Text)), 0).ToString() + ", " + Math.Max(((byte)Saturation.Value - System.Convert.ToByte(Errorsat.Text)), 0).ToString() + ", " + Math.Max(((byte)Value.Value - System.Convert.ToByte(Errorval.Text)), 0).ToString() + "], dtype=np.uint8)\r";
                    script2 += "self.HsvRougeMax = np.array([" + Math.Min(((byte)Hue.Value + System.Convert.ToByte(Errorhue.Text)), 255).ToString() + ", " + Math.Min(((byte)Saturation.Value + System.Convert.ToByte(Errorsat.Text)), 255).ToString() + ", " + Math.Min(((byte)Value.Value + System.Convert.ToByte(Errorval.Text)), 255).ToString() + "], dtype=np.uint8)\r";
                }

                if (colorNumber == 3)
                {
                    script3 = "\rself.HsvBleuMin = np.array([" + Math.Max(((byte)Hue.Value - System.Convert.ToByte(Errorhue.Text)), 0).ToString() + ", " + Math.Max(((byte)Saturation.Value - System.Convert.ToByte(Errorsat.Text)), 0).ToString() + ", " + Math.Max(((byte)Value.Value - System.Convert.ToByte(Errorval.Text)), 0).ToString() + "], dtype=np.uint8)\r";
                    script3 += "self.HsvBleuMax = np.array([" + Math.Min(((byte)Hue.Value + System.Convert.ToByte(Errorhue.Text)), 255).ToString() + ", " + Math.Min(((byte)Saturation.Value + System.Convert.ToByte(Errorsat.Text)), 255).ToString() + ", " + Math.Min(((byte)Value.Value + System.Convert.ToByte(Errorval.Text)), 255).ToString() + "], dtype=np.uint8)\r";
                }
            }
            else
            {
                if (colorNumber == 1)
                {
                    script1 = "\rself.HsvVertMin = np.array([" + ((byte)System.Convert.ToByte(hlow.Text)).ToString() + ", " + ((byte)System.Convert.ToByte(slow.Text)).ToString() + ", " + ((byte)System.Convert.ToByte(vlow.Text)).ToString() + "], dtype=np.uint8)\r";
                    script1 += "self.HsvVertMax = np.array([" + ((byte)System.Convert.ToByte(hhigh.Text)).ToString() + ", " + ((byte)System.Convert.ToByte(shigh.Text)).ToString() + ", " + ((byte)System.Convert.ToByte(vhigh.Text)).ToString() + "], dtype=np.uint8)\r";
                }

                if (colorNumber == 2)
                {
                    script2 = "\rself.HsvRougeMin = np.array([" + ((byte)System.Convert.ToByte(hlow.Text)).ToString() + ", " + ((byte)System.Convert.ToByte(slow.Text)).ToString() + ", " +((byte)System.Convert.ToByte(vlow.Text)).ToString() + "], dtype=np.uint8)\r";
                    script2 += "self.HsvRougeMax = np.array([" + ((byte)System.Convert.ToByte(hhigh.Text)).ToString() + ", " + ((byte)System.Convert.ToByte(shigh.Text)).ToString() + ", " +((byte)System.Convert.ToByte(vhigh.Text)).ToString() + "], dtype=np.uint8)\r";
                }

                if (colorNumber == 3)
                {
                    script3 = "\rself.HsvBleuMin = np.array([" + ((byte)System.Convert.ToByte(hlow.Text)).ToString() + ", " + ((byte)System.Convert.ToByte(slow.Text)).ToString() + ", " + ((byte)System.Convert.ToByte(vlow.Text)).ToString() + "], dtype=np.uint8)\r";
                    script3 += "self.HsvBleuMax = np.array([" + ((byte)System.Convert.ToByte(hhigh.Text)).ToString() + ", " + ((byte)System.Convert.ToByte(shigh.Text)).ToString() + ", " + ((byte)System.Convert.ToByte(vhigh.Text)).ToString() + "], dtype=np.uint8)\r";
                }
            }

            TextRange textRange = new TextRange(RTB_script.Document.ContentStart, RTB_script.Document.ContentEnd);
            textRange.Text = script1 + script2 + script3;
        }

        #region ValueChanged
        private void Exposure_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            cap.SetCaptureProperty(CapProp.Exposure, Exposure.Value);
        }
        private void CamIndex_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
                {
                    cap.Stop();
                    cap.Dispose();

                    cap = new VideoCapture(System.Convert.ToByte(CamIndex.Text));
                    int Codec = FourCC.MRLE; //Latency 64ms en 640-360 (plus haut pas possible)
                    cap.SetCaptureProperty(CapProp.FourCC, Codec);

                    //cap.SetCaptureProperty(CapProp.AutoExposure, 1);
                    cap.SetCaptureProperty(CapProp.FrameWidth, 640);
                    cap.SetCaptureProperty(CapProp.FrameHeight, 360);

                    //On enregistre l'event image capturée
                    cap.ImageGrabbed += ProcessFrame;

                    if (cap != null)
                    {
                        try
                        {
                            cap.Start();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }

                    if (cap != null)
                    {
                        try
                        {
                            //cap.Start();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
        private void ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            byte[] RGBcolor = HSVtoRGB((float)(Hue.Value * 360f / 255f), (float)(Saturation.Value / 255f), (float)(Value.Value / 255f));
            colorRec.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(RGBcolor[0], RGBcolor[1], RGBcolor[2]));
        }
        #endregion
        #region ButtonEvents

        private void BT_color1_Click(object sender, RoutedEventArgs e)
        {
            writeScript(1);
        }

        private void BT_color2_Click(object sender, RoutedEventArgs e)
        {
            writeScript(2);
        }

        private void BT_color3_Click(object sender, RoutedEventArgs e)
        {
            writeScript(3);
        }

        private void BT_restartCap_Click(object sender, RoutedEventArgs e)
        {
            cap.Stop();
            cap.Dispose();

            cap = new VideoCapture(0);
            int Codec = FourCC.MRLE; //Latency 64ms en 640-360 (plus haut pas possible)
            cap.SetCaptureProperty(CapProp.FourCC, Codec);

            //cap.SetCaptureProperty(CapProp.AutoExposure, 1);
            cap.SetCaptureProperty(CapProp.FrameWidth, 640);
            cap.SetCaptureProperty(CapProp.FrameHeight, 360);

            //On enregistre l'event image capturée
            cap.ImageGrabbed += ProcessFrame;

            if (cap != null)
            {
                try
                {
                    cap.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }




        #endregion
        #region Checkboxes

        private void CB_Expo_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)CB_Expo.IsChecked)
            {
                cap.SetCaptureProperty(CapProp.AutoExposure, 1);
                Exposure.Value = 0;
                Exposure.IsEnabled = false;
            }
            else
            {
                cap.SetCaptureProperty(CapProp.AutoExposure, 0);
                Exposure.IsEnabled = true;
            }
        }

        private void CB_Manual_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)CB_Manual.IsChecked)
            {
                hlow.IsEnabled = true;
                hhigh.IsEnabled = true;
                slow.IsEnabled = true;
                shigh.IsEnabled = true;
                vlow.IsEnabled = true;
                vhigh.IsEnabled = true;

                Hue.IsEnabled = false;
                Saturation.IsEnabled = false;
                Value.IsEnabled = false;
                Errorhue.IsEnabled = false;
                Errorsat.IsEnabled = false;
                Errorval.IsEnabled = false;
            }
            else
            {
                hlow.IsEnabled = false;
                hhigh.IsEnabled = false;
                slow.IsEnabled = false;
                shigh.IsEnabled = false;
                vlow.IsEnabled = false;
                vhigh.IsEnabled = false;

                Hue.IsEnabled = true;
                Saturation.IsEnabled = true;
                Value.IsEnabled = true;
                Errorhue.IsEnabled = true;
                Errorsat.IsEnabled = true;
                Errorval.IsEnabled = true;
            }
        }


        #endregion

    }

    public class FourCC
    {
        /// <summary>
        /// Opens the Codec Selection Dialog...
        /// </summary>
        public static int UserSelect = -1;

        public static int _1978 = 943143217;
        public static int _2VUY = 1498764850;
        public static int _3IV0 = 810961203;
        public static int _3IV1 = 827738419;
        public static int _3IV2 = 844515635;
        public static int _3IVD = 1146505523;
        public static int _3IVX = 1482049843;
        public static int _8BPS = 1397768760;
        public static int AAS4 = 877871425;
        public static int AASC = 1129529665;
        public static int ABYR = 1381581377;
        public static int ACTL = 1280590657;
        public static int ADV1 = 827737153;
        public static int ADVJ = 1247167553;
        public static int AEIK = 1263093057;
        public static int AEMI = 1229800769;
        public static int AFLC = 1129072193;
        public static int AFLI = 1229735489;
        public static int AHDV = 1447315521;
        public static int AJPG = 1196444225;
        public static int AMPG = 1196444993;
        public static int ANIM = 1296649793;
        public static int AP41 = 825512001;
        public static int AP42 = 842289217;
        public static int ASLC = 1129075521;
        public static int ASV1 = 827740993;
        public static int ASV2 = 844518209;
        public static int ASVX = 1482052417;
        public static int ATM4 = 877483073;
        public static int AUR2 = 844256577;
        public static int AURA = 1095914817;
        public static int AVC1 = 826496577;
        public static int AVRN = 1314018881;
        public static int BA81 = 825770306;
        public static int BINK = 1263421762;
        public static int BLZ0 = 811224130;
        public static int BT20 = 808604738;
        public static int BTCV = 1447253058;
        public static int BW10 = 808539970;
        public static int BYR1 = 827480386;
        public static int BYR2 = 844257602;
        public static int CC12 = 842089283;
        public static int CDVC = 1129727043;
        public static int CFCC = 1128482371;
        public static int CGDI = 1229211459;
        public static int CHAM = 1296123971;
        public static int CJPG = 1196444227;
        public static int CMYK = 1264143683;
        public static int CPLA = 1095520323;
        public static int CRAM = 1296126531;
        public static int CSCD = 1145262915;
        public static int CTRX = 1481790531;
        public static int CVID = 1145656899;
        public static int CWLT = 1414289219;
        public static int CXY1 = 827938883;
        public static int CXY2 = 844716099;
        public static int CYUV = 1448433987;
        public static int CYUY = 1498765635;
        public static int D261 = 825635396;
        public static int D263 = 859189828;
        public static int DAVC = 1129726276;
        public static int DCL1 = 827081540;
        public static int DCL2 = 843858756;
        public static int DCL3 = 860635972;
        public static int DCL4 = 877413188;
        public static int DCL5 = 894190404;
        public static int DIV3 = 861292868;
        public static int DIV4 = 878070084;
        public static int DIV5 = 894847300;
        public static int DIVX = 1482049860;
        public static int DM4V = 1446268228;
        public static int DMB1 = 826428740;
        public static int DMB2 = 843205956;
        public static int DMK2 = 843795780;
        public static int DSVD = 1146508100;
        public static int DUCK = 1262703940;
        public static int DV25 = 892491332;
        public static int DV50 = 808801860;
        public static int DVAN = 1312904772;
        public static int DVCS = 1396921924;
        public static int DVE2 = 843404868;
        public static int DVH1 = 826824260;
        public static int DVHD = 1145591364;
        public static int DVSD = 1146312260;
        public static int DVSL = 1280529988;
        public static int DVX1 = 827872836;
        public static int DVX2 = 844650052;
        public static int DVX3 = 861427268;
        public static int DX50 = 808802372;
        public static int DXGM = 1296521284;
        public static int DXTC = 1129601092;
        public static int DXTN = 1314150468;
        public static int EKQ0 = 810634053;
        public static int ELK0 = 810241093;
        public static int EM2V = 1446137157;
        public static int ES07 = 925913925;
        public static int ESCP = 1346589509;
        public static int ETV1 = 827741253;
        public static int ETV2 = 844518469;
        public static int ETVC = 1129731141;
        public static int FFV1 = 827737670;
        public static int FLJP = 1347046470;
        public static int FMP4 = 877677894;
        public static int FMVC = 1129729350;
        public static int FPS1 = 827543622;
        public static int FRWA = 1096241734;
        public static int FRWD = 1146573382;
        public static int FVF1 = 826693190;
        public static int GEOX = 1481590087;
        public static int GJPG = 1196444231;
        public static int GLZW = 1465535559;
        public static int GPEG = 1195724871;
        public static int GWLT = 1414289223;
        public static int H260 = 808858184;
        public static int H261 = 825635400;
        public static int H262 = 842412616;
        public static int H263 = 859189832;
        public static int H264 = 875967048;
        public static int H265 = 892744264;
        public static int H266 = 909521480;
        public static int H267 = 926298696;
        public static int H268 = 943075912;
        public static int H269 = 959853128;
        public static int HDYC = 1129923656;
        public static int HEVC = 1129727304;
        public static int HFYU = 1431914056;
        public static int HMCR = 1380142408;
        public static int HMRR = 1381125448;
        public static int I263 = 859189833;
        public static int ICLB = 1112294217;
        public static int IGOR = 1380927305;
        public static int IJPG = 1196444233;
        public static int ILVC = 1129729097;
        public static int ILVR = 1381387337;
        public static int IPDV = 1447317577;
        public static int IR21 = 825381449;
        public static int IRAW = 1463898697;
        public static int ISME = 1162695497;
        public static int IV30 = 808670793;
        public static int IV31 = 825448009;
        public static int IV32 = 842225225;
        public static int IV33 = 859002441;
        public static int IV34 = 875779657;
        public static int IV35 = 892556873;
        public static int IV36 = 909334089;
        public static int IV37 = 926111305;
        public static int IV38 = 942888521;
        public static int IV39 = 959665737;
        public static int IV40 = 808736329;
        public static int IV41 = 825513545;
        public static int IV42 = 842290761;
        public static int IV43 = 859067977;
        public static int IV44 = 875845193;
        public static int IV45 = 892622409;
        public static int IV46 = 909399625;
        public static int IV47 = 926176841;
        public static int IV48 = 942954057;
        public static int IV49 = 959731273;
        public static int IV50 = 808801865;

        /// <summary>
        /// This is considered to be the Dfault Fallback Codec for Linux and also Windows may be following suit.
        /// Not in the Litrature on the FOURCC website.
        /// See: http://www.emgu.com/wiki/index.php/Video_Files
        /// </summary>
        public static int IYUV = 1448433993;

        public static int JBYR = 1381581386;
        public static int JPEG = 1195724874;
        public static int JPGL = 1279742026;
        public static int KMVC = 1129729355;
        public static int L261 = 825635404;
        public static int L263 = 859189836;
        public static int LBYR = 1381581388;
        public static int LCMW = 1464681292;
        public static int LCW2 = 844579660;
        public static int LEAD = 1145128268;
        public static int LGRY = 1498564428;
        public static int LJ11 = 825313868;
        public static int LJ22 = 842156620;
        public static int LJ2K = 1261587020;
        public static int LJ44 = 875842124;
        public static int LJPG = 1196444236;
        public static int LMP2 = 844123468;
        public static int LMP4 = 877677900;
        public static int LSVC = 1129730892;
        public static int LSVM = 1297503052;
        public static int LSVX = 1482052428;
        public static int LZO1 = 827284044;
        public static int M261 = 825635405;
        public static int M263 = 859189837;
        public static int M4CC = 1128477773;
        public static int M4S2 = 844313677;
        public static int MC12 = 842089293;
        public static int MCAM = 1296122701;
        public static int MJ2C = 1127369293;
        public static int MJPG = 1196444237;
        public static int MMES = 1397050701;
        public static int MP2A = 1093816397;
        public static int MP2T = 1412583501;
        public static int MP2V = 1446137933;
        public static int MP42 = 842289229;
        public static int MP43 = 859066445;
        public static int MP4A = 1093947469;
        public static int MP4S = 1395937357;
        public static int MP4T = 1412714573;
        public static int MP4V = 1446269005;
        public static int MPEG = 1195724877;
        public static int MPG4 = 877088845;
        public static int MPGI = 1229410381;
        public static int MR16 = 909201997;
        public static int MRCA = 1094931021;
        public static int MRLE = 1162629709;
        public static int MSVC = 1129730893;
        public static int MSZH = 1213879117;
        public static int MTX1 = 827872333;
        public static int MTX2 = 844649549;
        public static int MTX3 = 861426765;
        public static int MTX4 = 878203981;
        public static int MTX5 = 894981197;
        public static int MTX6 = 911758413;
        public static int MTX7 = 928535629;
        public static int MTX8 = 945312845;
        public static int MTX9 = 962090061;
        public static int MVI1 = 826889805;
        public static int MVI2 = 843667021;
        public static int MWV1 = 827742029;
        public static int NAVI = 1230389582;
        public static int NDSC = 1129530446;
        public static int NDSM = 1297302606;
        public static int NDSP = 1347634254;
        public static int NDSS = 1397965902;
        public static int NDXC = 1129858126;
        public static int NDXH = 1213744206;
        public static int NDXP = 1347961934;
        public static int NDXS = 1398293582;
        public static int NHVU = 1431717966;
        public static int NTN1 = 827216974;
        public static int NTN2 = 843994190;
        public static int NVDS = 1396987470;
        public static int NVHS = 1397249614;
        public static int NVS0 = 810767950;
        public static int NVS1 = 827545166;
        public static int NVS2 = 844322382;
        public static int NVS3 = 861099598;
        public static int NVS4 = 877876814;
        public static int NVS5 = 894654030;
        public static int NVT0 = 810833486;
        public static int NVT1 = 827610702;
        public static int NVT2 = 844387918;
        public static int NVT3 = 861165134;
        public static int NVT4 = 877942350;
        public static int NVT5 = 894719566;
        public static int PDVC = 1129727056;
        public static int PGVV = 1448494928;
        public static int PHMO = 1330464848;
        public static int PIM1 = 827148624;
        public static int PIM2 = 843925840;
        public static int PIMJ = 1246579024;
        public static int PIXL = 1280854352;
        public static int PJPG = 1196444240;
        public static int PVEZ = 1514493520;
        public static int PVMM = 1296914000;
        public static int PVW2 = 844584528;
        public static int QPEG = 1195724881;
        public static int QPEQ = 1363497041;
        public static int RGBT = 1413629778;
        public static int RLE = 1162629727;
        public static int RLE4 = 876956754;
        public static int RLE8 = 944065618;
        public static int RMP4 = 877677906;
        public static int RPZA = 1096437842;
        public static int RT21 = 825381970;
        public static int RV20 = 808605266;
        public static int RV30 = 808670802;
        public static int RV40 = 808736338;
        public static int S422 = 842150995;
        public static int SAN3 = 860766547;
        public static int SDCC = 1128481875;
        public static int SEDG = 1195656531;
        public static int SFMC = 1129137747;
        public static int SMP4 = 877677907;
        public static int SMSC = 1129532755;
        public static int SMSD = 1146309971;
        public static int SMSV = 1448299859;
        public static int SP40 = 808734803;
        public static int SP44 = 875843667;
        public static int SP54 = 875909203;
        public static int SPIG = 1195987027;
        public static int SQZ2 = 844779859;
        public static int STVA = 1096176723;
        public static int STVB = 1112953939;
        public static int STVC = 1129731155;
        public static int STVX = 1482052691;
        public static int STVY = 1498829907;
        public static int SV10 = 808539731;
        public static int SVQ1 = 827414099;
        public static int SVQ3 = 860968531;
        public static int TLMS = 1397574740;
        public static int TLST = 1414745172;
        public static int TM20 = 808602964;
        public static int TM2X = 1479691604;
        public static int TMIC = 1128877396;
        public static int TMOT = 1414483284;
        public static int TR20 = 808604244;
        public static int TSCC = 1128485716;
        public static int TV10 = 808539732;
        public static int TVJP = 1347049044;
        public static int TVMJ = 1246582356;
        public static int TY0N = 1311791444;
        public static int TY2C = 1127373140;
        public static int TY2N = 1311922516;
        public static int UCOD = 1146045269;
        public static int ULTI = 1230261333;
        public static int V210 = 808530518;
        public static int V261 = 825635414;
        public static int V655 = 892679766;
        public static int VCR1 = 827474774;
        public static int VCR2 = 844251990;
        public static int VCR3 = 861029206;
        public static int VCR4 = 877806422;
        public static int VCR5 = 894583638;
        public static int VCR6 = 911360854;
        public static int VCR7 = 928138070;
        public static int VCR8 = 944915286;
        public static int VCR9 = 961692502;
        public static int VDCT = 1413694550;
        public static int VDOM = 1297040470;
        public static int VDOW = 1464812630;
        public static int VDTZ = 1515471958;
        public static int VGPX = 1481656150;
        public static int VIDS = 1396984150;
        public static int VIFP = 1346783574;
        public static int VIVO = 1331054934;
        public static int VIXL = 1280854358;
        public static int VLV1 = 827739222;
        public static int VP30 = 808669270;
        public static int VP31 = 825446486;
        public static int VP40 = 808734806;
        public static int VP50 = 808800342;
        public static int VP60 = 808865878;
        public static int VP61 = 825643094;
        public static int VP62 = 842420310;
        public static int VP70 = 808931414;
        public static int VP80 = 808996950;
        public static int VQC1 = 826495318;
        public static int VQC2 = 843272534;
        public static int VQJC = 1128943958;
        public static int VSSV = 1448301398;
        public static int VUUU = 1431655766;
        public static int VX1K = 1261525078;
        public static int VX2K = 1261590614;
        public static int VXSP = 1347639382;
        public static int VYU9 = 961894742;
        public static int VYUY = 1498765654;
        public static int WBVC = 1129726551;
        public static int WHAM = 1296123991;
        public static int WINX = 1481525591;
        public static int WJPG = 1196444247;
        public static int WMV1 = 827739479;
        public static int WMV2 = 844516695;
        public static int WMV3 = 861293911;
        public static int WMVA = 1096174935;
        public static int WNV1 = 827739735;
        public static int WVC1 = 826496599;
        public static int X263 = 859189848;
        public static int X264 = 875967064;
        public static int XLV0 = 810962008;
        public static int XMPG = 1196445016;
        public static int XVID = 1145656920;
        public static int XWV0 = 810964824;
        public static int XWV1 = 827742040;
        public static int XWV2 = 844519256;
        public static int XWV3 = 861296472;
        public static int XWV4 = 878073688;
        public static int XWV5 = 894850904;
        public static int XWV6 = 911628120;
        public static int XWV7 = 928405336;
        public static int XWV8 = 945182552;
        public static int XWV9 = 961959768;
        public static int XXAN = 1312905304;
        public static int Y16 = 909203807;
        public static int Y411 = 825308249;
        public static int Y41P = 1345401945;
        public static int Y444 = 875836505;
        public static int Y8 = 945381215;
        public static int YC12 = 842089305;
        public static int YUV8 = 945182041;
        public static int YUV9 = 961959257;
        public static int YUVP = 1347835225;
        public static int YUY2 = 844715353;
        public static int YUYV = 1448695129;
        public static int YV12 = 842094169;
        public static int YV16 = 909203033;
        public static int YV92 = 842618457;
        public static int ZLIB = 1112099930;
        public static int ZMBV = 1447185754;
        public static int ZPEG = 1195724890;
        public static int ZYGO = 1330075994;
        public static int ZYYY = 1499027802;
    }
}
