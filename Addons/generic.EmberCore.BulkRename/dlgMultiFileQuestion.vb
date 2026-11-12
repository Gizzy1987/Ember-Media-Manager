Imports EmberAPI
Imports NLog
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' Asks the user how multiple files of the same movie or episode should be treated.
''' </summary>
''' <remarks>Ember can detect that more than one file belongs to the same episode
''' (for example "S01E01A" and "S01E01B") or that a movie exists in several files.
''' The user decides for every group on his own whether the files are parts of one
''' whole ("-part-1", "-part-2", which Jellyfin plays as one episode), different
''' versions ("- 2", which Jellyfin shows as alternate versions) or files that do
''' not belong together at all. Files that are not checked do not belong to the
''' group - they keep their own name.
'''
''' Links steht die Liste der betroffenen Folgen, rechts die Dateien der gerade
''' gewaehlten Folge. So sieht der Nutzer auf einen Blick, welche Dateien zu
''' welcher Folge gehoeren und welche Behandlung schon gewaehlt ist. Mit den
''' Schaltflaechen "Alle Folgen" wird eine Behandlung auf einmal gesetzt.</remarks>
Public Class dlgMultiFileQuestion
    Inherits Form

#Region "Fields"

    Shared logger As Logger = LogManager.GetCurrentClassLogger()

    'Die Gruppen, die zur Auswahl stehen
    Private _groups As IList(Of MultiFileGroup)

    'Die Entscheidung je Gruppe (Schluessel = GroupKey)
    Private _decisions As New Dictionary(Of String, MultiFileDecision)(StringComparer.OrdinalIgnoreCase)

    'Zuordnung der Zeilen in der Dateiliste zu ihren Dateien
    Private _filesByItem As New Dictionary(Of ListViewItem, MultiFileFile)

    'Die gerade in der Folienliste markierte Gruppe
    Private _currentGroupKey As String = String.Empty

    Private _lblQuestion As Label
    Private _lblHint As Label
    Private _lblMode As Label
    Private _lblAll As Label
    Private _lstGroups As ListView
    Private _lstFiles As ListView
    Private _rbParts As RadioButton
    Private _rbVersions As RadioButton
    Private _rbNone As RadioButton
    Private _btnAllParts As Button
    Private _btnAllVersions As Button
    Private _btnAllNone As Button
    Private _btnOk As Button
    Private _btnCancel As Button

    'Verhindert, dass das Setzen der Radiobuttons die Ereignisse erneut ausloest
    Private _updating As Boolean = False

#End Region 'Fields

#Region "Properties"

    ''' <summary>
    ''' The decisions of the user, one entry per group.
    ''' </summary>
    Public ReadOnly Property Decisions() As IList(Of MultiFileDecision)
        Get
            Dim result As New List(Of MultiFileDecision)

            For Each group As MultiFileGroup In _groups
                Dim decision As MultiFileDecision = Nothing
                If Not _decisions.TryGetValue(group.GroupKey, decision) Then Continue For

                'Die Auswahl wird ausdruecklich gespeichert - so ist beim
                'naechsten Lauf klar, welche Dateien zur Gruppe gehoeren.
                Dim item As New MultiFileDecision With {
                    .GroupKey = decision.GroupKey,
                    .Mode = decision.Mode,
                    .AllFiles = False
                }

                For Each file As MultiFileFile In group.Files
                    If BulkRenameLogic.IsFileSelected(decision, file.FileKey) Then item.SelectedFiles.Add(file.FileKey)
                Next

                result.Add(item)
            Next

            Return result
        End Get
    End Property

#End Region 'Properties

