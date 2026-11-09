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
Imports System.Drawing
Imports System.IO
Imports NLog
Imports System.Reflection
Imports System.Security.Cryptography
Imports System.Text
Imports System.Xml
Imports System.Xml.Serialization

''' <summary>
''' Jellyfin interface for Ember Media Manager.
''' Allows to manage one or more Jellyfin servers and to trigger a library
''' refresh on them. Items are never synchronized one by one - Jellyfin cannot
''' do that - instead a debounced library refresh is triggered after a short
''' idle period.
''' </summary>
Public Class JellyfinInterface
    Implements Interfaces.GenericModule

#Region "Delegates"

    Public Delegate Sub Delegate_SetToolStripItem(ts As ToolStrip, value As ToolStripItem)
    Public Delegate Sub Delegate_AddToolStripMenuItem(tsi As ToolStripMenuItem, value As ToolStripMenuItem)
    Public Delegate Sub Delegate_ChangeTaskManagerStatus(control As ToolStripLabel, value As String)
    Public Delegate Sub Delegate_ChangeTaskManagerProgressBar(control As ToolStripProgressBar, value As ProgressBarStyle)

#End Region 'Delegates

#Region "Fields"

    Shared logger As Logger = LogManager.GetCurrentClassLogger()

    ''' <summary>
    ''' reflects the current host(s) settings/setup configured in settings, will be filled at module startup from XML settings (and is used to write changes of settings back into XML)
    ''' </summary>
    Private _SpecialSettings As New SpecialSettings
    Private _AssemblyName As String = String.Empty
    Private _Enabled As Boolean = False
    Private _Name As String = "Jellyfin"
    Private _setup As frmSettingsHolder
    Private _xmlSettingsPath As String = Path.Combine(Master.SettingsPath, "Interface.Jellyfin.xml")
    Private _api As New clsAPIJellyfin
    Private _busy As Boolean = False

    ''' <summary>
    ''' Scheme ("http" or "https") that worked for a host url - used to avoid
    ''' trying a failing scheme again on every call.
    ''' </summary>
    Private _hostSchemes As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

    ''' <summary>
    ''' Hosts for which the user was already warned about the unencrypted connection.
    ''' </summary>
    Private _warnedInsecureHosts As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

    ''' <summary>
    ''' Prefix that marks an api key stored in encrypted form in the settings file.
    ''' </summary>
    Private Const EncryptedApiKeyPrefix As String = "dpapi:"

    ''' <summary>
    ''' Additional entropy used for the dpapi encryption of the api key.
    ''' </summary>
    Private Shared ReadOnly ApiKeyEntropy As Byte() = Encoding.UTF8.GetBytes("Ember.MediaManager.Jellyfin.ApiKey")

    Private mnuMainToolsJellyfin As New ToolStripMenuItem
    Private mnuTrayToolsJellyfin As New ToolStripMenuItem

    Private lblTaskManagerStatus As New ToolStripLabel With {.Alignment = ToolStripItemAlignment.Right, .Visible = True}
    Private lblTaskManagerTitle As New ToolStripLabel With {.Alignment = ToolStripItemAlignment.Right, .Visible = True}
    Private tspTaskManager As New ToolStripProgressBar With {.Alignment = ToolStripItemAlignment.Right, .Visible = True}
    Private tssTaskManager1 As New ToolStripSeparator With {.Alignment = ToolStripItemAlignment.Right, .Visible = True}
    Private tssTaskManager2 As New ToolStripSeparator With {.Alignment = ToolStripItemAlignment.Right, .Visible = True}
    Private tssTaskManager3 As New ToolStripSeparator With {.Alignment = ToolStripItemAlignment.Right, .Visible = True}
    Private tssTaskManager4 As New ToolStripSeparator With {.Alignment = ToolStripItemAlignment.Right, .Visible = True}

    ''' <summary>
    ''' Debounce timer: a library refresh is only triggered when no further
    ''' sync/remove event was received for "RefreshDelayMilliseconds" milliseconds.
    ''' NOTE: the module is instantiated by a background worker, so a
    ''' System.Windows.Forms.Timer can not be used here (no message pump on that
    ''' thread) - a System.Threading.Timer is used instead.
    ''' </summary>
    Private Const RefreshDelayMilliseconds As Integer = 5000

    Private _refreshTimer As New System.Threading.Timer(AddressOf OnLibraryRefreshTimer,
                                                        Nothing,
                                                        System.Threading.Timeout.Infinite,
                                                        System.Threading.Timeout.Infinite)

#End Region 'Fields

#Region "Events"

    Public Event GenericEvent(ByVal mType As Enums.ModuleEventType, ByRef _params As List(Of Object)) Implements Interfaces.GenericModule.GenericEvent
    Public Event ModuleSetupChanged(ByVal Name As String, ByVal State As Boolean, ByVal diffOrder As Integer) Implements Interfaces.GenericModule.ModuleSetupChanged
    Public Event ModuleSettingsChanged() Implements Interfaces.GenericModule.ModuleSettingsChanged
    Public Event SetupNeedsRestart() Implements Interfaces.GenericModule.SetupNeedsRestart

#End Region 'Events

#Region "Properties"

    ''' <summary>
    ''' Subscribe to Eventtypes here
    ''' </summary>
    Public ReadOnly Property ModuleType() As List(Of Enums.ModuleEventType) Implements Interfaces.GenericModule.ModuleType
        Get
            Return New List(Of Enums.ModuleEventType)(New Enums.ModuleEventType() {
                Enums.ModuleEventType.Sync_Movie,
                Enums.ModuleEventType.Sync_MovieSet,
                Enums.ModuleEventType.Sync_TVEpisode,
                Enums.ModuleEventType.Sync_TVSeason,
                Enums.ModuleEventType.Sync_TVShow,
                Enums.ModuleEventType.Remove_Movie,
                Enums.ModuleEventType.Remove_TVEpisode,
                Enums.ModuleEventType.Remove_TVShow})
        End Get
    End Property

    Property Enabled() As Boolean Implements Interfaces.GenericModule.Enabled
        Get
            Return _Enabled
        End Get
        Set(ByVal value As Boolean)
            If _Enabled = value Then Return
            _Enabled = value
            If _Enabled Then
                Enable()
            Else
                Disable()
            End If
        End Set
    End Property

    ReadOnly Property IsBusy() As Boolean Implements Interfaces.GenericModule.IsBusy
        Get
            Return _busy
        End Get
    End Property

    ReadOnly Property ModuleName() As String Implements Interfaces.GenericModule.ModuleName
        Get
            Return _Name
        End Get
    End Property

    ReadOnly Property ModuleVersion() As String Implements Interfaces.GenericModule.ModuleVersion
        Get
            Return FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileVersion.ToString
        End Get
    End Property

#End Region 'Properties

#Region "Methods"

    ''' <summary>
    ''' Implementation of the realtime sync, triggered outside of this module
    ''' i.e. after finishing edits of a movie (=Enums.ModuleEventType.Sync_Movie)
    ''' </summary>
    ''' <remarks>
    ''' Jellyfin cannot update single items, so every sync/remove event only
    ''' schedules a debounced library refresh.
    ''' </remarks>
    Public Function RunGeneric(ByVal mType As Enums.ModuleEventType, ByRef _params As List(Of Object), ByRef _singleobjekt As Object, ByRef _dbelement As Database.DBElement) As Interfaces.ModuleResult Implements Interfaces.GenericModule.RunGeneric
        Select Case mType
            Case Enums.ModuleEventType.Sync_Movie,
                Enums.ModuleEventType.Sync_MovieSet,
                Enums.ModuleEventType.Sync_TVEpisode,
                Enums.ModuleEventType.Sync_TVSeason,
                Enums.ModuleEventType.Sync_TVShow,
                Enums.ModuleEventType.Remove_Movie,
                Enums.ModuleEventType.Remove_TVEpisode,
                Enums.ModuleEventType.Remove_TVShow
                ScheduleLibraryRefresh()
        End Select

        Return New Interfaces.ModuleResult With {.breakChain = False}
    End Function

    ''' <summary>
    ''' Actions on module startup (Ember startup)
    ''' </summary>
    Sub Init(ByVal sAssemblyName As String, ByVal sExecutable As String) Implements Interfaces.GenericModule.Init
        _AssemblyName = sAssemblyName
        LoadSettings()
    End Sub

    ''' <summary>
    ''' Load module settings
    ''' </summary>
    Private Sub LoadSettings()
        _SpecialSettings = New SpecialSettings

        If File.Exists(_xmlSettingsPath) Then
            Try
                Dim xmlSer As XmlSerializer = New XmlSerializer(GetType(SpecialSettings))
                Using xmlSR As StreamReader = New StreamReader(_xmlSettingsPath)
                    _SpecialSettings = DirectCast(xmlSer.Deserialize(xmlSR), SpecialSettings)
                End Using
            Catch ex As Exception
                logger.Error(ex, New StackFrame().GetMethod().Name)
                _SpecialSettings = New SpecialSettings
            End Try
        End If

        If _SpecialSettings.Hosts Is Nothing Then _SpecialSettings.Hosts = New List(Of Host)

        Dim bPlaintextApiKey As Boolean = False

        For Each mHost As Host In _SpecialSettings.Hosts
            mHost.Url = NormalizeHostUrl(mHost.Url)
            If mHost.HasPlaintextApiKey() Then bPlaintextApiKey = True
        Next

        'migrate api keys of older settings files into the encrypted form
        If bPlaintextApiKey Then SaveSettings()
    End Sub

    ''' <summary>
    ''' Save module settings to XML
    ''' </summary>
    Private Sub SaveSettings()
        Try
            If File.Exists(_xmlSettingsPath) Then
                Dim fAtt As FileAttributes = File.GetAttributes(_xmlSettingsPath)
                If (fAtt And FileAttributes.ReadOnly) = FileAttributes.ReadOnly Then
                    File.SetAttributes(_xmlSettingsPath, FileAttributes.Normal)
                End If
            End If

            Using xmlSW As New StreamWriter(_xmlSettingsPath)
                Dim xmlSer As New XmlSerializer(GetType(SpecialSettings))
                xmlSer.Serialize(xmlSW, _SpecialSettings)
            End Using
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    ''' <summary>
    ''' Load and fill controls of settings page of module
    ''' </summary>
    Function InjectSetup() As Containers.SettingsPanel Implements Interfaces.GenericModule.InjectSetup
        Dim SPanel As New Containers.SettingsPanel
        _setup = New frmSettingsHolder
        LoadSettings()

        _setup.chkEnabled.Checked = _Enabled
        _setup.chkNotification.Checked = _SpecialSettings.SendNotifications
        _setup.HostList = _SpecialSettings.Hosts
        _setup.ReloadHosts()

        SPanel.Name = _Name
        SPanel.Text = Master.eLang.GetString(1718, "Jellyfin Interface")
        SPanel.Prefix = "Jellyfin_"
        SPanel.Type = Master.eLang.GetString(802, "Modules")
        SPanel.ImageIndex = If(_Enabled, 9, 10)
        SPanel.Order = 100
        SPanel.Panel = _setup.pnlSettings

        AddHandler _setup.ModuleEnabledChanged, AddressOf Handle_ModuleEnabledChanged
        AddHandler _setup.ModuleSettingsChanged, AddressOf Handle_ModuleSettingsChanged
        Return SPanel
    End Function

    Sub SaveSetupModule(ByVal DoDispose As Boolean) Implements Interfaces.GenericModule.SaveSetup
        Enabled = _setup.chkEnabled.Checked
        _SpecialSettings.SendNotifications = _setup.chkNotification.Checked

        For Each mHost As Host In _SpecialSettings.Hosts
            mHost.Url = NormalizeHostUrl(mHost.Url)
        Next

        SaveSettings()

        If Enabled Then PopulateMenus()

        If DoDispose Then
            RemoveHandler _setup.ModuleEnabledChanged, AddressOf Handle_ModuleEnabledChanged
            RemoveHandler _setup.ModuleSettingsChanged, AddressOf Handle_ModuleSettingsChanged
            _setup.Dispose()
        End If
    End Sub

    ''' <summary>
    ''' Actions on module startup and runtime if module is enabled
    ''' </summary>
    Private Sub Enable()
        PopulateMenus()
    End Sub

    ''' <summary>
    ''' Actions on module startup and runtime if module is disabled
    ''' </summary>
    Private Sub Disable()
        Dim tsi As New ToolStripMenuItem

        'mnuMainTools
        tsi = DirectCast(ModulesManager.Instance.RuntimeObjects.MainMenu.Items("mnuMainTools"), ToolStripMenuItem)
        tsi.DropDownItems.Remove(mnuMainToolsJellyfin)

        'cmnuTrayTools
        tsi = DirectCast(ModulesManager.Instance.RuntimeObjects.TrayMenu.Items("cmnuTrayTools"), ToolStripMenuItem)
        tsi.DropDownItems.Remove(mnuTrayToolsJellyfin)

        'Task Manager
        Dim ts As ToolStrip = DirectCast(ModulesManager.Instance.RuntimeObjects.MainToolStrip, ToolStrip)
        ts.Items.Remove(lblTaskManagerStatus)
        ts.Items.Remove(lblTaskManagerTitle)
        ts.Items.Remove(tspTaskManager)
        ts.Items.Remove(tssTaskManager1)
        ts.Items.Remove(tssTaskManager2)
        ts.Items.Remove(tssTaskManager3)
        ts.Items.Remove(tssTaskManager4)
    End Sub

    ''' <summary>
    ''' Build the menu entries of this module
    ''' </summary>
    Private Sub PopulateMenus()
        Dim tsi As New ToolStripMenuItem

        'mnuMainTools menu
        mnuMainToolsJellyfin.DropDownItems.Clear()
        mnuMainToolsJellyfin.Image = New Bitmap(My.Resources.icon)
        mnuMainToolsJellyfin.Text = Master.eLang.GetString(1736, "Jellyfin")
        CreateToolsMenu(mnuMainToolsJellyfin)
        tsi = DirectCast(ModulesManager.Instance.RuntimeObjects.MainMenu.Items("mnuMainTools"), ToolStripMenuItem)
        AddToolStripItem(tsi, mnuMainToolsJellyfin)

        'mnuTrayTools
        mnuTrayToolsJellyfin.DropDownItems.Clear()
        mnuTrayToolsJellyfin.Image = New Bitmap(My.Resources.icon)
        mnuTrayToolsJellyfin.Text = Master.eLang.GetString(1736, "Jellyfin")
        CreateToolsMenu(mnuTrayToolsJellyfin)
        tsi = DirectCast(ModulesManager.Instance.RuntimeObjects.TrayMenu.Items("cmnuTrayTools"), ToolStripMenuItem)
        AddToolStripItem(tsi, mnuTrayToolsJellyfin)

        'Task Manager
        lblTaskManagerStatus.Text = Master.eLang.GetString(1731, "No Pending Tasks")
        lblTaskManagerTitle.Text = Master.eLang.GetString(1730, "Jellyfin Task Manager")
        Dim ts As ToolStrip = DirectCast(ModulesManager.Instance.RuntimeObjects.MainToolStrip, ToolStrip)
        AddToolStripItem(ts, tssTaskManager1)
        AddToolStripItem(ts, tssTaskManager2)
        AddToolStripItem(ts, tspTaskManager)
        AddToolStripItem(ts, tssTaskManager3)
        AddToolStripItem(ts, tssTaskManager4)
        AddToolStripItem(ts, lblTaskManagerStatus)
        AddToolStripItem(ts, lblTaskManagerTitle)
    End Sub

    ''' <summary>
    ''' Fill the tools/tray menu with the configured hosts
    ''' </summary>
    Private Sub CreateToolsMenu(ByRef tMenu As ToolStripMenuItem)
        Dim enabledHosts As List(Of Host) = New List(Of Host)

        If _SpecialSettings.Hosts IsNot Nothing Then
            For Each mHost As Host In _SpecialSettings.Hosts
                If mHost.Enabled Then enabledHosts.Add(mHost)
            Next
        End If

        If enabledHosts.Count = 0 Then
            Dim mnuDummy As New ToolStripMenuItem
            mnuDummy.Enabled = False
            mnuDummy.Text = Master.eLang.GetString(1729, "No Jellyfin host configured!")
            tMenu.DropDownItems.Add(mnuDummy)
            Exit Sub
        End If

        If enabledHosts.Count = 1 Then
            tMenu.DropDownItems.Add(CreateRefreshLibraryMenuItem(enabledHosts(0)))
            tMenu.DropDownItems.Add(CreateCheckConnectionMenuItem(enabledHosts(0)))
        Else
            For Each mHost As Host In enabledHosts
                Dim mnuHost As New ToolStripMenuItem
                mnuHost.Image = New Bitmap(My.Resources.icon)
                mnuHost.Text = mHost.Name
                mnuHost.DropDownItems.Add(CreateRefreshLibraryMenuItem(mHost))
                mnuHost.DropDownItems.Add(CreateCheckConnectionMenuItem(mHost))
                tMenu.DropDownItems.Add(mnuHost)
            Next
        End If
    End Sub

    Private Function CreateRefreshLibraryMenuItem(ByVal mHost As Host) As ToolStripMenuItem
        Dim mnuItem As New ToolStripMenuItem
        mnuItem.Image = New Bitmap(My.Resources.icon)
        mnuItem.Tag = mHost
        mnuItem.Text = Master.eLang.GetString(1732, "Update Library")
        AddHandler mnuItem.Click, AddressOf mnuHostRefreshLibrary_Click
        Return mnuItem
    End Function

    Private Function CreateCheckConnectionMenuItem(ByVal mHost As Host) As ToolStripMenuItem
        Dim mnuItem As New ToolStripMenuItem
        mnuItem.Image = New Bitmap(My.Resources.icon)
        mnuItem.Tag = mHost
        mnuItem.Text = Master.eLang.GetString(1722, "Check Connection")
        AddHandler mnuItem.Click, AddressOf mnuHostCheckConnection_Click
        Return mnuItem
    End Function

    ''' <summary>
    ''' Schedule a debounced library refresh - a batch import should only result
    ''' in a single library scan.
    ''' </summary>
    Private Sub ScheduleLibraryRefresh()
        If Not _Enabled Then Exit Sub
        If _SpecialSettings.Hosts Is Nothing OrElse _SpecialSettings.Hosts.Count = 0 Then Exit Sub

        'restart the countdown - a batch import should result in a single scan
        _refreshTimer.Change(RefreshDelayMilliseconds, System.Threading.Timeout.Infinite)
    End Sub

    ''' <summary>
    ''' Callback of the debounce timer - runs on a threadpool thread, the refresh
    ''' itself is done asynchronously and never blocks.
    ''' </summary>
    Private Sub OnLibraryRefreshTimer(ByVal state As Object)
        Try
            Dim tRefresh As Task = RefreshLibraryAsync(Nothing)
            If tRefresh IsNot Nothing Then
                'observe a potential unhandled exception of the fire and forget call
                tRefresh.ContinueWith(Sub(t) logger.Error(t.Exception, New StackFrame().GetMethod().Name),
                                      TaskContinuationOptions.OnlyOnFaulted)
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    ''' <summary>
    ''' Trigger a library refresh on the given host (or on all enabled hosts).
    ''' </summary>
    Private Async Function RefreshLibraryAsync(ByVal mHost As Host) As Task
        Dim hosts As New List(Of Host)

        Try
            If mHost Is Nothing Then
                If _SpecialSettings.Hosts IsNot Nothing Then
                    For Each h As Host In _SpecialSettings.Hosts
                        If h.Enabled Then hosts.Add(h)
                    Next
                End If
            Else
                hosts.Add(mHost)
            End If

            If hosts.Count = 0 Then Exit Function

            _busy = True
            ChangeTaskManagerStatus(lblTaskManagerStatus, Master.eLang.GetString(1725, "Update Jellyfin Library"))
            ChangeTaskManagerProgressBar(tspTaskManager, ProgressBarStyle.Marquee)

            Dim getError As Boolean = False

            For Each h As Host In hosts
                Dim result As clsAPIJellyfin.JellyfinResult = Await RefreshLibraryForHostAsync(h)
                If Not result.Success Then
                    getError = True
                    logger.Error(String.Format("Library refresh failed for host '{0}': {1}", h.Name, result.Message))
                End If
            Next

            _busy = False
            ChangeTaskManagerProgressBar(tspTaskManager, ProgressBarStyle.Continuous)
            ChangeTaskManagerStatus(lblTaskManagerStatus, Master.eLang.GetString(1731, "No Pending Tasks"))

            If _SpecialSettings.SendNotifications Then
                If getError Then
                    ShowNotification(Notifications.Type.Error,
                                     Master.eLang.GetString(1718, "Jellyfin Interface"),
                                     Master.eLang.GetString(1734, "Library refresh failed."))
                Else
                    ShowNotification(Notifications.Type.Information,
                                     Master.eLang.GetString(1718, "Jellyfin Interface"),
                                     Master.eLang.GetString(1733, "Library refresh triggered."))
                End If
            End If
        Catch ex As Exception
            _busy = False
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Function

    ''' <summary>
    ''' Triggers a library refresh on the given host. https is tried first, http is
    ''' used as a fallback for servers without TLS.
    ''' </summary>
    Private Async Function RefreshLibraryForHostAsync(ByVal mHost As Host) As Task(Of clsAPIJellyfin.JellyfinResult)
        Dim result As New clsAPIJellyfin.JellyfinResult

        If mHost Is Nothing Then
            result.SetError("No host given.")
            Return result
        End If

        Dim sApiKey As String = mHost.ApiKey
        Dim sLastUrl As String = String.Empty

        For Each sUrl As String In GetCandidateUrls(mHost)
            sLastUrl = sUrl
            result = Await _api.RefreshLibraryAsync(sUrl, sApiKey)

            If result.Success Then
                RememberScheme(mHost, sUrl)
                WarnIfInsecure(mHost, sUrl)
                Return result
            End If
        Next

        'no scheme worked - if the last try used http, the user should know that the api key was not encrypted
        WarnIfInsecure(mHost, sLastUrl)

        Return result
    End Function

    ''' <summary>
    ''' Test the connection to the given host
    ''' </summary>
    Private Async Function CheckConnectionAsync(ByVal mHost As Host) As Task
        Try
            If mHost Is Nothing Then Exit Function

            Dim sApiKey As String = mHost.ApiKey
            Dim info As clsAPIJellyfin.JellyfinServerInfo = Nothing
            Dim sLastUrl As String = String.Empty

            For Each sUrl As String In GetCandidateUrls(mHost)
                sLastUrl = sUrl
                info = Await _api.GetServerInfoAsync(sUrl, sApiKey)

                If info IsNot Nothing Then
                    RememberScheme(mHost, sUrl)
                    Exit For
                End If
            Next

            WarnIfInsecure(mHost, sLastUrl)

            If info Is Nothing Then
                ShowNotification(Notifications.Type.Error,
                                 Master.eLang.GetString(1718, "Jellyfin Interface"),
                                 Master.eLang.GetString(1724, "There was a problem communicating with host."))
            Else
                ShowNotification(Notifications.Type.Information,
                                 Master.eLang.GetString(1718, "Jellyfin Interface"),
                                 String.Format("{0}{1}{2}",
                                               Master.eLang.GetString(1723, "Connection to host successful!"),
                                               Environment.NewLine,
                                               info.ToString()))
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Function

    ''' <summary>
    ''' Shows a notification - marshalled to the UI thread, because this module
    ''' may run its work on a background thread.
    ''' </summary>
    Private Sub ShowNotification(ByVal nType As Notifications.Type, ByVal sTitle As String, ByVal sMessage As String)
        Dim ts As Control = TryCast(ModulesManager.Instance.RuntimeObjects.MainToolStrip, Control)

        If ts IsNot Nothing AndAlso ts.InvokeRequired Then
            Try
                ts.Invoke(New MethodInvoker(Sub() Notifications.NewNotification(nType, sTitle, sMessage)))
                Exit Sub
            Catch ex As Exception
                logger.Error(ex, New StackFrame().GetMethod().Name)
            End Try
        End If

        Notifications.NewNotification(nType, sTitle, sMessage)
    End Sub

    ''' <summary>
    ''' Normalizes a host url: trims whitespace and removes trailing slashes.
    ''' No scheme is added, so an url without a scheme can be resolved to https
    ''' (preferred) or http (fallback) at runtime.
    ''' </summary>
    Private Shared Function NormalizeHostUrl(ByVal sUrl As String) As String
        If String.IsNullOrWhiteSpace(sUrl) Then Return String.Empty

        Return sUrl.Trim().TrimEnd("/"c)
    End Function

    ''' <summary>
    ''' Returns the scheme ("http" or "https") of the given url, or an empty
    ''' string if the url does not contain one.
    ''' </summary>
    Private Shared Function GetUrlScheme(ByVal sUrl As String) As String
        If String.IsNullOrWhiteSpace(sUrl) Then Return String.Empty

        Dim s As String = sUrl.Trim()

        If s.StartsWith("https://", StringComparison.OrdinalIgnoreCase) Then Return "https"
        If s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) Then Return "http"

        Return String.Empty
    End Function

    ''' <summary>
    ''' Removes the scheme from the given url.
    ''' </summary>
    Private Shared Function StripUrlScheme(ByVal sUrl As String) As String
        If String.IsNullOrWhiteSpace(sUrl) Then Return String.Empty

        Dim s As String = sUrl.Trim()

        If s.StartsWith("https://", StringComparison.OrdinalIgnoreCase) Then
            Return s.Substring("https://".Length)
        ElseIf s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) Then
            Return s.Substring("http://".Length)
        End If

        Return s
    End Function

    ''' <summary>
    ''' Returns the urls to try for the given host. https is preferred, http is
    ''' used as a fallback for servers that do not support TLS (e.g. in a LAN).
    ''' The scheme that worked is remembered, so a failing scheme is not tried
    ''' again on every call.
    ''' </summary>
    Private Function GetCandidateUrls(ByVal mHost As Host) As List(Of String)
        Dim result As New List(Of String)

        If mHost Is Nothing Then Return result

        Dim sRest As String = StripUrlScheme(If(mHost.Url, String.Empty))
        If sRest.Length = 0 Then Return result

        Dim sHttps As String = String.Concat("https://", sRest).TrimEnd("/"c)
        Dim sHttp As String = String.Concat("http://", sRest).TrimEnd("/"c)

        If GetCachedScheme(sRest) = "http" Then
            result.Add(sHttp)
            result.Add(sHttps)
        Else
            result.Add(sHttps)
            result.Add(sHttp)
        End If

        Return result
    End Function

    ''' <summary>
    ''' Returns the scheme that worked for the given host url (without scheme).
    ''' </summary>
    Private Function GetCachedScheme(ByVal sRest As String) As String
        If String.IsNullOrWhiteSpace(sRest) Then Return String.Empty
        If _hostSchemes.ContainsKey(sRest) Then Return _hostSchemes(sRest)

        Return String.Empty
    End Function

    ''' <summary>
    ''' Remembers the scheme that worked for the given host.
    ''' </summary>
    Private Sub RememberScheme(ByVal mHost As Host, ByVal sUsedUrl As String)
        If mHost Is Nothing Then Exit Sub

        Dim sRest As String = StripUrlScheme(If(mHost.Url, String.Empty))
        If sRest.Length = 0 Then Exit Sub

        Dim sScheme As String = GetUrlScheme(sUsedUrl)
        If sScheme.Length = 0 Then Exit Sub

        _hostSchemes(sRest) = sScheme
    End Sub

    ''' <summary>
    ''' Warns the user (once per host) if the connection to the host is not
    ''' encrypted - the api key is then transferred as plain text.
    ''' </summary>
    Private Sub WarnIfInsecure(ByVal mHost As Host, ByVal sUsedUrl As String)
        If mHost Is Nothing Then Exit Sub
        If GetUrlScheme(sUsedUrl) <> "http" Then Exit Sub

        Dim sKey As String = String.Concat(mHost.Name, "|", StripUrlScheme(If(mHost.Url, String.Empty)))
        If _warnedInsecureHosts.Contains(sKey) Then Exit Sub

        _warnedInsecureHosts.Add(sKey)

        ShowNotification(Notifications.Type.Warning,
                         Master.eLang.GetString(1718, "Jellyfin Interface"),
                         String.Format(Master.eLang.GetString(1771, "The connection to host '{0}' is not encrypted (http://). The api key is transferred unencrypted."), mHost.Name))
    End Sub

    ''' <summary>
    ''' Encrypts the given api key with dpapi (current user), so that it is not
    ''' stored as plain text in the settings file.
    ''' </summary>
    Private Shared Function ProtectApiKey(ByVal sValue As String) As String
        If String.IsNullOrEmpty(sValue) Then Return String.Empty
        If sValue.StartsWith(EncryptedApiKeyPrefix, StringComparison.OrdinalIgnoreCase) Then Return sValue

        Try
            Dim tProtectedData As Type = GetProtectedDataType()
            Dim tScope As Type = GetDataProtectionScopeType()

            If tProtectedData Is Nothing OrElse tScope Is Nothing Then
                logger.Error("dpapi (System.Security.Cryptography.ProtectedData) is not available - the api key cannot be encrypted.")
                Return sValue
            End If

            Dim mProtect As MethodInfo = tProtectedData.GetMethod("Protect", New Type() {GetType(Byte()), GetType(Byte()), tScope})
            If mProtect Is Nothing Then
                logger.Error("dpapi (System.Security.Cryptography.ProtectedData.Protect) is not available - the api key cannot be encrypted.")
                Return sValue
            End If

            Dim objScope As Object = [Enum].Parse(tScope, "CurrentUser")
            Dim objData As Object = mProtect.Invoke(Nothing, New Object() {Encoding.UTF8.GetBytes(sValue), ApiKeyEntropy, objScope})
            Dim bytes As Byte() = DirectCast(objData, Byte())

            Return String.Concat(EncryptedApiKeyPrefix, Convert.ToBase64String(bytes))
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return sValue
        End Try
    End Function

    ''' <summary>
    ''' Decrypts an api key that was encrypted with dpapi. Values that are not
    ''' encrypted (older settings files) are returned unchanged.
    ''' </summary>
    Private Shared Function UnprotectApiKey(ByVal sValue As String) As String
        If String.IsNullOrEmpty(sValue) Then Return String.Empty
        If Not sValue.StartsWith(EncryptedApiKeyPrefix, StringComparison.OrdinalIgnoreCase) Then Return sValue

        Try
            Dim tProtectedData As Type = GetProtectedDataType()
            Dim tScope As Type = GetDataProtectionScopeType()

            If tProtectedData Is Nothing OrElse tScope Is Nothing Then
                logger.Error("dpapi (System.Security.Cryptography.ProtectedData) is not available - the encrypted api key cannot be read.")
                Return sValue
            End If

            Dim mUnprotect As MethodInfo = tProtectedData.GetMethod("Unprotect", New Type() {GetType(Byte()), GetType(Byte()), tScope})
            If mUnprotect Is Nothing Then
                logger.Error("dpapi (System.Security.Cryptography.ProtectedData.Unprotect) is not available - the encrypted api key cannot be read.")
                Return sValue
            End If

            Dim objScope As Object = [Enum].Parse(tScope, "CurrentUser")
            Dim bytes As Byte() = Convert.FromBase64String(sValue.Substring(EncryptedApiKeyPrefix.Length))
            Dim objData As Object = mUnprotect.Invoke(Nothing, New Object() {bytes, ApiKeyEntropy, objScope})

            Return Encoding.UTF8.GetString(DirectCast(objData, Byte()))
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return sValue
        End Try
    End Function

    ''' <summary>
    ''' Returns the type of System.Security.Cryptography.ProtectedData. The type
    ''' is loaded dynamically, because the project does not reference the
    ''' assembly that contains it.
    ''' </summary>
    Private Shared Function GetProtectedDataType() As Type
        Return GetTypeFromAssemblies("System.Security.Cryptography.ProtectedData")
    End Function

    ''' <summary>
    ''' Returns the type of System.Security.Cryptography.DataProtectionScope.
    ''' </summary>
    Private Shared Function GetDataProtectionScopeType() As Type
        Return GetTypeFromAssemblies("System.Security.Cryptography.DataProtectionScope")
    End Function

    ''' <summary>
    ''' Looks up the given type in the assemblies that can contain the dpapi
    ''' types - the assembly depends on the used runtime.
    ''' </summary>
    Private Shared Function GetTypeFromAssemblies(ByVal sTypeName As String) As Type
        Try
            Dim t As Type = Type.GetType(String.Concat(sTypeName, ", System.Security, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"))
            If t Is Nothing Then t = Type.GetType(String.Concat(sTypeName, ", System.Security.Cryptography.ProtectedData"))
            If t Is Nothing Then t = Type.GetType(String.Concat(sTypeName, ", System.Security"))
            If t Is Nothing Then t = Type.GetType(String.Concat(sTypeName, ", mscorlib"))

            Return t
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return Nothing
        End Try
    End Function

    Private Async Sub mnuHostRefreshLibrary_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim mHost As Host = DirectCast(DirectCast(sender, ToolStripMenuItem).Tag, Host)
        If mHost Is Nothing Then Exit Sub
        Await RefreshLibraryAsync(mHost)
    End Sub

    Private Async Sub mnuHostCheckConnection_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim mHost As Host = DirectCast(DirectCast(sender, ToolStripMenuItem).Tag, Host)
        If mHost Is Nothing Then Exit Sub
        Await CheckConnectionAsync(mHost)
    End Sub

    Public Sub AddToolStripItem(control As ToolStripMenuItem, value As ToolStripItem)
        If control.Owner IsNot Nothing Then
            If control.Owner.InvokeRequired Then
                control.Owner.Invoke(New Delegate_AddToolStripMenuItem(AddressOf AddToolStripItem), New Object() {control, value})
            Else
                control.DropDownItems.Add(value)
            End If
        End If
    End Sub

    Public Sub AddToolStripItem(control As ToolStrip, value As ToolStripItem)
        If control.InvokeRequired Then
            control.Invoke(New Delegate_SetToolStripItem(AddressOf AddToolStripItem), New Object() {control, value})
        Else
            control.Items.Add(value)
        End If
    End Sub

    Public Sub ChangeTaskManagerStatus(control As ToolStripLabel, value As String)
        If control.Owner IsNot Nothing Then
            If control.Owner.InvokeRequired Then
                control.Owner.BeginInvoke(New Delegate_ChangeTaskManagerStatus(AddressOf ChangeTaskManagerStatus), New Object() {control, value})
            Else
                control.Text = value
            End If
        End If
    End Sub

    Public Sub ChangeTaskManagerProgressBar(control As ToolStripProgressBar, value As ProgressBarStyle)
        If control.Owner IsNot Nothing Then
            If control.Owner.InvokeRequired Then
                control.Owner.BeginInvoke(New Delegate_ChangeTaskManagerProgressBar(AddressOf ChangeTaskManagerProgressBar), New Object() {control, value})
            Else
                control.Style = value
            End If
        End If
    End Sub

    Private Sub Handle_ModuleSettingsChanged()
        RaiseEvent ModuleSettingsChanged()
    End Sub

    Private Sub Handle_ModuleEnabledChanged(ByVal State As Boolean)
        RaiseEvent ModuleSetupChanged(_Name, State, 0)
    End Sub

#End Region 'Methods

#Region "Nested Types"

    ''' <summary>
    ''' Module settings, stored in Interface.Jellyfin.xml
    ''' </summary>
    <Serializable()>
    <XmlRoot("interface.jellyfin")>
    Class SpecialSettings

