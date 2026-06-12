Imports System
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Collections.Generic
Imports iTextSharp.text.pdf
Imports iTextSharp.text.pdf.parser

Public Class PdfExtractor

    ''' <summary>
    ''' Extract all text from an uploaded PDF stream using iTextSharp.
    ''' Returns empty string if extraction fails.
    ''' </summary>
    Public Shared Function ExtractText(ByVal pdfStream As Stream) As String
        Dim sb As New StringBuilder()
        Try
            Using reader As New PdfReader(pdfStream)
                For i As Integer = 1 To reader.NumberOfPages
                    Dim strategy As New LocationTextExtractionStrategy()
                    Dim pageText As String = PdfTextExtractor.GetTextFromPage(reader, i, strategy)
                    sb.AppendLine(pageText)
                Next
            End Using
        Catch ex As Exception
            Console.WriteLine("[PdfExtractor] Error reading PDF: " & ex.Message)
        End Try
        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Extract all text from PDF bytes.
    ''' </summary>
    Public Shared Function ExtractTextFromBytes(ByVal pdfBytes As Byte()) As String
        Using ms As New MemoryStream(pdfBytes)
            Return ExtractText(ms)
        End Using
    End Function

    ''' <summary>
    ''' Attempts to find an expiry/validity date in raw text extracted from a PDF.
    ''' Priority: dates near expiry keywords in future, then any future date, then latest overall.
    ''' Ported from ComplianceController.cs::ExtractExpiryDate()
    ''' </summary>
    Public Shared Function ExtractExpiryDate(ByVal text As String) As Nullable(Of DateTime)
        If String.IsNullOrEmpty(text) Then Return Nothing

        Dim datePatterns() As String = {
            "(\d{1,2})[\/\-\.](\d{1,2})[\/\-\.](\d{2,4})",
            "(\d{1,2})[\s\-\/\.]*(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*[\s\-\/\.,]+(\d{2,4})",
            "(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*[\s\-\/\.,]+(\d{1,2})[\s\-\/\.,]+(\d{2,4})",
            "(\d{4})[\/\-\.](\d{1,2})[\/\-\.](\d{1,2})"
        }

        Dim expiryKeywords() As String = {
            "expir", "valid till", "valid upto", "validity", "renewal date",
            "renew", "due date", "date of expiry", "expire", "validity upto", "validity date"
        }

        Dim matches As New List(Of Tuple(Of DateTime, Integer, Boolean))()

        For Each pattern As String In datePatterns
            Dim rx As New Regex(pattern, RegexOptions.IgnoreCase)
            Dim mc As MatchCollection = rx.Matches(text)
            For Each m As Match In mc
                Dim parsedDate As DateTime = Nothing
                If TryParseDate(m, pattern, parsedDate) Then
                    Dim startIdx As Integer = Math.Max(0, m.Index - 150)
                    Dim endIdx As Integer = Math.Min(text.Length, m.Index + m.Length + 150)
                    Dim surrounding As String = text.Substring(startIdx, endIdx - startIdx).ToLower()
                    Dim hasKeyword As Boolean = False
                    For Each kw As String In expiryKeywords
                        If surrounding.Contains(kw) Then hasKeyword = True : Exit For
                    Next
                    matches.Add(New Tuple(Of DateTime, Integer, Boolean)(parsedDate, m.Index, hasKeyword))
                End If
            Next
        Next

        If matches.Count = 0 Then Return Nothing

        ' Priority 1: keyword nearby + future date
        Dim futureKeyword = New List(Of Tuple(Of DateTime, Integer, Boolean))()
        For Each mt In matches
            If mt.Item3 AndAlso mt.Item1 > DateTime.Today Then futureKeyword.Add(mt)
        Next
        If futureKeyword.Count > 0 Then
            futureKeyword.Sort(Function(a, b) b.Item1.CompareTo(a.Item1))
            Return futureKeyword(0).Item1
        End If

        ' Priority 2: keyword nearby (any date)
        Dim withKeyword = New List(Of Tuple(Of DateTime, Integer, Boolean))()
        For Each mt In matches
            If mt.Item3 Then withKeyword.Add(mt)
        Next
        If withKeyword.Count > 0 Then
            withKeyword.Sort(Function(a, b) b.Item1.CompareTo(a.Item1))
            Return withKeyword(0).Item1
        End If

        ' Priority 3: any future date
        Dim futureOnly = New List(Of Tuple(Of DateTime, Integer, Boolean))()
        For Each mt In matches
            If mt.Item1 > DateTime.Today Then futureOnly.Add(mt)
        Next
        If futureOnly.Count > 0 Then
            futureOnly.Sort(Function(a, b) b.Item1.CompareTo(a.Item1))
            Return futureOnly(0).Item1
        End If

        ' Priority 4: latest date in document
        matches.Sort(Function(a, b) b.Item1.CompareTo(a.Item1))
        Return matches(0).Item1
    End Function

    Private Shared Function TryParseDate(ByVal m As Match, ByVal pattern As String, ByRef result As DateTime) As Boolean
        result = Nothing
        Try
            ' Pattern: dd/mm/yyyy
            If pattern.StartsWith("(\d{1,2})[\/") Then
                Dim p1 As Integer = Integer.Parse(m.Groups(1).Value)
                Dim p2 As Integer = Integer.Parse(m.Groups(2).Value)
                Dim yr As Integer = Integer.Parse(m.Groups(3).Value)
                If yr < 100 Then yr += 2000
                If p2 >= 1 AndAlso p2 <= 12 AndAlso p1 >= 1 AndAlso p1 <= 31 Then
                    If DateTime.TryParse(String.Format("{0}-{1:D2}-{2:D2}", yr, p2, p1), result) Then Return True
                End If
                If p1 >= 1 AndAlso p1 <= 12 AndAlso p2 >= 1 AndAlso p2 <= 31 Then
                    If DateTime.TryParse(String.Format("{0}-{1:D2}-{2:D2}", yr, p1, p2), result) Then Return True
                End If
                Return False
            End If

            ' Pattern: dd Mon YYYY
            If pattern.Contains("Jan|Feb") Then
                If DateTime.TryParse(m.Value, result) Then Return True
                Return False
            End If

            ' Pattern: yyyy-mm-dd
            If pattern.StartsWith("(\d{4})") Then
                Dim yr As Integer = Integer.Parse(m.Groups(1).Value)
                Dim mo As Integer = Integer.Parse(m.Groups(2).Value)
                Dim dy As Integer = Integer.Parse(m.Groups(3).Value)
                If DateTime.TryParse(String.Format("{0}-{1:D2}-{2:D2}", yr, mo, dy), result) Then Return True
                Return False
            End If
        Catch
        End Try
        Return False
    End Function
End Class
