Imports EmberAPI
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Shows the progress of a library update and what has been found.
''' </summary>
''' <remarks>The window stays open after the scan so the user can see which
''' movies, series and episodes have been added. It never blocks the scan - the
''' user can close the window while the scan is still running.</remarks>
Public Class dlgScanProgress
    Inherits Form

#Region "Fields"

    Private _lblStatus As Label
    Private _lblSummary As Label
    Private _lstFound As ListView
    Private _btnClose As Button

    'Die Anzahl der gefundenen Inhalte
    Private _foundCount As Integer = 0

    'Waehrend des Scans darf das Fenster nicht den Fokus bekommen -
    'der Nutzer soll in Ember weiterarbeiten koennen. Das Fenster wird darum
    'nach dem Anzeigen nur ueber den Bildschirm gehalten, der Fokus bleibt
    'aber beim Hauptfenster.

#End Region 'Fields

#Region "Constructors"

    Public Sub New()
        Me.Text = Master.eLang.GetString(1789, "Updating library")
        Me.FormBorderStyle = FormBorderStyle.FixedToolWindow
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ShowInTaskbar = False
        Me.TopMost = True
        Me.Font = SystemFonts.MessageBoxFont
        Me.ClientSize = New Size(620, 460)

        _lblStatus = New Label With {
            .Left = 12,
            .Top = 12,
            .Width = 596,
            .Height = 20,
            .Text = Master.eLang.GetString(1790, "Waiting for the scan to start...")
        }
        Me.Controls.Add(_lblStatus)

        _lstFound = New ListView With {
            .Left = 12,
            .Top = 38,
            .Width = 596,
            .Height = 348,
            .View = View.Details,
            .FullRowSelect = True,
            .HideSelection = False,
            .MultiSelect = True,
            .GridLines = True
        }
        _lstFound.Columns.Add(Master.eLang.GetString(1791, "Type"), 90)
        _lstFound.Columns.Add(Master.eLang.GetString(1792, "Content"), 496)
        Me.Controls.Add(_lstFound)

        _lblSummary = New Label With {
            .Left = 12,
            .Top = 392,
            .Width = 596,
            .Height = 20,
            .Text = String.Empty
        }
        Me.Controls.Add(_lblSummary)

        _btnClose = New Button With {
            .Left = 523,
            .Top = 424,
            .Width = 85,
            .Height = 27,
            .Text = Master.eLang.GetString(19, "Close"),
            .DialogResult = DialogResult.Cancel
        }
        Me.Controls.Add(_btnClose)

        Me.AcceptButton = _btnClose
        Me.CancelButton = _btnClose
    End Sub

#End Region 'Constructors

#Region "Methods"

    ''' <summary>
    ''' Zeigt an, was gerade gemacht wird.
    ''' </summary>
    Public Sub SetStatus(ByVal text As String)
        If String.IsNullOrEmpty(text) Then Return

        _lblStatus.Text = text
    End Sub

    ''' <summary>
    ''' Traegt einen gefundenen Inhalt in die Liste ein.
    ''' </summary>
    Public Sub AddFound(ByVal contentType As String, ByVal title As String)
        If String.IsNullOrEmpty(title) Then Return

        _foundCount += 1

        Dim item As New ListViewItem(contentType)
        item.SubItems.Add(title)
        _lstFound.Items.Add(item)

        'Der neueste Eintrag bleibt sichtbar
        If _lstFound.Items.Count > 0 Then
            _lstFound.Items(_lstFound.Items.Count - 1).EnsureVisible()
        End If

        UpdateSummary()
    End Sub

    ''' <summary>
    ''' Zeigt an, dass der Scan fertig ist.
    ''' </summary>
    Public Sub SetFinished()
        _lblStatus.Text = Master.eLang.GetString(1793, "Done.")
        UpdateSummary()
    End Sub

    ''' <summary>
    ''' Aktualisiert die Zusammenfassung unter der Liste.
    ''' </summary>
    Private Sub UpdateSummary()
        If _foundCount = 0 Then
            _lblSummary.Text = Master.eLang.GetString(1794, "No new content found.")
        Else
            _lblSummary.Text = String.Format(Master.eLang.GetString(1795, "{0} new items found."), _foundCount)
        End If
    End Sub

#End Region 'Methods"

End Class
