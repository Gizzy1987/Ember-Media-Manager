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


Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports System.Threading
Imports NLog

Public Class Notifications

#Region "Events"

    Public Shared Event ShowNotification(ByVal timeout As Integer, ByVal title As String, ByVal message As String, ByVal icon As Windows.Forms.ToolTipIcon)

    ''' <summary>
    ''' Raised whenever the state of the collected routine notifications changes.
    ''' The text is meant for the status bar of the main form, e.g. "New episodes added: 12".
    ''' </summary>
    Public Shared Event CollectionStatusChanged(ByVal status As String)

#End Region 'Events

#Region "Enumerations"

    Public Enum Type As Integer
        Added_Movie
        Added_Movieset
        Added_TVEpisode
        Added_TVShow
        [Error]
        Information
        Scraped_Movie
        Scraped_Movieset
        Scraped_TVEpisode
        Scraped_TVSeason
        Scraped_TVShow
        Warning
    End Enum

#End Region 'Enumerations

#Region "Constants"

    'Quiet period (in ms) after which the collected routine notifications are shown as one single message.
    Private Const CollectionDelay As Integer = 3000

    'Timeout of the collected summary notification
    Private Const SummaryTimeout As Integer = 5000

    'Titles of the summary notification
    Private Const TitleAdded As UInteger = 1737
    Private Const TitleUpdated As UInteger = 1738
    Private Const TitleAddedAndUpdated As UInteger = 1739

    'Verbs of the summary notification
    Private Const VerbAdded As UInteger = 1740
    Private Const VerbScraped As UInteger = 1741

    'Nouns (singular/plural) of the added notifications
    Private Const NameAddedMovie As UInteger = 1742
    Private Const NameAddedMovies As UInteger = 1743
    Private Const NameAddedMovieSet As UInteger = 1744
    Private Const NameAddedMovieSets As UInteger = 1745
    Private Const NameAddedEpisode As UInteger = 1746
    Private Const NameAddedEpisodes As UInteger = 1747
    Private Const NameAddedTVShow As UInteger = 1748
    Private Const NameAddedTVShows As UInteger = 1749

    'Nouns (singular/plural) of the scraped notifications
    Private Const NameScrapedMovie As UInteger = 1750
    Private Const NameScrapedMovies As UInteger = 1751
    Private Const NameScrapedMovieSet As UInteger = 1752
    Private Const NameScrapedMovieSets As UInteger = 1753
    Private Const NameScrapedEpisode As UInteger = 1754
    Private Const NameScrapedEpisodes As UInteger = 1755
    Private Const NameScrapedSeason As UInteger = 1756
    Private Const NameScrapedSeasons As UInteger = 1757
    Private Const NameScrapedTVShow As UInteger = 1758
    Private Const NameScrapedTVShows As UInteger = 1759

#End Region 'Constants'

#Region "Fields"

    Shared logger As Logger = LogManager.GetCurrentClassLogger()

    Private Shared ReadOnly mBufferLock As New Object()
    Private Shared mBuffer As New List(Of CollectedNotification)()
    Private Shared mTimer As System.Threading.Timer

#End Region 'Fields'

