using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NReco.VideoInfo;

namespace VideoProcessor {
    public partial class AboutVideoForm : Form {
        public AboutVideoForm(string fileName) {
            InitializeComponent();
            var ffProbe = new FFProbe();
            MediaInfo videoInfo = ffProbe.GetMediaInfo(fileName);
            MediaInfo.StreamInfo videoStream = videoInfo.Streams.FirstOrDefault(item => item.CodecType == "video");
            if (videoStream == null)
            {
                Close();
                MessageBox.Show("Video stream doesn't found");
                return;
            }
            FileInfo file = new FileInfo(fileName);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("Name: {0}", file.Name));
            sb.AppendLine(string.Format("Duration: {0}", videoInfo.Duration));
            sb.AppendLine(string.Format("Size: {0}x{1}", videoStream.Width, videoStream.Height));
            sb.AppendLine(string.Format("Frame rate: {0}", videoStream.FrameRate));
            sb.AppendLine(string.Format("Video codec: {0} ({1})", videoStream.CodecName, videoStream.CodecLongName));
            sb.AppendLine(string.Format("Pixel format: {0}", videoStream.PixelFormat));
            if (videoStream.Tags.Any())
            {
                sb.AppendLine(string.Format("Tags: {0}", string.Join(", ", videoStream.Tags.Select(item => item.Key + " = " + item.Value))));
            }

            textBox.Text = sb.ToString();
            textBox.SelectionStart = 0;
            textBox.SelectionLength = 0;
        }
    }
}
