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

Imports EmberAPI
Imports NLog
Imports System.IO
Imports System.Text.RegularExpressions

Public Class FileFolderRenamer

#Region "Fields"

    Shared logger As Logger = LogManager.GetCurrentClassLogger()

    Public MovieFolders As New List(Of String)
    Public TVShowFolders As New List(Of String)

    Private _episodes As New List(Of FileRename)
    Private _movies As New List(Of FileRename)

    'Antworten auf die Frage nach Teilen oder Versionen fuer die aktuelle Sitzung.
    'Schluessel = Element-ID (Serie oder Film), Wert = "Parts", "Versions" oder
    'leer, wenn der Nutzer die Frage abgebrochen hat.
    Private Shared _multiFileModes As New Dictionary(Of String, String)

#End Region 'Fields

#Region "Constructors"

    Public Sub New()
        Dim mePath As String = String.Concat(Functions.AppPath, "Images", Path.DirectorySeparatorChar, "Flags")

        _movies.Clear()
        Using SQLNewcommand As SQLite.SQLiteCommand = Master.DB.MyVideosDBConn.CreateCommand()
            SQLNewcommand.CommandText = String.Concat("SELECT strPath FROM moviesource;")
            Using SQLReader As SQLite.SQLiteDataReader = SQLNewcommand.ExecuteReader()
                While SQLReader.Read
                    MovieFolders.Add(If(SQLReader("strPath").ToString.EndsWith(Path.DirectorySeparatorChar), SQLReader("strPath").ToString, String.Concat(SQLReader("strPath").ToString, Path.DirectorySeparatorChar)))
                End While
            End Using
        End Using

        'put them in order so when we're checking for basepath the last one used will be the longest one
        'case:
        'Source 1 = C:/Movies/BluRay/FullRips
        'Source 2 = C:/Movies/BluRay
        'stupid to add sources this way, but possible
        MovieFolders.Sort()

        _episodes.Clear()
        Using SQLNewcommand As SQLite.SQLiteCommand = Master.DB.MyVideosDBConn.CreateCommand()
            SQLNewcommand.CommandText = String.Concat("SELECT strPath FROM tvshowsource;")
            Using SQLReader As SQLite.SQLiteDataReader = SQLNewcommand.ExecuteReader()
                While SQLReader.Read
                    TVShowFolders.Add(If(SQLReader("strPath").ToString.EndsWith(Path.DirectorySeparatorChar), SQLReader("strPath").ToString, String.Concat(SQLReader("strPath").ToString, Path.DirectorySeparatorChar)))
                End While
            End Using
        End Using

        'put them in order so when we're checking for basepath the last one used will be the longest one
        'case:
        'Source 1 = C:/Movies/BluRay/FullRips
        'Source 2 = C:/Movies/BluRay
        'stupid to add sources this way, but possible
        MovieFolders.Sort()
        TVShowFolders.Sort()
    End Sub

#End Region 'Constructors

#Region "Delegates"

    Public Delegate Function ShowProgress(ByVal movie As String, ByVal iProgress As Integer) As Boolean

#End Region 'Delegates

