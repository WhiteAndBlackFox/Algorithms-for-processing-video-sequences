using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using VideoProcessor.Helpers;
using VideoProcessor.Model;
using VideoProcessor.Properties;
using VideoProcessor.Video;
using CvInvoke = Emgu.CV.CvInvoke;

namespace VideoProcessor
{
    public partial class MainForm : Form
    {
        #region Constructor & private fields
        private Stopwatch _stopWatch;
        private string _fileName;

        private bool _isFrameReady;
        private bool _isVideoProcessing;
        private bool _isMetricFormOpened;
        private FileVideoSource _fileVideoSource;
        private readonly Frame[] _frames;

        private object timeFinish = "";

        private readonly EffectsForm _effectsForm;
        private readonly MetricsForm _metricsForm;

        public MainForm()
        {
            InitializeComponent();
            buttonPlay.Enabled = false;
            _isMetricFormOpened = false;

            _frames = new Frame[5];
            _effectsForm = new EffectsForm(_frames);
            _effectsForm.Visible = false;
            _effectsForm.Show();
            _effectsForm.Hide();

            _metricsForm = new MetricsForm();
            _metricsForm.Visible = false;
            _metricsForm.Show();
            _metricsForm.Hide();
            _metricsForm.Closing += (o, args) => {
                _isMetricFormOpened = false;
            };

            
            // get all images from C:\Users\Anne\Documents\MATLAB\distTrans\raw_image
            var p = "./640117___2.jpg";
            {
                Image<Bgr, Byte> img = new Image<Bgr, Byte>("./640117___2.jpg");
                Mat[] elements =
                {
                    CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(2, 2), new Point(-1, -1)),
                    CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(2, 2), new Point(-1, -1))
                };
                

                for (int elementId = 0; elementId < elements.Length; elementId++)
                {
                    var stage = new Image<Gray, byte>(img.Bitmap);
                    for (int i = 0; i < 1; i++) {
                        CvInvoke.Dilate(stage, stage, elements[elementId], new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));
                        stage.Save("./" + elementId + "__" + i + ".jpg");
                    }

