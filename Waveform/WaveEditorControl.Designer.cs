namespace WaveEditor.Waveform
{
	partial class WaveEditorControl
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		private System.Windows.Forms.Panel pnlWaveforms;
		private System.Windows.Forms.HScrollBar HScrollbar;
		private System.Windows.Forms.PictureBox WaveformImage;
		private System.Windows.Forms.Label lblZoomText;
		private System.Windows.Forms.Button BtnZoomIn;
		private System.Windows.Forms.Button BtnZoomOut;
		private System.Windows.Forms.Button BtnZoomOnSelection;
		private System.Windows.Forms.Button BtnZoomInAmp;
		private System.Windows.Forms.Button BtnZoomOutAmp;
		private System.Windows.Forms.Button BtnZoomMul2;
		private System.Windows.Forms.Button BtnZoomDiv2;
		private System.Windows.Forms.Label lblAudioInfo;
		private System.Windows.Forms.Label lblSelectionInfo;
		private System.Windows.Forms.FlowLayoutPanel flowBottomBar;
		
		/// <summary>
		/// Disposes resources used by the control.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
			this.pnlWaveforms = new System.Windows.Forms.Panel();
			this.WaveformImage = new System.Windows.Forms.PictureBox();
			this.HScrollbar = new System.Windows.Forms.HScrollBar();
			this.lblZoomText = new System.Windows.Forms.Label();
			this.BtnZoomIn = new System.Windows.Forms.Button();
			this.BtnZoomOut = new System.Windows.Forms.Button();
			this.BtnZoomOnSelection = new System.Windows.Forms.Button();
			this.BtnZoomInAmp = new System.Windows.Forms.Button();
			this.BtnZoomOutAmp = new System.Windows.Forms.Button();
			this.BtnZoomMul2 = new System.Windows.Forms.Button();
			this.BtnZoomDiv2 = new System.Windows.Forms.Button();
			this.lblAudioInfo = new System.Windows.Forms.Label();
			this.lblSelectionInfo = new System.Windows.Forms.Label();
			this.flowBottomBar = new System.Windows.Forms.FlowLayoutPanel();
			this.pnlWaveforms.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.WaveformImage)).BeginInit();
			this.flowBottomBar.SuspendLayout();
			this.SuspendLayout();
			// 
			// pnlWaveforms
			// 
			this.pnlWaveforms.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.pnlWaveforms.BackColor = System.Drawing.SystemColors.Control;
			this.pnlWaveforms.Controls.Add(this.WaveformImage);
			this.pnlWaveforms.Controls.Add(this.HScrollbar);
			this.pnlWaveforms.Location = new System.Drawing.Point(18, 41);
			this.pnlWaveforms.Name = "pnlWaveforms";
			this.pnlWaveforms.Size = new System.Drawing.Size(875, 301);
			this.pnlWaveforms.TabIndex = 1;
			// 
			// WaveformImage
			// 
			this.WaveformImage.BackColor = System.Drawing.SystemColors.GrayText;
			this.WaveformImage.Dock = System.Windows.Forms.DockStyle.Fill;
			this.WaveformImage.Location = new System.Drawing.Point(0, 0);
			this.WaveformImage.Name = "WaveformImage";
			this.WaveformImage.Size = new System.Drawing.Size(875, 281);
			this.WaveformImage.TabIndex = 1;
			this.WaveformImage.TabStop = false;
			this.WaveformImage.MouseMove += new System.Windows.Forms.MouseEventHandler(this.WaveformImageMouseMove);
			// 
			// HScrollbar
			// 
			this.HScrollbar.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.HScrollbar.Location = new System.Drawing.Point(0, 281);
			this.HScrollbar.Name = "HScrollbar";
			this.HScrollbar.Size = new System.Drawing.Size(875, 20);
			this.HScrollbar.TabIndex = 0;
			this.HScrollbar.ValueChanged += new System.EventHandler(this.Slider1ValueChanged);
			// 
			// lblZoomText
			// 
			this.lblZoomText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.lblZoomText.Location = new System.Drawing.Point(851, 6);
			this.lblZoomText.Name = "lblZoomText";
			this.lblZoomText.Size = new System.Drawing.Size(57, 23);
			this.lblZoomText.TabIndex = 4;
			this.lblZoomText.Text = "ZoomText";
			// 
			// BtnZoomIn
			// 
			this.BtnZoomIn.Location = new System.Drawing.Point(3, 3);
			this.BtnZoomIn.Name = "BtnZoomIn";
			this.BtnZoomIn.Size = new System.Drawing.Size(56, 23);
			this.BtnZoomIn.TabIndex = 5;
			this.BtnZoomIn.Text = "Zoom in";
			this.BtnZoomIn.UseVisualStyleBackColor = true;
			this.BtnZoomIn.Click += new System.EventHandler(this.BtnZoomInClick);
			// 
			// BtnZoomOut
			// 
			this.BtnZoomOut.Location = new System.Drawing.Point(65, 3);
			this.BtnZoomOut.Name = "BtnZoomOut";
			this.BtnZoomOut.Size = new System.Drawing.Size(62, 23);
			this.BtnZoomOut.TabIndex = 6;
			this.BtnZoomOut.Text = "Zoom out";
			this.BtnZoomOut.UseVisualStyleBackColor = true;
			this.BtnZoomOut.Click += new System.EventHandler(this.BtnZoomOutClick);
			// 
			// BtnZoomOnSelection
			// 
			this.BtnZoomOnSelection.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.BtnZoomOnSelection.Location = new System.Drawing.Point(133, 3);
			this.BtnZoomOnSelection.Name = "BtnZoomOnSelection";
			this.BtnZoomOnSelection.Size = new System.Drawing.Size(104, 23);
			this.BtnZoomOnSelection.TabIndex = 7;
			this.BtnZoomOnSelection.Text = "Zoom on selection";
			this.BtnZoomOnSelection.UseVisualStyleBackColor = true;
			this.BtnZoomOnSelection.Click += new System.EventHandler(this.BtnZoomOnSelectionClick);
			// 
			// BtnZoomInAmp
			// 
			this.BtnZoomInAmp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.BtnZoomInAmp.Location = new System.Drawing.Point(333, 3);
			this.BtnZoomInAmp.Name = "BtnZoomInAmp";
			this.BtnZoomInAmp.Size = new System.Drawing.Size(78, 23);
			this.BtnZoomInAmp.TabIndex = 8;
			this.BtnZoomInAmp.Text = "Zoom in amp";
			this.BtnZoomInAmp.UseVisualStyleBackColor = true;
			this.BtnZoomInAmp.Click += new System.EventHandler(this.BtnZoomInAmpClick);
			// 
			// BtnZoomOutAmp
			// 
			this.BtnZoomOutAmp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.BtnZoomOutAmp.Location = new System.Drawing.Point(243, 3);
			this.BtnZoomOutAmp.Name = "BtnZoomOutAmp";
			this.BtnZoomOutAmp.Size = new System.Drawing.Size(84, 23);
			this.BtnZoomOutAmp.TabIndex = 8;
			this.BtnZoomOutAmp.Text = "Zoom out amp";
			this.BtnZoomOutAmp.UseVisualStyleBackColor = true;
			this.BtnZoomOutAmp.Click += new System.EventHandler(this.BtnZoomOutAmpClick);
			// 
			// BtnZoomMul2
			// 
			this.BtnZoomMul2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.BtnZoomMul2.Location = new System.Drawing.Point(468, 3);
			this.BtnZoomMul2.Name = "BtnZoomMul2";
			this.BtnZoomMul2.Size = new System.Drawing.Size(45, 23);
			this.BtnZoomMul2.TabIndex = 9;
			this.BtnZoomMul2.Text = "X2";
			this.BtnZoomMul2.UseVisualStyleBackColor = true;
			this.BtnZoomMul2.Click += new System.EventHandler(this.BtnZoomMul2Click);
			// 
			// BtnZoomDiv2
			// 
			this.BtnZoomDiv2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.BtnZoomDiv2.Location = new System.Drawing.Point(417, 3);
			this.BtnZoomDiv2.Name = "BtnZoomDiv2";
			this.BtnZoomDiv2.Size = new System.Drawing.Size(45, 23);
			this.BtnZoomDiv2.TabIndex = 10;
			this.BtnZoomDiv2.Text = "/2";
			this.BtnZoomDiv2.UseVisualStyleBackColor = true;
			this.BtnZoomDiv2.Click += new System.EventHandler(this.BtnZoomDiv2Click);
			// 
			// lblAudioInfo
			// 
			this.lblAudioInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.lblAudioInfo.Location = new System.Drawing.Point(519, 6);
			this.lblAudioInfo.Name = "lblAudioInfo";
			this.lblAudioInfo.Size = new System.Drawing.Size(179, 23);
			this.lblAudioInfo.TabIndex = 11;
			this.lblAudioInfo.Text = "Audio Info";
			// 
			// lblSelectionInfo
			// 
			this.lblSelectionInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.lblSelectionInfo.Location = new System.Drawing.Point(704, 6);
			this.lblSelectionInfo.Name = "lblSelectionInfo";
			this.lblSelectionInfo.Size = new System.Drawing.Size(141, 23);
			this.lblSelectionInfo.TabIndex = 12;
			this.lblSelectionInfo.Text = "Selection Info";
			// 
			// flowBottomBar
			// 
			this.flowBottomBar.AutoSize = true;
			this.flowBottomBar.Controls.Add(this.BtnZoomIn);
			this.flowBottomBar.Controls.Add(this.BtnZoomOut);
			this.flowBottomBar.Controls.Add(this.BtnZoomOnSelection);
			this.flowBottomBar.Controls.Add(this.BtnZoomOutAmp);
			this.flowBottomBar.Controls.Add(this.BtnZoomInAmp);
			this.flowBottomBar.Controls.Add(this.BtnZoomDiv2);
			this.flowBottomBar.Controls.Add(this.BtnZoomMul2);
			this.flowBottomBar.Controls.Add(this.lblAudioInfo);
			this.flowBottomBar.Controls.Add(this.lblSelectionInfo);
			this.flowBottomBar.Controls.Add(this.lblZoomText);
			this.flowBottomBar.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.flowBottomBar.Location = new System.Drawing.Point(0, 348);
			this.flowBottomBar.Name = "flowBottomBar";
			this.flowBottomBar.Size = new System.Drawing.Size(915, 29);
			this.flowBottomBar.TabIndex = 13;
			// 
			// WaveEditorControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.flowBottomBar);
			this.Controls.Add(this.pnlWaveforms);
			this.Name = "WaveEditorControl";
			this.Size = new System.Drawing.Size(915, 377);
			this.Resize += new System.EventHandler(this.WaveEditorControlResize);
			this.pnlWaveforms.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.WaveformImage)).EndInit();
			this.flowBottomBar.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
	}
}
