' ################################################################################
' #                             EMBER MEDIA MANAGER                              #
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
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Imports EmberAPI
Imports UnitTests

Namespace EmberTests

    ''' <summary>
    ''' Tests der Erkennung von Staffel und Folge aus dem Dateinamen.
    ''' </summary>
    ''' <remarks>
    ''' Ausgangspunkt ist der Befund vom 12.11.2026: die Dateien der Serie
    ''' "Doctor Who" tragen einen Aufloesungsmarker im Namen ("S05E23E1080").
    ''' Der Scanner las daraus eine zweite Folge "S05E1080" und legte sie in der
    ''' Datenbank ab. Dadurch landeten die Dateien in einer einzigen Gruppe und
    ''' der Dialog "Mehrere Dateien zu einer Folge" zeigte 20 gleiche Eintraege.
    '''
    ''' Getestet wird die reine Erkennung. Sie ist bewusst von Master (Settings,
    ''' Datenbank) getrennt, damit sie isoliert laeuft.
    ''' </remarks>
    <TestClass()> Public Class Test_clsAPIScannerTVDetection

#Region "Testdaten"

        'Das Muster aus der Standard-Konfiguration (Settings.xml, ID 0)
        Private Const SHOW_MATCHING As String = "s([0-9]+)[ ._-]*e([0-9]+(?:(?:[a-i]|\.[1-9])(?![0-9]))?)([^\\\/]*)$"

        'Das Muster aus der Standard-Konfiguration (Settings.xml)
        Private Const MULTI_PART_MATCHING As String = "^[-_ex]+([0-9]+(?:(?:[a-i]|\.[1-9])(?![0-9]))?)"

        ''' <summary>
        ''' Legt die Standard-Muster fest, damit der Test unabhaengig von der
        ''' Konfiguration des Nutzers laeuft.
        ''' </summary>
        Private Function StandardMuster() As List(Of Settings.regexp)
            Dim muster As New List(Of Settings.regexp)
            muster.Add(New Settings.regexp With {.ID = 0, .Regexp = SHOW_MATCHING, .defaultSeason = -2, .byDate = False})
            Return muster
        End Function

        ''' <summary>
        ''' Erkennt die Folgen einer Datei mit den Standardmustern.
        ''' </summary>
        Private Function Erkennen(ByVal strPath As String) As List(Of Scanner.EpisodeItem)
            Return Scanner.GetTVEpisodeMatches(strPath, StandardMuster(), MULTI_PART_MATCHING)
        End Function

#End Region 'Testdaten

#Region "Aufloesungsmarker"

        <UnitTest>
        <TestMethod()>
        Public Sub Erkennung_Aufloesungsmarker_E1080_Wird_Nicht_Als_Folge_Gelesen()
            'Arrange - echter Dateiname aus Y:\Test_Serie\Doctor Who
            Dim strPath As String = "Y:\Test_Serie\Doctor Who\Season 05\Doctor Who - S05E23E1080 - Das Netz der Angst - Teil 1 - Doctorwho S05E1080.mkv"

            'Act
            Dim result As List(Of Scanner.EpisodeItem) = Erkennen(strPath)

            'Assert
            Assert.AreEqual(1, result.Count, "Es darf nur eine Folge erkannt werden, gefunden wurden {0}", result.Count)
            Assert.AreEqual(5, result(0).Season, "Falsche Staffel erkannt")
            Assert.AreEqual(23, result(0).Episode, "Falsche Folge erkannt")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub Erkennung_Aufloesungsmarker_E1080_Bei_Staffel_12()
            'Arrange
            Dim strPath As String = "Y:\Test_Serie\Doctor Who\Season 12\Doctor Who - S12E05E1080 - Die Arche im Weltraum - Teil 1 - Doctorwho S12E1080.mkv"

            'Act
            Dim result As List(Of Scanner.EpisodeItem) = Erkennen(strPath)

            'Assert
            Assert.AreEqual(1, result.Count, "Es darf nur eine Folge erkannt werden, gefunden wurden {0}", result.Count)
            Assert.AreEqual(12, result(0).Season, "Falsche Staffel erkannt")
            Assert.AreEqual(5, result(0).Episode, "Falsche Folge erkannt")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub Erkennung_Aufloesungsmarker_E720_Wird_Nicht_Als_Folge_Gelesen()
            'Arrange
            Dim strPath As String = "Y:\Test_Serie\Doctor Who\Season 08\Doctor Who - S08E05E720 - Die Maschine des Boesen - Teil 1 - Doctorwho S08E720.mkv"

            'Act
            Dim result As List(Of Scanner.EpisodeItem) = Erkennen(strPath)

            'Assert
            Assert.AreEqual(1, result.Count, "Es darf nur eine Folge erkannt werden, gefunden wurden {0}", result.Count)
            Assert.AreEqual(8, result(0).Season, "Falsche Staffel erkannt")
            Assert.AreEqual(5, result(0).Episode, "Falsche Folge erkannt")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub Erkennung_Aufloesungsmarker_1080p_Wird_Nicht_Als_Teil_Gelesen()
            'Arrange - "1080p" direkt hinter der Folge
            Dim strPath As String = "Y:\Test_Serie\Doctor Who\Season 05\Doctor Who - S05E23 1080p - Das Netz der Angst.mkv"

            'Act
            Dim result As List(Of Scanner.EpisodeItem) = Erkennen(strPath)

            'Assert
            Assert.AreEqual(1, result.Count, "Es darf nur eine Folge erkannt werden, gefunden wurden {0}", result.Count)
            Assert.AreEqual(23, result(0).Episode, "Falsche Folge erkannt")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub Erkennung_Codec_x264_Wird_Nicht_Als_Teil_Gelesen()
            'Arrange - der Codec haengt direkt an der Teile-Nummer
            Dim strPath As String = "Y:\Test_Serie\Doctor Who\Season 05\Doctor Who - S05E23-x264 - Das Netz der Angst.mkv"

            'Act
            Dim result As List(Of Scanner.EpisodeItem) = Erkennen(strPath)

            'Assert
            Assert.AreEqual(1, result.Count, "Es darf nur eine Folge erkannt werden, gefunden wurden {0}", result.Count)
            Assert.AreEqual(23, result(0).Episode, "Falsche Folge erkannt")
        End Sub