                    for (int i = 5; i < 10; i++) {
                        CvInvoke.Erode(stage, stage, elements[elementId], new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));
                        stage.Save("./" + elementId + "__" + i + ".jpg");
                    }
                }
            }
            //Application.Exit();
            //return;
            string directory = "./";
            List<string> imagePaths = GetImagesPath(directory);

            foreach (string imgPath in imagePaths)
            {
                if (imgPath.EndsWith(".jpg") || imgPath.EndsWith(".png"))
                try
                {
                    Image<Bgr, Byte> img = new Image<Bgr, Byte>(imgPath);
                    List<Rectangle> rects = DetectLetters(img);
                    foreach (Rectangle rect in rects)
                        img.Draw(rect, new Bgr(0, 255, 0), 3);

                    string direct = Path.GetDirectoryName(imgPath);
                    string fileName = Path.GetFileName(imgPath);
                    string path = GetFilePath(direct + "\\output\\", fileName);
                    try
                    {
                        Console.Write(path);
                        img.Save(path);
                    }
                    catch (Exception e)
                    {
                        //...
                    }
                }
                catch (Exception e)
                {
                    //...
                }
            }
        }

        public List<Rectangle> DetectLetters(Image<Bgr, Byte> img)
        {
            var hash = img.GetHashCode();
            List<Rectangle> rects = new List<Rectangle>();
            var imgGray = img.Convert<Gray, Byte>();
            var imgSobel = imgGray.Sobel(1, 0, 3).Convert<Gray, Byte>();
            var imgRes = new Image<Gray, byte>(imgSobel.Size);
            CvInvoke.Threshold(imgSobel, imgRes, 160, 255, ThresholdType.Binary | ThresholdType.Otsu);
            var element = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(2, 2), new Point(-1, -1));
            CvInvoke.Dilate(imgRes, imgRes, element, new Point(0, 0), 1, BorderType.Default, new MCvScalar(0));
            CvInvoke.Erode(imgRes, imgRes, element, new Point(0, 0), 12, BorderType.Default, new MCvScalar(0));
            using (Mat hierachy = new Mat())
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(imgRes, contours, hierachy, RetrType.Tree, ChainApproxMethod.ChainApproxNone);
                for (int i = 0; i < contours.Size; i++)
                {
                    Rectangle rectangle = CvInvoke.BoundingRectangle(contours[i]);
                    var area = rectangle.Width * rectangle.Height;
                    if (area > 1400 && rectangle.Width < img.Width * 0.7 && rectangle.Width > rectangle.Height * 1.5) {
                        rects.Add(rectangle);
                    }
                }
            }
            return rects;
        }

        public static string GetFilePath(string dir, string fileName) {
            if (Path.GetFileName(fileName) != fileName) {
                throw new Exception("'fileName' is invalid!");
            }
            string combined = Path.Combine(dir, fileName);
            return combined;
        }

        public static List<String> GetImagesPath(String folderName) {
            var folder = new DirectoryInfo(folderName);
            var images = folder.GetFiles();

            return images.Select(t => String.Format(@"{0}/{1}", folderName, t.Name)).ToList();
        }

        #endregion

        #region ToolStrip Menu items
        // Open video file using DirectShow
        private void openVideofileusingDirectShowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                OpenVideoFile(openFileDialog.FileName);
            }
        }

        private void openMjpegToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string videoUrl = System.IO.File.ReadAllText("./mjpg.conf");
            OpenMjpeg(videoUrl);

        }

        // Open video file using VFW
        private void openVideofileusingVFWToolStripMenuItem_Click(object sender, EventArgs e) {
            if (openFileDialog.ShowDialog() == DialogResult.OK) {
                AVIReader aviReader = new AVIReader();
                aviReader.Open(openFileDialog.FileName);
                OpenVideoFile(openFileDialog.FileName);
            }
        }

        private void videoInfoToolStripMenuItem_Click(object sender, EventArgs e) {
            AboutVideoForm form = new AboutVideoForm(_fileName);
            form.ShowDialog();
        }

        private void filtersToolStripMenuItem_Click(object sender, EventArgs e) {
            _effectsForm.Show();
        }

        private void metricsToolStripMenuItem_Click(object sender, EventArgs e) {
            _isMetricFormOpened = true;
            _metricsForm.Show();
        }

        private void saveOriginalFrameToolStripMenuItem_Click(object sender, EventArgs e) {
            SaveScreenShot(videoPlayer.GetCurrentVideoFrame());
        }

        private void saveProcessedFrameToolStripMenuItem_Click(object sender, EventArgs e) {
            SaveScreenShot(processedPicture.Image);
        }

        #endregion

        #region Video Control
        private void OpenVideoFile(string fileName)
        {
            _fileName = fileName;
            videoInfoToolStripMenuItem.Enabled = true;
            _isFrameReady = false;
            _isVideoProcessing = true;
            _fileVideoSource = new FileVideoSource(_fileName, duration => {
                progressBar.Invoke((MethodInvoker)delegate
                {
                    buttonPlay.Enabled = true;
                    buttonPlay.Image = Resources.if_PauseDisabled_22964;
                    TimeSpan durationTs = new TimeSpan(duration);
                    progressBar.Maximum = (int)durationTs.TotalSeconds;
                    var durationTime = durationTs.ToString();
                    timeFinish = durationTime.Substring(0, durationTime.LastIndexOf("."));
                    labelTimeProgressStart.Text = "00:00 / " + durationTime.Substring(0, durationTime.LastIndexOf("."));
                    // labelTimeProgressFinish.Text = durationTime.Substring(0, durationTime.LastIndexOf("."));
                    progressBar.Value = 0;
                });
            });
            Play(_fileVideoSource);
        }

        private void OpenMjpeg(string path)
        {
            MJPEGStream videoStream = new MJPEGStream();
            FormClosing += (o, args) => {
                videoStream.Stop();
            };
            videoStream.VideoSourceError += (o, args) => {
                MessageBox.Show(args.Description);
            };
            videoStream.Source = path;

            videoStream.Start();

            videoInfoToolStripMenuItem.Enabled = false;
            _isFrameReady = false;
            _isVideoProcessing = true;
            progressBar.Value = 0;
            progressBar.Maximum = 1;
            buttonPlay.Image = Resources.if_PauseDisabled_22964;
            buttonPlay.Enabled = false;
            labelTimeProgressFinish.Text = "-";

            Play(videoStream);
            timer.Enabled = false;
        }

        private void Play(IVideoSource source)
        {
            Stop();
            videoPlayer.VideoSource = source;
            videoPlayer.NewFrame += videoSourcePlayer_NewFrame;
            videoPlayer.PlayingFinished += (sender, reason) =>
            {
                try
                {
                    Invoke((MethodInvoker) delegate
                    {
                        pictureBox2_Click(null, null);
                    });
                }
                catch (Exception e)
                {
                    // ignored
                }
            };
            _effectsForm.Reset();
            videoPlayer.Start();
            _stopWatch = null;
            timer.Start();
        }

        private void Stop()
        {
            if (videoPlayer.VideoSource != null)
            {
                videoPlayer.SignalToStop();

                // wait 3 seconds
                for (int i = 0; i < 50; i++)
                {
                    if (!videoPlayer.IsRunning)
                        break;
                    System.Threading.Thread.Sleep(100);
                }

                if (videoPlayer.IsRunning)
                {
                    videoPlayer.Stop();
                }

                buttonPlay.Image = Resources.if_StepForwardDisabled_22933;

                videoPlayer.VideoSource = null;
                _fileVideoSource = null;
            }
        }

        private void videoSourcePlayer_NewFrame(object sender, ref Bitmap image)
        {
            if (!_isVideoProcessing) return;
            //Dispose last image
            if (_frames.Last() != null)
            {
                _isFrameReady = true;
                _frames.Last().Dispose();
            }
            //Move all frames to right 
            for (int i = 1; i < _frames.Length; i++)
            {
                _frames[i] = _frames[i - 1];
            }

            _frames[0] = new Frame(image);

            if (!_isFrameReady) return;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            processedPicture.Image = _effectsForm.Process();
            stopWatch.Stop();

            if (_isMetricFormOpened)
            {
                _metricsForm.AddData(stopWatch.Elapsed.TotalSeconds, _frames[0], _frames[1]);
            }
        }

        private void SaveScreenShot(Image image)
        {
            using (var dialog = new SaveFileDialog {
                DefaultExt = "jpg",
                AddExtension = true,
                Filter = @"Jpeg image|*.jpg",
                FileName = "Screenshot"
            }) {
                if (dialog.ShowDialog() == DialogResult.OK) {
                    image.Save(dialog.FileName, ImageFormat.Jpeg);
                }
            }
        }

        #endregion

        #region Form events

        private void MainForm_SizeChanged(object sender, EventArgs e) {
            sourceVideoPanel.Width = Width / 2 - 6;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            Stop();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Close();
        }

        private void buttonPlay_Click(object sender, EventArgs e) {
            if (_fileVideoSource == null)
            {
                OpenVideoFile(_fileName);
                buttonPlay.Image = Resources.if_PauseDisabled_22964;
            }
            else
            {
                if (_fileVideoSource.IsPlaying)
                {
                    _fileVideoSource.IsSetPause = true;
                    buttonPlay.Image = Resources.if_StepForwardDisabled_22933;
                }
                else
                {
                    _fileVideoSource.IsSetPlay = true;
                    buttonPlay.Image = Resources.if_PauseDisabled_22964;
                }
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            videoPlayer.Invoke((MethodInvoker) delegate
            {
                videoPlayer.SignalToStop();
            });
            processedPicture.Image = null;
            buttonPlay.Image = Resources.if_StepForwardDisabled_22933;
            labelTimeProgressFinish.Text = "--:--";
            labelTimeProgressStart.Text = "--:--";
            videoPlayer.VideoSource = null;
            _fileVideoSource = null;
        }

        private void progressBar_ValueChanged(double oldValue, double newValue) {
            if (_fileVideoSource != null) {
                _fileVideoSource.SetCurrentTime((long)newValue);
            }
        }

        private void processedPicture_Click(object sender, EventArgs e) {
            _isVideoProcessing = !_isVideoProcessing;
        }

        private void timer_Tick(object sender, EventArgs e) {
            if (_fileVideoSource != null)
            {
                var time = _fileVideoSource.GetCurrentTime();
                labelTimeProgressStart.Text = "00:0" + time.ToTimeString() + "/" + timeFinish;
                progressBar.Value = (int)time;
                int framesReceived = _fileVideoSource.FramesReceivedFromLastTime;
                fpsLabel.Text = string.Format("{0:f6} fps", 0) + "     " + string.Format("{0} frame", _fileVideoSource.FramesReceived);
                // labelFrameNumber.Text = string.Format("{0} frame", _fileVideoSource.FramesReceived);
                if (_stopWatch == null) {
                    _stopWatch = new Stopwatch();
                    _stopWatch.Start();
                }
                else {
                    _stopWatch.Stop();

                    float fps = 1000.0f * framesReceived / _stopWatch.ElapsedMilliseconds;
                    _metricsForm.AddData(_fileVideoSource.GetCurrentTime(), fps);
                    fpsLabel.Text = string.Format("{0:f6} fps", fps) + "     " + string.Format("{0} frame", _fileVideoSource.FramesReceived);

                    _stopWatch.Reset();
                    _stopWatch.Start();
                }
            }
            Application.DoEvents();
        }

        #endregion

        private void progressBar_Click(object sender, EventArgs e)
        {

        }

        private void videoPlayer_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void videoPlayer_Click_1(object sender, EventArgs e)
        {

        }

        private void labelTimeProgressFinish_Click(object sender, EventArgs e)
        {

        }

        private void сохранитьИсходныйКадрToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveScreenShot(videoPlayer.GetCurrentVideoFrame());
        }

        private void сохранитьОбработаннуюРамкуToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveScreenShot(processedPicture.Image);
        }
    }
}
