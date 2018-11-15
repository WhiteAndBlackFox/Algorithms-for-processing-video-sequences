using System.Drawing;
using AForge.Controls;
using VideoProcessor.Controls;

namespace VideoProcessor
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.mainMenuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openVideofileusingDirectShowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openVideoVwfToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openMjpegToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.videoInfoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.videoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.filtersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.metricsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.сохранитьИсходныйКадрToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.сохранитьОбработаннуюРамкуToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.processedVideoPanel = new System.Windows.Forms.Panel();
            this.processedPicture = new System.Windows.Forms.PictureBox();
            this.labelTimeProgressStart = new System.Windows.Forms.Label();
            this.panelVideoProgress = new System.Windows.Forms.Panel();
            this.labelTimeProgressFinish = new System.Windows.Forms.Label();
            this.progressBar = new VideoProcessor.Controls.UpdatableProgressBar();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.buttonPlay = new System.Windows.Forms.PictureBox();
            this.sourceVideoPanel = new System.Windows.Forms.Panel();
            this.videoPlayer = new AForge.Controls.VideoSourcePlayer();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.fpsLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.mainMenuStrip.SuspendLayout();
            this.processedVideoPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.processedPicture)).BeginInit();
            this.panelVideoProgress.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.buttonPlay)).BeginInit();
            this.sourceVideoPanel.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenuStrip
            // 
            this.mainMenuStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.mainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.videoToolStripMenuItem});
            this.mainMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.mainMenuStrip.Name = "mainMenuStrip";
            this.mainMenuStrip.Size = new System.Drawing.Size(808, 24);
            this.mainMenuStrip.TabIndex = 0;
            this.mainMenuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openVideofileusingDirectShowToolStripMenuItem,
            this.openVideoVwfToolStripMenuItem,
            this.openMjpegToolStripMenuItem,
            this.toolStripSeparator1,
            this.videoInfoToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.fileToolStripMenuItem.Text = "Файл";
            // 
            // openVideofileusingDirectShowToolStripMenuItem
            // 
            this.openVideofileusingDirectShowToolStripMenuItem.Name = "openVideofileusingDirectShowToolStripMenuItem";
            this.openVideofileusingDirectShowToolStripMenuItem.Size = new System.Drawing.Size(227, 22);
            this.openVideofileusingDirectShowToolStripMenuItem.Text = "Открыть видео (DirectShow)";
            this.openVideofileusingDirectShowToolStripMenuItem.Click += new System.EventHandler(this.openVideofileusingDirectShowToolStripMenuItem_Click);
            // 
            // openVideoVwfToolStripMenuItem
            // 
            this.openVideoVwfToolStripMenuItem.Name = "openVideoVwfToolStripMenuItem";
            this.openVideoVwfToolStripMenuItem.Size = new System.Drawing.Size(227, 22);
            this.openVideoVwfToolStripMenuItem.Text = "Открыть видео (VFW)";
            this.openVideoVwfToolStripMenuItem.Click += new System.EventHandler(this.openVideofileusingDirectShowToolStripMenuItem_Click);
            // 
            // openMjpegToolStripMenuItem
            // 
            this.openMjpegToolStripMenuItem.Name = "openMjpegToolStripMenuItem";
            this.openMjpegToolStripMenuItem.Size = new System.Drawing.Size(227, 22);
            this.openMjpegToolStripMenuItem.Text = "Открыть камеру";
            this.openMjpegToolStripMenuItem.Click += new System.EventHandler(this.openMjpegToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(254, 6);
            // 
            // videoInfoToolStripMenuItem
            // 
            this.videoInfoToolStripMenuItem.Enabled = false;
            this.videoInfoToolStripMenuItem.Name = "videoInfoToolStripMenuItem";
            this.videoInfoToolStripMenuItem.Size = new System.Drawing.Size(257, 22);
            this.videoInfoToolStripMenuItem.Text = "Информация о видео";
            this.videoInfoToolStripMenuItem.Click += new System.EventHandler(this.videoInfoToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(254, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(227, 22);
            this.exitToolStripMenuItem.Text = "Выход";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // videoToolStripMenuItem
            // 
            this.videoToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.filtersToolStripMenuItem,
            this.metricsToolStripMenuItem,
            this.toolStripSeparator2,
            this.сохранитьИсходныйКадрToolStripMenuItem,
            this.сохранитьОбработаннуюРамкуToolStripMenuItem});
            this.videoToolStripMenuItem.Name = "videoToolStripMenuItem";
            this.videoToolStripMenuItem.Size = new System.Drawing.Size(49, 20);
            this.videoToolStripMenuItem.Text = "Лабы";
            // 
            // filtersToolStripMenuItem
            // 
            this.filtersToolStripMenuItem.Name = "filtersToolStripMenuItem";
            this.filtersToolStripMenuItem.Size = new System.Drawing.Size(245, 22);
            this.filtersToolStripMenuItem.Text = "Эффекты";
            this.filtersToolStripMenuItem.Click += new System.EventHandler(this.filtersToolStripMenuItem_Click);
            // 
            // metricsToolStripMenuItem
            // 
            this.metricsToolStripMenuItem.Name = "metricsToolStripMenuItem";
            this.metricsToolStripMenuItem.Size = new System.Drawing.Size(245, 22);
            this.metricsToolStripMenuItem.Text = "Графики";
            this.metricsToolStripMenuItem.Click += new System.EventHandler(this.metricsToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(242, 6);
            // 
            // сохранитьИсходныйКадрToolStripMenuItem
            // 
            this.сохранитьИсходныйКадрToolStripMenuItem.Name = "сохранитьИсходныйКадрToolStripMenuItem";
            this.сохранитьИсходныйКадрToolStripMenuItem.Size = new System.Drawing.Size(245, 22);
            this.сохранитьИсходныйКадрToolStripMenuItem.Text = "Сохранить исходный кадр";
            this.сохранитьИсходныйКадрToolStripMenuItem.Click += new System.EventHandler(this.сохранитьИсходныйКадрToolStripMenuItem_Click);
            // 
            // сохранитьОбработаннуюРамкуToolStripMenuItem
            // 
            this.сохранитьОбработаннуюРамкуToolStripMenuItem.Name = "сохранитьОбработаннуюРамкуToolStripMenuItem";
            this.сохранитьОбработаннуюРамкуToolStripMenuItem.Size = new System.Drawing.Size(245, 22);
            this.сохранитьОбработаннуюРамкуToolStripMenuItem.Text = "Сохранить обработанный кадр";
            this.сохранитьОбработаннуюРамкуToolStripMenuItem.Click += new System.EventHandler(this.сохранитьОбработаннуюРамкуToolStripMenuItem_Click);
            // 
            // timer
            // 
            this.timer.Interval = 1000;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "Video files|*.avi;*.mp4|All files (*.*)|*.*";
            this.openFileDialog.Title = "Opem movie";
            // 
            // processedVideoPanel
            // 
            this.processedVideoPanel.Controls.Add(this.processedPicture);
            this.processedVideoPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.processedVideoPanel.Location = new System.Drawing.Point(403, 24);
            this.processedVideoPanel.Name = "processedVideoPanel";
            this.processedVideoPanel.Size = new System.Drawing.Size(405, 310);
            this.processedVideoPanel.TabIndex = 3;
            // 
            // processedPicture
            // 
            this.processedPicture.Dock = System.Windows.Forms.DockStyle.Fill;
            this.processedPicture.Location = new System.Drawing.Point(0, 0);
            this.processedPicture.Name = "processedPicture";
            this.processedPicture.Size = new System.Drawing.Size(405, 310);
            this.processedPicture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.processedPicture.TabIndex = 0;
            this.processedPicture.TabStop = false;
            this.processedPicture.Click += new System.EventHandler(this.processedPicture_Click);
            // 
            // labelTimeProgressStart
            // 
            this.labelTimeProgressStart.Dock = System.Windows.Forms.DockStyle.Left;
            this.labelTimeProgressStart.Location = new System.Drawing.Point(76, 0);
            this.labelTimeProgressStart.Name = "labelTimeProgressStart";
            this.labelTimeProgressStart.Size = new System.Drawing.Size(197, 28);
            this.labelTimeProgressStart.TabIndex = 4;
            this.labelTimeProgressStart.Text = "--:-- / --:--";
            this.labelTimeProgressStart.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelVideoProgress
            // 
            this.panelVideoProgress.Controls.Add(this.labelTimeProgressFinish);
            this.panelVideoProgress.Controls.Add(this.progressBar);
            this.panelVideoProgress.Controls.Add(this.labelTimeProgressStart);
            this.panelVideoProgress.Controls.Add(this.panel1);
            this.panelVideoProgress.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelVideoProgress.Location = new System.Drawing.Point(0, 334);
            this.panelVideoProgress.Name = "panelVideoProgress";
            this.panelVideoProgress.Size = new System.Drawing.Size(808, 28);
            this.panelVideoProgress.TabIndex = 5;
            // 
            // labelTimeProgressFinish
            // 
            this.labelTimeProgressFinish.Dock = System.Windows.Forms.DockStyle.Right;
            this.labelTimeProgressFinish.Location = new System.Drawing.Point(798, 0);
            this.labelTimeProgressFinish.Name = "labelTimeProgressFinish";
            this.labelTimeProgressFinish.Size = new System.Drawing.Size(10, 28);
            this.labelTimeProgressFinish.TabIndex = 10;
            this.labelTimeProgressFinish.Text = "--:--";
            this.labelTimeProgressFinish.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelTimeProgressFinish.Visible = false;
            this.labelTimeProgressFinish.Click += new System.EventHandler(this.labelTimeProgressFinish_Click);
            // 
            // progressBar
            // 
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBar.Location = new System.Drawing.Point(273, 0);
            this.progressBar.MarqueeAnimationSpeed = 0;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(535, 28);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar.TabIndex = 1;
            this.progressBar.ValueChanged += new VideoProcessor.Controls.UpdatableProgressBar.ValueChange(this.progressBar_ValueChanged);
            this.progressBar.Click += new System.EventHandler(this.progressBar_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.pictureBox2);
            this.panel1.Controls.Add(this.buttonPlay);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(76, 28);
            this.panel1.TabIndex = 6;
            this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = global::VideoProcessor.Properties.Resources.if_Stop1Disabled_22942;
            this.pictureBox2.Location = new System.Drawing.Point(37, 0);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(36, 28);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox2.TabIndex = 1;
            this.pictureBox2.TabStop = false;
            this.pictureBox2.Click += new System.EventHandler(this.pictureBox2_Click);
            // 
            // buttonPlay
            // 
            this.buttonPlay.Image = global::VideoProcessor.Properties.Resources.if_StepForwardDisabled_22933;
            this.buttonPlay.Location = new System.Drawing.Point(2, 0);
            this.buttonPlay.Name = "buttonPlay";
            this.buttonPlay.Size = new System.Drawing.Size(39, 28);
            this.buttonPlay.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.buttonPlay.TabIndex = 0;
            this.buttonPlay.TabStop = false;
            this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
            // 
            // sourceVideoPanel
            // 
            this.sourceVideoPanel.Controls.Add(this.videoPlayer);
            this.sourceVideoPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.sourceVideoPanel.Location = new System.Drawing.Point(0, 24);
            this.sourceVideoPanel.Name = "sourceVideoPanel";
            this.sourceVideoPanel.Size = new System.Drawing.Size(403, 310);
            this.sourceVideoPanel.TabIndex = 2;
            // 
            // videoPlayer
            // 
            this.videoPlayer.AutoSizeControl = true;
            this.videoPlayer.BackColor = System.Drawing.SystemColors.Control;
            this.videoPlayer.BorderColor = System.Drawing.SystemColors.Control;
            this.videoPlayer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.videoPlayer.ForeColor = System.Drawing.Color.White;
            this.videoPlayer.KeepAspectRatio = true;
            this.videoPlayer.Location = new System.Drawing.Point(0, 0);
            this.videoPlayer.Name = "videoPlayer";
            this.videoPlayer.Size = new System.Drawing.Size(403, 310);
            this.videoPlayer.TabIndex = 0;
            this.videoPlayer.VideoSource = null;
            this.videoPlayer.NewFrame += new AForge.Controls.VideoSourcePlayer.NewFrameHandler(this.videoSourcePlayer_NewFrame);
            this.videoPlayer.Click += new System.EventHandler(this.videoPlayer_Click_1);
            // 
            // statusStrip
            // 
            this.statusStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fpsLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 362);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(808, 22);
            this.statusStrip.TabIndex = 1;
            this.statusStrip.Text = "statusStrip1";
            // 
            // fpsLabel
            // 
            this.fpsLabel.Name = "fpsLabel";
            this.fpsLabel.Size = new System.Drawing.Size(793, 17);
            this.fpsLabel.Spring = true;
            this.fpsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(808, 384);
            this.Controls.Add(this.processedVideoPanel);
            this.Controls.Add(this.sourceVideoPanel);
            this.Controls.Add(this.panelVideoProgress);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.mainMenuStrip);
            this.MainMenuStrip = this.mainMenuStrip;
            this.Name = "MainForm";
            this.Text = "Video labs";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Shown += new System.EventHandler(this.MainForm_SizeChanged);
            this.SizeChanged += new System.EventHandler(this.MainForm_SizeChanged);
            this.Resize += new System.EventHandler(this.MainForm_SizeChanged);
            this.mainMenuStrip.ResumeLayout(false);
            this.mainMenuStrip.PerformLayout();
            this.processedVideoPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.processedPicture)).EndInit();
            this.panelVideoProgress.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.buttonPlay)).EndInit();
            this.sourceVideoPanel.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip mainMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.ToolStripMenuItem openVideofileusingDirectShowToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Panel processedVideoPanel;
        private System.Windows.Forms.PictureBox processedPicture;
        private System.Windows.Forms.ToolStripMenuItem videoInfoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openVideoVwfToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private UpdatableProgressBar progressBar;
        private System.Windows.Forms.Label labelTimeProgressStart;
        private System.Windows.Forms.Panel panelVideoProgress;
        private System.Windows.Forms.ToolStripMenuItem videoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem filtersToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem metricsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openMjpegToolStripMenuItem;
        private System.Windows.Forms.Panel sourceVideoPanel;
        private VideoSourcePlayer videoPlayer;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel fpsLabel;
        private System.Windows.Forms.Label labelTimeProgressFinish;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.PictureBox buttonPlay;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem сохранитьИсходныйКадрToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem сохранитьОбработаннуюРамкуToolStripMenuItem;
    }
}

