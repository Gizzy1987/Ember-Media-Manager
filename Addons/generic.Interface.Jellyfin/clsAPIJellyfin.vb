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

Imports NLog
Imports Newtonsoft.Json.Linq
Imports System.Net
Imports System.Net.Http
Imports System.Security.Cryptography
Imports System.Text
Imports System.Threading.Tasks

''' <summary>
''' Thin wrapper around the Jellyfin HTTP API.
''' All calls are executed asynchronously with a hard timeout, errors are never
''' thrown to the caller but returned as part of the result.
''' </summary>
Public Class clsAPIJellyfin

    Shared logger As Logger = LogManager.GetCurrentClassLogger()

    ''' <summary>
    ''' Timeout (in seconds) for every single API call.
    ''' </summary>
    Public Const TimeoutSeconds As Integer = 15

    ''' <summary>
    ''' Version reported to the Jellyfin server.
    ''' </summary>
    Public Const ClientVersion As String = "1.12.0.0"

#Region "Nested Types"

    ''' <summary>
    ''' Basic information about a Jellyfin server (result of /System/Info).
    ''' </summary>
    Public Class JellyfinServerInfo

        Public Property ServerName() As String = String.Empty
        Public Property Version() As String = String.Empty
        Public Property Id() As String = String.Empty

        Public Overrides Function ToString() As String
            Return String.Format("{0} ({1})", ServerName, Version)
        End Function

    End Class

    ''' <summary>
    ''' Result of an API call: success flag plus a human readable message.
    ''' </summary>
    Public Class JellyfinResult

        Public Property Success() As Boolean = False
        Public Property Message() As String = String.Empty

        Public Sub SetError(ByVal message As String)
            Success = False
            Message = message
        End Sub

        Public Sub SetSuccess(ByVal message As String)
            Success = True
            Message = message
        End Sub

    End Class

#End Region 'Nested Types

#Region "Methods"

    ''' <summary>
    ''' Normalizes a user supplied base url: trims whitespace, adds a missing
    ''' scheme (http://) and removes trailing slashes.
    ''' </summary>
    ''' <param name="url">raw url as entered by the user</param>
    Public Shared Function NormalizeBaseUrl(ByVal url As String) As String
        If String.IsNullOrWhiteSpace(url) Then Return String.Empty

        Dim s As String = url.Trim()

        If Not s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) AndAlso
           Not s.StartsWith("https://", StringComparison.OrdinalIgnoreCase) Then
            s = "http://" & s
        End If

        Return s.TrimEnd("/"c)
    End Function

    ''' <summary>
    ''' Checks if a normalized base url is a valid http/https url.
    ''' </summary>
    Public Shared Function IsValidBaseUrl(ByVal normalizedUrl As String) As Boolean
        If String.IsNullOrWhiteSpace(normalizedUrl) Then Return False

        Dim uri As Uri = Nothing
        If Not Uri.TryCreate(normalizedUrl, UriKind.Absolute, uri) Then Return False

        Return uri.Scheme = Uri.UriSchemeHttp OrElse uri.Scheme = Uri.UriSchemeHttps
    End Function

    ''' <summary>
    ''' Tests the connection to a Jellyfin server and returns its system information.
    ''' </summary>
    ''' <param name="baseUrl">normalized base url of the server</param>
    ''' <param name="apiKey">API key of the user</param>
    ''' <returns>server information or Nothing if the host is not reachable</returns>
    Public Async Function GetServerInfoAsync(ByVal baseUrl As String, ByVal apiKey As String) As Task(Of JellyfinServerInfo)
        Dim info As New JellyfinServerInfo

        Try
            Using client As New HttpClient
                client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds)

                Using request As New HttpRequestMessage(HttpMethod.Get, String.Format("{0}/System/Info", baseUrl))
                    ApplyAuthHeaders(request, apiKey)

                    Using response As HttpResponseMessage = Await client.SendAsync(request)
                        response.EnsureSuccessStatusCode()
                        Dim content As String = Await response.Content.ReadAsStringAsync()

                        Dim jo As JObject = JObject.Parse(content)
                        info.ServerName = jo.Value(Of String)("ServerName")
                        info.Version = jo.Value(Of String)("Version")
                        info.Id = jo.Value(Of String)("Id")

                        If String.IsNullOrEmpty(info.ServerName) Then info.ServerName = String.Empty
                        If String.IsNullOrEmpty(info.Version) Then info.Version = String.Empty
                        If String.IsNullOrEmpty(info.Id) Then info.Id = String.Empty
                    End Using
                End Using
            End Using
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return Nothing
        End Try

        Return info
    End Function

    ''' <summary>
    ''' Triggers a library scan on the Jellyfin server.
    ''' </summary>
    ''' <param name="baseUrl">normalized base url of the server</param>
    ''' <param name="apiKey">API key of the user</param>
    Public Async Function RefreshLibraryAsync(ByVal baseUrl As String, ByVal apiKey As String) As Task(Of JellyfinResult)
        Dim result As New JellyfinResult

        Try
            Using client As New HttpClient
                client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds)

                Using request As New HttpRequestMessage(HttpMethod.Post, String.Format("{0}/Library/Refresh", baseUrl))
                    ApplyAuthHeaders(request, apiKey)

                    Using response As HttpResponseMessage = Await client.SendAsync(request)
                        If response.IsSuccessStatusCode Then
                            result.SetSuccess(String.Format("HTTP {0}", CInt(response.StatusCode)))
                        Else
                            result.SetError(String.Format("HTTP {0}", CInt(response.StatusCode)))
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            result.SetError(ex.Message)
        End Try

        Return result
    End Function

    ''' <summary>
    ''' Reads the virtual folders (libraries) configured on the Jellyfin server.
    ''' </summary>
    ''' <param name="baseUrl">normalized base url of the server</param>
    ''' <param name="apiKey">API key of the user</param>
    ''' <returns>list of library names, empty list on error</returns>
    Public Async Function GetVirtualFoldersAsync(ByVal baseUrl As String, ByVal apiKey As String) As Task(Of List(Of String))
        Dim result As New List(Of String)

        Try
            Using client As New HttpClient
                client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds)

                Using request As New HttpRequestMessage(HttpMethod.Get, String.Format("{0}/Library/VirtualFolders", baseUrl))
                    ApplyAuthHeaders(request, apiKey)

                    Using response As HttpResponseMessage = Await client.SendAsync(request)
                        response.EnsureSuccessStatusCode()
                        Dim content As String = Await response.Content.ReadAsStringAsync()

                        Dim arr As JArray = JArray.Parse(content)
                        For Each item As JObject In arr
                            Dim sName As String = item.Value(Of String)("Name")
                            Dim sLocations As String = String.Empty
                            Dim locations As JArray = TryCast(item("Locations"), JArray)
                            If locations IsNot Nothing Then
                                For Each loc As JToken In locations
                                    If sLocations.Length > 0 Then sLocations &= ", "
                                    sLocations &= loc.ToString()
                                Next
                            End If
                            If String.IsNullOrEmpty(sName) Then sName = String.Empty
                            If String.IsNullOrEmpty(sLocations) Then
                                result.Add(sName)
                            Else
                                result.Add(String.Format("{0} ({1})", sName, sLocations))
                            End If
                        Next
                    End Using
                End Using
            End Using
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return New List(Of String)
        End Try

        Return result
    End Function

    ''' <summary>
    ''' Adds the authentication headers to a request.
    ''' </summary>
    Private Shared Sub ApplyAuthHeaders(ByVal request As HttpRequestMessage, ByVal apiKey As String)
        If request Is Nothing Then Exit Sub
        If String.IsNullOrWhiteSpace(apiKey) Then Exit Sub

        request.Headers.TryAddWithoutValidation("X-Emby-Token", apiKey)
        request.Headers.TryAddWithoutValidation("X-Emby-Authorization",
                                                String.Format("MediaBrowser Client=""Ember"", Device=""Ember"", DeviceId=""{0}"", Version=""{1}"", Token=""{2}""",
                                                              GetDeviceId(),
                                                              ClientVersion,
                                                              apiKey))
    End Sub

    ''' <summary>
    ''' Stable device id, derived from the machine and user name.
    ''' </summary>
    Private Shared Function GetDeviceId() As String
        If _deviceId.Length > 0 Then Return _deviceId

        Dim input As String = String.Format("{0}|{1}|Ember", Environment.MachineName, Environment.UserName)

        Using sha As SHA256 = SHA256.Create()
            Dim hash As Byte() = sha.ComputeHash(Encoding.UTF8.GetBytes(input))
            Dim sb As New StringBuilder()
            For Each b As Byte In hash
                sb.Append(b.ToString("x2"))
            Next
            _deviceId = sb.ToString()
        End Using

        Return _deviceId
    End Function

#End Region 'Methods'

#Region "Fields"

    Private Shared _deviceId As String = String.Empty

#End Region 'Fields

End Class
