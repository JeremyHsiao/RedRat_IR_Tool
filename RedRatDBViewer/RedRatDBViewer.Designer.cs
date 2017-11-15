namespace RedRatDatabaseViewer
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            this.btnGetRCFile = new System.Windows.Forms.Button();
            this.btnSingleRCPressed = new System.Windows.Forms.Button();
            this.listboxAVDeviceList = new System.Windows.Forms.ListBox();
            this.listboxRCKey = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.rtbSignalData = new System.Windows.Forms.RichTextBox();
            this.dgvPulseData = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.chkSelect2ndSignal = new System.Windows.Forms.CheckBox();
            this.rbDoubleSignalLED = new System.Windows.Forms.RadioButton();
            this.lbFreq = new System.Windows.Forms.Label();
            this.rtbDecodeRCSignal = new System.Windows.Forms.RichTextBox();
            this.btnStopRCButton = new System.Windows.Forms.Button();
            this.gbRC_File_Data = new System.Windows.Forms.GroupBox();
            this.dgvToggleBits = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label3 = new System.Windows.Forms.Label();
            this.lbModulationType = new System.Windows.Forms.Label();
            this.btnFreshCOMNo = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.btnConnectionControl = new System.Windows.Forms.Button();
            this.btnCheckHeartBeat = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPulseData)).BeginInit();
            this.gbRC_File_Data.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvToggleBits)).BeginInit();
            this.SuspendLayout();
            // 
            // btnGetRCFile
            // 
            this.btnGetRCFile.Location = new System.Drawing.Point(6, 12);
            this.btnGetRCFile.Name = "btnGetRCFile";
            this.btnGetRCFile.Size = new System.Drawing.Size(50, 46);
            this.btnGetRCFile.TabIndex = 0;
            this.btnGetRCFile.Text = "Get RC File";
            this.btnGetRCFile.UseVisualStyleBackColor = true;
            this.btnGetRCFile.Click += new System.EventHandler(this.btnGetRCFile_Click);
            // 
            // btnSingleRCPressed
            // 
            this.btnSingleRCPressed.Enabled = false;
            this.btnSingleRCPressed.Location = new System.Drawing.Point(443, 12);
            this.btnSingleRCPressed.Name = "btnSingleRCPressed";
            this.btnSingleRCPressed.Size = new System.Drawing.Size(82, 46);
            this.btnSingleRCPressed.TabIndex = 1;
            this.btnSingleRCPressed.Text = "Single RC";
            this.btnSingleRCPressed.UseVisualStyleBackColor = true;
            this.btnSingleRCPressed.Click += new System.EventHandler(this.SingleOutput_Click);
            // 
            // listboxAVDeviceList
            // 
            this.listboxAVDeviceList.Enabled = false;
            this.listboxAVDeviceList.FormattingEnabled = true;
            this.listboxAVDeviceList.ItemHeight = 12;
            this.listboxAVDeviceList.Location = new System.Drawing.Point(8, 31);
            this.listboxAVDeviceList.Name = "listboxAVDeviceList";
            this.listboxAVDeviceList.Size = new System.Drawing.Size(285, 172);
            this.listboxAVDeviceList.TabIndex = 2;
            this.listboxAVDeviceList.SelectedIndexChanged += new System.EventHandler(this.listboxAVDeviceList_SelectedIndexChanged);
            // 
            // listboxRCKey
            // 
            this.listboxRCKey.Enabled = false;
            this.listboxRCKey.FormattingEnabled = true;
            this.listboxRCKey.ItemHeight = 12;
            this.listboxRCKey.Location = new System.Drawing.Point(299, 31);
            this.listboxRCKey.Name = "listboxRCKey";
            this.listboxRCKey.Size = new System.Drawing.Size(132, 172);
            this.listboxRCKey.TabIndex = 3;
            this.listboxRCKey.SelectedIndexChanged += new System.EventHandler(this.listboxRCKey_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 12);
            this.label1.TabIndex = 4;
            this.label1.Text = "RC Device List";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(297, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 12);
            this.label2.TabIndex = 5;
            this.label2.Text = "RC Signal List";
            // 
            // rtbSignalData
            // 
            this.rtbSignalData.Location = new System.Drawing.Point(6, 524);
            this.rtbSignalData.Name = "rtbSignalData";
            this.rtbSignalData.ReadOnly = true;
            this.rtbSignalData.Size = new System.Drawing.Size(610, 148);
            this.rtbSignalData.TabIndex = 7;
            this.rtbSignalData.Text = "";
            // 
            // dgvPulseData
            // 
            this.dgvPulseData.AllowUserToAddRows = false;
            this.dgvPulseData.AllowUserToDeleteRows = false;
            this.dgvPulseData.AllowUserToResizeColumns = false;
            this.dgvPulseData.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("新細明體", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvPulseData.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvPulseData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPulseData.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1});
            this.dgvPulseData.Location = new System.Drawing.Point(8, 235);
            this.dgvPulseData.Name = "dgvPulseData";
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("新細明體", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvPulseData.RowHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvPulseData.RowHeadersWidth = 50;
            this.dgvPulseData.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dgvPulseData.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dgvPulseData.RowTemplate.Height = 24;
            this.dgvPulseData.Size = new System.Drawing.Size(153, 214);
            this.dgvPulseData.TabIndex = 8;
            // 
            // Column1
            // 
            this.Column1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.Column1.FillWeight = 120F;
            this.Column1.Frozen = true;
            this.Column1.HeaderText = "Width (ms)";
            this.Column1.Name = "Column1";
            this.Column1.ReadOnly = true;
            this.Column1.Width = 101;
            // 
            // chkSelect2ndSignal
            // 
            this.chkSelect2ndSignal.AutoSize = true;
            this.chkSelect2ndSignal.Enabled = false;
            this.chkSelect2ndSignal.Font = new System.Drawing.Font("微軟正黑體", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.chkSelect2ndSignal.Location = new System.Drawing.Point(62, 38);
            this.chkSelect2ndSignal.Name = "chkSelect2ndSignal";
            this.chkSelect2ndSignal.Size = new System.Drawing.Size(137, 20);
            this.chkSelect2ndSignal.TabIndex = 10;
            this.chkSelect2ndSignal.Text = "Display 2nd Signal?";
            this.chkSelect2ndSignal.UseVisualStyleBackColor = true;
            this.chkSelect2ndSignal.CheckedChanged += new System.EventHandler(this.chkSelect2ndSignal_CheckedChanged);
            // 
            // rbDoubleSignalLED
            // 
            this.rbDoubleSignalLED.AutoCheck = false;
            this.rbDoubleSignalLED.AutoSize = true;
            this.rbDoubleSignalLED.Font = new System.Drawing.Font("微軟正黑體", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.rbDoubleSignalLED.Location = new System.Drawing.Point(62, 12);
            this.rbDoubleSignalLED.Name = "rbDoubleSignalLED";
            this.rbDoubleSignalLED.Size = new System.Drawing.Size(165, 20);
            this.rbDoubleSignalLED.TabIndex = 11;
            this.rbDoubleSignalLED.TabStop = true;
            this.rbDoubleSignalLED.Text = "Double Signal Indicator";
            this.rbDoubleSignalLED.UseVisualStyleBackColor = true;
            this.rbDoubleSignalLED.CheckedChanged += new System.EventHandler(this.rbDoubleSignalLED_CheckedChanged);
            // 
            // lbFreq
            // 
            this.lbFreq.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbFreq.Font = new System.Drawing.Font("微軟正黑體", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lbFreq.Location = new System.Drawing.Point(299, 206);
            this.lbFreq.Name = "lbFreq";
            this.lbFreq.Size = new System.Drawing.Size(132, 26);
            this.lbFreq.TabIndex = 12;
            this.lbFreq.Text = "0 Hz";
            this.lbFreq.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // rtbDecodeRCSignal
            // 
            this.rtbDecodeRCSignal.Location = new System.Drawing.Point(437, 31);
            this.rtbDecodeRCSignal.Name = "rtbDecodeRCSignal";
            this.rtbDecodeRCSignal.ReadOnly = true;
            this.rtbDecodeRCSignal.Size = new System.Drawing.Size(165, 418);
            this.rtbDecodeRCSignal.TabIndex = 13;
            this.rtbDecodeRCSignal.Text = "";
            // 
            // btnStopRCButton
            // 
            this.btnStopRCButton.Location = new System.Drawing.Point(582, 12);
            this.btnStopRCButton.Name = "btnStopRCButton";
            this.btnStopRCButton.Size = new System.Drawing.Size(34, 46);
            this.btnStopRCButton.TabIndex = 14;
            this.btnStopRCButton.Text = "Stop RC";
            this.btnStopRCButton.UseVisualStyleBackColor = true;
            this.btnStopRCButton.Click += new System.EventHandler(this.StopCMDButton_Click);
            // 
            // gbRC_File_Data
            // 
            this.gbRC_File_Data.Controls.Add(this.dgvToggleBits);
            this.gbRC_File_Data.Controls.Add(this.label3);
            this.gbRC_File_Data.Controls.Add(this.lbModulationType);
            this.gbRC_File_Data.Controls.Add(this.listboxAVDeviceList);
            this.gbRC_File_Data.Controls.Add(this.listboxRCKey);
            this.gbRC_File_Data.Controls.Add(this.rtbDecodeRCSignal);
            this.gbRC_File_Data.Controls.Add(this.lbFreq);
            this.gbRC_File_Data.Controls.Add(this.dgvPulseData);
            this.gbRC_File_Data.Controls.Add(this.label1);
            this.gbRC_File_Data.Controls.Add(this.label2);
            this.gbRC_File_Data.Location = new System.Drawing.Point(6, 59);
            this.gbRC_File_Data.Name = "gbRC_File_Data";
            this.gbRC_File_Data.Size = new System.Drawing.Size(610, 459);
            this.gbRC_File_Data.TabIndex = 15;
            this.gbRC_File_Data.TabStop = false;
            // 
            // dgvToggleBits
            // 
            this.dgvToggleBits.AllowUserToAddRows = false;
            this.dgvToggleBits.AllowUserToDeleteRows = false;
            this.dgvToggleBits.AllowUserToResizeColumns = false;
            this.dgvToggleBits.AllowUserToResizeRows = false;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("新細明體", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvToggleBits.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvToggleBits.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvToggleBits.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.Column2});
            this.dgvToggleBits.Location = new System.Drawing.Point(167, 235);
            this.dgvToggleBits.Name = "dgvToggleBits";
            this.dgvToggleBits.ReadOnly = true;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("新細明體", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvToggleBits.RowHeadersDefaultCellStyle = dataGridViewCellStyle4;
            this.dgvToggleBits.RowHeadersWidth = 65;
            this.dgvToggleBits.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dgvToggleBits.RowsDefaultCellStyle = dataGridViewCellStyle5;
            this.dgvToggleBits.RowTemplate.Height = 24;
            this.dgvToggleBits.Size = new System.Drawing.Size(264, 214);
            this.dgvToggleBits.TabIndex = 16;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.Frozen = true;
            this.dataGridViewTextBoxColumn1.HeaderText = "Width1 (ms)";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // Column2
            // 
            this.Column2.Frozen = true;
            this.Column2.HeaderText = "Width2 (ms)";
            this.Column2.Name = "Column2";
            this.Column2.ReadOnly = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(435, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(122, 12);
            this.label3.TabIndex = 15;
            this.label3.Text = "RC Signal Parsing Result";
            // 
            // lbModulationType
            // 
            this.lbModulationType.Font = new System.Drawing.Font("微軟正黑體", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lbModulationType.Location = new System.Drawing.Point(8, 206);
            this.lbModulationType.Name = "lbModulationType";
            this.lbModulationType.Size = new System.Drawing.Size(285, 26);
            this.lbModulationType.TabIndex = 14;
            this.lbModulationType.Text = "RedRat RC Modulation Type";
            this.lbModulationType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnFreshCOMNo
            // 
            this.btnFreshCOMNo.Location = new System.Drawing.Point(250, 12);
            this.btnFreshCOMNo.Name = "btnFreshCOMNo";
            this.btnFreshCOMNo.Size = new System.Drawing.Size(49, 46);
            this.btnFreshCOMNo.TabIndex = 16;
            this.btnFreshCOMNo.Text = "Refresh COM";
            this.btnFreshCOMNo.UseVisualStyleBackColor = true;
            this.btnFreshCOMNo.Click += new System.EventHandler(this.btnFreshCOMNo_Click);
            // 
            // listBox1
            // 
            this.listBox1.Font = new System.Drawing.Font("Calibri", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 14;
            this.listBox1.Location = new System.Drawing.Point(305, 12);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(58, 46);
            this.listBox1.TabIndex = 17;
            // 
            // btnConnectionControl
            // 
            this.btnConnectionControl.Enabled = false;
            this.btnConnectionControl.Location = new System.Drawing.Point(369, 12);
            this.btnConnectionControl.Name = "btnConnectionControl";
            this.btnConnectionControl.Size = new System.Drawing.Size(68, 46);
            this.btnConnectionControl.TabIndex = 18;
            this.btnConnectionControl.Text = "Connect UART";
            this.btnConnectionControl.UseVisualStyleBackColor = true;
            this.btnConnectionControl.Click += new System.EventHandler(this.btnConnectionControl_Click);
            // 
            // btnCheckHeartBeat
            // 
            this.btnCheckHeartBeat.Location = new System.Drawing.Point(531, 12);
            this.btnCheckHeartBeat.Name = "btnCheckHeartBeat";
            this.btnCheckHeartBeat.Size = new System.Drawing.Size(45, 46);
            this.btnCheckHeartBeat.TabIndex = 17;
            this.btnCheckHeartBeat.Text = "Heart Beat";
            this.btnCheckHeartBeat.UseVisualStyleBackColor = true;
            this.btnCheckHeartBeat.Click += new System.EventHandler(this.btnCheckHeartBeat_Click);
            // 
            // RedRatDBViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(620, 678);
            this.Controls.Add(this.btnCheckHeartBeat);
            this.Controls.Add(this.btnConnectionControl);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.btnFreshCOMNo);
            this.Controls.Add(this.gbRC_File_Data);
            this.Controls.Add(this.btnStopRCButton);
            this.Controls.Add(this.rbDoubleSignalLED);
            this.Controls.Add(this.chkSelect2ndSignal);
            this.Controls.Add(this.rtbSignalData);
            this.Controls.Add(this.btnSingleRCPressed);
            this.Controls.Add(this.btnGetRCFile);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.Name = "RedRatDBViewer";
            this.Text = "RedRat Database Viewer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.RedRatDBViewer_Closing);
            this.Load += new System.EventHandler(this.RedRatDBViewer_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPulseData)).EndInit();
            this.gbRC_File_Data.ResumeLayout(false);
            this.gbRC_File_Data.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvToggleBits)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnGetRCFile;
        private System.Windows.Forms.Button btnSingleRCPressed;
        private System.Windows.Forms.ListBox listboxAVDeviceList;
        private System.Windows.Forms.ListBox listboxRCKey;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox rtbSignalData;
        private System.Windows.Forms.DataGridView dgvPulseData;
        private System.Windows.Forms.CheckBox chkSelect2ndSignal;
        private System.Windows.Forms.RadioButton rbDoubleSignalLED;
        private System.Windows.Forms.Label lbFreq;
        private System.Windows.Forms.RichTextBox rtbDecodeRCSignal;
        private System.Windows.Forms.Button btnStopRCButton;
        private System.Windows.Forms.GroupBox gbRC_File_Data;
        private System.Windows.Forms.Label lbModulationType;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridView dgvToggleBits;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.Button btnFreshCOMNo;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button btnConnectionControl;
        private System.Windows.Forms.Button btnCheckHeartBeat;
    }
}

