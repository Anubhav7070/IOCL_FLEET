Imports System
Imports System.Collections.Generic
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
            _fromName = If(ConfigurationManager.AppSettings("EmailFromName"), "IOCL Vehicle Compliance System")
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
    ' Consolidated Daily Alert/Digest Template
    ' ─────────────────────────────────────────────────────────────────────────
    Public Shared Sub SendConsolidatedDigest(ByVal toEmail As String, ByVal toName As String, ByVal roleOrDept As String, ByVal dt As DataTable)
        If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return

        Dim subject As String = "Daily Compliance Alerts Digest - " & DateTime.Now.ToString("dd/MM/yyyy") & " | Company Vehicle Management Portal"
        
        Dim sbRows As New System.Text.StringBuilder()
        Dim idx As Integer = 0
        For Each row As DataRow In dt.Rows
            Dim bg As String = If(idx Mod 2 = 0, "background:#ffffff;", "background:#f8fafc;")
            Dim status As String = row("Status").ToString()
            Dim statusColor As String = If(status.Equals("Expired", StringComparison.OrdinalIgnoreCase), "#dc2626", "#ea580c")
            
            Dim expDate As String = row("ExpiryDate").ToString()
            Dim daysLeft As Integer = 0
            Dim parsedDt As DateTime
            If DateTime.TryParse(expDate, parsedDt) Then
                daysLeft = Convert.ToInt32(Math.Ceiling((parsedDt.Date - DateTime.Today).TotalDays))
            End If
            
            Dim validityText As String = If(daysLeft < 0, Math.Abs(daysLeft).ToString() & " days overdue", daysLeft.ToString() & " days remaining")
            
            sbRows.Append("<tr style='" & bg & "; border-bottom:1px solid #e2e8f0;'>")
            sbRows.Append("<td style='padding:8px 10px; font-weight:bold; font-family:monospace;'>" & row("VehicleNumber").ToString() & "</td>")
            sbRows.Append("<td style='padding:8px 10px;'>" & row("VehicleType").ToString() & "</td>")
            sbRows.Append("<td style='padding:8px 10px;'>" & row("AllocatedDept").ToString() & "</td>")
            sbRows.Append("<td style='padding:8px 10px; font-weight:semibold;'>" & row("LicenseType").ToString().Replace("_", " ") & "</td>")
            sbRows.Append("<td style='padding:8px 10px; font-weight:bold; color:" & statusColor & ";'>" & expDate & "</td>")
            sbRows.Append("<td style='padding:8px 10px; font-weight:semibold; color:" & statusColor & ";'>" & status & "</td>")
            sbRows.Append("<td style='padding:8px 10px;'>" & validityText & "</td>")
            sbRows.Append("</tr>")
            idx += 1
        Next

        Dim sb As New System.Text.StringBuilder()
        sb.Append("<div style='font-family:Arial,sans-serif; padding:20px; border:1px solid #e2e8f0; border-radius:8px; max-width:700px; margin:0 auto;'>")
        sb.Append("<h2 style='color:#001F5B; border-bottom:2px solid #F47920; padding-bottom:10px; margin-top:0;'>Company Vehicle Management Portal</h2>")
        sb.Append("<p>Dear " & toName & " (" & roleOrDept & "),</p>")
        sb.Append("<p>Below is the daily consolidated report of vehicle documents that are expired or non-compliant (expiring soon) as of 10:00 AM today:</p>")
        
        sb.Append("<table style='width:100%; border-collapse:collapse; margin-top:15px; font-size:11px;'>")
        sb.Append("<tr style='background:#001F5B; color:#ffffff;'>")
        sb.Append("<th style='padding:8px 10px; text-align:left;'>Vehicle No</th>")
        sb.Append("<th style='padding:8px 10px; text-align:left;'>Type</th>")
        sb.Append("<th style='padding:8px 10px; text-align:left;'>Allocated Dept</th>")
        sb.Append("<th style='padding:8px 10px; text-align:left;'>Document</th>")
        sb.Append("<th style='padding:8px 10px; text-align:left;'>Expiry Date</th>")
        sb.Append("<th style='padding:8px 10px; text-align:left;'>Status</th>")
        sb.Append("<th style='padding:8px 10px; text-align:left;'>Validity</th>")
        sb.Append("</tr>")
        sb.Append(sbRows.ToString())
        sb.Append("</table>")
        
        sb.Append("<p style='margin-top:20px; font-size:12px;'>Please log in to the Company Vehicle Management Portal to submit document renewals directly from the dashboard.</p>")
        sb.Append("<hr style='border:none; border-top:1px solid #cbd5e1; margin:20px 0;' />")
        sb.Append("<p style='font-size:10px; color:#64748b; text-align:center;'>This is a system generated email. Do not reply. Company Vehicle Management Department.</p>")
        sb.Append("</div>")

        SendEmail(toEmail, subject, sb.ToString())
    End Sub

    ' ─────────────────────────────────────────────────────────────────────────
    ' Verification/Renewal Alerts preserving signatures for page dependencies
    ' ─────────────────────────────────────────────────────────────────────────
    Public Shared Sub NotifySuperAdminsOfRenewal(ByVal employeeName As String, ByVal vehicleNumber As String,
                                                  ByVal licenseType As String, Optional ByVal vehicleId As Integer = 0)
        Dim subject As String = "IOCL Vehicle: Document Renewed - " & vehicleNumber
        Dim body As String = "<div style='font-family:Arial,sans-serif;'>" &
            "<h2 style='color:#0054A6;'>Compliance Document Renewal Alert</h2>" &
            "<p><strong>Renewed by:</strong> " & employeeName & "</p>" &
            "<p><strong>Vehicle:</strong> " & vehicleNumber & "</p>" &
            "<p><strong>Document Type:</strong> " & licenseType.Replace("_", " ") & "</p>" &
            "<p>Please log in to the Company Vehicle Management Portal to review and verify the document.</p></div>"

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
        Dim subject As String = "IOCL Vehicle Alert: Vehicle Verification Approved"
        Dim body As String = "<h2>Verification Approved</h2><p>Dear " & name & ",</p><p>The compliance documentation for vehicle <strong>" & vehicleNumber & "</strong> has been verified and approved.</p>"
        SendEmail(email, subject, body)
    End Sub

    Public Shared Sub NotifySuperAdminsOfNewVehicle(ByVal registeredByEmpId As Integer, ByVal vehicleNumber As String)
        Dim dtReg As DataTable = Database.ExecuteDataTable("SELECT EmployeeName FROM Employee WHERE EmployeeId = @EmpId", New SQLiteParameter("@EmpId", registeredByEmpId))
        Dim regName As String = If(dtReg.Rows.Count > 0, dtReg.Rows(0)("EmployeeName").ToString(), "Unknown")

        Dim dtAdmins As DataTable = Database.ExecuteDataTable("SELECT e.EmailId FROM Authentication a INNER JOIN Employee e ON a.EmployeeId = e.EmployeeId WHERE a.Role = 'SuperAdmin'")
        If dtAdmins.Rows.Count = 0 Then Return

        Dim subject As String = "IOCL Vehicle: New Vehicle Registered - " & vehicleNumber
        Dim body As String = "<h2>New Vehicle Registration</h2>" &
                             "<p>A new vehicle has been registered in the Company Vehicle Management System.</p>" &
                             "<table style='border-collapse:collapse'>" &
                             "<tr><td style='padding:6px;font-weight:bold'>Vehicle Number:</td><td style='padding:6px'><strong>" & vehicleNumber & "</strong></td></tr>" &
                             "<tr><td style='padding:6px;font-weight:bold'>Registered By:</td><td style='padding:6px'>" & regName & "</td></tr>" &
                             "<tr><td style='padding:6px;font-weight:bold'>Date &amp; Time:</td><td style='padding:6px'>" & DateTime.Now.ToString("dd-MMM-yyyy HH:mm") & "</td></tr>" &
                             "</table>" &
                             "<p>Please log in to the Company Vehicle Management Portal to review and verify the vehicle.</p>"

        For Each row As DataRow In dtAdmins.Rows
            Dim adminEmail As String = row("EmailId").ToString()
            If Not String.IsNullOrEmpty(adminEmail) Then
                SendEmail(adminEmail, subject, body)
            End If
        Next
    End Sub

    Public Shared Sub NotifySuperAdminOfNewUser(ByVal name As String, ByVal empNo As String, ByVal role As String, ByVal dept As String)
        Dim dtAdmins As DataTable = Database.ExecuteDataTable("SELECT e.EmailId FROM Authentication a INNER JOIN Employee e ON a.EmployeeId = e.EmployeeId WHERE a.Role = 'SuperAdmin'")
        If dtAdmins.Rows.Count = 0 Then Return

        Dim subject As String = "Company Vehicle Management Portal - New User Account Registered"
        Dim body As String = "<h2>New Account Registration</h2>" &
                             "<p>A new operator/employee account has been registered in the Company Vehicle Management System.</p>" &
                             "<table style='border-collapse:collapse'>" &
                             "<tr><td style='padding:6px;font-weight:bold'>Employee Name:</td><td style='padding:6px'>" & name & "</td></tr>" &
                             "<tr><td style='padding:6px;font-weight:bold'>Employee Number:</td><td style='padding:6px'><strong>" & empNo & "</strong></td></tr>" &
                             "<tr><td style='padding:6px;font-weight:bold'>Assigned Role:</td><td style='padding:6px'>" & role & "</td></tr>" &
                             "<tr><td style='padding:6px;font-weight:bold'>Department:</td><td style='padding:6px'>" & If(String.IsNullOrEmpty(dept), "Global Scope", dept) & "</td></tr>" &
                             "<tr><td style='padding:6px;font-weight:bold'>Date &amp; Time:</td><td style='padding:6px'>" & DateTime.Now.ToString("dd-MMM-yyyy HH:mm") & "</td></tr>" &
                             "</table>" &
                             "<p>This account is now active and can be managed via the User Accounts page.</p>"

        For Each row As DataRow In dtAdmins.Rows
            Dim adminEmail As String = row("EmailId").ToString()
            If Not String.IsNullOrEmpty(adminEmail) Then
                SendEmail(adminEmail, subject, body)
            End If
        Next
    End Sub

    Public Shared Sub NotifyEmployeeOfDocumentApproval(ByVal employeeId As Integer, ByVal vehicleNumber As String, ByVal licenseType As String)
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT EmailId, EmployeeName FROM Employee WHERE EmployeeId = @EmpId", New SQLiteParameter("@EmpId", employeeId))
        If dt.Rows.Count = 0 Then Return
        Dim email As String = dt.Rows(0)("EmailId").ToString()
        Dim name As String = dt.Rows(0)("EmployeeName").ToString()
        Dim subject As String = "IOCL Vehicle Alert: Compliance Document Approved - " & vehicleNumber
        Dim body As String = "<h2>Compliance Document Approved</h2>" &
                              "<p>Dear " & name & ",</p>" &
                              "<p>The compliance document <strong>" & licenseType.Replace("_", " ") & "</strong> for vehicle <strong>" & vehicleNumber & "</strong> has been verified and approved.</p>"
        SendEmail(email, subject, body)
    End Sub

    Public Shared Sub NotifyEmployeeOfDocumentExpiry(ByVal employeeId As Integer, ByVal vehicleNumber As String, ByVal licenseType As String, ByVal expiryDate As String, ByVal daysRemaining As Integer)
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT EmailId, EmployeeName FROM Employee WHERE EmployeeId = @EmpId", New SQLiteParameter("@EmpId", employeeId))
        If dt.Rows.Count = 0 Then Return
        Dim email As String = dt.Rows(0)("EmailId").ToString()
        Dim name As String = dt.Rows(0)("EmployeeName").ToString()
        
        Dim docName As String = licenseType.Replace("_", " ")
        Dim subject As String = "IOCL Vehicle Renewal Reminder: " & docName & " for " & vehicleNumber
        
        Dim statusText As String
        If daysRemaining <= 0 Then
            statusText = "<p style='color: #dc2626; font-weight: bold;'>WARNING: This document has EXPIRED. The vehicle is blocked from entering the refinery.</p>"
        Else
            statusText = "<p style='color: #ea580c; font-weight: bold;'>NOTICE: This document is going to expire in " & daysRemaining & " days.</p>"
        End If
        
        Dim body As New System.Text.StringBuilder()
        body.Append("<div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #cbd5e1; border-radius: 8px;'>")
        body.Append("<h2 style='color: #001F5B;'>IOCL Vehicle Document Renewal Notification</h2>")
        body.Append("<p>Dear " & name & ",</p>")
        body.Append("<p>A security compliance review indicates that a safety document for vehicle <strong>" & vehicleNumber & "</strong> requires attention:</p>")
        body.Append("<table style='width: 100%; border-collapse: collapse; margin: 15px 0;'>")
        body.Append("<tr style='background: #f8fafc;'><td style='padding: 8px; border: 1px solid #cbd5e1; font-weight: bold;'>Document Type:</td><td style='padding: 8px; border: 1px solid #cbd5e1;'>" & docName & "</td></tr>")
        body.Append("<tr><td style='padding: 8px; border: 1px solid #cbd5e1; font-weight: bold;'>Expiry Date:</td><td style='padding: 8px; border: 1px solid #cbd5e1;'>" & expiryDate & "</td></tr>")
        body.Append("<tr style='background: #f8fafc;'><td style='padding: 8px; border: 1px solid #cbd5e1; font-weight: bold;'>Days Remaining:</td><td style='padding: 8px; border: 1px solid #cbd5e1;'>" & daysRemaining.ToString() & " days</td></tr>")
        body.Append("</table>")
        body.Append(statusText)
        body.Append("<p>Please log in to the Company Vehicle Management Portal and submit the renewal certificate as soon as possible.</p>")
        body.Append("<hr style='border: none; border-top: 1px solid #cbd5e1; margin: 20px 0;' />")
        body.Append("<p style='font-size: 11px; color: #64748b;'>This is a system generated email. Do not reply. Company Vehicle Management Department.</p>")
        body.Append("</div>")
        
        SendEmail(email, subject, body.ToString())
    End Sub

    ' ─────────────────────────────────────────────────────────────────────────
    ' Forgot Password OTP Email
    ' ─────────────────────────────────────────────────────────────────────────
    Public Shared Sub SendForgotPasswordOtp(ByVal toEmail As String, ByVal toName As String,
                                            ByVal otp As String, ByVal expiryMinutes As Integer)
        Dim subject As String = "Company Vehicle Management Portal - Password Reset OTP"

        Dim sb As New System.Text.StringBuilder()
        sb.Append("<!DOCTYPE html><html><head><meta charset='UTF-8'></head>")
        sb.Append("<body style='margin:0;padding:0;background:#0a0f1e;font-family:Arial,sans-serif;'>")
        sb.Append("<div style='max-width:520px;margin:40px auto;background:rgba(15,23,42,0.95);border:1px solid rgba(255,255,255,0.1);border-radius:16px;overflow:hidden;'>")

        ' Header bar
        sb.Append("<div style='background:linear-gradient(135deg,#0054A6,#0077cc);padding:24px 32px;'>")
        sb.Append("<h1 style='margin:0;font-size:18px;font-weight:700;color:#fff;letter-spacing:0.02em;'>Company Vehicle Management Portal</h1>")
        sb.Append("<p style='margin:4px 0 0;font-size:11px;color:rgba(255,255,255,0.8);letter-spacing:0.08em;text-transform:uppercase;'>Panipat Refinery</p>")
        sb.Append("</div>")

        ' Body
        sb.Append("<div style='padding:32px;'>")
        sb.Append("<p style='color:#94a3b8;font-size:13px;margin:0 0 6px;'>Dear <strong style='color:#e2e8f0;'>" & toName & "</strong>,</p>")
        sb.Append("<p style='color:#94a3b8;font-size:13px;margin:0 0 28px;'>We received a request to reset the password for your account. Use the One-Time Password below to proceed:</p>")

        ' OTP box
        sb.Append("<div style='text-align:center;margin:0 0 28px;'>")
        sb.Append("<div style='display:inline-block;background:rgba(0,84,166,0.12);border:2px dashed #0054A6;border-radius:12px;padding:20px 40px;'>")
        sb.Append("<div style='font-size:40px;font-weight:800;letter-spacing:12px;color:#0077cc;font-family:monospace;'>" & otp & "</div>")
        sb.Append("<p style='margin:8px 0 0;font-size:11px;color:#64748b;'>Expires in <strong style='color:#f59e0b;'>" & expiryMinutes & " minutes</strong></p>")
        sb.Append("</div></div>")

        sb.Append("<p style='color:#64748b;font-size:12px;margin:0 0 8px;'>&#9888; Do <strong>not</strong> share this OTP with anyone.</p>")
        sb.Append("</div>")

        ' Footer
        sb.Append("<div style='background:rgba(2,6,23,0.6);border-top:1px solid rgba(255,255,255,0.06);padding:16px 32px;'>")
        sb.Append("<p style='margin:0;font-size:10px;color:#334155;text-align:center;letter-spacing:0.05em;'>COMPANY VEHICLE MANAGEMENT SYSTEM &nbsp;|&nbsp; PANIPAT REFINERY &nbsp;|&nbsp; DO NOT REPLY</p>")
        sb.Append("</div>")
        sb.Append("</div></body></html>")

        SendEmail(toEmail, subject, sb.ToString())
    End Sub

    ' ─────────────────────────────────────────────────────────────────────────
    ' First-Login Email Verification OTP Email
    ' ─────────────────────────────────────────────────────────────────────────
    Public Shared Sub SendEmailVerificationOtp(ByVal toEmail As String, ByVal toName As String,
                                               ByVal otp As String)
        Dim subject As String = "Company Vehicle Management Portal - Email Verification OTP"

        Dim sb As New System.Text.StringBuilder()
        sb.Append("<!DOCTYPE html><html><head><meta charset='UTF-8'></head>")
        sb.Append("<body style='margin:0;padding:0;background:#0a0f1e;font-family:Arial,sans-serif;'>")
        sb.Append("<div style='max-width:520px;margin:40px auto;background:rgba(15,23,42,0.95);border:1px solid rgba(255,255,255,0.1);border-radius:16px;overflow:hidden;'>")

        ' Header bar
        sb.Append("<div style='background:linear-gradient(135deg,#0054A6,#0077cc);padding:24px 32px;'>")
        sb.Append("<h1 style='margin:0;font-size:18px;font-weight:700;color:#fff;letter-spacing:0.02em;'>Company Vehicle Management Portal</h1>")
        sb.Append("<p style='margin:4px 0 0;font-size:11px;color:rgba(255,255,255,0.8);letter-spacing:0.08em;text-transform:uppercase;'>Email Verification</p>")
        sb.Append("</div>")

        ' Body
        sb.Append("<div style='padding:32px;'>")
        sb.Append("<p style='color:#94a3b8;font-size:13px;margin:0 0 6px;'>Dear <strong style='color:#e2e8f0;'>" & toName & "</strong>,</p>")
        sb.Append("<p style='color:#94a3b8;font-size:13px;margin:0 0 8px;'>Please verify your email address by entering the OTP below in the portal:</p>")

        ' OTP box
        sb.Append("<div style='text-align:center;margin:24px 0;'>")
        sb.Append("<div style='display:inline-block;background:rgba(0,84,166,0.12);border:2px dashed #0054A6;border-radius:12px;padding:20px 40px;'>")
        sb.Append("<div style='font-size:40px;font-weight:800;letter-spacing:12px;color:#0077cc;font-family:monospace;'>" & otp & "</div>")
        sb.Append("<p style='margin:8px 0 0;font-size:11px;color:#64748b;'>Valid for <strong style='color:#f59e0b;'>15 minutes</strong></p>")
        sb.Append("</div></div>")

        sb.Append("</div>")

        ' Footer
        sb.Append("<div style='background:rgba(2,6,23,0.6);border-top:1px solid rgba(255,255,255,0.06);padding:16px 32px;'>")
        sb.Append("<p style='margin:0;font-size:10px;color:#334155;text-align:center;letter-spacing:0.05em;'>COMPANY VEHICLE MANAGEMENT SYSTEM &nbsp;|&nbsp; PANIPAT REFINERY &nbsp;|&nbsp; DO NOT REPLY</p>")
        sb.Append("</div>")
        sb.Append("</div></body></html>")

        SendEmail(toEmail, subject, sb.ToString())
    End Sub
End Class