#Region "Constructors"

    ''' <param name="strTitle">Title of the dialog (the name of the movie or show)</param>
    ''' <param name="groups">The groups of files the user has to decide about</param>
    ''' <param name="isTV">True for a series, false for a movie</param>
    ''' <param name="knownDecisions">Answers that are already known - they are pre-selected</param>
    Public Sub New(ByVal strTitle As String, ByVal groups As IList(Of MultiFileGroup), Optional ByVal isTV As Boolean = False, Optional ByVal knownDecisions As IDictionary(Of String, MultiFileDecision) = Nothing)
        _groups = If(groups Is Nothing, New List(Of MultiFileGroup), groups)

        Me.Text = String.Concat(strTitle, " - ", Master.eLang.GetString(CUInt(If(isTV, 1760, 1763)), If(isTV, "Multiple files for one episode", "Multiple files for one movie")))
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Font = SystemFonts.MessageBoxFont

        'Der Dialog wird aus dem Renamer-Thread geoeffnet und hat deshalb keinen
        'Besitzer. Ohne diese Angaben landet das Fenster hinter dem
        'Hauptfenster der Anwendung und ist nicht zu sehen.
        Me.TopMost = True
        AddHandler Me.Shown, AddressOf FormShown

        'Ohne Gruppen gibt es nichts zu entscheiden - dann reicht eine kleine
        'Frage ohne Liste.
        Dim hasGroups As Boolean = _groups.Count > 0

        Me.ClientSize = If(hasGroups, New Size(880, 520), New Size(880, 240))

        _lblQuestion = New Label With {
            .Left = 12,
            .Top = 12,
            .Width = 856,
            .Height = 40,
            .Text = Master.eLang.GetString(CUInt(If(isTV, 1774, 1762)), If(isTV, "For some episodes of this series several files were found. Decide for every episode whether the files are parts of one whole, different versions, or files that do not belong together. Files you uncheck do not belong to the group.", "For this movie several files were found. Are they parts of one whole, or different versions?"))
        }
        Me.Controls.Add(_lblQuestion)

        _lblHint = New Label With {
            .Left = 12,
            .Top = 54,
            .Width = 856,
            .Height = 28,
            .Text = Master.eLang.GetString(1777, "Check the files that belong together. The setting on the right applies to every episode you select.")
        }
        _lblHint.Visible = hasGroups
        Me.Controls.Add(_lblHint)

        'Die Folgen mit ihrer bisherigen Behandlung
        _lstGroups = New ListView With {
            .Left = 12,
            .Top = 88,
            .Width = 400,
            .Height = 280,
            .View = View.Details,
            .FullRowSelect = True,
            .HideSelection = False,
            .MultiSelect = False,
            .GridLines = True,
            .HeaderStyle = ColumnHeaderStyle.Nonclickable
        }
        _lstGroups.Columns.Add(Master.eLang.GetString(1778, "Episode"), 200)
        _lstGroups.Columns.Add(Master.eLang.GetString(1779, "Files"), 70)
        _lstGroups.Columns.Add(Master.eLang.GetString(1780, "Handling"), 120)
        AddHandler _lstGroups.SelectedIndexChanged, AddressOf GroupsSelectedIndexChanged
        _lstGroups.Visible = hasGroups
        Me.Controls.Add(_lstGroups)

        'Die Dateien der gerade gewaehlten Folge
        _lstFiles = New ListView With {
            .Left = 424,
            .Top = 88,
            .Width = 444,
            .Height = 280,
            .View = View.Details,
            .CheckBoxes = True,
            .FullRowSelect = True,
            .HideSelection = False,
            .MultiSelect = True,
            .GridLines = True,
            .HeaderStyle = ColumnHeaderStyle.Nonclickable
        }
        _lstFiles.Columns.Add(Master.eLang.GetString(1775, "File"), 260)
        _lstFiles.Columns.Add(Master.eLang.GetString(1776, "Folder"), 174)
        AddHandler _lstFiles.ItemChecked, AddressOf FileItemChecked
        _lstFiles.Visible = hasGroups
        Me.Controls.Add(_lstFiles)

        _lblMode = New Label With {
            .Left = 12,
            .Top = 376,
            .Width = 856,
            .Height = 20,
            .Text = Master.eLang.GetString(1781, "Handling of the selected episode:")
        }
        _lblMode.Visible = hasGroups
        Me.Controls.Add(_lblMode)

        _rbParts = New RadioButton With {
            .Left = 12,
            .Top = 398,
            .Width = 280,
            .Height = 24,
            .Checked = True,
            .Text = Master.eLang.GetString(1784, "Parts of one whole (-part-1)")
        }
        AddHandler _rbParts.CheckedChanged, AddressOf ModeChanged
        Me.Controls.Add(_rbParts)

        _rbVersions = New RadioButton With {
            .Left = 300,
            .Top = 398,
            .Width = 280,
            .Height = 24,
            .Checked = False,
            .Text = Master.eLang.GetString(1785, "Different versions (- 2)")
        }
        AddHandler _rbVersions.CheckedChanged, AddressOf ModeChanged
        Me.Controls.Add(_rbVersions)

        _rbNone = New RadioButton With {
            .Left = 588,
            .Top = 398,
            .Width = 280,
            .Height = 24,
            .Checked = False,
            .Text = Master.eLang.GetString(1786, "Keep separate")
        }
        AddHandler _rbNone.CheckedChanged, AddressOf ModeChanged
        Me.Controls.Add(_rbNone)

        'Sammel-Aktionen: eine Behandlung fuer alle Folgen auf einmal
        _lblAll = New Label With {
            .Left = 12,
            .Top = 436,
            .Width = 110,
            .Height = 24,
            .Text = Master.eLang.GetString(1782, "All episodes:")
        }
        _lblAll.Visible = hasGroups
        Me.Controls.Add(_lblAll)

        _btnAllParts = New Button With {
            .Left = 126,
            .Top = 432,
            .Width = 110,
            .Height = 27,
            .Text = Master.eLang.GetString(1783, "Parts")
        }
        AddHandler _btnAllParts.Click, AddressOf AllParts_Click
        _btnAllParts.Visible = hasGroups
        Me.Controls.Add(_btnAllParts)

        _btnAllVersions = New Button With {
            .Left = 242,
            .Top = 432,
            .Width = 110,
            .Height = 27,
            .Text = Master.eLang.GetString(1787, "Versions")
        }
        AddHandler _btnAllVersions.Click, AddressOf AllVersions_Click
        _btnAllVersions.Visible = hasGroups
        Me.Controls.Add(_btnAllVersions)

        _btnAllNone = New Button With {
            .Left = 358,
            .Top = 432,
            .Width = 110,
            .Height = 27,
            .Text = Master.eLang.GetString(1788, "Separate")
        }
        AddHandler _btnAllNone.Click, AddressOf AllNone_Click
        _btnAllNone.Visible = hasGroups
        Me.Controls.Add(_btnAllNone)

        _btnOk = New Button With {
            .Left = 684,
            .Top = 470,
            .Width = 85,
            .Height = 27,
            .Text = "OK",
            .DialogResult = DialogResult.OK
        }
        Me.Controls.Add(_btnOk)

        _btnCancel = New Button With {
            .Left = 777,
            .Top = 470,
            .Width = 91,
            .Height = 27,
            .Text = Master.eLang.GetString(167, "Cancel"),
            .DialogResult = DialogResult.Cancel
        }
        Me.Controls.Add(_btnCancel)

        Me.AcceptButton = _btnOk
        Me.CancelButton = _btnCancel

        PrepareDecisions(knownDecisions)
        FillGroupList()

        If hasGroups AndAlso _lstGroups.Items.Count > 0 Then _lstGroups.Items(0).Selected = True
    End Sub

