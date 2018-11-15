using System;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace VideoProcessor.Helpers
{
    public static class ChartHelper
    {
        public static void SetSeriesVisible(this Chart chart, int index, bool visible)
        {
            chart.Series[index].Enabled = visible;
            chart.ChartAreas[0].AxisY.Maximum = Double.NaN;
            chart.ChartAreas[0].AxisY.Minimum = Double.NaN;
            chart.ChartAreas[0].RecalculateAxesScale();
        }

        public static void SetCheckBoxes(this Chart chart, CheckBox checkBox1, CheckBox checkBox2, CheckBox checkBox3)
        {
            chart.SetCheckBoxes(new []{checkBox1, checkBox2, checkBox3});
        }

        public static void SetCheckBoxes(this Chart chart, CheckBox[] checkBoxes)
        {
            UiComponentsHelper.OneCheckedAlways(checkBoxes);
            for (var index = 0; index < checkBoxes.Length; index++)
            {
                var item = checkBoxes[index];
                var legendIndex = index;
                item.CheckedChanged += (sender, args) =>
                {
                    var checkBox = (CheckBox) sender;
                    chart.SetSeriesVisible(legendIndex, checkBox.Checked);
                };
            }
        }
    }
}
