using System;
using System.Windows.Forms;
using VideoProcessor.Helpers;
using VideoProcessor.Model;

namespace VideoProcessor {
    public partial class MetricsForm : Form
    {
        private int _frameNumber;
        public MetricsForm() {
            InitializeComponent();
            chartMetrics.SetCheckBoxes(checkBox1, checkBox2, checkBox3);
            _frameNumber = 0;
        }

        public void AddData(ProcessingInfo processingInfo)
        {
            Invoke((MethodInvoker)delegate
            {
                _frameNumber++;
                chartTime.Series[0].Points.AddXY(_frameNumber, processingInfo.Time);
                chartMetrics.Series[0].Points.AddXY(_frameNumber, processingInfo.Mse);
                chartMetrics.Series[1].Points.AddXY(_frameNumber, processingInfo.Psnr);
                chartMetrics.Series[2].Points.AddXY(_frameNumber, processingInfo.Bfm);

                if (textBoxLog.Text.Length == 0)
                {
                    textBoxLog.Text += @"Frame; Time (Sec); MSE; PSNR; MBF" + Environment.NewLine;
                }
                textBoxLog.Text += string.Format("{0}; {1:F7}; {2}; {3}; {4};{5}",
                    _frameNumber, processingInfo.Time, 
                    processingInfo.Mse, processingInfo.Psnr, processingInfo.Bfm, Environment.NewLine);
            });
        }

        public void AddData(double time, Frame frame1, Frame frame2) {
            Invoke((MethodInvoker)delegate {
                ProcessingInfo processingInfo = new ProcessingInfo(time, frame1, frame2);
                _frameNumber++;
                chartTime.Series[0].Points.AddXY(_frameNumber, processingInfo.Time);
                chartMetrics.Series[0].Points.AddXY(_frameNumber, processingInfo.Mse);
                chartMetrics.Series[1].Points.AddXY(_frameNumber, processingInfo.Psnr);
                chartMetrics.Series[2].Points.AddXY(_frameNumber, processingInfo.Bfm);

                if (textBoxLog.Text.Length == 0) {
                    textBoxLog.Text += @"Frame; Time (Sec); MSE; PSNR; MBF" + Environment.NewLine;
                }
                textBoxLog.Text += string.Format("{0}; {1:F7}; {2}; {3}; {4};{5}",
                    _frameNumber, processingInfo.Time,
                    processingInfo.Mse, processingInfo.Psnr, processingInfo.Bfm, Environment.NewLine);
            });
        }

        public void AddData(uint time, float fps) {
            Invoke((MethodInvoker)delegate {
                chartFps.Series[0].Points.AddXY(time, fps);
            });
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            e.Cancel = true;
            Hide();
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            foreach (var series in chartTime.Series)
            {
                series.Points.Clear();
            }

            foreach (var series in chartFps.Series) {
                series.Points.Clear();
            }

            foreach (var series in chartMetrics.Series) {
                series.Points.Clear();
            }

            textBoxLog.Text = "";
        }
    }
}
