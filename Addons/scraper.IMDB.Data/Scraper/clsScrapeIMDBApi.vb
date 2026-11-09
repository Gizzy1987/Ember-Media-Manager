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
' # but WITHOUT ANY WARRANTY; without even the implied warranty of                #
' # MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                #
' # GNU General Public License for more details.                                 #
' #                                                                              #
' # You should have received a copy of the GNU General Public License            #
' # along with Ember Media Manager.  If not, see <http://www.gnu.org/licenses/>. #
' ################################################################################

Imports NLog
Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Web
Imports System.Web.Script.Serialization

''' <summary>
''' Client for the official IMDb GraphQL API and for the public IMDb suggestion API.
''' </summary>
''' <remarks>
''' The classic imdb.com HTML pages can no longer be scraped: they answer with a
''' bot protection challenge (HTTP 202 with an empty body) and the old
''' "/reference" page does not contain the markup the scraper was written for.
''' The GraphQL API used by the website itself returns the same data as JSON and
''' only needs a browser user agent plus an "Origin" header.
''' </remarks>
Public Class clsIMDBApi

#Region "Fields"

    Shared logger As Logger = LogManager.GetCurrentClassLogger()

    Public Const GRAPHQL_URL As String = "https://api.graphql.imdb.com/"
    Public Const SUGGESTION_URL As String = "https://v3.sg.media-imdb.com/suggestion/x/"

    Private Const USER_AGENT As String = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"

    Private Const DQ As String = """"
    Private Const BS As String = "\"

    ''' <summary>
    ''' All fields of a movie that are needed by Ember.
    ''' </summary>
    Public Const TITLE_FIELDS As String = "id titleText { text } originalTitleText { text } " &
                                          "releaseYear { year } releaseDate { day month year } runtime { seconds } " &
                                          "plot { plotText { plainText } } " &
                                          "plots(first: 5) { edges { node { plotText { plainText } } } } " &
                                          "taglines(first: 1) { edges { node { text } } } " &
                                          "genres { genres { text } } " &
                                          "ratingsSummary { aggregateRating voteCount } " &
                                          "countriesOfOrigin { countries { id text } } " &
                                          "releaseDates(first: 25) { edges { node { country { id text } day month year } } } " &
                                          "credits(first: 150) { edges { node { name { id nameText { text } } category { id } ... on Cast { characters { name } } } } } " &
                                          "principalCredits { credits { name { id nameText { text } } category { id } } } " &
                                          "companyCredits(first: 25) { edges { node { company { id companyText { text } } category { id text } } } } " &
                                          "akas(first: 40) { edges { node { text country { id text } } } } " &
                                          "certificate { rating country { id text } attributes { text } } " &
                                          "primaryImage { url width height } " &
                                          "titleType { id text }"

    ''' <summary>
    ''' All fields of a TV show (series) that are needed by Ember.
    ''' </summary>
    Public Const SHOW_FIELDS As String = "id titleText { text } originalTitleText { text } " &
                                          "releaseYear { year } releaseDate { day month year } runtime { seconds } " &
                                          "plot { plotText { plainText } } plots(first: 5) { edges { node { plotText { plainText } } } } " &
                                          "taglines(first: 1) { edges { node { text } } } " &
                                          "genres { genres { text } } ratingsSummary { aggregateRating voteCount } " &
                                          "countriesOfOrigin { countries { id text } } " &
                                          "credits(first: 60) { edges { node { name { id nameText { text } } category { id } ... on Cast { characters { name } } } } } " &
                                          "principalCredits { credits { name { id nameText { text } } category { id } } } " &
                                          "companyCredits(first: 10) { edges { node { company { id companyText { text } } category { id } } } } " &
                                          "akas(first: 40) { edges { node { text country { id text } } } } " &
                                          "certificate { rating country { id text } attributes { text } } " &
                                          "primaryImage { url width height } titleType { id text } " &
                                          "episodes { episodes(first: 250) { edges { node { id titleText { text } " &
                                          "series { episodeNumber { seasonNumber episodeNumber } } " &
                                          "releaseDate { day month year } runtime { seconds } " &
                                          "ratingsSummary { aggregateRating voteCount } plot { plotText { plainText } } " &
                                          "credits(first: 20) { edges { node { name { id nameText { text } } category { id } ... on Cast { characters { name } } } } } } } } }"

    ''' <summary>
    ''' All fields of a single episode that are needed by Ember.
    ''' </summary>
    Public Const EPISODE_FIELDS As String = "id titleText { text } originalTitleText { text } " &
                                            "series { episodeNumber { seasonNumber episodeNumber } } " &
                                            "releaseDate { day month year } runtime { seconds } " &
                                            "plot { plotText { plainText } } " &
                                            "ratingsSummary { aggregateRating voteCount } " &
                                            "credits(first: 30) { edges { node { name { id nameText { text } } category { id } ... on Cast { characters { name } } } } } " &
                                            "akas(first: 20) { edges { node { text country { id text } } } } " &
                                            "primaryImage { url width height }"

#End Region 'Fields

#Region "Methods"

    ''' <summary>
    ''' Queries all data of a movie.
    ''' </summary>
    Public Shared Function QueryTitle(ByVal id As String) As Dictionary(Of String, Object)
        Return QueryTitle(id, TITLE_FIELDS)
    End Function

    ''' <summary>
    ''' Queries all data of a TV show (series) including its episodes.
    ''' </summary>
    Public Shared Function QueryShow(ByVal id As String) As Dictionary(Of String, Object)
        If String.IsNullOrEmpty(id) Then Return Nothing
        Return QueryTitle(id, SHOW_FIELDS)
    End Function

    ''' <summary>
    ''' Queries all data of a single episode.
    ''' </summary>
    Public Shared Function QueryEpisode(ByVal id As String) As Dictionary(Of String, Object)
        If String.IsNullOrEmpty(id) Then Return Nothing
        Return QueryTitle(id, EPISODE_FIELDS)
    End Function

    ''' <summary>
    ''' Queries a title with a free field set.
    ''' </summary>
    Public Shared Function QueryTitle(ByVal id As String, ByVal fields As String) As Dictionary(Of String, Object)
        If String.IsNullOrEmpty(id) Then Return Nothing

        Try
            Dim strGraphQL As String = String.Format("query {{ title(id: ""{0}"") {{ {1} }} }}", id, fields)
            Dim strBody As String = "{" & DQ & "query" & DQ & ":" & DQ & strGraphQL.Replace(DQ, BS & DQ) & DQ & "}"

            Dim strResponse As String = PostJson(GRAPHQL_URL, strBody)
            If String.IsNullOrEmpty(strResponse) Then Return Nothing

            Dim root = TryCast(Deserialize(strResponse), Dictionary(Of String, Object))
            If root Is Nothing Then Return Nothing

            If root.ContainsKey("errors") Then
                logger.Error(String.Format("[IMDB] [API] GraphQL error for ""{0}"": {1}", id, root("errors").ToString()))
                Return Nothing
            End If

            Return JObj(JObj(root, "data"), "title")
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Returns all episodes of a TV show.
    ''' </summary>
    Public Shared Function GetShowEpisodes(ByVal id As String) As List(Of Dictionary(Of String, Object))
        Dim result As New List(Of Dictionary(Of String, Object))
        If String.IsNullOrEmpty(id) Then Return result

        Dim data = QueryShow(id)
        If data Is Nothing Then Return result

        For Each node In JEdges(JObj(JObj(data, "episodes"), "episodes"))
            result.Add(node)
        Next

        Return result
    End Function

    ''' <summary>
    ''' Deserializes a JSON string.
    ''' </summary>
    Public Shared Function Deserialize(ByVal json As String) As Object
        If String.IsNullOrEmpty(json) Then Return Nothing
        Try
            Dim serializer As New JavaScriptSerializer
            serializer.MaxJsonLength = Int32.MaxValue
            serializer.RecursionLimit = 100
            Return serializer.DeserializeObject(json)
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Returns a child object of a JSON object.
    ''' </summary>
    Public Shared Function JObj(ByVal source As Object, ByVal key As String) As Dictionary(Of String, Object)
        Dim d = TryCast(source, Dictionary(Of String, Object))
        If d Is Nothing Then Return Nothing
        If Not d.ContainsKey(key) Then Return Nothing
        Return TryCast(d(key), Dictionary(Of String, Object))
    End Function

    ''' <summary>
    ''' Returns a child array of a JSON object.
    ''' </summary>
    Public Shared Function JArr(ByVal source As Object, ByVal key As String) As List(Of Object)
        Dim result As New List(Of Object)
        Dim d = TryCast(source, Dictionary(Of String, Object))
        If d Is Nothing Then Return result
        If Not d.ContainsKey(key) Then Return result
        Dim arr = TryCast(d(key), Object())
        If arr Is Nothing Then Return result
        result.AddRange(arr)
        Return result
    End Function

    ''' <summary>
    ''' Returns the "node" of every entry of a GraphQL "edges" array.
    ''' </summary>
    Public Shared Function JEdges(ByVal source As Object) As List(Of Dictionary(Of String, Object))
        Dim result As New List(Of Dictionary(Of String, Object))
        For Each edge In JArr(source, "edges")
            Dim node = JObj(edge, "node")
            If node IsNot Nothing Then result.Add(node)
        Next
        Return result
    End Function

    ''' <summary>
    ''' Returns a string value of a JSON object.
    ''' </summary>
    Public Shared Function JStr(ByVal source As Object, ByVal key As String) As String
        Dim d = TryCast(source, Dictionary(Of String, Object))
        If d Is Nothing Then Return String.Empty
        If Not d.ContainsKey(key) Then Return String.Empty
        Dim value = d(key)
        If value Is Nothing Then Return String.Empty
        If TypeOf value Is String Then Return DirectCast(value, String)
        Return Convert.ToString(value, CultureInfo.InvariantCulture)
    End Function

    ''' <summary>
    ''' Returns a numeric value of a JSON object, -1 if the value is missing.
    ''' </summary>
    Public Shared Function JNum(ByVal source As Object, ByVal key As String) As Double
        Dim d = TryCast(source, Dictionary(Of String, Object))
        If d Is Nothing Then Return -1
        If Not d.ContainsKey(key) Then Return -1
        Dim value = d(key)
        If value Is Nothing Then Return -1
        Try
            Return Convert.ToDouble(value, CultureInfo.InvariantCulture)
        Catch
            Return -1
        End Try
    End Function

    Private Shared Function PostJson(ByVal url As String, ByVal body As String) As String
        Try
            Dim request = DirectCast(WebRequest.Create(url), HttpWebRequest)
            request.Method = "POST"
            request.ContentType = "application/json"
            request.Accept = "application/json"
            request.UserAgent = USER_AGENT
            request.Referer = "https://www.imdb.com/"
            request.Timeout = 30000
            request.ReadWriteTimeout = 30000
            request.KeepAlive = False
            request.AutomaticDecompression = DecompressionMethods.GZip Or DecompressionMethods.Deflate
            request.Headers.Add("Origin", "https://www.imdb.com")
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9")

            Dim data = Encoding.UTF8.GetBytes(body)
            request.ContentLength = data.Length
            Using stream = request.GetRequestStream()
                stream.Write(data, 0, data.Length)
            End Using

            Using response = DirectCast(request.GetResponse(), HttpWebResponse)
                Using reader = New StreamReader(response.GetResponseStream(), Encoding.UTF8)
                    Return reader.ReadToEnd()
                End Using
            End Using
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return String.Empty
        End Try
    End Function

    Private Shared Function GetJson(ByVal url As String) As String
        Try
            Dim request = DirectCast(WebRequest.Create(url), HttpWebRequest)
            request.Method = "GET"
            request.Accept = "application/json"
            request.UserAgent = USER_AGENT
            request.Referer = "https://www.imdb.com/"
            request.Timeout = 30000
            request.KeepAlive = False
            request.AutomaticDecompression = DecompressionMethods.GZip Or DecompressionMethods.Deflate

            Using response = DirectCast(request.GetResponse(), HttpWebResponse)
                Using reader = New StreamReader(response.GetResponseStream(), Encoding.UTF8)
                    Return reader.ReadToEnd()
                End Using
            End Using
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return String.Empty
        End Try
    End Function

    ''' <summary>
    ''' Searches titles with the public IMDb suggestion API.
    ''' </summary>
    Public Shared Function Suggest(ByVal query As String) As List(Of Dictionary(Of String, Object))
        Dim result As New List(Of Dictionary(Of String, Object))
        If String.IsNullOrEmpty(query) Then Return result

        Try
            Dim strResponse As String = GetJson(String.Concat(SUGGESTION_URL, HttpUtility.UrlEncode(query, Encoding.UTF8), ".json"))
            If String.IsNullOrEmpty(strResponse) Then Return result

            Dim root = TryCast(Deserialize(strResponse), Dictionary(Of String, Object))
            If root Is Nothing Then Return result

            For Each item In JArr(root, "d")
                Dim d = TryCast(item, Dictionary(Of String, Object))
                If d IsNot Nothing Then result.Add(d)
            Next
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try

        Return result
    End Function

#End Region 'Methods

End Class
