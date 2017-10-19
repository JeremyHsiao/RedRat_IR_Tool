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
            this.button1 = new System.Windows.Forms.Button();
            this.btnSingleRCPressed = new System.Windows.Forms.Button();
            this.listboxAVDeviceList = new System.Windows.Forms.ListBox();
            this.listboxRCKey = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.rtbSignalData = new System.Windows.Forms.RichTextBox();
            this.dgvPulseData = new System.Windows.Forms.DataGridView();
            this.txtFreq = new System.Windows.Forms.TextBox();
            this.chkSelectDoubleSignal = new System.Windows.Forms.CheckBox();
            this.rbDoubleSignalLED = new System.Windows.Forms.RadioButton();
            this.lbFreq = new System.Windows.Forms.Label();
            this.rtbDecodeRCSignal = new System.Windows.Forms.RichTextBox();
            this.btnRepeatOnce = new System.Windows.Forms.Button();
            this.gbRC_File_Data = new System.Windows.Forms.GroupBox();
            this.lbModulationType = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPulseData)).BeginInit();
            this.gbRC_File_Data.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(119, 42);
            this.button1.TabIndex = 0;
            this.button1.Text = "Get RC File";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnSingleRCPressed
            // 
            this.btnSingleRCPressed.Location = new System.Drawing.Point(400, 13);
            this.btnSingleRCPressed.Name = "btnSingleRCPressed";
            this.btnSingleRCPressed.Size = new System.Drawing.Size(102, 41);
            this.btnSingleRCPressed.TabIndex = 1;
            this.btnSingleRCPressed.Text = "Single Press";
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
            this.listboxAVDeviceList.Size = new System.Drawing.Size(276, 172);
            this.listboxAVDeviceList.TabIndex = 2;
            this.listboxAVDeviceList.SelectedIndexChanged += new System.EventHandler(this.listboxAVDeviceList_SelectedIndexChanged);
            // 
            // listboxRCKey
            // 
            this.listboxRCKey.Enabled = false;
            this.listboxRCKey.FormattingEnabled = true;
            this.listboxRCKey.ItemHeight = 12;
            this.listboxRCKey.Location = new System.Drawing.Point(290, 31);
            this.listboxRCKey.Name = "listboxRCKey";
            this.listboxRCKey.Size = new System.Drawing.Size(83, 172);
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
            this.label2.Location = new System.Drawing.Point(288, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 12);
            this.label2.TabIndex = 5;
            this.label2.Text = "RC Signal List";
            // 
            // rtbSignalData
            // 
            this.rtbSignalData.Location = new System.Drawing.Point(12, 524);
            this.rtbSignalData.Name = "rtbSignalData";
            this.rtbSignalData.ReadOnly = true;
            this.rtbSignalData.Size = new System.Drawing.Size(604, 148);
            this.rtbSignalData.TabIndex = 7;
            this.rtbSignalData.Text = "";
            // 
            // dgvPulseData
            // 
            this.dgvPulseData.AllowUserToAddRows = false;
            this.dgvPulseData.AllowUserToDeleteRows = false;
            this.dgvPulseData.AllowUserToOrderColumns = true;
            this.dgvPulseData.AllowUserToResizeRows = false;
            this.dgvPulseData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPulseData.Location = new System.Drawing.Point(8, 235);
            this.dgvPulseData.Name = "dgvPulseData";
            this.dgvPulseData.RowTemplate.Height = 24;
            this.dgvPulseData.Size = new System.Drawing.Size(365, 214);
            this.dgvPulseData.TabIndex = 8;
            this.dgvPulseData.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvPulseData_CellContentClick);
            // 
            // txtFreq
            // 
            this.txtFreq.Location = new System.Drawing.Point(101, 336);
            this.txtFreq.Name = "txtFreq";
            this.txtFreq.Size = new System.Drawing.Size(113, 22);
            this.txtFreq.TabIndex = 9;
            // 
            // chkSelectDoubleSignal
            // 
            this.chkSelectDoubleSignal.AutoSize = true;
            this.chkSelectDoubleSignal.Enabled = false;
            this.chkSelectDoubleSignal.Font = new System.Drawing.Font("微軟正黑體", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.chkSelectDoubleSignal.Location = new System.Drawing.Point(137, 38);
            this.chkSelectDoubleSignal.Name = "chkSelectDoubleSignal";
            this.chkSelectDoubleSignal.Size = new System.Drawing.Size(259, 20);
            this.chkSelectDoubleSignal.TabIndex = 10;
            this.chkSelectDoubleSignal.Text = "Select 2nd of Double Signal / Toogle Bit?";
            this.chkSelectDoubleSignal.UseVisualStyleBackColor = true;
            this.chkSelectDoubleSignal.CheckedChanged += new System.EventHandler(this.chkSelectDoubleSignal_CheckedChanged);
            // 
            // rbDoubleSignalLED
            // 
            this.rbDoubleSignalLED.AutoCheck = false;
            this.rbDoubleSignalLED.AutoSize = true;
            this.rbDoubleSignalLED.Font = new System.Drawing.Font("微軟正黑體", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.rbDoubleSignalLED.Location = new System.Drawing.Point(137, 12);
            this.rbDoubleSignalLED.Name = "rbDoubleSignalLED";
            this.rbDoubleSignalLED.Size = new System.Drawing.Size(236, 20);
            this.rbDoubleSignalLED.TabIndex = 11;
            this.rbDoubleSignalLED.TabStop = true;
            this.rbDoubleSignalLED.Text = "Double Signal / Toogle Bit Indicator";
            this.rbDoubleSignalLED.UseVisualStyleBackColor = true;
            this.rbDoubleSignalLED.CheckedChanged += new System.EventHandler(this.rbDoubleSignalLED_CheckedChanged);
            // 
            // lbFreq
            // 
            this.lbFreq.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbFreq.Font = new System.Drawing.Font("微軟正黑體", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lbFreq.Location = new System.Drawing.Point(290, 206);
            this.lbFreq.Name = "lbFreq";
            this.lbFreq.Size = new System.Drawing.Size(83, 26);
            this.lbFreq.TabIndex = 12;
            this.lbFreq.Text = "0 Hz";
            this.lbFreq.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // rtbDecodeRCSignal
            // 
            this.rtbDecodeRCSignal.Location = new System.Drawing.Point(379, 31);
            this.rtbDecodeRCSignal.Name = "rtbDecodeRCSignal";
            this.rtbDecodeRCSignal.ReadOnly = true;
            this.rtbDecodeRCSignal.Size = new System.Drawing.Size(213, 418);
            this.rtbDecodeRCSignal.TabIndex = 13;
            this.rtbDecodeRCSignal.Text = "";
            // 
            // btnRepeatOnce
            // 
            this.btnRepeatOnce.Location = new System.Drawing.Point(508, 13);
            this.btnRepeatOnce.Name = "btnRepeatOnce";
            this.btnRepeatOnce.Size = new System.Drawing.Size(102, 41);
            this.btnRepeatOnce.TabIndex = 14;
            this.btnRepeatOnce.Text = "Repeated Press (x1)";
            this.btnRepeatOnce.UseVisualStyleBackColor = true;
            this.btnRepeatOnce.Click += new System.EventHandler(this.button3_Click);
            // 
            // gbRC_File_Data
            // 
            this.gbRC_File_Data.Controls.Add(this.label3);
            this.gbRC_File_Data.Controls.Add(this.lbModulationType);
            this.gbRC_File_Data.Controls.Add(this.listboxAVDeviceList);
            this.gbRC_File_Data.Controls.Add(this.listboxRCKey);
            this.gbRC_File_Data.Controls.Add(this.rtbDecodeRCSignal);
            this.gbRC_File_Data.Controls.Add(this.lbFreq);
            this.gbRC_File_Data.Controls.Add(this.dgvPulseData);
            this.gbRC_File_Data.Controls.Add(this.label1);
            this.gbRC_File_Data.Controls.Add(this.label2);
            this.gbRC_File_Data.Controls.Add(this.txtFreq);
            this.gbRC_File_Data.Location = new System.Drawing.Point(12, 60);
            this.gbRC_File_Data.Name = "gbRC_File_Data";
            this.gbRC_File_Data.Size = new System.Drawing.Size(604, 458);
            this.gbRC_File_Data.TabIndex = 15;
            this.gbRC_File_Data.TabStop = false;
            // 
            // lbModulationType
            // 
            this.lbModulationType.Font = new System.Drawing.Font("微軟正黑體", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.lbModulationType.Location = new System.Drawing.Point(8, 206);
            this.lbModulationType.Name = "lbModulationType";
            this.lbModulationType.Size = new System.Drawing.Size(276, 26);
            this.lbModulationType.TabIndex = 14;
            this.lbModulationType.Text = "Modulation Type";
            this.lbModulationType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(377, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(122, 12);
            this.label3.TabIndex = 15;
            this.label3.Text = "RC Signal Parsing Result";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // RedRatDBViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(628, 686);
            this.Controls.Add(this.gbRC_File_Data);
            this.Controls.Add(this.btnRepeatOnce);
            this.Controls.Add(this.rbDoubleSignalLED);
            this.Controls.Add(this.chkSelectDoubleSignal);
            this.Controls.Add(this.rtbSignalData);
            this.Controls.Add(this.btnSingleRCPressed);
            this.Controls.Add(this.button1);
            this.Name = "RedRatDBViewer";
            this.Text = "RedRat Database Viewer";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPulseData)).EndInit();
            this.gbRC_File_Data.ResumeLayout(false);
            this.gbRC_File_Data.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnSingleRCPressed;
        private System.Windows.Forms.ListBox listboxAVDeviceList;
        private System.Windows.Forms.ListBox listboxRCKey;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox rtbSignalData;
        private System.Windows.Forms.DataGridView dgvPulseData;
        private System.Windows.Forms.TextBox txtFreq;
        private System.Windows.Forms.CheckBox chkSelectDoubleSignal;
        private System.Windows.Forms.RadioButton rbDoubleSignalLED;
        private System.Windows.Forms.Label lbFreq;
        private System.Windows.Forms.RichTextBox rtbDecodeRCSignal;
        private System.Windows.Forms.Button btnRepeatOnce;
        private System.Windows.Forms.GroupBox gbRC_File_Data;
        private System.Windows.Forms.Label lbModulationType;
        private System.Windows.Forms.Label label3;
    }
}