#Region "Methods"

    Public Sub Add_Movie(ByVal _movie As FileRename)
        _movies.Add(_movie)
    End Sub

    Public Sub Add_TVEpisode(ByVal _episode As FileRename)
        _episodes.Add(_episode)
    End Sub

    Private Shared Function ApplyPattern(ByVal pattern As String, ByVal flag As String, ByVal v As String) As String
        v = v.Replace("\", "-")
        v = v.Replace("/", "-")
        pattern = pattern.Replace(String.Concat("$", flag), v)
        If Not String.IsNullOrEmpty(v) Then
            pattern = pattern.Replace(String.Concat("$-", flag), v)
            pattern = pattern.Replace(String.Concat("$+", flag), v)
            pattern = pattern.Replace(String.Concat("$^", flag), v)
        Else
            Dim pos = -1
            Dim size = 3
            Dim nextC = pattern.IndexOf(String.Concat("$+", flag))
            If nextC >= 0 Then
                If nextC + 3 < pattern.Length Then size += 1
                pos = nextC
            End If
            Dim prevC = pattern.IndexOf(String.Concat("$-", flag))
            If prevC >= 0 Then
                If prevC + 3 < pattern.Length Then size += 1
                If prevC > 0 Then
                    prevC -= 1
                End If
                pos = prevC
            End If
            Dim bothC = pattern.IndexOf(String.Concat("$^", flag))
            If bothC >= 0 Then
                If bothC + 3 < pattern.Length Then size += 1
                If bothC > 0 Then
                    size += 1
                    bothC -= 1
                End If
                pos = bothC
            End If

            If Not pos = -1 Then pattern = pattern.Remove(pos, size)
        End If
        Return pattern
    End Function

    ''' <summary>
    ''' Normalizes an audio codec name as delivered by the file scanner
    ''' (ffprobe/MediaInfo) into a conventional spelling for file and folder names.
    ''' </summary>
    ''' <remarks>The scanners return lower case names like "ac3", "eac3" or "truehd",
    ''' which look wrong in a file name. Unknown codecs are returned unchanged.</remarks>
    Public Shared Function NormalizeAudioCodec(ByVal codec As String) As String
        If String.IsNullOrEmpty(codec) Then Return String.Empty

        Select Case codec.Trim.ToLowerInvariant()
            Case "ac3", "ac-3", "dd", "dolby digital"
                Return "AC3"
            Case "eac3", "e-ac-3", "ddplus", "dd+", "dolby digital plus", "eac3-joc"
                Return "E-AC3"
            Case "dts"
                Return "DTS"
            Case "dts-hd", "dtshd", "dts-hd ma", "dts-hd hra"
                Return "DTS-HD"
            Case "truehd", "true hd"
                Return "TrueHD"
            Case "truehd+atmos", "atmos"
                Return "TrueHD+Atmos"
            Case "aac"
                Return "AAC"
            Case "flac"
                Return "FLAC"
            Case "opus"
                Return "Opus"
            Case "vorbis"
                Return "Vorbis"
            Case "mp3", "mpeg audio"
                Return "MP3"
            Case "pcm", "lpcm", "pcm_s16le", "pcm_s24le", "pcm_s32le"
                Return "PCM"
            Case Else
                Return codec.Trim
        End Select
    End Function

    Public Sub DoRename_Movies(Optional ByVal sfunction As ShowProgress = Nothing)
        Dim _movieDB As Database.DBElement = Nothing
        Dim iProg As Integer = 0
        Try
            For Each f As FileRename In _movies.Where(Function(s) s.DoRename AndAlso Not s.FileExist AndAlso Not s.IsLock)
                iProg += 1
                If Not f.ID = -1 Then
                    _movieDB = Master.DB.Load_Movie(f.ID)
                    DoRenameSingle_Movie(f, _movieDB, True, False, True, sfunction, iProg)
                End If
            Next
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Public Sub DoRename_TVEpisodes(Optional ByVal sfunction As ShowProgress = Nothing)
        Dim _tvDB As Database.DBElement = Nothing
        Dim iProg As Integer = 0
        Try
            Using SQLtransaction As SQLite.SQLiteTransaction = Master.DB.MyVideosDBConn.BeginTransaction()
                For Each f As FileRename In _episodes.Where(Function(s) s.DoRename AndAlso Not s.FileExist AndAlso Not s.IsLock)
                    iProg += 1
                    If Not f.ID = -1 Then
                        _tvDB = Master.DB.Load_TVEpisode(f.ID, True)
                        DoRenameSingle_TVEpisode(f, _tvDB, True, False, True, sfunction, iProg)
                    End If
                Next
                SQLtransaction.Commit()
            End Using
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Private Shared Sub DoRenameSingle_Movie(ByVal _frename As FileRename, ByRef _movie As Database.DBElement, ByVal BatchMode As Boolean, ByVal ShowError As Boolean, ByVal toDB As Boolean, Optional ByVal sfunction As ShowProgress = Nothing, Optional ByVal iProg As Integer = 0)
        logger.Trace(String.Format("[{0}] [{1}] [Start]", Reflection.MethodBase.GetCurrentMethod.ReflectedType, Reflection.MethodBase.GetCurrentMethod.Name))
        Try
            If Not _movie.IsLock AndAlso Not _frename.FileExist Then
                Dim getError As Boolean = False
                Dim merged As Boolean = False
                Dim srcDir As String = Path.Combine(_frename.BasePath, _frename.Path)
                Dim destDir As String = Path.Combine(_frename.BasePath, _frename.NewPath)
                Dim srcFilenamePath As String = Path.Combine(_frename.BasePath, _frename.Path, _frename.OldFileName)
                Dim dstFilenamePath As String = Path.Combine(_frename.BasePath, _frename.NewPath, _frename.NewFileName)

                'Rename Directory
                If Not srcDir = destDir Then
                    Try
                        If sfunction IsNot Nothing Then
                            If Not sfunction(_frename.NewPath, iProg) Then Return
                        End If

                        If _frename.DirExist Then
                            'Der Zielordner existiert bereits und ist nicht leer. Statt den Vorgang
                            'abzubrechen wird der Inhalt des Quellordners dorthin verschoben.
                            MergeSingleMovie(srcDir, destDir, _frename.OldFileName, _frename.NewFileName, _frename.NewSidecarName, _movie)
                            merged = True
                        ElseIf Not _movie.IsSingle Then
                            Directory.CreateDirectory(destDir)
                        Else
                            If srcDir.ToLower = destDir.ToLower Then
                                Directory.Move(srcDir, String.Concat(destDir, ".$emm"))
                                Directory.Move(String.Concat(destDir, ".$emm"), destDir)
                            Else
                                If Not Directory.Exists(Directory.GetParent(destDir).FullName) Then Directory.CreateDirectory(Directory.GetParent(destDir).FullName)
                                Directory.Move(srcDir, destDir)
                            End If
                        End If
                    Catch ex As Exception
                        If ShowError Then
                            MessageBox.Show(String.Format(Master.eLang.GetString(144, "An error occured while attempting to rename the directory:{0}{0}{1}{0}{0}Please ensure that you are not accessing this directory or any of its files from another program (including browsing via Windows Explorer)."), Environment.NewLine, ex.Message), Master.eLang.GetString(165, "Unable to Rename Directory"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Else
                            logger.Error("Dir: <{0}> - <{1}>", srcDir, destDir)
                        End If
                        getError = True
                    End Try

                End If

                'Rename Files
                If Not getError AndAlso Not merged AndAlso Not _frename.IsVideoTS AndAlso Not _frename.IsBDMV Then
                    If (Not _frename.NewFileName = _frename.OldFileName) OrElse
                       (Not String.IsNullOrEmpty(_frename.NewSidecarName) AndAlso Not _frename.NewSidecarName = _frename.OldFileName) OrElse
                       (_frename.Path = String.Empty AndAlso Not _frename.NewPath = String.Empty) OrElse Not _movie.IsSingle Then
                        Dim di As DirectoryInfo

                        If _frename.IsSingle Then
                            di = New DirectoryInfo(destDir)
                        Else
                            di = New DirectoryInfo(srcDir)
                        End If

                        Dim lFi As New List(Of FileInfo)
                        Try
                            lFi.AddRange(di.GetFiles())
                            For Each subtitle In _movie.Subtitles.Where(Function(f) f.PathSpecified)
                                Dim nPath = subtitle.Path.Replace(srcDir, destDir)
                                If lFi.Where(Function(f) f.FullName = nPath).Count = 0 Then
                                    lFi.Add(New FileInfo(nPath))
                                End If
                            Next
                        Catch ex As Exception
                            logger.Error(ex, New StackFrame().GetMethod().Name)
                        End Try
                        If lFi.Count > 0 Then
                            For Each lFile As FileInfo In lFi.OrderBy(Function(s) s.Name)
                                Dim srcFile As String = lFile.FullName
                                Dim newBase As String = GetRenameBase(_frename, lFile)
                                Dim dstFile As String = Path.Combine(Directory.GetParent(srcFile).FullName, lFile.Name.Replace(_frename.OldFileName.Trim, newBase.Trim))
                                If Not srcFile = dstFile Then
                                    Try
                                        If sfunction IsNot Nothing Then
                                            If Not sfunction(_frename.NewFileName, iProg) Then Return
                                        End If

                                        If srcFile.ToLower = dstFile.ToLower Then
                                            File.Move(srcFile, String.Concat(dstFile, ".$emm$"))
                                            File.Move(String.Concat(dstFile, ".$emm$"), dstFile)
                                        Else
                                            If lFile.Name.StartsWith(_frename.OldFileName, StringComparison.OrdinalIgnoreCase) Then
                                                File.Move(srcFile, dstFile)
                                            End If
                                        End If

                                    Catch ex As Exception
                                        If ShowError Then
                                            MessageBox.Show(String.Format(Master.eLang.GetString(166, "An error occured while attempting to rename a file:{0}{0}{1}{0}{0}Please ensure that you are not accessing this file from another program."), Environment.NewLine, ex.Message), Master.eLang.GetString(171, "Unable to Rename File"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                                        Else
                                            logger.Error("File <{0}> - <{1}>", srcFile, dstFile)
                                        End If
                                        getError = True
                                    End Try
                                End If
                            Next
                        End If
                    End If
                End If

                If Not getError Then
                    UpdatePaths_Movie(_movie, srcDir, destDir, _frename.OldFileName, _frename.NewFileName, _frename.NewSidecarName)

                    If toDB Then
                        Master.DB.Save_Movie(_movie, BatchMode, False, False, True, False)
                    End If

                    'remove empty folders
                    Dim strBasePath As String = _frename.BasePath
                    Dim currPath As DirectoryInfo = New DirectoryInfo(_frename.OldFullPath)
                    While Not currPath.FullName.ToLower = strBasePath.ToLower
                        If currPath.Exists AndAlso currPath.GetFiles.Count = 0 AndAlso currPath.GetDirectories.Count = 0 Then currPath.Delete()
                        currPath = currPath.Parent
                    End While
                End If
            Else
                Dim strErrorMessage As String = String.Format("{0}{2}{2}{1}{2}{2}{3}: {4}{2}{5}: {6}{2}{7}: {8}",
                                                              Master.eLang.GetString(171, "Unable to Rename File"),
                                                              _frename.OldFullFileName,
                                                              Environment.NewLine,
                                                              Master.eLang.GetString(43, "Locked"),
                                                              _frename.IsLock.ToString,
                                                              Master.eLang.GetString(1084, "File already exists"),
                                                              _frename.FileExist.ToString,
                                                              Master.eLang.GetString(1085, "Directory already exists or is not empty"),
                                                              _frename.DirExist.ToString)
                If ShowError Then
                    'MessageBox.Show(Master.eLang.GetString(171, "Unable to Rename File"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    MessageBox.Show(strErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
                logger.Error(String.Format("[{0}] [{1}] [{2}]", Reflection.MethodBase.GetCurrentMethod.ReflectedType, Reflection.MethodBase.GetCurrentMethod.Name, strErrorMessage.Replace(Environment.NewLine, "; ").Replace("; ;", ";")))
            End If
            logger.Trace(String.Format("[{0}] [{1}] [Done]", Reflection.MethodBase.GetCurrentMethod.ReflectedType, Reflection.MethodBase.GetCurrentMethod.Name))
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            logger.Trace(String.Format("[{0}] [{1}] [Abort]", Reflection.MethodBase.GetCurrentMethod.ReflectedType, Reflection.MethodBase.GetCurrentMethod.Name))
        End Try
    End Sub

    ''' <summary>
    ''' Removes the version specific tokens (resolution and audio codec) from a pattern.
    ''' </summary>
    ''' <remarks>Sidecar files like poster and fanart belong to the movie and not to a
    ''' specific video file, so they must not carry the resolution and codec suffix.</remarks>
    Private Shared Function StripTechnicalTokens(ByVal pattern As String) As String
        If String.IsNullOrEmpty(pattern) Then Return pattern

        Dim result As String = pattern
        Dim searchFrom As Integer = 0

        'Optionale Bloecke, die nur technische Tokens enthalten, komplett entfernen -
        'sonst blieben die leeren Klammern im Dateinamen stehen.
        While True
            Dim startIndex As Integer = result.IndexOf("{", searchFrom)
            If startIndex = -1 Then Exit While
            Dim endIndex As Integer = result.IndexOf("}", startIndex)
            If endIndex = -1 Then Exit While

            Dim inner As String = result.Substring(startIndex + 1, endIndex - startIndex - 1)
            Dim strippedInner As String = RemoveTechnicalTokens(inner)

            If strippedInner.Trim = String.Empty Then
                Dim removeFrom As Integer = startIndex
                If removeFrom > 0 AndAlso result.Substring(removeFrom - 1, 1) = " " Then removeFrom -= 1
                result = result.Remove(removeFrom, endIndex - removeFrom + 1)
                searchFrom = removeFrom
            Else
                result = result.Remove(startIndex, endIndex - startIndex + 1).Insert(startIndex, strippedInner)
                searchFrom = startIndex + strippedInner.Length
            End If
        End While

        Return RemoveTechnicalTokens(result)
    End Function

    ''' <summary>
    ''' Removes the resolution and audio codec tokens including a preceding space.
    ''' </summary>
    Private Shared Function RemoveTechnicalTokens(ByVal text As String) As String
        If String.IsNullOrEmpty(text) Then Return text
        Dim result As String = text
        For Each token In New String() {"$-R", "$+R", "$^R", "$R", "$-J", "$+J", "$^J", "$J"}
            Dim idx As Integer = result.IndexOf(token)
            While idx > -1
                Dim removeLen As Integer = token.Length
                If idx > 0 AndAlso result.Substring(idx - 1, 1) = " " Then removeLen += 1
                If idx + removeLen > result.Length Then removeLen = result.Length - idx
                result = result.Remove(idx, removeLen)
                idx = result.IndexOf(token)
            End While
        Next
        Return result
    End Function

    ''' <summary>
    ''' Returns the base name under which a sidecar file is renamed.
    ''' </summary>
    ''' <remarks>The movie file itself and its NFO carry the full file pattern including
    ''' resolution and audio codec. All other files (poster, fanart, ...) use the same
    ''' pattern without these version specific tokens.</remarks>
    Private Shared Function GetRenameBase(ByVal _frename As FileRename, ByVal lFile As FileInfo) As String
        If String.IsNullOrEmpty(_frename.NewSidecarName) Then Return _frename.NewFileName
        If Path.GetFileNameWithoutExtension(lFile.Name).Equals(_frename.OldFileName.Trim, StringComparison.OrdinalIgnoreCase) Then
            Return _frename.NewFileName
        End If
        Return _frename.NewSidecarName
    End Function

    ''' <summary>
    ''' Moves the content of the movie folder into the already existing target folder
    ''' and gives the movie files their new name on the way.
    ''' </summary>
    ''' <remarks>Used when the target folder exists already but the target file does not.
    ''' Instead of aborting the whole rename the movie is moved into the target folder.
    ''' Files with the same name are replaced by the current ones, because the source
    ''' folder holds the live version of the movie. Afterwards the emptied source folder
    ''' is removed.</remarks>
    Private Shared Sub MergeSingleMovie(ByVal srcDir As String, ByVal destDir As String,
                                        ByVal oldFileName As String, ByVal newFileName As String,
                                        ByVal sidecarName As String,
                                        ByVal movie As Database.DBElement)
        Dim diSrc As New DirectoryInfo(srcDir)

        ' 1. Files of the source folder get their new name
        For Each lFile As FileInfo In diSrc.GetFiles()
            If lFile.Name.StartsWith(oldFileName, StringComparison.OrdinalIgnoreCase) Then
                Dim baseName As String = If(Path.GetFileNameWithoutExtension(lFile.Name).Equals(oldFileName.Trim, StringComparison.OrdinalIgnoreCase),
                                            newFileName.Trim,
                                            If(String.IsNullOrEmpty(sidecarName), newFileName.Trim, sidecarName.Trim))
                Dim dstFile As String = Path.Combine(srcDir, lFile.Name.Replace(oldFileName.Trim, baseName))
                If Not lFile.FullName.ToLower = dstFile.ToLower Then
                    If File.Exists(dstFile) Then File.Delete(dstFile)
                    File.Move(lFile.FullName, dstFile)
                End If
            End If
        Next

        ' 2. Subtitles as well
        If movie IsNot Nothing AndAlso movie.SubtitlesSpecified Then
            For Each subtitle In movie.Subtitles.Where(Function(f) f.PathSpecified)
                If subtitle.Path.StartsWith(srcDir, StringComparison.OrdinalIgnoreCase) Then
                    Dim dstSub As String = Path.Combine(srcDir, Path.GetFileName(subtitle.Path).Replace(oldFileName.Trim, newFileName.Trim))
                    If Not subtitle.Path.ToLower = dstSub.ToLower AndAlso File.Exists(subtitle.Path) Then
                        If File.Exists(dstSub) Then File.Delete(dstSub)
                        File.Move(subtitle.Path, dstSub)
                    End If
                End If
            Next
        End If

        ' 3. Move the content into the target folder, existing files are replaced
        For Each lFile As FileInfo In diSrc.GetFiles()
            Dim target As String = Path.Combine(destDir, lFile.Name)
            If File.Exists(target) Then File.Delete(target)
            File.Move(lFile.FullName, target)
        Next
        For Each lDir As DirectoryInfo In diSrc.GetDirectories()
            Dim target As String = Path.Combine(destDir, lDir.Name)
            If Directory.Exists(target) Then
                MergeFolder(lDir.FullName, target)
            Else
                Directory.Move(lDir.FullName, target)
            End If
        Next

        ' 4. Remove the emptied source folder
        Directory.Delete(srcDir, True)
    End Sub

    ''' <summary>
    ''' Merges the content of <paramref name="srcDir"/> into the existing folder
    ''' <paramref name="destDir"/>, replacing files with the same name.
    ''' </summary>
    Private Shared Sub MergeFolder(ByVal srcDir As String, ByVal destDir As String)
        For Each lFile As FileInfo In New DirectoryInfo(srcDir).GetFiles()
            Dim target As String = Path.Combine(destDir, lFile.Name)
            If File.Exists(target) Then File.Delete(target)
            File.Move(lFile.FullName, target)
        Next
        For Each lDir As DirectoryInfo In New DirectoryInfo(srcDir).GetDirectories()
            Dim target As String = Path.Combine(destDir, lDir.Name)
            If Directory.Exists(target) Then
                MergeFolder(lDir.FullName, target)
            Else
                Directory.Move(lDir.FullName, target)
            End If
        Next
        Directory.Delete(srcDir, True)
    End Sub

    Private Shared Sub DoRenameSingle_TVEpisode(ByVal _frename As FileRename, ByRef _tv As Database.DBElement, ByVal BatchMode As Boolean, ByVal ShowError As Boolean, ByVal toDB As Boolean, Optional ByVal sfunction As ShowProgress = Nothing, Optional ByVal iProg As Integer = 0)
        Try
            Dim srcFilenamePath As String = Path.Combine(_frename.BasePath, _frename.Path, _frename.OldFileName)
            'Ist die Quelldatei ueberhaupt noch da? Ein verwaister Datenbankeintrag
            '(die Datei wurde zwischenzeitlich umbenannt oder geloescht) ist kein
            'Fehler - es gibt nichts umzubenennen.
            Dim srcExists As Boolean = File.Exists(srcFilenamePath)

            If Not _tv.IsLock AndAlso (Not _frename.FileExist OrElse Not srcExists) Then
                Dim getError As Boolean = False
                Dim srcDir As String = Path.Combine(_frename.BasePath, _frename.Path)
                Dim destDir As String = Path.Combine(_frename.BasePath, _frename.NewPath)
                Dim dstFilenamePath As String = Path.Combine(_frename.BasePath, _frename.NewPath, _frename.NewFileName)

                'Rename/Create Directory
                If Not srcDir = destDir Then
                    Try
                        If sfunction IsNot Nothing Then
                            If Not sfunction(_frename.NewPath, iProg) Then Return
                        End If

                        If srcDir.ToLower = destDir.ToLower Then
                            Directory.Move(srcDir, String.Concat(destDir, ".$emm"))
                            Directory.Move(String.Concat(destDir, ".$emm"), destDir)
                        Else
                            If Not Directory.Exists(destDir) Then Directory.CreateDirectory(destDir)
                        End If

                        'copy actor thumbs folder
                        If Directory.Exists(Path.Combine(srcDir, ".actors")) Then
                            FileUtils.Common.DirectoryCopy(Path.Combine(srcDir, ".actors"), Path.Combine(destDir, ".actors"), True, True)
                        End If
                    Catch ex As Exception
                        If ShowError Then
                            MessageBox.Show(String.Format(Master.eLang.GetString(144, "An error occured while attempting to rename the directory:{0}{0}{1}{0}{0}Please ensure that you are not accessing this directory or any of its files from another program (including browsing via Windows Explorer)."), Environment.NewLine, ex.Message), Master.eLang.GetString(165, "Unable to Rename Directory"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Else
                            logger.Error("Dir: <{0}> - <{1}>", srcDir, destDir)
                        End If
                        getError = True
                    End Try
                End If

                'Rename Files
                If Not _frename.IsVideoTS AndAlso Not _frename.IsBDMV Then
                    If (Not srcFilenamePath = dstFilenamePath) OrElse (_frename.Path = String.Empty AndAlso Not _frename.NewPath = String.Empty) Then 'OrElse Not _tv.IsSingle Then
                        Dim di As DirectoryInfo

                        di = New DirectoryInfo(srcDir)

                        Dim lFi As New List(Of FileInfo)

                        Try
                            lFi.AddRange(di.GetFiles(String.Concat(_frename.OldFileName, "*")))
                            For Each subtitle In _tv.Subtitles.Where(Function(f) f.PathSpecified)
                                If lFi.Where(Function(f) f.FullName = subtitle.Path).Count = 0 Then
                                    lFi.Add(New FileInfo(subtitle.Path))
                                End If
                            Next
                        Catch ex As Exception
                            logger.Error(ex, New StackFrame().GetMethod().Name)
                        End Try
                        If lFi.Count > 0 Then
                            For Each lFile As FileInfo In lFi.OrderBy(Function(s) s.Name)
                                Dim srcFile As String = lFile.FullName
                                Dim dstFile As String = Path.Combine(lFile.Directory.FullName.Replace(srcDir, destDir), lFile.Name.Replace(_frename.OldFileName.Trim, _frename.NewFileName.Trim))
                                If Not srcFile = dstFile Then
                                    Try
                                        If sfunction IsNot Nothing Then
                                            If Not sfunction(_frename.NewFileName, iProg) Then Return
                                        End If

                                        If srcFile.ToLower = dstFile.ToLower Then
                                            File.Move(srcFile, String.Concat(dstFile, ".$emm$"))
                                            File.Move(String.Concat(dstFile, ".$emm$"), dstFile)
                                        Else
                                            If lFile.Name.StartsWith(_frename.OldFileName, StringComparison.OrdinalIgnoreCase) Then
                                                If Not Directory.Exists(Directory.GetParent(dstFile).FullName) Then
                                                    Directory.CreateDirectory(Directory.GetParent(dstFile).FullName)
                                                End If
                                                File.Move(srcFile, dstFile)
                                            End If
                                        End If

                                    Catch ex As Exception
                                        If ShowError Then
                                            MessageBox.Show(String.Format(Master.eLang.GetString(166, "An error occured while attempting to rename a file:{0}{0}{1}{0}{0}Please ensure that you are not accessing this file from another program."), Environment.NewLine, ex.Message), Master.eLang.GetString(171, "Unable to Rename File"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                                        Else
                                            logger.Error("File <{0}> - <{1}>", srcFile, dstFile)
                                        End If
                                        getError = True
                                    End Try
                                End If
                            Next
                        End If
                    End If
                End If

                If Not getError Then
                    UpdatePaths_TVEpisode(_tv, srcDir, destDir, _frename.OldFileName, _frename.NewFileName)

                    If toDB Then
                        Master.DB.Save_TVEpisode(_tv, BatchMode, False, False, False, True)
                    End If

                    'remove empty folders
                    Dim strBasePath As String = _frename.ShowFullPath
                    Dim currPath As DirectoryInfo = New DirectoryInfo(_frename.OldFullPath)
                    While Not currPath.FullName.ToLower = strBasePath.ToLower
                        If currPath.Exists AndAlso
                            ((currPath.GetFiles.Count = 0 AndAlso currPath.GetDirectories.Count = 0) OrElse
                            (currPath.GetFiles.Count = 0 AndAlso currPath.GetDirectories.Count = 1 AndAlso
                            currPath.GetDirectories.First.Name.ToLower = ".actors")) Then currPath.Delete(True)
                        currPath = currPath.Parent
                    End While
                End If
            ElseIf srcExists Then
                Dim strErrorMessage As String = String.Format("{0}{2}{2}{1}{2}{2}{3}: {4}{2}{5}: {6}",
                                                              Master.eLang.GetString(171, "Unable to Rename File"),
                                                              _frename.OldFullFileName,
                                                              Environment.NewLine,
                                                              Master.eLang.GetString(43, "Locked"),
                                                              _frename.IsLock.ToString,
                                                              Master.eLang.GetString(1084, "File already exists"),
                                                              _frename.FileExist.ToString)
                If ShowError Then
                    'MessageBox.Show(Master.eLang.GetString(171, "Unable to Rename File"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    MessageBox.Show(strErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
                logger.Error(strErrorMessage.Replace(Environment.NewLine, "; ").Replace("; ;", ";"))
            Else
                'Die Quelldatei existiert nicht mehr - der Eintrag ist verwais und
                'wird von der Datenbankbereinigung entfernt. Kein Fehler.
                logger.Info("Source file no longer exists - nothing to rename: {0}", _frename.OldFullFileName)
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Private Shared Sub DoRenameSingle_TVShow(ByVal _frename As FileRename, ByRef _tv As Database.DBElement, ByVal BatchMode As Boolean, ByVal ShowError As Boolean, ByVal toDB As Boolean)
        Try
            If Not _tv.IsLock AndAlso Not _frename.DirExist Then
                Dim getError As Boolean = False
                Dim srcDir As String = _frename.OldFullPath
                Dim destDir As String = Path.Combine(_frename.BasePath, _frename.NewPath)

                'Rename Directory
                If Not srcDir = destDir Then
                    Try
                        If srcDir.ToLower = destDir.ToLower Then
                            Directory.Move(srcDir, String.Concat(destDir, ".$emm"))
                            Directory.Move(String.Concat(destDir, ".$emm"), destDir)
                        Else
                            If Not Directory.Exists(Directory.GetParent(destDir).FullName) Then Directory.CreateDirectory(Directory.GetParent(destDir).FullName)
                            Directory.Move(srcDir, destDir)
                        End If
                    Catch ex As Exception
                        If ShowError Then
                            MessageBox.Show(String.Format(Master.eLang.GetString(144, "An error occured while attempting to rename the directory:{0}{0}{1}{0}{0}Please ensure that you are not accessing this directory or any of its files from another program (including browsing via Windows Explorer)."), Environment.NewLine, ex.Message), Master.eLang.GetString(165, "Unable to Rename Directory"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Else
                            logger.Error("Dir: <{0}> - <{1}>", srcDir, destDir)
                        End If
                        getError = True
                    End Try
                End If

                If Not getError Then
                    'update tvshow paths
                    UpdatePaths_TVShow(_tv, srcDir, destDir)

                    'update season paths
                    For Each aSeason As Database.DBElement In _tv.Seasons
                        UpdatePaths_TVShow(aSeason, srcDir, destDir)
                    Next

                    'update episode paths
                    For Each aEpisode As Database.DBElement In _tv.Episodes
                        UpdatePaths_TVShow(aEpisode, srcDir, destDir)
                    Next

                    If toDB Then
                        Master.DB.Save_TVShow(_tv, BatchMode, False, False, True)
                    End If

                    'remove empty folders
                    Dim strBasePath As String = _frename.BasePath
                    Dim currPath As DirectoryInfo = New DirectoryInfo(_frename.OldFullPath)
                    While Not currPath.FullName.ToLower = strBasePath.ToLower
                        If currPath.Exists AndAlso currPath.GetFiles.Count = 0 AndAlso currPath.GetDirectories.Count = 0 Then currPath.Delete()
                        currPath = currPath.Parent
                    End While
                End If
            Else
                If ShowError Then
                    MessageBox.Show(Master.eLang.GetString(165, "Unable to Rename Folder"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Public Function GetCountAll_Movies() As Integer
        Return _movies.Count
    End Function

    Public Function GetCountAll_TVEpisodes() As Integer
        Return _episodes.Count
    End Function

    Public Function GetCountLocked_Movies() As Integer
        Dim c As Integer = c
        For Each f As FileRename In _movies
            If f.IsLock Then c += 1
        Next
        Return c
    End Function

    Public Function GetCountLocked_TVEpisodes() As Integer
        Dim c As Integer = c
        For Each f As FileRename In _episodes
            If f.IsLock Then c += 1
        Next
        Return c
    End Function

    Public Function GetCountMax_Movies() As Integer
        Dim Renamed = From rList In _movies Where rList.DoRename = True
        Return Renamed.Count
    End Function

    Public Function GetCountMax_TVEpisodes() As Integer
        Dim Renamed = From rList In _episodes Where rList.DoRename = True
        Return Renamed.Count
    End Function

    Public Shared Function GetInfo_Movie(ByVal _DBElement As Database.DBElement) As FileRename
        Dim MovieFile As New FileRename

        'ID
        MovieFile.ID = _DBElement.ID

        'Countries
        If _DBElement.Movie.CountriesSpecified Then
            MovieFile.Countries = _DBElement.Movie.Countries
        End If

        'Director
        If _DBElement.Movie.DirectorsSpecified Then
            MovieFile.Directors = _DBElement.Movie.Directors
        End If

        'Edition
        If _DBElement.Movie.EditionSpecified Then
            MovieFile.Edition = _DBElement.Movie.Edition
        End If

        'Genres
        If _DBElement.Movie.GenresSpecified Then
            MovieFile.Genres = _DBElement.Movie.Genres
        End If

        'IMDB
        If _DBElement.Movie.UniqueIDs.IMDbIdSpecified Then
            MovieFile.IMDB = _DBElement.Movie.UniqueIDs.IMDbId
        End If

        'IsLock
        MovieFile.IsLock = _DBElement.IsLock

        'IsSingle
        MovieFile.IsSingle = _DBElement.IsSingle

        'ListTitle
        If _DBElement.Movie.TitleSpecified Then
            MovieFile.ListTitle = StringUtils.SortTokens(_DBElement.Movie.Title)
        End If

        'MovieSets
        If _DBElement.Movie.SetsSpecified Then
            MovieFile.Collection = _DBElement.Movie.Sets.Items(0).Title
            MovieFile.CollectionListTitle = StringUtils.SortTokens(_DBElement.Movie.Sets.Items(0).Title)
        End If

        'MPAA
        If _DBElement.Movie.MPAASpecified Then
            MovieFile.MPAA = SelectMPAA(_DBElement.Movie.MPAA)
        End If

        'OriginalTitle
        If _DBElement.Movie.OriginalTitleSpecified Then
            MovieFile.OriginalTitle = _DBElement.Movie.OriginalTitle
        End If

        'Rating
        If _DBElement.Movie.RatingSpecified Then
            MovieFile.Rating = _DBElement.Movie.Rating
        End If

        'SortTitle
        If _DBElement.Movie.SortTitleSpecified Then
            MovieFile.SortTitle = _DBElement.Movie.SortTitle
        End If

        'Title
        If _DBElement.Movie.TitleSpecified Then
            MovieFile.Title = _DBElement.Movie.Title
        End If

        'VideoSource
        If _DBElement.Movie.VideoSourceSpecified Then
            MovieFile.VideoSource = _DBElement.Movie.VideoSource
        End If

        'Year
        If _DBElement.Movie.YearSpecified Then
            MovieFile.Year = _DBElement.Movie.Year
        End If

        If _DBElement.Movie.FileInfoSpecified Then
            'Resolution
            If _DBElement.Movie.FileInfo.StreamDetails.VideoSpecified Then
                Dim tVid As MediaContainers.Video = NFO.GetBestVideo(_DBElement.Movie.FileInfo)
                Dim tRes As String = NFO.GetResFromDimensions(tVid)
                MovieFile.Resolution = String.Format("{0}", If(String.IsNullOrEmpty(tRes), Master.eLang.GetString(138, "Unknown"), tRes))
            End If

            If _DBElement.Movie.FileInfo.StreamDetails.AudioSpecified Then
                Dim tAud As MediaContainers.Audio = NFO.GetBestAudio(_DBElement.Movie.FileInfo, False)

                'Audio Channels
                If tAud.ChannelsSpecified Then
                    MovieFile.AudioChannels = tAud.Channels
                End If

                'Audio Codec
                If tAud.CodecSpecified Then
                    MovieFile.AudioCodec = NormalizeAudioCodec(tAud.Codec)
                End If
            End If

            'MultiViewCount
            If _DBElement.Movie.FileInfo.StreamDetails.VideoSpecified Then
                If Not String.IsNullOrEmpty(_DBElement.Movie.FileInfo.StreamDetails.Video.Item(0).MultiViewCount) AndAlso CDbl(_DBElement.Movie.FileInfo.StreamDetails.Video.Item(0).MultiViewCount) > 1 Then
                    MovieFile.MultiViewCount = "3d"
                End If
            End If

            'MultiViewLayout
            If _DBElement.Movie.FileInfo.StreamDetails.VideoSpecified Then
                If _DBElement.Movie.FileInfo.StreamDetails.Video.Item(0).MultiViewLayoutSpecified Then
                    MovieFile.MultiViewLayout = _DBElement.Movie.FileInfo.StreamDetails.Video.Item(0).MultiViewLayout
                End If
            End If

            'StereoMode
            If _DBElement.Movie.FileInfo.StreamDetails.VideoSpecified Then
                If _DBElement.Movie.FileInfo.StreamDetails.Video.Item(0).StereoModeSpecified Then
                    MovieFile.StereoMode = _DBElement.Movie.FileInfo.StreamDetails.Video.Item(0).StereoMode
                    MovieFile.ShortStereoMode = _DBElement.Movie.FileInfo.StreamDetails.Video.Item(0).ShortStereoMode
                End If
            End If

            'Video Codec
            If _DBElement.Movie.FileInfo.StreamDetails.VideoSpecified Then
                If _DBElement.Movie.FileInfo.StreamDetails.Video.Item(0).CodecSpecified Then
                    MovieFile.VideoCodec = _DBElement.Movie.FileInfo.StreamDetails.Video.Item(0).Codec
                End If
            End If
        End If

        'Path
        MovieFile.BasePath = _DBElement.Source.Path
        MovieFile.OldFullPath = FileUtils.Common.GetMainPath(_DBElement.Filename).FullName
        MovieFile.Parent = FileUtils.Common.GetMainPath(_DBElement.Filename).Name
        If MovieFile.BasePath = FileUtils.Common.GetMainPath(_DBElement.Filename).FullName Then
            MovieFile.OldPath = String.Empty
            MovieFile.BasePath = Directory.GetParent(MovieFile.BasePath).FullName
        Else
            MovieFile.OldPath = Directory.GetParent(FileUtils.Common.GetMainPath(_DBElement.Filename).FullName).FullName.Replace(MovieFile.BasePath, String.Empty)
        End If
        MovieFile.IsBDMV = FileUtils.Common.isBDRip(_DBElement.Filename)
        MovieFile.IsVideoTS = FileUtils.Common.isVideoTS(_DBElement.Filename)

        MovieFile.Path = Path.Combine(MovieFile.OldPath, MovieFile.Parent)
        MovieFile.Path = If(MovieFile.Path.StartsWith(Path.DirectorySeparatorChar), MovieFile.Path.Substring(1), MovieFile.Path)

        'File Name
        MovieFile.Extension = Path.GetExtension(_DBElement.Filename)
        MovieFile.OldFullFileName = _DBElement.Filename
        If MovieFile.IsBDMV Then
            MovieFile.OldFileName = String.Concat("BDMV", Path.DirectorySeparatorChar, "STREAM")
        ElseIf MovieFile.IsVideoTS Then
            MovieFile.OldFileName = "VIDEO_TS"
        Else
            If Path.GetFileName(_DBElement.Filename.ToLower) = "video_ts.ifo" Then
                MovieFile.OldFileName = Path.GetFileNameWithoutExtension(_DBElement.Filename)
            Else
                MovieFile.OldFileName = Path.GetFileNameWithoutExtension(FileUtils.Common.RemoveStackingMarkers(_DBElement.Filename))
                Dim stackMark As String = Path.GetFileNameWithoutExtension(_DBElement.Filename).Replace(MovieFile.OldFileName, String.Empty).ToLower
                If Not stackMark = String.Empty AndAlso _DBElement.Movie.Title.ToLower.EndsWith(stackMark) Then
                    MovieFile.OldFileName = Path.GetFileNameWithoutExtension(_DBElement.Filename)
                End If
            End If
        End If

        Return MovieFile
    End Function

    Public Shared Function GetInfo_TVEpisode(ByVal _DBElement As Database.DBElement, Optional lstEpsiodes As List(Of Database.DBElement) = Nothing) As FileRename
        Dim EpisodeFile As New FileRename

        'get list of all episodes for multi-episode files
        Dim aSeasonsEpisodes As New List(Of SeasonsEpisodes)

        If _DBElement.FilenameIDSpecified Then

            'first step: get a list of all seasons
            Dim aSeasonsList As New List(Of Integer)
            Using SQLNewcommand As SQLite.SQLiteCommand = Master.DB.MyVideosDBConn.CreateCommand()
                SQLNewcommand.CommandText = String.Concat("SELECT Season FROM episode WHERE idFile = ", _DBElement.FilenameID, ";")
                Using SQLReader As SQLite.SQLiteDataReader = SQLNewcommand.ExecuteReader()
                    While SQLReader.Read
                        If Not aSeasonsList.Contains(Convert.ToInt32(SQLReader("Season"))) Then aSeasonsList.Add(Convert.ToInt32(SQLReader("Season")))
                    End While
                End Using
                aSeasonsList.Sort()
            End Using

            'second step: get all episodes per season
            For Each aSeason As Integer In aSeasonsList
                Dim aEpisodesList As New List(Of Episode)
                Using SQLNewcommand As SQLite.SQLiteCommand = Master.DB.MyVideosDBConn.CreateCommand()
                    SQLNewcommand.CommandText = String.Concat("SELECT idEpisode, Episode, Title, SubEpisode FROM episode WHERE idFile = ", _DBElement.FilenameID, " AND Season = ", aSeason, ";")
                    Using SQLReader As SQLite.SQLiteDataReader = SQLNewcommand.ExecuteReader()
                        While SQLReader.Read
                            Dim aEpisode As New Episode
                            aEpisode.ID = Convert.ToInt32(SQLReader("idEpisode"))
                            aEpisode.Episode = Convert.ToInt32(SQLReader("Episode"))
                            If Not DBNull.Value.Equals(SQLReader("SubEpisode")) Then aEpisode.SubEpisode = Convert.ToInt32(SQLReader("SubEpisode"))

                            'Title check
                            'Title from scraping results
                            If aEpisode.Episode = _DBElement.TVEpisode.Episode AndAlso
                                aEpisode.SubEpisode = _DBElement.TVEpisode.SubEpisode AndAlso
                                aSeason = _DBElement.TVEpisode.Season Then
                                aEpisode.Title = _DBElement.TVEpisode.Title
                            Else
                                'Title from scraping results
                                Dim nEpisodeInfo As Database.DBElement = Nothing
                                If lstEpsiodes IsNot Nothing Then
                                    nEpisodeInfo = lstEpsiodes.FirstOrDefault(Function(f) f.TVEpisode.Season = aSeason AndAlso f.TVEpisode.Episode = aEpisode.Episode)
                                End If
                                If nEpisodeInfo IsNot Nothing AndAlso nEpisodeInfo.TVEpisode.TitleSpecified Then
                                    aEpisode.Title = nEpisodeInfo.TVEpisode.Title
                                Else
                                    'Title from existing DB entry
                                    aEpisode.Title = SQLReader("Title").ToString
                                End If
                            End If
                            aEpisodesList.Add(aEpisode)
                        End While
                    End Using
                    aEpisodesList.Sort()
                End Using
                Dim aSeasonEpisodesList As New SeasonsEpisodes With {.Season = aSeason, .Episodes = aEpisodesList}
                aSeasonsEpisodes.Add(aSeasonEpisodesList)
            Next
        End If

        EpisodeFile.SeasonsEpisodes.AddRange(aSeasonsEpisodes)

        If EpisodeFile.SeasonsEpisodes.Count > 1 Then
            EpisodeFile.IsMultiEpisode = True
        Else
            For Each se In EpisodeFile.SeasonsEpisodes
                If se.Episodes.Count > 1 Then
                    EpisodeFile.IsMultiEpisode = True
                    Exit For
                End If
            Next
        End If

        'ID
        EpisodeFile.ID = _DBElement.ID

        'Aired
        If _DBElement.TVEpisode.AiredSpecified Then
            EpisodeFile.Aired = _DBElement.TVEpisode.Aired
        End If

        'Episode Title
        If Not EpisodeFile.IsMultiEpisode Then
            If _DBElement.TVEpisode.TitleSpecified Then
                EpisodeFile.Title = _DBElement.TVEpisode.Title
            End If
        Else
            Dim lTitles As New List(Of String)
            For Each lSeason In EpisodeFile.SeasonsEpisodes
                For Each lEpisode In lSeason.Episodes
                    lTitles.Add(lEpisode.Title)
                Next
            Next
            EpisodeFile.Title = String.Join(" - ", lTitles)
        End If

        'IsLock
        EpisodeFile.IsLock = _DBElement.IsLock

        'Rating
        If Not EpisodeFile.IsMultiEpisode Then
            If _DBElement.TVEpisode.RatingSpecified Then
                EpisodeFile.Rating = _DBElement.TVEpisode.Rating
            End If
        Else
            EpisodeFile.Rating = String.Empty
        End If

        'Show ListTitle
        If _DBElement.TVShow.TitleSpecified Then
            EpisodeFile.ListTitle = StringUtils.SortTokens(_DBElement.TVShow.Title)
        End If

        'Show OriginalTitle
        If _DBElement.TVShow.OriginalTitleSpecified Then
            EpisodeFile.OriginalTitle = _DBElement.TVShow.OriginalTitle
        End If

        'Show SortTitle
        If _DBElement.TVShow.SortTitleSpecified Then
            EpisodeFile.SortTitle = _DBElement.TVShow.SortTitle
        End If

        'Show Title
        If _DBElement.TVShow.TitleSpecified Then
            EpisodeFile.ShowTitle = _DBElement.TVShow.Title
        End If

        'VideoSource
        If _DBElement.TVEpisode.VideoSourceSpecified Then
            EpisodeFile.VideoSource = _DBElement.TVEpisode.VideoSource
        End If

        If _DBElement.TVEpisode.FileInfoSpecified Then
            'Resolution
            If _DBElement.TVEpisode.FileInfo.StreamDetails.VideoSpecified Then
                Dim tVid As MediaContainers.Video = NFO.GetBestVideo(_DBElement.TVEpisode.FileInfo)
                Dim tRes As String = NFO.GetResFromDimensions(tVid)
                EpisodeFile.Resolution = String.Format("{0}", If(String.IsNullOrEmpty(tRes), Master.eLang.GetString(138, "Unknown"), tRes))
            End If

            If _DBElement.TVEpisode.FileInfo.StreamDetails.AudioSpecified Then
                Dim tAud As MediaContainers.Audio = NFO.GetBestAudio(_DBElement.TVEpisode.FileInfo, False)

                'Audio Channels
                If tAud.ChannelsSpecified Then
                    EpisodeFile.AudioChannels = tAud.Channels
                End If

                'AudioCodec
                If tAud.CodecSpecified Then
                    EpisodeFile.AudioCodec = NormalizeAudioCodec(tAud.Codec)
                End If
            End If

            'MultiViewCount
            If _DBElement.TVEpisode.FileInfo.StreamDetails.VideoSpecified Then
                If _DBElement.TVEpisode.FileInfo.StreamDetails.Video.Item(0).MultiViewCountSpecified AndAlso CDbl(_DBElement.TVEpisode.FileInfo.StreamDetails.Video.Item(0).MultiViewCount) > 1 Then
                    EpisodeFile.MultiViewCount = "3d"
                End If
            End If

            'MultiViewLayout
            If _DBElement.TVEpisode.FileInfo.StreamDetails.VideoSpecified Then
                If _DBElement.TVEpisode.FileInfo.StreamDetails.Video.Item(0).MultiViewLayoutSpecified Then
                    EpisodeFile.MultiViewLayout = _DBElement.TVEpisode.FileInfo.StreamDetails.Video.Item(0).MultiViewLayout
                End If
            End If

            'StereoMode
            If _DBElement.TVEpisode.FileInfo.StreamDetails.VideoSpecified Then
                If _DBElement.TVEpisode.FileInfo.StreamDetails.Video.Item(0).StereoModeSpecified Then
                    EpisodeFile.StereoMode = _DBElement.TVEpisode.FileInfo.StreamDetails.Video.Item(0).StereoMode
                    EpisodeFile.ShortStereoMode = _DBElement.TVEpisode.FileInfo.StreamDetails.Video.Item(0).ShortStereoMode
                End If
            End If

            'Video Codec
            If _DBElement.TVEpisode.FileInfo.StreamDetails.VideoSpecified Then
                If _DBElement.TVEpisode.FileInfo.StreamDetails.Video.Item(0).CodecSpecified Then
                    EpisodeFile.VideoCodec = _DBElement.TVEpisode.FileInfo.StreamDetails.Video.Item(0).Codec
                End If
            End If
        End If

        'Path
        EpisodeFile.BasePath = _DBElement.Source.Path
        EpisodeFile.OldFullPath = FileUtils.Common.GetMainPath(_DBElement.Filename).FullName
        EpisodeFile.ShowFullPath = _DBElement.ShowPath
        EpisodeFile.ShowPath = _DBElement.ShowPath.Replace(_DBElement.Source.Path, String.Empty)
        EpisodeFile.ShowPath = If(EpisodeFile.ShowPath.StartsWith(Path.DirectorySeparatorChar), EpisodeFile.ShowPath.Substring(1), EpisodeFile.ShowPath)
        If FileUtils.Common.isVideoTS(_DBElement.Filename) Then
            EpisodeFile.Parent = FileUtils.Common.GetMainPath(_DBElement.Filename).Name
            If EpisodeFile.BasePath = FileUtils.Common.GetMainPath(_DBElement.Filename).FullName Then
                EpisodeFile.OldPath = String.Empty
                EpisodeFile.BasePath = Directory.GetParent(EpisodeFile.BasePath).FullName
            Else
                EpisodeFile.OldPath = Directory.GetParent(FileUtils.Common.GetMainPath(_DBElement.Filename).FullName).FullName.Replace(_DBElement.Source.Path, String.Empty)
            End If
        ElseIf FileUtils.Common.isBDRip(_DBElement.Filename) Then
            EpisodeFile.Parent = FileUtils.Common.GetMainPath(_DBElement.Filename).Name
            If EpisodeFile.BasePath = Directory.GetParent(FileUtils.Common.GetMainPath(_DBElement.Filename).FullName).FullName Then
                EpisodeFile.OldPath = String.Empty
                EpisodeFile.BasePath = Directory.GetParent(EpisodeFile.BasePath).FullName
            Else
                EpisodeFile.OldPath = Directory.GetParent(FileUtils.Common.GetMainPath(_DBElement.Filename).FullName).FullName.Replace(_DBElement.Source.Path, String.Empty)
            End If
        Else
            EpisodeFile.Parent = Directory.GetParent(_DBElement.Filename).FullName.Replace(Path.Combine(_DBElement.Source.Path, EpisodeFile.ShowPath), String.Empty).Trim
            EpisodeFile.Path = Directory.GetParent(_DBElement.Filename).FullName.Replace(_DBElement.Source.Path, String.Empty)
        End If
        EpisodeFile.IsBDMV = FileUtils.Common.isBDRip(_DBElement.Filename)
        EpisodeFile.IsVideoTS = FileUtils.Common.isVideoTS(_DBElement.Filename)

        EpisodeFile.Parent = If(EpisodeFile.Parent.StartsWith(Path.DirectorySeparatorChar), EpisodeFile.Parent.Substring(1), EpisodeFile.Parent)
        EpisodeFile.Path = If(EpisodeFile.Path.StartsWith(Path.DirectorySeparatorChar), EpisodeFile.Path.Substring(1), EpisodeFile.Path)

        'File
        EpisodeFile.Extension = Path.GetExtension(_DBElement.Filename)
        EpisodeFile.OldFullFileName = _DBElement.Filename
        If Not EpisodeFile.IsVideoTS AndAlso Not EpisodeFile.IsBDMV Then
            If Path.GetFileName(_DBElement.Filename.ToLower) = "video_ts.ifo" Then
                EpisodeFile.OldFileName = "VIDEO_TS"
            Else
                EpisodeFile.OldFileName = Path.GetFileNameWithoutExtension(FileUtils.Common.RemoveStackingMarkers(_DBElement.Filename))
                Dim stackMark As String = Path.GetFileNameWithoutExtension(_DBElement.Filename).Replace(EpisodeFile.OldFileName, String.Empty).ToLower
                If Not stackMark = String.Empty AndAlso _DBElement.TVEpisode.Title.ToLower.EndsWith(stackMark) Then
                    EpisodeFile.OldFileName = Path.GetFileNameWithoutExtension(_DBElement.Filename)
                End If
            End If
        ElseIf EpisodeFile.IsBDMV Then
            EpisodeFile.OldFileName = String.Concat("BDMV", Path.DirectorySeparatorChar, "STREAM")
        Else
            EpisodeFile.OldFileName = "VIDEO_TS"
        End If

        Return EpisodeFile
    End Function

    Public Shared Function GetInfo_TVShow(ByVal _DBElement As Database.DBElement) As FileRename
        Dim ShowFile As New FileRename

        'ID
        ShowFile.ID = _DBElement.ShowID

        'Genres
        If _DBElement.TVShow.GenresSpecified Then
            ShowFile.Genres = _DBElement.TVShow.Genres
        End If

        'IMDB
        If _DBElement.TVShow.UniqueIDs.IMDbIdSpecified Then
            ShowFile.IMDB = _DBElement.TVShow.UniqueIDs.IMDbId
        End If

        'IsLock
        ShowFile.IsLock = _DBElement.IsLock

        'IsSingle
        ShowFile.IsSingle = _DBElement.Source.IsSingle

        'ListTitle
        If _DBElement.TVShow.TitleSpecified Then
            ShowFile.ListTitle = StringUtils.SortTokens(_DBElement.TVShow.Title)
        End If

        'MPAA
        If _DBElement.TVShow.MPAASpecified Then
            ShowFile.MPAA = SelectMPAA(_DBElement.TVShow.MPAA)
        End If

        'OriginalTitle
        If _DBElement.TVShow.OriginalTitleSpecified Then
            ShowFile.OriginalTitle = _DBElement.TVShow.OriginalTitle
        End If

        'Rating
        If _DBElement.TVShow.RatingSpecified Then
            ShowFile.Rating = _DBElement.TVShow.Rating
        End If

        'SortTitle
        If _DBElement.TVShow.SortTitleSpecified Then
            ShowFile.SortTitle = _DBElement.TVShow.SortTitle
        End If

        'Title / ShowTitle
        If _DBElement.TVShow.TitleSpecified Then
            ShowFile.Title = _DBElement.TVShow.Title
            ShowFile.ShowTitle = _DBElement.TVShow.Title
        End If

        'TVDB
        If _DBElement.TVShow.UniqueIDs.TVDbIdSpecified Then
            ShowFile.TVDBID = _DBElement.TVShow.UniqueIDs.TVDbId.ToString
        End If

        'Year
        If _DBElement.TVShow.PremieredSpecified Then
            Dim tmpDate As Date
            If Date.TryParse(_DBElement.TVShow.Premiered, tmpDate) Then
                ShowFile.Year = tmpDate.Year.ToString
            End If
        End If

        'Path
        ShowFile.BasePath = _DBElement.Source.Path
        ShowFile.OldFullPath = _DBElement.ShowPath
        ShowFile.ShowPath = _DBElement.ShowPath.Replace(_DBElement.Source.Path, String.Empty)
        ShowFile.ShowPath = If(ShowFile.ShowPath.StartsWith(Path.DirectorySeparatorChar), ShowFile.ShowPath.Substring(1), ShowFile.ShowPath)
        If ShowFile.BasePath = Directory.GetParent(_DBElement.ShowPath).FullName Then
            ShowFile.OldPath = String.Empty
        Else
            ShowFile.OldPath = Directory.GetParent(Directory.GetParent(_DBElement.ShowPath).FullName).FullName.Replace(_DBElement.Source.Path, String.Empty)
        End If

        ShowFile.Path = Path.Combine(ShowFile.OldPath, Path.GetFileName(_DBElement.ShowPath))
        ShowFile.Path = If(ShowFile.Path.StartsWith(Path.DirectorySeparatorChar), ShowFile.Path.Substring(1), ShowFile.Path)

        Return ShowFile
    End Function

    Public Function GetTable_Movies() As DataTable
        Dim dtMovies As New DataTable

        dtMovies.Columns.Add(Master.eLang.GetString(21, "Title"), GetType(String))
        dtMovies.Columns.Add(Master.eLang.GetString(410, "Path"), GetType(String))
        dtMovies.Columns.Add(Master.eLang.GetString(15, "File Name"), GetType(String))
        dtMovies.Columns.Add(Master.eLang.GetString(141, "New Path"), GetType(String))
        dtMovies.Columns.Add(Master.eLang.GetString(142, "New File Name"), GetType(String))
        dtMovies.Columns.Add("IsLocked", GetType(Boolean))
        dtMovies.Columns.Add("DirExist", GetType(Boolean))
        dtMovies.Columns.Add("FileExist", GetType(Boolean))
        dtMovies.Columns.Add("IsSingle", GetType(Boolean))
        dtMovies.Columns.Add("IsRenamed", GetType(Boolean))

        For Each dtRow As FileRename In _movies
            dtMovies.Rows.Add(dtRow.Title, dtRow.Path, dtRow.OldFileName, dtRow.NewPath,
                              dtRow.NewFileName, dtRow.IsLock, dtRow.DirExist,
                              dtRow.FileExist, dtRow.IsSingle, dtRow.DoRename)
        Next

        Return dtMovies
    End Function

    Public Function GetTable_TVEpisodes() As DataTable
        Dim dtEpisodes As New DataTable

        dtEpisodes.Columns.Add(Master.eLang.GetString(21, "Title"), GetType(String))
        dtEpisodes.Columns.Add(Master.eLang.GetString(410, "Path"), GetType(String))
        dtEpisodes.Columns.Add(Master.eLang.GetString(15, "File Name"), GetType(String))
        dtEpisodes.Columns.Add(Master.eLang.GetString(141, "New Path"), GetType(String))
        dtEpisodes.Columns.Add(Master.eLang.GetString(142, "New File Name"), GetType(String))
        dtEpisodes.Columns.Add("IsLocked", GetType(Boolean))
        dtEpisodes.Columns.Add("DirExist", GetType(Boolean))
        dtEpisodes.Columns.Add("FileExist", GetType(Boolean))
        dtEpisodes.Columns.Add("IsSingle", GetType(Boolean))
        dtEpisodes.Columns.Add("IsRenamed", GetType(Boolean))

        For Each dtRow As FileRename In _episodes
            dtEpisodes.Rows.Add(dtRow.Title, dtRow.Path, dtRow.OldFileName, dtRow.NewPath,
                              dtRow.NewFileName, dtRow.IsLock, dtRow.DirExist,
                              dtRow.FileExist, dtRow.IsSingle, dtRow.DoRename)
        Next

        Return dtEpisodes
    End Function

    Public Shared Function HaveBase(ByVal strPattern As String) As Boolean
        Return strPattern.Contains("$B")
    End Function

    Public Shared Sub Process_Movie(ByRef MovieFile As FileRename, ByVal folderPattern As String, ByVal filePattern As String)
        Try
            'Mehrfach-Datei-Situation: der Unterepisoden-Anteil darf nicht in den
            'Dateinamen, sonst entsteht "S01E01.2" zuzueglich Suffix.
            If Not String.IsNullOrEmpty(MovieFile.MultiFileMode) Then SuppressSubEpisode(MovieFile)

            If HaveBase(folderPattern) Then
                MovieFile.NewPath = ProccessPattern(MovieFile, If(MovieFile.IsSingle, folderPattern, "$D"), True).Trim
            Else
                MovieFile.NewPath = Path.Combine(MovieFile.OldPath, ProccessPattern(MovieFile, If(MovieFile.IsSingle, folderPattern, "$D"), True).Trim)
            End If

            If Not MovieFile.IsVideoTS AndAlso Not MovieFile.IsBDMV Then
                If MovieFile.OldFileName.ToLower = "video_ts" Then
                    MovieFile.NewFileName = MovieFile.OldFileName
                Else
                    MovieFile.NewFileName = ProccessPattern(MovieFile, filePattern, False).Trim
                End If
            ElseIf MovieFile.IsBDMV Then
                MovieFile.NewFileName = MovieFile.OldFileName
            Else
                MovieFile.NewFileName = MovieFile.OldFileName
            End If

            'Begleitdateien (Poster, Fanart, ...) tragen keinen versionsspezifischen
            'Zusatz. Aufloesung ($R) und Audio-Codec ($J) stehen nur in der Videodatei
            'und ihrer NFO.
            Dim strippedPattern As String = StripTechnicalTokens(filePattern)
            MovieFile.NewSidecarName = If(strippedPattern <> filePattern,
                                         ProccessPattern(MovieFile, strippedPattern, False).Trim,
                                         MovieFile.NewFileName)

            'Teile oder Versionen: Suffix an Datei- und Begleitdateinamen anhaengen
            ApplyMultiFileSuffix(MovieFile)

            'removes all leading DirectorySeparatorChar (otherwise, Path.Combine later does not work)
            MovieFile.NewPath = StringUtils.CleanPath(MovieFile.NewPath)

            UpdateMovieRenameState(MovieFile)
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            MovieFile.DoRename = False
        End Try
    End Sub

    Public Shared Sub Process_TVEpisode(ByRef EpisodeFile As FileRename, ByVal folderPatternSeasons As String, ByVal filePatternEpisodes As String)
        Try
            'Mehrfach-Datei-Situation: der Unterepisoden-Anteil darf nicht in den
            'Dateinamen, sonst entsteht "S01E01.2" zuzueglich Suffix.
            If Not String.IsNullOrEmpty(EpisodeFile.MultiFileMode) Then SuppressSubEpisode(EpisodeFile)

            Dim pSeason As String = ProccessPattern(EpisodeFile, folderPatternSeasons, True).Trim

            'Staffel 0 wird wie bei tinyMediaManager und Jellyfin im Ordner
            '"Specials" abgelegt und nicht als "Season 00".
            If EpisodeFile.SeasonsEpisodes.Any(Function(s) s.Season = 0) Then
                Dim strSpecials As String = AdvancedSettings.GetSetting("SpecialsFolder", "Specials", MULTIFILE_SECTION, Enums.ContentType.TVSeason)
                If Not String.IsNullOrEmpty(strSpecials) Then pSeason = strSpecials
            End If

            EpisodeFile.NewPath = Path.Combine(EpisodeFile.ShowPath, pSeason)

            If Not EpisodeFile.IsVideoTS AndAlso Not EpisodeFile.IsBDMV Then
                If EpisodeFile.OldFileName.ToLower = "video_ts" Then
                    EpisodeFile.NewFileName = EpisodeFile.OldFileName
                Else
                    EpisodeFile.NewFileName = ProccessPattern(EpisodeFile, filePatternEpisodes, False, EpisodeFile.IsMultiEpisode).Trim
                End If
            ElseIf EpisodeFile.IsBDMV Then
                EpisodeFile.NewFileName = EpisodeFile.OldFileName
            Else
                EpisodeFile.NewFileName = EpisodeFile.OldFileName
            End If

            'Teile oder Versionen: Suffix an den Dateinamen anhaengen
            ApplyMultiFileSuffix(EpisodeFile)

            Dim newFullFileName As String = Path.Combine(EpisodeFile.BasePath, Path.Combine(EpisodeFile.NewPath, String.Concat(EpisodeFile.NewFileName, EpisodeFile.Extension)))
            EpisodeFile.FileExist = File.Exists(newFullFileName) AndAlso Not (newFullFileName.ToLower = EpisodeFile.OldFullFileName.ToLower)
            EpisodeFile.DoRename = Not EpisodeFile.NewPath = EpisodeFile.Path OrElse Not EpisodeFile.NewFileName = EpisodeFile.OldFileName
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            EpisodeFile.DoRename = False
        End Try
    End Sub

    Public Shared Sub Process_TVShow(ByRef ShowFile As FileRename, ByVal folderPatternShows As String)
        Try
            If HaveBase(folderPatternShows) Then
                ShowFile.NewPath = ProccessPattern(ShowFile, folderPatternShows, True).Trim
            Else
                ShowFile.NewPath = Path.Combine(ShowFile.OldPath, ProccessPattern(ShowFile, folderPatternShows, True).Trim)
            End If

            'removes all leading DirectorySeparatorChar (otherwise, Path.Combine later does not work)
            ShowFile.NewPath = StringUtils.CleanPath(ShowFile.NewPath)

            Dim newFullDirPath As String = Path.Combine(ShowFile.BasePath, ShowFile.NewPath)
            Dim newDirInfo As New DirectoryInfo(newFullDirPath)
            ShowFile.DirExist = newDirInfo.Exists AndAlso Not If(newFullDirPath.ToLower = ShowFile.OldFullPath.ToLower, True, newDirInfo.GetFileSystemInfos.Count = 0)
            ShowFile.DoRename = Not ShowFile.NewPath = ShowFile.Path
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            ShowFile.DoRename = False
        End Try
    End Sub

    Public Sub ProccessFiles_Movies(ByVal folderPattern As String, ByVal filePattern As String, Optional ByVal folderPatternIsNotSingle As String = "$D")
        Try
            For Each f As FileRename In _movies
                Process_Movie(f, If(f.IsSingle, folderPattern, folderPatternIsNotSingle), filePattern)
            Next

            'Mehrfach-Dateien: mehrere Filme bekommen denselben Zielnamen
            HandleMultiFile_Movies()
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Public Sub ProccessFiles_TVEpisodes(ByVal folderPatternSeasons As String, ByVal filePatternEpisodes As String)
        Try
            For Each f As FileRename In _episodes
                Process_TVEpisode(f, folderPatternSeasons, filePatternEpisodes)
            Next
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    Public Shared Function ProccessPattern(ByVal f As FileRename, ByVal opattern As String, ByVal isPath As Boolean, Optional isMultiEpisode As Boolean = False) As String
        Try
            If Not String.IsNullOrEmpty(opattern) Then
                Dim pattern As String = opattern
                Dim joinIndex As Integer
                Dim nextC = pattern.IndexOf("$")
                Dim nextIB = pattern.IndexOf("{")
                Dim nextEB = pattern.IndexOf("}")
                Dim strCond As String
                Dim strBase As String
                Dim strNoFlags As String
                Dim strJoin As String
                Dim dRating As Double
                While Not nextC = -1
                    If nextC > nextIB AndAlso nextC < nextEB AndAlso Not nextC = -1 AndAlso Not nextIB = -1 AndAlso Not nextEB = -1 Then
                        strCond = pattern.Substring(nextIB, nextEB - nextIB + 1)
                        strNoFlags = strCond
                        strBase = strCond
                        strCond = ApplyPattern(strCond, "1", If(Not String.IsNullOrEmpty(f.SortTitle), f.SortTitle.Substring(0, 1), String.Empty))
                        strCond = ApplyPattern(strCond, "2", f.Aired)
                        strCond = ApplyPattern(strCond, "3", f.ShortStereoMode)
                        strCond = ApplyPattern(strCond, "4", f.StereoMode)
                        strCond = ApplyPattern(strCond, "5", f.CollectionListTitle)
                        strCond = ApplyPattern(strCond, "6", f.Edition)
                        strCond = ApplyPattern(strCond, "A", f.AudioChannels)
                        strCond = ApplyPattern(strCond, "B", String.Empty) 'This is not needed here, Only to HaveBase
                        strCond = ApplyPattern(strCond, "C", String.Join(",", f.Directors))
                        strCond = ApplyPattern(strCond, "D", f.Parent)
                        strCond = ApplyPattern(strCond, "E", f.SortTitle)
                        strCond = ApplyPattern(strCond, "F", f.OldFileName.Replace("\", String.Empty))
                        '                                G   Genres
                        strCond = ApplyPattern(strCond, "H", f.VideoCodec)
                        strCond = ApplyPattern(strCond, "I", f.IMDB)
                        strCond = ApplyPattern(strCond, "J", f.AudioCodec)
                        '                                K   Season
                        strCond = ApplyPattern(strCond, "L", f.ListTitle)
                        strCond = ApplyPattern(strCond, "M", f.MPAA)
                        strCond = ApplyPattern(strCond, "N", f.Collection)
                        '                                O   OriginalTitle
                        '                                OO  OriginalTitle if different from the title
                        dRating = 0
                        strCond = ApplyPattern(strCond, "P", If(Not String.IsNullOrEmpty(f.Rating) AndAlso Double.TryParse(f.Rating, Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, dRating), String.Format("{0:0.0}", dRating), String.Empty))
                        '                                Q   Episode
                        strCond = ApplyPattern(strCond, "R", f.Resolution)
                        strCond = ApplyPattern(strCond, "S", f.VideoSource)
                        strCond = ApplyPattern(strCond, "T", f.Title)
                        '                                U   Countries
                        strCond = ApplyPattern(strCond, "V", f.MultiViewCount)
                        '                                W   SeasonEpisode
                        '                                X   
                        strCond = ApplyPattern(strCond, "Y", f.Year)
                        strCond = ApplyPattern(strCond, "Z", f.ShowTitle)

                        'Genres
                        joinIndex = strCond.IndexOf("$G")
                        If Not joinIndex = -1 Then
                            If strCond.Length > joinIndex + 2 Then
                                strJoin = strCond.Substring(joinIndex + 2, 1)
                                If Not ". -,".IndexOf(strJoin) = -1 Then
                                    strCond = ApplyPattern(strCond, String.Concat("G", strJoin), String.Join(strJoin, f.Genres))
                                Else
                                    strCond = ApplyPattern(strCond, "G", String.Join(" ", f.Genres))
                                End If
                            Else
                                strCond = ApplyPattern(strCond, "G", String.Join(" ", f.Genres))
                            End If
                        End If

                        'OriginalTitle
                        joinIndex = strCond.IndexOf("$O")
                        If Not joinIndex = -1 Then
                            If strCond.Length > joinIndex + 2 Then
                                strJoin = strCond.Substring(joinIndex + 2, 1)
                                If strJoin = "O" Then
                                    strCond = ApplyPattern(strCond, "OO", If(Not f.OriginalTitle = f.Title, f.OriginalTitle, String.Empty))
                                Else
                                    strCond = ApplyPattern(strCond, "O", f.OriginalTitle)
                                End If
                            Else
                                strCond = ApplyPattern(strCond, "O", f.OriginalTitle)
                            End If
                        End If

                        'Countries
                        joinIndex = strCond.IndexOf("$U")
                        If Not joinIndex = -1 Then
                            If strCond.Length > joinIndex + 2 Then
                                strJoin = strCond.Substring(joinIndex + 2, 1)
                                If Not ". -,".IndexOf(strJoin) = -1 Then
                                    strCond = ApplyPattern(strCond, String.Concat("U", strJoin), String.Join(strJoin, f.Countries))
                                Else
                                    strCond = ApplyPattern(strCond, "U", String.Join(" ", f.Countries))
                                End If
                            Else
                                strCond = ApplyPattern(strCond, "U", String.Join(" ", f.Countries))
                            End If
                        End If

                        strNoFlags = Regex.Replace(strNoFlags, "\$((?:OO|[123456ABCDEFHIJKLMNOPQRSTVWY]|G[. -,]|U[. -,]?))", String.Empty) '"(?i)\$([DFTYRAS])"  "\$((?i:[DFTYRAS]))"
                        If strCond.Trim = strNoFlags.Trim Then
                            strCond = String.Empty
                        Else
                            strCond = strCond.Substring(1, strCond.Length - 2)
                        End If
                        pattern = pattern.Replace(strBase, strCond)
                        nextC = pattern.IndexOf("$")
                    Else
                        nextC = pattern.IndexOf("$", nextC + 1)
                    End If
                    nextIB = pattern.IndexOf("{")
                    nextEB = pattern.IndexOf("}")
                End While

                pattern = ApplyPattern(pattern, "1", If(Not String.IsNullOrEmpty(f.SortTitle), f.SortTitle.Substring(0, 1), String.Empty))
                pattern = ApplyPattern(pattern, "2", f.Aired)
                pattern = ApplyPattern(pattern, "3", f.ShortStereoMode)
                pattern = ApplyPattern(pattern, "4", f.StereoMode)
                pattern = ApplyPattern(pattern, "5", f.CollectionListTitle)
                pattern = ApplyPattern(pattern, "6", f.Edition)
                pattern = ApplyPattern(pattern, "A", f.AudioChannels)
                pattern = ApplyPattern(pattern, "B", String.Empty) 'This is not need here, Only to HaveBase
                pattern = ApplyPattern(pattern, "C", String.Join(",", f.Directors))
                pattern = ApplyPattern(pattern, "D", f.Parent) '.Replace("\", String.Empty))
                pattern = ApplyPattern(pattern, "E", f.SortTitle)
                pattern = ApplyPattern(pattern, "F", f.OldFileName.Replace("\", String.Empty))
                '                                G   Genres
                pattern = ApplyPattern(pattern, "H", f.VideoCodec)
                pattern = ApplyPattern(pattern, "I", f.IMDB)
                pattern = ApplyPattern(pattern, "J", f.AudioCodec)
                '                                K   Season
                pattern = ApplyPattern(pattern, "L", f.ListTitle)
                pattern = ApplyPattern(pattern, "M", f.MPAA)
                pattern = ApplyPattern(pattern, "N", f.Collection)
                '                                O   OriginalTitle
                '                                OO  OriginalTitle if different from the title
                dRating = 0
                pattern = ApplyPattern(pattern, "P", If(Not String.IsNullOrEmpty(f.Rating) AndAlso Double.TryParse(f.Rating, Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, dRating), String.Format("{0:0.0}", dRating), String.Empty))
                '                                Q   Episode
                pattern = ApplyPattern(pattern, "R", f.Resolution)
                pattern = ApplyPattern(pattern, "S", f.VideoSource)
                pattern = ApplyPattern(pattern, "T", f.Title)
                '                                U   Countries
                pattern = ApplyPattern(pattern, "V", f.MultiViewCount)
                '                                W   SeasonEpisode
                '                                X   
                pattern = ApplyPattern(pattern, "Y", f.Year)
                pattern = ApplyPattern(pattern, "Z", f.ShowTitle)

                'season and episode
                nextC = pattern.IndexOf("$W")
                If Not nextC = -1 Then
                    If pattern.Length > nextC + 2 Then
                        Dim ePattern As String = String.Empty
                        Dim ePrefix As String = String.Empty
                        Dim ePadding As Integer = 1
                        Dim eFormat As String = "{0:0}"
                        Dim eSeparator As String = String.Empty
                        Dim fPattern As String = String.Empty
                        Dim sPattern As String = String.Empty
                        Dim sPrefix As String = String.Empty
                        Dim sSeparator As String = String.Empty
                        Dim seString As String = String.Empty
                        Dim sPadding As Integer = 1
                        Dim sFormat As String = "{0:0}"

                        strBase = pattern.Substring(nextC)
                        nextIB = strBase.IndexOf("?")
                        If nextIB > -1 Then
                            nextEB = strBase.IndexOf("?", nextIB + 1)
                            If nextEB > -1 Then
                                sPattern = strBase.Substring(2, nextIB - 2)
                                ePattern = strBase.Substring(nextIB + 1, nextEB - nextIB - 1)
                                fPattern = strBase.Substring(0, nextEB + 1)
                            Else
                                'Nur eine Trennstelle: der Token endet nach der Staffel.
                                sPattern = strBase.Substring(2, nextIB - 2)
                                ePattern = "2E"
                                fPattern = strBase.Substring(0, nextIB + 1)
                            End If
                        Else
                            'Kein "?" im Token (z.B. "$W2_S"): die Folge wird im
                            'Standardformat "E01" angehaengt.
                            sPattern = If(strBase.Length > 2, strBase.Substring(2), String.Empty)
                            ePattern = "2E"
                            fPattern = strBase
                        End If
                        If Not String.IsNullOrEmpty(sPattern) AndAlso Integer.TryParse(sPattern.Substring(0, 1), 0) Then
                            sPadding = CInt(sPattern.Substring(0, 1))
                            sPattern = sPattern.Substring(1)
                            If Not sPadding > 0 Then
                                sPadding = 1
                            End If
                        End If
                        If Not String.IsNullOrEmpty(ePattern) AndAlso Integer.TryParse(ePattern.Substring(0, 1), 0) Then
                            ePadding = CInt(ePattern.Substring(0, 1))
                            ePattern = ePattern.Substring(1)
                            If Not ePadding > 0 Then
                                ePadding = 1
                            End If
                        End If

                        If sPattern.StartsWith(".") OrElse sPattern.StartsWith("_") Then
                            sSeparator = sPattern.Substring(0, 1)
                            sPrefix = sPattern.Remove(0, 1)
                        Else
                            sPrefix = sPattern
                        End If

                        For i As Integer = 1 To sPadding - 1
                            sFormat = sFormat.Insert(sFormat.Length - 1, "0")
                        Next

                        If ePattern.StartsWith(".") OrElse ePattern.StartsWith("_") OrElse ePattern.StartsWith("x") Then
                            eSeparator = ePattern.Substring(0, 1)
                            ePrefix = ePattern.Remove(0, 1)
                        Else
                            ePrefix = ePattern
                        End If

                        For i As Integer = 1 To ePadding - 1
                            eFormat = eFormat.Insert(eFormat.Length - 1, "0")
                        Next

                        For Each season As SeasonsEpisodes In f.SeasonsEpisodes
                            seString = String.Concat(seString, sSeparator, sPrefix, String.Format(sFormat, season.Season))
                            For Each episode In season.Episodes
                                If Not episode.SubEpisode = -1 Then
                                    seString = String.Concat(seString, eSeparator, ePrefix, String.Concat(String.Format(eFormat, episode.Episode), ".", episode.SubEpisode))
                                Else
                                    seString = String.Concat(seString, eSeparator, ePrefix, String.Format(eFormat, episode.Episode))
                                End If
                            Next
                        Next

                        If Not String.IsNullOrEmpty(sSeparator) AndAlso seString.StartsWith(sSeparator) Then seString = seString.Remove(0, 1)

                        pattern = pattern.Replace(fPattern, seString)
                    End If
                End If

                'season
                nextC = pattern.IndexOf("$K")
                If Not nextC = -1 Then
                    If pattern.Length > nextC + 1 Then
                        Dim sPattern As String = String.Empty
                        Dim sPrefix As String = String.Empty
                        Dim sSeparator As String = String.Empty
                        Dim fPattern As String = String.Empty
                        Dim sString As String = String.Empty
                        Dim sPadding As Integer = 1
                        Dim sFormat As String = "{0:0}"

                        strBase = pattern.Substring(nextC)
                        nextIB = strBase.IndexOf("?")
                        If nextIB > -1 Then
                            sPattern = strBase.Substring(2, nextIB - 2)
                            fPattern = strBase.Substring(0, nextIB + 1)
                        Else
                            'Kein "?" im Token (z.B. "Season $K2"): der Token wird komplett
                            'ersetzt. Ohne diesen Zweig bliebe das Suchmuster leer und
                            'String.Replace wuerde mit einer Ausnahme abbrechen.
                            sPattern = If(strBase.Length > 2, strBase.Substring(2), String.Empty)
                            fPattern = strBase
                        End If
                        If Not String.IsNullOrEmpty(sPattern) AndAlso Integer.TryParse(sPattern.Substring(0, 1), 0) Then
                            sPadding = CInt(sPattern.Substring(0, 1))
                            sPattern = sPattern.Substring(1)
                            If Not sPadding > 0 Then
                                sPadding = 1
                            End If
                        End If

                        If sPattern.StartsWith(".") OrElse sPattern.StartsWith("_") OrElse sPattern.StartsWith("x") Then
                            sSeparator = sPattern.Substring(0, 1)
                            sPrefix = sPattern.Remove(0, 1)
                        Else
                            sPrefix = sPattern
                        End If

                        For i As Integer = 1 To sPadding - 1
                            sFormat = sFormat.Insert(sFormat.Length - 1, "0")
                        Next

                        For Each season As SeasonsEpisodes In f.SeasonsEpisodes
                            sString = String.Concat(sString, sSeparator, sPrefix, String.Format(sFormat, season.Season))
                        Next

                        If Not String.IsNullOrEmpty(sSeparator) AndAlso sString.StartsWith(sSeparator) Then sString = sString.Remove(0, 1)

                        pattern = pattern.Replace(fPattern, sString)
                    End If
                End If

                'episode
                nextC = pattern.IndexOf("$Q")
                If Not nextC = -1 Then
                    If pattern.Length > nextC + 2 Then
                        Dim ePattern As String = String.Empty
                        Dim ePrefix As String = String.Empty
                        Dim eSeparator As String = String.Empty
                        Dim fPattern As String = String.Empty
                        Dim eString As String = String.Empty
                        Dim ePadding As Integer = 1
                        Dim eFormat As String = "{0:0}"

                        strBase = pattern.Substring(nextC)
                        nextIB = strBase.IndexOf("?")
                        If nextIB > -1 Then
                            ePattern = strBase.Substring(2, nextIB - 2)
                            fPattern = strBase.Substring(0, nextIB + 1)
                        Else
                            'Kein "?" im Token: der Token wird komplett ersetzt, sonst
                            'wuerde String.Replace mit einem leeren Suchmuster eine
                            'Ausnahme ausloesen.
                            ePattern = If(strBase.Length > 2, strBase.Substring(2), String.Empty)
                            fPattern = strBase
                        End If
                        If Not String.IsNullOrEmpty(ePattern) AndAlso Integer.TryParse(ePattern.Substring(0, 1), 0) Then
                            ePadding = CInt(ePattern.Substring(0, 1))
                            ePattern = ePattern.Substring(1)
                            If Not ePadding > 0 Then
                                ePadding = 1
                            End If
                        End If

                        If ePattern.StartsWith(".") OrElse ePattern.StartsWith("_") OrElse ePattern.StartsWith("x") Then
                            eSeparator = ePattern.Substring(0, 1)
                            ePrefix = ePattern.Remove(0, 1)
                        Else
                            ePrefix = ePattern
                        End If

                        For i As Integer = 1 To ePadding - 1
                            eFormat = eFormat.Insert(eFormat.Length - 1, "0")
                        Next

                        For Each season As SeasonsEpisodes In f.SeasonsEpisodes
                            For Each episode In season.Episodes
                                If Not episode.SubEpisode = -1 Then
                                    eString = String.Concat(eString, eSeparator, ePrefix, String.Concat(String.Format(eFormat, episode.Episode), ".", episode.SubEpisode))
                                Else
                                    eString = String.Concat(eString, eSeparator, ePrefix, String.Format(eFormat, episode.Episode))
                                End If
                            Next
                        Next

                        If Not String.IsNullOrEmpty(eSeparator) AndAlso eString.StartsWith(eSeparator) Then eString = eString.Remove(0, 1)

                        pattern = pattern.Replace(fPattern, eString)
                    End If
                End If

                'OriginalTitle
                nextC = pattern.IndexOf("$O")
                If Not nextC = -1 Then
                    If pattern.Length > nextC + 2 Then
                        strCond = pattern.Substring(nextC + 2, 1)
                        If strCond = "O" Then
                            pattern = ApplyPattern(pattern, "OO", If(Not f.OriginalTitle = f.Title, f.OriginalTitle, String.Empty))
                        Else
                            pattern = ApplyPattern(pattern, "O", f.OriginalTitle)
                        End If
                    Else
                        pattern = ApplyPattern(pattern, "O", f.OriginalTitle)
                    End If
                End If

                'Genres
                nextC = pattern.IndexOf("$G")
                If Not nextC = -1 Then
                    If pattern.Length > nextC + 2 Then
                        strCond = pattern.Substring(nextC + 2, 1)
                        If Not ". -,".IndexOf(strCond) = -1 Then
                            pattern = ApplyPattern(pattern, String.Concat("G", strCond), String.Join(strCond, f.Genres))
                        Else
                            pattern = ApplyPattern(pattern, "G", String.Join(" ", f.Genres))
                        End If
                    Else
                        pattern = ApplyPattern(pattern, "G", String.Join(" ", f.Genres))
                    End If
                End If

                'Countries
                nextC = pattern.IndexOf("$U")
                If Not nextC = -1 Then
                    If pattern.Length > nextC + 2 Then
                        strCond = pattern.Substring(nextC + 2, 1)
                        If Not ". -,".IndexOf(strCond) = -1 Then
                            pattern = ApplyPattern(pattern, String.Concat("U", strCond), String.Join(strCond, f.Countries))
                        Else
                            pattern = ApplyPattern(pattern, "U", String.Join(" ", f.Countries))
                        End If
                    Else
                        pattern = ApplyPattern(pattern, "U", String.Join(" ", f.Countries))
                    End If
                End If

                'Replace all spaces with character
                nextC = pattern.IndexOf("$X")
                If Not nextC = -1 AndAlso pattern.Length > nextC + 2 Then
                    strCond = pattern.Substring(nextC + 2, 1)
                    pattern = pattern.Replace(String.Concat("$X", strCond), String.Empty)
                    pattern = pattern.Replace(" ", strCond)
                End If

                'Replace character(s) with another character(s)
                nextC = pattern.IndexOf("$?")
                Dim strmore As String = String.Empty
                While nextC > -1
                    'If nextC > -1 Then
                    strBase = pattern.Substring(nextC + 2)
                    pattern = pattern.Substring(0, nextC)
                    If Not strBase = String.Empty Then
                        nextIB = strBase.IndexOf("?")
                        If nextIB > -1 Then
                            nextEB = strBase.Substring(nextIB + 1).IndexOf("?")
                            If nextEB > -1 Then
                                strCond = strBase.Substring(nextIB + 1, nextEB)
                                strmore = strBase.Substring(nextIB + nextEB + 2)
                                strBase = strBase.Substring(0, nextIB)
                                If Not strBase = String.Empty Then pattern = pattern.Replace(strBase, strCond)
                            End If
                        End If
                    End If
                    'End If
                    pattern = String.Concat(pattern, strmore)
                    nextC = pattern.IndexOf("$?")
                End While

                'Lowercase all letters
                nextC = pattern.IndexOf("$;")
                If Not nextC = -1 Then
                    pattern = pattern.ToLower
                    pattern = pattern.Replace("$;", String.Empty)
                End If

                'Uppercase first letter in each word
                nextC = pattern.IndexOf("$!")
                If Not nextC = -1 Then
                    pattern = Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(pattern)
                    pattern = pattern.Replace("$!", String.Empty)
                End If

                'Cleaning
                If isPath Then
                    pattern = StringUtils.CleanPath(pattern)
                Else
                    pattern = StringUtils.CleanFileName(pattern)
                End If

                Return pattern.Trim
            Else
                Return String.Empty
            End If
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return String.Empty
        End Try
    End Function

    Public Shared Sub RenameSingle_Movie(ByRef _tmpMovie As Database.DBElement, ByVal folderPattern As String, ByVal filePattern As String, ByVal BatchMode As Boolean, ByVal ShowError As Boolean, ByVal toDB As Boolean)
        Dim MovieFile As New FileRename

        MovieFile = GetInfo_Movie(_tmpMovie)
        Process_Movie(MovieFile, folderPattern, filePattern)

        'Mehrfach-Datei-Situation: die berechnete Zieldatei existiert bereits
        If MovieFile.FileExist Then
            Dim mode As String = GetMultiFileMode(MultiFileKey(MULTIFILE_PREFIX_MOVIE, MovieFile.ID), MovieFile.Title, Enums.ContentType.Movie)
            If Not String.IsNullOrEmpty(mode) Then
                MovieFile.MultiFileMode = mode
                MovieFile.PartNumber = GetMultiFilePartNumber(MovieFile, mode)
                ApplyMultiFileSuffix(MovieFile)
                UpdateMovieRenameState(MovieFile)
            End If
        End If

        If MovieFile.DoRename Then
            DoRenameSingle_Movie(MovieFile, _tmpMovie, BatchMode, ShowError, toDB)
        Else
            If toDB Then
                Master.DB.Save_Movie(_tmpMovie, BatchMode, False, False, True, False)
            End If
        End If
    End Sub

    Public Shared Sub RenameSingle_TVEpisode(ByRef _tmpEpisode As Database.DBElement, ByVal folderPatternSeasons As String, ByVal filePatternEpisodes As String, ByVal BatchMode As Boolean, ByVal ShowError As Boolean, ByVal toDB As Boolean)
        Dim EpisodeFile As New FileRename

        EpisodeFile = GetInfo_TVEpisode(_tmpEpisode)
        Process_TVEpisode(EpisodeFile, folderPatternSeasons, filePatternEpisodes)

        If EpisodeFile.DoRename Then
            DoRenameSingle_TVEpisode(EpisodeFile, _tmpEpisode, BatchMode, ShowError, toDB)
        Else
            If toDB Then
                Master.DB.Save_TVEpisode(_tmpEpisode, BatchMode, False, False, False, True)
            End If
        End If
    End Sub

    Public Shared Sub RenameSingle_TVShow(ByRef _tmpShow As Database.DBElement, ByVal folderPatternShows As String, ByVal folderPatternSeasons As String, ByVal filePatternEpisodes As String, ByVal BatchMode As Boolean, ByVal ShowError As Boolean, ByVal toDB As Boolean)
        Dim ShowFile As New FileRename

        ShowFile = GetInfo_TVShow(_tmpShow)
        Process_TVShow(ShowFile, folderPatternShows)

        'rename episodes (does not change the tvshow folder)
        If Not String.IsNullOrEmpty(folderPatternSeasons) AndAlso Not String.IsNullOrEmpty(filePatternEpisodes) Then
            'Folgen und ihre Dateien sammeln und nach Staffel und Folge gruppieren
            Dim episodeInfos As New List(Of EpisodeFileInfo)
            Dim episodeGroups As New Dictionary(Of String, List(Of EpisodeFileInfo))

            For Each tEpisode As Database.DBElement In _tmpShow.Episodes.Where(Function(f) f.IsOnline)
                Dim episodeInfo As New EpisodeFileInfo With {
                    .Element = tEpisode,
                    .File = GetInfo_TVEpisode(tEpisode, _tmpShow.Episodes),
                    .GroupKey = String.Concat(tEpisode.TVEpisode.Season, "_", tEpisode.TVEpisode.Episode)
                }
                episodeInfos.Add(episodeInfo)
                If Not episodeGroups.ContainsKey(episodeInfo.GroupKey) Then episodeGroups.Add(episodeInfo.GroupKey, New List(Of EpisodeFileInfo))
                episodeGroups(episodeInfo.GroupKey).Add(episodeInfo)
            Next

            'Frage einmal pro Serie stellen, wenn mehrere Dateien zu derselben Folge gehoeren
            Dim multiFileMode As String = GetTVShowMultiFileMode(_tmpShow, episodeGroups)

            For Each episodeInfo As EpisodeFileInfo In episodeInfos
                Dim EpisodeFile As FileRename = episodeInfo.File
                Dim tEpisode As Database.DBElement = episodeInfo.Element

                If Not String.IsNullOrEmpty(multiFileMode) Then
                    SetMultiFileInfo_Episode(episodeInfo, episodeGroups, multiFileMode)
                End If

                Process_TVEpisode(EpisodeFile, folderPatternSeasons, filePatternEpisodes)
                If EpisodeFile.DoRename Then
                    DoRenameSingle_TVEpisode(EpisodeFile, tEpisode, BatchMode, ShowError, False)

                    'refresh Filename of Multi-Episode files
                    Dim lstEpisodes = _tmpShow.Episodes.Where(Function(f) f.FilenameID = tEpisode.FilenameID AndAlso Not f.ID = tEpisode.ID)
                    For Each nEpisode In lstEpisodes
                        nEpisode.Filename = tEpisode.Filename
                    Next
                End If
            Next
        End If

        If ShowFile.DoRename AndAlso Not ShowFile.IsSingle Then
            'rename tvshow
            DoRenameSingle_TVShow(ShowFile, _tmpShow, BatchMode, ShowError, toDB)
        Else
            If toDB Then
                Master.DB.Save_TVShow(_tmpShow, BatchMode, False, False, True)
            End If
            If ShowFile.DoRename AndAlso ShowFile.IsSingle Then
                If ShowError Then
                    MessageBox.Show(Master.eLang.GetString(1086, "The directory can not be renamed because the directory is specified as the source."), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
                logger.Error(String.Format("[{0}] [{1}] [Partially Done] {2}",
                                           Reflection.MethodBase.GetCurrentMethod.ReflectedType,
                                           Reflection.MethodBase.GetCurrentMethod.Name,
                                           "Can't rename a directory that has been set as source"))
            End If
        End If
    End Sub

    Public Shared Function SelectMPAA(ByVal strMPAA As String) As String
        If Not String.IsNullOrEmpty(strMPAA) Then
            If strMPAA.ToLower.StartsWith("rated g") Then
                Return "G"
            ElseIf strMPAA.ToLower.StartsWith("rated pg-13") Then
                Return "PG-13"
            ElseIf strMPAA.ToLower.StartsWith("rated pg") Then
                Return "PG"
            ElseIf strMPAA.ToLower.StartsWith("rated r") Then
                Return "R"
            ElseIf strMPAA.ToLower.StartsWith("rated nc-17") Then
                Return "NC-17"
            ElseIf strMPAA.Contains(":") Then 'might be a certification
                Dim strSplit As String = strMPAA.Split(Convert.ToChar(":")).Last
                Return Regex.Replace(strSplit, "rated", String.Empty, RegexOptions.IgnoreCase).Trim
            Else
                Return strMPAA
            End If
        Else
            Return String.Empty
        End If
    End Function

    Public Sub SetIsLocked_Movies(ByVal path As String, ByVal filename As String, ByVal lock As Boolean)
        For Each f As FileRename In _movies
            If (f.Path = path AndAlso f.OldFileName = filename) OrElse filename = String.Empty Then f.IsLock = lock
        Next
    End Sub

    Public Sub SetIsLocked_TVEpisodes(ByVal path As String, ByVal filename As String, ByVal lock As Boolean)
        For Each f As FileRename In _episodes
            If (f.Path = path AndAlso f.OldFileName = filename) OrElse filename = String.Empty Then f.IsLock = lock
        Next
    End Sub

    Private Shared Sub UpdatePaths_Movie(ByRef _DBM As Database.DBElement, ByVal oldPath As String, ByVal newPath As String, ByVal oldFile As String, ByVal newFile As String, Optional ByVal sidecarFile As String = "")
        If String.IsNullOrEmpty(sidecarFile) Then sidecarFile = newFile
        If _DBM.ExtrafanartsPathSpecified Then _DBM.ExtrafanartsPath = Path.Combine(Directory.GetParent(_DBM.ExtrafanartsPath).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBM.ExtrafanartsPath).Replace(oldFile, newFile))
        If _DBM.ExtrathumbsPathSpecified Then _DBM.ExtrathumbsPath = Path.Combine(Directory.GetParent(_DBM.ExtrathumbsPath).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBM.ExtrathumbsPath).Replace(oldFile, newFile))
        If _DBM.FilenameSpecified Then _DBM.Filename = Path.Combine(Directory.GetParent(_DBM.Filename).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBM.Filename).Replace(oldFile, newFile))
        If _DBM.ImagesContainer.Banner.LocalFilePathSpecified Then _DBM.ImagesContainer.Banner.LocalFilePath = Path.Combine(Directory.GetParent(_DBM.ImagesContainer.Banner.LocalFilePath).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBM.ImagesContainer.Banner.LocalFilePath).Replace(oldFile, sidecarFile))
        If _DBM.ImagesContainer.ClearArt.LocalFilePathSpecified Then _DBM.ImagesContainer.ClearArt.LocalFilePath = Path.Combine(Directory.GetParent(_DBM.ImagesContainer.ClearArt.LocalFilePath).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBM.ImagesContainer.ClearArt.LocalFilePath).Replace(oldFile, sidecarFile))
        If _DBM.ImagesContainer.ClearLogo.LocalFilePathSpecified Then _DBM.ImagesContainer.ClearLogo.LocalFilePath = Path.Combine(Directory.GetParent(_DBM.ImagesContainer.ClearLogo.LocalFilePath).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBM.ImagesContainer.ClearLogo.LocalFilePath).Replace(oldFile, sidecarFile))
        If _DBM.ImagesContainer.DiscArt.LocalFilePathSpecified Then _DBM.ImagesContainer.DiscArt.LocalFilePath = Path.Combine(Directory.GetParent(_DBM.ImagesContainer.DiscArt.LocalFilePath).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBM.ImagesContainer.DiscArt.LocalFilePath).Replace(oldFile, sidecarFile))
        If _DBM.ImagesContainer.Fanart.LocalFilePathSpecified Then _DBM.ImagesContainer.Fanart.LocalFilePath = Path.Combine(Directory.GetParent(_DBM.ImagesContainer.Fanart.LocalFilePath).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBM.ImagesContainer.Fanart.LocalFilePath).Replace(oldFile, sidecarFile))
        If _DBM.ImagesContainer.Landscape.LocalFilePathSpecified Then _DBM.ImagesContainer.Landscape.LocalFilePath = Path.Combine(Directory.GetParent(_DBM.ImagesContainer.Landscape.LocalFilePath).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBM.ImagesContainer.Landscape.LocalFilePath).Replace(oldFile, sidecarFile))
        If _DBM.ImagesContainer.Poster.LocalFilePathSpecified Then _DBM.ImagesContainer.Poster.LocalFilePath = Path.Combine(Directory.GetParent(_DBM.ImagesContainer.Poster.LocalFilePath).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBM.ImagesContainer.Poster.LocalFilePath).Replace(oldFile, sidecarFile))
        If _DBM.NfoPathSpecified Then _DBM.NfoPath = Path.Combine(Directory.GetParent(_DBM.NfoPath).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBM.NfoPath).Replace(oldFile, newFile))
        If Not String.IsNullOrEmpty(_DBM.Theme.LocalFilePath) Then _DBM.Theme.LocalFilePath = Path.Combine(Directory.GetParent(_DBM.Theme.LocalFilePath).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBM.Theme.LocalFilePath).Replace(oldFile, sidecarFile))
        If Not String.IsNullOrEmpty(_DBM.Trailer.LocalFilePath) Then _DBM.Trailer.LocalFilePath = Path.Combine(Directory.GetParent(_DBM.Trailer.LocalFilePath).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBM.Trailer.LocalFilePath).Replace(oldFile, sidecarFile))
        If _DBM.ImagesContainer.Extrafanarts.Count > 0 Then
            For Each eImg In _DBM.ImagesContainer.Extrafanarts.Where(Function(f) f.LocalFilePathSpecified)
                eImg.LocalFilePath = Path.Combine(Directory.GetParent(eImg.LocalFilePath).FullName.Replace(oldPath, newPath), Path.GetFileName(eImg.LocalFilePath).Replace(oldFile, sidecarFile))
            Next
        End If
        If _DBM.ImagesContainer.Extrathumbs.Count > 0 Then
            For Each eImg In _DBM.ImagesContainer.Extrathumbs.Where(Function(f) f.LocalFilePathSpecified)
                eImg.LocalFilePath = Path.Combine(Directory.GetParent(eImg.LocalFilePath).FullName.Replace(oldPath, newPath), Path.GetFileName(eImg.LocalFilePath).Replace(oldFile, sidecarFile))
            Next
        End If
        If _DBM.SubtitlesSpecified Then
            For Each subtitle In _DBM.Subtitles.Where(Function(f) f.PathSpecified)
                subtitle.Path = Path.Combine(Directory.GetParent(subtitle.Path).FullName.Replace(oldPath, newPath), Path.GetFileName(subtitle.Path).Replace(oldFile, newFile))
            Next
        End If
    End Sub

    Private Shared Sub UpdatePaths_TVEpisode(ByRef _DBE As Database.DBElement, ByVal oldPath As String, ByVal newPath As String, ByVal oldFile As String, ByVal newFile As String)
        If _DBE.FilenameSpecified Then _DBE.Filename = Path.Combine(Directory.GetParent(_DBE.Filename).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBE.Filename).Replace(oldFile, newFile))
        If _DBE.ImagesContainer.Fanart.LocalFilePathSpecified Then _DBE.ImagesContainer.Fanart.LocalFilePath = Path.Combine(Directory.GetParent(_DBE.ImagesContainer.Fanart.LocalFilePath).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBE.ImagesContainer.Fanart.LocalFilePath).Replace(oldFile, newFile))
        If _DBE.ImagesContainer.Poster.LocalFilePathSpecified Then _DBE.ImagesContainer.Poster.LocalFilePath = Path.Combine(Directory.GetParent(_DBE.ImagesContainer.Poster.LocalFilePath).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBE.ImagesContainer.Poster.LocalFilePath).Replace(oldFile, newFile))
        If _DBE.NfoPathSpecified Then _DBE.NfoPath = Path.Combine(Directory.GetParent(_DBE.NfoPath).FullName.Replace(oldPath, newPath), Path.GetFileName(_DBE.NfoPath).Replace(oldFile, newFile))
        If _DBE.SubtitlesSpecified Then
            For Each subtitle In _DBE.Subtitles.Where(Function(f) f.PathSpecified)
                subtitle.Path = Path.Combine(Directory.GetParent(subtitle.Path).FullName.Replace(oldPath, newPath), Path.GetFileName(subtitle.Path).Replace(oldFile, newFile))
            Next
        End If
    End Sub

    Private Shared Sub UpdatePaths_TVShow(ByRef _DBE As Database.DBElement, ByVal oldPath As String, ByVal newPath As String)
        If _DBE.ExtrafanartsPathSpecified Then _DBE.ExtrafanartsPath = _DBE.ExtrafanartsPath.Replace(oldPath, newPath)
        If _DBE.FilenameSpecified Then _DBE.Filename = _DBE.Filename.Replace(oldPath, newPath)
        If _DBE.ImagesContainer.Banner.LocalFilePathSpecified Then _DBE.ImagesContainer.Banner.LocalFilePath = _DBE.ImagesContainer.Banner.LocalFilePath.Replace(oldPath, newPath)
        If _DBE.ImagesContainer.CharacterArt.LocalFilePathSpecified Then _DBE.ImagesContainer.CharacterArt.LocalFilePath = _DBE.ImagesContainer.CharacterArt.LocalFilePath.Replace(oldPath, newPath)
        If _DBE.ImagesContainer.ClearArt.LocalFilePathSpecified Then _DBE.ImagesContainer.ClearArt.LocalFilePath = _DBE.ImagesContainer.ClearArt.LocalFilePath.Replace(oldPath, newPath)
        If _DBE.ImagesContainer.ClearLogo.LocalFilePathSpecified Then _DBE.ImagesContainer.ClearLogo.LocalFilePath = _DBE.ImagesContainer.ClearLogo.LocalFilePath.Replace(oldPath, newPath)
        If _DBE.ImagesContainer.Fanart.LocalFilePathSpecified Then _DBE.ImagesContainer.Fanart.LocalFilePath = _DBE.ImagesContainer.Fanart.LocalFilePath.Replace(oldPath, newPath)
        If _DBE.ImagesContainer.Landscape.LocalFilePathSpecified Then _DBE.ImagesContainer.Landscape.LocalFilePath = _DBE.ImagesContainer.Landscape.LocalFilePath.Replace(oldPath, newPath)
        If _DBE.ImagesContainer.Poster.LocalFilePathSpecified Then _DBE.ImagesContainer.Poster.LocalFilePath = _DBE.ImagesContainer.Poster.LocalFilePath.Replace(oldPath, newPath)
        If _DBE.NfoPathSpecified Then _DBE.NfoPath = _DBE.NfoPath.Replace(oldPath, newPath)
        If _DBE.ShowPathSpecified Then _DBE.ShowPath = _DBE.ShowPath.Replace(oldPath, newPath)
        If Not String.IsNullOrEmpty(_DBE.Theme.LocalFilePath) Then _DBE.Theme.LocalFilePath = _DBE.Theme.LocalFilePath.Replace(oldPath, newPath)
        If _DBE.SubtitlesSpecified Then
            For Each subtitle In _DBE.Subtitles.Where(Function(f) f.PathSpecified)
                subtitle.Path = subtitle.Path.Replace(oldPath, newPath)
            Next
        End If
    End Sub

#End Region 'Methods

#Region "Multi-File handling"

    'Moegliche Behandlungen von Mehrfach-Dateien
    Private Const MULTIFILE_MODE_PARTS As String = "Parts"
    Private Const MULTIFILE_MODE_VERSIONS As String = "Versions"

    'Zustand, wenn der Nutzer die Frage abgebrochen hat
    Private Const MULTIFILE_STATE_CANCELLED As String = "Cancelled"

    'Praefix und Section der AdvancedSettings, in denen die Antworten abgelegt werden
    Private Const MULTIFILE_PREFIX_TVSHOW As String = "MultiFileMode_TVShow"
    Private Const MULTIFILE_PREFIX_MOVIE As String = "MultiFileMode_Movie"
    Private Const MULTIFILE_SECTION As String = "generic.EmberCore.BulkRename"

    ''' <summary>
    ''' Baut den Schluessel fuer die Antwort eines Elements (Serie oder Film).
    ''' </summary>
    Private Shared Function MultiFileKey(ByVal prefix As String, ByVal id As Long) As String
        Return String.Concat(prefix, "_", id.ToString)
    End Function

    ''' <summary>
    ''' Liefert den bekannten Stand der Antwort, ohne den Nutzer zu fragen.
    ''' </summary>
    ''' <remarks>Zuerst wird der aktuelle Lauf nachgeschlagen, danach die
    ''' AdvancedSettings. Ein Abbruch wird als "Cancelled" gemeldet und nicht
    ''' dauerhaft gespeichert.</remarks>
    Private Shared Function GetMultiFileModeState(ByVal key As String, ByVal contentType As Enums.ContentType) As String
        If _multiFileModes.ContainsKey(key) Then
            Dim cached As String = _multiFileModes(key)
            If String.IsNullOrEmpty(cached) Then Return MULTIFILE_STATE_CANCELLED
            Return cached
        End If

        Dim stored As String = AdvancedSettings.GetSetting(key, String.Empty, MULTIFILE_SECTION, contentType)
        If stored = MULTIFILE_MODE_PARTS OrElse stored = MULTIFILE_MODE_VERSIONS Then
            _multiFileModes(key) = stored
            Return stored
        End If

        Return String.Empty
    End Function

    ''' <summary>
    ''' Liefert die bereits bekannte Antwort, ohne den Nutzer zu fragen.
    ''' </summary>
    Private Shared Function GetKnownMultiFileMode(ByVal key As String, ByVal contentType As Enums.ContentType) As String
        Dim state As String = GetMultiFileModeState(key, contentType)
        If state = MULTIFILE_STATE_CANCELLED Then Return String.Empty
        Return state
    End Function

    ''' <summary>
    ''' Holt die Antwort des Nutzers und legt sie dauerhaft ab.
    ''' </summary>
    ''' <param name="key">Schluessel fuer die dauerhafte Ablage</param>
    ''' <param name="displayTitle">Titel des Dialogs (Film- oder Serienname)</param>
    ''' <param name="contentType">Inhaltstyp</param>
    ''' <param name="affectedItems">Betroffene Folgen oder Filme fuer die Anzeige im Dialog</param>
    ''' <returns>"Parts", "Versions" oder leer, wenn der Nutzer abgebrochen hat</returns>
    Private Shared Function GetMultiFileMode(ByVal key As String, ByVal displayTitle As String, ByVal contentType As Enums.ContentType, Optional ByVal affectedItems As IList(Of String) = Nothing) As String
        If _multiFileModes.ContainsKey(key) Then Return _multiFileModes(key)

        Dim known As String = GetKnownMultiFileMode(key, contentType)
        If Not String.IsNullOrEmpty(known) Then Return known

        'Der Dialog wird auch im Renamer-Thread (BackgroundWorker) aufgerufen.
        Using dlg As New dlgMultiFileQuestion(displayTitle, affectedItems, contentType = Enums.ContentType.TVShow)
            Dim mode As String = String.Empty
            If dlg.ShowDialog() = DialogResult.OK Then
                mode = If(dlg.IsParts, MULTIFILE_MODE_PARTS, MULTIFILE_MODE_VERSIONS)
            End If

            _multiFileModes(key) = mode
            If Not String.IsNullOrEmpty(mode) Then SaveMultiFileMode(key, mode, contentType)
            Return mode
        End Using
    End Function

    ''' <summary>
    ''' Setzt die Antwort fuer ein Element und speichert sie dauerhaft.
    ''' </summary>
    Private Shared Sub SetMultiFileMode(ByVal key As String, ByVal mode As String, ByVal contentType As Enums.ContentType)
        _multiFileModes(key) = mode
        If Not String.IsNullOrEmpty(mode) Then SaveMultiFileMode(key, mode, contentType)
    End Sub

    ''' <summary>
    ''' Schreibt die Antwort in die AdvancedSettings.
    ''' </summary>
    Private Shared Sub SaveMultiFileMode(ByVal key As String, ByVal mode As String, ByVal contentType As Enums.ContentType)
        Try
            Using settings As New AdvancedSettings()
                settings.SetSetting(key, mode, MULTIFILE_SECTION, False, contentType)
                'SetSetting aendert nur den Speicher - ohne diesen Aufruf ist die
                'Antwort beim naechsten Start wieder vergessen.
                settings.SaveSettings()
            End Using
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

    ''' <summary>
    ''' Liefert das Suffix fuer Teile oder Versionen.
    ''' </summary>
    Private Shared Function GetMultiFileSuffix(ByVal mode As String, ByVal partNumber As Integer) As String
        If String.IsNullOrEmpty(mode) OrElse partNumber < 1 Then Return String.Empty

        If mode = MULTIFILE_MODE_PARTS Then
            'Teile eines Ganzen: auch die erste Datei bekommt "-part-1"
            Return String.Concat("-part-", partNumber.ToString)
        ElseIf mode = MULTIFILE_MODE_VERSIONS Then
            'Versionen: die erste Datei behaelt den Basisnamen
            If partNumber < 2 Then Return String.Empty
            Return String.Concat(" - ", partNumber.ToString)
        End If

        Return String.Empty
    End Function

    ''' <summary>
    ''' Haengt das Suffix an den Dateinamen an.
    ''' </summary>
    ''' <remarks>Bei Filmen traegt auch der Name der Begleitdateien (NFO, Poster, ...)
    ''' das Suffix, sonst passen Video und NFO nicht mehr zusammen.</remarks>
    Private Shared Sub ApplyMultiFileSuffix(ByRef f As FileRename)
        If f Is Nothing Then Exit Sub
        If String.IsNullOrEmpty(f.MultiFileMode) Then Exit Sub

        Dim suffix As String = GetMultiFileSuffix(f.MultiFileMode, f.PartNumber)
        If String.IsNullOrEmpty(suffix) Then Exit Sub

        f.NewFileName = String.Concat(f.NewFileName, suffix)
        If Not String.IsNullOrEmpty(f.NewSidecarName) Then
            f.NewSidecarName = String.Concat(f.NewSidecarName, suffix)
        End If
    End Sub

    ''' <summary>
    ''' Unterdrueckt den Unterepisoden-Anteil in den $W- und $Q-Tokens.
    ''' </summary>
    ''' <remarks>Ohne diese Unterdrueckung entstuende aus "S01E01A" der Name
    ''' "S01E01.1" und zusaetzlich das Suffix fuer die Teile.</remarks>
    Private Shared Sub SuppressSubEpisode(ByRef f As FileRename)
        For Each season As SeasonsEpisodes In f.SeasonsEpisodes
            For Each episode As Episode In season.Episodes
                episode.SubEpisode = -1
            Next
        Next
    End Sub

    ''' <summary>
    ''' Berechnet die Existenz der Zieldatei und das Umbenennungs-Flag neu.
    ''' </summary>
    Private Shared Sub UpdateMovieRenameState(ByRef f As FileRename)
        Dim newFullFileName As String = Path.Combine(f.BasePath, Path.Combine(f.NewPath, String.Concat(f.NewFileName, f.Extension)))
        Dim newFullDirPath As String = Path.Combine(f.BasePath, f.NewPath)
        Dim newDirInfo As New DirectoryInfo(newFullDirPath)
        f.FileExist = File.Exists(newFullFileName) AndAlso Not (newFullFileName.ToLower = f.OldFullFileName.ToLower)
        f.DirExist = f.IsSingle AndAlso newDirInfo.Exists AndAlso Not If(newFullDirPath.ToLower = f.OldFullPath.ToLower, True, newDirInfo.GetFileSystemInfos.Count = 0)
        f.DoRename = Not f.NewPath = f.Path OrElse
                     Not f.NewFileName = f.OldFileName OrElse
                     (Not String.IsNullOrEmpty(f.NewSidecarName) AndAlso Not f.NewSidecarName = f.OldFileName)
    End Sub

    ''' <summary>
    ''' Ermittelt die Nummer der Datei innerhalb ihrer Gruppe.
    ''' </summary>
    ''' <remarks>Im Einzelpfad (Umbenennen eines einzelnen Films) wird der Zielordner
    ''' nach bereits vorhandenen Teilen bzw. Versionen durchsucht. Eine Datei ohne
    ''' Suffix zaehlt dabei als Teil 1 bzw. Version 1.</remarks>
    Private Shared Function GetMultiFilePartNumber(ByVal f As FileRename, ByVal mode As String) As Integer
        Dim number As Integer = 1

        Try
            Dim destDir As String = Path.Combine(f.BasePath, f.NewPath)
            If Not Directory.Exists(destDir) Then Return number

            For Each file As FileInfo In New DirectoryInfo(destDir).GetFiles()
                Dim name As String = Path.GetFileNameWithoutExtension(file.Name)

                If mode = MULTIFILE_MODE_PARTS Then
                    Dim partMatch As Match = Regex.Match(name, String.Concat(Regex.Escape(f.NewFileName), "-part-(\d+)$"), RegexOptions.IgnoreCase)
                    If partMatch.Success Then
                        Dim partNumber As Integer = 0
                        If Integer.TryParse(partMatch.Groups(1).Value, partNumber) Then number = Math.Max(number, partNumber + 1)
                    ElseIf name.Equals(f.NewFileName, StringComparison.OrdinalIgnoreCase) Then
                        'Die vorhandene Datei ohne Suffix ist Teil 1
                        number = Math.Max(number, 2)
                    End If
                Else
                    If name.Equals(f.NewFileName, StringComparison.OrdinalIgnoreCase) Then
                        'Die vorhandene Datei ohne Suffix ist Version 1
                        number = Math.Max(number, 2)
                    Else
                        Dim versionMatch As Match = Regex.Match(name, String.Concat(Regex.Escape(f.NewFileName), " - (\d+)$"), RegexOptions.IgnoreCase)
                        If versionMatch.Success Then
                            Dim versionNumber As Integer = 0
                            If Integer.TryParse(versionMatch.Groups(1).Value, versionNumber) Then number = Math.Max(number, versionNumber + 1)
                        End If
                    End If
                End If
            Next
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try

        Return number
    End Function

    ''' <summary>
    ''' Prueft, ob zu einer Folge der Serie mehrere Dateien gehoeren.
    ''' </summary>
    ''' <returns>Der Modus ("Parts" oder "Versions") oder leer, wenn es keine
    ''' Mehrfach-Datei-Situation gibt oder der Nutzer abgebrochen hat</returns>
    Private Shared Function GetTVShowMultiFileMode(ByVal _tmpShow As Database.DBElement, ByVal episodeGroups As Dictionary(Of String, List(Of EpisodeFileInfo))) As String
        Try
            'Gehoeren mehrere Dateien zu derselben Folge?
            Dim hasMultipleFiles As Boolean = episodeGroups.Values.Any(Function(g) g.Select(Function(i) i.File.OldFileName).Distinct(StringComparer.OrdinalIgnoreCase).Count > 1)
            If Not hasMultipleFiles Then Return String.Empty

            Dim showTitle As String = String.Empty
            If _tmpShow.TVShow IsNot Nothing AndAlso _tmpShow.TVShow.TitleSpecified Then showTitle = _tmpShow.TVShow.Title

            'Die betroffenen Folgen auflisten, damit der Nutzer sieht, worum es geht
            Dim affected As New List(Of String)
            For Each group In episodeGroups.Values.Where(Function(g) g.Select(Function(i) i.File.OldFileName).Distinct(StringComparer.OrdinalIgnoreCase).Count > 1)
                Dim first = group.FirstOrDefault()
                If first IsNot Nothing AndAlso first.Element IsNot Nothing AndAlso first.Element.TVEpisode IsNot Nothing Then
                    Dim ep = first.Element.TVEpisode
                    Dim label As String = String.Format("S{0:00}E{1:00}", ep.Season, ep.Episode)
                    If ep.TitleSpecified AndAlso Not String.IsNullOrEmpty(ep.Title) Then
                        label = String.Concat(label, " - ", ep.Title)
                    End If
                    If Not affected.Contains(label) Then affected.Add(label)
                End If
            Next
            affected.Sort()

            Return GetMultiFileMode(MultiFileKey(MULTIFILE_PREFIX_TVSHOW, _tmpShow.ShowID), showTitle, Enums.ContentType.TVShow, affected)
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
            Return String.Empty
        End Try
    End Function

    ''' <summary>
    ''' Setzt Modus und Nummer der Datei innerhalb ihrer Gruppe.
    ''' </summary>
    Private Shared Sub SetMultiFileInfo_Episode(ByRef info As EpisodeFileInfo, ByVal episodeGroups As Dictionary(Of String, List(Of EpisodeFileInfo)), ByVal mode As String)
        If Not episodeGroups.ContainsKey(info.GroupKey) Then Exit Sub

        Dim group As List(Of EpisodeFileInfo) = episodeGroups(info.GroupKey)

        'Nur Folgen, zu denen mehrere Dateien gehoeren, bekommen einen Suffix.
        'Eine einzelne Datei pro Folge bleibt ohne "-part-1" o.ae.
        If group.Select(Function(i) i.File.OldFileName).Distinct(StringComparer.OrdinalIgnoreCase).Count < 2 Then Exit Sub

        Dim number As Integer = 0
        Dim lastFileName As String = String.Empty

        For Each item As EpisodeFileInfo In group.OrderBy(Function(i) i.File.OldFileName, StringComparer.OrdinalIgnoreCase)
            'Dateien, die mehrfach in der Gruppe stehen, bekommen dieselbe Nummer
            If Not item.File.OldFileName.Equals(lastFileName, StringComparison.OrdinalIgnoreCase) Then
                number += 1
                lastFileName = item.File.OldFileName
            End If

            If item.File.ID = info.File.ID Then
                info.File.MultiFileMode = mode
                info.File.PartNumber = number
                Exit For
            End If
        Next
    End Sub

    ''' <summary>
    ''' Sucht Filme, die denselben Zielnamen bekommen, und benennt sie um.
    ''' </summary>
    Private Sub HandleMultiFile_Movies()
        Try
            Dim groups As New Dictionary(Of String, List(Of FileRename))

            For Each f As FileRename In _movies
                Dim groupKey As String = String.Concat(f.NewPath.ToLower, "|", f.NewFileName.ToLower)
                If Not groups.ContainsKey(groupKey) Then groups.Add(groupKey, New List(Of FileRename))
                groups(groupKey).Add(f)
            Next

            For Each group As List(Of FileRename) In groups.Values.Where(Function(g) g.Count > 1)
                Dim ordered As List(Of FileRename) = group.OrderBy(Function(x) x.OldFileName, StringComparer.OrdinalIgnoreCase).ToList()

                'Antwort der Gruppe ermitteln: bekannte Antwort oder eine Frage
                Dim mode As String = String.Empty
                Dim cancelled As Boolean = False

                For Each f As FileRename In ordered
                    Dim state As String = GetMultiFileModeState(MultiFileKey(MULTIFILE_PREFIX_MOVIE, f.ID), Enums.ContentType.Movie)
                    If state = MULTIFILE_STATE_CANCELLED Then
                        cancelled = True
                        Exit For
                    ElseIf Not String.IsNullOrEmpty(state) Then
                        mode = state
                    End If
                Next

                If Not cancelled AndAlso String.IsNullOrEmpty(mode) Then
                    Dim affected As New List(Of String)
                    If Not String.IsNullOrEmpty(ordered.First.Title) Then affected.Add(ordered.First.Title)
                    mode = GetMultiFileMode(MultiFileKey(MULTIFILE_PREFIX_MOVIE, ordered.First.ID), ordered.First.Title, Enums.ContentType.Movie, affected)
                End If

                'Nutzer hat abgebrochen: fuer diese Filme wird nichts umbenannt
                If cancelled OrElse String.IsNullOrEmpty(mode) Then
                    For Each f As FileRename In ordered
                        Dim key As String = MultiFileKey(MULTIFILE_PREFIX_MOVIE, f.ID)
                        If String.IsNullOrEmpty(GetMultiFileModeState(key, Enums.ContentType.Movie)) Then
                            'Als abgebrochen merken, damit in diesem Lauf nicht erneut gefragt wird
                            _multiFileModes(key) = String.Empty
                        End If
                    Next
                    Continue For
                End If

                'Die Antwort gilt fuer alle Filme der Gruppe
                For Each f As FileRename In ordered
                    SetMultiFileMode(MultiFileKey(MULTIFILE_PREFIX_MOVIE, f.ID), mode, Enums.ContentType.Movie)
                Next

                Dim number As Integer = 0
                Dim lastFileName As String = String.Empty
                For Each f As FileRename In ordered
                    If Not f.OldFileName.Equals(lastFileName, StringComparison.OrdinalIgnoreCase) Then
                        number += 1
                        lastFileName = f.OldFileName
                    End If

                    f.MultiFileMode = mode
                    f.PartNumber = number
                    ApplyMultiFileSuffix(f)
                    UpdateMovieRenameState(f)
                Next
            Next
        Catch ex As Exception
            logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try
    End Sub

#End Region 'Multi-File handling

#Region "Nested Types"

    ''' <summary>
    ''' Zuordnung einer Episode zu ihrer Datei fuer die Mehrfach-Datei-Erkennung.
    ''' </summary>
    Private Class EpisodeFileInfo

#Region "Properties"

        Public Property Element() As Database.DBElement = Nothing

        Public Property File() As FileRename = Nothing

        Public Property GroupKey() As String = String.Empty

#End Region 'Properties

    End Class

    Class FileRename

#Region "Properties"

        Public Property Aired() As String = String.Empty

        Public Property AudioChannels() As String = String.Empty

        Public Property AudioCodec() As String = String.Empty

        Public Property BasePath() As String = String.Empty

        Public Property Collection() As String = String.Empty

        Public Property CollectionListTitle() As String = String.Empty

        Public Property Directors() As List(Of String) = New List(Of String)

        Public Property DirExist() As Boolean = False

        Public Property DoRename() As Boolean = False

        Public Property Edition() As String = String.Empty

        Public Property Extension() As String = String.Empty

        Public Property FileExist() As Boolean = False

        Public Property Genres() As List(Of String) = New List(Of String)

        Public Property ID() As Long = -1

        Public Property IsBDMV() As Boolean = False

        Public Property IsLock() As Boolean = False

        Public Property IsMultiEpisode() As Boolean = False

        Public Property IsSingle() As Boolean = False

        Public Property IsVideoTS() As Boolean = False

        Public Property ListTitle() As String = String.Empty

        Public Property MPAA() As String = String.Empty

        'Parts oder Versions bei mehreren Dateien zu einem Titel
        Public Property MultiFileMode() As String = String.Empty

        Public Property MultiViewCount() As String = String.Empty

        Public Property MultiViewLayout() As String = String.Empty

        Public Property NewFileName() As String = String.Empty

        Public Property NewPath() As String = String.Empty

        Public Property NewSidecarName() As String = String.Empty

        Public Property OldFileName() As String = String.Empty

        Public Property OldFullFileName() As String = String.Empty

        Public Property OldFullPath() As String = String.Empty

        Public Property OldPath() As String = String.Empty

        Public Property OriginalTitle() As String = String.Empty

        'Nummer der Datei innerhalb ihrer Gruppe (1-basiert)
        Public Property PartNumber() As Integer = 0

        Public Property Parent() As String = String.Empty

        Public Property Path() As String = String.Empty

        Public Property Rating() As String = String.Empty

        Public Property Resolution() As String = String.Empty

        Public Property Countries() As List(Of String) = New List(Of String)

        Public Property Title() As String = String.Empty

        Public Property TVDBID() As String = String.Empty

        Public Property SeasonsEpisodes() As List(Of SeasonsEpisodes) = New List(Of SeasonsEpisodes)

        Public Property ShortStereoMode() As String = String.Empty

        Public Property ShowFullPath() As String = String.Empty

        Public Property ShowPath() As String = String.Empty

        Public Property ShowTitle() As String = String.Empty

        Public Property SortTitle() As String = String.Empty

        Public Property Status() As String = String.Empty

        Public Property StereoMode() As String = String.Empty

        Public Property VideoCodec() As String = String.Empty

        Public Property Year() As String = String.Empty

        Public Property IMDB() As String = String.Empty

        Public Property VideoSource() As String = String.Empty

#End Region 'Properties 

    End Class

    Class SeasonsEpisodes

#Region "Properties"

        Public Property Season() As Integer = -1

        Public Property Episodes() As List(Of Episode) = New List(Of Episode)

#End Region 'Properties 

    End Class

    Class Episode
        Implements IComparable(Of Episode)

#Region "Properties"

        Public Property ID() As Integer = -1

        Public Property Episode() As Integer = -1

        Public Property SubEpisode() As Integer = -1

        Public Property Title() As String = String.Empty

#End Region 'Properties

#Region "Methods"

        Public Function CompareTo(ByVal obj As Episode) As Integer Implements IComparable(Of Episode).CompareTo
            Return Episode.CompareTo(obj.Episode)
        End Function

#End Region 'Methods

    End Class

#End Region 'Nested Types

End Class