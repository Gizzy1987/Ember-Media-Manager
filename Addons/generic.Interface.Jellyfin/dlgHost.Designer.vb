<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class dlgHost
    Inherits System.Windows.Forms.Form

    'Das Formular überschreibt den Löschvorgang, um die Komponentenliste zu bereinigen.
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

    'Wird vom Windows Form-Designer benötigt.
    Private components As System.ComponentModel.IContainer

    'Hinweis: Die folgende Prozedur ist für den Windows Form-Designer erforderlich.
    'Das Bearbeiten ist mit dem Windows Form-Designer möglich.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.gbHostDetails = New System.Windows.Forms.GroupBox()
        Me.lblHostLabel = New System.Windows.Forms.Label()
        Me.txtLabel = New System.Windows.Forms.TextBox()
        Me.lblHostUrl = New System.Windows.Forms.Label()
        Me.txtUrl = New System.Windows.Forms.TextBox()
        Me.lblHostApiKey = New System.Windows.Forms.Label()
        Me.txtApiKey = New System.Windows.Forms.TextBox()
        Me.chkHostEnabled = New System.Windows.Forms.CheckBox()
        Me.btnHostConnectionCheck = New System.Windows.Forms.Button()
        Me.lblHostLibraries = New System.Windows.Forms.Label()
        Me.lbHostLibraries = New System.Windows.Forms.ListBox()
        Me.btnOK = New System.Windows.Forms.Button()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.gbHostDetails.SuspendLayout()
        Me.SuspendLayout()
        '
        'gbHostDetails
        '
        Me.gbHostDetails.Controls.Add(Me.lblHostLabel)
        Me.gbHostDetails.Controls.Add(Me.txtLabel)
        Me.gbHostDetails.Controls.Add(Me.lblHostUrl)
        Me.gbHostDetails.Controls.Add(Me.txtUrl)
        Me.gbHostDetails.Controls.Add(Me.lblHostApiKey)
        Me.gbHostDetails.Controls.Add(Me.txtApiKey)
        Me.gbHostDetails.Controls.Add(Me.chkHostEnabled)
        Me.gbHostDetails.Controls.Add(Me.btnHostConnectionCheck)
        Me.gbHostDetails.Controls.Add(Me.lblHostLibraries)
        Me.gbHostDetails.Controls.Add(Me.lbHostLibraries)
        Me.gbHostDetails.Location = New System.Drawing.Point(8, 8)
        Me.gbHostDetails.Name = "gbHostDetails"
        Me.gbHostDetails.Size = New System.Drawing.Size(480, 200)
        Me.gbHostDetails.TabIndex = 0
        Me.gbHostDetails.TabStop = False
        Me.gbHostDetails.Text = "Jellyfin Host"
        '
        'lblHostLabel
        '
        Me.lblHostLabel.AutoSize = True
        Me.lblHostLabel.Location = New System.Drawing.Point(12, 28)
        Me.lblHostLabel.Name = "lblHostLabel"
        Me.lblHostLabel.Size = New System.Drawing.Size(35, 13)
        Me.lblHostLabel.TabIndex = 0
        Me.lblHostLabel.Text = "Name"
        '
        'txtLabel
        '
        Me.txtLabel.Location = New System.Drawing.Point(100, 25)
        Me.txtLabel.Name = "txtLabel"
        Me.txtLabel.Size = New System.Drawing.Size(240, 21)
        Me.txtLabel.TabIndex = 1
        Me.txtLabel.Text = "My Jellyfin"
        '
        'lblHostUrl
        '
        Me.lblHostUrl.AutoSize = True
        Me.lblHostUrl.Location = New System.Drawing.Point(12, 55)
        Me.lblHostUrl.Name = "lblHostUrl"
        Me.lblHostUrl.Size = New System.Drawing.Size(53, 13)
        Me.lblHostUrl.TabIndex = 2
        Me.lblHostUrl.Text = "Base URL"
        '
        'txtUrl
        '
        Me.txtUrl.Location = New System.Drawing.Point(100, 52)
        Me.txtUrl.Name = "txtUrl"
        Me.txtUrl.Size = New System.Drawing.Size(240, 21)
        Me.txtUrl.TabIndex = 3
        Me.txtUrl.Text = "http://localhost:8096"
        '
        'lblHostApiKey
        '
        Me.lblHostApiKey.AutoSize = True
        Me.lblHostApiKey.Location = New System.Drawing.Point(12, 82)
        Me.lblHostApiKey.Name = "lblHostApiKey"
        Me.lblHostApiKey.Size = New System.Drawing.Size(45, 13)
        Me.lblHostApiKey.TabIndex = 4
        Me.lblHostApiKey.Text = "API Key"
        '
        'txtApiKey
        '
        Me.txtApiKey.Location = New System.Drawing.Point(100, 79)
        Me.txtApiKey.Name = "txtApiKey"
        Me.txtApiKey.PasswordChar = Global.Microsoft.VisualBasic.ChrW(42)
        Me.txtApiKey.Size = New System.Drawing.Size(240, 21)
        Me.txtApiKey.TabIndex = 5
        '
        'chkHostEnabled
        '
        Me.chkHostEnabled.AutoSize = True
        Me.chkHostEnabled.Location = New System.Drawing.Point(352, 25)
        Me.chkHostEnabled.Name = "chkHostEnabled"
        Me.chkHostEnabled.Size = New System.Drawing.Size(53, 17)
        Me.chkHostEnabled.TabIndex = 6
        Me.chkHostEnabled.Text = "Enabled"
        Me.chkHostEnabled.UseVisualStyleBackColor = True
        '
        'btnHostConnectionCheck
        '
        Me.btnHostConnectionCheck.Location = New System.Drawing.Point(352, 52)
        Me.btnHostConnectionCheck.Name = "btnHostConnectionCheck"
        Me.btnHostConnectionCheck.Size = New System.Drawing.Size(120, 25)
        Me.btnHostConnectionCheck.TabIndex = 7
        Me.btnHostConnectionCheck.Text = "Check Connection"
        Me.btnHostConnectionCheck.UseVisualStyleBackColor = True
        '
        'lblHostLibraries
        '
        Me.lblHostLibraries.AutoSize = True
        Me.lblHostLibraries.Location = New System.Drawing.Point(352, 85)
        Me.lblHostLibraries.Name = "lblHostLibraries"
        Me.lblHostLibraries.Size = New System.Drawing.Size(48, 13)
        Me.lblHostLibraries.TabIndex = 8
        Me.lblHostLibraries.Text = "Libraries"
        '
        'lbHostLibraries
        '
        Me.lbHostLibraries.FormattingEnabled = True
        Me.lbHostLibraries.Location = New System.Drawing.Point(352, 102)
        Me.lbHostLibraries.Name = "lbHostLibraries"
        Me.lbHostLibraries.Size = New System.Drawing.Size(120, 88)
        Me.lbHostLibraries.TabIndex = 9
        '
        'btnOK
        '
        Me.btnOK.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnOK.DialogResult = System.Windows.Forms.DialogResult.None
        Me.btnOK.Location = New System.Drawing.Point(300, 220)
        Me.btnOK.Name = "btnOK"
        Me.btnOK.Size = New System.Drawing.Size(85, 25)
        Me.btnOK.TabIndex = 10
        Me.btnOK.Text = "OK"
        Me.btnOK.UseVisualStyleBackColor = True
        '
        'btnCancel
        '
        Me.btnCancel.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.btnCancel.Location = New System.Drawing.Point(395, 220)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(85, 25)
        Me.btnCancel.TabIndex = 11
        Me.btnCancel.Text = "Cancel"
        Me.btnCancel.UseVisualStyleBackColor = True
        '
        'dlgHost
        '
        Me.AcceptButton = Me.btnOK
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.btnCancel
        Me.ClientSize = New System.Drawing.Size(500, 260)
        Me.Controls.Add(Me.gbHostDetails)
        Me.Controls.Add(Me.btnOK)
        Me.Controls.Add(Me.btnCancel)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "dlgHost"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "Jellyfin Interface"
        Me.gbHostDetails.ResumeLayout(False)
        Me.gbHostDetails.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents gbHostDetails As System.Windows.Forms.GroupBox
    Friend WithEvents lblHostLabel As System.Windows.Forms.Label
    Friend WithEvents txtLabel As System.Windows.Forms.TextBox
    Friend WithEvents lblHostUrl As System.Windows.Forms.Label
    Friend WithEvents txtUrl As System.Windows.Forms.TextBox
    Friend WithEvents lblHostApiKey As System.Windows.Forms.Label
    Friend WithEvents txtApiKey As System.Windows.Forms.TextBox
    Friend WithEvents chkHostEnabled As System.Windows.Forms.CheckBox
    Friend WithEvents btnHostConnectionCheck As System.Windows.Forms.Button
    Friend WithEvents lblHostLibraries As System.Windows.Forms.Label
    Friend WithEvents lbHostLibraries As System.Windows.Forms.ListBox
    Friend WithEvents btnOK As System.Windows.Forms.Button
    Friend WithEvents btnCancel As System.Windows.Forms.Button
End Class
