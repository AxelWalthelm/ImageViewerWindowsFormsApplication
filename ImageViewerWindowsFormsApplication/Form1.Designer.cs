namespace ImageViewerWindowsFormsApplication
{
    partial class Form1
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
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.buttonOpen = new System.Windows.Forms.Button();
            this.checkBoxSmoothPixel = new System.Windows.Forms.CheckBox();
            this.buttonClear = new System.Windows.Forms.Button();
            this.comboBoxVisualizationMode = new System.Windows.Forms.ComboBox();
            this.imageZoomView1 = new ImageViewerWindowsFormsApplication.ImageZoomView();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.Location = new System.Drawing.Point(172, 93);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(100, 156);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // buttonOpen
            // 
            this.buttonOpen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOpen.Location = new System.Drawing.Point(172, 13);
            this.buttonOpen.Name = "buttonOpen";
            this.buttonOpen.Size = new System.Drawing.Size(78, 23);
            this.buttonOpen.TabIndex = 1;
            this.buttonOpen.Text = "Open...";
            this.buttonOpen.UseVisualStyleBackColor = true;
            this.buttonOpen.Click += new System.EventHandler(this.buttonOpen_Click);
            // 
            // checkBoxSmoothPixel
            // 
            this.checkBoxSmoothPixel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxSmoothPixel.Location = new System.Drawing.Point(172, 70);
            this.checkBoxSmoothPixel.Name = "checkBoxSmoothPixel";
            this.checkBoxSmoothPixel.Size = new System.Drawing.Size(100, 17);
            this.checkBoxSmoothPixel.TabIndex = 4;
            this.checkBoxSmoothPixel.Text = "smooth pixel";
            this.checkBoxSmoothPixel.UseVisualStyleBackColor = true;
            this.checkBoxSmoothPixel.CheckedChanged += new System.EventHandler(this.checkBoxSmoothPixel_CheckedChanged);
            // 
            // buttonClear
            // 
            this.buttonClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClear.Location = new System.Drawing.Point(249, 13);
            this.buttonClear.Name = "buttonClear";
            this.buttonClear.Size = new System.Drawing.Size(23, 23);
            this.buttonClear.TabIndex = 2;
            this.buttonClear.Text = "X";
            this.buttonClear.UseVisualStyleBackColor = true;
            this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);
            // 
            // comboBoxVisualizationMode
            // 
            this.comboBoxVisualizationMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxVisualizationMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxVisualizationMode.FormattingEnabled = true;
            this.comboBoxVisualizationMode.Location = new System.Drawing.Point(172, 43);
            this.comboBoxVisualizationMode.Name = "comboBoxVisualizationMode";
            this.comboBoxVisualizationMode.Size = new System.Drawing.Size(100, 21);
            this.comboBoxVisualizationMode.TabIndex = 3;
            this.comboBoxVisualizationMode.SelectedIndexChanged += new System.EventHandler(this.comboBoxVisualizationMode_SelectedIndexChanged);
            // 
            // imageZoomView1
            // 
            this.imageZoomView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.imageZoomView1.BackColor = System.Drawing.Color.Black;
            this.imageZoomView1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.imageZoomView1.ForeColor = System.Drawing.Color.White;
            this.imageZoomView1.Image = null;
            this.imageZoomView1.Location = new System.Drawing.Point(13, 13);
            this.imageZoomView1.MaximumPixelSize = 42D;
            this.imageZoomView1.Name = "imageZoomView1";
            this.imageZoomView1.Size = new System.Drawing.Size(153, 236);
            this.imageZoomView1.TabIndex = 0;
            this.imageZoomView1.Text = "No Image";
            this.imageZoomView1.ZoomAreaVisualizationSize = 0.15D;
            this.imageZoomView1.ZoomVisualization = ImageViewerWindowsFormsApplication.ImageZoomView.ZoomVisualizationMode.AreasAndScale;
            this.imageZoomView1.Paint += new System.Windows.Forms.PaintEventHandler(this.imageZoomView1_Paint);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.comboBoxVisualizationMode);
            this.Controls.Add(this.checkBoxSmoothPixel);
            this.Controls.Add(this.buttonClear);
            this.Controls.Add(this.buttonOpen);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.imageZoomView1);
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "ImageZoomView – Test Application";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private ImageZoomView imageZoomView1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button buttonOpen;
        private System.Windows.Forms.CheckBox checkBoxSmoothPixel;
        private System.Windows.Forms.Button buttonClear;
        private System.Windows.Forms.ComboBox comboBoxVisualizationMode;
    }
}

