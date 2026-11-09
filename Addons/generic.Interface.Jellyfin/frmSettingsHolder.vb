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

Public Class frmSettingsHolder

#Region "Fields"

    Shared logger As Logger = LogManager.GetCurrentClassLogger()

    Public HostList As New List(Of JellyfinInterface.Host)

#End Region 'Fields

#Region "Events"

    Public Event ModuleEnabledChanged(ByVal State As Boolean)
    Public Event ModuleSettingsChanged()

#End Region 'Events

#Region "Constructors"

    Public Sub New()
        InitializeComponent()
        SetUp()
    End Sub

#End Region 'Constructors'

#Region "Methods"

    ''' <summary>
    ''' Set labels/translation text
    ''' </summary>
    Private Sub SetUp()
        btnAddHost.Text = Master.eLang.GetString(28, "Add")
        btnEditHost.Text = Master.eLang.GetString(1440, "Edit")
        btnRemoveHost.Text = Master.eLang.GetString(30, "Remove")
        chkEnabled.Text = Master.eLang.GetString(774, "Enabled")
        chkNotification.Text = Master.eLang.GetString(1441, "Send Notifications")
        gbHosts.Text = Master.eLang.GetString(420, "Settings")
    End Sub

    ''' <summary>
    ''' Enable/Disable module
    ''' </summary>
    Private Sub chkEnabled_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) Handles chkEnabled.CheckedChanged
        RaiseEvent ModuleEnabledChanged(chkEnabled.Checked)
    End Sub

    ''' <summary>
    ''' Refresh view of hosts in settings page
    ''' </summary>
    Public Sub ReloadHosts()
        btnEditHost.Enabled = False
        btnRemoveHost.Enabled = False
        lbHosts.Items.Clear()
        For Each mHost As JellyfinInterface.Host In HostList
            lbHosts.Items.Add(mHost.Name)
        Next
    End Sub

    ''' <summary>
    ''' Remove selected host
    ''' </summary>
    Private Sub btnRemoveHost_Click(sender As Object, e As EventArgs) Handles btnRemoveHost.Click
        RemoveHost()
    End Sub

    ''' <summary>
    ''' Remove selected host (DEL-key on keyboard)
    ''' </summary>
    Private Sub lbHosts_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles lbHosts.KeyDown
        If e.KeyCode = Keys.Delete Then RemoveHost()
    End Sub

    ''' <summary>
    ''' Remove selected host from the list
    ''' </summary>
    Private Sub RemoveHost()
        If lbHosts.SelectedItems.Count > 0 Then
            Dim mHost As JellyfinInterface.Host = HostList.FirstOrDefault(Function(f) f.Name = lbHosts.SelectedItem.ToString)
            If mHost IsNot Nothing Then
                HostList.Remove(mHost)
                ReloadHosts()
                RaiseEvent ModuleSettingsChanged()
            End If
        End If
    End Sub

    ''' <summary>
    ''' Open host dialog to enter a new host
    ''' </summary>
    Private Sub btnAddHost_Click(sender As Object, e As EventArgs) Handles btnAddHost.Click
        Dim newHost As New JellyfinInterface.Host
        Dim dlgNew As New dlgHost(newHost)
        If dlgNew.ShowDialog = Windows.Forms.DialogResult.OK Then
            HostList.Add(dlgNew.Result)
            ReloadHosts()
            RaiseEvent ModuleSettingsChanged()
        End If
    End Sub

    ''' <summary>
    ''' Open host dialog to modify a host
    ''' </summary>
    Private Sub btnEditHost_Click(sender As Object, e As EventArgs) Handles btnEditHost.Click, lbHosts.DoubleClick
        If lbHosts.SelectedItems.Count > 0 Then
            Dim mHost As JellyfinInterface.Host = HostList.FirstOrDefault(Function(f) f.Name = lbHosts.SelectedItem.ToString)
            If mHost IsNot Nothing Then
                Dim dlgNew As New dlgHost(mHost)
                If dlgNew.ShowDialog = DialogResult.OK Then
                    ReloadHosts()
                    RaiseEvent ModuleSettingsChanged()
                End If
            End If
        End If
    End Sub

    ''' <summary>
    ''' Enable/Disable Edit and Remove buttons
    ''' </summary>
    Private Sub lbHosts_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles lbHosts.SelectedIndexChanged
        If lbHosts.SelectedItems.Count > 0 Then
            btnEditHost.Enabled = True
            btnRemoveHost.Enabled = True
        Else
            btnEditHost.Enabled = False
            btnRemoveHost.Enabled = False
        End If
    End Sub

    Private Sub EnableApplyButton() Handles chkNotification.CheckedChanged
        RaiseEvent ModuleSettingsChanged()
    End Sub

#End Region 'Methods'

#Region "Nested Types"
#End Region 'Nested Types"

End Class
