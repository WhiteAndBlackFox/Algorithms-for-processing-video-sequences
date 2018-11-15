using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using VideoProcessor.Algorithms;
using VideoProcessor.Helpers;
using VideoProcessor.Model;
using VideoProcessor.MotionDetector;

// ReSharper disable InconsistentNaming

namespace VideoProcessor {
    public partial class EffectsForm : Form
    {
        private readonly ImageProcessor _imageProcessor;
        private readonly Frame[] _frames;

        private float _gammaCorrectionValue;
        private int _logCorrectionValue;

        private ProcessTypeEnum _processType;
        private ColorModelEnum _colorModel;
        private int _frameNumber;

        private int _linearAverageRadiusValue;
        private int _medianFilterRadius;
        private int _2DCLeanerRadiusValue;
        private int _2DCLeanerThresholdValue;
        private int _backgroundSubstractorThreshold;
        private int _sceneChangeDetectorThreshold;
        private int _vocrContourThreshold;
        private float _vocrContourGain;
        private int _vocrContourBrightnessThreshold;
        private bool _vocrContourShow;


        private int _blockThreshold;
        private int _blockSize;

        private BackgroundSubstractor _backgroundSubstractor;
        private FeaturePointMotionDetector _feautePointMotionDetector;
        private SceneChangeDetector _sceneChangeDetector;
        private BlockMatchingDetector _blockMatchingDetector;

        public EffectsForm(Frame[] frames) {
            InitializeComponent();
            _frames = frames;
            _imageProcessor = new ImageProcessor();

            //trackBarGammaCorrection.Setup(checkBoxGammaCorrection, "Gamma Correction (gamma = {0:f})",
            //    value => _gammaCorrectionValue = value,
            //    value => value / 10f);

            //trackBarLogCorrection.Setup(checkBoxLogCorrection, "Log Correction (k = {0:f})",
            //    value => _logCorrectionValue = (int)value);

            trackBarFrameNumber.Setup(labelFrameNumber, "Frame number = {0}",
                value => _frameNumber = (int)value);

            trackBarLinearAverageRadius.Setup(labelLinearAverageRadius, "Радиус = {0}",
                value => _linearAverageRadiusValue = (int)value);

            trackBar2DCleanerRadius.Setup(label2DCleanerRadius, "Радиус = {0}",
                value => _2DCLeanerRadiusValue = (int)value);

            trackBar2DCleanerThreshold.Setup(label2DCleanerThreshold, "Порог = {0}",
                value => _2DCLeanerThresholdValue = (int)value);

            trackBarMedianFilterRadius.Setup(labelMedianFilterRadius, "Радиус = {0}",
                value => _medianFilterRadius = (int)value);

            trackBarBackgroundSubstractorThreshold.Setup(labelBackroundSubstractorThreshold, "Порог = {0}",
                value => _backgroundSubstractorThreshold = (int)value);

            trackBarSceneChangeDetectorThreshold.Setup(labelSceneChangeDetectorThreshold, "Порог = {0}",
                value => _sceneChangeDetectorThreshold = (int)value);

            trackBarBlockThreshold.Setup(labelBlockThreshold, "Порог = {0}",
                value => _blockThreshold = (int)value);

            trackBarBlockSize.Setup(labelBlockSize, "Размер блока = {0}",
                value => _blockSize = (int)value);

            trackBarVocrControurThreshold.Setup(labelVocrControurThreshold, "Порог = {0}",
                value => _vocrContourThreshold = (int)value);

            trackBarVocrControurGain.Setup(labelVocrControurGain, "Усилене = {0}",
                value => _vocrContourGain = value, i => i / 100f);

            trackBarVocrControurBrightnessThreshold.Setup(labelVocrControurBrightnessThreshold, "Порог яркости = {0}",
                value => _vocrContourBrightnessThreshold = (int)value);

            checkBoxVocrContourShow.Setup(value => _vocrContourShow = value);

            radioButtonProcessTypeParallelepiped.CheckedChanged +=  ProcessTypeRadioButtonHandler;
            radioButtonProcessTypePyramid.CheckedChanged +=  ProcessTypeRadioButtonHandler;
            radioButtonProcessTypeCone.CheckedChanged +=  ProcessTypeRadioButtonHandler;
            radioButtonColorModelRgb.CheckedChanged += ColorModelRadioButtonHandler;
            radioButtonColorModelYuv.CheckedChanged += ColorModelRadioButtonHandler;

            _backgroundSubstractor = new BackgroundSubstractor();
            _feautePointMotionDetector = new FeaturePointMotionDetector();
            _sceneChangeDetector = new SceneChangeDetector();
            _blockMatchingDetector = new BlockMatchingDetector();

            chartMotionDetector.SetCheckBoxes(checkBoxObjectCountColumn, checkBoxMinAreaColumn, checkBoxMaxAreaColumn);
            chartFeaturePoints.SetCheckBoxes(new []{checkBoxFeaturePointColumn, checkBoxSimilarPointColumn});
        }

