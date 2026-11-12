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
Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Imports EmberAPI
Imports UnitTests

Namespace EmberTests

    ''' <summary>
    ''' Tests fuer die Zusammenfuehrung der Scraper-Ergebnisse von TV-Episoden.
    ''' </summary>
    ''' <remarks>
    ''' Die Merge-Funktionen selbst sind an Master.DB und Master.eSettings gekoppelt
    ''' und deshalb nicht isoliert testbar. Getestet werden daher die reinen
    ''' Entscheidungsfunktionen in clsAPINFO, die ohne Master auskommen.
    ''' </remarks>
    <TestClass()> Public Class Test_clsAPITVMerge_NFO

        ''' <summary>
        ''' Erzeugt einen Scraper-Treffer fuer eine Episode.
        ''' </summary>
        Private Function NewScrapedEpisode(ByVal titel As String, ByVal aired As String) As MediaContainers.EpisodeDetails
            Return New MediaContainers.EpisodeDetails With {
                .Title = titel,
                .Aired = aired,
                .Season = 7,
                .Episode = 14}
        End Function

        ''' <summary>
        ''' Ohne Scraper-Treffer gibt es nichts auszuwaehlen.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub WaehlePassendeEpisodeDetails_OhneTreffer_LeereListe()
            'Arrange
            Dim lScraped As New List(Of MediaContainers.EpisodeDetails)

            'Act
            Dim lErgebnis As List(Of MediaContainers.EpisodeDetails) = NFO.WaehlePassendeEpisodeDetails("Der Name des Doktors", "2013-05-18", lScraped)

            'Assert
            Assert.AreEqual(0, lErgebnis.Count, "Ohne Scraper-Treffer darf kein Treffer zurueckgegeben werden.")
        End Sub

        ''' <summary>
        ''' Passen mehrere Scraper-Treffer nicht zum lokalen Eintrag, werden sie verworfen.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub WaehlePassendeEpisodeDetails_KeinPassenderTreffer_Verworfen()
            'Arrange
            Dim lScraped As New List(Of MediaContainers.EpisodeDetails) From {
                NewScrapedEpisode("In Teufels Küche (2)", "2015-12-05"),
                NewScrapedEpisode("Die Ankunft der Pelikane", "2013-05-18")}

            'Act
            Dim lErgebnis As List(Of MediaContainers.EpisodeDetails) = NFO.WaehlePassendeEpisodeDetails("Der Name des Doktors", String.Empty, lScraped)

            'Assert
            Assert.AreEqual(0, lErgebnis.Count, "Kein Treffer passt zum lokalen Titel - die Zuordnung muss verworfen werden.")
        End Sub

        ''' <summary>
        ''' Von mehreren Treffern wird derjenige gewaehlt, dessen Titel zum lokalen Eintrag passt.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub WaehlePassendeEpisodeDetails_TitelTreffer_WaehltPassendenTreffer()
            'Arrange
            Dim lScraped As New List(Of MediaContainers.EpisodeDetails) From {
                NewScrapedEpisode("In Teufels Küche (2)", "2015-12-05"),
                NewScrapedEpisode("Der Name des Doktors", "2013-05-18")}

            'Act
            Dim lErgebnis As List(Of MediaContainers.EpisodeDetails) = NFO.WaehlePassendeEpisodeDetails("Der Name des Doktors", String.Empty, lScraped)

            'Assert
            Assert.AreEqual(1, lErgebnis.Count, "Nur der zum lokalen Titel passende Treffer darf uebernommen werden.")
            Assert.AreEqual("Der Name des Doktors", lErgebnis.Item(0).Title, "Der passende Treffer muss ausgewaehlt werden.")
        End Sub

        ''' <summary>
        ''' Ein abweichender Titel wird auch dann verworfen, wenn das Ausstrahlungsdatum passt.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub WaehlePassendeEpisodeDetails_DatumsTreffer_WaehltPassendenTreffer()
            'Arrange
            Dim lScraped As New List(Of MediaContainers.EpisodeDetails) From {
                NewScrapedEpisode("In Teufels Küche (2)", "2015-12-05"),
                NewScrapedEpisode("The Name of the Doctor", "2013-05-18")}

            'Act
            Dim lErgebnis As List(Of MediaContainers.EpisodeDetails) = NFO.WaehlePassendeEpisodeDetails("Der Name des Doktors", "2013-05-18", lScraped)

            'Assert
            Assert.AreEqual(1, lErgebnis.Count, "Nur der zum lokalen Datum passende Treffer darf uebernommen werden.")
            Assert.AreEqual("The Name of the Doctor", lErgebnis.Item(0).Title, "Der passende Treffer muss ausgewaehlt werden.")
        End Sub

        ''' <summary>
        ''' Datumsangaben in unterschiedlichen Formaten sind vergleichbar.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub WaehlePassendeEpisodeDetails_Datumsformate_SindVergleichbar()
            'Arrange
            Dim lScraped As New List(Of MediaContainers.EpisodeDetails) From {
                NewScrapedEpisode("In Teufels Küche (2)", "2015-12-05"),
                NewScrapedEpisode("The Name of the Doctor", "2013-05-18")}

            'Act
            Dim lErgebnis As List(Of MediaContainers.EpisodeDetails) = NFO.WaehlePassendeEpisodeDetails("Der Name des Doktors", "18.05.2013", lScraped)

            'Assert
            Assert.AreEqual(1, lErgebnis.Count, "Das Datum muss unabhaengig vom Format verglichen werden.")
            Assert.AreEqual("The Name of the Doctor", lErgebnis.Item(0).Title, "Der zum Datum passende Treffer muss ausgewaehlt werden.")
        End Sub

        ''' <summary>
        ''' Der beste Treffer steht in der Ergebnisliste vorne.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub WaehlePassendeEpisodeDetails_BesterTreffer_KommtZuerst()
            'Arrange
            Dim lScraped As New List(Of MediaContainers.EpisodeDetails) From {
                NewScrapedEpisode("Doctor Who - Special", "2013-05-18"),
                NewScrapedEpisode("Doctor Who", "2013-05-18")}

            'Act
            Dim lErgebnis As List(Of MediaContainers.EpisodeDetails) = NFO.WaehlePassendeEpisodeDetails("Doctor Who", String.Empty, lScraped)

            'Assert
            Assert.AreEqual(2, lErgebnis.Count, "Beide Treffer passen zum lokalen Titel.")
            Assert.AreEqual("Doctor Who", lErgebnis.Item(0).Title, "Der exakt passende Treffer muss vorne stehen.")
        End Sub

        ''' <summary>
        ''' Ein Titel, der den Dateinamen-Zaehler enthaelt, passt trotzdem zum Treffer.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub WaehlePassendeEpisodeDetails_TitelMitDateinameZaehler_WirdAkzeptiert()
            'Arrange
            Dim lScraped As New List(Of MediaContainers.EpisodeDetails) From {
                NewScrapedEpisode("In Teufels Küche (2)", "2015-12-05"),
                NewScrapedEpisode("The Ambassadors of Death", "2013-06-01")}

            'Act
            Dim lErgebnis As List(Of MediaContainers.EpisodeDetails) = NFO.WaehlePassendeEpisodeDetails("The Ambassadors of Death (3)", String.Empty, lScraped)

            'Assert
            Assert.AreEqual(1, lErgebnis.Count, "Der Zaehler im Dateinamen-Titel darf die Zuordnung nicht verhindern.")
            Assert.AreEqual("The Ambassadors of Death", lErgebnis.Item(0).Title, "Der passende Treffer muss ausgewaehlt werden.")
        End Sub

        ''' <summary>
        ''' Ohne lokalen Titel und ohne lokales Datum gibt es keine Auswahlkriterien.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub WaehlePassendeEpisodeDetails_OhneLokaleIdentitaet_AlleTreffer()
            'Arrange
            Dim lScraped As New List(Of MediaContainers.EpisodeDetails) From {
                NewScrapedEpisode("In Teufels Küche (2)", "2015-12-05"),
                NewScrapedEpisode("Der Name des Doktors", "2013-05-18")}

            'Act
            Dim lErgebnis As List(Of MediaContainers.EpisodeDetails) = NFO.WaehlePassendeEpisodeDetails(String.Empty, String.Empty, lScraped)

            'Assert
            Assert.AreEqual(2, lErgebnis.Count, "Ohne Titel und ohne Datum darf kein Treffer verworfen werden.")
        End Sub

        ''' <summary>
        ''' Ein nicht gesetzter lokaler Titel darf durch einen Scraped-Titel ersetzt werden.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub DarfEpisodenTitelErsetztWerden_KeinLokalerTitel_Erlaubt()
            'Act
            Dim bErlaubt As Boolean = NFO.DarfEpisodenTitelErsetztWerden(String.Empty, "The Name of the Doctor")

            'Assert
            Assert.IsTrue(bErlaubt, "Ohne lokalen Titel darf der Scraped-Titel uebernommen werden.")
        End Sub

        ''' <summary>
        ''' Ein lokaler Titel, der nur aus der Dateiname-Erkennung stammt, gilt als nicht gesetzt.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub DarfEpisodenTitelErsetztWerden_TitelMitEpisodeMarker_Erlaubt()
            'Act
            Dim bErlaubt As Boolean = NFO.DarfEpisodenTitelErsetztWerden("Doctor Who S07E14", "The Name of the Doctor")

            'Assert
            Assert.IsTrue(bErlaubt, "Ein Titel, der nur die Episodennummer enthaelt, gilt als nicht gesetzt.")
        End Sub

        ''' <summary>
        ''' Ein zum lokalen Titel passender Scraped-Titel darf uebernommen werden.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub DarfEpisodenTitelErsetztWerden_PassenderTitel_Erlaubt()
            'Act
            Dim bErlaubt As Boolean = NFO.DarfEpisodenTitelErsetztWerden("Der Name des Doktors", "Der Name des Doktors")

            'Assert
            Assert.IsTrue(bErlaubt, "Ein passender Scraped-Titel darf uebernommen werden.")
        End Sub

        ''' <summary>
        ''' Ein abweichender Scraped-Titel darf den lokalen Titel nicht ueberschreiben.
        ''' </summary>
        <UnitTest>
        <TestMethod()>
        Public Sub DarfEpisodenTitelErsetztWerden_AbweichenderTitel_Verboten()
            'Act
            Dim bErlaubt As Boolean = NFO.DarfEpisodenTitelErsetztWerden("Der Name des Doktors", "In Teufels Küche")

            'Assert
            Assert.IsFalse(bErlaubt, "Ein abweichender Scraped-Titel darf den lokalen Titel nicht ueberschreiben.")
        End Sub

    End Class

End Namespace
