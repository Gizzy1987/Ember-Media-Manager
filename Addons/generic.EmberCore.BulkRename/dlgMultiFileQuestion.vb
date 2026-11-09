Imports EmberAPI
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Asks the user how multiple files of the same movie or episode should be treated.
''' </summary>
''' <remarks>Ember can detect that more than one file belongs to the same episode
''' (for example "S01E01A" and "S01E01B") or that a movie exists in several files.
''' Depending on the answer the files are renamed as parts of one whole ("-part-1",
''' "-part-2", which Jellyfin plays as one episode) or as different versions
''' ("- 2", which Jellyfin shows as alternate versions).</remarks>
Public Class dlgMultiFileQuestion
    Inherits Form

#Region "Fields"

    Private _lblQuestion As Label
    Private _lblAffected As Label
    Private _lstAffected As ListBox
    Private _rbParts As RadioButton
    Private _rbVersions As RadioButton
    Private _btnOk As Button
    Private _btnCancel As Button

#End Region 'Fields

#Region "Properties"

    ''' <summary>
    ''' True if the files should be treated as parts of one whole.
    ''' </summary>
    Public ReadOnly Property IsParts() As Boolean
        Get
            Return _rbParts.Checked
        End Get
    End Property

#End Region 'Properties

#Region "Constructors"

    ''' <param name="strTitle">Title of the dialog (the name of the movie or show)</param>
    ''' <param name="affectedItems">The episodes or movies that are affected</param>
    ''' <param name="isTV">True for a series, false for a movie</param>
    Public Sub New(ByVal strTitle As String, Optional ByVal affectedItems As IList(Of String) = Nothing, Optional ByVal isTV As Boolean = False)
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

        Dim hasDetails As Boolean = affectedItems IsNot Nothing AndAlso affectedItems.Count > 0
        Dim listHeight As Integer = 0
        If hasDetails Then
            listHeight = Math.Min(140, 22 + (affectedItems.Count * 16))
        End If

        Me.ClientSize = New Size(470, 200 + listHeight)

        _lblQuestion = New Label With {
            .Left = 12,
            .Top = 12,
            .Width = 446,
            .Height = 44,
            .Text = Master.eLang.GetString(CUInt(If(isTV, 1715, 1762)), If(isTV, "For some episodes of this series several files were found. Are they parts of one whole, or different versions?", "For this movie several files were found. Are they parts of one whole, or different versions?"))
        }
        Me.Controls.Add(_lblQuestion)

        Dim top As Integer = 60

        If hasDetails Then
            _lblAffected = New Label With {
                .Left = 16,
                .Top = top,
                .Width = 440,
                .Height = 16,
                .Text = Master.eLang.GetString(1761, "Affected items:")
            }
            Me.Controls.Add(_lblAffected)
            top += 20

            _lstAffected = New ListBox With {
                .Left = 16,
                .Top = top,
                .Width = 440,
                .Height = listHeight,
                .IntegralHeight = False
            }
            For Each item As String In affectedItems
                _lstAffected.Items.Add(item)
            Next
            Me.Controls.Add(_lstAffected)
            top += listHeight + 8
        End If

        _rbParts = New RadioButton With {
            .Left = 20,
            .Top = top,
            .Width = 430,
            .Height = 40,
            .Checked = True,
            .Text = Master.eLang.GetString(1716, "Parts of one whole (-part-1, -part-2) - Jellyfin plays them as one episode")
        }
        Me.Controls.Add(_rbParts)
        top += 42

        _rbVersions = New RadioButton With {
            .Left = 20,
            .Top = top,
            .Width = 430,
            .Height = 40,
            .Checked = False,
            .Text = Master.eLang.GetString(1717, "Different versions (- 2, - 3) - Jellyfin offers them as selectable versions")
        }
        Me.Controls.Add(_rbVersions)
        top += 48

        _btnOk = New Button With {
            .Left = 300,
            .Top = top,
            .Width = 75,
            .Height = 27,
            .Text = "OK",
            .DialogResult = DialogResult.OK
        }
        Me.Controls.Add(_btnOk)

        _btnCancel = New Button With {
            .Left = 384,
            .Top = top,
            .Width = 75,
            .Height = 27,
            .Text = Master.eLang.GetString(167, "Cancel"),
            .DialogResult = DialogResult.Cancel
        }
        Me.Controls.Add(_btnCancel)

        Me.AcceptButton = _btnOk
        Me.CancelButton = _btnCancel
    End Sub

#End Region 'Constructors

#Region "Methods"

    ''' <summary>
    ''' Holt den Dialog in den Vordergrund, sobald er sichtbar wird.
    ''' </summary>
    Private Sub FormShown(ByVal sender As Object, ByVal e As EventArgs)
        Me.Activate()
        Me.BringToFront()
    End Sub

    ''' <summary>
    ''' Shows the dialog and returns the answer.
    ''' </summary>
    ''' <param name="strTitle">Title of the dialog</param>
    ''' <param name="affectedItems">The episodes or movies that are affected</param>
    ''' <param name="isTV">True for a series, false for a movie</param>
    ''' <returns>True if the files are parts of one whole, false for different versions</returns>
    Public Shared Function Ask(ByVal strTitle As String, Optional ByVal affectedItems As IList(Of String) = Nothing, Optional ByVal isTV As Boolean = False) As Boolean
        Using dlg As New dlgMultiFileQuestion(strTitle, affectedItems, isTV)
            dlg.ShowDialog()
            Return dlg.IsParts
        End Using
    End Function

#End Region 'Methods

End Class