        public void Reset()
        {
            _backgroundSubstractor = new BackgroundSubstractor();
            _feautePointMotionDetector = new FeaturePointMotionDetector();
            _sceneChangeDetector = new SceneChangeDetector();
            _blockMatchingDetector = new BlockMatchingDetector();
        }

        public Image Process()
        {
            var frame = _frames.First().Copy();

            if (checkAutolevel.Checked) {
                _imageProcessor.AlgorithmAutoLevel(frame);
            }

            if (checkHistogram.Checked) {
                _imageProcessor.AlgorithmHistogram(frame, Int32.Parse(minR.Text), Int32.Parse(minG.Text), Int32.Parse(minB.Text), Int32.Parse(maxR.Text), Int32.Parse(maxG.Text), Int32.Parse(maxB.Text));
            }

            //if (checkBoxGrayScale.Checked) {
            //    _imageProcessor.AlgorithmGrayScale(frame, GrayScale.FromRgb);
            //}

            //if (checkBoxGammaCorrection.Checked) {
            //    _imageProcessor.AlgorithmGammaCorrection(frame, _gammaCorrectionValue);
            //}

            //if (checkBoxLogCorrection.Checked) {
            //    _imageProcessor.AlgorithmLogCorrection(frame, _logCorrectionValue);
            //}

            if (checkBox6.Checked) {
                _imageProcessor.FilterLinearAverage(frame, _frames, _frameNumber, _linearAverageRadiusValue, _colorModel, _processType);
            }

            if (checkBox4.Checked) {
                _imageProcessor.FilterMedian(frame, _frames, _frameNumber, _medianFilterRadius, _colorModel, _processType);
            }

            if (checkBox5.Checked) {
                _imageProcessor.Filter2DCleaner(frame, _frames, _2DCLeanerRadiusValue, _2DCLeanerThresholdValue);
            }

            DetectorResult detectorResult = null;
            DetectorResult textDetectorResult = null;
            MKeyPoint[] points = null;

            if (checkBoxVocrContour.Checked) {
                textDetectorResult = _imageProcessor.VocrControur(frame, _vocrContourThreshold, _vocrContourGain, _vocrContourBrightnessThreshold, _vocrContourShow);
            }

            if (checkBoxUsingColorInfromation.Checked)
            {
                textDetectorResult = _imageProcessor.VocrColorInformation(frame, _vocrContourThreshold, _vocrContourGain, _vocrContourBrightnessThreshold, _vocrContourShow);
            }
            
            if (checkBoxBackgroundSubtraction.Checked) {
                detectorResult = _backgroundSubstractor.Process(frame, _backgroundSubstractorThreshold);
            }

            if (checkBoxFeatureMotionDetector.Checked) {
                detectorResult = _feautePointMotionDetector.Process(frame.Copy(true), _frames[1].Copy(true));
            }

            if (checkBoxBlockMatchingDetector.Checked) {
                detectorResult = _blockMatchingDetector.Process(frame.Copy(true), _blockSize, _blockThreshold);
            }

            frame.SaveChanges();

            using (Graphics g = Graphics.FromImage(frame.Image))
            {
                Pen pen = new Pen(Color.Green, 3);
                Pen pen2 = new Pen(Color.White);
                if (checkBoxSceneChangeDetector.Checked) {
                    _sceneChangeDetector.Process(frame, _sceneChangeDetectorThreshold, sceneNumber => {
                        Brush brush = new SolidBrush(Color.White);
                        g.DrawString("Scene " + sceneNumber, new Font(FontFamily.GenericSansSerif, 40), brush, 10, 30);
                    });
                }

                if (textDetectorResult != null)
                {
                    var totalTextRegions = textDetectorResult.Regions.Count;
                    var regions = textDetectorResult.Regions
                        .Where(item => item.IsGoodTextRegion)
                        .OrderByDescending(item => item.Area)
                        .ToArray();


                    //All regions
                    foreach (var region in regions)
                    {
                        g.DrawRectangle(pen2, region.Rectangle);
                    }

                    //Good regions
                    for (int regIndex = 0; regIndex < regions.Length; regIndex++) {
                        var region = regions[regIndex];
                        //если уже был отрисован прямоугольник, пересекающийся с данным, то пропустить
                        if (regIndex > 0 && regions.Take(regIndex).Any(item => item.Rectangle.IntersectsWith(region.Rectangle))) continue;

                        g.DrawRectangle(pen, region.Rectangle);
                    }
                }
                if (points != null) {
                    foreach (var point in points) {
                        g.DrawArc(pen, (int)point.Point.X - 3, (int)point.Point.Y - 3, 6, 6, 0, 360);
                    }
                }
                if (detectorResult != null)
                {
                    var regions = detectorResult.Regions
                        .Where(item => item.IsGoodRegion)
                        .OrderByDescending(item => item.Area)
                        .ToArray();

                    if (checkBoxLog.Checked && regions.Any())
                    {
                        chartMotionDetector.Invoke((MethodInvoker) delegate
                        {
                            var x = chartMotionDetector.Series[0].Points.Count + 1;
                            var min = regions.Min(item => item.Area) / 1000f;
                            var max = regions.Max(item => item.Area) / 1000f;
                            chartMotionDetector.Series[0].Points.AddXY(x, regions.Length);
                            chartMotionDetector.Series[1].Points.AddXY(x, min);
                            chartMotionDetector.Series[2].Points.AddXY(x, max);

                            var log = string.Format("{0}; {1}; {2}; {3}", x, regions.Length, min, max);
                            var featureDetectorResult = detectorResult as FeatureDetectorResult;
                            if (featureDetectorResult != null)
                            {
                                x = chartFeaturePoints.Series[0].Points.Count + 1;
                                chartFeaturePoints.Series[0].Points.AddXY(x, featureDetectorResult.FeaturePointCount);
                                chartFeaturePoints.Series[1].Points.AddXY(x, featureDetectorResult.SimilarPointCount);
                                log += string.Format("; {0}; {1}", featureDetectorResult.FeaturePointCount, featureDetectorResult.SimilarPointCount);
                            }
                            textBoxLog.Text += log + "\r\n";
                        });
                    }

                    for (int regIndex = 0; regIndex < regions.Length; regIndex++) {
                        var region = regions[regIndex];
                        //если уже был отрисован прямоугольник, пересекающийся с данным, то пропустить
                        if (regIndex > 0 && regions.Take(regIndex).Any(item => item.Rectangle.IntersectsWith(region.Rectangle))) continue;

                        g.DrawRectangle(pen, region.Rectangle);
                    }
                }
            }

            return frame.Image;
        }

