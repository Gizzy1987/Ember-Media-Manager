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

Imports System.IO
Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Imports EmberAPI
Imports UnitTests

Namespace EmberTests

    ''' <summary>
    ''' Tests fuer den Konflikt zwischen der Episodennummer aus dem Dateinamen und der
    ''' Episodennummer aus der NFO-Datei.
    ''' </summary>
    ''' <remarks>
    ''' Der Scanner selbst ist an Master.DB und Master.eSettings gekoppelt und deshalb
    ''' nicht isoliert testbar. Getestet werden daher die reinen Entscheidungsfunktionen
    ''' in clsAPIScanner, die ohne Master auskommen. Der Konfliktfall stammt aus dem
    ''' Befundbericht vom 10.11.2026 (Doctor Who): die NFO-Datei enthielt die Nummer 14,
    ''' der Dateiname dagegen S07E13.
    ''' </remarks>
    <TestClass()> Public Class Test_clsAPIScannerNFO

        ''' <summary>
        ''' Legt eine NFO-Datei im TEMP-Verzeichnis ab und gibt deren Pfad zurueck.
        ''' </summary>
        Private Function CreateNFOFile(ByVal strEpisodeNumber As String, ByVal strTitle As String) As String
            Dim strFolder As String = Path.Combine(Path.GetTempPath(), "EmberMM_Test_NFO")
            If Not Directory.Exists(strFolder) Then Directory.CreateDirectory(strFolder)

            Dim strFile As String = Path.Combine(strFolder, "Doctor Who - S07E13 - Der Name des Doktors.nfo")
            Dim strContent As String = String.Join(Environment.NewLine, New String() {
                "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes"" ?>",
                "<episodedetails>",
                "  <title>" & strTitle & "</title>",
                "  <season>7</season>",
                "  <episode>" & strEpisodeNumber & "</episode>",
                "  <aired>2013-05-18</aired>",
                "  <plot>Befundbericht vom 10.11.2026</plot>",
                "</episodedetails>"})

            File.WriteAllText(strFile, strContent, Encoding.UTF8)
            Return strFile
        End Function

        ''' <summary>
        ''' Der echte Konfliktfall: die NFO-Datei nennt die Nummer 14, der Dateiname S07E13.
        ''' </summary>
        <UnitTest>
        <TestMethod()> Public Sub ResolveEpisodeNumber_NFODateiMitFalscherNummer_NummerAusDateinameGewinnt()
            'Arrange
            Dim strNFO As String = CreateNFOFile("14", "Der Name des Doktors")
            Dim cNFO As MediaContainers.EpisodeDetails = NFO.LoadFromNFO_TVEpisode(strNFO, 7, 13)

            'Act
            Dim iErgebnis As Integer = Scanner.ResolveEpisodeNumber(13, cNFO.Episode, strNFO)

            'Assert
            Assert.AreEqual(14, cNFO.Episode, "Die NFO-Datei muss die Nummer 14 liefern, sonst prueft der Test nichts.")
            Assert.AreEqual(13, iErgebnis, "Bei einem Konflikt muss die Nummer aus dem Dateinamen gewinnen.")
        End Sub

        ''' <summary>
        ''' Ohne Widerspruch bleibt die Nummer aus der NFO-Datei erhalten.
        ''' </summary>
        <UnitTest>
        <TestMethod()> Public Sub ResolveEpisodeNumber_KeinWiderspruch_NummerAusNFOWirdVerwendet()
            'Act
            Dim iErgebnis As Integer = Scanner.ResolveEpisodeNumber(13, 13, "Doctor Who - S07E13 - Der Name des Doktors.mkv")

            'Assert
            Assert.AreEqual(13, iErgebnis, "Ohne Widerspruch darf die Nummer nicht veraendert werden.")
        End Sub

        ''' <summary>
        ''' Enthaelt der Dateiname keine Nummer, gilt weiterhin die Nummer aus der NFO-Datei.
        ''' </summary>
        <UnitTest>
        <TestMethod()> Public Sub ResolveEpisodeNumber_KeineNummerImDateinamen_NummerAusNFOWirdVerwendet()
            'Act
            Dim iErgebnis As Integer = Scanner.ResolveEpisodeNumber(-1, 14, "Doctor Who - Der Name des Doktors.mkv")

            'Assert
            Assert.AreEqual(14, iErgebnis, "Ohne Nummer im Dateinamen muss die Nummer aus der NFO-Datei verwendet werden.")
        End Sub

        ''' <summary>
        ''' Enthaelt die NFO-Datei keine Nummer, wird die Nummer aus dem Dateinamen verwendet.
        ''' </summary>
        <UnitTest>
        <TestMethod()> Public Sub ResolveEpisodeNumber_KeineNummerInNFO_NummerAusDateinameWirdVerwendet()
            'Act
            Dim iErgebnis As Integer = Scanner.ResolveEpisodeNumber(13, -1, "Doctor Who - S07E13 - Der Name des Doktors.mkv")

            'Assert
            Assert.AreEqual(13, iErgebnis, "Ohne Nummer in der NFO-Datei muss die Nummer aus dem Dateinamen verwendet werden.")
        End Sub

        ''' <summary>
        ''' Abweichende Nummern sind ein Widerspruch.
        ''' </summary>
        <UnitTest>
        <TestMethod()> Public Sub HasEpisodeNumberConflict_AbweichendeNummern_Widerspruch()
            'Act
            Dim bErgebnis As Boolean = Scanner.HasEpisodeNumberConflict(13, 14)

            'Assert
            Assert.IsTrue(bErgebnis, "Abweichende Nummern muessen als Widerspruch erkannt werden.")
        End Sub

        ''' <summary>
        ''' Gleiche Nummern sind kein Widerspruch.
        ''' </summary>
        <UnitTest>
        <TestMethod()> Public Sub HasEpisodeNumberConflict_GleicheNummern_KeinWiderspruch()
            'Act
            Dim bErgebnis As Boolean = Scanner.HasEpisodeNumberConflict(13, 13)

            'Assert
            Assert.IsFalse(bErgebnis, "Gleiche Nummern sind kein Widerspruch.")
        End Sub

        ''' <summary>
        ''' Eine unbekannte Nummer kann keinen Widerspruch ausloesen.
        ''' </summary>
        <UnitTest>
        <TestMethod()> Public Sub HasEpisodeNumberConflict_UnbekannteNummer_KeinWiderspruch()
            'Act
            Dim bOhneNummerImDateinamen As Boolean = Scanner.HasEpisodeNumberConflict(-1, 14)
            Dim bOhneNummerInNFO As Boolean = Scanner.HasEpisodeNumberConflict(13, -1)

            'Assert
            Assert.IsFalse(bOhneNummerImDateinamen, "Ohne Nummer im Dateinamen kann es keinen Widerspruch geben.")
            Assert.IsFalse(bOhneNummerInNFO, "Ohne Nummer in der NFO-Datei kann es keinen Widerspruch geben.")
        End Sub

        ''' <summary>
        ''' Der Konfliktfall aus dem Auftrag: NFO mit der Nummer 14 zu einer Datei mit S07E13
        ''' und einem nicht passenden NFO-Titel. Die Nummer wird korrigiert, der Titel der NFO
        ''' wird verworfen.
        ''' </summary>
        <UnitTest>
        <TestMethod()> Public Sub Konfliktfall_NummerAusDateiname_Und_TitelAusNFOVerworfen()
            'Arrange
            Dim strNFO As String = CreateNFOFile("14", "Der Name des Doktors")
            Dim cNFO As MediaContainers.EpisodeDetails = NFO.LoadFromNFO_TVEpisode(strNFO, 7, 13)
            Dim strFileTitle As String = "S07E13 - The Rings of Akhaten"

            'Act
            Dim iNummer As Integer = Scanner.ResolveEpisodeNumber(13, cNFO.Episode, strNFO)
            Dim bTitelPasst As Boolean = Scanner.IsNFOTitleConsistent(strFileTitle, cNFO.Title)

            'Assert
            Assert.AreEqual(13, iNummer, "Die Nummer aus dem Dateinamen muss gewinnen.")
            Assert.IsFalse(bTitelPasst, "Ein nicht passender NFO-Titel muss als Widerspruch erkannt werden.")
        End Sub

        ''' <summary>
        ''' Ein zum Dateinamen passender Titel wird nicht verworfen.
        ''' </summary>
        <UnitTest>
        <TestMethod()> Public Sub IsNFOTitleConsistent_PassenderTitel_WirdBehalten()
            'Act
            Dim bErgebnis As Boolean = Scanner.IsNFOTitleConsistent("S07E13 - Der Name des Doktors", "Der Name des Doktors")

            'Assert
            Assert.IsTrue(bErgebnis, "Ein passender Titel darf nicht verworfen werden.")
        End Sub

        ''' <summary>
        ''' Ein abweichender Titel wird als Widerspruch erkannt.
        ''' </summary>
        <UnitTest>
        <TestMethod()> Public Sub IsNFOTitleConsistent_AbweichenderTitel_WirdVerworfen()
            'Act
            Dim bErgebnis As Boolean = Scanner.IsNFOTitleConsistent("S07E13 - The Rings of Akhaten", "Der Name des Doktors")

            'Assert
            Assert.IsFalse(bErgebnis, "Ein abweichender Titel muss verworfen werden.")
        End Sub

        ''' <summary>
        ''' Enthaelt der Dateiname keinen Folgentitel, gibt es nichts zu vergleichen - der Titel
        ''' aus der NFO-Datei wird dann behalten.
        ''' </summary>
        <UnitTest>
        <TestMethod()> Public Sub IsNFOTitleConsistent_KeinFolgentitelImDateinamen_WirdBehalten()
            'Act
            Dim bErgebnis As Boolean = Scanner.IsNFOTitleConsistent("S07E13", "Der Name des Doktors")

            'Assert
            Assert.IsTrue(bErgebnis, "Ohne Folgentitel im Dateinamen darf der NFO-Titel nicht verworfen werden.")
        End Sub

    End Class

End Namespace
