Imports System
Imports System.Data
Imports System.Data.SQLite
Imports System.Net
Imports System.Net.Mail
Imports System.Configuration
Imports System.IO

Public Class EmailService
    Private Shared _host As String
    Private Shared _port As Integer
    Private Shared _user As String
    Private Shared _pass As String
    Private Shared _fromName As String
    Private Shared _fromAddress As String
    Private Shared _configured As Boolean = False

    Shared Sub New()
        Try
            _host = If(ConfigurationManager.AppSettings("EmailHost"), "smtp.gmail.com")
            _port = Convert.ToInt32(If(ConfigurationManager.AppSettings("EmailPort"), "587"))
            _user = If(ConfigurationManager.AppSettings("EmailUser"), "")
            _pass = If(ConfigurationManager.AppSettings("EmailPass"), "")
            _fromName = If(ConfigurationManager.AppSettings("EmailFromName"), "IOCL Fleet Compliance System")
            _fromAddress = If(ConfigurationManager.AppSettings("EmailFromAddress"), _user)
            _configured = Not String.IsNullOrEmpty(_user) AndAlso Not String.IsNullOrEmpty(_pass)
        Catch ex As Exception
            Console.WriteLine("[MAIL] Config error: " & ex.Message)
        End Try
    End Sub

    Public Shared Sub SendEmail(ByVal recipientEmail As String, ByVal subject As String, ByVal htmlBody As String,
                                Optional ByVal pdfAttachment As Byte() = Nothing,
                                Optional ByVal attachmentName As String = Nothing)
        If String.IsNullOrEmpty(recipientEmail) Then Return
        If Not _configured Then
            Console.WriteLine("[MAIL] Not configured - skipping email to " & recipientEmail)
            Return
        End If

        Try
            Dim fromAddr As New MailAddress(_fromAddress, _fromName)
            Dim toAddr As New MailAddress(recipientEmail)

            Dim smtp As New SmtpClient()
            smtp.Host = _host
            smtp.Port = _port
            smtp.EnableSsl = True
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network
            smtp.UseDefaultCredentials = False
            smtp.Credentials = New NetworkCredential(_user, _pass)
            smtp.Timeout = 8000

            Using message As New MailMessage(fromAddr, toAddr)
                message.Subject = subject
                message.Body = htmlBody
                message.IsBodyHtml = True

                If pdfAttachment IsNot Nothing AndAlso pdfAttachment.Length > 0 AndAlso Not String.IsNullOrEmpty(attachmentName) Then
                    Dim ms As New MemoryStream(pdfAttachment)
                    Dim att As New Attachment(ms, attachmentName, "application/pdf")
                    message.Attachments.Add(att)
                End If

                smtp.Send(message)
            End Using

            Console.WriteLine("[MAIL] Sent to: " & recipientEmail)
        Catch ex As Exception
            Console.WriteLine("[MAIL ERROR] Failed to " & recipientEmail & ": " & ex.Message)
        End Try
    End Sub

    ' ─────────────────────────────────────────────────────────────────────────
    ' Compliance Alert Email — ported from EmailService.cs::SendComplianceAlert()
    ' ─────────────────────────────────────────────────────────────────────────
    Public Shared Sub SendComplianceAlert(ByVal toEmail As String, ByVal toName As String,
                                          ByVal vehicleNumber As String, ByVal vehicleType As String,
                                          ByVal department As String, ByVal licenseType As String,
                                          ByVal expiryDate As String, ByVal daysRemaining As Integer,
                                          ByVal status As String)
        Dim isExpired As Boolean = (daysRemaining <= 0)
        Dim urgency As String
        If isExpired Then
            urgency = "IMMEDIATE ACTION REQUIRED"
        ElseIf daysRemaining <= 7 Then
            urgency = "URGENT: Expiring Soon"
        Else
            urgency = "Compliance Reminder"
        End If

        Dim subject As String
        If isExpired Then
            subject = "EXPIRED: " & vehicleNumber & " - " & licenseType.Replace("_", " ") & " | IOCL Refinery"
        Else
            subject = daysRemaining.ToString() & "d Left: " & vehicleNumber & " - " & licenseType.Replace("_", " ") & " | IOCL Refinery"
        End If

        Dim sb As New System.Text.StringBuilder()
        sb.Append("<div style='font-family:Arial,sans-serif;padding:20px;border:1px solid #e2e8f0;border-radius:8px;max-width:600px;'>")
        sb.Append("<h2 style='color:#0054A6;border-bottom:2px solid #FF6B00;padding-bottom:10px;'>IOCL Panipat Refinery Fleet Compliance Alert</h2>")
        sb.Append("<p>Dear " & toName & ",</p>")
        sb.Append("<p><strong>" & urgency & "</strong></p>")
        sb.Append("<table style='width:100%;border-collapse:collapse;margin-top:15px;'>")
        sb.Append("<tr style='background:#f8fafc;'><td style='padding:8px;font-weight:bold;border:1px solid #cbd5e1;'>Vehicle Number:</td><td style='padding:8px;border:1px solid #cbd5e1;'>" & vehicleNumber & "</td></tr>")
        sb.Append("<tr><td style='padding:8px;font-weight:bold;border:1px solid #cbd5e1;'>Vehicle Type:</td><td style='padding:8px;border:1px solid #cbd5e1;'>" & vehicleType & "</td></tr>")
        sb.Append("<tr style='background:#f8fafc;'><td style='padding:8px;font-weight:bold;border:1px solid #cbd5e1;'>Department:</td><td style='padding:8px;border:1px solid #cbd5e1;'>" & department & "</td></tr>")
        sb.Append("<tr><td style='padding:8px;font-weight:bold;border:1px solid #cbd5e1;'>Certificate Type:</td><td style='padding:8px;border:1px solid #cbd5e1;'>" & licenseType.Replace("_", " ") & "</td></tr>")
        sb.Append("<tr style='background:#f8fafc;'><td style='padding:8px;font-weight:bold;border:1px solid #cbd5e1;'>Expiry Date:</td><td style='padding:8px;font-weight:bold;color:#dc2626;border:1px solid #cbd5e1;'>" & expiryDate & "</td></tr>")
        sb.Append("<tr><td style='padding:8px;font-weight:bold;border:1px solid #cbd5e1;'>Status:</td><td style='padding:8px;font-weight:bold;color:#dc2626;border:1px solid #cbd5e1;'>" & status & "</td></tr>")
        sb.Append("<tr style='background:#f8fafc;'><td style='padding:8px;font-weight:bold;border:1px solid #cbd5e1;'>Days Remaining:</td><td style='padding:8px;border:1px solid #cbd5e1;'>" & daysRemaining.ToString() & "</td></tr>")
        sb.Append("</table>")
        sb.Append("<p style='margin-top:20px;'>Please log in to the refinery compliance dashboard to renew the certificate.</p>")
        sb.Append("<hr style='border:none;border-top:1px solid #cbd5e1;margin:20px 0;' />")
        sb.Append("<p style='font-size:11px;color:#64748b;'>This is a system generated email. Do not reply. IOCL Fleet Compliance Dept.</p></div>")

        SendEmail(toEmail, subject, sb.ToString())
    End Sub

    ' ─────────────────────────────────────────────────────────────────────────
    ' Daily Digest Email — ported from EmailService.cs::SendDailySummary()
    ' ─────────────────────────────────────────────────────────────────────────
    Public Shared Sub SendDailySummary(ByVal toEmail As String, ByVal toName As String,
                                       ByVal totalVehicles As Integer, ByVal expiredCount As Integer,
                                       ByVal criticalCount As Integer, ByVal warningCount As Integer,
                                       ByVal deptBreakdown As DataTable,
                                       Optional ByVal expiringDt As DataTable = Nothing)
        Dim subject As String = "Daily Compliance Digest - " & DateTime.Now.ToString("dd/MM/yyyy") & " | IOCL Panipat Refinery"

        Dim deptRows As New System.Text.StringBuilder()
        If deptBreakdown IsNot Nothing Then
            Dim i As Integer = 0
            For Each row As DataRow In deptBreakdown.Rows
                Dim bg As String = If(i Mod 2 = 0, "background:#fff;", "background:#f8fafc;")
                Dim score As Double = Convert.ToDouble(row("ComplianceScore"))
                Dim scoreColor As String = If(score >= 80, "#16a34a", If(score >= 60, "#d97706", "#dc2626"))
                deptRows.Append("<tr style='" & bg & "'>")
                deptRows.Append("<td style='padding:8px 12px;'>" & row("Name").ToString() & "</td>")
                deptRows.Append("<td style='text-align:center;padding:8px;'>" & row("VehicleCount").ToString() & "</td>")
                deptRows.Append("<td style='text-align:center;padding:8px;font-weight:bold;color:" & scoreColor & ";'>" & score.ToString("0.0") & "%</td></tr>")
                i += 1
            Next
        End If

        Dim sb As New System.Text.StringBuilder()
        sb.Append("<div style='font-family:Arial,sans-serif;padding:20px;max-width:600px;'>")
        sb.Append("<h2 style='color:#0054A6;'>IOCL Daily Compliance Digest</h2>")
        sb.Append("<p>Good morning, " & toName & ".</p>")
        sb.Append("<p>Fleet: <strong>" & totalVehicles & "</strong> | ")
        sb.Append("Expired: <strong style='color:#dc2626'>" & expiredCount & "</strong> | ")
        sb.Append("Critical: <strong style='color:#ea580c'>" & criticalCount & "</strong> | ")
        sb.Append("Warning: <strong style='color:#d97706'>" & warningCount & "</strong></p>")
        sb.Append("<p>Please find attached the PDF report listing vehicles and documents that are expiring or have expired.</p>")
        sb.Append("<table style='width:100%;border-collapse:collapse;font-size:12px;border:1px solid #e2e8f0;'>")
        sb.Append("<tr style='background:#f8fafc;'><th style='padding:8px;text-align:left;'>Department</th><th style='padding:8px;text-align:center;'>Vehicles</th><th style='padding:8px;text-align:center;'>Score</th></tr>")
        sb.Append(deptRows.ToString())
        sb.Append("</table>")
        sb.Append("<hr /><p style='font-size:11px;color:#64748b;'>System generated. Do not reply.</p></div>")

        Dim pdfBytes As Byte() = Nothing
        If expiringDt IsNot Nothing AndAlso expiringDt.Rows.Count > 0 Then
            Try
                pdfBytes = ReportGenerator.GenerateExpiryPdfBytes(expiringDt)
            Catch ex As Exception
                Console.WriteLine("[MAIL] PDF attachment failed: " & ex.Message)
            End Try
        End If

        SendEmail(toEmail, subject, sb.ToString(), pdfBytes, "Expiring_Vehicles_Report.pdf")
    End Sub

    ' ─────────────────────────────────────────────────────────────────────────
    ' Legacy helpers preserved for backwards compatibility
    ' ─────────────────────────────────────────────────────────────────────────
    ' Notify SuperAdmins + the vehicle owner when a document is renewed/uploaded
    Public Shared Sub NotifySuperAdminsOfRenewal(ByVal employeeName As String, ByVal vehicleNumber As String,
                                                  ByVal licenseType As String, Optional ByVal vehicleId As Integer = 0)
        Dim subject As String = "IOCL Fleet: Document Renewed - " & vehicleNumber
        Dim body As String = "<div style='font-family:Arial,sans-serif;'>" &
            "<h2 style='color:#0054A6;'>Compliance Document Renewal Alert</h2>" &
            "<p><strong>Renewed by:</strong> " & employeeName & "</p>" &
            "<p><strong>Vehicle:</strong> " & vehicleNumber & "</p>" &
            "<p><strong>Document Type:</strong> " & licenseType.Replace("_", " ") & "</p>" &
            "<p>Please log in to the compliance portal to review and verify the document.</p></div>"

        Dim sentEmails As New List(Of String)()

        ' 1. Notify vehicle owner
        If vehicleId > 0 Then
            Dim ownerDt As DataTable = Database.ExecuteDataTable(
                "SELECT e.EmailId, e.EmployeeName FROM Vehicles v " &
                "INNER JOIN Employee e ON v.EmployeeId = e.EmployeeId " &
                "WHERE v.Id = @VehId LIMIT 1",
                New SQLiteParameter("@VehId", vehicleId))
            For Each row As DataRow In ownerDt.Rows
                Dim email As String = row("EmailId").ToString()
                If Not String.IsNullOrEmpty(email) AndAlso Not sentEmails.Contains(email.ToLower()) Then
                    sentEmails.Add(email.ToLower())
                    SendEmail(email, subject, body)
                End If
            Next
        End If

        ' 2. Notify all SuperAdmins
        Dim adminDt As DataTable = Database.ExecuteDataTable(
            "SELECT e.EmailId FROM Authentication a INNER JOIN Employee e ON a.EmployeeId = e.EmployeeId WHERE a.Role = 'SuperAdmin'")
        For Each row As DataRow In adminDt.Rows
            Dim email As String = row("EmailId").ToString()
            If Not String.IsNullOrEmpty(email) AndAlso Not sentEmails.Contains(email.ToLower()) Then
                sentEmails.Add(email.ToLower())
                SendEmail(email, subject, body)
            End If
        Next
    End Sub

    Public Shared Sub NotifyEmployeeOfApproval(ByVal employeeId As Integer, ByVal vehicleNumber As String)
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT EmailId, EmployeeName FROM Employee WHERE EmployeeId = @EmpId", New SQLiteParameter("@EmpId", employeeId))
        If dt.Rows.Count = 0 Then Return
        Dim email As String = dt.Rows(0)("EmailId").ToString()
        Dim name As String = dt.Rows(0)("EmployeeName").ToString()
        Dim subject As String = "IOCL Fleet Alert: Vehicle Verification Approved"
        Dim body As String = "<h2>Verification Approved</h2><p>Dear " & name & ",</p><p>The compliance documentation for vehicle <strong>" & vehicleNumber & "</strong> has been verified and approved.</p>"
        SendEmail(email, subject, body)
    End Sub

    Public Shared Sub NotifySuperAdminsOfNewVehicle(ByVal registeredByEmpId As Integer, ByVal vehicleNumber As String)
        Dim dtReg As DataTable = Database.ExecuteDataTable("SELECT EmployeeName FROM Employee WHERE EmployeeId = @EmpId", New SQLiteParameter("@EmpId", registeredByEmpId))
        Dim regName As String = If(dtReg.Rows.Count > 0, dtReg.Rows(0)("EmployeeName").ToString(), "Unknown")

        Dim dtAdmins As DataTable = Database.ExecuteDataTable("SELECT EmailId, EmployeeName FROM Employee WHERE Role = 'SuperAdmin' AND EmailId IS NOT NULL AND EmailId <> ''")
        If dtAdmins.Rows.Count = 0 Then Return

        Dim subject As String = "IOCL Fleet: New Vehicle Registered - " & vehicleNumber
        Dim body As String = "<h2>New Vehicle Registration</h2>" &
                             "<p>A new vehicle has been registered in the IOCL Fleet Compliance System.</p>" &
                             "<table style='border-collapse:collapse'>" &
                             "<tr><td style='padding:6px;font-weight:bold'>Vehicle Number:</td><td style='padding:6px'><strong>" & vehicleNumber & "</strong></td></tr>" &
                             "<tr><td style='padding:6px;font-weight:bold'>Registered By:</td><td style='padding:6px'>" & regName & "</td></tr>" &
                             "<tr><td style='padding:6px;font-weight:bold'>Date &amp; Time:</td><td style='padding:6px'>" & DateTime.Now.ToString("dd-MMM-yyyy HH:mm") & "</td></tr>" &
                             "</table>" &
                             "<p>Please log in to the Fleet Compliance Portal to review and verify the vehicle.</p>"

        For Each row As DataRow In dtAdmins.Rows
            Dim adminEmail As String = row("EmailId").ToString()
            If Not String.IsNullOrEmpty(adminEmail) Then
                Console.WriteLine("[MAIL] Notifying SuperAdmin of new vehicle: " & adminEmail)
                SendEmail(adminEmail, subject, body)
            End If
        Next
    End Sub
End Class
