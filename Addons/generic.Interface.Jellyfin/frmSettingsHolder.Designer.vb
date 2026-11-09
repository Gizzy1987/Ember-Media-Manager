<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmSettingsHolder
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

    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.pnlSettings = New System.Windows.Forms.Panel()
        Me.chkEnabled = New System.Windows.Forms.CheckBox()
        Me.chkNotification = New System.Windows.Forms.CheckBox()
        Me.gbHosts = New System.Windows.Forms.GroupBox()
        Me.lbHosts = New System.Windows.Forms.ListBox()
        Me.btnAddHost = New System.Windows.Forms.Button()
        Me.btnEditHost = New System.Windows.Forms.Button()
        Me.btnRemoveHost = New System.Windows.Forms.Button()
        Me.pnlSettings.SuspendLayout()
        Me.gbHosts.SuspendLayout()
        Me.SuspendLayout()
        '
        'pnlSettings
        '
        Me.pnlSettings.AutoSize = True
        Me.pnlSettings.BackColor = System.Drawing.Color.White
        Me.pnlSettings.Controls.Add(Me.gbHosts)
        Me.pnlSettings.Controls.Add(Me.chkNotification)
        Me.pnlSettings.Controls.Add(Me.chkEnabled)
        Me.pnlSettings.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pnlSettings.Location = New System.Drawing.Point(0, 0)
        Me.pnlSettings.Name = "pnlSettings"
        Me.pnlSettings.Size = New System.Drawing.Size(500, 310)
        Me.pnlSettings.TabIndex = 0
        '
        'chkEnabled
        '
        Me.chkEnabled.AutoSize = True
        Me.chkEnabled.Location = New System.Drawing.Point(12, 12)
        Me.chkEnabled.Name = "chkEnabled"
        Me.chkEnabled.Size = New System.Drawing.Size(53, 17)
        Me.chkEnabled.TabIndex = 0
        Me.chkEnabled.Text = "Enabled"
        Me.chkEnabled.UseVisualStyleBackColor = True
        '
        'chkNotification
        '
        Me.chkNotification.AutoSize = True
        Me.chkNotification.Location = New System.Drawing.Point(12, 40)
        Me.chkNotification.Name = "chkNotification"
        Me.chkNotification.Size = New System.Drawing.Size(121, 17)
        Me.chkNotification.TabIndex = 1
        Me.chkNotification.Text = "Send Notifications"
        Me.chkNotification.UseVisualStyleBackColor = True
        '
        'gbHosts
        '
        Me.gbHosts.Controls.Add(Me.lbHosts)
        Me.gbHosts.Controls.Add(Me.btnAddHost)
        Me.gbHosts.Controls.Add(Me.btnEditHost)
        Me.gbHosts.Controls.Add(Me.btnRemoveHost)
        Me.gbHosts.Location = New System.Drawing.Point(12, 68)
        Me.gbHosts.Name = "gbHosts"
        Me.gbHosts.Size = New System.Drawing.Size(470, 230)
        Me.gbHosts.TabIndex = 2
        Me.gbHosts.TabStop = False
        Me.gbHosts.Text = "Settings"
        '
        'lbHosts
        '
        Me.lbHosts.FormattingEnabled = True
        Me.lbHosts.Location = New System.Drawing.Point(12, 25)
        Me.lbHosts.Name = "lbHosts"
        Me.lbHosts.Size = New System.Drawing.Size(280, 192)
        Me.lbHosts.Sorted = True
        Me.lbHosts.TabIndex = 0
        '
        'btnAddHost
        '
        Me.btnAddHost.Location = New System.Drawing.Point(306, 25)
        Me.btnAddHost.Name = "btnAddHost"
        Me.btnAddHost.Size = New System.Drawing.Size(90, 25)
        Me.btnAddHost.TabIndex = 1
        Me.btnAddHost.Text = "Add"
        Me.btnAddHost.UseVisualStyleBackColor = True
        '
        'btnEditHost
        '
        Me.btnEditHost.Enabled = False
        Me.btnEditHost.Location = New System.Drawing.Point(306, 60)
        Me.btnEditHost.Name = "btnEditHost"
        Me.btnEditHost.Size = New System.Drawing.Size(90, 25)
        Me.btnEditHost.TabIndex = 2
        Me.btnEditHost.Text = "Edit"
        Me.btnEditHost.UseVisualStyleBackColor = True
        '
        'btnRemoveHost
        '
        Me.btnRemoveHost.Enabled = False
        Me.btnRemoveHost.Location = New System.Drawing.Point(306, 95)
        Me.btnRemoveHost.Name = "btnRemoveHost"
        Me.btnRemoveHost.Size = New System.Drawing.Size(90, 25)
        Me.btnRemoveHost.TabIndex = 3
        Me.btnRemoveHost.Text = "Remove"
        Me.btnRemoveHost.UseVisualStyleBackColor = True
        '
        'frmSettingsHolder
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(500, 310)
        Me.Controls.Add(Me.pnlSettings)
        Me.Name = "frmSettingsHolder"
        Me.Text = "Jellyfin Interface"
        Me.pnlSettings.ResumeLayout(False)
        Me.pnlSettings.PerformLayout()
        Me.gbHosts.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents pnlSettings As System.Windows.Forms.Panel
    Friend WithEvents chkEnabled As System.Windows.Forms.CheckBox
    Friend WithEvents chkNotification As System.Windows.Forms.CheckBox
    Friend WithEvents gbHosts As System.Windows.Forms.GroupBox
    Friend WithEvents lbHosts As System.Windows.Forms.ListBox
    Friend WithEvents btnAddHost As System.Windows.Forms.Button
    Friend WithEvents btnEditHost As System.Windows.Forms.Button
    Friend WithEvents btnRemoveHost As System.Windows.Forms.Button
End Class
