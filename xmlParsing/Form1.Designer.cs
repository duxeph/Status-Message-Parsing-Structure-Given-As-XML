﻿namespace xmlParsing
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
            this.treeViewStatus = new System.Windows.Forms.TreeView();
            this.SuspendLayout();
            // 
            // treeViewStatus
            // 
            this.treeViewStatus.ForeColor = System.Drawing.Color.Black;
            this.treeViewStatus.Location = new System.Drawing.Point(12, 12);
            this.treeViewStatus.Name = "treeViewStatus";
            this.treeViewStatus.Size = new System.Drawing.Size(464, 508);
            this.treeViewStatus.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(488, 532);
            this.Controls.Add(this.treeViewStatus);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView treeViewStatus;
    }
}

