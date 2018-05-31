Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Text.RegularExpressions

'Handles all the song formatting and parsing of lyrics
Public Class song
    'Constants-----------------------------------------------------------------------------------------
    Const appId As String = "****" 'Removed to protect data
    Const secret As String = "****" 'Removed to protect data

    Const getSong As String = "https://api.planningcenteronline.com/services/v2/songs/"
    'End Constants-------------------------------------------------------------------------------------

    Private _idNum As Integer
    Private _arrangement As Integer
    Private _roadMap As List(Of String)
    Private _headers As Dictionary(Of String, String)
    Private _author As String
    Private _copyright As String
    Private Shared _cache As List(Of song) = New List(Of song)

    'Do not use. Use song.Create instead.
    Sub New(ByVal id As Integer, ByVal arr As Integer)
        _idNum = id
        _arrangement = arr
        _roadMap = New List(Of String)
        _headers = New Dictionary(Of String, String)
        _author = getAuthor(_idNum)
        _copyright = getCopyright(_idNum)
        buildSongInfo()
    End Sub

    'Function to utilize cache
    Public Shared Function Create(ByVal id As Integer, ByVal arr As Integer) As song
        Dim sng As song = Nothing
        If _cache.Count > 0 Then
            For Each s As song In _cache
                If (s.idNum = id And s.arrangement = arr) Then
                    sng = s
                    Exit For
                End If
            Next
        End If

        If sng Is Nothing Then
            sng = New song(id, arr)
            _cache.Add(sng)
            If _cache.Count > 10 Then
                _cache.Remove(_cache.First)
            End If
        End If

        Return sng
    End Function

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

    Private Function getCopyright(ByVal int As Integer) As String
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


            rtnStr = json("data")("attributes")("copyright")
        End If

        Return rtnStr
    End Function

    Property idNum As Integer
        Get
            Return _idNum
        End Get
        Set(value As Integer)
            _idNum = value
        End Set
    End Property

    ReadOnly Property author As String
        Get
            Return _author
        End Get
    End Property

    ReadOnly Property copyright As String
        Get
            Return _copyright
        End Get
    End Property

    Property arrangement As Integer
        Get
            Return _arrangement
        End Get
        Set(value As Integer)
            _arrangement = value
        End Set
    End Property

    ReadOnly Property roadMap As List(Of String)
        Get
            Return _roadMap
        End Get
    End Property

    Function getHeader(ByVal index As Integer) As String
        Return _headers.Keys(index)
    End Function

    Function headerList() As Dictionary(Of String, String)
        Return _headers
    End Function

    Sub addHeader(ByVal text As String, ByVal lyrics As String, Optional ByVal addToRdMp As Boolean = False)
        text = text.Replace("PreChorus", "Pre-Chorus")
        text = text.Trim()
        If addToRdMp = True Then
            _roadMap.Add(text.Trim({"("c, ")"c}))
        End If
        _headers.Add(text.Trim({"("c, ")"c}), lyrics)
    End Sub

    Function headerExists(ByVal str As String) As Boolean
        Dim rtn As Boolean = False

        If _headers.Count > 0 Then
            rtn = _headers.Keys.Contains(str.Trim({"("c, ")"c}))
        End If

        Return rtn
    End Function

    Function buildLyrics(ByVal duplicates As Boolean) As String
        Dim rtnStr As String = ""
        Dim written As List(Of String) = New List(Of String)
        For Each str As String In roadMap
            If (duplicates = False And written.Contains(str)) Then
                Continue For
            End If

            For Each sr As String In headerList.Keys
                If (sr.Contains(str) And (Split(sr)(0) = Split(str)(0))) Then
                    rtnStr &= str & vbNewLine & headerList.Item(sr) & vbNewLine
                    written.Add(str)
                    Exit For
                End If
            Next
        Next

        Return rtnStr
    End Function

    'Internal Functions and Subroutines----------------------------------------------------------------------------------------

    Private Sub buildSongInfo()
        Dim webClient As New System.Net.WebClient
        Dim result As String = ""
        webClient.Credentials = New Net.NetworkCredential(appId, secret)
        webClient.Encoding = Text.Encoding.UTF8
        Try
        result = webClient.DownloadString(getSong & _idNum & "/arrangements/" & _arrangement)
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try

        If result <> vbNullString Then
            Dim json As JToken = JToken.Parse(result)

            Dim lyrics As JToken = json("data")("attributes")("chord_chart")
            For Each dat As JToken In json("data")("attributes")("sequence")
                _roadMap.Add(dat.ToString().Replace("PreChorus", "Pre-Chorus"))
            Next

            parseLyrics(lyrics, _roadMap.Count = 0)
        End If
    End Sub

    'Still kinda spaghetti code(ish) but functional
    Private Sub parseLyrics(ByVal lyr As String, Optional ByVal addToRoadmap As Boolean = False)
        Using sr As New IO.StringReader(lyr)
            Dim line As String = ""
            Dim header As String = ""
            Dim section As String = ""
            Dim rx As New Regex("\bVerse\b(\s[0-9]|\n)|\bChorus\b(\s[0-9]|\n)*|\bMisc\b(\s[0-9]|\n|\.)|\(*\bBridge\b(\s[0-9]|\n|\))*|(\(*\bEnding\b\)*)|(\(*\bPre-Chorus\b\)*)", RegexOptions.IgnoreCase)

            'Yo dawg I herd u liek "if" statements.
            line = sr.ReadLine()
            While line IsNot Nothing

                'vbNullString is not the same as Nothing
                If (line.Trim() <> vbNullString) Then
                    If (line = "Chorus") Then
                        line &= " 1"
                    End If

                    If (line = "Verse2") Then
                        line = "Verse 2"
                    End If

                    If (rx.IsMatch(line.Trim()) = True And Split(line.Trim()).Count < 3) Then

                        If (headerExists(header) = False And section <> vbNullString And header <> vbNullString) Then
                            addHeader(header, section, addToRoadmap)
                            header = ""
                            section = ""
                        End If

                        If line.Contains("Misc") Then
                            'If the header is a misc header then advance the position to the misc header name
                            Do
                                line = sr.ReadLine()
                            Loop Until line <> vbNullString

                            'Trading My Sorrows hackfix
                            If (line = "(Vamp)") Then
                                line = "Bridge"
                            End If
                        End If

                        header = line
                    Else
                        If (headerExists(header) = False And header <> vbNullString) Then
                            section &= line.Trim() & vbNewLine
                        End If
                    End If
                End If
                line = sr.ReadLine()
            End While

            If (header <> vbNullString And section <> vbNullString) Then
                addHeader(header, section)
                header = ""
                section = ""
            End If
        End Using
    End Sub
End Class