#Region "Fields"

        Private _hosts As New List(Of Host)
        Private _sendnotifications As Boolean

#End Region 'Fields

#Region "Properties"

        <XmlElement("host")>
        Public Property Hosts() As List(Of Host)
            Get
                Return _hosts
            End Get
            Set(ByVal value As List(Of Host))
                _hosts = value
            End Set
        End Property

        <XmlElement("sendnotifications")>
        Public Property SendNotifications() As Boolean
            Get
                Return _sendnotifications
            End Get
            Set(ByVal value As Boolean)
                _sendnotifications = value
            End Set
        End Property

#End Region 'Properties

#Region "Constructors"

        Public Sub New()
            Clear()
        End Sub

#End Region 'Constructors

#Region "Methods"

        Public Sub Clear()
            If _hosts Is Nothing Then _hosts = New List(Of Host)
            _hosts.Clear()
            _sendnotifications = False
        End Sub

#End Region 'Methods'

    End Class

    ''' <summary>
    ''' A single Jellyfin server
    ''' </summary>
    <Serializable()>
    Class Host

#Region "Fields"

        Private _name As String
        Private _url As String
        Private _apikey As String
        Private _enabled As Boolean

#End Region 'Fields

#Region "Properties"

        <XmlElement("name")>
        Public Property Name() As String
            Get
                Return _name
            End Get
            Set(ByVal value As String)
                _name = value
            End Set
        End Property

        <XmlElement("url")>
        Public Property Url() As String
            Get
                Return _url
            End Get
            Set(ByVal value As String)
                _url = value
            End Set
        End Property

        ''' <summary>
        ''' The api key of the user. This is the plain text key that is used for
        ''' the api calls - the encrypted value is written to the settings file
        ''' (see ApiKeyEncrypted).
        ''' </summary>
        <XmlIgnore()>
        Public Property ApiKey() As String
            Get
                Return UnprotectApiKey(_apikey)
            End Get
            Set(ByVal value As String)
                _apikey = value
            End Set
        End Property

        ''' <summary>
        ''' The api key as it is written to the settings file - encrypted with
        ''' dpapi. Settings files written by older versions contain the plain
        ''' text key, that is encrypted with the next save of the settings.
        ''' </summary>
        <XmlElement("apikey")>
        Public Property ApiKeyEncrypted() As String
            Get
                Return ProtectApiKey(_apikey)
            End Get
            Set(ByVal value As String)
                _apikey = value
            End Set
        End Property

        ''' <summary>
        ''' True if the api key is not encrypted (settings file of an older version).
        ''' </summary>
        Public Function HasPlaintextApiKey() As Boolean
            If String.IsNullOrEmpty(_apikey) Then Return False

            Return Not _apikey.StartsWith(EncryptedApiKeyPrefix, StringComparison.OrdinalIgnoreCase)
        End Function

        <XmlElement("enabled")>
        Public Property Enabled() As Boolean
            Get
                Return _enabled
            End Get
            Set(ByVal value As Boolean)
                _enabled = value
            End Set
        End Property

#End Region 'Properties

#Region "Constructors"

        Public Sub New()
            Clear()
        End Sub

#End Region 'Constructors

#Region "Methods"

        Public Sub Clear()
            _name = "New Host"
            _url = String.Empty
            _apikey = String.Empty
            _enabled = True
        End Sub

#End Region 'Methods'

    End Class

#End Region 'Nested Types"

End Class
