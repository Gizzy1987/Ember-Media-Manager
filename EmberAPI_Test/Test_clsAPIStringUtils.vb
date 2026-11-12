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

Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Drawing

Imports EmberAPI
Imports UnitTests

Namespace EmberTests

    <TestClass()> Public Class Test_clsAPIStringUtils_StringUtils

        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_AlphaNumericOnly_VariousUnicode_NoSpecial()
            'Arrange
            Dim source As New Dictionary(Of Char, Boolean)
            With source
                .Add("A"c, True)    'Letter, Uppercase http://www.fileformat.info/info/unicode/category/Lu/index.htm
                .Add("Z"c, True)    'Letter, Uppercase http://www.fileformat.info/info/unicode/category/Lu/index.htm
                .Add("a"c, True)    'Letter, Lowercase http://www.fileformat.info/info/unicode/category/Ll/index.htm
                .Add("z"c, True)    'Letter, Lowercase http://www.fileformat.info/info/unicode/category/Ll/index.htm
                .Add("1"c, True)    'Number, Decimal Digit http://www.fileformat.info/info/unicode/category/Nd/index.htm
                .Add("9"c, True)    'Number, Decimal Digit http://www.fileformat.info/info/unicode/category/Nd/index.htm
                .Add("0"c, True)    'Number, Decimal Digit http://www.fileformat.info/info/unicode/category/Nd/index.htm
                .Add(ChrW(&H1C5), True) 'Letter, Uppercase http://www.fileformat.info/info/unicode/category/Lu/index.htm
                .Add(ChrW(&H2E0), True) 'Letter, Modifier http://www.fileformat.info/info/unicode/category/Lm/index.htm
                .Add(ChrW(&H5DA), True) 'Letter, Other http://www.fileformat.info/info/unicode/category/Lo/index.htm
                .Add(ChrW(&H6F5), True) 'Number, Decimal Digit http://www.fileformat.info/info/unicode/category/Nd/index.htm
                .Add(ChrW(0), False)    'Other, Control http://www.fileformat.info/info/unicode/category/Cc/index.htm
                .Add("["c, False)   'Punctuation, Open http://www.fileformat.info/info/unicode/category/Ps/index.htm
                .Add(" "c, False)   'Separator, Space http://www.fileformat.info/info/unicode/category/Zs/index.htm
                .Add(ChrW(&HA0), False)  'Separator, Space http://www.fileformat.info/info/unicode/category/Zs/index.htm
                .Add(ChrW(&H2028), False)   'Separator, Line http://www.fileformat.info/info/unicode/category/Zl/index.htm
                .Add("+"c, False)   'Symbol, Math http://www.fileformat.info/info/unicode/category/Sm/index.htm
                .Add("$"c, False)   'Special Asc 36
                .Add("%"c, False)   'Special Asc 37
                .Add("&"c, False)   'Special Asc 38
                .Add(","c, False)   'Special Asc 44
                .Add("-"c, False)   'Special Asc 45
                .Add("."c, False)   'Special Asc 46
                .Add(":"c, False)   'Special Asc 58
                .Add(")"c, False)   'Special Asc 41
            End With

            For Each pair As KeyValuePair(Of Char, Boolean) In source
                'Act
                Dim returnValue As Boolean = StringUtils.AlphaNumericOnly(pair.Key, False)

                'Assert
                Assert.AreEqual(pair.Value, returnValue, "Expected {0}, but received {1} for value: U+{2:X4} ({3})", pair.Value, returnValue, Convert.ToUInt16(pair.Key), pair.Key)
            Next
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_AlphaNumericOnly_VariousUnicode_WithSpecial()
            'Arrange
            Dim source As New Dictionary(Of Char, Boolean)
            With source
                .Add("A"c, True)    'Letter, Uppercase http://www.fileformat.info/info/unicode/category/Lu/index.htm
                .Add("Z"c, True)    'Letter, Uppercase http://www.fileformat.info/info/unicode/category/Lu/index.htm
                .Add("a"c, True)    'Letter, Lowercase http://www.fileformat.info/info/unicode/category/Ll/index.htm
                .Add("z"c, True)    'Letter, Lowercase http://www.fileformat.info/info/unicode/category/Ll/index.htm
                .Add("1"c, True)    'Number, Decimal Digit http://www.fileformat.info/info/unicode/category/Nd/index.htm
                .Add("9"c, True)    'Number, Decimal Digit http://www.fileformat.info/info/unicode/category/Nd/index.htm
                .Add("0"c, True)    'Number, Decimal Digit http://www.fileformat.info/info/unicode/category/Nd/index.htm
                .Add(ChrW(&H1C5), True) 'Letter, Uppercase http://www.fileformat.info/info/unicode/category/Lu/index.htm
                .Add(ChrW(&H2E0), True) 'Letter, Modifier http://www.fileformat.info/info/unicode/category/Lm/index.htm
                .Add(ChrW(&H5DA), True) 'Letter, Other http://www.fileformat.info/info/unicode/category/Lo/index.htm
                .Add(ChrW(&H6F5), True) 'Number, Decimal Digit http://www.fileformat.info/info/unicode/category/Nd/index.htm
                .Add(ChrW(0), True) 'Other, Control http://www.fileformat.info/info/unicode/category/Cc/index.htm
                .Add("["c, False)   'Punctuation, Open http://www.fileformat.info/info/unicode/category/Ps/index.htm
                .Add(" "c, True)    'Separator, Space http://www.fileformat.info/info/unicode/category/Zs/index.htm
                .Add(ChrW(&HA0), True)  'Separator, Space http://www.fileformat.info/info/unicode/category/Zs/index.htm
                .Add(ChrW(&H2028), True)    'Separator, Line http://www.fileformat.info/info/unicode/category/Zl/index.htm
                .Add("+"c, False)   'Symbol, Math http://www.fileformat.info/info/unicode/category/Sm/index.htm
                .Add("$"c, False)   'Special Asc 36
                .Add("%"c, False)   'Special Asc 37
                .Add("&"c, False)   'Special Asc 38
                .Add(","c, True)    'Special Asc 44
                .Add("-"c, True)    'Special Asc 45
                .Add("."c, True)    'Special Asc 46
                .Add(":"c, True)    'Special Asc 58
                .Add(")"c, False)   'Special Asc 41
            End With

            For Each pair As KeyValuePair(Of Char, Boolean) In source
                'Act
                Dim returnValue As Boolean = StringUtils.AlphaNumericOnly(pair.Key, True)
                Dim val As Integer = Convert.ToUInt16(pair.Key)
                'Assert
                Assert.AreEqual(pair.Value, returnValue, "Expected {0}, but received {1} for value: U+{2:X4} ({3})", pair.Value, returnValue, Convert.ToUInt16(pair.Key), pair.Key)
            Next
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_AlphaNumericOnly_NothingChar_NoSpecial()
            'Arrange
            'Act
            Dim returnValue As Boolean = StringUtils.AlphaNumericOnly(Nothing, False)
            'Assert
            Assert.IsFalse(returnValue, "Expected False, but received True, for KeyChar of Nothing")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_AlphaNumericOnly_NothingChar_WithSpecial()
            'Arrange
            'NOTE that this test will PASS because the Nothing will be interpreted as a 0 (default value for Char)
            'and since 0 is a valid Control value (accepted with Specials) this will pass.
            'Act
            Dim returnValue As Boolean = StringUtils.AlphaNumericOnly(Nothing, True)
            'Assert
            Assert.IsTrue(returnValue, "Expected True, but received False, for KeyChar of Nothing")
        End Sub
        'Hinweis: APIXML.CacheXMLs und StringUtils.GenreFilter existieren in EmberAPI nicht mehr
        'und haben keinen Ersatz. Die vier zugehoerigen Tests wurden deshalb entfernt.
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_RemoveStackingMarkers()
            'Arrange
            'Die heutige Funktion lautet FileUtils.Common.RemoveStackingMarkers und arbeitet
            'auf einem vollstaendigen Pfad. Sie entfernt den ersten Stacking-Marker
            '(- cd1, .pt1, .part2, .dvd2, .disc3, .disk4 usw.) aus dem Dateinamen.
            'Ordner-Marker (z.B. "Film CD1") werden bewusst NICHT entfernt, weil das
            'Muster zwingend auf eine Dateiendung zeigen muss.
            Dim source As New Dictionary(Of String, String)
            With source
                .Add(String.Empty, String.Empty)
                .Add("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.avi", "C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.avi")   'Kein Marker - bleibt unveraendert
                .Add("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.cd1.mkv", "C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.mkv")
                .Add("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.pt1.mkv", "C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.mkv")
                .Add("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.part2.mkv", "C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.mkv")
                .Add("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.dvd2.mkv", "C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.mkv")
                .Add("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.disc3.mkv", "C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.mkv")
                .Add("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.disk4.mkv", "C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.mkv")
                .Add("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.cc1.mkv", "C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.cc1.mkv")  '"cc" ist kein Stacking-Marker
                .Add("C:\Movies\Yodeling In The Alps - Part 2\yodeling.in.the.alps.pt1.mkv", "C:\Movies\Yodeling In The Alps - Part 2\yodeling.in.the.alps.mkv")  'Nur der erste Marker wird entfernt
                .Add("C:\Movies\Film CD1\film.mkv", "C:\Movies\Film CD1\film.mkv")    'Ordnermarker werden nicht entfernt
            End With

            For Each entry As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = FileUtils.Common.RemoveStackingMarkers(entry.Key)
                'Assert
                Assert.AreEqual(entry.Value, returnValue, "Source <{0}>", entry.Key)
            Next
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_CleanURL()
            'Arrange

            'Act

            'Assert
            Assert.Inconclusive("Test not implemented")
        End Sub
        ''' <summary>
        ''' Internal structure to assist testing with Levenshtein.
        ''' The values are the two strings to be compared,
        ''' and the levenshtein is the expected Levenshtein result.
        ''' </summary>
        ''' <remarks>No validation, as this is all internal</remarks>
        Friend Structure LevenshteinData
            Dim value1 As String
            Dim value2 As String
            Dim levenshtein As Integer


            Sub New(val1 As String, val2 As String, lev As Integer)
                value1 = val1
                value2 = val2
                levenshtein = lev
            End Sub
        End Structure
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_ComputeLevenshtein()
            'Arrange
            Dim source As New List(Of LevenshteinData)
            With source
                .Add(New LevenshteinData(Nothing, "airplane", 100))
                .Add(New LevenshteinData("airplane", Nothing, 8))
                .Add(New LevenshteinData(Nothing, Nothing, 100))
                .Add(New LevenshteinData("planes", "airplane", 4))
                .Add(New LevenshteinData("airplane", "planes", 4))
                .Add(New LevenshteinData("automobile", "automobile", 0))
                .Add(New LevenshteinData("airbender", "aIrBenDER", 0))
                .Add(New LevenshteinData("airbender", "Avatar: The Last Airbender", 17))
                .Add(New LevenshteinData("airbender", "The Last Airbender", 9))
                .Add(New LevenshteinData("Airbender", "The Last Airbender: Siege of the North", 29))
            End With

            For Each entry As LevenshteinData In source
                'Act
                Dim returnValue As Integer = StringUtils.ComputeLevenshtein(entry.value1, entry.value2)
                'Assert
                Assert.AreEqual(entry.levenshtein, returnValue, "Source <{0}> and <{1}>", entry.value1, entry.value2)
            Next
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_Decode_NothingParameter()
            'Arrange
            Dim returnValue As String = StringUtils.Decode(Nothing)
            'Assert
            Assert.AreEqual(String.Empty, returnValue, "Source was Nothing")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_Decode()
            'Arrange
            'VERY IMPORTANT NOTE: Source strings are identical to the Encode example,
            'however the value is used as the source string, and the key value is used as the expected result.
            'This is opposite of normal, but simplifies reverse testing!

            'Additional Note: Some random whitespaces in the Value should be inserted to test the decode routine's robustness
            Dim source As New Dictionary(Of String, String)
            With source
                .Add(String.Empty, String.Empty)
                .Add("asdf", "YXNkZg==")
                .Add("asdf1", "YXN kZjE=")
                .Add("asdf12", "YXNkZjEy")
                .Add("asdf123", vbTab & "YXNkZjEyMw  ==")
                .Add("asdf1234", "YXNkZjEyMzQ=")
                .Add("a sdf", "YSBzZGY=")
                .Add("as" & vbTab & "df", "YXMJZGY=")
                .Add("asd" & vbCrLf & "f", "YXNkDQpm")
            End With

            For Each entry As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.Decode(entry.Value)
                'Assert
                Assert.AreEqual(entry.Key, returnValue, "Source <{0}>", entry.Value)
            Next

        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_Encode_NothingParameter()
            'Arrange
            Dim returnValue As String = StringUtils.Encode(Nothing)
            'Assert
            Assert.AreEqual(String.Empty, returnValue, "Source was Nothing")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_Encode()
            'Arrange
            Dim source As New Dictionary(Of String, String)
            With source
                .Add(String.Empty, String.Empty)
                .Add("asdf", "YXNkZg==")
                .Add("asdf1", "YXNkZjE=")
                .Add("asdf12", "YXNkZjEy")
                .Add("asdf123", "YXNkZjEyMw==")
                .Add("asdf1234", "YXNkZjEyMzQ=")
                .Add("a sdf", "YSBzZGY=")
                .Add("as" & vbTab & "df", "YXMJZGY=")
                .Add("asd" & vbCrLf & "f", "YXNkDQpm")
            End With

            For Each entry As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.Encode(entry.Key)
                'Assert
                Assert.AreEqual(entry.Value, returnValue, "Source <{0}>", entry.Key)
            Next

        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_FilterTitleFromPath_Movie_NothingParameter()
            'Arrange
            'Act
            Dim returnValue As String = StringUtils.FilterTitleFromPath_Movie(Nothing, False, False)
            'Assert
            Assert.AreEqual(String.Empty, returnValue, "Source was Nothing")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_FilterTitleFromPath_Movie_AusDateiname()
            'Arrange
            'Die heutige Funktion lautet StringUtils.FilterTitleFromPath_Movie und bekommt
            'einen vollstaendigen Pfad. Die beiden Flags bestimmen, ob der Dateiname oder
            'der Ordnername verwendet wird - die alten Flags fuer "Extras" und
            '"Satzzeichen entfernen" gibt es nicht mehr.
            'Es werden die Standardfilter der Einstellungen verwendet
            '(Jahresangabe, Aufloesung, BluRay usw. werden entfernt).
            Dim source As New Dictionary(Of String, String)
            With source
                .Add("C:\Movies\The Movie (2013)\The Movie (2013).mkv", "The Movie")
                .Add("C:\Movies\Some.Movie.1080p.BluRay.x264\Some.Movie.1080p.BluRay.x264.mkv", "Some Movie")
            End With

            For Each entry As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.FilterTitleFromPath_Movie(entry.Key, False, False)
                'Assert
                Assert.AreEqual(entry.Value, returnValue, "Source <{0}>", entry.Key)
            Next

        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_FilterTitleFromPath_Movie_AusOrdnername()
            'Arrange
            'Bei IsSingle = True und UseForderName = True wird der Name des
            'Ordners statt des Dateinamens verwendet.
            Dim source As New Dictionary(Of String, String)
            With source
                .Add("C:\Movies\Best.Movie.1080p\Best.Movie.mkv", "Best Movie")
                .Add("C:\Movies\The Movie (2013)\The Movie (2013).mkv", "The Movie")
            End With

            For Each entry As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.FilterTitleFromPath_Movie(entry.Key, True, True)
                'Assert
                Assert.AreEqual(entry.Value, returnValue, "Source <{0}>", entry.Key)
            Next

        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_FilterTitleFromPath_Movie_IsSingleOhneOrdnername()
            'Arrange
            'IsSingle = True allein reicht nicht aus - ohne UseForderName wird
            'weiterhin der Dateiname verwendet.
            Dim source As New Dictionary(Of String, String)
            With source
                .Add("C:\Movies\The Movie (2013)\The Movie (2013).mkv", "The Movie")
                .Add("C:\Movies\Some.Movie.1080p.BluRay.x264\Some.Movie.1080p.BluRay.x264.mkv", "Some Movie")
            End With

            For Each entry As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.FilterTitleFromPath_Movie(entry.Key, True, False)
                'Assert
                Assert.AreEqual(entry.Value, returnValue, "Source <{0}>", entry.Key)
            Next

        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_RemovePunctuation()
            'Arrange
            Dim source As New Dictionary(Of String, String)
            With source
                .Add(String.Empty, String.Empty)
                .Add("Movie.Name.COMPLETE.720p.BluRay.DTS.x264-LiZard", "Movie Name COMPLETE 720p BluRay DTS x264 LiZard")
                .Add("The." & vbTab & " Movie.", "The Movie")
                .Add("The." & vbTab & vbTab & vbLf & " Movie.", "The Movie")
                .Add("A" & vbTab & "B", "A B")
                .Add("A B.C. Movie", "A B C Movie")
                .Add("Wow! Nice_-One", "Wow Nice_ One")
            End With

            For Each entry As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.RemovePunctuation(entry.Key)
                'Assert
                Assert.AreEqual(entry.Value, returnValue, "Source <{0}>", entry.Key)
            Next

        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_SortTokens_NothingParameter()
            'Arrange
            'Die heutige Funktion lautet StringUtils.SortTokens. Sie benutzt die
            'Sortier-Token aus Master.eSettings.GeneralSortTokens und gibt den Titel
            'unveraendert zurueck, wenn kein Token passt.
            'Act
            Dim result As String = StringUtils.SortTokens(Nothing)
            'Assert
            Assert.IsNull(result, "Bei Nothing wird der Eingabewert unveraendert zurueckgegeben")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_SortTokens()
            'Arrange
            'Die Standard-Tokens sind "a", "an", "das", "der", "die" und "the".
            'Ein Token muss direkt am Anfang des Titels stehen und von einem
            'Leerzeichen gefolgt werden, sonst greift es nicht.
            Dim source As New Dictionary(Of String, String)
            With source
                .Add(String.Empty, String.Empty)
                .Add("The Movie", "Movie, The")
                .Add("A Movie", "Movie, A")
                .Add("An Old Movie", "Old Movie, An")
                .Add("The A In That", "A In That, The")
                .Add("Movie.Name.COMPLETE.720p.BluRay.DTS.x264-LiZard", "Movie.Name.COMPLETE.720p.BluRay.DTS.x264-LiZard")
                .Add("The. Movie.", "The. Movie.")   'Kein Leerzeichen nach "The" - Token greift nicht
            End With

            For Each entry As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.SortTokens(entry.Key)
                'Assert
                Assert.AreEqual(entry.Value, returnValue, "Source <{0}>", entry.Key)
            Next

        End Sub

        '=========================================

        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_FilterTitleFromPath_TVEpisode_NothingPfad()
            'Arrange
            'Die heutige Funktion lautet StringUtils.FilterTitleFromPath_TVEpisode und
            'erwartet einen vollstaendigen Pfad sowie den Namen der Serie.
            'Act
            Dim returnValue As String = StringUtils.FilterTitleFromPath_TVEpisode(Nothing, "Test Show")
            'Assert
            Assert.AreEqual(String.Empty, returnValue, "Pfad war Nothing")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_FilterTitleFromPath_TVEpisode_NothingShowName()
            'Arrange
            'Ohne Seriennamen wird der gefilterte Episodentitel unveraendert zurueckgegeben.
            Dim episodeName = "Test Episode"
            'Act
            Dim returnValue As String = StringUtils.FilterTitleFromPath_TVEpisode(episodeName.Clone, Nothing)  'Cloned to prevent original from being modified
            'Assert
            Assert.AreEqual(episodeName, returnValue, "Show name was Nothing")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_FilterTitleFromPath_TVEpisode_NothingPfadUndShowName()
            'Arrange
            'Act
            Dim returnValue As String = StringUtils.FilterTitleFromPath_TVEpisode(Nothing, Nothing)
            'Assert
            Assert.AreEqual(String.Empty, returnValue, "Episode and Show name was Nothing")
        End Sub
        ''' <summary>
        ''' Internal structure to aid with testing TV Filters
        ''' </summary>
        ''' <remarks>Contains input Episode path and Show name, along with an expected result</remarks>
        Friend Structure Triplet
            Dim Episode As String
            Dim Show As String
            Dim Expected As String

            Public Sub New(_episode As String, _show As String, _expected As String)
                Episode = _episode
                Show = _show
                Expected = _expected
            End Sub
        End Structure
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_FilterTitleFromPath_TVEpisode_SatzzeichenBleiben()
            'Arrange
            'Die alten Flags fuer "Extras" und "Satzzeichen entfernen" gibt es nicht mehr.
            'Bindestriche und kaufmaennische Zeichen bleiben daher erhalten, Punkte
            'werden zu Leerzeichen und Unterstriche ebenfalls zu Leerzeichen.
            Dim source As New List(Of Triplet)
            With source
                .Add(New Triplet("C:\TV\Show Name\S01E02\Episode-Name.mkv", "Show Name", "Episode-Name"))
                .Add(New Triplet("C:\TV\Show Name\S01E02\Episode & Name.mkv", "Show Name", "Episode & Name"))
                .Add(New Triplet("C:\TV\Show Name\S01E02\Episode Name.mkv", "Show Name", "Episode Name"))
                .Add(New Triplet("C:\TV\Show Name\S01E02\Episode_Name.mkv", "Show Name", "Episode Name"))
            End With

            For Each entry As Triplet In source
                'Act
                Dim returnValue As String = StringUtils.FilterTitleFromPath_TVEpisode(entry.Episode, entry.Show)
                'Assert
                Assert.AreEqual(entry.Expected, returnValue, "Episode <{0}>, Show <{1}>", entry.Episode, entry.Show)
            Next

        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_FilterTitleFromPath_TVEpisode_SeriennameWirdEntfernt()
            'Arrange
            'Der Name der Serie wird aus dem Episodentitel entfernt.
            Dim source As New List(Of Triplet)
            With source
                .Add(New Triplet("C:\TV\Show Name\S01E02\Show Name - Episode Name.mkv", "Show Name", "Episode Name"))
            End With

            For Each entry As Triplet In source
                'Act
                Dim returnValue As String = StringUtils.FilterTitleFromPath_TVEpisode(entry.Episode, entry.Show)
                'Assert
                Assert.AreEqual(entry.Expected, returnValue, "Episode <{0}>, Show <{1}>", entry.Episode, entry.Show)
            Next

        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_FilterTitleFromPath_TVEpisode_AufloesungWirdEntfernt()
            'Arrange
            'Aufloesung und Quellangaben werden durch die Standardfilter entfernt.
            Dim source As New List(Of Triplet)
            With source
                .Add(New Triplet("C:\TV\Show Name\S01E02\Episode.Name.1080p.mkv", "Show Name", "Episode Name"))
            End With

            For Each entry As Triplet In source
                'Act
                Dim returnValue As String = StringUtils.FilterTitleFromPath_TVEpisode(entry.Episode, entry.Show)
                'Assert
                Assert.AreEqual(entry.Expected, returnValue, "Episode <{0}>, Show <{1}>", entry.Episode, entry.Show)
            Next

        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_FilterTitleFromPath_TVEpisode_AllesGefiltert()
            'Arrange
            'Sonderfall: Wenn nach dem Filtern und dem Entfernen des Seriennamens nichts
            'uebrig bleibt, wird der ungefilterte Dateiname zurueckgegeben.
            Dim source As New List(Of Triplet)
            With source
                .Add(New Triplet("C:\TV\Show Name\S01E02\Show Name S01E02.mkv", "Show Name", "Show Name S01E02"))
            End With

            For Each entry As Triplet In source
                'Act
                Dim returnValue As String = StringUtils.FilterTitleFromPath_TVEpisode(entry.Episode, entry.Show)
                'Assert
                Assert.AreEqual(entry.Expected, returnValue, "Episode <{0}>, Show <{1}>", entry.Episode, entry.Show)
            Next

        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_FilterTitleFromPath_TVShow_NothingParameter()
            'Arrange
            'Die heutige Funktion lautet StringUtils.FilterTitleFromPath_TVShow und
            'erwartet den Pfad des Serienordners.
            'Act
            Dim returnValue As String = StringUtils.FilterTitleFromPath_TVShow(Nothing)
            'Assert
            Assert.AreEqual(String.Empty, returnValue, "Source was Nothing")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_FilterTitleFromPath_TVShow_Standardfilter()
            'Arrange
            'Es werden die Standardfilter der Einstellungen verwendet - Jahresangabe,
            'Aufloesung und Punktuierungen werden entfernt.
            Dim source As New Dictionary(Of String, String)
            With source
                .Add("C:\TV\The Show (2013)", "The Show")
                .Add("C:\TV\Some.Show.1080p", "Some Show")
            End With

            For Each entry As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.FilterTitleFromPath_TVShow(entry.Key)
                'Assert
                Assert.AreEqual(entry.Value, returnValue, "Source <{0}>", entry.Key)
            Next

        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_FilterTitleFromPath_TVShow_UnterordnerWirdVerwendet()
            'Arrange
            'Verwendet wird der Name des letzten Ordners im Pfad.
            Dim source As New Dictionary(Of String, String)
            With source
                .Add("C:\TV\The Show (2013)\Season 01", "Season 01")
            End With

            For Each entry As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.FilterTitleFromPath_TVShow(entry.Key)
                'Assert
                Assert.AreEqual(entry.Value, returnValue, "Source <{0}>", entry.Key)
            Next

        End Sub
        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_FilterYear_NothingParameter()
            'Arrange
            'Act
            Dim result As String = StringUtils.FilterYear(Nothing)
            'Assert
            Assert.AreEqual(result, String.Empty, "Nothing parameter")
        End Sub
        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_FilterYear()
            Dim source As New Dictionary(Of String, String)
            With source
                .Add("title (2013)", "title")
                .Add("title_(2013)", "title")
                .Add("title.(2013)", "title")
                .Add("title-(2013)", "title")
                .Add("title 2013)", "title")
                .Add("title_(2013", "title_(2013")  'Closing bracket expected
                .Add("title.2013", "title.2013")  'Closing bracket expected
                .Add("title-((2013))", "title-((2013))")
                .Add("In 1956", "In 1956")
                .Add("no dates at all", "no dates at all")
                .Add("(1943 thoMAS)", "(1943 thoMAS)")
                .Add("(2345) Healey", "(2345) Healey")  'No whitespace before (, so leaves it alone
                .Add("In(2045)Nothing", "In(2045)Nothing")  'No whitespace before (, so leaves it alone
                .Add("In (2045)Nothing", "InNothing")   'Whitespace before (, so should remove it
                .Add("Movie trailer (3)", "Movie trailer (3)")  'Not enough numbers
                .Add("Another movie (45321)", "Another movie (45321)")  'Too many numbers
            End With

            For Each entry As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.FilterYear(entry.Key)
                'Assert
                Assert.AreEqual(entry.Value, returnValue, "Value '{0}'", entry.Key)
            Next
        End Sub
        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_HtmlEncode_NothingParameter()
            'Arrange
            'Act
            Dim result As String = StringUtils.HtmlEncode(Nothing)
            'Assert
            Assert.AreEqual(result, String.Empty, "Nothing parameter")
        End Sub
        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_HtmlEncode()
            Dim source As New Dictionary(Of String, String)
            With source
                .Add("title (2013)", "title (2013)")
                .Add("ABC", "ABC")
                .Add("ZZZ", "ZZZ")
                .Add("`~!@#$%^&*()_-+=", "`~!@#$%^&amp;*()_-+=")
                .Add("<>() !,`", "&lt;&gt;() !,`")
                .Add("éèåçïêæ£│┤■²√Θ", "&#233;&#232;&#229;&#231;&#239;&#234;&#230;&#163;&#9474;&#9508;&#9632;&#178;&#8730;&#920;")
                .Add("Ωπℵ∞♣♥♈♉♊♋♌♍♎♏♐♑♒♓", "&#937;&#960;&#8501;&#8734;&#9827;&#9829;&#9800;&#9801;&#9802;&#9803;&#9804;&#9805;&#9806;&#9807;&#9808;&#9809;&#9810;&#9811;")
            End With

            For Each entry As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.HtmlEncode(entry.Key)
                'Assert
                Assert.AreEqual(entry.Value, returnValue, "Value '{0}'", entry.Key)
            Next
        End Sub
        ''' <summary>
        ''' Internal structure for the isStacked method testing
        ''' </summary>
        ''' <remarks></remarks>
        Friend Structure IsStackedStructure
            Dim Path As String
            Dim Expected As Boolean

            Public Sub New(_path As String, _expected As Boolean)
                Path = _path
                Expected = _expected
            End Sub
        End Structure
        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_IsStacked_NothingParameter()
            'Arrange
            'Die heutige Funktion lautet FileUtils.Common.isStacked und erwartet einen
            'vollstaendigen Pfad. Der alte Parameter fuer VIDEO_TS gibt es nicht mehr.
            'Act
            Dim result As Boolean = FileUtils.Common.isStacked(Nothing)
            'Assert
            Assert.IsFalse(result, "Nothing parameter")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_IsStacked()
            'Arrange
            'isStacked vergleicht den Pfad mit dem Pfad ohne Stacking-Marker.
            'Als Marker zaehlen "cd1", "pt1", "part2", "dvd2", "disc3" und "disk4"
            'im Dateinamen - Ordner-Marker werden nicht ausgewertet.
            Dim source As New List(Of IsStackedStructure)
            With source
                .Add(New IsStackedStructure(String.Empty, False))
                .Add(New IsStackedStructure("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.mkv", False))
                .Add(New IsStackedStructure("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.cd1.mkv", True))
                .Add(New IsStackedStructure("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.pt1.mkv", True))
                .Add(New IsStackedStructure("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.part2.mkv", True))
                .Add(New IsStackedStructure("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.dvd2.mkv", True))
                .Add(New IsStackedStructure("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.disc3.mkv", True))
                .Add(New IsStackedStructure("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.disk4.mkv", True))
                .Add(New IsStackedStructure("C:\Movies\Yodeling In The Alps\yodeling.in.the.alps.cc1.mkv", False))  '"cc" ist kein Stacking-Marker
                .Add(New IsStackedStructure("C:\Movies\Film CD1\film.mkv", False))    'Ordnermarker werden nicht ausgewertet
            End With

            For Each entry As IsStackedStructure In source
                'Act
                Dim returnValue As Boolean = FileUtils.Common.isStacked(entry.Path)
                'Assert
                Assert.AreEqual(entry.Expected, returnValue, "Path <{0}>", entry.Path)
            Next
        End Sub
        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_isValidURL_NothingParameter()
            'Arrange
            'Act
            Dim result As String = StringUtils.isValidURL(Nothing)
            'Assert
            Assert.IsFalse(result, "Nothing parameter")
        End Sub
        <UnitTest>
         <TestMethod()>
        Public Sub StringUtils_isValidURL()
            'So many possible test cases...
            ' http://home.deds.nl/~aeron/regex/invalid_ipv6.txt
            ' http://home.deds.nl/~aeron/regex/valid_ipv6.txt

            'Arrange
            Dim source As New Dictionary(Of String, Boolean)
            With source
                .Add(String.Empty, False)
                .Add("www.google.com", False)
                .Add("http://www.google.com", True)
                .Add("a://www.google.com", False)
                .Add("ab://www.google.com", False)
                .Add("abc://www.google.com", False)
                .Add("abcdefg://www.google.com", False)
                .Add("http://.google.com", False)
                .Add("http:// .google.com", False)
                .Add("http://.google .com", False)
                .Add("http://www.google.com/asdf", True)
                .Add("http://www.google.com//asdf", True)  'This is an edge case, and is surprisingly valid according to the specs
                .Add("://www.google.com", False)
                .Add("javascript:alert('hacked')", False)   'This could allow javascript injection if it returns True!
                'IPv4 notation
                .Add("192.168.0.1", False)
                .Add("http://192.168.0.1", True)
                .Add("192 .168.0.1", False)  '???? This works (True) because it strips out white space. Should really be FALSE though.
                .Add("192.168-0.1", False)
                'IPv6 Notation
                .Add("2001:0db8:85a3:0000:0000:8a2e:0370:7334", False)
                .Add("http://2001:0db8:85a3:0000:0000:8a2e:0370:7334/", False)
                .Add("http://[2001:0db8:85a3:0000:0000:8a2e:0370:7334]", True)
                .Add("::1", False)   'Compressed loopback - should be valid!! Why is this False? Bug in MS code?
                .Add("http://::1", False)   'Compressed loopback - should be valid!! Why is this False? Bug in MS code?
                .Add("[::1]", False)   'Compressed loopback - should be valid!! Why is this False? Bug in MS code?
                'Looks like compressed IPv6 is only supported if fully qualified... 
                .Add("http://[::1]", True)   'Compressed loopback
                .Add("http://[2001:0db8::0001]", True)
                .Add("http://[2001:db8::1]", True)
                .Add("http://[2001:db8:0:0:0:0:2:1]", True)
                .Add("http://[2001:db8::2:1]", True)
                .Add("http://[2001:db8:0000:1:1:1:1:1]", True)
                .Add("http://[2001:db8:0:1:1:1:1:1]", True)
                .Add("http://[2001:db8:0:0:1:0:0:1]", True)
                .Add("http://[2001:db8::1:0:0:1]", True)
                .Add("http://[2001:db8:0:0:1::1]", True)   '??? This is not recommended, but often valid
                .Add("http://[2001:db8::1:0:0:1]/something/else.what?", True)
            End With

            For Each pair As KeyValuePair(Of String, Boolean) In source
                'Act
                Dim returnValue As Boolean = StringUtils.isValidURL(pair.Key)

                'Assert
                Assert.AreEqual(pair.Value, returnValue, "URL <{0}>", pair.Key)
            Next
        End Sub
        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_ConvertToProperCase_NothingParameter()
            'Arrange
            'Die heutige Funktion lautet StringUtils.ConvertToProperCase.
            'Act
            Dim result As String = StringUtils.ConvertToProperCase(Nothing)
            'Assert
            Assert.AreEqual(String.Empty, result, "Nothing parameter")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_ConvertToProperCase()
            'Arrange
            'Der Text wird in die Grossschreibung am Wortanfang umgewandelt.
            'Zusaetzlich werden bestimmte Kuerzel (hd, cd, dvd, ii, iii, ...) in
            'Grossbuchstaben geschrieben - aber nur bei einer Wortgrenze.
            Dim source As New Dictionary(Of String, String)
            With source
                .Add(String.Empty, String.Empty)
                .Add("This is my movie", "This Is My Movie")
                .Add(" wOW WonDER ", "Wow Wonder")   'leading and trailing whitespace
                .Add("123 Sycamore Lane", "123 Sycamore Lane")
                .Add("movie cd1", "Movie Cd1")   'Keine Wortgrenze nach "cd" - bleibt unveraendert
                .Add("movie cd 1", "Movie CD 1")
                .Add("movie dvd", "Movie DVD")
                .Add("movie hd", "Movie HD")
                .Add("Retourne à l'école", "Retourne À L'école")
                .Add("Michael d'Angelo", "Michael D'angelo")
                .Add("movie I", "Movie I")
                .Add("movie ii", "Movie II")
                .Add("movie xi", "Movie Xi")   'Case support stops at 10
                .Add("End of wwii", "End Of Wwii")  'Respect word boundary for II
            End With

            For Each pair As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.ConvertToProperCase(pair.Key)

                'Assert
                Assert.AreEqual(pair.Value, returnValue, "Source <{0}>", pair.Key)
            Next
        End Sub
        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_StringToSize_NothingParameter()
            'Arrange
            'Act
            Dim result As Size = StringUtils.StringToSize(Nothing)
            'Assert
            Assert.IsTrue((result.Width = 0 AndAlso result.Height = 0), "Nothing parameter")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_StringToSize()
            'Arrange
            Dim source As New Dictionary(Of String, Size)
            With source
                .Add(String.Empty, New Size(0, 0))
                .Add("4x3", New Size(4, 3))
                .Add("4X3", New Size(4, 3))
                .Add("16x9", New Size(16, 9))
                .Add("12345678x987654321", New Size(12345678, 987654321))
                .Add("3x5", New Size(3, 5))
                .Add("x3x5", New Size(0, 0))
                .Add("3x5.", New Size(0, 0))
                .Add("5x", New Size(0, 0))
            End With

            For Each pair As KeyValuePair(Of String, Size) In source
                'Act
                Dim returnValue As Size = StringUtils.StringToSize(pair.Key)
                Dim equal As Boolean = False
                If pair.Value.Equals(returnValue) Then
                    equal = True
                End If

                'Assert
                Assert.IsTrue(equal, "Source <{0}> evaluated to <{1} x {2}> but expected <{3} x {4}>", pair.Key.ToString(), returnValue.Width, returnValue.Height, pair.Value.Width, pair.Value.Height)
            Next
        End Sub
        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_TruncateURL_NothingParameter_ValidLength_FalseEndOnly()
            'Arrange
            'Act
            Dim result As String = StringUtils.TruncateURL(Nothing, 10, False)
            'Assert
            Assert.AreEqual(String.Empty, result, "Nothing parameter")
        End Sub
        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_TruncateURL_NothingParameter_ValidLength_TrueEndOnly()
            'Arrange
            'Act
            Dim result As String = StringUtils.TruncateURL(Nothing, 10, False)
            'Assert
            Assert.AreEqual(String.Empty, result, "Nothing parameter")
        End Sub
        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_TruncateURL_NothingParameter_InvalidLength_FalseEndOnly()
            'Arrange
            'Act
            Dim result As String = StringUtils.TruncateURL(Nothing, 10, False)
            'Assert
            Assert.AreEqual(String.Empty, result, "Nothing parameter")
        End Sub
        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_TruncateURL_NothingParameter_InvalidLength_TrueEndOnly()
            'Arrange
            'Act
            Dim result As String = StringUtils.TruncateURL(Nothing, 10, False)
            'Assert
            Assert.AreEqual(String.Empty, result, "Nothing parameter")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_TruncateURL_Length40_FalseEndOnly()
            'Arrange
            Dim source As New Dictionary(Of String, String)
            With source
                .Add(String.Empty, String.Empty)
                .Add("http://www.google.com/This_is_a_really_long_URL_name.jpg", "ht.../This_is_a_really_long_URL_name.jpg")
                .Add("http://www.Something.com/extra/place/short_name.jpg", "http://www.Something.c.../short_name.jpg")
                .Add("http://www.google.com/This_is_an_even_longer_really_long_URL_name.jpg", "..._even_longer_really_long_URL_name.jpg")
                .Add("http://www.really_really_long_domain_name_of_doom_to_contend_with.com/name.jpg", "http://www.really_really_lon.../name.jpg")
                .Add("http://www.google.com/short.jpg", "http://www.google.com/short.jpg")
                .Add("h", "h")
                .Add("123456789/123456789/123456789/123456789", "123456789/123456789/123456789/123456789")
                .Add("123456789/123456789/123456789/123456789/", "123456789/123456789/123456789/123456789/")
                .Add("123456789/123456789/123456789/123456789/1", "123456789/123456789/123456789/12345.../1")
                .Add("123456789/123456789/123456789/123456789/12", "123456789/123456789/123456789/1234.../12")
            End With

            For Each pair As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.TruncateURL(pair.Key, 40, False)

                'Assert
                Assert.AreEqual(pair.Value, returnValue, "URL <{0}>", pair.Key)
            Next
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_TruncateURL_Length0_FalseEndOnly()
            'Arrange
            Dim source As New Dictionary(Of String, String)
            With source
                .Add(String.Empty, String.Empty)
                .Add("http://www.google.com/This_is_a_really_long_URL_name.jpg", String.Empty)
                .Add("http://www.Something.com/extra/place/short_name.jpg", String.Empty)
                .Add("http://www.google.com/This_is_an_even_longer_really_long_URL_name.jpg", String.Empty)
                .Add("http://www.really_really_long_domain_name_of_doom_to_contend_with.com/name.jpg", String.Empty)
                .Add("http://www.google.com/short.jpg", String.Empty)
                .Add("h", String.Empty)
                .Add("123456789/123456789/123456789/123456789", String.Empty)
                .Add("123456789/123456789/123456789/123456789/", String.Empty)
                .Add("123456789/123456789/123456789/123456789/1", String.Empty)
                .Add("123456789/123456789/123456789/123456789/12", String.Empty)
            End With

            For Each pair As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.TruncateURL(pair.Key, 0, False)

                'Assert
                Assert.AreEqual(pair.Value, returnValue, "URL <{0}>", pair.Key)
            Next
        End Sub
        <UnitTest>
