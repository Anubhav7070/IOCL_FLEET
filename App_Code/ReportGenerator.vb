Imports System
Imports System.IO
Imports System.Data
Imports System.Data.SQLite
Imports System.Text
Imports iTextSharp.text
Imports iTextSharp.text.pdf

Public Class ReportGenerator

    Private Shared ReadOnly LicenseNames As New Dictionary(Of String, String)() From {
        {"ROAD_PERMIT", "Road Permit (By RTO)"},
        {"AGE_DETERMINATION", "Date of Manufacture / Age Determination"},
        {"PUC", "Pollution Under Control (PUC)"},
        {"FITNESS", "Fitness License (By RTO)"},
        {"EXPLOSIVE", "Explosive License"},
        {"GREEN_CARD", "Green Card"},
        {"INSURANCE", "Vehicle Insurance"},
        {"CALIBRATION", "Calibration Certificate"}
    }

    Private Shared Function GetLicenseName(ByVal key As String) As String
        Dim name As String = Nothing
        If LicenseNames.TryGetValue(key, name) Then Return name
        Return key.Replace("_", " ")
    End Function

    Private Shared Function FmtDate(ByVal d As String) As String
        If String.IsNullOrEmpty(d) Then Return "PENDING"
        Dim dt As DateTime
        If DateTime.TryParse(d, dt) Then Return dt.ToString("dd-MMM-yyyy")
        Return d
    End Function

    Private Shared Function GetDaysRemaining(ByVal expiryDate As String) As String
        If String.IsNullOrEmpty(expiryDate) Then Return "N/A"
        Dim dt As DateTime
        If Not DateTime.TryParse(expiryDate, dt) Then Return "N/A"
        Dim diff As Integer = Convert.ToInt32(Math.Ceiling((dt.Date - DateTime.Today).TotalDays))
        If diff < 0 Then Return Math.Abs(diff).ToString() & "d overdue"
        Return diff.ToString() & "d remaining"
    End Function

    Private Shared Function MakeCell(ByVal tbl As PdfPTable, ByVal txt As String, ByVal fnt As Font, ByVal bg As BaseColor) As PdfPCell
        Dim c As New PdfPCell(New Phrase(txt, fnt))
        c.BackgroundColor = bg
        c.Padding = 4
        c.Border = Rectangle.BOTTOM_BORDER
        c.BorderColor = New BaseColor(226, 232, 240)
        tbl.AddCell(c)
        Return c
    End Function

    Private Shared Function MakeHeaderCell(ByVal tbl As PdfPTable, ByVal txt As String, ByVal fnt As Font, ByVal bg As BaseColor) As PdfPCell
        Dim c As New PdfPCell(New Phrase(txt, fnt))
        c.BackgroundColor = bg
        c.Padding = 5
        c.Border = Rectangle.NO_BORDER
        tbl.AddCell(c)
        Return c
    End Function

    Public Shared Function GenerateCompliancePdf(ByVal department As String) As Byte()
        Dim sql As String = "SELECT v.VehicleNumber, v.VehicleType, v.OverallStatus, v.Department As DeptName, " &
                           "r.LicenseType, r.LicenseNumber, r.IssuingAuthority, r.IssueDate, r.ExpiryDate, r.Status " &
                           "FROM ComplianceRecords r " &
                           "INNER JOIN Vehicles v ON r.VehicleId = v.Id"

        Dim parameters As New List(Of SQLiteParameter)()
        If Not String.IsNullOrEmpty(department) AndAlso department <> "0" Then
            sql &= " WHERE v.Department = @Dept"
            parameters.Add(New SQLiteParameter("@Dept", department))
        End If
        sql &= " ORDER BY v.VehicleNumber, r.LicenseType"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, parameters.ToArray())

        Using ms As New MemoryStream()
            ' Landscape A4 (842x595 points)
            Dim doc As New Document(New Rectangle(842, 595), 28, 28, 36, 28)
            PdfWriter.GetInstance(doc, ms)
            doc.Open()

            Dim darkRed As New BaseColor(127, 29, 29)
            Dim ioclOrange As New BaseColor(255, 107, 0)
            Dim lightGray As New BaseColor(241, 245, 249)
            Dim slate As New BaseColor(30, 41, 59)

            Dim fontTitle As Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.WHITE)
            Dim fontSub As Font = FontFactory.GetFont(FontFactory.HELVETICA, 8, New BaseColor(100, 116, 139))
            Dim fontHeader As Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, BaseColor.WHITE)
            Dim fontCell As Font = FontFactory.GetFont(FontFactory.HELVETICA, 8, slate)
            Dim fontCellBold As Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, slate)
            Dim fontRed As Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, New BaseColor(220, 38, 38))
            Dim fontOrange As Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, New BaseColor(234, 88, 12))
            Dim fontAmber As Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, New BaseColor(217, 119, 6))

            ' Header table
            Dim hdrTable As New PdfPTable(1)
            hdrTable.WidthPercentage = 100

            Dim hdrCell As New PdfPCell(New Phrase("INDIAN OIL CORPORATION LIMITED - Panipat Refinery", fontTitle))
            hdrCell.BackgroundColor = darkRed
            hdrCell.Padding = 12
            hdrCell.Border = Rectangle.NO_BORDER
            hdrTable.AddCell(hdrCell)

            Dim subCell As New PdfPCell(New Phrase("Fleet Compliance Status Report  |  Generated: " & DateTime.Now.ToString("dd/MM/yyyy HH:mm"), fontSub))
            subCell.Padding = 6
            subCell.Border = Rectangle.BOTTOM_BORDER
            subCell.BorderColor = ioclOrange
            subCell.BorderWidth = 2
            hdrTable.AddCell(subCell)
            doc.Add(hdrTable)
            doc.Add(New Paragraph(" "))

            ' Data table - 9 columns
            Dim tbl As New PdfPTable(9)
            tbl.WidthPercentage = 100
            tbl.SetWidths(New Single() {1.8F, 1.5F, 2.5F, 2.8F, 1.4F, 1.8F, 2.0F, 1.7F, 1.8F})

            Dim headers() As String = {"Vehicle No", "Dept", "License Type", "License No", "Issue Date", "Expiry Date", "Status", "Days", "Authority"}
            For Each h As String In headers
                MakeHeaderCell(tbl, h, fontHeader, darkRed)
            Next

            Dim alt As Boolean = False
            For Each row As DataRow In dt.Rows
                Dim bg As BaseColor = If(alt, lightGray, BaseColor.WHITE)
                Dim status As String = row("Status").ToString()
                Dim statusFont As Font = fontCell
                Select Case status
                    Case "EXPIRED" : statusFont = fontRed
                    Case "HIGH_CRITICAL", "MEDIUM_CRITICAL" : statusFont = fontOrange
                    Case "WARNING" : statusFont = fontAmber
                End Select

                MakeCell(tbl, row("VehicleNumber").ToString(), fontCellBold, bg)
                MakeCell(tbl, row("DeptName").ToString(), fontCell, bg)
                MakeCell(tbl, GetLicenseName(row("LicenseType").ToString()), fontCell, bg)
                MakeCell(tbl, If(row("LicenseNumber") Is DBNull.Value, "PENDING", row("LicenseNumber").ToString()), fontCell, bg)
                MakeCell(tbl, FmtDate(If(row("IssueDate") Is DBNull.Value, "", row("IssueDate").ToString())), fontCell, bg)
                MakeCell(tbl, FmtDate(If(row("ExpiryDate") Is DBNull.Value, "", row("ExpiryDate").ToString())), statusFont, bg)
                MakeCell(tbl, status.Replace("_", " "), statusFont, bg)
                MakeCell(tbl, GetDaysRemaining(If(row("ExpiryDate") Is DBNull.Value, "", row("ExpiryDate").ToString())), statusFont, bg)
                MakeCell(tbl, If(row("IssuingAuthority") Is DBNull.Value, "N/A", row("IssuingAuthority").ToString()), fontCell, bg)

                alt = Not alt
            Next

            doc.Add(tbl)
            doc.Add(New Paragraph(" "))

            Dim footNote As String = "CONFIDENTIAL - FOR INTERNAL REFINERY USE ONLY | IOCL Panipat Refinery | Total Records: " & dt.Rows.Count.ToString()
            doc.Add(New Paragraph(footNote, FontFactory.GetFont(FontFactory.HELVETICA, 7, New BaseColor(148, 163, 184))))

            doc.Close()
            Return ms.ToArray()
        End Using
    End Function

    Public Shared Function GenerateExpiryPdfBytes(ByVal dt As DataTable) As Byte()
        Using ms As New MemoryStream()
            ' Landscape A4 (842x595 points)
            Dim doc As New Document(New Rectangle(842, 595), 28, 28, 36, 28)
            PdfWriter.GetInstance(doc, ms)
            doc.Open()

            Dim darkRed As New BaseColor(127, 29, 29)
            Dim lightGray As New BaseColor(241, 245, 249)
            Dim slate As New BaseColor(30, 41, 59)

            Dim fontHeader As Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, BaseColor.WHITE)
            Dim fontCell As Font = FontFactory.GetFont(FontFactory.HELVETICA, 8, slate)
            Dim fontCellBold As Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, slate)
            Dim fontRed As Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, New BaseColor(220, 38, 38))
            Dim fontOrange As Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, New BaseColor(234, 88, 12))
            Dim fontAmber As Font = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, New BaseColor(217, 119, 6))

            Dim title As New Paragraph("IOCL Panipat Refinery - Compliance Expiry Alert Register", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13, darkRed))
            title.SpacingAfter = 4
            doc.Add(title)

            Dim subTitle As New Paragraph("Documents Requiring Renewal  |  Generated: " & DateTime.Now.ToString("dd/MM/yyyy HH:mm"), FontFactory.GetFont(FontFactory.HELVETICA, 8, New BaseColor(100, 116, 139)))
            subTitle.SpacingAfter = 10
            doc.Add(subTitle)

            Dim tbl As New PdfPTable(8)
            tbl.WidthPercentage = 100
            tbl.SetWidths(New Single() {1.8F, 1.8F, 3.0F, 1.5F, 2.0F, 1.8F, 2.0F, 1.8F})

            Dim headers() As String = {"Vehicle No", "Department", "Document / License Type", "License No", "Issuing Authority", "Expiry Date", "Alert Status", "Days Remaining"}
            For Each h As String In headers
                MakeHeaderCell(tbl, h, fontHeader, darkRed)
            Next

            Dim alt As Boolean = False
            For Each row As DataRow In dt.Rows
                Dim bg As BaseColor = If(alt, lightGray, BaseColor.WHITE)
                Dim status As String = row("Status").ToString()
                Dim statusFont As Font = fontCell
                Select Case status
                    Case "EXPIRED" : statusFont = fontRed
                    Case "HIGH_CRITICAL", "MEDIUM_CRITICAL" : statusFont = fontOrange
                    Case "WARNING" : statusFont = fontAmber
                End Select

                Dim expiry As String = If(row("ExpiryDate") Is DBNull.Value, "", row("ExpiryDate").ToString())
                Dim deptName As String = ""
                If dt.Columns.Contains("DeptName") AndAlso row("DeptName") IsNot DBNull.Value Then deptName = row("DeptName").ToString()

                MakeCell(tbl, row("VehicleNumber").ToString(), fontCellBold, bg)
                MakeCell(tbl, deptName, fontCell, bg)
                MakeCell(tbl, GetLicenseName(row("LicenseType").ToString()), fontCell, bg)
                MakeCell(tbl, If(row("LicenseNumber") Is DBNull.Value, "PENDING", row("LicenseNumber").ToString()), fontCell, bg)
                Dim issuingAuth As String = ""
                If dt.Columns.Contains("IssuingAuthority") AndAlso row("IssuingAuthority") IsNot DBNull.Value Then issuingAuth = row("IssuingAuthority").ToString()
                MakeCell(tbl, issuingAuth, fontCell, bg)
                MakeCell(tbl, FmtDate(expiry), statusFont, bg)
                MakeCell(tbl, status.Replace("_", " "), statusFont, bg)
                MakeCell(tbl, GetDaysRemaining(expiry), statusFont, bg)

                alt = Not alt
            Next

            doc.Add(tbl)
            doc.Close()
            Return ms.ToArray()
        End Using
    End Function

    Public Shared Function GenerateComplianceExcel(ByVal department As String) As Byte()
        Dim sql As String = "SELECT v.VehicleNumber, v.VehicleType, v.OverallStatus, v.Department As DeptName, " &
                           "r.LicenseType, r.LicenseNumber, r.IssuingAuthority, r.IssueDate, r.ExpiryDate, r.Status " &
                           "FROM ComplianceRecords r " &
                           "INNER JOIN Vehicles v ON r.VehicleId = v.Id"

        Dim parameters As New List(Of SQLiteParameter)()
        If Not String.IsNullOrEmpty(department) AndAlso department <> "0" Then
            sql &= " WHERE v.Department = @Dept"
            parameters.Add(New SQLiteParameter("@Dept", department))
        End If
        sql &= " ORDER BY v.VehicleNumber, r.LicenseType"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, parameters.ToArray())

        Dim sb As New StringBuilder()
        sb.AppendLine("<html><head><meta charset='utf-8'/></head><body>")
        sb.AppendLine("<h2>IOCL Panipat Refinery - Fleet Compliance Report</h2>")
        sb.AppendLine("<p>Generated: " & DateTime.Now.ToString("dd/MM/yyyy HH:mm") & "</p>")
        sb.AppendLine("<table border='1' cellpadding='4' cellspacing='0' style='border-collapse:collapse;font-family:Arial;font-size:10px;'>")
        sb.AppendLine("<tr style='background:#7F1D1D;color:#fff;font-weight:bold;'>")
        Dim hdrs() As String = {"Vehicle No", "Vehicle Type", "Department", "Overall Status", "License Type", "License No", "Issuing Authority", "Issue Date", "Expiry Date", "Status", "Days Remaining"}
        For Each h As String In hdrs
            sb.Append("<th>" & h & "</th>")
        Next
        sb.AppendLine("</tr>")

        Dim alt As Boolean = False
        For Each row As DataRow In dt.Rows
            Dim bg As String = If(alt, "#F1F5F9", "#FFFFFF")
            Dim status As String = row("Status").ToString()
            Dim statusColor As String = "#1E293B"
            Select Case status
                Case "EXPIRED" : statusColor = "#DC2626"
                Case "HIGH_CRITICAL", "MEDIUM_CRITICAL" : statusColor = "#EA580C"
                Case "WARNING" : statusColor = "#D97706"
            End Select
            Dim expiry As String = If(row("ExpiryDate") Is DBNull.Value, "", row("ExpiryDate").ToString())

            sb.AppendLine("<tr style='background:" & bg & ";'>")
            sb.Append("<td><b>" & row("VehicleNumber").ToString() & "</b></td>")
            sb.Append("<td>" & row("VehicleType").ToString() & "</td>")
            sb.Append("<td>" & row("DeptName").ToString() & "</td>")
            sb.Append("<td>" & row("OverallStatus").ToString().Replace("_", " ") & "</td>")
            sb.Append("<td>" & GetLicenseName(row("LicenseType").ToString()) & "</td>")
            sb.Append("<td>" & If(row("LicenseNumber") Is DBNull.Value, "PENDING", row("LicenseNumber").ToString()) & "</td>")
            sb.Append("<td>" & If(row("IssuingAuthority") Is DBNull.Value, "N/A", row("IssuingAuthority").ToString()) & "</td>")
            sb.Append("<td>" & FmtDate(If(row("IssueDate") Is DBNull.Value, "", row("IssueDate").ToString())) & "</td>")
            sb.Append("<td style='color:" & statusColor & ";font-weight:bold;'>" & FmtDate(expiry) & "</td>")
            sb.Append("<td style='color:" & statusColor & ";font-weight:bold;'>" & status.Replace("_", " ") & "</td>")
            sb.Append("<td style='color:" & statusColor & ";font-weight:bold;'>" & GetDaysRemaining(expiry) & "</td>")
            sb.AppendLine("</tr>")
            alt = Not alt
        Next

        sb.AppendLine("</table></body></html>")
        Return System.Text.Encoding.UTF8.GetBytes(sb.ToString())
    End Function
End Class
