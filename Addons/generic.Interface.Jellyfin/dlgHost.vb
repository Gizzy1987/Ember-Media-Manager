' ################################################################################
' #                             EMBER MEDIA MANAGER                              #
' ################################################################################
' ################################################################################
' # This file is part of Ember Media Manager.                                    #
' #                                                                              #
' # Ember Media Manager is free software: you can redistribute it and/or modify  #
' # it under the terms of the GNU General Public License as published by         #
' # the Free Software Foundation, either version 3 of the License, or            #
' # (at your option) any later version.                                          #
' #                                                                              #
' # Ember Media Manager is distributed in the hope that it will be useful,       #
' # but WITHOUT ANY WARRANTY; without even the implied warranty of               #
' # MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                #
' # GNU General Public License for more details.                                 #
' #                                                                              #
' # You should have received a copy of the GNU General Public License            #
' # along with Ember Media Manager.  If not, see <http://www.gnu.org/licenses/>. #
' ################################################################################

Imports EmberAPI
Imports NLog

Public Class dlgHost

#Region "Fields"

    Shared logger As Logger = LogManager.GetCurrentClassLogger()

    'current edited host
    Private _currentHost As JellyfinInterface.Host = Nothing

#End Region 'Fields

#Region "Properties"

    Public Property Result As JellyfinInterface.Host
        Get
            Return _currentHost
        End Get
        Set(value As JellyfinInterface.Host)
            _currentHost = value
        End Set
    End Property

#End Region 'Properties

#Region "Constructors"

    Sub New(ByVal Host As JellyfinInterface.Host)
        ' This call is required by the designer.
        InitializeComponent()
        Left = Master.AppPos.Left + (Master.AppPos.Width - Width) \ 2
        Top = Master.AppPos.Top + (Master.AppPos.Height - Height) \ 2
        StartPosition = FormStartPosition.Manual

        _currentHost = Host
    End Sub

#End Region 'Constructors'

#Region "Methods"

    ''' <summary>
    ''' Formload of host dialog
    ''' </summary>
    Private Sub dlgHost_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Setup()

        If Not _currentHost Is Nothing Then
            txtLabel.Text = _currentHost.Name
            txtUrl.Text = _currentHost.Url
            txtApiKey.Text = _currentHost.ApiKey
            chkHostEnabled.Checked = _currentHost.Enabled
        Else
            'new host entry (should not be possible)
            _currentHost = New JellyfinInterface.Host
        End If
    End Sub

    ''' <summary>
    ''' Set labels/translation text
    ''' </summary>
    Private Sub Setup()
        Text = Master.eLang.GetString(1718, "Jellyfin Interface")
        btnOK.Text = Master.eLang.GetString(179, "OK")
        btnCancel.Text = Master.eLang.GetString(167, "Cancel")
        btnHostConnectionCheck.Text = Master.eLang.GetString(1722, "Check Connection")

        gbHostDetails.Text = Master.eLang.GetString(1719, "Jellyfin Host")

        chkHostEnabled.Text = Master.eLang.GetString(774, "Enabled")
        lblHostLabel.Text = String.Concat(Master.eLang.GetString(232, "Name"), ":")
        lblHostUrl.Text = String.Concat(Master.eLang.GetString(1720, "Base URL"), ":")
        lblHostApiKey.Text = String.Concat(Master.eLang.GetString(1721, "API Key"), ":")
        lblHostLibraries.Text = String.Concat(Master.eLang.GetString(1735, "Libraries"), ":")
    End Sub

    Private Sub btnCancel_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnCancel.Click
        DialogResult = DialogResult.Cancel
    End Sub

    ''' <summary>
    ''' Test the connection to the current host and read its libraries
    ''' </summary>
    Private Async Sub btnHostConnectionCheck_Click(sender As Object, e As EventArgs) Handles btnHostConnectionCheck.Click
        Dim sUrl As String = clsAPIJellyfin.NormalizeBaseUrl(txtUrl.Text)

        If Not clsAPIJellyfin.IsValidBaseUrl(sUrl) Then
            MessageBox.Show(Master.eLang.GetString(1728, "Please enter a valid base URL (http:// or https://)!"),
                            Master.eLang.GetString(356, "Warning"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation)
            Exit Sub
        End If

        SetControlsEnabled(False)

        Try
            Dim api As New clsAPIJellyfin
            Dim info As clsAPIJellyfin.JellyfinServerInfo = Await api.GetServerInfoAsync(sUrl, txtApiKey.Text)

            If info Is Nothing Then
                MessageBox.Show(Master.eLang.GetString(1724, "There was a problem communicating with host."),
                                Master.eLang.GetString(356, "Warning"),
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation)
            Else
                MessageBox.Show(Master.eLang.GetString(1723, "Connection to host successful!") & Environment.NewLine & info.ToString(),
                                "",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information)

                Dim libraries As List(Of String) = Await api.GetVirtualFoldersAsync(sUrl, txtApiKey.Text)
                lbHostLibraries.Items.Clear()
                For Each sLibrary As String In libraries
                    lbHostLibraries.Items.Add(sLibrary)
                Next
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            MessageBox.Show(Master.eLang.GetString(1724, "There was a problem communicating with host."),
                            Master.eLang.GetString(356, "Warning"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation)
        Finally
            SetControlsEnabled(True)
        End Try
    End Sub

    Private Sub btnOK_Click(sender As Object, e As EventArgs) Handles btnOK.Click
        If String.IsNullOrWhiteSpace(txtLabel.Text) Then
            MessageBox.Show(Master.eLang.GetString(1726, "You must enter a name for this host!"),
                            Master.eLang.GetString(356, "Warning"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation)
            Exit Sub
        End If

        Dim sUrl As String = clsAPIJellyfin.NormalizeBaseUrl(txtUrl.Text)

        If String.IsNullOrWhiteSpace(txtUrl.Text) Then
            MessageBox.Show(Master.eLang.GetString(1727, "You must enter a base URL for this host!"),
                            Master.eLang.GetString(356, "Warning"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation)
            Exit Sub
        End If

        If Not clsAPIJellyfin.IsValidBaseUrl(sUrl) Then
            MessageBox.Show(Master.eLang.GetString(1728, "Please enter a valid base URL (http:// or https://)!"),
                            Master.eLang.GetString(356, "Warning"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation)
            Exit Sub
        End If

        SetInfo()

        DialogResult = DialogResult.OK
    End Sub

    Private Sub SetControlsEnabled(ByVal isEnabled As Boolean)
        gbHostDetails.Enabled = isEnabled
        btnHostConnectionCheck.Enabled = isEnabled
        btnOK.Enabled = isEnabled
        btnCancel.Enabled = isEnabled
        txtUrl.Enabled = isEnabled
        txtLabel.Enabled = isEnabled
        txtApiKey.Enabled = isEnabled
        chkHostEnabled.Enabled = isEnabled
        lbHostLibraries.Enabled = isEnabled
    End Sub

    ''' <summary>
    ''' Copy the values of the controls back into the host
    ''' </summary>
    Private Sub SetInfo()
        If _currentHost Is Nothing Then _currentHost = New JellyfinInterface.Host

        _currentHost.Name = txtLabel.Text.Trim()
        _currentHost.Url = clsAPIJellyfin.NormalizeBaseUrl(txtUrl.Text)
        _currentHost.ApiKey = txtApiKey.Text
        _currentHost.Enabled = chkHostEnabled.Checked
    End Sub

#End Region 'Methods'

#Region "Nested Types"
#End Region 'Nested Types"

End Class
