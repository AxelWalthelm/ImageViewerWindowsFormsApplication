﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageViewerWindowsFormsApplication
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            checkBox1_CheckedChanged(this, new EventArgs());

#if true
            SetImage(@"C:\Users\Axel\Pictures\test.png");
#endif
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Open Image";
                dlg.Filter = "Image Files (*.bmp;*.jpg;*.jpeg,*.png)|*.BMP;*.JPG;*.JPEG;*.PNG";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    string fileName = dlg.FileName;
                    SetImage(fileName);
                }
            }
        }

        private void SetImage(string fileName)
        {
            this.Text = Regex.Replace(this.Text, @"(?<=\s\p{Pd}\s).*$", "") + fileName;

            Image image = Image.FromFile(fileName);
            this.pictureBox1.Image = image;
            this.imageZoomView1.Image = image;
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            this.pictureBox1.Image = null;
            this.imageZoomView1.Image = null;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.imageZoomView1.ShowPixelBorders = !checkBox1.Checked;
        }
    }
}
