﻿namespace RedRatDatabaseViewer
{
    partial class RedRatDBViewer
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置 Managed 資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.listboxAVDeviceList = new System.Windows.Forms.ListBox();
            this.listboxRCKey = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.rtbSignalData = new System.Windows.Forms.RichTextBox();
            this.dgvPulseData = new System.Windows.Forms.DataGridView();
            this.txtFreq = new System.Windows.Forms.TextBox();
            this.chkSelectDoubleSignal = new System.Windows.Forms.CheckBox();
            this.rbDoubleSignalLED = new System.Windows.Forms.RadioButton();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPulseData)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(361, 17);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(95, 42);
            this.button1.TabIndex = 0;
            this.button1.Text = "Get RC File";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(462, 18);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(114, 41);
            this.button2.TabIndex = 1;
            this.button2.Text = "Analyze File";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // listboxAVDeviceList
            // 
            this.listboxAVDeviceList.Enabled = false;
            this.listboxAVDeviceList.FormattingEnabled = true;
            this.listboxAVDeviceList.ItemHeight = 12;
            this.listboxAVDeviceList.Location = new System.Drawing.Point(12, 85);
            this.listboxAVDeviceList.Name = "listboxAVDeviceList";
            this.listboxAVDeviceList.Size = new System.Drawing.Size(141, 184);
            this.listboxAVDeviceList.TabIndex = 2;
            this.listboxAVDeviceList.SelectedIndexChanged += new System.EventHandler(this.listboxAVDeviceList_SelectedIndexChanged);
            // 
            // listboxRCKey
            // 
            this.listboxRCKey.Enabled = false;
            this.listboxRCKey.FormattingEnabled = true;
            this.listboxRCKey.ItemHeight = 12;
            this.listboxRCKey.Location = new System.Drawing.Point(168, 85);
            this.listboxRCKey.Name = "listboxRCKey";
            this.listboxRCKey.Size = new System.Drawing.Size(161, 184);
            this.listboxRCKey.TabIndex = 3;
            this.listboxRCKey.SelectedIndexChanged += new System.EventHandler(this.listboxRCKey_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 70);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 12);
            this.label1.TabIndex = 4;
            this.label1.Text = "RC Device List";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(166, 70);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 12);
            this.label2.TabIndex = 5;
            this.label2.Text = "RC Signal List";
            // 
            // rtbSignalData
            // 
            this.rtbSignalData.Location = new System.Drawing.Point(12, 503);
            this.rtbSignalData.Name = "rtbSignalData";
            this.rtbSignalData.ReadOnly = true;
            this.rtbSignalData.Size = new System.Drawing.Size(564, 148);
            this.rtbSignalData.TabIndex = 7;
            this.rtbSignalData.Text = "";
            this.rtbSignalData.TextChanged += new System.EventHandler(this.rtbSignalData_TextChanged);
            // 
            // dgvPulseData
            // 
            this.dgvPulseData.AllowUserToAddRows = false;
            this.dgvPulseData.AllowUserToDeleteRows = false;
            this.dgvPulseData.AllowUserToOrderColumns = true;
            this.dgvPulseData.AllowUserToResizeRows = false;
            this.dgvPulseData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPulseData.Location = new System.Drawing.Point(12, 297);
            this.dgvPulseData.Name = "dgvPulseData";
            this.dgvPulseData.RowTemplate.Height = 24;
            this.dgvPulseData.Size = new System.Drawing.Size(317, 200);
            this.dgvPulseData.TabIndex = 8;
            // 
            // txtFreq
            // 
            this.txtFreq.Location = new System.Drawing.Point(361, 114);
            this.txtFreq.Name = "txtFreq";
            this.txtFreq.Size = new System.Drawing.Size(214, 22);
            this.txtFreq.TabIndex = 9;
            // 
            // chkSelectDoubleSignal
            // 
            this.chkSelectDoubleSignal.AutoSize = true;
            this.chkSelectDoubleSignal.Enabled = false;
            this.chkSelectDoubleSignal.Location = new System.Drawing.Point(12, 275);
            this.chkSelectDoubleSignal.Name = "chkSelectDoubleSignal";
            this.chkSelectDoubleSignal.Size = new System.Drawing.Size(191, 16);
            this.chkSelectDoubleSignal.TabIndex = 10;
            this.chkSelectDoubleSignal.Text = "Select 2nd Signal of Double Signal?";
            this.chkSelectDoubleSignal.UseVisualStyleBackColor = true;
            this.chkSelectDoubleSignal.CheckedChanged += new System.EventHandler(this.chkSelectDoubleSignal_CheckedChanged);
            // 
            // rbDoubleSignalLED
            // 
            this.rbDoubleSignalLED.AutoCheck = false;
            this.rbDoubleSignalLED.AutoSize = true;
            this.rbDoubleSignalLED.Location = new System.Drawing.Point(364, 90);
            this.rbDoubleSignalLED.Name = "rbDoubleSignalLED";
            this.rbDoubleSignalLED.Size = new System.Drawing.Size(134, 16);
            this.rbDoubleSignalLED.TabIndex = 11;
            this.rbDoubleSignalLED.TabStop = true;
            this.rbDoubleSignalLED.Text = "Double Signal Indicator";
            this.rbDoubleSignalLED.UseVisualStyleBackColor = true;
            // 
            // RedRatDBViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(587, 663);
            this.Controls.Add(this.rbDoubleSignalLED);
            this.Controls.Add(this.chkSelectDoubleSignal);
            this.Controls.Add(this.txtFreq);
            this.Controls.Add(this.dgvPulseData);
            this.Controls.Add(this.rtbSignalData);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listboxRCKey);
            this.Controls.Add(this.listboxAVDeviceList);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Name = "RedRatDBViewer";
            this.Text = "RedRat Database Viewer";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPulseData)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ListBox listboxAVDeviceList;
        private System.Windows.Forms.ListBox listboxRCKey;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox rtbSignalData;
        private System.Windows.Forms.DataGridView dgvPulseData;
        private System.Windows.Forms.TextBox txtFreq;
        private System.Windows.Forms.CheckBox chkSelectDoubleSignal;
        private System.Windows.Forms.RadioButton rbDoubleSignalLED;
    }
}