        private void FilterForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void ProcessTypeRadioButtonHandler(object sender, EventArgs e) {
            if (radioButtonProcessTypeParallelepiped.Checked)
            {
                _processType = ProcessTypeEnum.Parallelepiped;
            }
            else if (radioButtonProcessTypePyramid.Checked)
            {
                _processType = ProcessTypeEnum.Pyramid;
            }
            else
            {
                _processType = ProcessTypeEnum.Cone;
            }
        }

        private void ColorModelRadioButtonHandler(object sender, EventArgs e) {
            _colorModel = radioButtonColorModelRgb.Checked ? ColorModelEnum.Rgb : ColorModelEnum.Yuv;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void minR_TextChanged(object sender, EventArgs e)
        {
            int box_int = 0; Int32.TryParse(minR.Text, out box_int);
            if (box_int < 0 && minR.Text != "") { minR.Text = "0"; }
            if (box_int > 255 && minR.Text != "") { minR.Text = "255"; }
        }

        private void maxR_TextChanged(object sender, EventArgs e)
        {
            int box_int = 0; Int32.TryParse(maxR.Text, out box_int);
            if (box_int < 0 && maxR.Text != "") { maxR.Text = "0"; }
            if (box_int > 255 && maxR.Text != "") { maxR.Text = "255"; }
        }

        private void minG_TextChanged(object sender, EventArgs e)
        {
            int box_int = 0; Int32.TryParse(minG.Text, out box_int);
            if (box_int < 0 && minG.Text != "") { minG.Text = "0"; }
            if (box_int > 255 && minG.Text != "") { minG.Text = "255"; }
        }

        private void maxG_TextChanged(object sender, EventArgs e)
        {
            int box_int = 0; Int32.TryParse(maxG.Text, out box_int);
            if (box_int < 0 && maxG.Text != "") { maxG.Text = "0"; }
            if (box_int > 255 && maxG.Text != "") { maxG.Text = "255"; }
        }

        private void minB_TextChanged(object sender, EventArgs e)
        {
            int box_int = 0; Int32.TryParse(minB.Text, out box_int);
            if (box_int < 0 && minB.Text != "") { minB.Text = "0"; }
            if (box_int > 255 && minB.Text != "") { minB.Text = "255"; }
        }

        private void maxB_TextChanged(object sender, EventArgs e)
        {
            int box_int = 0; Int32.TryParse(maxB.Text, out box_int);
            if (box_int < 0 && maxB.Text != "") { maxB.Text = "0"; }
            if (box_int > 255 && maxB.Text != "") { maxB.Text = "255"; }
        }

        private void checkHistogram_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            int box_int = 0; Int32.TryParse(textBox1.Text, out box_int);
            if (box_int < 1 && textBox1.Text != "") { textBox1.Text = "3"; }
            if (box_int > 5 && textBox1.Text != "") { textBox1.Text = "3"; }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            int box_int = 0; Int32.TryParse(textBox2.Text, out box_int);
            if (box_int < 1 && textBox2.Text != "") { textBox2.Text = "5"; }
            if (box_int > 10 && textBox2.Text != "") { textBox2.Text = "5"; }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            int box_int = 0; Int32.TryParse(textBox3.Text, out box_int);
            if (box_int < 1 && textBox3.Text != "") { textBox3.Text = "5"; }
            if (box_int > 10 && textBox3.Text != "") { textBox3.Text = "5"; }
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            int box_int = 0; Int32.TryParse(maxB.Text, out box_int);
            if (box_int < 0 && maxB.Text != "") { maxB.Text = "0"; }
            if (box_int > 255 && maxB.Text != "") { maxB.Text = "255"; }
        }

        private void checkBoxLog_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
