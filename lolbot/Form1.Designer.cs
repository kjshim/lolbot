namespace lolbot
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
            this.components = new System.ComponentModel.Container();
            this.button1 = new System.Windows.Forms.Button();
            this.listLog = new System.Windows.Forms.ListBox();
            this.pCapture = new System.Windows.Forms.PictureBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.saveImgFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.chooseFile = new System.Windows.Forms.Button();
            this.openImgFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.button2 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pCapture)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(22, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // listLog
            // 
            this.listLog.FormattingEnabled = true;
            this.listLog.ItemHeight = 12;
            this.listLog.Location = new System.Drawing.Point(22, 136);
            this.listLog.Name = "listLog";
            this.listLog.Size = new System.Drawing.Size(264, 208);
            this.listLog.TabIndex = 1;
            // 
            // pCapture
            // 
            this.pCapture.Location = new System.Drawing.Point(292, 52);
            this.pCapture.Name = "pCapture";
            this.pCapture.Size = new System.Drawing.Size(674, 437);
            this.pCapture.TabIndex = 2;
            this.pCapture.TabStop = false;
            // 
            // timer1
            // 
            this.timer1.Interval = 300;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // chooseFile
            // 
            this.chooseFile.Location = new System.Drawing.Point(292, 20);
            this.chooseFile.Name = "chooseFile";
            this.chooseFile.Size = new System.Drawing.Size(160, 26);
            this.chooseFile.TabIndex = 3;
            this.chooseFile.Text = "saveCapture";
            this.chooseFile.UseVisualStyleBackColor = true;
            this.chooseFile.Click += new System.EventHandler(this.chooseFile_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(462, 23);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(151, 23);
            this.button2.TabIndex = 4;
            this.button2.Text = "Test Process Image";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(993, 506);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.chooseFile);
            this.Controls.Add(this.pCapture);
            this.Controls.Add(this.listLog);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.From1_Closed);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pCapture)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ListBox listLog;
        private System.Windows.Forms.PictureBox pCapture;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.SaveFileDialog saveImgFileDialog1;
        private System.Windows.Forms.Button chooseFile;
        private System.Windows.Forms.OpenFileDialog openImgFileDialog1;
        private System.Windows.Forms.Button button2;
    }
}

