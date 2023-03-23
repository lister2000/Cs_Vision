namespace Cs_Vision
{
    partial class Form2
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
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.label_thld = new System.Windows.Forms.Label();
            this.label_thldval = new System.Windows.Forms.Label();
            this.label_minareaval = new System.Windows.Forms.Label();
            this.label_minarea = new System.Windows.Forms.Label();
            this.trackBar2 = new System.Windows.Forms.TrackBar();
            this.label_maxareaval = new System.Windows.Forms.Label();
            this.label_maxarea = new System.Windows.Forms.Label();
            this.trackBar3 = new System.Windows.Forms.TrackBar();
            this.label_alikeval = new System.Windows.Forms.Label();
            this.label_alike = new System.Windows.Forms.Label();
            this.trackBar4 = new System.Windows.Forms.TrackBar();
            this.label_sideval = new System.Windows.Forms.Label();
            this.label_side = new System.Windows.Forms.Label();
            this.trackBar5 = new System.Windows.Forms.TrackBar();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar5)).BeginInit();
            this.SuspendLayout();
            // 
            // trackBar1
            // 
            this.trackBar1.Location = new System.Drawing.Point(94, 28);
            this.trackBar1.Maximum = 100;
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(279, 69);
            this.trackBar1.TabIndex = 0;
            this.trackBar1.Value = 50;
            this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
            // 
            // label_thld
            // 
            this.label_thld.AutoSize = true;
            this.label_thld.Location = new System.Drawing.Point(20, 28);
            this.label_thld.Name = "label_thld";
            this.label_thld.Size = new System.Drawing.Size(82, 24);
            this.label_thld.TabIndex = 1;
            this.label_thld.Text = "二值化值";
            // 
            // label_thldval
            // 
            this.label_thldval.AutoSize = true;
            this.label_thldval.Location = new System.Drawing.Point(381, 28);
            this.label_thldval.Name = "label_thldval";
            this.label_thldval.Size = new System.Drawing.Size(32, 24);
            this.label_thldval.TabIndex = 2;
            this.label_thldval.Text = "50";
            // 
            // label_minareaval
            // 
            this.label_minareaval.AutoSize = true;
            this.label_minareaval.Location = new System.Drawing.Point(381, 100);
            this.label_minareaval.Name = "label_minareaval";
            this.label_minareaval.Size = new System.Drawing.Size(32, 24);
            this.label_minareaval.TabIndex = 5;
            this.label_minareaval.Text = "50";
            // 
            // label_minarea
            // 
            this.label_minarea.AutoSize = true;
            this.label_minarea.Location = new System.Drawing.Point(20, 100);
            this.label_minarea.Name = "label_minarea";
            this.label_minarea.Size = new System.Drawing.Size(82, 24);
            this.label_minarea.TabIndex = 4;
            this.label_minarea.Text = "最小面积";
            // 
            // trackBar2
            // 
            this.trackBar2.Location = new System.Drawing.Point(94, 100);
            this.trackBar2.Maximum = 100;
            this.trackBar2.Name = "trackBar2";
            this.trackBar2.Size = new System.Drawing.Size(279, 69);
            this.trackBar2.TabIndex = 3;
            this.trackBar2.Value = 50;
            this.trackBar2.Scroll += new System.EventHandler(this.trackBar2_Scroll);
            // 
            // label_maxareaval
            // 
            this.label_maxareaval.AutoSize = true;
            this.label_maxareaval.Location = new System.Drawing.Point(381, 172);
            this.label_maxareaval.Name = "label_maxareaval";
            this.label_maxareaval.Size = new System.Drawing.Size(32, 24);
            this.label_maxareaval.TabIndex = 8;
            this.label_maxareaval.Text = "50";
            // 
            // label_maxarea
            // 
            this.label_maxarea.AutoSize = true;
            this.label_maxarea.Location = new System.Drawing.Point(20, 172);
            this.label_maxarea.Name = "label_maxarea";
            this.label_maxarea.Size = new System.Drawing.Size(82, 24);
            this.label_maxarea.TabIndex = 7;
            this.label_maxarea.Text = "最大面积";
            // 
            // trackBar3
            // 
            this.trackBar3.Location = new System.Drawing.Point(94, 172);
            this.trackBar3.Maximum = 100;
            this.trackBar3.Name = "trackBar3";
            this.trackBar3.Size = new System.Drawing.Size(279, 69);
            this.trackBar3.TabIndex = 6;
            this.trackBar3.Value = 50;
            this.trackBar3.Scroll += new System.EventHandler(this.trackBar3_Scroll);
            // 
            // label_alikeval
            // 
            this.label_alikeval.AutoSize = true;
            this.label_alikeval.Location = new System.Drawing.Point(381, 244);
            this.label_alikeval.Name = "label_alikeval";
            this.label_alikeval.Size = new System.Drawing.Size(32, 24);
            this.label_alikeval.TabIndex = 11;
            this.label_alikeval.Text = "50";
            // 
            // label_alike
            // 
            this.label_alike.AutoSize = true;
            this.label_alike.Location = new System.Drawing.Point(20, 244);
            this.label_alike.Name = "label_alike";
            this.label_alike.Size = new System.Drawing.Size(82, 24);
            this.label_alike.TabIndex = 10;
            this.label_alike.Text = "相似度值";
            // 
            // trackBar4
            // 
            this.trackBar4.Location = new System.Drawing.Point(94, 244);
            this.trackBar4.Maximum = 100;
            this.trackBar4.Name = "trackBar4";
            this.trackBar4.Size = new System.Drawing.Size(279, 69);
            this.trackBar4.TabIndex = 9;
            this.trackBar4.Value = 50;
            this.trackBar4.Scroll += new System.EventHandler(this.trackBar4_Scroll);
            // 
            // label_sideval
            // 
            this.label_sideval.AutoSize = true;
            this.label_sideval.Location = new System.Drawing.Point(381, 316);
            this.label_sideval.Name = "label_sideval";
            this.label_sideval.Size = new System.Drawing.Size(32, 24);
            this.label_sideval.TabIndex = 14;
            this.label_sideval.Text = "50";
            // 
            // label_side
            // 
            this.label_side.AutoSize = true;
            this.label_side.Location = new System.Drawing.Point(20, 316);
            this.label_side.Name = "label_side";
            this.label_side.Size = new System.Drawing.Size(82, 24);
            this.label_side.TabIndex = 13;
            this.label_side.Text = "形状约束";
            // 
            // trackBar5
            // 
            this.trackBar5.Location = new System.Drawing.Point(94, 316);
            this.trackBar5.Maximum = 100;
            this.trackBar5.Name = "trackBar5";
            this.trackBar5.Size = new System.Drawing.Size(279, 69);
            this.trackBar5.TabIndex = 12;
            this.trackBar5.Value = 50;
            this.trackBar5.SizeChanged += new System.EventHandler(this.trackBar5_Scroll);
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(507, 465);
            this.Controls.Add(this.label_sideval);
            this.Controls.Add(this.label_side);
            this.Controls.Add(this.trackBar5);
            this.Controls.Add(this.label_alikeval);
            this.Controls.Add(this.label_alike);
            this.Controls.Add(this.trackBar4);
            this.Controls.Add(this.label_maxareaval);
            this.Controls.Add(this.label_maxarea);
            this.Controls.Add(this.trackBar3);
            this.Controls.Add(this.label_minareaval);
            this.Controls.Add(this.label_minarea);
            this.Controls.Add(this.trackBar2);
            this.Controls.Add(this.label_thldval);
            this.Controls.Add(this.label_thld);
            this.Controls.Add(this.trackBar1);
            this.Name = "Form2";
            this.Text = "Form2";
            this.Load += new System.EventHandler(this.Form2_Load);
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar5)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Label label_thld;
        private Label label_minareaval;
        private Label label_minarea;
        private Label label_maxareaval;
        private Label label_maxarea;
        private Label label_alikeval;
        private Label label_alike;
        private Label label_sideval;
        private Label label_side;
        public TrackBar trackBar1;
        public TrackBar trackBar2;
        public TrackBar trackBar3;
        public TrackBar trackBar4;
        public TrackBar trackBar5;
        public Label label_thldval;
    }
}