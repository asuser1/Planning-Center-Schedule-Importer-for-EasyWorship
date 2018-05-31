Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Text.RegularExpressions

Public Class Form1
    'Constants
    '-----------------------------------------------------------------------------------------------------------------------
    'Personal access token
    Const appId As String = "****"
    Const secret As String = "****"

    'Planning Center API - This is a Godsend
    Const urlPlans As String = "https://api.planningcenteronline.com/services/v2/service_types/411816/plans?filter=future"
    'Const urlPlansPast As String = "https://api.planningcenteronline.com/services/v2/service_types/411816/plans?per_page=5"
    Const getPlan As String = "https://api.planningcenteronline.com/services/v2/service_types/411816/plans/"
    Const getSong As String = "https://api.planningcenteronline.com/services/v2/songs/"

    'Planning center serviceType IDs
    Const igniteId As Integer = 449272 'Not used but keeping for possible future use
    Const elevenId As Integer = 411816
    '-----------------------------------------------------------------------------------------------------------------------
    'End Constants

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ListView2.Columns(0).Width = 78
        ListView2.Columns(1).Width = 92
        ListView2.Columns(2).Width = 133
        ListView2.Columns(3).Width = 133

        populatePlans()
    End Sub

    Private Sub populatePlans()
        'Start by loading the most recent plans with dates
        Dim webClient As New System.Net.WebClient
        Dim result As String = ""
        webClient.Credentials = New Net.NetworkCredential(appId, secret)
        Try
            result = webClient.DownloadString(urlPlans)
        Catch ex As Exception
            MessageBox.Show(ex.Message)
            Return
        End Try

        parsePlans(result)
    End Sub

    Private Sub parsePlans(ByVal str As String)
        Dim json As JToken = JToken.Parse(str)

        If json.HasValues = True Then
            For Each val As JToken In json("data")

                Dim id As JToken = val("id")
                Dim attr As JToken = val("attributes")
                Dim dates As JToken = attr("dates")

                Dim itm As New ListViewItem(New String() {id.ToString, dates.ToString()})

                ListView1.Items.Add(itm)
            Next
        End If
    End Sub

    Private Sub loadSongList(ByVal planId As Integer)
        Dim webClient As New System.Net.WebClient
        Dim result As String = ""

        webClient.Credentials = New Net.NetworkCredential(appId, secret)
        Try
            result = webClient.DownloadString(getPlan & planId & "/items")
        Catch ex As Exception
            MessageBox.Show(ex.Message)
            Return
        End Try

        If result <> vbNullString Then
            Dim json As JToken = JToken.Parse(result)

            For Each dat As JToken In json("data")
                Dim arrDat As JToken = dat("relationships")("arrangement")("data")
                Dim songDat As JToken = dat("relationships")("song")("data")
                If songDat.SelectToken("id") IsNot Nothing And arrDat.SelectToken("id") IsNot Nothing Then
                    Dim songId As JToken = songDat("id")
                    Dim arrId As JToken = arrDat("id")
                    Dim title As JToken = dat("attributes")("title")
                    Dim author As String = getAuthor(songId)

                    Dim itm As New ListViewItem(New String() {songId, arrId, title, author})

                    ListView2.Items.Add(itm)
                End If
            Next
        End If
    End Sub

    Private Sub loadLyrics(ByVal s As song)
        RichTextBox1.Text = ""
        ListView3.Items.Clear()

        For Each itm As String In s.roadMap
            If CheckBox1.Checked = True Then
                ListView3.Items.Add(itm)
            Else
                If (ListView3.FindItemWithText(itm) Is Nothing) Then
                    ListView3.Items.Add(itm)
                End If
            End If
        Next

        RichTextBox1.Text &= s.buildLyrics(CheckBox1.Checked)
    End Sub

    Private Function getAuthor(ByVal int As Integer) As String
        Dim webClient As New System.Net.WebClient
        Dim result As String = ""
        Dim rtnStr As String = ""

        webClient.Credentials = New Net.NetworkCredential(appId, secret)
        Try
            result = webClient.DownloadString(getSong & int)
        Catch ex As Exception
            MessageBox.Show(ex.Message)
            result = ""
        End Try

        If result <> vbNullString Then
            Dim json As JToken = JToken.Parse(result)

            rtnStr = json("data")("attributes")("author")
        End If

        Return rtnStr
    End Function

    Private Sub ListView1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListView1.SelectedIndexChanged
        If (ListView1.SelectedItems.Count > 0) Then
            ListView2.Items.Clear()
            loadSongList(ListView1.SelectedItems.Item(0).Text)
        End If
    End Sub

    Private Sub ListView2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListView2.SelectedIndexChanged
        If (ListView2.SelectedItems.Count > 0) Then
            ListView3.Items.Clear()
            Dim sng As song = song.Create(ListView2.SelectedItems.Item(0).Text, ListView2.SelectedItems.Item(0).SubItems(1).Text)
            If (sng IsNot Nothing) Then
                ListView3.Tag = sng
                loadLyrics(sng)
            End If
        End If
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        If (ListView3.Tag IsNot Nothing) Then
            loadLyrics(ListView3.Tag)
        End If
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        RichTextBox1.Text = ""
        ListView1.Items.Clear()
        ListView2.Items.Clear()
        ListView3.Items.Clear()
        populatePlans()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If (RichTextBox1.Text <> vbNullString) Then
            Using sfd As New SaveFileDialog()
                sfd.Filter = "Rich Text File|*.rtf"
                Dim result As DialogResult = sfd.ShowDialog()
                If result = DialogResult.OK Or result = DialogResult.Yes Then
                    RichTextBox1.SaveFile(sfd.FileName)
                End If
            End Using
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        If (ListView2.Items.Count > 0) Then
            If (FolderBrowserDialog1.ShowDialog() = DialogResult.OK) Then
                Dim rtb As New RichTextBox()
                For Each val As ListViewItem In ListView2.Items
                    Dim writer As New IO.StreamWriter(FolderBrowserDialog1.SelectedPath & "/" & val.SubItems(2).Text & ".rtf")
                    Dim sng As song = song.Create(val.Text, val.SubItems(1).Text)
                    rtb.Text = sng.buildLyrics(CheckBox1.Checked)
                    writer.Write(rtb.Rtf)
                    writer.Close()
                Next
                rtb.Dispose()
            End If
        End If
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        If ListView2.SelectedItems.Count > 0 Then
            Dim db As New SQLite
            Dim sng As song = song.Create(ListView2.SelectedItems.Item(0).Text, ListView2.SelectedItems.Item(0).SubItems(1).Text)
            If (db.insertSong(ListView2.SelectedItems.Item(0).SubItems(2).Text, sng, CheckBox1.Checked) = 0) Then
                MessageBox.Show("Import completed successfully.")
            Else
                MessageBox.Show("Import failed.")
            End If
        End If
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        If ListView2.Items.Count > 0 Then
            Dim errors As Integer = 0
            Dim db As New SQLite
            Dim sng As song = Nothing
            For Each itm As ListViewItem In ListView2.Items
                sng = song.Create(itm.Text, itm.SubItems(1).Text)
                errors += db.insertSong(itm.SubItems(2).Text, sng, CheckBox1.Checked)
            Next

            If errors = 0 Then
                MessageBox.Show("Import completed successfully.")
            Else
                MessageBox.Show("Import was successful but returned errors. Some songs may not work properly.")
            End If
        End If
    End Sub
End Class