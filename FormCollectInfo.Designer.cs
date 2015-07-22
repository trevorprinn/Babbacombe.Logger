#region Licence
/*
The MIT License (MIT)

Copyright (c) 2015 Babbacombe Computers Ltd

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion
namespace Babbacombe.Logger {
    partial class FormCollectInfo {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label label2;
            System.Windows.Forms.Label label3;
            System.Windows.Forms.Label label4;
            this.labelExit = new System.Windows.Forms.Label();
            this.labelSend = new System.Windows.Forms.Label();
            this.textName = new System.Windows.Forms.TextBox();
            this.textEmail = new System.Windows.Forms.TextBox();
            this.checkScreenshot = new System.Windows.Forms.CheckBox();
            this.checkExit = new System.Windows.Forms.CheckBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.textNotes = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.Location = new System.Drawing.Point(9, 9);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(251, 18);
            label1.TabIndex = 0;
            label1.Text = "An unexpected error has occurred.";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(12, 59);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(38, 13);
            label2.TabIndex = 3;
            label2.Text = "Name:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(12, 89);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(35, 13);
            label3.TabIndex = 5;
            label3.Text = "Email:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(9, 140);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(38, 13);
            label4.TabIndex = 11;
            label4.Text = "Notes:";
            // 
            // labelExit
            // 
            this.labelExit.AutoSize = true;
            this.labelExit.Location = new System.Drawing.Point(178, 9);
            this.labelExit.Name = "labelExit";
            this.labelExit.Size = new System.Drawing.Size(106, 13);
            this.labelExit.TabIndex = 1;
            this.labelExit.Text = "The program will exit.";
            // 
            // labelSend
            // 
            this.labelSend.AutoSize = true;
            this.labelSend.Location = new System.Drawing.Point(9, 27);
            this.labelSend.Name = "labelSend";
            this.labelSend.Size = new System.Drawing.Size(157, 13);
            this.labelSend.TabIndex = 2;
            this.labelSend.Text = "You can send an error report to ";
            // 
            // textName
            // 
            this.textName.Location = new System.Drawing.Point(56, 56);
            this.textName.Name = "textName";
            this.textName.Size = new System.Drawing.Size(272, 20);
            this.textName.TabIndex = 4;
            // 
            // textEmail
            // 
            this.textEmail.Location = new System.Drawing.Point(56, 86);
            this.textEmail.Name = "textEmail";
            this.textEmail.Size = new System.Drawing.Size(272, 20);
            this.textEmail.TabIndex = 6;
            // 
            // checkScreenshot
            // 
            this.checkScreenshot.AutoSize = true;
            this.checkScreenshot.Location = new System.Drawing.Point(15, 112);
            this.checkScreenshot.Name = "checkScreenshot";
            this.checkScreenshot.Size = new System.Drawing.Size(115, 17);
            this.checkScreenshot.TabIndex = 7;
            this.checkScreenshot.Text = "Send a screenshot";
            this.checkScreenshot.UseVisualStyleBackColor = true;
            // 
            // checkExit
            // 
            this.checkExit.AutoSize = true;
            this.checkExit.Location = new System.Drawing.Point(163, 112);
            this.checkExit.Name = "checkExit";
            this.checkExit.Size = new System.Drawing.Size(102, 17);
            this.checkExit.TabIndex = 8;
            this.checkExit.Text = "Exit the program";
            this.checkExit.UseVisualStyleBackColor = true;
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(65, 215);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(75, 23);
            this.btnSend.TabIndex = 9;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(209, 215);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 10;
            this.btnCancel.Text = "Close";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // textNotes
            // 
            this.textNotes.Location = new System.Drawing.Point(56, 140);
            this.textNotes.Multiline = true;
            this.textNotes.Name = "textNotes";
            this.textNotes.Size = new System.Drawing.Size(272, 69);
            this.textNotes.TabIndex = 12;
            // 
            // FormCollectInfo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(340, 250);
            this.ControlBox = false;
            this.Controls.Add(this.textNotes);
            this.Controls.Add(label4);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.checkExit);
            this.Controls.Add(this.checkScreenshot);
            this.Controls.Add(this.textEmail);
            this.Controls.Add(label3);
            this.Controls.Add(this.textName);
            this.Controls.Add(label2);
            this.Controls.Add(this.labelSend);
            this.Controls.Add(this.labelExit);
            this.Controls.Add(label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormCollectInfo";
            this.Text = "Unexpected Error";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        protected System.Windows.Forms.Label labelExit;
        protected System.Windows.Forms.Label labelSend;
        protected System.Windows.Forms.TextBox textName;
        protected System.Windows.Forms.TextBox textEmail;
        protected System.Windows.Forms.CheckBox checkScreenshot;
        protected System.Windows.Forms.CheckBox checkExit;
        protected System.Windows.Forms.Button btnSend;
        protected System.Windows.Forms.Button btnCancel;
        protected System.Windows.Forms.TextBox textNotes;

    }
}