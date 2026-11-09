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

Imports System.Runtime.InteropServices
Imports NLog

''' <summary>
''' ITaskbarList3 (shobjidl_core) - used to set the explicit AppUserModelID of the
''' running process. Only the methods of the interface are declared, the vtable
''' order has to match the original COM interface exactly.
''' </summary>
Friend Interface ITaskbarList3

    'ITaskbarList
    <PreserveSig> Function HrInit() As Integer
    <PreserveSig> Function AddTab(ByVal hwnd As IntPtr) As Integer
    <PreserveSig> Function DeleteTab(ByVal hwnd As IntPtr) As Integer
    <PreserveSig> Function ActivateTab(ByVal hwnd As IntPtr) As Integer
    <PreserveSig> Function SetActiveAlt(ByVal hwnd As IntPtr) As Integer
    <PreserveSig> Function MarkFullscreenWindow(ByVal hwnd As IntPtr, ByVal fFullscreen As Boolean) As Integer

    'ITaskbarList2
    <PreserveSig> Function SetOverlayIcon(ByVal hwnd As IntPtr, ByVal hIcon As IntPtr, <MarshalAs(UnmanagedType.LPWStr)> ByVal pszDescription As String) As Integer
    <PreserveSig> Function SetThumbnailClip(ByVal hwnd As IntPtr, ByRef prcClip As IntPtr) As Integer
    <PreserveSig> Function SetThumbnailTooltip(ByVal hwnd As IntPtr, <MarshalAs(UnmanagedType.LPWStr)> ByVal pszTip As String) As Integer
    <PreserveSig> Function SetThumbnailImage(ByVal hwnd As IntPtr, ByVal hbm As IntPtr) As Integer
    <PreserveSig> Function SetProgressValue(ByVal hwnd As IntPtr, ByVal ullProgress As ULong, ByVal ullTotal As ULong) As Integer
    <PreserveSig> Function SetProgressState(ByVal hwnd As IntPtr, ByVal flags As Integer) As Integer

    'ITaskbarList3
    <PreserveSig> Function RegisterTab(ByVal hwndTab As IntPtr, ByVal pitem As IntPtr) As Integer
    <PreserveSig> Function UnregisterTab(ByVal hwnd As IntPtr) As Integer
    <PreserveSig> Function HrUnregisterTab(ByVal hwnd As IntPtr) As Integer
    <PreserveSig> Function SetTabOrder(ByVal hwndTab As IntPtr, ByVal hwndInsertBefore As IntPtr) As Integer
    <PreserveSig> Function SetTabActive(ByVal hwndTab As IntPtr, ByVal hwndActive As IntPtr, ByVal fLastActive As Boolean) As Integer
    <PreserveSig> Function ThumbBarAddButtons(ByVal hwnd As IntPtr, ByVal cButtons As UInteger, ByVal lpButtons As IntPtr) As Integer
    <PreserveSig> Function ThumbBarUpdateButton(ByVal hwnd As IntPtr, ByVal lpButton As IntPtr) As Integer
    <PreserveSig> Function ThumbBarSetImageList(ByVal hwnd As IntPtr, ByVal himl As IntPtr) As Integer
    <PreserveSig> Function SetOverlayIconEx(ByVal hwnd As IntPtr, ByVal hIcon As IntPtr, <MarshalAs(UnmanagedType.LPWStr)> ByVal pszDescription As String, ByVal fPreferGuiGuidelines As Boolean) As Integer
    <PreserveSig> Function SetCurrentProcessExplicitAppUserModelID(<MarshalAs(UnmanagedType.LPWStr)> ByVal appID As String) As Integer

End Interface

''' <summary>
''' Sets the explicit AppUserModelID (AUMID) of the running process.
'''
''' Without an AUMID, Windows shows the tray notifications of the application with
''' the placeholder "NotifyIconGeneratedAumid_..." as title.
'''
''' See: https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-itaskbarlist3
''' </summary>
Public Class AppUserModelId

#Region "Fields"

    Shared logger As Logger = LogManager.GetCurrentClassLogger()

#End Region 'Fields

#Region "Methods"

    ''' <summary>
    ''' Sets the process AppUserModelID to <see cref="DefaultAppUserModelId"/>.
    ''' This method never throws - the application has to start even if no AUMID
    ''' could be set.
    ''' </summary>
    Public Shared Sub Initialize()
        Try
            If SetAppUserModelId(DefaultAppUserModelId) Then
                logger.Info(String.Format("[AppUserModelId] Process AppUserModelID set to '{0}'.", DefaultAppUserModelId))
            Else
                logger.Warn("[AppUserModelId] Could not set the process AppUserModelID.")
            End If
        Catch ex As Exception
            logger.Error(ex, "AppUserModelId.Initialize")
        End Try
    End Sub

    ''' <summary>
    ''' Sets the explicit AppUserModelID of the current process via ITaskbarList3.
    ''' </summary>
    ''' <returns>True if the AUMID could be set.</returns>
    Public Shared Function SetAppUserModelId(ByVal appUserModelId As String) As Boolean
        If String.IsNullOrEmpty(appUserModelId) Then Return False

        Dim lPointer As IntPtr = IntPtr.Zero

        Try
            Dim lHResult As Integer = CoCreateInstance(New Guid(CLSID_TaskbarList), IntPtr.Zero, CLSCTX_INPROC_SERVER, New Guid(IID_ITaskbarList3), lPointer)

            If lHResult <> S_OK OrElse lPointer = IntPtr.Zero Then
                logger.Warn(String.Format("[AppUserModelId] CoCreateInstance(ITaskbarList3) failed with HRESULT 0x{0:X8}.", lHResult))
                Return SetAppUserModelIdFallback(appUserModelId)
            End If

            Dim lTaskbarList As ITaskbarList3 = TryCast(Marshal.GetObjectForIUnknown(lPointer), ITaskbarList3)

            If lTaskbarList Is Nothing Then
                logger.Warn("[AppUserModelId] Could not get the ITaskbarList3 interface.")
                Return SetAppUserModelIdFallback(appUserModelId)
            End If

            lHResult = lTaskbarList.SetCurrentProcessExplicitAppUserModelID(appUserModelId)

            If lHResult <> S_OK Then
                logger.Warn(String.Format("[AppUserModelId] SetCurrentProcessExplicitAppUserModelID failed with HRESULT 0x{0:X8}.", lHResult))
                Return SetAppUserModelIdFallback(appUserModelId)
            End If

            Return True
        Catch ex As Exception
            logger.Error(ex, "AppUserModelId.SetAppUserModelId")
            Return SetAppUserModelIdFallback(appUserModelId)
        Finally
            If lPointer <> IntPtr.Zero Then Marshal.Release(lPointer)
        End Try
    End Function

    ''' <summary>
    ''' Fallback for the case that the COM interface is not available - shell32.dll
    ''' exports the very same function.
    ''' </summary>
    Private Shared Function SetAppUserModelIdFallback(ByVal appUserModelId As String) As Boolean
        Try
            Dim lHResult As Integer = Shell32SetCurrentProcessExplicitAppUserModelID(appUserModelId)

            If lHResult = S_OK Then
                logger.Info("[AppUserModelId] Process AppUserModelID set via shell32.dll.")
                Return True
            End If

            logger.Warn(String.Format("[AppUserModelId] SetCurrentProcessExplicitAppUserModelID (shell32.dll) failed with HRESULT 0x{0:X8}.", lHResult))
            Return False
        Catch ex As Exception
            logger.Error(ex, "AppUserModelId.SetAppUserModelIdFallback")
            Return False
        End Try
    End Function