<TestMethod()>
        Public Sub StringUtils_TruncateURL_NegativeLength_FalseEndOnly()
            'Arrange
            Dim source As New Dictionary(Of String, String)
            With source
                .Add(String.Empty, String.Empty)
                .Add("http://www.google.com/This_is_a_really_long_URL_name.jpg", String.Empty)
                .Add("http://www.Something.com/extra/place/short_name.jpg", String.Empty)
                .Add("http://www.google.com/This_is_an_even_longer_really_long_URL_name.jpg", String.Empty)
                .Add("http://www.really_really_long_domain_name_of_doom_to_contend_with.com/name.jpg", String.Empty)
                .Add("http://www.google.com/short.jpg", String.Empty)
                .Add("h", String.Empty)
                .Add("123456789/123456789/123456789/123456789", String.Empty)
                .Add("123456789/123456789/123456789/123456789/", String.Empty)
                .Add("123456789/123456789/123456789/123456789/1", String.Empty)
                .Add("123456789/123456789/123456789/123456789/12", String.Empty)
            End With

            For Each pair As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.TruncateURL(pair.Key, -1, False)

                'Assert
                Assert.AreEqual(pair.Value, returnValue, "URL <{0}>", pair.Key)
            Next
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_TruncateURL_Length40_TrueEndOnly()
            'Arrange
            Dim source As New Dictionary(Of String, String)
            With source
                '.Add(String.Empty, String.Empty)
                .Add("http://www.google.com/This_is_a_really_long_URL_name.jpg", "e.com/This_is_a_really_long_URL_name.jpg")
                .Add("http://www.Something.com/extra/place/short_name.jpg", "Something.com/extra/place/short_name.jpg")
                .Add("http://www.google.com/This_is_an_even_longer_really_long_URL_name.jpg", "_an_even_longer_really_long_URL_name.jpg")
                .Add("http://www.really_really_long_domain_name_of_doom_to_contend_with.com/name.jpg", "ame_of_doom_to_contend_with.com/name.jpg")
                .Add("http://www.google.com/short.jpg", "http://www.google.com/short.jpg")
                .Add("h", "h")
                .Add("123456789/123456789/123456789/123456789", "123456789/123456789/123456789/123456789")
                .Add("123456789/123456789/123456789/123456789/", "123456789/123456789/123456789/123456789/")
                .Add("123456789/123456789/123456789/123456789/1", "23456789/123456789/123456789/123456789/1")
                .Add("123456789/123456789/123456789/123456789/12", "3456789/123456789/123456789/123456789/12")
            End With

            For Each pair As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.TruncateURL(pair.Key, 40, True)

                'Assert
                Assert.AreEqual(pair.Value, returnValue, "URL <{0}>", pair.Key)
            Next
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_TruncateURL_Length0_TrueEndOnly()
            'Arrange
            Dim source As New Dictionary(Of String, String)
            With source
                .Add(String.Empty, String.Empty)
                .Add("http://www.google.com/This_is_a_really_long_URL_name.jpg", String.Empty)
                .Add("http://www.Something.com/extra/place/short_name.jpg", String.Empty)
                .Add("http://www.google.com/This_is_an_even_longer_really_long_URL_name.jpg", String.Empty)
                .Add("http://www.really_really_long_domain_name_of_doom_to_contend_with.com/name.jpg", String.Empty)
                .Add("http://www.google.com/short.jpg", String.Empty)
                .Add("h", String.Empty)
                .Add("123456789/123456789/123456789/123456789", String.Empty)
                .Add("123456789/123456789/123456789/123456789/", String.Empty)
                .Add("123456789/123456789/123456789/123456789/1", String.Empty)
                .Add("123456789/123456789/123456789/123456789/12", String.Empty)
            End With

            For Each pair As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.TruncateURL(pair.Key, 0, True)

                'Assert
                Assert.AreEqual(pair.Value, returnValue, "URL <{0}>", pair.Key)
            Next
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_TruncateURL_NegativeLength_TrueEndOnly()
            'Arrange
            Dim source As New Dictionary(Of String, String)
            With source
                .Add(String.Empty, String.Empty)
                .Add("http://www.google.com/This_is_a_really_long_URL_name.jpg", String.Empty)
                .Add("http://www.Something.com/extra/place/short_name.jpg", String.Empty)
                .Add("http://www.google.com/This_is_an_even_longer_really_long_URL_name.jpg", String.Empty)
                .Add("http://www.really_really_long_domain_name_of_doom_to_contend_with.com/name.jpg", String.Empty)
                .Add("http://www.google.com/short.jpg", String.Empty)
                .Add("h", String.Empty)
                .Add("123456789/123456789/123456789/123456789", String.Empty)
                .Add("123456789/123456789/123456789/123456789/", String.Empty)
                .Add("123456789/123456789/123456789/123456789/1", String.Empty)
                .Add("123456789/123456789/123456789/123456789/12", String.Empty)
            End With

            For Each pair As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.TruncateURL(pair.Key, -1, True)

                'Assert
                Assert.AreEqual(pair.Value, returnValue, "URL <{0}>", pair.Key)
            Next
        End Sub


        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_CleanFileName_NothingParameter()
            'Arrange
            'Act
            Dim result As String = StringUtils.CleanFileName(Nothing)
            'Assert
            Assert.AreEqual(String.Empty, result, "Nothing parameter")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_CleanFileName()
            'Arrange
            Dim source As New Dictionary(Of String, String)
            With source
                .Add(String.Empty, String.Empty)
                .Add("This_is_a_really_long_URL_name.jpg", "This_is_a_really_long_URL_name.jpg")
                .Add("Test_Filename", "Test_Filename")
                .Add("Test Filename", "Test Filename")
                .Add("Test-Filename", "Test-Filename")
                .Add("Test:Filename", "Test -Filename")
                .Add("Test" & vbTab & "Filename", "TestFilename")
                .Add("Te:st" & vbTab & "Filename", "Te -stFilename")    'Multiple items at once
                .Add("Test     Filename", "Test     Filename")
                .Add("Test:::::Filename", "Test - - - - -Filename")
                .Add("Test_File""name", "Test_Filename")
            End With

            For Each pair As KeyValuePair(Of String, String) In source
                'Act
                Dim returnValue As String = StringUtils.CleanFileName(pair.Key)

                'Assert
                Assert.AreEqual(pair.Value, returnValue, "Source <{0}>", pair.Key)
            Next
        End Sub
        ''' <summary>
        ''' Internal structure for ShortenOutline testing
        ''' </summary>
        ''' <remarks></remarks>
        Friend Structure ShortenOutlineTest
            Public Source As String
            Public Length As Integer
            Public Expected As String
            Public Sub New(_source As String, _length As Integer, _expected As String)
                Source = _source
                Length = _length
                Expected = _expected
            End Sub
        End Structure
        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_ShortenOutline_NothingParameter_ValidLength()
            'Arrange
            'Act
            Dim result As String = StringUtils.ShortenOutline(Nothing, 10)
            'Assert
            Assert.AreEqual(String.Empty, result, "Nothing parameter. Valid length")
        End Sub
        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_ShortenOutline_NothingParameter_InvalidLength()
            'Arrange
            'Act
            Dim result As String = StringUtils.ShortenOutline(Nothing, 0)
            'Assert
            Assert.AreEqual(String.Empty, result, "Nothing parameter. 0 length")
        End Sub
        <UnitTest>
        <TestMethod>
        Public Sub StringUtils_ShortenOutline_NothingParameter_NegativeLength()
            'Arrange
            'Act
            Dim result As String = StringUtils.ShortenOutline(Nothing, -10)
            'Assert
            Assert.AreEqual(String.Empty, result, "Nothing parameter. Negative length")
        End Sub
        <UnitTest>
        <TestMethod()>
        Public Sub StringUtils_ShortenOutline()
            'Arrange
            Dim source As New List(Of ShortenOutlineTest)
            With source
                '.Add(New ShortenOutlineTest(String.Empty, 10, String.Empty))
                '.Add(New ShortenOutlineTest(String.Empty, 0, String.Empty))
                '.Add(New ShortenOutlineTest(String.Empty, -1, String.Empty))
                '.Add(New ShortenOutlineTest("12345", 10, "12345"))      ' Test with short string, no periods
                '.Add(New ShortenOutlineTest("12345.", 10, "12345."))    ' Test with short string, with periods
                '.Add(New ShortenOutlineTest("123.45", 10, "123.45"))    ' Test with short string, with periods
                '.Add(New ShortenOutlineTest(".12345", 10, ".12345"))    ' Test with short string, with periods
                .Add(New ShortenOutlineTest("12345678901234567890", 10, "1234567..."))  ' Test with long string, no periods
                .Add(New ShortenOutlineTest("1234567890.234567890", 10, "1234567..."))  ' Test with long string, with periods
                .Add(New ShortenOutlineTest("123456789.1234567890", 10, "1234567..."))  ' Test with long string, with periods
                .Add(New ShortenOutlineTest("12345678.01234567890", 10, "1234567..."))  ' Test with long string, with periods
                .Add(New ShortenOutlineTest("1234567.901234567890", 10, "1234567..."))  ' Test with long string, with periods
                .Add(New ShortenOutlineTest("123456.8901234567890", 10, "123456..."))   ' Test with long string, with periods
                .Add(New ShortenOutlineTest("123.5678901234567890", 10, "123..."))      ' Test with long string, with periods
                .Add(New ShortenOutlineTest("12.45678901234567890", 10, "12..."))       ' Test with long string, with periods
                .Add(New ShortenOutlineTest("1.345678901234567890", 10, "1..."))        ' Test with long string, with periods
                .Add(New ShortenOutlineTest(".2345678901234567890", 10, "..."))         ' Test with long string, with periods
                .Add(New ShortenOutlineTest("12345", 6, "12345"))       ' Test with short string, no periods
                .Add(New ShortenOutlineTest("12345", 5, "12345"))       ' Test with short string, no periods
                .Add(New ShortenOutlineTest("12345", 4, "1..."))       ' Test with short string, no periods
                .Add(New ShortenOutlineTest("12345", 3, "..."))       ' Test with short string, no periods
                .Add(New ShortenOutlineTest("12345", 2, ".."))       ' Test with short string, no periods
                .Add(New ShortenOutlineTest("12345", 1, "."))        ' Test with short string, no periods
                .Add(New ShortenOutlineTest("12345", 0, "12345"))  ' Limit 0 bedeutet: voller Plot beibehalten
                .Add(New ShortenOutlineTest("12345", -1, String.Empty)) ' Test with short string, no periods
                .Add(New ShortenOutlineTest("12345", -10, String.Empty))    ' Test with short string, no periods
            End With

            For Each entry As ShortenOutlineTest In source
                'Act
                Dim returnValue As String = StringUtils.ShortenOutline(entry.Source, entry.Length)

                'Assert
                Assert.AreEqual(entry.Expected, returnValue, "Source <{0}> length <{1}>", entry.Source, entry.Length)
            Next
        End Sub

    End Class
End Namespace