#Region "Methods"

    Private Shared Function IsEnabled(ByVal type As Type) As Boolean
        With Master.eSettings
            Select Case type
                Case Type.Added_Movie
                    Return .GeneralNotificationAddedMovie
                Case Type.Added_Movieset
                    Return .GeneralNotificationAddedMovieset
                Case Type.Added_TVEpisode
                    Return .GeneralNotificationAddedTVEpisode
                Case Type.Added_TVShow
                    Return .GeneralNotificationAddedTVShow
                Case Type.Error
                    Return .GeneralNotificationError
                Case Type.Information
                    Return .GeneralNotificationInformation
                Case Type.Scraped_Movie
                    Return .GeneralNotificationScrapedMovie
                Case Type.Scraped_Movieset
                    Return .GeneralNotificationScrapedMovieset
                Case Type.Scraped_TVEpisode
                    Return .GeneralNotificationScrapedTVEpisode
                Case Type.Scraped_TVSeason
                    Return .GeneralNotificationScrapedTVSeason
                Case Type.Scraped_TVShow
                    Return .GeneralNotificationScrapedTVShow
                Case Type.Warning
                    Return .GeneralNotificationWarning
                Case Else
                    Return False
            End Select
        End With
    End Function

    Public Shared Sub NewNotification(ByVal [type] As Type, ByVal message As String)
        NewNotification(type, String.Empty, message)
    End Sub

    Public Shared Sub NewNotification(ByVal [type] As Type, ByVal title As String, ByVal message As String)
        If Not IsEnabled(type) Then Exit Sub

        'Routine messages are collected and shown as one single message
        If IsRoutine(type) Then
            CollectNotification(type, title, message)
            Return
        End If

        'Errors, warnings and information are shown immediately
        Dim iTimeout As Integer = 5000
        Dim nIcon As Windows.Forms.ToolTipIcon = Windows.Forms.ToolTipIcon.Info
        Dim strTitle As String = title

        Select Case type
            Case Type.Error
                iTimeout = 15000
                nIcon = Windows.Forms.ToolTipIcon.Error
            Case Type.Information
            Case Type.Warning
                iTimeout = 15000
                nIcon = Windows.Forms.ToolTipIcon.Warning
        End Select

        RaiseEvent ShowNotification(iTimeout, strTitle, message, nIcon)
    End Sub

    ''' <summary>
    ''' Shows the collected routine notifications immediately as one single message.
    ''' Use this at the end of a scan or scrape run instead of waiting for the quiet period.
    ''' </summary>
    Public Shared Sub FlushNotifications()
        Dim lCounts As New Dictionary(Of Type, Integer)()

        SyncLock (mBufferLock)
            If mTimer IsNot Nothing Then mTimer.Change(Timeout.Infinite, Timeout.Infinite)

            If mBuffer.Count = 0 Then Return

            lCounts = GetCounts()
            mBuffer.Clear()
        End SyncLock

        RaiseEvent ShowNotification(SummaryTimeout, GetSummaryTitle(lCounts), GetSummaryMessage(lCounts), Windows.Forms.ToolTipIcon.Info)
        RaiseEvent CollectionStatusChanged(String.Empty)
    End Sub

    Private Shared Sub CollectNotification(ByVal [type] As Type, ByVal title As String, ByVal message As String)
        Dim sStatus As String = String.Empty

        SyncLock (mBufferLock)
            mBuffer.Add(New CollectedNotification With {.NotificationType = [type], .Title = title, .Message = message})

            If mTimer Is Nothing Then
                mTimer = New System.Threading.Timer(AddressOf OnCollectionTimeout, Nothing, Timeout.Infinite, Timeout.Infinite)
            End If

            'Restart the quiet period - the summary is shown once nothing happens anymore
            mTimer.Change(CollectionDelay, Timeout.Infinite)

            sStatus = GetStatusText([type], GetCount([type]))
        End SyncLock

        RaiseEvent CollectionStatusChanged(sStatus)
    End Sub

    Private Shared Sub OnCollectionTimeout(ByVal state As Object)
        Try
            FlushNotifications()
        Catch ex As Exception
            logger.Error(ex, "Notifications.OnCollectionTimeout")
        End Try
    End Sub

    Private Shared Function GetCounts() As Dictionary(Of Type, Integer)
        Dim lCounts As New Dictionary(Of Type, Integer)()

        For Each lItem As CollectedNotification In mBuffer
            If lCounts.ContainsKey(lItem.NotificationType) Then
                lCounts(lItem.NotificationType) += 1
            Else
                lCounts.Add(lItem.NotificationType, 1)
            End If
        Next

        Return lCounts
    End Function

    Private Shared Function GetCount(ByVal [type] As Type) As Integer
        Dim lCount As Integer = 0

        For Each lItem As CollectedNotification In mBuffer
            If lItem.NotificationType = [type] Then lCount += 1
        Next

        Return lCount
    End Function

    Private Shared Function IsRoutine(ByVal [type] As Type) As Boolean
        Select Case [type]
            Case Type.Added_Movie,
                 Type.Added_Movieset,
                 Type.Added_TVEpisode,
                 Type.Added_TVShow,
                 Type.Scraped_Movie,
                 Type.Scraped_Movieset,
                 Type.Scraped_TVEpisode,
                 Type.Scraped_TVSeason,
                 Type.Scraped_TVShow
                Return True
            Case Else
                Return False
        End Select
    End Function

    Private Shared Function IsAdded(ByVal [type] As Type) As Boolean
        Select Case [type]
            Case Type.Added_Movie,
                 Type.Added_Movieset,
                 Type.Added_TVEpisode,
                 Type.Added_TVShow
                Return True
            Case Else
                Return False
        End Select
    End Function

    ''' <summary>
    ''' Title of the summary notification - for a single type the title describes the
    ''' collected items, otherwise a generic title is used.
    ''' </summary>
    Private Shared Function GetSummaryTitle(ByVal counts As Dictionary(Of Type, Integer)) As String
        If counts.Count = 1 Then
            Dim lType As Type = counts.Keys.First()
            Dim lCount As Integer = counts(lType)

            Return Capitalize(String.Concat(GetNameText(lType, lCount), " ", GetVerbText(lType)))
        End If

        Dim lAdded As Boolean = False
        Dim lScraped As Boolean = False

        For Each lType As Type In counts.Keys
            If IsAdded(lType) Then
                lAdded = True
            Else
                lScraped = True
            End If
        Next

        If lAdded AndAlso lScraped Then
            Return Master.eLang.GetString(TitleAddedAndUpdated, "Media added and updated")
        ElseIf lAdded Then
            Return Master.eLang.GetString(TitleAdded, "New media added")
        Else
            Return Master.eLang.GetString(TitleUpdated, "Media updated")
        End If
    End Function

    ''' <summary>
    ''' Message of the summary notification - one line per collected type.
    ''' </summary>
    Private Shared Function GetSummaryMessage(ByVal counts As Dictionary(Of Type, Integer)) As String
        Dim lParts As New List(Of String)()

        For Each lType As Type In counts.Keys.OrderBy(Function(f) CInt(f))
            Dim lCount As Integer = counts(lType)

            lParts.Add(String.Concat(lCount.ToString(), " ", GetNameText(lType, lCount), " ", GetVerbText(lType)))
        Next

        Return String.Join(", ", lParts)
    End Function

    ''' <summary>
    ''' Status bar text while the routine notifications are collected, e.g. "New episodes added: 12".
    ''' </summary>
    Private Shared Function GetStatusText(ByVal [type] As Type, ByVal count As Integer) As String
        Dim lText As String = String.Concat(GetNameText([type], count), " ", GetVerbText([type]))

        Return String.Concat(Capitalize(lText), ": ", count.ToString)
    End Function

    Private Shared Function GetNameText(ByVal [type] As Type, ByVal count As Integer) As String
        Dim lId As UInteger
        Dim lFallback As String

        Select Case [type]
            Case Type.Added_Movie
                If count = 1 Then
                    lId = NameAddedMovie
                    lFallback = "new movie"
                Else
                    lId = NameAddedMovies
                    lFallback = "new movies"
                End If
            Case Type.Added_Movieset
                If count = 1 Then
                    lId = NameAddedMovieSet
                    lFallback = "new movie set"
                Else
                    lId = NameAddedMovieSets
                    lFallback = "new movie sets"
                End If
            Case Type.Added_TVEpisode
                If count = 1 Then
                    lId = NameAddedEpisode
                    lFallback = "new episode"
                Else
                    lId = NameAddedEpisodes
                    lFallback = "new episodes"
                End If
            Case Type.Added_TVShow
                If count = 1 Then
                    lId = NameAddedTVShow
                    lFallback = "new TV show"
                Else
                    lId = NameAddedTVShows
                    lFallback = "new TV shows"
                End If
            Case Type.Scraped_Movie
                If count = 1 Then
                    lId = NameScrapedMovie
                    lFallback = "movie"
                Else
                    lId = NameScrapedMovies
                    lFallback = "movies"
                End If
            Case Type.Scraped_Movieset
                If count = 1 Then
                    lId = NameScrapedMovieSet
                    lFallback = "movie set"
                Else
                    lId = NameScrapedMovieSets
                    lFallback = "movie sets"
                End If
            Case Type.Scraped_TVEpisode
                If count = 1 Then
                    lId = NameScrapedEpisode
                    lFallback = "episode"
                Else
                    lId = NameScrapedEpisodes
                    lFallback = "episodes"
                End If
            Case Type.Scraped_TVSeason
                If count = 1 Then
                    lId = NameScrapedSeason
                    lFallback = "season"
                Else
                    lId = NameScrapedSeasons
                    lFallback = "seasons"
                End If
            Case Type.Scraped_TVShow
                If count = 1 Then
                    lId = NameScrapedTVShow
                    lFallback = "TV show"
                Else
                    lId = NameScrapedTVShows
                    lFallback = "TV shows"
                End If
            Case Else
                Return String.Empty
        End Select

        Return Master.eLang.GetString(lId, lFallback)
    End Function

    Private Shared Function GetVerbText(ByVal [type] As Type) As String
        If IsAdded([type]) Then
            Return Master.eLang.GetString(VerbAdded, "added")
        Else
            Return Master.eLang.GetString(VerbScraped, "scraped")
        End If
    End Function

    Private Shared Function Capitalize(ByVal text As String) As String
        If String.IsNullOrEmpty(text) Then Return text

        Dim lCulture As CultureInfo = CultureInfo.CurrentUICulture

        Return String.Concat(lCulture.TextInfo.ToTitleCase(text.Substring(0, 1).ToLower(lCulture)), text.Substring(1))
    End Function

#End Region 'Methods'

#Region "Nested Types"

    ''' <summary>
    ''' A collected routine notification (title and message of one hit).
    ''' </summary>
    Private Class CollectedNotification

        Public Property NotificationType As Type
        Public Property Title As String
        Public Property Message As String

    End Class

#End Region 'Nested Types'

End Class