#End Region 'Methods'

#Region "Constants"

    ''' <summary>
    ''' The AppUserModelID of the application - used as title of the Windows notifications.
    ''' </summary>
    Public Const DefaultAppUserModelId As String = "Ember Media Manager for Jellyfin"

    'CLSID_TaskbarList = {56FDF344-FD6D-11d0-958A-006097C9A090}
    Private Const CLSID_TaskbarList As String = "56FDF344-FD6D-11D0-958A-006097C9A090"

    'IID_ITaskbarList3 = {ea1afb91-9e28-4f86-90e9-9e9f8a5eefaf}
    Private Const IID_ITaskbarList3 As String = "EA1AFB91-9E28-4F86-90E9-9E9F8A5EEFAF"

    Private Const CLSCTX_INPROC_SERVER As UInteger = 1
    Private Const S_OK As Integer = 0

#End Region 'Constants'

#Region "Imports"

    <DllImport("ole32.dll", ExactSpelling:=True, PreserveSig:=True)>
    Private Shared Function CoCreateInstance(ByVal rclsid As Guid, ByVal pUnkOuter As IntPtr, ByVal dwClsContext As UInteger, ByVal riid As Guid, ByRef ppv As IntPtr) As Integer
    End Function

    <DllImport("shell32.dll", EntryPoint:="SetCurrentProcessExplicitAppUserModelID", CharSet:=CharSet.Unicode, PreserveSig:=True)>
    Private Shared Function Shell32SetCurrentProcessExplicitAppUserModelID(<MarshalAs(UnmanagedType.LPWStr)> ByVal appID As String) As Integer
    End Function

#End Region 'Imports'

End Class
