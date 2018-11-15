using System;
using System.Windows.Forms;
using Cursors = System.Windows.Forms.Cursors;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace VideoProcessor.Controls {
    public class UpdatableProgressBar : ProgressBar {
        public delegate void ValueChange(double oldValue, double newValue);

        public event ValueChange ValueChanged;

        public UpdatableProgressBar() {
            MouseDown += this_MouseDown;
            MouseEnter += this_MouseEnter;
            MouseLeave += this_MouseLeave;
        }

        private void this_MouseEnter(object sender, EventArgs e)
        {
            Cursor = Cursors.Hand;
        }

        private void this_MouseLeave(object sender, EventArgs e)
        {
            Cursor = Cursors.Arrow;
        }

        private int SetProgressBarValue(double mousePosition) {
            double ratio = mousePosition / Width;
            double progressBarValue = Math.Max(0, Math.Min(ratio * Maximum, Maximum));

            if (ValueChanged != null)
            {
                ValueChanged(Value, progressBarValue);
            }

            return (int)progressBarValue;
        }

        private void this_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left)
            {
                Value = SetProgressBarValue(e.Location.X);
            }
            else if (e.Button == MouseButtons.Right)
            {
                Value = 0;
            }
        }
    }
}
