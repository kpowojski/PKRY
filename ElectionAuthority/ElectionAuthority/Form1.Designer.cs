﻿namespace ElectionAuthority
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.startElectionAuthorityButton = new System.Windows.Forms.Button();
            this.logsListView = new System.Windows.Forms.ListView();
            this.logColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.configButton = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // startElectionAuthorityButton
            // 
            this.startElectionAuthorityButton.Enabled = false;
            this.startElectionAuthorityButton.Location = new System.Drawing.Point(12, 211);
            this.startElectionAuthorityButton.Name = "startElectionAuthorityButton";
            this.startElectionAuthorityButton.Size = new System.Drawing.Size(123, 35);
            this.startElectionAuthorityButton.TabIndex = 0;
            this.startElectionAuthorityButton.Text = "Start ElectionAuthority";
            this.startElectionAuthorityButton.UseVisualStyleBackColor = true;
            this.startElectionAuthorityButton.Click += new System.EventHandler(this.startElectionAuthorityButton_Click);
            // 
            // logsListView
            // 
            this.logsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.logColumn});
            this.logsListView.FullRowSelect = true;
            this.logsListView.Location = new System.Drawing.Point(13, 12);
            this.logsListView.Name = "logsListView";
            this.logsListView.Size = new System.Drawing.Size(740, 143);
            this.logsListView.TabIndex = 1;
            this.logsListView.UseCompatibleStateImageBehavior = false;
            this.logsListView.View = System.Windows.Forms.View.Details;
            // 
            // logColumn
            // 
            this.logColumn.Text = "Logs";
            this.logColumn.Width = 734;
            // 
            // configButton
            // 
            this.configButton.Location = new System.Drawing.Point(13, 170);
            this.configButton.Name = "configButton";
            this.configButton.Size = new System.Drawing.Size(122, 35);
            this.configButton.TabIndex = 2;
            this.configButton.Text = "Load Configuration";
            this.configButton.UseVisualStyleBackColor = true;
            this.configButton.Click += new System.EventHandler(this.configButton_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog";
            this.openFileDialog.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(765, 409);
            this.Controls.Add(this.configButton);
            this.Controls.Add(this.logsListView);
            this.Controls.Add(this.startElectionAuthorityButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Election Authority";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button startElectionAuthorityButton;
        private System.Windows.Forms.ListView logsListView;
        private System.Windows.Forms.Button configButton;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.ColumnHeader logColumn;
    }
}

