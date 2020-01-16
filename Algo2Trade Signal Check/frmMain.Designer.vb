<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmMain
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmMain))
        Me.saveFile = New System.Windows.Forms.SaveFileDialog()
        Me.btnExport = New System.Windows.Forms.Button()
        Me.chkbHA = New System.Windows.Forms.CheckBox()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.nmrcTimeFrame = New System.Windows.Forms.NumericUpDown()
        Me.lblTimeFrame = New System.Windows.Forms.Label()
        Me.dtpckrToDate = New System.Windows.Forms.DateTimePicker()
        Me.dtpckrFromDate = New System.Windows.Forms.DateTimePicker()
        Me.lblToDate = New System.Windows.Forms.Label()
        Me.lblFromDate = New System.Windows.Forms.Label()
        Me.btnView = New System.Windows.Forms.Button()
        Me.txtInstrumentName = New System.Windows.Forms.TextBox()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.cmbCategory = New System.Windows.Forms.ComboBox()
        Me.lblCategory = New System.Windows.Forms.Label()
        Me.cmbRule = New System.Windows.Forms.ComboBox()
        Me.lblRule = New System.Windows.Forms.Label()
        Me.lblProgress = New System.Windows.Forms.Label()
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.btnBrowse = New System.Windows.Forms.Button()
        Me.txtFilePath = New System.Windows.Forms.TextBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.dgvSignal = New System.Windows.Forms.DataGridView()
        Me.opnFile = New System.Windows.Forms.OpenFileDialog()
        Me.lblDescription = New System.Windows.Forms.Label()
        CType(Me.nmrcTimeFrame, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.Panel1.SuspendLayout()
        CType(Me.dgvSignal, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'saveFile
        '
        '
        'btnExport
        '
        Me.btnExport.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnExport.Location = New System.Drawing.Point(1140, 658)
        Me.btnExport.Margin = New System.Windows.Forms.Padding(4)
        Me.btnExport.Name = "btnExport"
        Me.btnExport.Size = New System.Drawing.Size(100, 28)
        Me.btnExport.TabIndex = 24
        Me.btnExport.Text = "Export"
        Me.btnExport.UseVisualStyleBackColor = True
        '
        'chkbHA
        '
        Me.chkbHA.AutoSize = True
        Me.chkbHA.Location = New System.Drawing.Point(962, 15)
        Me.chkbHA.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.chkbHA.Name = "chkbHA"
        Me.chkbHA.Size = New System.Drawing.Size(149, 21)
        Me.chkbHA.TabIndex = 30
        Me.chkbHA.Text = "HeikenAshi Candle"
        Me.chkbHA.UseVisualStyleBackColor = True
        '
        'btnCancel
        '
        Me.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Right
        Me.btnCancel.Location = New System.Drawing.Point(1125, 44)
        Me.btnCancel.Margin = New System.Windows.Forms.Padding(4)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(100, 28)
        Me.btnCancel.TabIndex = 29
        Me.btnCancel.Text = "Stop"
        Me.btnCancel.UseVisualStyleBackColor = True
        '
        'nmrcTimeFrame
        '
        Me.nmrcTimeFrame.Location = New System.Drawing.Point(883, 12)
        Me.nmrcTimeFrame.Margin = New System.Windows.Forms.Padding(4)
        Me.nmrcTimeFrame.Name = "nmrcTimeFrame"
        Me.nmrcTimeFrame.Size = New System.Drawing.Size(58, 22)
        Me.nmrcTimeFrame.TabIndex = 28
        '
        'lblTimeFrame
        '
        Me.lblTimeFrame.AutoSize = True
        Me.lblTimeFrame.Location = New System.Drawing.Point(760, 13)
        Me.lblTimeFrame.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblTimeFrame.Name = "lblTimeFrame"
        Me.lblTimeFrame.Size = New System.Drawing.Size(126, 17)
        Me.lblTimeFrame.TabIndex = 27
        Me.lblTimeFrame.Text = "Signal TimeFrame:"
        '
        'dtpckrToDate
        '
        Me.dtpckrToDate.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpckrToDate.Location = New System.Drawing.Point(277, 50)
        Me.dtpckrToDate.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.dtpckrToDate.Name = "dtpckrToDate"
        Me.dtpckrToDate.Size = New System.Drawing.Size(108, 22)
        Me.dtpckrToDate.TabIndex = 26
        '
        'dtpckrFromDate
        '
        Me.dtpckrFromDate.Format = System.Windows.Forms.DateTimePickerFormat.[Short]
        Me.dtpckrFromDate.Location = New System.Drawing.Point(93, 48)
        Me.dtpckrFromDate.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.dtpckrFromDate.Name = "dtpckrFromDate"
        Me.dtpckrFromDate.Size = New System.Drawing.Size(108, 22)
        Me.dtpckrFromDate.TabIndex = 25
        '
        'lblToDate
        '
        Me.lblToDate.AutoSize = True
        Me.lblToDate.Location = New System.Drawing.Point(211, 51)
        Me.lblToDate.Name = "lblToDate"
        Me.lblToDate.Size = New System.Drawing.Size(63, 17)
        Me.lblToDate.TabIndex = 24
        Me.lblToDate.Text = "To Date:"
        '
        'lblFromDate
        '
        Me.lblFromDate.AutoSize = True
        Me.lblFromDate.Location = New System.Drawing.Point(11, 50)
        Me.lblFromDate.Name = "lblFromDate"
        Me.lblFromDate.Size = New System.Drawing.Size(78, 17)
        Me.lblFromDate.TabIndex = 23
        Me.lblFromDate.Text = "From Date:"
        '
        'btnView
        '
        Me.btnView.Anchor = System.Windows.Forms.AnchorStyles.Right
        Me.btnView.Location = New System.Drawing.Point(1125, 7)
        Me.btnView.Margin = New System.Windows.Forms.Padding(4)
        Me.btnView.Name = "btnView"
        Me.btnView.Size = New System.Drawing.Size(100, 28)
        Me.btnView.TabIndex = 22
        Me.btnView.Text = "View"
        Me.btnView.UseVisualStyleBackColor = True
        '
        'txtInstrumentName
        '
        Me.txtInstrumentName.Location = New System.Drawing.Point(518, 52)
        Me.txtInstrumentName.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.txtInstrumentName.Name = "txtInstrumentName"
        Me.txtInstrumentName.Size = New System.Drawing.Size(205, 22)
        Me.txtInstrumentName.TabIndex = 21
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(398, 53)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(119, 17)
        Me.Label4.TabIndex = 20
        Me.Label4.Text = "Instrument Name:"
        '
        'cmbCategory
        '
        Me.cmbCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbCategory.FormattingEnabled = True
        Me.cmbCategory.Items.AddRange(New Object() {"Intraday Cash", "Intraday Commodity", "Intraday Currency", "Intraday Futures", "EOD Cash", "EOD Commodity", "EOD Currency", "EOD Futures", "EOD Postional"})
        Me.cmbCategory.Location = New System.Drawing.Point(607, 9)
        Me.cmbCategory.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.cmbCategory.Name = "cmbCategory"
        Me.cmbCategory.Size = New System.Drawing.Size(146, 24)
        Me.cmbCategory.TabIndex = 19
        '
        'lblCategory
        '
        Me.lblCategory.AutoSize = True
        Me.lblCategory.Location = New System.Drawing.Point(466, 11)
        Me.lblCategory.Name = "lblCategory"
        Me.lblCategory.Size = New System.Drawing.Size(139, 17)
        Me.lblCategory.TabIndex = 18
        Me.lblCategory.Text = "Instrument Category:"
        '
        'cmbRule
        '
        Me.cmbRule.FormattingEnabled = True
        Me.cmbRule.Items.AddRange(New Object() {"Stall Pattern", "Piercing And Dark Cloud", "One Sided Volume", "Constriction At Breakout", "HK Trend Opposing By Volume", "HK Temporary Pause", "HK Reversal", "Get Raw Candle", "Daily Strong HK Opposite Color Volume", "Fractal Cut 2 MA", "Volume Index", "EOD Signal", "Pin Bar Formation", "Bollinger With ATR Bands", "Low Loss High Gain VWAP", "Double Volume EOD", "Fractal Breakout Short Trend", "Donchian Breakout Short Trend", "Pinocchio Bar Formation", "Market Open HA Breakout Screener", "Volume With Candle Range", "DayHighLow", "Low SL Candle", "Inside Bar High Low", "Reversal HHLL Breakout", "Double Inside Bar", "High Low Support Resistance", "Open=High/Open=Low", "Spot Future Arbritrage", "Swing Candle"})
        Me.cmbRule.Location = New System.Drawing.Point(108, 7)
        Me.cmbRule.Margin = New System.Windows.Forms.Padding(4)
        Me.cmbRule.Name = "cmbRule"
        Me.cmbRule.Size = New System.Drawing.Size(354, 24)
        Me.cmbRule.TabIndex = 17
        '
        'lblRule
        '
        Me.lblRule.AutoSize = True
        Me.lblRule.Location = New System.Drawing.Point(11, 12)
        Me.lblRule.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblRule.Name = "lblRule"
        Me.lblRule.Size = New System.Drawing.Size(93, 17)
        Me.lblRule.TabIndex = 16
        Me.lblRule.Text = "Choose Rule:"
        '
        'lblProgress
        '
        Me.lblProgress.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lblProgress.Location = New System.Drawing.Point(4, 657)
        Me.lblProgress.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblProgress.Name = "lblProgress"
        Me.lblProgress.Size = New System.Drawing.Size(1128, 29)
        Me.lblProgress.TabIndex = 23
        Me.lblProgress.Text = "Progess Status ....."
        '
        'Panel1
        '
        Me.Panel1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Panel1.Controls.Add(Me.btnBrowse)
        Me.Panel1.Controls.Add(Me.txtFilePath)
        Me.Panel1.Controls.Add(Me.Label1)
        Me.Panel1.Controls.Add(Me.chkbHA)
        Me.Panel1.Controls.Add(Me.btnCancel)
        Me.Panel1.Controls.Add(Me.nmrcTimeFrame)
        Me.Panel1.Controls.Add(Me.lblTimeFrame)
        Me.Panel1.Controls.Add(Me.dtpckrToDate)
        Me.Panel1.Controls.Add(Me.dtpckrFromDate)
        Me.Panel1.Controls.Add(Me.lblToDate)
        Me.Panel1.Controls.Add(Me.lblFromDate)
        Me.Panel1.Controls.Add(Me.btnView)
        Me.Panel1.Controls.Add(Me.txtInstrumentName)
        Me.Panel1.Controls.Add(Me.Label4)
        Me.Panel1.Controls.Add(Me.cmbCategory)
        Me.Panel1.Controls.Add(Me.lblCategory)
        Me.Panel1.Controls.Add(Me.cmbRule)
        Me.Panel1.Controls.Add(Me.lblRule)
        Me.Panel1.Location = New System.Drawing.Point(4, 4)
        Me.Panel1.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(1237, 84)
        Me.Panel1.TabIndex = 22
        '
        'btnBrowse
        '
        Me.btnBrowse.Location = New System.Drawing.Point(1055, 52)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(33, 23)
        Me.btnBrowse.TabIndex = 33
        Me.btnBrowse.Text = "..."
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'txtFilePath
        '
        Me.txtFilePath.Location = New System.Drawing.Point(799, 52)
        Me.txtFilePath.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.txtFilePath.Name = "txtFilePath"
        Me.txtFilePath.Size = New System.Drawing.Size(252, 22)
        Me.txtFilePath.TabIndex = 32
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(730, 53)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(67, 17)
        Me.Label1.TabIndex = 31
        Me.Label1.Text = "File Path:"
        '
        'dgvSignal
        '
        Me.dgvSignal.AllowUserToAddRows = False
        Me.dgvSignal.AllowUserToDeleteRows = False
        Me.dgvSignal.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.dgvSignal.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvSignal.Location = New System.Drawing.Point(4, 145)
        Me.dgvSignal.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.dgvSignal.Name = "dgvSignal"
        Me.dgvSignal.ReadOnly = True
        Me.dgvSignal.RowTemplate.Height = 24
        Me.dgvSignal.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.dgvSignal.Size = New System.Drawing.Size(1237, 508)
        Me.dgvSignal.TabIndex = 21
        '
        'opnFile
        '
        '
        'lblDescription
        '
        Me.lblDescription.Location = New System.Drawing.Point(5, 90)
        Me.lblDescription.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.lblDescription.Name = "lblDescription"
        Me.lblDescription.Size = New System.Drawing.Size(1236, 53)
        Me.lblDescription.TabIndex = 25
        Me.lblDescription.Text = "Description ....."
        '
        'frmMain
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1244, 690)
        Me.Controls.Add(Me.lblDescription)
        Me.Controls.Add(Me.btnExport)
        Me.Controls.Add(Me.lblProgress)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.dgvSignal)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "frmMain"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Algo2Trade Signal Check"
        CType(Me.nmrcTimeFrame, System.ComponentModel.ISupportInitialize).EndInit()
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        CType(Me.dgvSignal, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents saveFile As SaveFileDialog
    Friend WithEvents btnExport As Button
    Friend WithEvents chkbHA As CheckBox
    Friend WithEvents btnCancel As Button
    Friend WithEvents nmrcTimeFrame As NumericUpDown
    Friend WithEvents lblTimeFrame As Label
    Friend WithEvents dtpckrToDate As DateTimePicker
    Friend WithEvents dtpckrFromDate As DateTimePicker
    Friend WithEvents lblToDate As Label
    Friend WithEvents lblFromDate As Label
    Friend WithEvents btnView As Button
    Friend WithEvents txtInstrumentName As TextBox
    Friend WithEvents Label4 As Label
    Friend WithEvents cmbCategory As ComboBox
    Friend WithEvents lblCategory As Label
    Friend WithEvents cmbRule As ComboBox
    Friend WithEvents lblRule As Label
    Friend WithEvents lblProgress As Label
    Friend WithEvents Panel1 As Panel
    Friend WithEvents dgvSignal As DataGridView
    Friend WithEvents txtFilePath As TextBox
    Friend WithEvents Label1 As Label
    Friend WithEvents btnBrowse As Button
    Friend WithEvents opnFile As OpenFileDialog
    Friend WithEvents lblDescription As Label
End Class