#End Region 'Aufloesungsmarker'

#Region "Gegenproben"

        <UnitTest>
        <TestMethod()>
        Public Sub Erkennung_Echte_Zweite_Folge_Wird_Weiterhin_Gefunden()
            'Arrange - zwei Folgen in einer Datei
            Dim strPath As String = "Y:\Test_Serie\Doctor Who\Season 05\Doctor Who - S05E23 - Teil 1 und S05E24 - Teil 2.mkv"

            'Act
            Dim result As List(Of Scanner.EpisodeItem) = Erkennen(strPath)

            'Assert
            Assert.AreEqual(2, result.Count, "Beide Folgen muessen erkannt werden, gefunden wurden {0}", result.Count)
            Assert.AreEqual(23, result(0).Episode, "Falsche erste Folge erkannt")
            Assert.AreEqual(24, result(1).Episode, "Falsche zweite Folge erkannt")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub Erkennung_Echte_Teil_Nummer_Wird_Weiterhin_Gefunden()
            'Arrange - "S05E23-2" ist eine echte zweite Datei, keine Aufloesung
            Dim strPath As String = "Y:\Test_Serie\Doctor Who\Season 05\Doctor Who - S05E23-2 - Das Netz der Angst.mkv"

            'Act
            Dim result As List(Of Scanner.EpisodeItem) = Erkennen(strPath)

            'Assert
            Assert.AreEqual(2, result.Count, "Die Teile-Nummer muss erkannt werden, gefunden wurden {0}", result.Count)
            Assert.AreEqual(23, result(0).Episode, "Falsche erste Folge erkannt")
            Assert.AreEqual(2, result(1).Episode, "Falsche zweite Folge erkannt")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub Erkennung_Ohne_Aufloesung_bleibt_Unveraendert()
            'Arrange - einfacher Dateiname ohne jeden Zusatz
            Dim strPath As String = "Y:\Test_Serie\Doctor Who\Season 05\Doctor Who - S05E23 - Das Netz der Angst.mkv"

            'Act
            Dim result As List(Of Scanner.EpisodeItem) = Erkennen(strPath)

            'Assert
            Assert.AreEqual(1, result.Count, "Es darf nur eine Folge erkannt werden, gefunden wurden {0}", result.Count)
            Assert.AreEqual(5, result(0).Season, "Falsche Staffel erkannt")
            Assert.AreEqual(23, result(0).Episode, "Falsche Folge erkannt")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub Erkennung_Aufloesungsmarker_1080p_am_Ende_Wird_Nicht_Als_Teil_Gelesen()
            'Arrange - die Aufloesung steht als Suffix am Dateinamen
            Dim strPath As String = "Y:\Test_Serie\doctorwho\tv4a-doctorwho.s05e23-1080p.mkv"

            'Act
            Dim result As List(Of Scanner.EpisodeItem) = Erkennen(strPath)

            'Assert
            Assert.AreEqual(1, result.Count, "Es darf nur eine Folge erkannt werden, gefunden wurden {0}", result.Count)
            Assert.AreEqual(5, result(0).Season, "Falsche Staffel erkannt")
            Assert.AreEqual(23, result(0).Episode, "Falsche Folge erkannt")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub Erkennung_Aufloesungsmarker_1080p_am_Ende_Bei_Staffel_18()
            'Arrange
            Dim strPath As String = "Y:\Test_Serie\doctorwho\tv4a-doctorwho.s18e01-1080p.mkv"

            'Act
            Dim result As List(Of Scanner.EpisodeItem) = Erkennen(strPath)

            'Assert
            Assert.AreEqual(1, result.Count, "Es darf nur eine Folge erkannt werden, gefunden wurden {0}", result.Count)
            Assert.AreEqual(18, result(0).Season, "Falsche Staffel erkannt")
            Assert.AreEqual(1, result(0).Episode, "Falsche Folge erkannt")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub Erkennung_Aufloesungsmarker_1080p_mit_Punkt_bleibt_Erkennung_von_Teil_2()
            'Arrange - "2.mp4" ist eine echte Teile-Nummer, keine Aufloesung
            Dim strPath As String = "Y:\Test_Serie\doctorwho\tv4a-doctorwho.s05e23-2.mp4"

            'Act
            Dim result As List(Of Scanner.EpisodeItem) = Erkennen(strPath)

            'Assert
            Assert.AreEqual(2, result.Count, "Die Teile-Nummer muss erkannt werden, gefunden wurden {0}", result.Count)
            Assert.AreEqual(23, result(0).Episode, "Falsche erste Folge erkannt")
            Assert.AreEqual(2, result(1).Episode, "Falsche zweite Folge erkannt")
        End Sub

#End Region 'Gegenproben'

    End Class

End Namespace
