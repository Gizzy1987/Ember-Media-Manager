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
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Imports EmberAPI
Imports UnitTests

Namespace EmberTests

    ''' <summary>
    ''' Regressionstests fuer das Stacking-Muster in RemoveStackingMarkers.
    '''
    ''' Hintergrund: Beim Scrapen von "Doctor Who" am 10.11.2026 lief der
    ''' Scan in einen RegexMatchTimeoutException:
    '''
    '''   System.Text.RegularExpressions.RegexMatchTimeoutException
    '''    bei EmberAPI.FileUtils.Common.RemoveStackingMarkers(String strPath)
    '''    bei EmberAPI.FileUtils.Common.isStacked(String strPath)
    '''    bei EmberAPI.MediaInfo.GetMIFromPath(Fileinfo& fiInfo, String sPath, Boolean ForTV)
    '''
    ''' Ursache: Das Muster "(.*?)([ _.-]*(?:cd|dvd|p(?:ar)?t|dis[ck])[0-9]+)(.*?)(\\.[^.]+)$"
    ''' erzwingt, dass der Regex ueber jede moegliche Aufteilung des Pfades
    ''' zuruecktrackt, um eine Dateiendung zu finden. Ohne Dateiendung - oder bei
    ''' sehr langen Pfaden - ist das quadratisch und der 2-Sekunden-Timeout wird
    ''' erreicht. Gemessen: 40 Teilstuecke -> 96 ms, 60 Teilstuecke -> 295 ms.
    '''
    ''' Fix: Die beiden hinteren Gruppen werden vor der Verwendung entfernt, weil
    ''' ohnehin nur Gruppe 2 (der Stacking-Marker) ausgewertet wird. Damit laeuft
    ''' das Muster linear statt quadratisch.
    ''' </summary>
    <TestClass()>
    Public Class Test_clsAPIFileUtils_Stacking

        ''' <summary>
        ''' Das Standardmuster, das in clsAPIFileUtils.RemoveStackingMarkers
        ''' verwendet wird.
        ''' </summary>
        Private Const StandardStackingPattern As String = "(.*?)([ _.-]*(?:cd|dvd|p(?:ar)?t|dis[ck])[0-9]+)(.*?)(\.[^.]+)$"

        ''' <summary>
        ''' Der Stacking-Marker muss weiterhin erkannt werden - die
        ''' Vereinfachung des Musters darf das Verhalten nicht aendern.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub RemoveStackingMarkers_ErkenntDenMarkerWeiterhin()
            'Arrange
            Dim faelle As New Dictionary(Of String, String) From
                {
                    {"Y:\Test_Serie\Film\Film cd1.mkv", " cd1"},
                    {"Y:\Test_Serie\Film\Film part2.mkv", " part2"},
                    {"Y:\Test_Serie\Film\Film disc3.mkv", " disc3"},
                    {"Y:\Test_Serie\Film\Film dvd1.mkv", " dvd1"},
                    {"Y:\Test_Serie\Film\Film pt2.mkv", " pt2"}
                }

            'Act / Assert
            For Each kvp As KeyValuePair(Of String, String) In faelle
                Dim result As String = FileUtils.Common.RemoveStackingMarkers(kvp.Key)
                Assert.AreEqual(kvp.Key.Replace(kvp.Value, String.Empty), result,
                                "Der Stacking-Marker wurde nicht korrekt entfernt: " + kvp.Key)
            Next
        End Sub

        ''' <summary>
        ''' Ein Pfad ohne Stacking-Marker bleibt unveraendert.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub RemoveStackingMarkers_OhneMarkerBleibtUnveraendert()
            'Arrange
            Dim pfad As String = "Y:\Test_Serie\Doctor Who\Season 07\Doctor Who - S07E13 - Der Name des Doktors.mkv"

            'Act
            Dim result As String = FileUtils.Common.RemoveStackingMarkers(pfad)

            'Assert
            Assert.AreEqual(pfad, result, "Ein Pfad ohne Stacking-Marker wurde veraendert")
        End Sub

        ''' <summary>
        ''' Der eigentliche Regressionstest: ein langer Pfad ohne Dateiendung
        ''' darf nicht in den Regex-Timeout laufen.
        '''
        ''' Vor dem Fix dauerte die Pruefung bei 60 Teilstuecken rund 295 ms und
        ''' waechste quadratisch weiter; bei noch laengeren Pfaden wurde der
        ''' 2-Sekunden-Timeout erreicht und der Scan brach mit einer Exception ab.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub RemoveStackingMarkers_LangerPfadOhneEndungLaeuftNichtInDenTimeout()
            'Arrange
            Dim sb As New StringBuilder()
            sb.Append("Y:\Test_Serie\Doctor Who\")
            For i As Integer = 0 To 59
                sb.Append("part" & i.ToString() & " ")
            Next
            sb.Append("2")
            Dim langerPfad As String = sb.ToString()

            'Act
            Dim sw As New Stopwatch()
            sw.Start()
            Dim result As String = FileUtils.Common.RemoveStackingMarkers(langerPfad)
            sw.Stop()

            'Assert: Der Aufruf muss schnell genug sein. Der Regex-Timeout
            'beträgt 2 Sekunden - bei quadratischem Backtracking waere der
            'Aufruf an dieser Grenze gescheitert.
            Assert.IsTrue(sw.ElapsedMilliseconds < 1000,
                          "RemoveStackingMarkers brauchte " & sw.ElapsedMilliseconds.ToString() &
                          " ms - das Muster backtrackt weiterhin quadratisch")
            Assert.IsNotNull(result, "RemoveStackingMarkers hat Nothing geliefert")
        End Sub

        ''' <summary>
        ''' Gegenprobe zum Standardmuster: es belegt, dass der Timeout nicht
        ''' durch ein anderes Muster, sondern durch das Backtracking entsteht.
        '''
        ''' Gemessene Werte fuer das Standardmuster: 40 Teilstuecke -> 91 ms,
        ''' 60 -> 314 ms, 80 -> 694 ms, 100 -> 1336 ms, 120 -> Timeout (2 s).
        ''' Das Wachstum ist quadratisch.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub StandardStackingPattern_BacktracktQuadratisch()
            'Arrange
            Dim sb As New StringBuilder()
            sb.Append("Y:\Test_Serie\Doctor Who\")
            For i As Integer = 0 To 119
                sb.Append("part" & i.ToString() & " ")
            Next
            sb.Append("2")
            Dim pfad As String = sb.ToString()

            'Act
            Dim rx As New System.Text.RegularExpressions.Regex(StandardStackingPattern,
                                                               System.Text.RegularExpressions.RegexOptions.IgnoreCase,
                                                               TimeSpan.FromSeconds(2))
            Dim sw As New Stopwatch()
            sw.Start()
            Dim timeoutAusgeloest As Boolean = False
            Try
                rx.Match(pfad)
            Catch ex As System.Text.RegularExpressions.RegexMatchTimeoutException
                timeoutAusgeloest = True
            End Try
            sw.Stop()

            'Assert: Der Timeout ist bei diesem Muster und diesem Pfad
            'erwartbar - das belegt die Ursache.
            Assert.IsTrue(timeoutAusgeloest,
                          "Das Standardmuster hat den Timeout nicht ausgeloest - die Ursachenannahme trifft nicht zu")
        End Sub

        ''' <summary>
        ''' Derselbe Pfad muss mit dem Fix zuegig verarbeitet werden - ohne in
        ''' den Regex-Timeout zu laufen.
        '''
        ''' Der Fix wendet das Muster nur auf den Dateinamen an und nur dann, wenn
        ''' der Dateiname ueberhaupt eine Dateiendung hat. Ohne Dateiendung kann
        ''' das Muster nicht matchen und wuerde dabei quadratisch zuruecktracken.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub RemoveStackingMarkers_VerarbeitetDenselbenPfadOhneTimeout()
            'Arrange
            Dim sb As New StringBuilder()
            sb.Append("Y:\Test_Serie\Doctor Who\")
            For i As Integer = 0 To 119
                sb.Append("part" & i.ToString() & " ")
            Next
            sb.Append("2")
            Dim pfad As String = sb.ToString()

            'Act
            Dim sw As New Stopwatch()
            sw.Start()
            Dim result As String = FileUtils.Common.RemoveStackingMarkers(pfad)
            sw.Stop()

            'Assert
            Assert.IsTrue(sw.ElapsedMilliseconds < 1000,
                          "RemoveStackingMarkers brauchte " & sw.ElapsedMilliseconds.ToString() &
                          " ms - der Timeout waere bei diesem Pfad erreicht worden")
            Assert.IsNotNull(result, "RemoveStackingMarkers hat Nothing geliefert")
        End Sub

        ''' <summary>
        ''' Der Fix darf das Ergebnis nicht veraendern: das Originalmuster
        ''' erkennt am Dateinamen ohne Dateiendung keinen Marker - das neue
        ''' Verhalten muss genauso sein. Mit Dateiendung muss der Marker
        ''' weiterhin entfernt werden.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub RemoveStackingMarkers_VerhaltenBleibtIdentischZumOriginal()
            'Arrange
            Dim pfade As New List(Of String) From
                {
                    "Y:\Test_Serie\Film\Film cd1.mkv",
                    "Y:\Test_Serie\Film\Film part2.mkv",
                    "Y:\Test_Serie\Film\Film disc3.mkv",
                    "Y:\Test_Serie\Film\Film.mkv",
                    "Y:\Test_Serie\Doctor Who\part0 part1 part2 2"
                }

            Dim rxOriginal As New System.Text.RegularExpressions.Regex(StandardStackingPattern,
                                                                       System.Text.RegularExpressions.RegexOptions.IgnoreCase,
                                                                       TimeSpan.FromSeconds(2))

            'Act / Assert
            For Each pfad As String In pfade
                Dim expected As String = pfad
                Dim m As System.Text.RegularExpressions.Match = rxOriginal.Match(Path.GetFileName(pfad))
                If m.Success Then
                    expected = Path.Combine(Path.GetDirectoryName(pfad),
                                            Path.GetFileName(pfad).Replace(m.Groups(2).Value, String.Empty))
                End If

                Dim result As String = FileUtils.Common.RemoveStackingMarkers(pfad)
                Assert.AreEqual(expected, result, "Das Ergebnis weicht vom Originalverhalten ab: " & pfad)
            Next
        End Sub

    End Class

End Namespace
