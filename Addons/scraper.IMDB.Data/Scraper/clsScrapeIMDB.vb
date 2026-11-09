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
Imports HtmlAgilityPack
Imports NLog
Imports System.IO
Imports System.Text.RegularExpressions

Public Class SearchResults_Movie

#Region "Properties"

    Public Property ExactMatches() As New List(Of MediaContainers.Movie)

    Public Property PartialMatches() As New List(Of MediaContainers.Movie)

    Public Property PopularTitles() As New List(Of MediaContainers.Movie)

    Public Property TvTitles() As New List(Of MediaContainers.Movie)

    Public Property VideoTitles() As New List(Of MediaContainers.Movie)

    Public Property ShortTitles() As New List(Of MediaContainers.Movie)

#End Region 'Properties

End Class

Public Class SearchResults_TVShow

#Region "Properties"

    Public Property Matches() As New List(Of MediaContainers.TVShow)

#End Region 'Properties

End Class

Public Class Scraper

#Region "Fields"

    Shared logger As Logger = LogManager.GetCurrentClassLogger()

    Friend WithEvents bwIMDB As New ComponentModel.BackgroundWorker

    Private Const REGEX_Certifications As String = "<a href=""/search/title\?certificates=[^""]*"">([^<]*):([^<]*)</a>[^<]*(<i>([^<]*)</i>)?"
    Private Const REGEX_IMDBID As String = "tt\d\d\d\d\d\d\d"

    Private htmldPlotSummary As HtmlDocument = Nothing
    Private htmldReleaseInfo As HtmlDocument = Nothing
    Private strPosterURL As String = String.Empty

    Private _SpecialSettings As IMDB_Data.SpecialSettings

#End Region 'Fields

#Region "Enumerations"

    Private Enum SearchType
        Details = 0
        Movies = 1
        SearchDetails_Movie = 2
        SearchDetails_TVShow = 3
        TVShows = 4
    End Enum

#End Region 'Enumerations

#Region "Events"

    Public Event Exception(ByVal ex As Exception)

    Public Event SearchInfoDownloaded_Movie(ByVal sPoster As String, ByVal sInfo As MediaContainers.Movie)
    Public Event SearchInfoDownloaded_TV(ByVal sPoster As String, ByVal sInfo As MediaContainers.TVShow)

    Public Event SearchResultsDownloaded_Movie(ByVal mResults As SearchResults_Movie)
    Public Event SearchResultsDownloaded_TV(ByVal mResults As SearchResults_TVShow)

#End Region 'Events

