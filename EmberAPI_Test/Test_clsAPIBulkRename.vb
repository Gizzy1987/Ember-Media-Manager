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
Imports System.Drawing
Imports System.Text
Imports System.Threading
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Imports EmberAPI
Imports UnitTests
Imports generic.EmberCore.BulkRename

Namespace EmberTests

    ''' <summary>
    ''' Tests der Mehrfach-Datei-Logik des BulkRename-Addons (Dialog-Anzeige,
    ''' Suffix-Vergabe und Eindeutigkeit der Zielnamen).
    ''' </summary>
    <TestClass()> Public Class Test_clsAPIBulkRename

#Region "Dialog-Anzeige"

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_BuildEpisodeLabel_zeigt_den_echten_Dateinamen()
            'Arrange
            Dim fileName As String = "Doctor Who - S07E14 - The Ambassadors of Death (3).mkv"
            Dim dbTitle As String = "Der Name des Doktors"

            'Act
            Dim label As String = BulkRenameLogic.BuildEpisodeLabel(7, 14, fileName, dbTitle)

            'Assert
            Assert.IsTrue(label.Contains("S07E14"), "Die Staffel-/Folgenangabe fehlt: <{0}>", label)
            Assert.IsTrue(label.Contains(fileName), "Der echte Dateiname fehlt in der Anzeige: <{0}>", label)
            Assert.IsTrue(label.Contains(dbTitle), "Der DB-Titel fehlt als Zusatz in der Anzeige: <{0}>", label)
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_BuildEpisodeLabel_ohne_DB_Titel()
            'Arrange
            Dim fileName As String = "Doctor Who - S07E13 - Der Name des Doktors.mkv"

            'Act
            Dim label As String = BulkRenameLogic.BuildEpisodeLabel(7, 13, fileName, String.Empty)

            'Assert
            Assert.IsTrue(label.Contains(fileName), "Der echte Dateiname fehlt in der Anzeige: <{0}>", label)
            Assert.IsFalse(label.Contains("(DB:"), "Ohne DB-Titel darf kein DB-Zusatz erscheinen: <{0}>", label)
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_BuildEpisodeLabel_unterscheidet_Dateien_mit_gleichem_Namen()
            'Arrange
            Dim fileName1 As String = "Doctor Who - S07E14 - Der Name des Doktors.mkv"
            Dim fileName2 As String = "Doctor Who - S07E13 - Der Name des Doktors.mkv"

            'Act
            Dim label1 As String = BulkRenameLogic.BuildEpisodeLabel(7, 14, fileName1, "Der Name des Doktors")
            Dim label2 As String = BulkRenameLogic.BuildEpisodeLabel(7, 13, fileName2, "Der Name des Doktors")

            'Assert
            Assert.AreNotEqual(label1, label2, "Zwei verschiedene Dateien muessen unterschiedliche Anzeigen ergeben")
        End Sub

#End Region 'Dialog-Anzeige

#Region "Suffix-Vergabe"

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_GetMultiFileSuffix_Versions()
            'Arrange/Act/Assert
            Assert.AreEqual(String.Empty, BulkRenameLogic.GetMultiFileSuffix(BulkRenameLogic.ModeVersions, 1),
                            "Die erste Version darf kein Suffix bekommen")
            Assert.AreEqual(" - 2", BulkRenameLogic.GetMultiFileSuffix(BulkRenameLogic.ModeVersions, 2),
                            "Die zweite Version muss das Suffix ' - 2' bekommen")
            Assert.AreEqual(" - 3", BulkRenameLogic.GetMultiFileSuffix(BulkRenameLogic.ModeVersions, 3),
                            "Die dritte Version muss das Suffix ' - 3' bekommen")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_GetMultiFileSuffix_Parts()
            'Arrange/Act/Assert
            Assert.AreEqual("-part-1", BulkRenameLogic.GetMultiFileSuffix(BulkRenameLogic.ModeParts, 1),
                            "Der erste Teil muss das Suffix '-part-1' bekommen")
            Assert.AreEqual("-part-2", BulkRenameLogic.GetMultiFileSuffix(BulkRenameLogic.ModeParts, 2),
                            "Der zweite Teil muss das Suffix '-part-2' bekommen")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_GetMultiFileSuffix_ohne_Modus()
            'Arrange/Act/Assert
            Assert.AreEqual(String.Empty, BulkRenameLogic.GetMultiFileSuffix(String.Empty, 1),
                            "Ohne Modus darf kein Suffix vergeben werden")
            Assert.AreEqual(String.Empty, BulkRenameLogic.GetMultiFileSuffix(String.Empty, 2),
                            "Ohne Modus darf kein Suffix vergeben werden")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_AssignPartNumbers_gleiche_Quelldatei_bekommt_gleiche_Nummer()
            'Arrange - zwei Datenbankzeilen zeigen auf dieselbe Datei, die dritte auf eine andere
            Dim fileKeys As New List(Of String) From {"Y:\Serie\Season 07\Folge.mkv",
                                                       "Y:\Serie\Season 07\Folge.mkv",
                                                       "Y:\Serie\Season 07\Andere Folge.mkv"}

            'Act
            Dim numbers As IList(Of Integer) = BulkRenameLogic.AssignPartNumbers(fileKeys)

            'Assert
            Assert.AreEqual(3, numbers.Count, "Es muss eine Nummer je Datei zurueckgegeben werden")
            Assert.AreEqual(1, numbers(0), "Die erste Datei muss die Nummer 1 bekommen")
            Assert.AreEqual(1, numbers(1), "Dieselbe Quelldatei muss dieselbe Nummer bekommen")
            Assert.AreEqual(2, numbers(2), "Eine andere Datei muss eine eigene Nummer bekommen")
        End Sub

#End Region 'Suffix-Vergabe

#Region "Eindeutigkeit der Zielnamen"

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_ResolveGroupTargets_Zielnamen_sind_eindeutig()
            'Arrange - vier Dateien derselben Folge mit identischem DB-Titel
            Dim baseNames As New List(Of String) From {"Doctor Who - S07E14 - Der Name des Doktors",
                                                        "Doctor Who - S07E14 - Der Name des Doktors",
                                                        "Doctor Who - S07E14 - Der Name des Doktors",
                                                        "Doctor Who - S07E14 - Der Name des Doktors"}
            Dim preferred As New List(Of Integer) From {1, 2, 3, 4}

            'Act
            Dim targets As IList(Of BulkRenameTarget) = BulkRenameLogic.ResolveGroupTargets(baseNames, BulkRenameLogic.ModeVersions, preferred, Nothing)

            'Assert
            Assert.AreEqual(4, targets.Count, "Es muss ein Ziel je Datei zurueckgegeben werden")
            For Each t As BulkRenameTarget In targets
                Assert.IsFalse(String.IsNullOrEmpty(t.Name), "Jede Datei braucht einen Zielnamen")
            Next
            Assert.AreEqual(4, targets.Select(Function(t) t.Name).Distinct().Count, "Die Zielnamen muessen eindeutig sein")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_ResolveGroupTargets_erster_Zielname_bleibt_ohne_Suffix()
            'Arrange
            Dim baseNames As New List(Of String) From {"Folge", "Folge"}
            Dim preferred As New List(Of Integer) From {1, 2}

            'Act
            Dim targets As IList(Of BulkRenameTarget) = BulkRenameLogic.ResolveGroupTargets(baseNames, BulkRenameLogic.ModeVersions, preferred, Nothing)

            'Assert
            Assert.AreEqual("Folge", targets(0).Name, "Die erste Datei muss den Basisnamen behalten")
            Assert.AreEqual("Folge - 2", targets(1).Name, "Die zweite Datei muss ein eindeutiges Suffix bekommen")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_ResolveGroupTargets_belegter_Name_wird_umgangen()
            'Arrange - der Basisname und "Folge - 2" sind schon vergeben
            Dim baseNames As New List(Of String) From {"Folge", "Folge"}
            Dim preferred As New List(Of Integer) From {1, 2}
            Dim taken As New HashSet(Of String) From {"Folge", "Folge - 2"}

            'Act
            Dim targets As IList(Of BulkRenameTarget) = BulkRenameLogic.ResolveGroupTargets(baseNames, BulkRenameLogic.ModeVersions, preferred,
                                                                                         Function(index As Integer, candidate As String) As Boolean
                                                                                             Return taken.Contains(candidate)
                                                                                         End Function)

            'Assert
            Assert.AreNotEqual(targets(0).Name, "Folge", "Ein bereits vergebener Name darf nicht vergeben werden")
            Assert.AreNotEqual(targets(1).Name, "Folge - 2", "Ein bereits vergebener Name darf nicht vergeben werden")
            Assert.AreNotEqual(targets(0).Name, targets(1).Name, "Die Zielnamen muessen sich unterscheiden")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_ResolveGroupTargets_ohne_Modus_bleibt_eindeutig()
            'Arrange - der Dialog wurde abgebrochen, es gibt daher keinen Modus
            Dim baseNames As New List(Of String) From {"Folge", "Folge"}
            Dim preferred As New List(Of Integer) From {1, 2}

            'Act
            Dim targets As IList(Of BulkRenameTarget) = BulkRenameLogic.ResolveGroupTargets(baseNames, String.Empty, preferred, Nothing)

            'Assert
            Assert.AreEqual(2, targets.Count, "Es muss ein Ziel je Datei zurueckgegeben werden")
            Assert.AreNotEqual(targets(0).Name, targets(1).Name, "Auch ohne Modus muessen die Zielnamen eindeutig sein")
        End Sub

#End Region 'Eindeutigkeit der Zielnamen

#Region "Entscheidung je Folge-Gruppe"

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_Entscheidung_wird_kodiert_und_wieder_gelesen()
            'Arrange
            Dim decision As New MultiFileDecision With {.GroupKey = "5_23", .Mode = BulkRenameLogic.ModeParts}
            decision.SelectedFiles.Add("C:\Serie\a.mkv")
            decision.SelectedFiles.Add("C:\Serie\b.mkv")

            'Act
            Dim encoded As String = BulkRenameLogic.EncodeDecision(decision)
            Dim decoded As MultiFileDecision = BulkRenameLogic.DecodeDecision("5_23", encoded)

            'Assert
            Assert.AreEqual(BulkRenameLogic.ModeParts, decoded.Mode, "Der Modus muss erhalten bleiben")
            Assert.AreEqual("5_23", decoded.GroupKey, "Der Gruppenschluessel muss erhalten bleiben")
            Assert.AreEqual(2, decoded.SelectedFiles.Count, "Beide Dateien muessen erhalten bleiben")
            Assert.AreEqual("C:\Serie\a.mkv", decoded.SelectedFiles(0), "Die erste Datei muss erhalten bleiben")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_Alter_Wert_gilt_fuer_alle_Dateien_der_Gruppe()
            'Arrange - eine frueher gespeicherte Antwort kennt keine Dateiliste
            Dim decision As MultiFileDecision = BulkRenameLogic.DecodeDecision("5_23", BulkRenameLogic.ModeParts)

            'Assert
            Assert.AreEqual(BulkRenameLogic.ModeParts, decision.Mode, "Der Modus muss erhalten bleiben")
            Assert.IsTrue(decision.AllFiles, "Eine Antwort ohne Dateiliste gilt fuer alle Dateien")
            Assert.IsTrue(BulkRenameLogic.IsFileSelected(decision, "C:\beliebig.mkv"), "Ohne Dateiliste sind alle Dateien ausgewaehlt")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_Abgewahlte_Dateien_bleiben_abgewahlt()
            'Arrange - der Nutzer hat alle Dateien der Gruppe abgewahlt
            Dim decision As New MultiFileDecision With {.GroupKey = "5_23", .Mode = BulkRenameLogic.ModeNone}

            'Assert
            Assert.IsFalse(BulkRenameLogic.IsFileSelected(decision, "C:\beliebig.mkv"), "Eine leere Auswahl heisst: keine Datei gehoert zur Gruppe")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_Dateiliste_bleibt_beim_Speichern_erhalten()
            'Arrange
            Dim decision As New MultiFileDecision With {.GroupKey = "5_23", .Mode = BulkRenameLogic.ModeParts}
            decision.SelectedFiles.Add("C:\a.mkv")

            'Act
            Dim decoded As MultiFileDecision = BulkRenameLogic.DecodeDecision("5_23", BulkRenameLogic.EncodeDecision(decision))

            'Assert
            Assert.IsFalse(decoded.AllFiles, "Eine ausgetraegte Auswahl ist keine Auswahl fuer alle Dateien")
            Assert.AreEqual(1, decoded.SelectedFiles.Count, "Die ausgewaehlte Datei muss erhalten bleiben")
            Assert.IsTrue(BulkRenameLogic.IsFileSelected(decoded, "C:\a.mkv"), "Die ausgewaehlte Datei muss ausgewaehlt bleiben")
            Assert.IsFalse(BulkRenameLogic.IsFileSelected(decoded, "C:\b.mkv"), "Die abgewahlte Datei darf nicht ausgewaehlt sein")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_Nur_ausgewaehlte_Dateien_bekommen_Parts_Nummern()
            'Arrange - drei Dateien, zwei davon gehoeren zusammen
            Dim fileKeys As New List(Of String) From {"C:\a.mkv", "C:\b.mkv", "C:\c.mkv"}
            Dim baseNames As New List(Of String) From {"Folge", "Folge", "Folge"}
            Dim decision As New MultiFileDecision With {.Mode = BulkRenameLogic.ModeParts}
            decision.SelectedFiles.Add("C:\a.mkv")
            decision.SelectedFiles.Add("C:\b.mkv")

            'Act
            Dim targets As IList(Of BulkRenameTarget) = BulkRenameLogic.ResolveGroupTargetsBySelection(fileKeys, baseNames, decision, Nothing)

            'Assert
            Assert.AreEqual(3, targets.Count, "Es muss ein Ziel je Datei zurueckgegeben werden")
            Assert.AreEqual("Folge-part-1", targets(0).Name, "Die erste ausgewaehlte Datei ist Teil 1")
            Assert.AreEqual("Folge-part-2", targets(1).Name, "Die zweite ausgewaehlte Datei ist Teil 2")
            Assert.AreEqual("Folge", targets(2).Name, "Die nicht ausgewaehlte Datei darf kein Suffix bekommen")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_Versionen_gelten_nur_fuer_ausgewaehlte_Dateien()
            'Arrange
            Dim fileKeys As New List(Of String) From {"C:\a.mkv", "C:\b.mkv", "C:\c.mkv"}
            Dim baseNames As New List(Of String) From {"Folge", "Folge", "Folge"}
            Dim decision As New MultiFileDecision With {.Mode = BulkRenameLogic.ModeVersions}
            decision.SelectedFiles.Add("C:\a.mkv")

            'Act
            Dim targets As IList(Of BulkRenameTarget) = BulkRenameLogic.ResolveGroupTargetsBySelection(fileKeys, baseNames, decision, Nothing)

            'Assert
            Assert.AreEqual(3, targets.Count, "Es muss ein Ziel je Datei zurueckgegeben werden")
            Assert.AreEqual("Folge", targets(0).Name, "Die einzige ausgewaehlte Datei ist Version 1")
            Assert.AreEqual("Folge - 2", targets(1).Name, "Die uebrigen Dateien brauchen eindeutige Namen")
            Assert.AreEqual("Folge - 3", targets(2).Name, "Die uebrigen Dateien brauchen eindeutige Namen")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_Getrennt_laesst_keine_Teil_Nummern_entstehen()
            'Arrange - der Nutzer sagt: diese Dateien gehoeren nicht zusammen
            Dim fileKeys As New List(Of String) From {"C:\a.mkv", "C:\b.mkv"}
            Dim baseNames As New List(Of String) From {"Folge", "Folge"}
            Dim decision As New MultiFileDecision With {.Mode = BulkRenameLogic.ModeNone}

            'Act
            Dim targets As IList(Of BulkRenameTarget) = BulkRenameLogic.ResolveGroupTargetsBySelection(fileKeys, baseNames, decision, Nothing)

            'Assert
            Assert.AreEqual(2, targets.Count, "Es muss ein Ziel je Datei zurueckgegeben werden")
            Assert.AreEqual("Folge", targets(0).Name, "Getrennte Dateien bekommen keine Teil-Nummer")
            Assert.AreNotEqual(targets(0).Name, targets(1).Name, "Auch getrennte Dateien brauchen eindeutige Namen")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_Gruppen_Anzeige_enthaelt_Folge_und_Titel()
            'Act
            Dim label As String = BulkRenameLogic.BuildGroupLabel(5, 23, "Das Netz der Angst")

            'Assert
            Assert.IsTrue(label.Contains("S05E23"), "Die Staffel-/Folgenangabe fehlt: <{0}>", label)
            Assert.IsTrue(label.Contains("Das Netz der Angst"), "Der Titel fehlt in der Anzeige: <{0}>", label)
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_Einzelne_ausgewaehlte_Datei_bleibt_ohne_Suffix()
            'Arrange - der Nutzer hat nur eine Datei der Gruppe ausgewaehlt
            Dim fileKeys As New List(Of String) From {"C:\a.mkv", "C:\b.mkv"}
            Dim baseNames As New List(Of String) From {"Folge", "Folge"}
            Dim decision As New MultiFileDecision With {.Mode = BulkRenameLogic.ModeParts}
            decision.SelectedFiles.Add("C:\a.mkv")

            'Act
            Dim targets As IList(Of BulkRenameTarget) = BulkRenameLogic.ResolveGroupTargetsBySelection(fileKeys, baseNames, decision, Nothing)

            'Assert
            Assert.AreEqual(2, targets.Count, "Es muss ein Ziel je Datei zurueckgegeben werden")
            Assert.AreEqual("Folge", targets(0).Name, "Eine einzelne Datei bekommt kein Teil-Suffix")
            Assert.AreNotEqual(targets(0).Name, targets(1).Name, "Die Zielnamen muessen sich unterscheiden")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_Dialog_zeigt_die_Dateien_der_Gruppe()
            'Arrange
            Dim group As New MultiFileGroup With {.GroupKey = "5_23", .Label = "S05E23 - Das Netz der Angst"}
            group.Files.Add(New MultiFileFile With {.FileKey = "C:\Serie\a.mkv", .FileName = "a.mkv"})
            group.Files.Add(New MultiFileFile With {.FileKey = "C:\Serie\b.mkv", .FileName = "b.mkv"})

            Dim groups As New List(Of MultiFileGroup)
            groups.Add(group)

            'Act
            Using dlg As New dlgMultiFileQuestion("Doctor Who", groups, isTV:=True)
                Dim decisions As IList(Of MultiFileDecision) = dlg.Decisions

                'Assert
                Assert.AreEqual(1, decisions.Count, "Es muss eine Entscheidung je Gruppe geben")
                Assert.AreEqual(2, decisions(0).SelectedFiles.Count, "Beide Dateien der Gruppe muessen in der Liste stehen und ausgewaehlt sein")
                Assert.AreEqual(BulkRenameLogic.ModeParts, decisions(0).Mode, "Die Voreinstellung muss " & BulkRenameLogic.ModeParts & " sein")
            End Using
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_Gruppe_mit_gleicher_Folge_ist_eine_Mehrfach_Gruppe()
            'Arrange - zwei Dateien derselben Folge
            Dim keys As New List(Of String) From {"12|5", "12|5"}

            'Act
            Dim result As Boolean = BulkRenameLogic.IsSingleEpisode(keys)

            'Assert
            Assert.IsTrue(result, "Zwei Dateien derselben Folge sind eine Mehrfach-Datei-Gruppe")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_Gruppe_mit_verschiedenen_Folgen_ist_keine_Mehrfach_Gruppe()
            'Arrange - die Dateien gehoeren zu verschiedenen Folgen. Das passiert,
            'wenn die Datenbank aus einem alten Scan eine falsche Folge enthaelt.
            Dim keys As New List(Of String) From {"18|1", "18|2", "18|3"}

            'Act
            Dim result As Boolean = BulkRenameLogic.IsSingleEpisode(keys)

            'Assert
            Assert.IsFalse(result, "Dateien aus verschiedenen Folgen gehoeren nicht zu einer Mehrfach-Datei-Gruppe")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_Ohne_Erkennung_bleibt_die_Gruppe()
            'Arrange - keine Datei liess sich einer Folge zuordnen
            Dim keys As New List(Of String) From {"", ""}

            'Act
            Dim result As Boolean = BulkRenameLogic.IsSingleEpisode(keys)

            'Assert
            Assert.IsTrue(result, "Ohne Erkennung bleibt die Datenbank massgeblich")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_Nummern_nur_fuer_ausgewaehlte_Dateien()
            'Arrange
            Dim fileKeys As New List(Of String) From {"C:\a.mkv", "C:\b.mkv", "C:\c.mkv"}
            Dim decision As New MultiFileDecision With {.Mode = BulkRenameLogic.ModeParts}
            decision.SelectedFiles.Add("C:\a.mkv")
            decision.SelectedFiles.Add("C:\c.mkv")

            'Act
            Dim numbers As IList(Of Integer) = BulkRenameLogic.AssignPartNumbersBySelection(fileKeys, decision)

            'Assert
            Assert.AreEqual(3, numbers.Count, "Es muss eine Nummer je Datei geben")
            Assert.AreEqual(1, numbers(0), "Die erste ausgewaehlte Datei ist Teil 1")
            Assert.AreEqual(0, numbers(1), "Die nicht ausgewaehlte Datei gehoert nicht zur Gruppe")
            Assert.AreEqual(2, numbers(2), "Die zweite ausgewaehlte Datei ist Teil 2")
        End Sub

        <UnitTest>
        <TestMethod()>
        Public Sub BulkRename_Dialog_UEberlebt_das_Anzeigen_und_einen_Klick()
            'Der Dialog zeigt eine Liste mit Checkboxen. Beim Anzeigen kommen die
            'Ereignisse der Liste asynchron - dabei ist es einmal zu einem
            'NullReferenceException gekommen (12.11.2026).
            Dim fehler As Exception = Nothing

            Dim t As New Thread(Sub()
                                  Try
                                      'Der Fehler muss abgefangen werden, sonst zeigt
                                      'Windows seinen eigenen Fehlerdialog.
                                      Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)
                                      AddHandler Application.ThreadException, AddressOf OnThreadException

                                      Dim group As New MultiFileGroup With {.GroupKey = "5_23", .Label = "S05E23 - Das Netz der Angst"}
                                      group.Files.Add(New MultiFileFile With {.FileKey = "C:\Serie\a.mkv", .FileName = "a.mkv"})
                                      group.Files.Add(New MultiFileFile With {.FileKey = "C:\Serie\b.mkv", .FileName = "b.mkv"})

                                      Dim group2 As New MultiFileGroup With {.GroupKey = "5_24", .Label = "S05E24 - Das Netz der Angst"}
                                      group2.Files.Add(New MultiFileFile With {.FileKey = "C:\Serie\c.mkv", .FileName = "c.mkv"})
                                      group2.Files.Add(New MultiFileFile With {.FileKey = "C:\Serie\d.mkv", .FileName = "d.mkv"})

                                      Dim group3 As New MultiFileGroup With {.GroupKey = "12_5", .Label = "S12E05 - Die Arche im Weltraum"}
                                      group3.Files.Add(New MultiFileFile With {.FileKey = "C:\Serie\e.mkv", .FileName = "e.mkv"})
                                      group3.Files.Add(New MultiFileFile With {.FileKey = "C:\Serie\f.mkv", .FileName = "f.mkv"})

                                      Dim groups As New List(Of MultiFileGroup)
                                      groups.Add(group)
                                      groups.Add(group2)
                                      groups.Add(group3)

                                      Using dlg As New dlgMultiFileQuestion("Test", groups, isTV:=True)
                                          'Ausserhalb des Bildschirms - im Test soll niemand
                                          'gestoert werden
                                          dlg.TopMost = False
                                          dlg.Location = New Point(-4000, -4000)
                                          dlg.Show()
                                          Application.DoEvents()
                                          Thread.Sleep(200)
                                          Application.DoEvents()

                                          'Die erste Checkbox abhaken - das loest das
                                          'ItemChecked-Ereignis der Dateiliste aus
                                          Dim lstFiles As ListView = DirectCast(dlgType.GetField("_lstFiles", Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance).GetValue(dlg), ListView)
                                          If lstFiles.Items.Count > 0 Then
                                              lstFiles.Items(0).Checked = False
                                              Application.DoEvents()
                                          End If

                                          'Und das Fenster einmal zeichnen - dabei liest
                                          'die Liste ihre Eintraege
                                          Using bmp As New Bitmap(dlg.Width, dlg.Height)
                                              dlg.DrawToBitmap(bmp, New Rectangle(0, 0, dlg.Width, dlg.Height))
                                          End Using
                                      End Using
                                  Catch ex As Exception
                                      fehler = ex
                                  End Try
                              End Sub)

            t.SetApartmentState(ApartmentState.STA)
            t.Start()
            t.Join()

            fehler = If(fehler, _threadException)

            If fehler IsNot Nothing Then
                Assert.Fail("Der Dialog hat einen Fehler ausgeloest: " & fehler.ToString())
            End If
        End Sub

        'Der Fehler aus dem Fenster-Thread (der Dialog laeuft in einem eigenen Thread)
        Private Shared _threadException As Exception

        'Der Dialog-typ fuer den Zugriff auf die Dateiliste
        Private Shared dlgType As Type = GetType(dlgMultiFileQuestion)

        Private Sub OnThreadException(ByVal sender As Object, ByVal e As ThreadExceptionEventArgs)
            _threadException = e.Exception
        End Sub

#End Region 'Entscheidung je Folge-Gruppe

    End Class
End Namespace
