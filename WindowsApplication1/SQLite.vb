Imports System.Data.SQLite

Public Class SQLite

    Private db As SQLiteConnection

    Public Function insertSong(ByVal title As String, ByVal sng As song, ByVal dup As Boolean) As Integer
        Dim directory As String = "C:\Users\Public\Documents\Softouch\Easyworship\Default\v6.1\Databases\Data"
        Dim song_id As String = ""
        Dim hash As String = GenerateRandomString(40)
        If (IO.Directory.Exists(directory) = False) Then
            Using fbd As New FolderBrowserDialog()
                If (fbd.ShowDialog = DialogResult.OK) Then
                    directory = fbd.SelectedPath()
                End If
            End Using
        End If

        db = New SQLiteConnection("Data Source=" & directory & "\Songs.db")

        Dim cmd As New SQLiteCommand(db)
        Try
            db.Open()
            cmd.CommandText = "SELECT * FROM song WHERE title = '" & title & "' AND author = '" & sng.author & "';"
            If cmd.ExecuteScalar() = Nothing Then
                cmd.CommandText = "INSERT INTO song (song_item_uid, song_uid, title, author, copyright, vendor_id, presentation_id, layout_revision, revision) VALUES ('PCI-" & hash & "','PCI-" & hash & "','" & title & "','" & sng.author & "',""" & sng.copyright & """,'0','0','1','1');"
                cmd.ExecuteNonQuery()
                cmd.CommandText = "SELECT last_insert_rowid();"
                song_id = cmd.ExecuteScalar.ToString()
            Else
                MessageBox.Show("Song with title """ & title & """ already exists. Skipping.")
            End If
            db.Close()
        Catch ex As Exception
            MessageBox.Show(ex.Message)
            Return 1
        End Try

        Dim rtf As New RichTextBox()
        rtf.Text = sng.buildLyrics(dup)
        db = New SQLiteConnection("Data Source=" & directory & "\SongWords.db")
        cmd = New SQLiteCommand(db)
        Try
            db.Open()
            cmd.CommandText = "INSERT INTO word (song_id, words) VALUES ('" & song_id & "',""" & rtf.Rtf & """);"
            cmd.ExecuteNonQuery()
            db.Close()
        Catch ex As Exception
            MessageBox.Show(ex.Message)
            Return 1
        End Try

        db.Dispose()
        cmd.Dispose()
        rtf.Dispose()
        Return 0
    End Function

    Function GenerateRandomString(ByRef lenStr As Integer, Optional ByVal upper As Boolean = False) As String
        Dim rand As New Random()
        Dim allowableChars() As Char =
                "abcdef0123456789".ToCharArray()
        Dim final As New System.Text.StringBuilder
        Do
            final.Append(allowableChars(rand.Next(0, allowableChars.Length)))
        Loop Until final.Length = lenStr
        Return If(upper, final.ToString.ToUpper(), final.ToString)
    End Function

End Class