#Region "Methods"

    Public Sub New(ByVal SpecialSettings As IMDB_Data.SpecialSettings)
        _SpecialSettings = SpecialSettings
    End Sub

    Public Sub CancelAsync()

        If bwIMDB.IsBusy Then
            bwIMDB.CancelAsync()
        End If

        While bwIMDB.IsBusy
            Application.DoEvents()
            Threading.Thread.Sleep(50)
        End While
    End Sub

    Private Sub bwIMDB_DoWork(ByVal sender As Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles bwIMDB.DoWork
        Dim Args As Arguments = DirectCast(e.Argument, Arguments)

        Select Case Args.Search
            Case SearchType.Movies
                Dim r As SearchResults_Movie = SearchMovie(Args.Parameter, Args.Year)
                e.Result = New Results With {.ResultType = SearchType.Movies, .Result = r}

            Case SearchType.TVShows
                Dim r As SearchResults_TVShow = SearchTVShow(Args.Parameter)
                e.Result = New Results With {.ResultType = SearchType.TVShows, .Result = r}

            Case SearchType.SearchDetails_Movie
                Dim r As MediaContainers.Movie = GetMovieInfo(Args.Parameter, True, Args.Options_Movie)
                e.Result = New Results With {.ResultType = SearchType.SearchDetails_Movie, .Result = r}

            Case SearchType.SearchDetails_TVShow
                Dim r As MediaContainers.TVShow = GetTVShowInfo(Args.Parameter, Args.ScrapeModifiers, Args.Options_TV, True)
                e.Result = New Results With {.ResultType = SearchType.SearchDetails_TVShow, .Result = r}
        End Select
    End Sub

    Private Sub bwIMDB_RunWorkerCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles bwIMDB.RunWorkerCompleted
        Dim Res As Results = DirectCast(e.Result, Results)

        Select Case Res.ResultType
            Case SearchType.Movies
                RaiseEvent SearchResultsDownloaded_Movie(DirectCast(Res.Result, SearchResults_Movie))

            Case SearchType.TVShows
                RaiseEvent SearchResultsDownloaded_TV(DirectCast(Res.Result, SearchResults_TVShow))

            Case SearchType.SearchDetails_Movie
                Dim movieInfo As MediaContainers.Movie = DirectCast(Res.Result, MediaContainers.Movie)
                RaiseEvent SearchInfoDownloaded_Movie(strPosterURL, movieInfo)

            Case SearchType.SearchDetails_TVShow
                Dim showInfo As MediaContainers.TVShow = DirectCast(Res.Result, MediaContainers.TVShow)
                RaiseEvent SearchInfoDownloaded_TV(strPosterURL, showInfo)
        End Select
    End Sub

    ''' <summary>
    '''  Scrape MovieDetails from IMDB
    ''' </summary>
    ''' <param name="id">IMDBID of movie to be scraped</param>
    ''' <param name="FullCrew">Module setting: Scrape full cast?</param>
    ''' <param name="getposter">Scrape posters for the movie?</param>
    ''' <param name="Options">Module settings<param>
    ''' <param name="IsSearch">Not used at moment</param>
    ''' <returns>True: success, false: no success</returns>
    ''' <remarks></remarks>
    Public Function GetMovieInfo(ByVal id As String, ByVal getposter As Boolean, ByVal filteredoptions As Structures.ScrapeOptions) As MediaContainers.Movie
        If String.IsNullOrEmpty(id) Then Return Nothing

        Try
            If bwIMDB.CancellationPending Then Return Nothing

            Dim bIsScraperLanguage As Boolean = _SpecialSettings.PrefLanguage.ToLower.StartsWith("en")

            strPosterURL = String.Empty
            Dim nMovie As New MediaContainers.Movie With {
                .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.Movie) With {.IMDbId = id},
                .Scrapersource = "IMDB"
            }

            'all data is fetched with one single GraphQL request
            Dim data As Dictionary(Of String, Object) = clsIMDBApi.QueryTitle(id)
            If data Is Nothing Then
                logger.Error(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] no data returned by the IMDb API", id))
                Return Nothing
            End If

            'title and originaltitle
            Dim strOriginalTitle As String = clsIMDBApi.JStr(clsIMDBApi.JObj(data, "originalTitleText"), "text")
            Dim strTitle As String = clsIMDBApi.JStr(clsIMDBApi.JObj(data, "titleText"), "text")
            If String.IsNullOrEmpty(strOriginalTitle) Then strOriginalTitle = strTitle
            If String.IsNullOrEmpty(strTitle) Then strTitle = strOriginalTitle
            If String.IsNullOrEmpty(strOriginalTitle) Then
                logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Originaltitle", id))
            End If

            'year
            Dim strYear As String = clsIMDBApi.JStr(clsIMDBApi.JObj(data, "releaseYear"), "year")
            If Not String.IsNullOrEmpty(strYear) Then nMovie.Year = strYear

            'actors
            If filteredoptions.bMainActors Then
                Dim nActors As New List(Of MediaContainers.Person)
                For Each node In clsIMDBApi.JEdges(clsIMDBApi.JObj(data, "credits"))
                    Dim strCategory As String = clsIMDBApi.JStr(clsIMDBApi.JObj(node, "category"), "id")
                    If strCategory = "actor" OrElse strCategory = "actress" OrElse strCategory = "self" Then
                        Dim strActor As String = clsIMDBApi.JStr(clsIMDBApi.JObj(clsIMDBApi.JObj(node, "name"), "nameText"), "text")
                        If Not String.IsNullOrEmpty(strActor) Then
                            Dim nActor As New MediaContainers.Person With {.Name = strActor}
                            Dim lstCharacters = clsIMDBApi.JArr(node, "characters")
                            If lstCharacters.Count > 0 Then
                                nActor.Role = clsIMDBApi.JStr(lstCharacters(0), "name")
                            End If
                            nActors.Add(nActor)
                        End If
                    End If
                Next
                If nActors.Count > 0 Then
                    nMovie.Actors = nActors
                Else
                    logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Actors", id))
                End If
            End If

            'directors and writers
            Dim lstDirectors As New List(Of String)
            Dim lstWriters As New List(Of String)
            For Each group In clsIMDBApi.JArr(data, "principalCredits")
                For Each credit In clsIMDBApi.JArr(group, "credits")
                    Dim strCategory As String = clsIMDBApi.JStr(clsIMDBApi.JObj(credit, "category"), "id")
                    Dim strName As String = clsIMDBApi.JStr(clsIMDBApi.JObj(clsIMDBApi.JObj(credit, "name"), "nameText"), "text")
                    If String.IsNullOrEmpty(strName) Then Continue For
                    Select Case strCategory
                        Case "director"
                            If Not lstDirectors.Contains(strName) Then lstDirectors.Add(strName)
                        Case "writer"
                            If Not lstWriters.Contains(strName) Then lstWriters.Add(strName)
                    End Select
                Next
            Next

            If filteredoptions.bMainDirectors Then
                If lstDirectors.Count > 0 Then
                    nMovie.Directors = lstDirectors
                Else
                    logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Directors", id))
                End If
            End If

            If filteredoptions.bMainWriters Then
                If lstWriters.Count > 0 Then
                    nMovie.Credits = lstWriters
                Else
                    logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Writers (Credits)", id))
                End If
            End If

            'countries
            If filteredoptions.bMainCountries Then
                Dim lstCountries As New List(Of String)
                For Each country In clsIMDBApi.JArr(clsIMDBApi.JObj(data, "countriesOfOrigin"), "countries")
                    Dim strCountry As String = clsIMDBApi.JStr(country, "text")
                    If Not String.IsNullOrEmpty(strCountry) AndAlso Not lstCountries.Contains(strCountry) Then lstCountries.Add(strCountry)
                Next
                If lstCountries.Count > 0 Then
                    nMovie.Countries = lstCountries
                Else
                    logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Countries", id))
                End If
            End If

            'runtime
            If filteredoptions.bMainRuntime Then
                Dim lSeconds As Long = CLng(clsIMDBApi.JNum(clsIMDBApi.JObj(data, "runtime"), "seconds"))
                If lSeconds > 0 Then
                    'the nfo format expects minutes (all other scrapers deliver minutes
                    'too, see MovieScraperDurationRuntimeFormat = "<m>"), so do not use
                    'the "h:mm" notation here
                    nMovie.Runtime = CStr(Math.Round(lSeconds / 60, 0))
                Else
                    logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Runtime", id))
                End If
            End If

            'genres
            If filteredoptions.bMainGenres Then
                Dim lstGenres As New List(Of String)
                For Each genre In clsIMDBApi.JArr(clsIMDBApi.JObj(data, "genres"), "genres")
                    Dim strGenre As String = clsIMDBApi.JStr(genre, "text")
                    If Not String.IsNullOrEmpty(strGenre) AndAlso Not lstGenres.Contains(strGenre) Then lstGenres.Add(strGenre)
                Next
                If lstGenres.Count > 0 Then
                    nMovie.Genres = lstGenres
                Else
                    logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Genres", id))
                End If
            End If

            'plot and outline
            Dim lstPlots As New List(Of String)
            For Each node In clsIMDBApi.JEdges(clsIMDBApi.JObj(data, "plots"))
                Dim strPlot As String = clsIMDBApi.JStr(clsIMDBApi.JObj(clsIMDBApi.JObj(node, "plotText"), "plainText"), "plainText")
                If Not String.IsNullOrEmpty(strPlot) AndAlso Not lstPlots.Contains(strPlot) Then lstPlots.Add(strPlot)
            Next
            If lstPlots.Count = 0 Then
                Dim strPlot As String = clsIMDBApi.JStr(clsIMDBApi.JObj(clsIMDBApi.JObj(data, "plot"), "plotText"), "plainText")
                If Not String.IsNullOrEmpty(strPlot) Then lstPlots.Add(strPlot)
            End If
            If lstPlots.Count > 0 Then
                If filteredoptions.bMainOutline Then nMovie.Outline = lstPlots(0)
                If filteredoptions.bMainPlot Then
                    nMovie.Plot = If(lstPlots.Count > 1, lstPlots(1), lstPlots(0))
                End If
            ElseIf filteredoptions.bMainPlot OrElse filteredoptions.bMainOutline Then
                logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Plot/Outline", id))
            End If

            'tagline
            If filteredoptions.bMainTagline AndAlso bIsScraperLanguage Then
                For Each node In clsIMDBApi.JEdges(clsIMDBApi.JObj(data, "taglines"))
                    Dim strTagline As String = clsIMDBApi.JStr(node, "text")
                    If Not String.IsNullOrEmpty(strTagline) Then
                        nMovie.Tagline = strTagline
                        Exit For
                    End If
                Next
            End If

            'premiered
            If filteredoptions.bMainPremiered Then
                Dim datePremiered As Date
                If TryGetReleaseDate(data, datePremiered) Then
                    nMovie.Premiered = datePremiered.ToString("yyyy-MM-dd")
                Else
                    logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Premiered/Year", id))
                End If
            End If

            'rating
            If filteredoptions.bMainRating Then
                Dim ratingsSummary = clsIMDBApi.JObj(data, "ratingsSummary")
                Dim dblRating As Double = clsIMDBApi.JNum(ratingsSummary, "aggregateRating")
                If dblRating >= 0 Then
                    nMovie.Ratings.Add(New MediaContainers.RatingDetails With {
                        .Max = 10,
                        .Type = "imdb",
                        .Value = dblRating,
                        .Votes = CInt(clsIMDBApi.JNum(ratingsSummary, "voteCount"))
                    })
                Else
                    logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Rating", id))
                End If
            End If

            'studios
            If filteredoptions.bMainStudios Then
                For Each node In clsIMDBApi.JEdges(clsIMDBApi.JObj(data, "companyCredits"))
                    Dim strCategory As String = clsIMDBApi.JStr(clsIMDBApi.JObj(node, "category"), "id")
                    'the GraphQL API uses the category id "distribution" for distributors
                    If strCategory = "production" OrElse (_SpecialSettings.StudiowithDistributors AndAlso strCategory = "distribution") Then
                        Dim strStudio As String = clsIMDBApi.JStr(clsIMDBApi.JObj(clsIMDBApi.JObj(node, "company"), "companyText"), "text")
                        If Not String.IsNullOrEmpty(strStudio) AndAlso Not nMovie.Studios.Contains(strStudio) Then nMovie.Studios.Add(strStudio)
                    End If
                Next
                If nMovie.Studios.Count = 0 Then
                    logger.Trace(String.Format("[IMDB] [GetMovieInfo] [ID:""{0}""] can't parse Studios", id))
                End If
            End If

            'poster for the search result dialog
            If getposter Then
                Dim strPoster As String = clsIMDBApi.JStr(clsIMDBApi.JObj(data, "primaryImage"), "url")
                If Not String.IsNullOrEmpty(strPoster) Then strPosterURL = strPoster
            End If

            'title
            If filteredoptions.bMainTitle Then
                If Not String.IsNullOrEmpty(_SpecialSettings.ForceTitleLanguage) Then
                    nMovie.Title = GetTitleForLanguage(data, _SpecialSettings.ForceTitleLanguage, strTitle)
                Else
                    nMovie.Title = strTitle
                End If
            End If

            'originaltitle
            If filteredoptions.bMainOriginalTitle Then
                nMovie.OriginalTitle = strOriginalTitle
            End If

            'certifications
            'the GraphQL API does provide the certificate of a title (the original rating),
            'see the "certificate" field in the query
            If filteredoptions.bMainCertifications Then
                ParseCertificate(data, nMovie.Certifications)
                Dim strMPAA As String = GetCertificateRating(data)
                If Not String.IsNullOrEmpty(strMPAA) Then nMovie.MPAA = strMPAA
            End If

            Return nMovie
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Returns the release date of a title. The oldest release date is used, like
    ''' the former parsing of the "releaseinfo" page did.
    ''' </summary>
    Private Function TryGetReleaseDate(ByVal data As Dictionary(Of String, Object), ByRef releasedate As Date) As Boolean
        Dim lstDates As New List(Of Date)
        For Each node In clsIMDBApi.JEdges(clsIMDBApi.JObj(data, "releaseDates"))
            Dim nDate As Date
            If TryGetDate(node, nDate) Then lstDates.Add(nDate)
        Next

        If lstDates.Count > 0 Then
            lstDates.Sort()
            releasedate = lstDates(0)
            Return True
        End If

        'fallback to the main release date of the title
        Dim nMainDate As Date
        If TryGetDate(clsIMDBApi.JObj(data, "releaseDate"), nMainDate) Then
            releasedate = nMainDate
            Return True
        End If

        Return False
    End Function

    ''' <summary>
    ''' Reads a date out of a GraphQL node with the fields "year", "month" and "day".
    ''' </summary>
    Private Function TryGetDate(ByVal node As Object, ByRef value As Date) As Boolean
        If node Is Nothing Then Return False
        Dim nYear As Integer = CInt(clsIMDBApi.JNum(node, "year"))
        Dim nMonth As Integer = CInt(clsIMDBApi.JNum(node, "month"))
        Dim nDay As Integer = CInt(clsIMDBApi.JNum(node, "day"))
        If nYear < 1900 OrElse nYear > 2100 Then Return False
        If nMonth < 1 OrElse nMonth > 12 Then nMonth = 1
        If nDay < 1 OrElse nDay > 28 Then nDay = 1
        Return Date.TryParse(String.Format("{0:0000}-{1:00}-{2:00}", nYear, nMonth, nDay), value)
    End Function

    ''' <summary>
    ''' Returns the title of a title in the given country (ISO code), if there is one.
    ''' </summary>
    Private Function GetTitleForLanguage(ByVal data As Dictionary(Of String, Object), ByVal language As String, ByVal fallback As String) As String
        For Each node In clsIMDBApi.JEdges(clsIMDBApi.JObj(data, "akas"))
            Dim strCountry As String = clsIMDBApi.JStr(clsIMDBApi.JObj(node, "country"), "id")
            If strCountry.Equals(language, StringComparison.OrdinalIgnoreCase) Then
                Dim strText As String = clsIMDBApi.JStr(node, "text")
                If Not String.IsNullOrEmpty(strText) Then Return strText
            End If
        Next
        Return fallback
    End Function

    Public Function GetTVEpisodeInfo(ByVal id As String, ByRef filteredoptions As Structures.ScrapeOptions, Optional ByVal episodeNode As Dictionary(Of String, Object) = Nothing) As MediaContainers.EpisodeDetails
        If String.IsNullOrEmpty(id) Then Return Nothing

        Try
            Dim bIsScraperLanguage As Boolean = _SpecialSettings.PrefLanguage.ToLower.StartsWith("en")

            Dim nTVEpisode As New MediaContainers.EpisodeDetails With {
                .Scrapersource = "IMDB",
                .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.TVEpisode) With {.IMDbId = id}
            }

            Dim data As Dictionary(Of String, Object) = episodeNode
            If data Is Nothing Then data = clsIMDBApi.QueryEpisode(id)
            If data Is Nothing Then
                logger.Error(String.Format("[IMDB] [GetTVEpisodeInfo] [ID:""{0}""] no data returned by the IMDb API", id))
                Return Nothing
            End If

            'title and originaltitle
            Dim strTitle As String = clsIMDBApi.JStr(clsIMDBApi.JObj(data, "titleText"), "text")
            Dim strOriginalTitle As String = clsIMDBApi.JStr(clsIMDBApi.JObj(data, "originalTitleText"), "text")
            If String.IsNullOrEmpty(strOriginalTitle) Then strOriginalTitle = strTitle
            If String.IsNullOrEmpty(strTitle) Then strTitle = strOriginalTitle

            If filteredoptions.bMainTitle Then
                If Not String.IsNullOrEmpty(_SpecialSettings.ForceTitleLanguage) Then
                    nTVEpisode.Title = GetTitleForLanguage(data, _SpecialSettings.ForceTitleLanguage, strTitle)
                Else
                    nTVEpisode.Title = strTitle
                End If
            End If

            If filteredoptions.bMainOriginalTitle Then
                nTVEpisode.OriginalTitle = strOriginalTitle
            End If

            'season and episode number
            Dim episodeNumber = clsIMDBApi.JObj(clsIMDBApi.JObj(data, "series"), "episodeNumber")
            If episodeNumber IsNot Nothing Then
                nTVEpisode.Season = CInt(clsIMDBApi.JNum(episodeNumber, "seasonNumber"))
                nTVEpisode.Episode = CInt(clsIMDBApi.JNum(episodeNumber, "episodeNumber"))
            End If

            'aired
            Dim dateAired As New Date
            If TryGetReleaseDate(data, dateAired) Then
                nTVEpisode.Aired = dateAired.ToString("yyyy-MM-dd")
            End If

            'runtime
            Dim lSeconds As Long = CLng(clsIMDBApi.JNum(clsIMDBApi.JObj(data, "runtime"), "seconds"))
            If lSeconds > 0 Then
                'the nfo format expects minutes (all other scrapers deliver minutes too)
                nTVEpisode.Runtime = CStr(Math.Round(lSeconds / 60, 0))
            End If

            'rating
            Dim ratingsSummary = clsIMDBApi.JObj(data, "ratingsSummary")
            Dim dblRating As Double = clsIMDBApi.JNum(ratingsSummary, "aggregateRating")
            If dblRating >= 0 Then
                nTVEpisode.Ratings.Add(New MediaContainers.RatingDetails With {
                    .Max = 10,
                    .Type = "imdb",
                    .Value = dblRating,
                    .Votes = CInt(clsIMDBApi.JNum(ratingsSummary, "voteCount"))
                })
            End If

            'plot
            If filteredoptions.bMainPlot AndAlso bIsScraperLanguage Then
                Dim strPlot As String = clsIMDBApi.JStr(clsIMDBApi.JObj(clsIMDBApi.JObj(data, "plot"), "plotText"), "plainText")
                If Not String.IsNullOrEmpty(strPlot) Then nTVEpisode.Plot = strPlot
            End If

            'directors and writers
            For Each node In clsIMDBApi.JEdges(clsIMDBApi.JObj(data, "credits"))
                Dim strCategory As String = clsIMDBApi.JStr(clsIMDBApi.JObj(node, "category"), "id")
                Dim strName As String = clsIMDBApi.JStr(clsIMDBApi.JObj(clsIMDBApi.JObj(node, "name"), "nameText"), "text")
                If String.IsNullOrEmpty(strName) Then Continue For
                If strCategory = "director" Then
                    If Not nTVEpisode.Directors.Contains(strName) Then nTVEpisode.Directors.Add(strName)
                ElseIf strCategory = "writer" Then
                    If Not nTVEpisode.Credits.Contains(strName) Then nTVEpisode.Credits.Add(strName)
                End If
            Next

            'actors
            For Each node In clsIMDBApi.JEdges(clsIMDBApi.JObj(data, "credits"))
                Dim strCategory As String = clsIMDBApi.JStr(clsIMDBApi.JObj(node, "category"), "id")
                If strCategory = "actor" OrElse strCategory = "actress" OrElse strCategory = "self" Then
                    Dim strActor As String = clsIMDBApi.JStr(clsIMDBApi.JObj(clsIMDBApi.JObj(node, "name"), "nameText"), "text")
                    If Not String.IsNullOrEmpty(strActor) Then
                        Dim nActor As New MediaContainers.Person With {.Name = strActor}
                        Dim lstCharacters = clsIMDBApi.JArr(node, "characters")
                        If lstCharacters.Count > 0 Then nActor.Role = clsIMDBApi.JStr(lstCharacters(0), "name")
                        nTVEpisode.Actors.Add(nActor)
                    End If
                End If
            Next

            Return nTVEpisode
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return Nothing
        End Try
    End Function


    Public Function GetTVEpisodeInfo(ByVal showid As String, ByVal season As Integer, ByVal episode As Integer, ByRef filteredoptions As Structures.ScrapeOptions) As MediaContainers.EpisodeDetails
        If String.IsNullOrEmpty(showid) OrElse season = -1 OrElse episode = -1 Then Return Nothing

        For Each node In clsIMDBApi.GetShowEpisodes(showid)
            Dim episodeNumber = clsIMDBApi.JObj(clsIMDBApi.JObj(node, "series"), "episodeNumber")
            If episodeNumber Is Nothing Then Continue For
            If CInt(clsIMDBApi.JNum(episodeNumber, "seasonNumber")) <> season Then Continue For
            If CInt(clsIMDBApi.JNum(episodeNumber, "episodeNumber")) <> episode Then Continue For

            Dim strID As String = clsIMDBApi.JStr(node, "id")
            If Not String.IsNullOrEmpty(strID) Then
                Dim nEpisode As MediaContainers.EpisodeDetails = GetTVEpisodeInfo(strID, filteredoptions, node)
                If nEpisode IsNot Nothing Then Return nEpisode
            End If
        Next

        Return Nothing
    End Function


    Public Sub GetTVSeasonInfo(ByRef nTVShow As MediaContainers.TVShow, ByVal showid As String, ByVal season As Integer, ByRef scrapemodifiers As Structures.ScrapeModifiers, ByRef filteredoptions As Structures.ScrapeOptions, Optional ByVal lstEpisodes As List(Of Dictionary(Of String, Object)) = Nothing)
        Try
            If scrapemodifiers.withEpisodes Then
                If lstEpisodes Is Nothing Then lstEpisodes = clsIMDBApi.GetShowEpisodes(showid)

                For Each nodeEpisode In lstEpisodes
                    Dim episodeNumber = clsIMDBApi.JObj(clsIMDBApi.JObj(nodeEpisode, "series"), "episodeNumber")
                    If episodeNumber Is Nothing Then Continue For
                    If CInt(clsIMDBApi.JNum(episodeNumber, "seasonNumber")) <> season Then Continue For

                    Dim strIMDBID As String = clsIMDBApi.JStr(nodeEpisode, "id")
                    Dim nEpisode As MediaContainers.EpisodeDetails = GetTVEpisodeInfo(strIMDBID, filteredoptions, nodeEpisode)
                    If nEpisode IsNot Nothing Then
                        nTVShow.KnownEpisodes.Add(nEpisode)
                    End If
                Next
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub
    ''' <summary>
    '''  Scrape TV Show details from IMDB
    ''' </summary>
    ''' <param name="id">IMDB ID of tv show to be scraped</param>
    ''' <param name="getposter">Scrape posters for the tv show?</param> 
    ''' <returns>True: success, false: no success</returns>
    Public Function GetTVShowInfo(ByVal id As String, ByVal scrapemodifier As Structures.ScrapeModifiers, ByVal filteredoptions As Structures.ScrapeOptions, ByVal getposter As Boolean) As MediaContainers.TVShow
        If String.IsNullOrEmpty(id) Then Return Nothing

        Try
            If bwIMDB.CancellationPending Then Return Nothing

            Dim bIsScraperLanguage As Boolean = _SpecialSettings.PrefLanguage.ToLower.StartsWith("en")

            strPosterURL = String.Empty
            Dim nTVShow As New MediaContainers.TVShow With {
                .Scrapersource = "IMDB",
                .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.TVShow) With {.IMDbId = id}
            }

            Dim data As Dictionary(Of String, Object) = clsIMDBApi.QueryShow(id)
            If data Is Nothing Then
                logger.Error(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] no data returned by the IMDb API", id))
                Return Nothing
            End If

            If bwIMDB.CancellationPending Then Return Nothing

            'title and originaltitle
            Dim strTitle As String = clsIMDBApi.JStr(clsIMDBApi.JObj(data, "titleText"), "text")
            Dim strOriginalTitle As String = clsIMDBApi.JStr(clsIMDBApi.JObj(data, "originalTitleText"), "text")
            If String.IsNullOrEmpty(strOriginalTitle) Then strOriginalTitle = strTitle
            If String.IsNullOrEmpty(strTitle) Then strTitle = strOriginalTitle

            If filteredoptions.bMainOriginalTitle Then
                nTVShow.OriginalTitle = strOriginalTitle
            End If

            If filteredoptions.bMainTitle Then
                If Not String.IsNullOrEmpty(_SpecialSettings.ForceTitleLanguage) Then
                    nTVShow.Title = GetTitleForLanguage(data, _SpecialSettings.ForceTitleLanguage, strTitle)
                Else
                    nTVShow.Title = strTitle
                End If
            End If

            If bwIMDB.CancellationPending Then Return Nothing

            'actors
            If filteredoptions.bMainActors Then
                Dim lstActors As New List(Of MediaContainers.Person)
                For Each node In clsIMDBApi.JEdges(clsIMDBApi.JObj(data, "credits"))
                    Dim strCategory As String = clsIMDBApi.JStr(clsIMDBApi.JObj(node, "category"), "id")
                    If strCategory = "actor" OrElse strCategory = "actress" OrElse strCategory = "self" Then
                        Dim strActor As String = clsIMDBApi.JStr(clsIMDBApi.JObj(clsIMDBApi.JObj(node, "name"), "nameText"), "text")
                        If Not String.IsNullOrEmpty(strActor) Then
                            Dim nActor As New MediaContainers.Person With {.Name = strActor}
                            Dim lstCharacters = clsIMDBApi.JArr(node, "characters")
                            If lstCharacters.Count > 0 Then nActor.Role = clsIMDBApi.JStr(lstCharacters(0), "name")
                            lstActors.Add(nActor)
                        End If
                    End If
                Next
                If lstActors.Count > 0 Then
                    nTVShow.Actors = lstActors
                Else
                    logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Actors", id))
                End If
            End If

            If bwIMDB.CancellationPending Then Return Nothing

            'creators
            If filteredoptions.bMainCreators Then
                Dim lstCreators As New List(Of String)
                For Each group In clsIMDBApi.JArr(data, "principalCredits")
                    For Each credit In clsIMDBApi.JArr(group, "credits")
                        Dim strCategory As String = clsIMDBApi.JStr(clsIMDBApi.JObj(credit, "category"), "id")
                        If strCategory = "writer" OrElse strCategory = "creator" Then
                            Dim strName As String = clsIMDBApi.JStr(clsIMDBApi.JObj(clsIMDBApi.JObj(credit, "name"), "nameText"), "text")
                            If Not String.IsNullOrEmpty(strName) AndAlso Not lstCreators.Contains(strName) Then lstCreators.Add(strName)
                        End If
                    Next
                Next
                If lstCreators.Count > 0 Then
                    nTVShow.Creators = lstCreators
                Else
                    logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Creators", id))
                End If
            End If

            If bwIMDB.CancellationPending Then Return Nothing

            'genres
            If filteredoptions.bMainGenres Then
                Dim lstGenres As New List(Of String)
                For Each genre In clsIMDBApi.JArr(clsIMDBApi.JObj(data, "genres"), "genres")
                    Dim strGenre As String = clsIMDBApi.JStr(genre, "text")
                    If Not String.IsNullOrEmpty(strGenre) AndAlso Not lstGenres.Contains(strGenre) Then lstGenres.Add(strGenre)
                Next
                If lstGenres.Count > 0 Then
                    nTVShow.Genres = lstGenres
                Else
                    logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Genres", id))
                End If
            End If

            If bwIMDB.CancellationPending Then Return Nothing

            'countries
            If filteredoptions.bMainCountries Then
                Dim lstCountries As New List(Of String)
                For Each country In clsIMDBApi.JArr(clsIMDBApi.JObj(data, "countriesOfOrigin"), "countries")
                    Dim strCountry As String = clsIMDBApi.JStr(country, "text")
                    If Not String.IsNullOrEmpty(strCountry) AndAlso Not lstCountries.Contains(strCountry) Then lstCountries.Add(strCountry)
                Next
                If lstCountries.Count > 0 Then
                    nTVShow.Countries = lstCountries
                Else
                    logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Countries", id))
                End If
            End If

            If bwIMDB.CancellationPending Then Return Nothing

            'runtime
            If filteredoptions.bMainRuntime Then
                Dim lSeconds As Long = CLng(clsIMDBApi.JNum(clsIMDBApi.JObj(data, "runtime"), "seconds"))
                If lSeconds > 0 Then
                    'the nfo format expects minutes (all other scrapers deliver minutes too)
                    nTVShow.Runtime = CStr(Math.Round(lSeconds / 60, 0))
                Else
                    logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Runtime", id))
                End If
            End If

            If bwIMDB.CancellationPending Then Return Nothing

            'tagline
            If filteredoptions.bMainTagline AndAlso bIsScraperLanguage Then
                For Each node In clsIMDBApi.JEdges(clsIMDBApi.JObj(data, "taglines"))
                    Dim strTagline As String = clsIMDBApi.JStr(node, "text")
                    If Not String.IsNullOrEmpty(strTagline) Then
                        nTVShow.Tagline = strTagline
                        Exit For
                    End If
                Next
            End If

            'premiered
            If filteredoptions.bMainPremiered Then
                Dim datePremiered As New Date
                If TryGetReleaseDate(data, datePremiered) Then
                    nTVShow.Premiered = datePremiered.ToString("yyyy-MM-dd")
                Else
                    logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Premiered", id))
                End If
            End If

            If bwIMDB.CancellationPending Then Return Nothing

            'rating
            If filteredoptions.bMainRating Then
                Dim ratingsSummary = clsIMDBApi.JObj(data, "ratingsSummary")
                Dim dblRating As Double = clsIMDBApi.JNum(ratingsSummary, "aggregateRating")
                If dblRating >= 0 Then
                    nTVShow.Ratings.Add(New MediaContainers.RatingDetails With {
                        .Max = 10,
                        .Type = "imdb",
                        .Value = dblRating,
                        .Votes = CInt(clsIMDBApi.JNum(ratingsSummary, "voteCount"))
                    })
                Else
                    logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Rating", id))
                End If
            End If

            If bwIMDB.CancellationPending Then Return Nothing

            'plot
            If filteredoptions.bMainPlot AndAlso bIsScraperLanguage Then
                Dim lstPlots As New List(Of String)
                For Each node In clsIMDBApi.JEdges(clsIMDBApi.JObj(data, "plots"))
                    Dim strPlot As String = clsIMDBApi.JStr(clsIMDBApi.JObj(clsIMDBApi.JObj(node, "plotText"), "plainText"), "plainText")
                    If Not String.IsNullOrEmpty(strPlot) AndAlso Not lstPlots.Contains(strPlot) Then lstPlots.Add(strPlot)
                Next
                If lstPlots.Count = 0 Then
                    Dim strPlot As String = clsIMDBApi.JStr(clsIMDBApi.JObj(clsIMDBApi.JObj(data, "plot"), "plotText"), "plainText")
                    If Not String.IsNullOrEmpty(strPlot) Then lstPlots.Add(strPlot)
                End If
                If lstPlots.Count > 0 Then
                    nTVShow.Plot = lstPlots(0)
                Else
                    logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Plot", id))
                End If
            End If

            If bwIMDB.CancellationPending Then Return Nothing

            'studios
            If filteredoptions.bMainStudios Then
                For Each node In clsIMDBApi.JEdges(clsIMDBApi.JObj(data, "companyCredits"))
                    Dim strCategory As String = clsIMDBApi.JStr(clsIMDBApi.JObj(node, "category"), "id")
                    If strCategory = "production" Then
                        Dim strStudio As String = clsIMDBApi.JStr(clsIMDBApi.JObj(clsIMDBApi.JObj(node, "company"), "companyText"), "text")
                        If Not String.IsNullOrEmpty(strStudio) AndAlso Not nTVShow.Studios.Contains(strStudio) Then nTVShow.Studios.Add(strStudio)
                    End If
                Next
                If nTVShow.Studios.Count = 0 Then
                    logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] can't parse Studios", id))
                End If
            End If

            If bwIMDB.CancellationPending Then Return Nothing

            'certifications
            'the GraphQL API does provide the certificate of a title (the original rating),
            'see the "certificate" field in the query
            If filteredoptions.bMainCertifications Then
                ParseCertificate(data, nTVShow.Certifications)
                Dim strMPAA As String = GetCertificateRating(data)
                If Not String.IsNullOrEmpty(strMPAA) Then nTVShow.MPAA = strMPAA
            End If

            'poster for the search result dialog
            If getposter Then
                Dim strPoster As String = clsIMDBApi.JStr(clsIMDBApi.JObj(data, "primaryImage"), "url")
                If Not String.IsNullOrEmpty(strPoster) Then strPosterURL = strPoster
            End If

            'seasons and episodes
            If scrapemodifier.withEpisodes OrElse scrapemodifier.withSeasons Then
                Dim lstEpisodes As New List(Of Dictionary(Of String, Object))
                For Each node In clsIMDBApi.JEdges(clsIMDBApi.JObj(clsIMDBApi.JObj(data, "episodes"), "episodes"))
                    lstEpisodes.Add(node)
                Next
                logger.Trace(String.Format("[IMDB] [GetTVShowInfo] [ID:""{0}""] episode nodes: {1}", id, lstEpisodes.Count))

                Dim lstSeasons As New List(Of Integer)
                For Each episode In lstEpisodes
                    Dim iSeason As Integer = CInt(clsIMDBApi.JNum(clsIMDBApi.JObj(clsIMDBApi.JObj(clsIMDBApi.JObj(episode, "series"), "episodeNumber"), "seasonNumber"), "seasonNumber"))
                    If iSeason >= 0 AndAlso Not lstSeasons.Contains(iSeason) Then lstSeasons.Add(iSeason)
                Next
                lstSeasons.Sort()

                For Each tSeason In lstSeasons
                    If bwIMDB.CancellationPending Then Return Nothing
                    GetTVSeasonInfo(nTVShow, id, tSeason, scrapemodifier, filteredoptions, lstEpisodes)
                    If scrapemodifier.withSeasons Then
                        nTVShow.KnownSeasons.Add(New MediaContainers.SeasonDetails With {.Season = tSeason})
                    End If
                Next
            End If

            Return nTVShow
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return Nothing
        End Try
    End Function


    Public Function GetMovieStudios(ByVal id As String) As List(Of String)
        Dim lstStudios As New List(Of String)

        'the old html scraping of the "reference" page does not work any more:
        'imdb answers with http 202 and an empty body, so the studios were never found.
        'the graphql api delivers the company credits (including the distributors).
        Try
            Dim data As Dictionary(Of String, Object) = clsIMDBApi.QueryTitle(id)
            If data IsNot Nothing Then
                For Each node In clsIMDBApi.JEdges(clsIMDBApi.JObj(data, "companyCredits"))
                    Dim strCategory As String = clsIMDBApi.JStr(clsIMDBApi.JObj(node, "category"), "id")
                    If strCategory = "production" OrElse (_SpecialSettings.StudiowithDistributors AndAlso strCategory = "distribution") Then
                        Dim strStudio As String = clsIMDBApi.JStr(clsIMDBApi.JObj(clsIMDBApi.JObj(node, "company"), "companyText"), "text")
                        If Not String.IsNullOrEmpty(strStudio) AndAlso Not lstStudios.Contains(strStudio) Then lstStudios.Add(strStudio)
                    End If
                Next
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try

        Return lstStudios
    End Function

    Public Function GetSearchMovieInfo(ByVal title As String,
                                           ByVal year As String,
                                           ByRef oDBElement As Database.DBElement,
                                           ByVal scrapetype As Enums.ScrapeType,
                                           ByVal filteredoptions As Structures.ScrapeOptions) As MediaContainers.Movie
        Dim r As SearchResults_Movie = SearchMovie(title, year)

        'only a result that really fits to the title and the year of the file is taken over
        Dim nBest As StringUtils.ScraperSearchCandidate = GetBestMovieMatch(title, GetYearAsInteger(year), r.ExactMatches)

        Try
            Select Case scrapetype
                Case Enums.ScrapeType.AllAsk, Enums.ScrapeType.FilterAsk, Enums.ScrapeType.MarkedAsk, Enums.ScrapeType.MissingAsk, Enums.ScrapeType.NewAsk, Enums.ScrapeType.SelectedAsk, Enums.ScrapeType.SingleField
                    If r.ExactMatches.Count = 1 AndAlso nBest IsNot Nothing Then
                        Return GetMovieInfo(r.ExactMatches.Item(nBest.Index).UniqueIDs.IMDbId, False, filteredoptions)
                    Else
                        Using dlgSearch As New dlgIMDBSearchResults_Movie(_SpecialSettings, Me)
                            If dlgSearch.ShowDialog(r, title, oDBElement.Filename) = DialogResult.OK Then
                                If Not String.IsNullOrEmpty(dlgSearch.Result.UniqueIDs.IMDbId) Then
                                    Return GetMovieInfo(dlgSearch.Result.UniqueIDs.IMDbId, False, filteredoptions)
                                End If
                            End If
                        End Using
                    End If

                Case Enums.ScrapeType.AllSkip, Enums.ScrapeType.FilterSkip, Enums.ScrapeType.MarkedSkip, Enums.ScrapeType.MissingSkip, Enums.ScrapeType.NewSkip, Enums.ScrapeType.SelectedSkip
                    If r.ExactMatches.Count = 1 AndAlso nBest IsNot Nothing Then
                        Return GetMovieInfo(r.ExactMatches.Item(nBest.Index).UniqueIDs.IMDbId, False, filteredoptions)
                    End If

                Case Enums.ScrapeType.AllAuto, Enums.ScrapeType.FilterAuto, Enums.ScrapeType.MarkedAuto, Enums.ScrapeType.MissingAuto, Enums.ScrapeType.NewAuto, Enums.ScrapeType.SelectedAuto, Enums.ScrapeType.SingleScrape
                    'a result is only taken over if its title and year fit to the media file
                    If nBest IsNot Nothing Then
                        Return GetMovieInfo(r.ExactMatches.Item(nBest.Index).UniqueIDs.IMDbId, False, filteredoptions)
                    Else
                        logger.Warn("[IMDB] [GetSearchMovieInfo] No search result for '{0}' fits to the file name - nothing was set", title)
                    End If
            End Select

            Return Nothing
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return Nothing
        End Try
    End Function

    Public Sub GetSearchMovieInfoAsync(ByVal imdbID As String, ByVal FilteredOptions As Structures.ScrapeOptions)
        Try
            If Not bwIMDB.IsBusy Then
                bwIMDB.WorkerReportsProgress = False
                bwIMDB.WorkerSupportsCancellation = True
                bwIMDB.RunWorkerAsync(New Arguments With {.Search = SearchType.SearchDetails_Movie,
                                           .Parameter = imdbID, .Options_Movie = FilteredOptions})
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Public Function GetSearchTVShowInfo(ByVal title As String, ByRef oDBElement As Database.DBElement, ByVal scrapetype As Enums.ScrapeType, ByVal scrapemodifier As Structures.ScrapeModifiers, ByVal FilteredOptions As Structures.ScrapeOptions) As MediaContainers.TVShow
        Dim strYear As String = GetYearFromShowPath(oDBElement.ShowPath)
        Dim r As SearchResults_TVShow = SearchTVShow(title, strYear)

        'only a result that really fits to the title and the year of the folder is taken over
        Dim nBest As StringUtils.ScraperSearchCandidate = GetBestTVShowMatch(title, GetYearAsInteger(strYear), r.Matches)

        Select Case scrapetype
            Case Enums.ScrapeType.AllAsk, Enums.ScrapeType.FilterAsk, Enums.ScrapeType.MarkedAsk, Enums.ScrapeType.MissingAsk, Enums.ScrapeType.NewAsk, Enums.ScrapeType.SelectedAsk, Enums.ScrapeType.SingleField
                If r.Matches.Count = 1 AndAlso nBest IsNot Nothing Then
                    Return GetTVShowInfo(r.Matches.Item(nBest.Index).UniqueIDs.IMDbId, scrapemodifier, FilteredOptions, False)
                Else
                    Using dlgSearch As New dlgIMDBSearchResults_TV(_SpecialSettings, Me)
                        If dlgSearch.ShowDialog(r, title, oDBElement.ShowPath) = DialogResult.OK Then
                            If Not String.IsNullOrEmpty(dlgSearch.Result.UniqueIDs.IMDbId) Then
                                Return GetTVShowInfo(dlgSearch.Result.UniqueIDs.IMDbId, scrapemodifier, FilteredOptions, False)
                            End If
                        End If
                    End Using
                End If

            Case Enums.ScrapeType.AllSkip, Enums.ScrapeType.FilterSkip, Enums.ScrapeType.MarkedSkip, Enums.ScrapeType.MissingSkip, Enums.ScrapeType.NewSkip, Enums.ScrapeType.SelectedSkip
                If r.Matches.Count = 1 AndAlso nBest IsNot Nothing Then
                    Return GetTVShowInfo(r.Matches.Item(nBest.Index).UniqueIDs.IMDbId, scrapemodifier, FilteredOptions, False)
                End If

            Case Enums.ScrapeType.AllAuto, Enums.ScrapeType.FilterAuto, Enums.ScrapeType.MarkedAuto, Enums.ScrapeType.MissingAuto, Enums.ScrapeType.NewAuto, Enums.ScrapeType.SelectedAuto, Enums.ScrapeType.SingleScrape
                If nBest IsNot Nothing Then
                    Return GetTVShowInfo(r.Matches.Item(nBest.Index).UniqueIDs.IMDbId, scrapemodifier, FilteredOptions, False)
                Else
                    logger.Warn("[IMDB] [GetSearchTVShowInfo] No search result for '{0}' fits to the folder name - nothing was set", title)
                End If
        End Select

        Return Nothing
    End Function

    Public Sub GetSearchTVShowInfoAsync(ByVal id As String, ByVal options As Structures.ScrapeOptions)
        Try
            If Not bwIMDB.IsBusy Then
                bwIMDB.WorkerReportsProgress = False
                bwIMDB.WorkerSupportsCancellation = True
                bwIMDB.RunWorkerAsync(New Arguments With {.Search = SearchType.SearchDetails_TVShow,
                                           .Parameter = id, .Options_TV = options})
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Private Function ParseActors(ByRef htmldReference As HtmlDocument, Optional ByVal removeepisodecount As Boolean = False) As List(Of MediaContainers.Person)
        Dim nActors As New List(Of MediaContainers.Person)
        Dim strThumbsSize = AdvancedSettings.GetSetting("ActorThumbsSize", "SX1000_SY1000")
        Dim selNode = htmldReference.DocumentNode.SelectSingleNode("//table[@class=""cast_list""]")
        If selNode IsNot Nothing Then
            Dim ncCast = selNode.Descendants("tr")
            If ncCast IsNot Nothing Then
                For Each nCast In ncCast.Where(Function(f) _
                                                       f.Attributes IsNot Nothing AndAlso
                                                       f.Attributes.Where(Function(a) a.Name = "class" AndAlso a.Value = "odd" OrElse a.Value = "even").Any
                                                       )
                    Dim nActor As New MediaContainers.Person
                    'get actor thumb
                    Dim ndActorthumb = nCast.Descendants("img").FirstOrDefault
                    If ndActorthumb IsNot Nothing AndAlso
                        ndActorthumb.Attributes("loadlate") IsNot Nothing AndAlso
                        Not String.IsNullOrEmpty(ndActorthumb.Attributes("loadlate").Value) AndAlso
                        Not ndActorthumb.Attributes("loadlate").Value.Contains("no_image") AndAlso
                        Not ndActorthumb.Attributes("loadlate").Value.Contains("no_photo") AndAlso
                        Not ndActorthumb.Attributes("loadlate").Value.Contains("thumb") AndAlso
                        Not ndActorthumb.Attributes("loadlate").Value.Contains("addtiny") Then
                        Dim rToReplace = Regex.Match(ndActorthumb.Attributes("loadlate").Value, ".*\.(.+)\.jpg$")
                        If rToReplace.Success Then
                            nActor.Thumb.URLOriginal = ndActorthumb.Attributes("loadlate").Value.Replace(rToReplace.Groups(1).Value, strThumbsSize)
                        End If
                    End If
                    'get actor name
                    Dim ndName = nCast.Descendants("td").Where(Function(f) f.Attributes.Where(Function(a) a.Name = "itemprop" AndAlso a.Value = "actor").Any).FirstOrDefault
                    'get character name
                    Dim ndCharacter = nCast.Descendants("td").Where(Function(f) f.Attributes.Where(Function(a) a.Name = "class" AndAlso a.Value = "character").Any).FirstOrDefault
                    'character cleanup (removes whitespaces)
                    Dim strCharacter As String = Regex.Replace(HttpUtility.HtmlDecode(ndCharacter.InnerText.Trim), "\s{2,}", " ").Trim
                    'character cleanup (removes "/ ...")
                    strCharacter = Regex.Replace(strCharacter, "\/ \.\.\.", String.Empty).Trim
                    If removeepisodecount Then
                        'character cleanup (removes episode info like "(49 episodes, 2011-2017)")
                        strCharacter = Regex.Replace(strCharacter, "\(\d+.*\)", String.Empty).Trim
                    End If
                    If ndName IsNot Nothing AndAlso ndCharacter IsNot Nothing Then
                        nActor.Name = HttpUtility.HtmlDecode(ndName.InnerText.Trim)
                        nActor.Role = strCharacter
                        nActors.Add(nActor)
                    End If
                Next
                Return nActors
            End If
        End If
        Return Nothing
    End Function

    Private Function ParseCertifications(ByRef htmldReference As HtmlDocument) As List(Of String)
        Dim selNode = htmldReference.DocumentNode.SelectSingleNode("//tr[@class=""ipl-zebra-list__item""]/td[@class=""ipl-zebra-list__label""][.='Certification']")
        If selNode IsNot Nothing AndAlso selNode.ParentNode IsNot Nothing Then
            Dim rCert As MatchCollection = Regex.Matches(selNode.ParentNode.InnerHtml, REGEX_Certifications)
            If rCert.Count > 0 Then
                Dim Certs = From M In rCert Select N = String.Format("{0}:{1}", DirectCast(M, Match).Groups(1).ToString.Trim, DirectCast(M, Match).Groups(2).ToString.Trim) Order By N Ascending
                Dim lstCertifications As New List(Of String)
                For Each tCert In Certs
                    tCert = tCert.Replace("United Kingdom", "UK")
                    tCert = tCert.Replace("United States", "USA")
                    tCert = tCert.Replace("West", String.Empty)
                    lstCertifications.Add(HttpUtility.HtmlDecode(tCert).Trim)
                Next
                lstCertifications = lstCertifications.Distinct.ToList
                lstCertifications.Sort()
                Return lstCertifications
            End If
        End If
        Return Nothing
    End Function

    Private Function ParseCountries(ByRef htmldReference As HtmlDocument) As List(Of String)
        Dim selNode = htmldReference.DocumentNode.SelectSingleNode("//tr[@class=""ipl-zebra-list__item""]/td[@class=""ipl-zebra-list__label""][.='Country']")
        If selNode IsNot Nothing AndAlso selNode.ParentNode IsNot Nothing Then
            Dim ncCountries = selNode.ParentNode.Descendants("a")
            If ncCountries IsNot Nothing Then
                Return ncCountries.Select(Function(f) HttpUtility.HtmlDecode(f.InnerText)).Distinct.ToList
            End If
        End If
        Return Nothing
    End Function

    Private Function ParseCreators(ByRef htmldReference As HtmlDocument) As List(Of String)
        Dim selNodes = htmldReference.DocumentNode.SelectNodes("//div[@class=""titlereference-overview-section""]")
        If selNodes IsNot Nothing Then
            Dim selNode = selNodes.Where(Function(f) f.InnerText.Trim.StartsWith("Creator")).FirstOrDefault
            If selNode IsNot Nothing Then
                Dim nCreators = selNode.Descendants("a").Where(Function(f) Not f.InnerText.ToLower = "see more")
                If nCreators IsNot Nothing Then
                    Return nCreators.Select(Function(f) HttpUtility.HtmlDecode(f.InnerText)).Distinct.ToList
                End If
            End If
        End If
        Return Nothing
    End Function

    Private Function ParseCredits(ByRef htmldReference As HtmlDocument) As List(Of String)
        Dim selNodes = htmldReference.DocumentNode.SelectNodes("//div[@class=""titlereference-overview-section""]")
        If selNodes IsNot Nothing Then
            Dim selNode = selNodes.Where(Function(f) f.InnerText.Trim.StartsWith("Writer")).FirstOrDefault
            If selNode IsNot Nothing Then
                Dim nCredits = selNode.Descendants("a").Where(Function(f) Not f.InnerText.ToLower = "see more")
                If nCredits IsNot Nothing Then
                    Return nCredits.Select(Function(f) HttpUtility.HtmlDecode(f.InnerText.Trim)).Distinct.ToList.Where(Function(f) Not f.ToLower.StartsWith("see more")).ToList
                End If
            End If
        End If
        Return Nothing
    End Function

    Private Function ParseDirectors(ByRef htmldReference As HtmlDocument) As List(Of String)
        Dim selNodes = htmldReference.DocumentNode.SelectNodes("//div[@class=""titlereference-overview-section""]")
        If selNodes IsNot Nothing Then
            Dim selNode = selNodes.Where(Function(f) f.InnerText.Trim.StartsWith("Director")).FirstOrDefault
            If selNode IsNot Nothing Then
                Dim nDirectors = selNode.Descendants("a").Where(Function(f) Not f.InnerText.ToLower = "see more")
                If nDirectors IsNot Nothing Then
                    Return nDirectors.Select(Function(f) HttpUtility.HtmlDecode(f.InnerText)).Distinct.ToList
                End If
            End If
        End If
        Return Nothing
    End Function

    Private Function ParseDuration(ByRef htmldReference As HtmlDocument) As String
        Dim selNode = htmldReference.DocumentNode.SelectSingleNode("//tr[@class=""ipl-zebra-list__item""]/td[@class=""ipl-zebra-list__label""][.='Runtime']")
        If selNode IsNot Nothing AndAlso selNode.ParentNode IsNot Nothing Then
            Dim nDuration = selNode.ParentNode.Descendants("td").Where(Function(f) Not f.InnerText = "Runtime").FirstOrDefault
            If nDuration IsNot Nothing Then
                Return nDuration.InnerText.Trim
            End If
        End If
        Return Nothing
    End Function

    ''' <summary>
    ''' Returns the title of a title in the forced title language.
    ''' </summary>
    Private Function ParseForcedTitle(ByVal id As String, ByVal originaltitle As String) As String
        If String.IsNullOrEmpty(_SpecialSettings.ForceTitleLanguage) Then Return originaltitle

        Dim data As Dictionary(Of String, Object) = clsIMDBApi.QueryTitle(id)
        If data IsNot Nothing Then
            Dim strTitle As String = GetTitleForLanguage(data, _SpecialSettings.ForceTitleLanguage, String.Empty)
            If Not String.IsNullOrEmpty(strTitle) Then Return strTitle
        End If

        Return originaltitle
    End Function

    ''' <summary>
    ''' Reads the certificate of a title from the GraphQL response.
    ''' The "certificate" field carries the original rating including its country,
    ''' e.g. { "rating": "R", "country": { "id": "US", "text": "United States" } }.
    ''' </summary>
    ''' <param name="data">The GraphQL title object</param>
    ''' <returns>The rating (e.g. "R", "TV-MA") or an empty string</returns>
    Private Function GetCertificateRating(ByVal data As Object) As String
        Try
            Return clsIMDBApi.JStr(clsIMDBApi.JObj(data, "certificate"), "rating")
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return String.Empty
        End Try
    End Function

    ''' <summary>
    ''' Adds the certificate of a title to the certification list.
    ''' The format follows the other scrapers: "&lt;Land&gt;:&lt;FSK&gt;" (e.g. "United States:R").
    ''' </summary>
    ''' <param name="data">The GraphQL title object</param>
    ''' <param name="certifications">The list to add the certification to</param>
    Private Sub ParseCertificate(ByVal data As Object, ByRef certifications As List(Of String))
        Try
            Dim strRating As String = GetCertificateRating(data)
            If String.IsNullOrEmpty(strRating) Then Exit Sub

            Dim strCountry As String = clsIMDBApi.JStr(clsIMDBApi.JObj(clsIMDBApi.JObj(data, "certificate"), "country"), "text")
            Dim strCertification As String = If(String.IsNullOrEmpty(strCountry), strRating, String.Concat(strCountry, ":", strRating))

            If certifications Is Nothing Then Exit Sub
            If Not certifications.Contains(strCertification) Then certifications.Add(strCertification)
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Private Function ParseGenres(ByRef htmldReference As HtmlDocument) As List(Of String)
        Dim selNode = htmldReference.DocumentNode.SelectSingleNode("//tr[@class=""ipl-zebra-list__item""]/td[@class=""ipl-zebra-list__label""][.='Genres']")
        If selNode IsNot Nothing AndAlso selNode.ParentNode IsNot Nothing Then
            Dim ncGenres = selNode.ParentNode.Descendants("a").Where(Function(f) Not f.InnerText.ToLower = "see more")
            If ncGenres IsNot Nothing Then
                Return ncGenres.Select(Function(f) HttpUtility.HtmlDecode(f.InnerText)).Distinct.ToList
            End If
        End If
        Return Nothing
    End Function

    Private Function ParseMPAA(ByRef htmldReference As HtmlDocument, id As String) As String
        'try to get the full MPAA from "Parents Guide" page
        If _SpecialSettings.MPAADescription Then
            Dim webParsing As New HtmlWeb
            Dim htmldParentsGuide As HtmlDocument = webParsing.Load(String.Concat("http://www.imdb.com/title/", id, "/parentalguide"))
            Dim selNode = htmldParentsGuide.DocumentNode.SelectSingleNode("//tr[@class=""ipl-zebra-list__item""]/td[@class=""ipl-zebra-list__label""][.='MPAA']")
            If selNode IsNot Nothing AndAlso selNode.ParentNode IsNot Nothing Then
                Dim ncMPAA = selNode.ParentNode.Descendants("td").Where(Function(f) Not f.InnerText = "MPAA").FirstOrDefault
                If ncMPAA IsNot Nothing AndAlso Not String.IsNullOrEmpty(ncMPAA.InnerText.Trim) Then
                    Return ncMPAA.InnerText.Trim
                End If
            End If
        End If
        If _SpecialSettings.MPAADescription Then logger.Trace(String.Format("[IMDB] [ParseMPAA] [ID:""{0}""] can't parse full MPAA, try to parse the short rating", id))
        'try to get short Rating from "reference" page
        Dim selNodeTitleHeader = htmldReference.DocumentNode.SelectSingleNode("//div[@class=""titlereference-header""]")
        If selNodeTitleHeader IsNot Nothing Then
            Dim selNode = selNodeTitleHeader.Descendants("li").FirstOrDefault
            If selNode IsNot Nothing AndAlso
                        selNode.InnerText.Trim = "G" OrElse
                        selNode.InnerText.Trim = "PG" OrElse
                        selNode.InnerText.Trim = "PG-13" OrElse
                        selNode.InnerText.Trim = "R" OrElse
                        selNode.InnerText.Trim = "NC-17" Then
                Return String.Format("Rated {0}", HttpUtility.HtmlDecode(selNode.InnerText.Trim))
            End If
        End If
        Return Nothing
    End Function

    Private Function ParseOutline(ByRef htmldReference As HtmlDocument, id As String) As String
        Dim selNode = htmldReference.DocumentNode.SelectSingleNode("//section[@class=""titlereference-section-overview""]/div/text()")
        If selNode IsNot Nothing AndAlso selNode.InnerText IsNot Nothing Then
            'get the outline, even if only a part of the outline is available
            Dim strReferenceOutline = HttpUtility.HtmlDecode(selNode.InnerText.Trim)
            'remove the three dots to search the same text on the "plotsummary" page
            Dim strOutlineForSearch As String = Regex.Replace(strReferenceOutline, "\.\.\.", String.Empty)
            If selNode.NextSibling IsNot Nothing AndAlso selNode.NextSibling.InnerText.Trim.ToLower.StartsWith("see more") Then
                'parse the "plotsummary" page for full outline text
                strOutlineForSearch = ParsePlotFromSummaryPage(id, strOutlineForSearch)
                If Not String.IsNullOrEmpty(strOutlineForSearch) Then
                    Return strOutlineForSearch
                Else
                    logger.Trace(String.Format("[IMDB] [ParseOutline] [ID:""{0}""] no result from ""plotsummary"" page for Outline", id))
                End If
            End If
            If Not String.IsNullOrEmpty(strReferenceOutline) AndAlso Not strReferenceOutline.ToLower = "know what this is about?" Then
                Return strReferenceOutline
            End If
        End If
        Return Nothing
    End Function

    Private Function ParsePlot(ByRef htmldReference As HtmlDocument) As String
        Dim selNode = htmldReference.DocumentNode.SelectSingleNode("//tr[@class=""ipl-zebra-list__item""]/td[@class=""ipl-zebra-list__label""][.='Plot Summary']")
        If selNode IsNot Nothing AndAlso selNode.ParentNode IsNot Nothing Then
            Dim ndPlot = selNode.ParentNode.Descendants("p").FirstOrDefault
            If ndPlot IsNot Nothing AndAlso ndPlot.FirstChild IsNot Nothing AndAlso Not String.IsNullOrEmpty(ndPlot.FirstChild.InnerText.Trim) Then
                Return HttpUtility.HtmlDecode(ndPlot.FirstChild.InnerText.Trim)
            End If
        End If
        Return Nothing
    End Function

    Private Function ParsePlotFromSummaryPage(ByVal id As String, Optional ByVal outline As String = "") As String
        If htmldPlotSummary Is Nothing Then
            Dim webParsing As New HtmlWeb
            htmldPlotSummary = webParsing.Load(String.Concat("http://www.imdb.com/title/", id, "/plotsummary"))
        End If
        If htmldPlotSummary IsNot Nothing Then
            Dim selNode As HtmlNode = Nothing
            If Not String.IsNullOrEmpty(outline) Then
                selNode = htmldPlotSummary.DocumentNode.SelectNodes("//p").Where(Function(f) f.InnerText IsNot Nothing AndAlso
                                                                                           HttpUtility.HtmlDecode(f.InnerText.Trim).StartsWith(outline)).FirstOrDefault
            Else
                selNode = htmldPlotSummary.DocumentNode.SelectNodes("//ul[@id=""plot-summaries-content""]/li/p").FirstOrDefault
            End If
            If selNode IsNot Nothing AndAlso Not selNode.InnerText.Trim.ToLower.StartsWith("it looks like we don't have any plot summaries") Then
                Return HttpUtility.HtmlDecode(selNode.InnerText.Trim)
            Else
                logger.Trace(String.Format("[IMDB] [ParsePlotFromSummaryPage] [ID:""{0}""] no proper result from the ""plotsummary"" page for Outline", id))
            End If
        Else
            logger.Trace(String.Format("[IMDB] [ParsePlotFromSummaryPage] [ID:""{0}""] can't parse the ""plotsummary"" page", id))
        End If
        Return String.Empty
    End Function

    Private Sub ParsePosterURL(ByRef htmldReference As HtmlDocument)
        Dim selNode = htmldReference.DocumentNode.SelectSingleNode("//img[@class=""titlereference-primary-image""]")
        If selNode IsNot Nothing Then
            Dim attSource = selNode.Attributes("src")
            If attSource IsNot Nothing Then
                strPosterURL = attSource.Value
            End If
        End If
    End Sub

    Private Function ParseRating(ByRef htmldReference As HtmlDocument) As MediaContainers.RatingDetails
        Dim selNodeRating = htmldReference.DocumentNode.SelectSingleNode("//span[@class=""ipl-rating-star__rating""]")
        Dim selNodeVotes = htmldReference.DocumentNode.SelectSingleNode("//span[@class=""ipl-rating-star__total-votes""]")
        If selNodeRating IsNot Nothing AndAlso selNodeVotes IsNot Nothing Then
            Dim dblRating As Double
            Dim iVotes As Integer
            If Double.TryParse(selNodeRating.InnerText.Trim, Globalization.NumberStyles.AllowDecimalPoint, Globalization.CultureInfo.InvariantCulture, dblRating) AndAlso
                Integer.TryParse(NumUtils.CleanVotes(Regex.Match(selNodeVotes.InnerText.Trim, "[0-9,.]+").Value), iVotes) Then
                Return New MediaContainers.RatingDetails With {
                    .Max = 10,
                    .Type = "imdb",
                    .Value = dblRating,
                    .Votes = iVotes
                }
            End If
        End If
        Return Nothing
    End Function

    Private Function ParseRuntime(ByRef htmldReference As HtmlDocument) As String
        Dim selNode = htmldReference.DocumentNode.SelectSingleNode("//tr[@class=""ipl-zebra-list__item""]/td[@class=""ipl-zebra-list__label""][.='Runtime']")
        If selNode IsNot Nothing AndAlso selNode.ParentNode IsNot Nothing Then
            Dim nDuration = selNode.ParentNode.Descendants("td").Where(Function(f) Not f.InnerText = "Runtime").FirstOrDefault
            If nDuration IsNot Nothing Then
                Return nDuration.InnerText.Trim
            End If
        End If
        Return Nothing
    End Function

    Private Function ParsePremiered(ByRef htmldReference As HtmlDocument, ByRef releasedate As Date) As Boolean
        Dim selNode = htmldReference.DocumentNode.SelectSingleNode("//div[@class=""titlereference-header""]")
        If selNode IsNot Nothing Then
            Dim test = selNode.Descendants("a").Where(Function(f) f.Attributes.Where(Function(a) a.Value.Contains("releaseinfo")).Any)
            Dim nReleaseNode = selNode.Descendants("a").Where(Function(f) f.Attributes.Where(Function(a) a.Value.Contains("releaseinfo")).Any).FirstOrDefault
            If nReleaseNode IsNot Nothing Then
                'find a date like "17 Apr 2017"
                Dim mRelDate = Regex.Match(
                        nReleaseNode.InnerText,
                        "\d+\s\w+\s\d\d\d\d"
                        )
                If mRelDate.Success Then
                    If Date.TryParse(mRelDate.Value, releasedate) Then
                        Return True
                    End If
                End If
            End If
        End If
        Return False
    End Function

    Private Function ParsePremiered(ByVal id As String, ByRef releasedate As Date) As Boolean
        If htmldReleaseInfo Is Nothing Then
            Dim webParsing As New HtmlWeb
            htmldReleaseInfo = webParsing.Load(String.Concat("http://www.imdb.com/title/", id, "/releaseinfo"))
        End If
        If htmldReleaseInfo IsNot Nothing Then
            'get all release dates from table
            Dim selNodes = htmldReleaseInfo.DocumentNode.SelectNodes("//td[@class=""release-date-item__date""]")
            If selNodes IsNot Nothing Then
                Dim lstReleaseDates As New List(Of Date)
                For Each ndReleaseDate In selNodes
                    Dim nDate As New Date
                    If Date.TryParse(ndReleaseDate.InnerText.Trim, nDate) Then
                        lstReleaseDates.Add(nDate)
                    End If
                Next
                'remove douplicates
                lstReleaseDates = lstReleaseDates.Distinct.ToList
                'sort list by date to get to oldest at the first position
                lstReleaseDates.Sort()
                If lstReleaseDates.Count > 0 Then
                    releasedate = lstReleaseDates.First
                    Return True
                End If
            End If
        End If
        Return False
    End Function

    Private Function ParseTagline(ByRef htmldReference As HtmlDocument) As String
        Dim selNode = htmldReference.DocumentNode.SelectSingleNode("//tr[@class=""ipl-zebra-list__item""]/td[@class=""ipl-zebra-list__label""][.='Taglines']")
        If selNode IsNot Nothing AndAlso selNode.ParentNode IsNot Nothing Then
            Dim ndTagline = selNode.ParentNode.Descendants("td").Where(Function(f) Not f.InnerText = "Taglines").FirstOrDefault
            If ndTagline IsNot Nothing AndAlso ndTagline.FirstChild IsNot Nothing AndAlso Not String.IsNullOrEmpty(ndTagline.FirstChild.InnerText.Trim) Then
                Return HttpUtility.HtmlDecode(ndTagline.FirstChild.InnerText.Trim)
            End If
        End If
        Return Nothing
    End Function

    ''' <summary>
    ''' Searches movies on IMDb.
    ''' </summary>
    ''' <remarks>
    ''' The old HTML search of imdb.com can no longer be used, it answers with a bot
    ''' protection challenge. The public suggestion API is used instead. It only
    ''' knows the original titles, so results that do not really match the searched
    ''' title are dropped - otherwise a wrong movie could be scraped.
    ''' </remarks>
    Private Function SearchMovie(ByVal title As String, ByVal year As String) As SearchResults_Movie
        Dim R As New SearchResults_Movie

        Dim strQuery As String = NormalizeSearchTerm(title)
        If String.IsNullOrEmpty(strQuery) Then Return R

        Dim nSourceYear As Integer = GetYearAsInteger(year)

        For Each suggestion In clsIMDBApi.Suggest(strQuery)
            Dim strId As String = clsIMDBApi.JStr(suggestion, "id")
            If Not strId.StartsWith("tt") Then Continue For

            'only movies, no series, shorts or videos
            Dim strType As String = clsIMDBApi.JStr(suggestion, "q")
            If strType <> "feature" AndAlso strType <> "TV movie" Then Continue For

            Dim strSuggestionTitle As String = clsIMDBApi.JStr(suggestion, "l")
            If String.IsNullOrEmpty(strSuggestionTitle) Then Continue For
            Dim strSuggestionYear As String = clsIMDBApi.JStr(suggestion, "y")

            Dim strNormalizedTitle As String = NormalizeSearchTerm(strSuggestionTitle)
            Dim nLev As Integer = StringUtils.ComputeLevenshtein(strQuery, strNormalizedTitle)
            Dim bContains As Boolean = strNormalizedTitle.Contains(strQuery)

            'drop results that do not match the searched title
            If Not bContains AndAlso nLev > 5 Then Continue For

            Dim nMovie As New MediaContainers.Movie With {
                .Lev = nLev,
                .Title = strSuggestionTitle,
                .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.Movie) With {.IMDbId = strId},
                .Year = strSuggestionYear
            }

            'only a result whose title and year fit to the media file is taken over automatically
            If StringUtils.IsSearchMatch(title, nSourceYear, strSuggestionTitle, String.Empty, GetYearAsInteger(strSuggestionYear)) Then
                R.ExactMatches.Add(nMovie)
            Else
                R.PartialMatches.Add(nMovie)
            End If
        Next

        'movies with the wanted year first
        If Not String.IsNullOrEmpty(year) Then
            R.ExactMatches = R.ExactMatches.OrderBy(Function(f) If(f.Year = year, 0, 1)).ToList
            R.PartialMatches = R.PartialMatches.OrderBy(Function(f) If(f.Year = year, 0, 1)).ToList
        End If

        Return R
    End Function

    ''' <summary>
    ''' Converts a year of a media file into a number. 0 is returned if the year is unknown.
    ''' </summary>
    Private Function GetYearAsInteger(ByVal strYear As String) As Integer
        Dim nYear As Integer = 0
        If Not String.IsNullOrEmpty(strYear) Then
            If Not Integer.TryParse(strYear.Trim, nYear) Then nYear = 0
        End If
        Return nYear
    End Function

    ''' <summary>
    ''' Searches the search result that fits best to the title and the year of the media file.
    ''' </summary>
    Private Function GetBestMovieMatch(ByVal strTitle As String, ByVal nYear As Integer, ByVal Matches As List(Of MediaContainers.Movie)) As StringUtils.ScraperSearchCandidate
        Dim candidates As New List(Of StringUtils.ScraperSearchCandidate)
        For nIndex As Integer = 0 To Matches.Count - 1
            candidates.Add(New StringUtils.ScraperSearchCandidate With {
                            .Title = Matches(nIndex).Title,
                            .OriginalTitle = Matches(nIndex).OriginalTitle,
                            .Year = GetYearAsInteger(Matches(nIndex).Year),
                            .Index = nIndex})
        Next
        Return StringUtils.GetBestSearchMatch(strTitle, nYear, candidates)
    End Function

    ''' <summary>
    ''' Searches the search result that fits best to the title and the year of the tv show folder.
    ''' </summary>
    Private Function GetBestTVShowMatch(ByVal strTitle As String, ByVal nYear As Integer, ByVal Matches As List(Of MediaContainers.TVShow)) As StringUtils.ScraperSearchCandidate
        Dim candidates As New List(Of StringUtils.ScraperSearchCandidate)
        For nIndex As Integer = 0 To Matches.Count - 1
            candidates.Add(New StringUtils.ScraperSearchCandidate With {
                            .Title = Matches(nIndex).Title,
                            .OriginalTitle = Matches(nIndex).OriginalTitle,
                            .Year = GetYearAsInteger(Matches(nIndex).Premiered),
                            .Index = nIndex})
        Next
        Return StringUtils.GetBestSearchMatch(strTitle, nYear, candidates)
    End Function

    ''' <summary>
    ''' Normalizes a title for searching and for comparing it with other titles.
    ''' The year is removed and all characters that are no letters or digits are
    ''' replaced by spaces.
    ''' </summary>
    Private Function NormalizeSearchTerm(ByVal value As String) As String
        If String.IsNullOrEmpty(value) Then Return String.Empty
        Dim strFiltered As String = StringUtils.FilterYear(value).ToLower.Trim
        Dim sb As New System.Text.StringBuilder
        For Each c In strFiltered
            If Char.IsLetterOrDigit(c) OrElse c = " " Then
                sb.Append(c)
            Else
                sb.Append(" ")
            End If
        Next
        Dim strResult As String = sb.ToString
        Do While strResult.Contains("  ")
            strResult = strResult.Replace("  ", " ")
        Loop
        Return strResult.Trim
    End Function

    Public Sub SearchMovieAsync(ByVal title As String, ByVal year As String, ByVal filteredoptions As Structures.ScrapeOptions)
        Try
            If Not bwIMDB.IsBusy Then
                bwIMDB.WorkerReportsProgress = False
                bwIMDB.WorkerSupportsCancellation = True
                bwIMDB.RunWorkerAsync(New Arguments With {.Search = SearchType.Movies, .Parameter = title, .Year = year, .Options_Movie = filteredoptions})
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Private Function SearchTVShow(ByVal title As String, Optional ByVal year As String = "") As SearchResults_TVShow
        Dim R As New SearchResults_TVShow

        Dim strQuery As String = NormalizeSearchTerm(title)
        If String.IsNullOrEmpty(strQuery) Then Return R

        'Titel, Jahr, IMDb-ID und Rang (0 = exakter Titel, 1 = Titel enthalten, 2 = Aehnlichkeit)
        Dim candidates As New List(Of Tuple(Of String, String, String, Integer))

        For Each suggestion In clsIMDBApi.Suggest(strQuery)
            Dim strID As String = clsIMDBApi.JStr(suggestion, "id")
            If Not strID.StartsWith("tt") Then Continue For

            Dim strType As String = clsIMDBApi.JStr(suggestion, "q")
            If strType <> "TV series" AndAlso strType <> "TV mini series" Then Continue For

            Dim strSuggestionTitle As String = clsIMDBApi.JStr(suggestion, "l")
            If String.IsNullOrEmpty(strSuggestionTitle) Then Continue For
            Dim strSuggestionYear As String = clsIMDBApi.JStr(suggestion, "y")

            'Nur Treffer behalten, deren Titel zum gesuchten Namen passt
            Dim strNormalized As String = NormalizeSearchTerm(strSuggestionTitle)
            Dim nLev As Integer = StringUtils.ComputeLevenshtein(strQuery, strNormalized)
            Dim bContains As Boolean = strNormalized.Contains(strQuery)
            If Not bContains AndAlso nLev > 5 Then Continue For

            'Das Jahr aus dem Ordnernamen ist das entscheidende Kriterium, wenn
            'mehrere Serien mit gleichem Namen existieren. Eine Toleranz von einem Jahr ist erlaubt.
            If Not StringUtils.IsYearMatch(GetYearAsInteger(year), GetYearAsInteger(strSuggestionYear)) Then Continue For

            Dim nRank As Integer = If(strNormalized = strQuery, 0, If(bContains, 1, 2))
            candidates.Add(Tuple.Create(strSuggestionTitle, strSuggestionYear, strID, nRank))
        Next

        'Die Vorschlags-API sortiert nach Popularitaet. Ohne eigene Sortierung
        'liege z.B. "Ted Lasso" vor "Ted (2024)". Exakter Titel und passendes
        'Jahr haben deshalb Vorrang, die Popularitaet entscheidet nur bei Gleichstand.
        Dim ordered = candidates.OrderBy(Function(f) f.Item4).
                                     ThenBy(Function(f) If(String.IsNullOrEmpty(year), 0, If(f.Item2 = year, 0, 1))).
                                     ToList()

        For Each candidate In ordered
            Dim nMatch As New MediaContainers.TVShow With {
                .Title = candidate.Item1,
                .Premiered = candidate.Item2,
                .UniqueIDs = New MediaContainers.UniqueidContainer(Enums.ContentType.TVShow) With {.ImdbId = candidate.Item3}
            }
            R.Matches.Add(nMatch)
        Next

        logger.Trace(String.Format("[IMDB] [SearchTVShow] query '{0}' year '{1}' -> {2} matches (first: '{3}')",
                                   strQuery, year, R.Matches.Count, If(R.Matches.Count > 0, R.Matches(0).Title, String.Empty)))
        Return R
    End Function

    ''' <summary>
    ''' Reads the year out of a show folder name like "Ted (2024)".
    ''' </summary>
    Private Function GetYearFromShowPath(ByVal showPath As String) As String
        If String.IsNullOrEmpty(showPath) Then Return String.Empty

        Dim strFolder As String = System.IO.Path.GetFileName(showPath.TrimEnd(System.IO.Path.DirectorySeparatorChar))
        Dim m = Regex.Match(strFolder, "\((\d{4})\)")
        If m.Success Then Return m.Groups(1).Value

        m = Regex.Match(strFolder, "[._\s](\d{4})")
        If m.Success Then Return m.Groups(1).Value

        Return String.Empty
    End Function

    Public Sub SearchTVShowAsync(ByVal title As String, ByVal scrapemodifiers As Structures.ScrapeModifiers, ByVal filteredoptions As Structures.ScrapeOptions)

        If Not bwIMDB.IsBusy Then
            bwIMDB.WorkerReportsProgress = False
            bwIMDB.WorkerSupportsCancellation = True
            bwIMDB.RunWorkerAsync(New Arguments With {.Search = SearchType.TVShows,
                  .Parameter = title, .Options_TV = filteredoptions, .ScrapeModifiers = scrapemodifiers})
        End If
    End Sub

#End Region 'Methods

#Region "Nested Types"

    Private Structure Arguments

#Region "Fields"

        Dim FullCast As Boolean
        Dim FullCrew As Boolean
        Dim Options_Movie As Structures.ScrapeOptions
        Dim Options_TV As Structures.ScrapeOptions
        Dim Parameter As String
        Dim ScrapeModifiers As Structures.ScrapeModifiers
        Dim Search As SearchType
        Dim Year As String

#End Region 'Fields

    End Structure

    Private Structure Results

#Region "Fields"

        Dim Result As Object
        Dim ResultType As SearchType

#End Region 'Fields

    End Structure

#End Region 'Nested Types

End Class
