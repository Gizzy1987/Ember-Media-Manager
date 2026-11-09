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

Imports System.Text.RegularExpressions
Imports EmberAPI
Imports NLog

Namespace OFDB

    Public Class Scraper

#Region "Fields"

        Shared logger As Logger = LogManager.GetCurrentClassLogger()

#End Region 'Fields

#Region "Methods"

        Private Function CleanTitle(ByVal sString As String) As String
            Dim CleanString As String = sString

            Try
                If sString.StartsWith("""") Then CleanString = sString.Remove(0, 1)

                If sString.EndsWith("""") Then CleanString = CleanString.Remove(CleanString.Length - 1, 1)

                If sString.EndsWith(", Der") Then
                    CleanString = String.Concat("Der ", sString.Replace(", Der", " ")).Trim
                ElseIf sString.EndsWith(", Die") Then
                    CleanString = String.Concat("Die ", sString.Replace(", Die", " ")).Trim
                ElseIf sString.EndsWith(", Das") Then
                    CleanString = String.Concat("Das ", sString.Replace(", Das", " ")).Trim
                ElseIf sString.EndsWith(", The") Then
                    CleanString = String.Concat("The ", sString.Replace(", The", " ")).Trim
                ElseIf sString.EndsWith(", Ein") Then
                    CleanString = String.Concat("Ein ", sString.Replace(", Ein", " ")).Trim
                ElseIf sString.EndsWith(", Eine") Then
                    CleanString = String.Concat("Eine ", sString.Replace(", Eine", " ")).Trim
                Else
                    CleanString = sString
                End If
            Catch ex As Exception
                logger.Error(ex, New StackFrame().GetMethod().Name)
            End Try
            Return CleanString
        End Function
        ''' <summary>
        ''' Scrapes FSK rating for one given movie from ODFB
        ''' </summary>
        ''' <param name="HTML"><c>String</c> which contains downloaded HTMLcode of moviesite</param>  
        ''' <returns><c>String</c> that contains FSK number (or empty string if nothing was found)</returns>
        ''' <remarks>This one is used for scraping the FSK rating from ODFB
        ''' 
        ''' 2014/06/23 Cocotus - First implementation
        ''' </remarks>
        Private Function GetCertification(ByVal HTML As String) As String
            Dim FSK As String = String.Empty
            Try
                If Not String.IsNullOrEmpty(HTML) Then
                    'the page shows the release as "Freigabe: FSK <value>", e.g. "Freigabe: FSK ab 12"
                    Dim strFSKPattern As String = "Freigabe:\s*FSK\s*(?<FSK>[^<]*)"
                    FSK = HttpUtility.HtmlDecode(Regex.Match(HTML, strFSKPattern, RegexOptions.Singleline).Groups(1).Value).Trim

                    If FSK.Contains("18") Then
                        FSK = "18"
                    ElseIf FSK.Contains("o.A") Then
                        FSK = "0"
                    Else
                        Dim mNumber = Regex.Match(FSK, "(\d{1,2})")
                        If mNumber.Success Then
                            FSK = mNumber.Groups(1).Value
                        Else
                            FSK = String.Empty
                        End If
                    End If

                    If Not Integer.TryParse(FSK, 0) Then
                        FSK = String.Empty
                    End If

                    If Not String.IsNullOrEmpty(FSK) Then
                        FSK = String.Concat("Germany:", FSK)
                    End If
                End If
            Catch ex As Exception
                logger.Error(ex, New StackFrame().GetMethod().Name)
            End Try

            Return FSK
        End Function

        Private Function GetFullPlot(ByVal sURL As String) As String
            Dim FullPlot As String = String.Empty

            Try
                If Not String.IsNullOrEmpty(sURL) Then
                    Dim sHTTP As New HTTP
                    Dim HTML As String = sHTTP.DownloadData(sURL, Text.Encoding.UTF8)
                    sHTTP = Nothing

                    If Not String.IsNullOrEmpty(HTML) Then
                        Dim strPlot As String = String.Empty
                        Dim strPlotPattern = "Eine Inhaltsangabe.*?<br><br>(?<PLOT>.*?)<\/font>"
                        strPlot = HttpUtility.HtmlDecode(Regex.Match(HTML, strPlotPattern, RegexOptions.Singleline).Groups(1).Value).Trim
                        FullPlot = StringUtils.CleanPlotOutline(strPlot)
                    End If
                End If
            Catch ex As Exception
                logger.Error(ex, New StackFrame().GetMethod().Name)
            End Try

            Return FullPlot
        End Function

        Public Function GetMovieInfo(ByVal strIMDBID As String, ByVal FilteredOptions As Structures.ScrapeOptions, ByVal strLanguage As String, Optional ByVal strTitle As String = "", Optional ByVal strOriginalTitle As String = "") As MediaContainers.Movie
            Try
                Dim bIsScraperLanguage As Boolean = strLanguage.ToLower.StartsWith("de")

                Dim nMovie As New MediaContainers.Movie
                nMovie.Scrapersource = "OFDB"

                Dim sURL As String = SearchMovie(strIMDBID)

                If Not String.IsNullOrEmpty(sURL) Then
                    Dim sHTTP As New HTTP
                    Dim HTML As String = sHTTP.DownloadData(sURL, Text.Encoding.UTF8)
                    sHTTP = Nothing

                    If Not String.IsNullOrEmpty(HTML) Then

                        'the movie of the page must fit to the movie of the media file
                        If Not IsMatchingMovie(strTitle, strOriginalTitle, HTML) Then
                            logger.Warn("OFDB: The movie of the page does not fit to '{0}' - nothing was set", If(String.IsNullOrEmpty(strOriginalTitle), strTitle, strOriginalTitle))
                            Return Nothing
                        End If

                        'Certification
                        If FilteredOptions.bMainCertifications Then
                            Dim strCert As String = GetCertification(HTML)
                            If Not String.IsNullOrEmpty(strCert) Then nMovie.Certifications.Add(strCert)
                        End If

                        'Genres
                        If FilteredOptions.bMainGenres Then
                            Dim strGenrePattern As String = "itemprop=""genre"">(?<GENRE>.*?)</span>"
                            Dim gResult As MatchCollection = Regex.Matches(HTML, strGenrePattern, RegexOptions.Singleline)
                            For ctr As Integer = 0 To gResult.Count - 1
                                Dim strGenre As String = HttpUtility.HtmlDecode(gResult.Item(ctr).Groups(1).Value.Trim)
                                If Not String.IsNullOrEmpty(strGenre) AndAlso Not nMovie.Genres.Contains(strGenre) Then nMovie.Genres.Add(strGenre)
                            Next
                        End If

                        'Countries
                        If FilteredOptions.bMainCountries Then
                            Dim strCountryPattern As String = "itemprop=""countryOfOrigin"">(?<COUNTRY>.*?)</span>"
                            Dim cResult As MatchCollection = Regex.Matches(HTML, strCountryPattern, RegexOptions.Singleline)
                            For ctr As Integer = 0 To cResult.Count - 1
                                Dim strCountry As String = HttpUtility.HtmlDecode(cResult.Item(ctr).Groups(1).Value.Trim)
                                If Not String.IsNullOrEmpty(strCountry) AndAlso Not nMovie.Countries.Contains(strCountry) Then nMovie.Countries.Add(strCountry)
                            Next
                        End If

                        'Original Title
                        If FilteredOptions.bMainOriginalTitle Then
                            Dim strOriginalTitlePattern As String = "Originaltitel:</b></dt>\s*<dd>(?<OTITLE>.*?)</dd>"
                            nMovie.OriginalTitle = CleanTitle(HttpUtility.HtmlDecode(Regex.Match(HTML, strOriginalTitlePattern, RegexOptions.Singleline).Groups(1).Value.ToString.Trim))
                        End If

                        'Plot
                        If FilteredOptions.bMainPlot AndAlso bIsScraperLanguage Then
                            Dim strPlotPattern As String = "itemprop=""description"">(?<PLOT>.*?)</span>"
                            Dim strPlot As String = HttpUtility.HtmlDecode(Regex.Match(HTML, strPlotPattern, RegexOptions.Singleline).Groups(1).Value).Trim
                            If Not String.IsNullOrEmpty(strPlot) AndAlso
                                    Not strPlot.ToLower.Contains("keine inhaltsangabe") AndAlso
                                    Not strPlot.ToLower.Contains("inhaltsangabe") Then
                                nMovie.Plot = StringUtils.CleanPlotOutline(strPlot)
                            End If
                        End If

                        'Title
                        If FilteredOptions.bMainTitle AndAlso bIsScraperLanguage Then
                            Dim strTitlePattern As String = "itemprop=""name"">(?<TITLE>.*?)(?:\s*\(\d{4}\))?</h1>"
                            nMovie.Title = CleanTitle(HttpUtility.HtmlDecode(Regex.Match(HTML, strTitlePattern, RegexOptions.Singleline).Groups(1).Value.ToString.Trim))
                        End If
                    End If
                End If

                Return nMovie
            Catch ex As Exception
                logger.Error(ex, New StackFrame().GetMethod().Name)
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Checks if the movie of the OFDB page fits to the movie of the media file.
        ''' The title of the page and the original title of the page are compared with
        ''' the title and the original title of the media file.
        ''' </summary>
        Private Function IsMatchingMovie(ByVal strTitle As String, ByVal strOriginalTitle As String, ByVal HTML As String) As Boolean
            'no title to compare with -> nothing to check
            If String.IsNullOrEmpty(strTitle) AndAlso String.IsNullOrEmpty(strOriginalTitle) Then Return True

            Dim strPageTitle As String = HttpUtility.HtmlDecode(Regex.Match(HTML, "itemprop=""name"">(?<TITLE>.*?)(?:\s*\(\d{4}\))?</h1>", RegexOptions.Singleline).Groups("TITLE").Value.ToString.Trim)
            Dim strPageOriginalTitle As String = HttpUtility.HtmlDecode(Regex.Match(HTML, "Originaltitel:</b></dt>\s*<dd>(?<OTITLE>.*?)</dd>", RegexOptions.Singleline).Groups("OTITLE").Value.ToString.Trim)

            For Each strSource As String In New String() {strTitle, strOriginalTitle}
                If String.IsNullOrEmpty(strSource) Then Continue For
                If StringUtils.IsTitleMatch(strSource, strPageTitle) Then Return True
                If StringUtils.IsTitleMatch(strSource, strPageOriginalTitle) Then Return True
            Next

            Return False
        End Function

        ''' <summary>
        ''' Searches the movie on OFDb and returns the URL of the movie page.
        ''' </summary>
        ''' <remarks>
        ''' The old search pages of ofdb.de do not work anymore, the search is done
        ''' by the quick search of the website. It answers with JSON.
        ''' </remarks>
        Private Function SearchMovie(ByVal strIMDBID As String) As String
            Dim strURL As String = String.Empty
            Try
                If Not String.IsNullOrEmpty(strIMDBID) Then
                    Dim sHTTP As New HTTP
                    Dim HTML As String = sHTTP.DownloadData(String.Concat("https://www.ofdb.de/quicksearch.php?term=", HttpUtility.UrlEncode(strIMDBID)), Text.Encoding.UTF8)
                    sHTTP = Nothing

                    If Not String.IsNullOrEmpty(HTML) Then
                        'the answer is a JSON array of objects with the fields "category", "value" and "url"
                        'the answer contains the url with escaped slashes and unicode escapes ("\u25cf"),
                        'so the id of the movie is taken and the url is built from it
                        Dim resResult = Regex.Match(HTML, "film\\?/(?<FID>\d+)")
                        If resResult.Success Then
                            strURL = String.Concat("https://www.ofdb.de/film/", resResult.Groups(1).Value, "/")
                        End If

                        If String.IsNullOrEmpty(strURL) Then
                            logger.Warn("OFDB Query returned no results for ID of <{0}>", strIMDBID)
                        End If
                    Else
                        logger.Warn("OFDB Query returned no results for ID of <{0}>", strIMDBID)
                    End If
                End If
            Catch ex As Exception
                logger.Error(ex, New StackFrame().GetMethod().Name & Convert.ToChar(Keys.Tab) & "Error scraping ODFB (too many connections?):" & strIMDBID)
            End Try

            Return strURL
        End Function

#End Region 'Methods

    End Class

End Namespace