#End Region 'Constructors

#Region "Methods"

    ''' <summary>
    ''' Legt die Entscheidungen an - bekannte Antworten uebernehmen, sonst die
    ''' Voreinstellung.
    ''' </summary>
    Private Sub PrepareDecisions(ByVal knownDecisions As IDictionary(Of String, MultiFileDecision))
        For Each group As MultiFileGroup In _groups
            Dim known As MultiFileDecision = Nothing
            If knownDecisions IsNot Nothing Then knownDecisions.TryGetValue(group.GroupKey, known)

            Dim decision As New MultiFileDecision With {.GroupKey = group.GroupKey, .Mode = BulkRenameLogic.ModeParts}

            If known Is Nothing Then
                'Ohne bekannte Antwort sind alle Dateien der Gruppe voreingestellt
                decision.AllFiles = True
            Else
                decision.Mode = known.Mode
                decision.AllFiles = known.AllFiles
                If known.SelectedFiles IsNot Nothing Then
                    For Each file As String In known.SelectedFiles
                        decision.SelectedFiles.Add(file)
                    Next
                End If
            End If

            _decisions(group.GroupKey) = decision
        Next
    End Sub

    ''' <summary>
    ''' Fuellt die Liste der Folgen mit ihrer Behandlung.
    ''' </summary>
    Private Sub FillGroupList()
        _updating = True
        Try
            _lstGroups.Items.Clear()

            For Each group As MultiFileGroup In _groups
                Dim decision As MultiFileDecision = Nothing
                If Not _decisions.TryGetValue(group.GroupKey, decision) Then Continue For

                Dim item As New ListViewItem(group.Label)
                item.SubItems.Add(CountTextOf(decision, group))
                item.SubItems.Add(ModeTextOf(decision.Mode))
                item.Tag = group.GroupKey

                _lstGroups.Items.Add(item)
            Next
        Finally
            _updating = False
        End Try
    End Sub

    ''' <summary>
    ''' Liefert den Text der Behandlung fuer die Anzeige.
    ''' </summary>
    Private Shared Function ModeTextOf(ByVal mode As String) As String
        Select Case mode
            Case BulkRenameLogic.ModeParts
                Return Master.eLang.GetString(1783, "Parts")
            Case BulkRenameLogic.ModeVersions
                Return Master.eLang.GetString(1787, "Versions")
            Case Else
                Return Master.eLang.GetString(1788, "Separate")
        End Select
    End Function

    ''' <summary>
    ''' Liefert den Text fuer die Spalte mit der Dateizahl ("2/3").
    ''' </summary>
    Private Function CountTextOf(ByVal decision As MultiFileDecision, ByVal group As MultiFileGroup) As String
        Dim selected As Integer = 0
        For Each file As MultiFileFile In group.Files
            If BulkRenameLogic.IsFileSelected(decision, file.FileKey) Then selected += 1
        Next

        If selected = group.Files.Count Then Return group.Files.Count.ToString

        Return String.Concat(selected.ToString, "/", group.Files.Count.ToString)
    End Function

    ''' <summary>
    ''' Zeigt die Dateien der markierten Folge.
    ''' </summary>
    Private Sub GroupsSelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
        If _lstGroups.SelectedItems.Count = 0 Then Return

        _currentGroupKey = CStr(_lstGroups.SelectedItems(0).Tag)
        ShowFiles(_currentGroupKey)
    End Sub

    ''' <summary>
    ''' Fuellt die Dateiliste mit den Dateien der Gruppe.
    ''' </summary>
    Private Sub ShowFiles(ByVal groupKey As String)
        _updating = True
        Try
            _lstFiles.Items.Clear()
            _filesByItem.Clear()

            Dim decision As MultiFileDecision = Nothing
            If Not _decisions.TryGetValue(groupKey, decision) Then Return

            For Each file As MultiFileFile In GroupOf(groupKey).Files
                Dim item As New ListViewItem(file.FileName)
                item.SubItems.Add(FolderOf(file.FileKey))
                'Nur die Dateien anhaeken, die der Nutzer zur Gruppe gezaehlt hat
                item.Checked = BulkRenameLogic.IsFileSelected(decision, file.FileKey)
                item.Tag = file.FileKey

                _lstFiles.Items.Add(item)
                _filesByItem.Add(item, file)
            Next

            UpdateRadios(decision)
        Finally
            _updating = False
        End Try
    End Sub

    ''' <summary>
    ''' Liefert die Gruppe mit dem angegebenen Schluessel.
    ''' </summary>
    Private Function GroupOf(ByVal groupKey As String) As MultiFileGroup
        For Each group As MultiFileGroup In _groups
            If String.Equals(group.GroupKey, groupKey, StringComparison.OrdinalIgnoreCase) Then Return group
        Next

        Return Nothing
    End Function

    ''' <summary>
    ''' Uebernimmt die angehakten Dateien in die Entscheidung der Gruppe.
    ''' </summary>
    Private Sub FileItemChecked(ByVal sender As Object, ByVal e As ItemCheckedEventArgs)
        If _updating Then Exit Sub
        If String.IsNullOrEmpty(_currentGroupKey) Then Exit Sub

        Try
            Dim decision As MultiFileDecision = Nothing
            If Not _decisions.TryGetValue(_currentGroupKey, decision) Then Exit Sub
            If decision Is Nothing Then Exit Sub
            If decision.SelectedFiles Is Nothing Then decision.SelectedFiles = New List(Of String)

            'Eine Kopie der Eintraege: waehrend der Auswertung kann sich die
            'Liste aendern (die Ereignisse kommen asynchron aus dem Fenster).
            Dim entries As New List(Of ListViewItem)
            For Each entry As ListViewItem In _lstFiles.Items
                If entry IsNot Nothing Then entries.Add(entry)
            Next

            Dim selected As New List(Of String)
            For Each item As ListViewItem In entries
                If item.Checked AndAlso _filesByItem.ContainsKey(item) Then selected.Add(_filesByItem(item).FileKey)
            Next

            decision.AllFiles = False
            decision.SelectedFiles.Clear()
            For Each file As String In selected
                decision.SelectedFiles.Add(file)
            Next

            UpdateGroupRow(_currentGroupKey)
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    ''' <summary>
    ''' Uebernimmt die gewaehlte Behandlung fuer die markierte Folge.
    ''' </summary>
    Private Sub ModeChanged(ByVal sender As Object, ByVal e As EventArgs)
        If _updating Then Exit Sub
        If String.IsNullOrEmpty(_currentGroupKey) Then Exit Sub

        Dim decision As MultiFileDecision = Nothing
        If Not _decisions.TryGetValue(_currentGroupKey, decision) Then Exit Sub

        decision.Mode = CurrentMode()
        UpdateGroupRow(_currentGroupKey)
    End Sub

    ''' <summary>
    ''' Traegt die gewaehlte Behandlung in die Radiobuttons ein.
    ''' </summary>
    Private Sub UpdateRadios(ByVal decision As MultiFileDecision)
        If decision Is Nothing Then Return

        Dim mode As String = decision.Mode

        _updating = True
        Try
            _rbParts.Checked = (mode = BulkRenameLogic.ModeParts)
            _rbVersions.Checked = (mode = BulkRenameLogic.ModeVersions)
            _rbNone.Checked = (mode = BulkRenameLogic.ModeNone)
        Finally
            _updating = False
        End Try
    End Sub

    ''' <summary>
    ''' Liefert die gerade gewaehlte Behandlung.
    ''' </summary>
    Private Function CurrentMode() As String
        If _rbParts.Checked Then Return BulkRenameLogic.ModeParts
        If _rbVersions.Checked Then Return BulkRenameLogic.ModeVersions

        Return BulkRenameLogic.ModeNone
    End Function

    ''' <summary>
    ''' Aktualisiert die Zeile einer Folge in der Liste.
    ''' </summary>
    Private Sub UpdateGroupRow(ByVal groupKey As String)
        For Each item As ListViewItem In _lstGroups.Items
            If Not String.Equals(CStr(item.Tag), groupKey, StringComparison.OrdinalIgnoreCase) Then Continue For

            Dim decision As MultiFileDecision = Nothing
            If Not _decisions.TryGetValue(groupKey, decision) Then Exit For

            Dim group As MultiFileGroup = GroupOf(groupKey)
            If group Is Nothing Then Exit For

            item.SubItems(1).Text = CountTextOf(decision, group)
            item.SubItems(2).Text = ModeTextOf(decision.Mode)
            Exit For
        Next
    End Sub

    ''' <summary>
    ''' Setzt die Behandlung fuer alle Folgen auf einmal.
    ''' </summary>
    Private Sub SetAllModes(ByVal mode As String)
        For Each group As MultiFileGroup In _groups
            Dim decision As MultiFileDecision = Nothing
            If _decisions.TryGetValue(group.GroupKey, decision) Then decision.Mode = mode
        Next

        FillGroupList()

        If Not String.IsNullOrEmpty(_currentGroupKey) Then
            Dim decision As MultiFileDecision = Nothing
            If _decisions.TryGetValue(_currentGroupKey, decision) Then UpdateRadios(decision)
        End If
    End Sub

    Private Sub AllParts_Click(ByVal sender As Object, ByVal e As EventArgs)
        SetAllModes(BulkRenameLogic.ModeParts)
    End Sub

    Private Sub AllVersions_Click(ByVal sender As Object, ByVal e As EventArgs)
        SetAllModes(BulkRenameLogic.ModeVersions)
    End Sub

    Private Sub AllNone_Click(ByVal sender As Object, ByVal e As EventArgs)
        SetAllModes(BulkRenameLogic.ModeNone)
    End Sub

    ''' <summary>
    ''' Liefert den letzten Ordner des Pfades fuer die Anzeige.
    ''' </summary>
    Private Shared Function FolderOf(ByVal fileKey As String) As String
        If String.IsNullOrEmpty(fileKey) Then Return String.Empty

        Try
            Dim folder As String = Path.GetDirectoryName(fileKey)
            If String.IsNullOrEmpty(folder) Then Return String.Empty

            Return Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar))
        Catch ex As Exception
            Return String.Empty
        End Try
    End Function

    ''' <summary>
    ''' Holt den Dialog in den Vordergrund, sobald er sichtbar wird.
    ''' </summary>
    Private Sub FormShown(ByVal sender As Object, ByVal e As EventArgs)
        Me.Activate()
        Me.BringToFront()
    End Sub

#End Region 'Methods

End Class
