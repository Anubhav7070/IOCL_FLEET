Imports System
Imports System.Data
Imports System.Data.SQLite
Imports System.Threading
Imports System.Collections.Generic

Public Class Compliance

    ' Calculate status based on license type and expiry date
    Public Shared Function CalculateStatus(ByVal licenseType As String, ByVal expiryDate As String) As String
        If String.IsNullOrEmpty(expiryDate) OrElse expiryDate = "PENDING" Then Return "Valid"
 
        Dim expiry As DateTime
        If Not DateTime.TryParse(expiryDate, expiry) Then Return "Valid"
 
        Dim today As DateTime = DateTime.Today
        Dim diffDays As Integer = Convert.ToInt32(Math.Ceiling((expiry.Date - today).TotalDays))
 
        If diffDays <= 0 Then Return "Expired"
 
        Dim buffer As Integer = 0
        Select Case licenseType.ToUpper()
            Case "RC"
                buffer = 365
            Case "INSURANCE"
                buffer = 20
            Case "PUCC"
                buffer = 10
            Case "FITNESS"
                buffer = 30
            Case Else
                buffer = 0
        End Select
 
        If diffDays <= buffer Then Return "Expiring"
        Return "Valid"
    End Function

    ' Update overall status of a vehicle from its compliance records
    Public Shared Function UpdateVehicleStatus(ByVal vehicleId As Integer) As String
        Dim dt As DataTable = Database.ExecuteDataTable(
            "SELECT Id, LicenseType, ExpiryDate, Status FROM ComplianceRecords WHERE VehicleId = @VehId",
            New SQLiteParameter("@VehId", vehicleId))
 
        If dt.Rows.Count = 0 Then
            Database.ExecuteNonQuery("UPDATE Vehicles SET OverallStatus = 'Valid', UpdatedAt = datetime('now') WHERE Id = @VehId",
                New SQLiteParameter("@VehId", vehicleId))
            Return "Valid"
        End If
 
        Dim hasExpired As Boolean = False
        Dim hasExpiring As Boolean = False
 
        For Each row As DataRow In dt.Rows
            Dim recordId As Integer = Convert.ToInt32(row("Id"))
            Dim licType As String = row("LicenseType").ToString()
            Dim expiryDate As String = row("ExpiryDate").ToString()
            Dim currentStatus As String = row("Status").ToString()
            Dim computedStatus As String = CalculateStatus(licType, expiryDate)
 
            If currentStatus <> computedStatus Then
                Database.ExecuteNonQuery(
                    "UPDATE ComplianceRecords SET Status = @Status, UpdatedAt = datetime('now') WHERE Id = @Id",
                    New SQLiteParameter("@Status", computedStatus),
                    New SQLiteParameter("@Id", recordId))
            End If
 
            Select Case computedStatus
                Case "Expired" : hasExpired = True
                Case "Expiring" : hasExpiring = True
            End Select
        Next
 
        Dim overall As String = "Valid"
        If hasExpired Then
            overall = "Expired"
        ElseIf hasExpiring Then
            overall = "Expiring"
        End If
 
        Database.ExecuteNonQuery(
            "UPDATE Vehicles SET OverallStatus = @Overall, UpdatedAt = datetime('now') WHERE Id = @VehId",
            New SQLiteParameter("@Overall", overall),
            New SQLiteParameter("@VehId", vehicleId))
 
        Return overall
    End Function

    ' Run a full compliance scan across all database records
    Public Shared Sub RunComplianceCheck()
        Try
            Console.WriteLine("[ComplianceCheck] Starting compliance scan...")
            Dim dt As DataTable = Database.ExecuteDataTable("SELECT r.Id, r.VehicleId, r.LicenseType, r.ExpiryDate, r.Status FROM ComplianceRecords r INNER JOIN Vehicles v ON r.VehicleId = v.Id WHERE (v.IsDecommissioned = 0 OR v.IsDecommissioned IS NULL)")
            For Each row As DataRow In dt.Rows
                Dim recordId As Integer = Convert.ToInt32(row("Id"))
                Dim vehicleId As Integer = Convert.ToInt32(row("VehicleId"))
                Dim licType As String = row("LicenseType").ToString()
                Dim expiryDate As String = row("ExpiryDate").ToString()
                Dim currentStatus As String = row("Status").ToString()
                
                Dim computedStatus As String = CalculateStatus(licType, expiryDate)
                If currentStatus <> computedStatus Then
                    Database.ExecuteNonQuery(
                        "UPDATE ComplianceRecords SET Status = @Status, LastUpdatedTimestamp = datetime('now'), UpdatedAt = datetime('now') WHERE Id = @Id",
                        New SQLiteParameter("@Status", computedStatus),
                        New SQLiteParameter("@Id", recordId))
                    UpdateVehicleStatus(vehicleId)
                End If
            Next
            Console.WriteLine("[ComplianceCheck] Compliance scan finished.")
        Catch ex As Exception
            Console.WriteLine("[ComplianceCheck] Error during scan: " & ex.Message)
        End Try
    End Sub

    ' Send daily consolidated digests to Vehicle Owners and Super Admins
    Public Shared Sub SendDailyDigest()
        Try
            Console.WriteLine("[ComplianceCheck] Preparing daily consolidated digest emails...")
            
            ' Fetch all expiring (Expiring) or expired documents, joining Employee to identify vehicle owners
            Dim sql As String = "SELECT r.LicenseType, r.ExpiryDate, r.Status, v.VehicleNumber, v.VehicleType, v.EmployeeId As OwnerId, emp.EmployeeName As OwnerName, emp.EmailId As OwnerEmail, v.Department As AllocatedDept " &
                               "FROM ComplianceRecords r " &
                               "INNER JOIN Vehicles v ON r.VehicleId = v.Id " &
                               "INNER JOIN Employee emp ON v.EmployeeId = emp.EmployeeId " &
                               "WHERE r.Status IN ('Expiring', 'Expired') AND (v.IsDecommissioned = 0 OR v.IsDecommissioned IS NULL) " &
                               "ORDER BY v.VehicleNumber, r.LicenseType"
            Dim dt As DataTable = Database.ExecuteDataTable(sql)
 
            ' 1. Group alerts by OwnerId and email each Vehicle Owner their specific alerts
            Dim ownerGroups As New Dictionary(Of Integer, List(Of DataRow))()
            Dim ownerDetails As New Dictionary(Of Integer, Tuple(Of String, String))()
 
            For Each row As DataRow In dt.Rows
                Dim ownerId As Integer = Convert.ToInt32(row("OwnerId"))
                Dim ownerName As String = row("OwnerName").ToString()
                Dim ownerEmail As String = row("OwnerEmail").ToString()
 
                If Not ownerGroups.ContainsKey(ownerId) Then
                    ownerGroups(ownerId) = New List(Of DataRow)()
                    ownerDetails(ownerId) = Tuple.Create(ownerName, ownerEmail)
                End If
                ownerGroups(ownerId).Add(row)
            Next
 
            For Each kvp As KeyValuePair(Of Integer, List(Of DataRow)) In ownerGroups
                Dim ownerId As Integer = kvp.Key
                Dim details As Tuple(Of String, String) = ownerDetails(ownerId)
                Dim name As String = details.Item1
                Dim email As String = details.Item2
                Dim ownerRows As List(Of DataRow) = kvp.Value
 
                If Not String.IsNullOrEmpty(email) Then
                    Dim ownerDt As DataTable = dt.Clone()
                    For Each r As DataRow In ownerRows
                        ownerDt.ImportRow(r)
                    Next
                    Try
                        EmailService.SendConsolidatedDigest(email, name, "Vehicle Owner", ownerDt)
                    Catch ex As Exception
                        Console.WriteLine("[ComplianceCheck] Failed to send owner digest to " & email & ". Error: " & ex.Message)
                    End Try
                End If
            Next
 
            ' 2. Send Consolidated view-only master report to all Super Admins
            Dim superAdmins As DataTable = Database.ExecuteDataTable(
                "SELECT e.EmailId, e.EmployeeName FROM Authentication a INNER JOIN Employee e ON a.EmployeeId = e.EmployeeId WHERE a.Role = 'SuperAdmin' AND e.EmailId IS NOT NULL AND e.EmailId <> ''")
            
            For Each adminRow As DataRow In superAdmins.Rows
                Dim email As String = adminRow("EmailId").ToString()
                Dim name As String = adminRow("EmployeeName").ToString()
                Try
                    EmailService.SendConsolidatedDigest(email, name, "Super Admin (Master Report)", dt)
                Catch ex As Exception
                    Console.WriteLine("[ComplianceCheck] Failed to send consolidated digest to Super Admin: " & email & ". Error: " & ex.Message)
                End Try
            Next
 
            Console.WriteLine("[ComplianceCheck] Consolidated digests sent.")
        Catch ex As Exception
            Console.WriteLine("[ComplianceCheck] Consolidated email dispatch failed: " & ex.Message)
        End Try
    End Sub

    ' Send individual dynamic reminders and daily escalations as per matrix rules
    Public Shared Sub SendReminderAndEscalationEmails()
        Try
            Console.WriteLine("[ComplianceCheck] Running reminder and escalation matrix email scans...")
            
            ' Fetch global settings (HR1, HR2, Central Compliance Team Email)
            Dim dtConfig As DataTable = Database.ExecuteDataTable("SELECT * FROM SystemConfiguration LIMIT 1")
            Dim hr1Name As String = "HR Admin 1"
            Dim hr1Email As String = "hr1@iocl.co.in"
            Dim hr2Name As String = "HR Admin 2"
            Dim hr2Email As String = "hr2@iocl.co.in"
            Dim complianceEmail As String = "compliance@iocl.co.in"
            
            If dtConfig.Rows.Count > 0 Then
                Dim confRow As DataRow = dtConfig.Rows(0)
                hr1Name = If(confRow("Hr1Name") Is DBNull.Value, hr1Name, confRow("Hr1Name").ToString())
                hr1Email = If(confRow("Hr1Email") Is DBNull.Value, hr1Email, confRow("Hr1Email").ToString())
                hr2Name = If(confRow("Hr2Name") Is DBNull.Value, hr2Name, confRow("Hr2Name").ToString())
                hr2Email = If(confRow("Hr2Email") Is DBNull.Value, hr2Email, confRow("Hr2Email").ToString())
                complianceEmail = If(confRow("CentralComplianceEmail") Is DBNull.Value, complianceEmail, confRow("CentralComplianceEmail").ToString())
            End If

            ' Fetch all vehicle compliance documents
            Dim sql As String = "SELECT r.Id, r.VehicleId, r.LicenseType, r.LicenseNumber, r.ExpiryDate, r.LastAlertSent, r.Status, " &
                                "v.VehicleNumber, v.VehicleType, v.EmployeeId As OwnerId, " &
                                "emp.EmployeeName As OwnerName, emp.EmailId As OwnerEmail, emp.ManagerEmail, emp.HodEmail, emp.GmEmail, emp.CgmEmail, " &
                                "v.Department As AllocatedDept " &
                                "FROM ComplianceRecords r " &
                                "INNER JOIN Vehicles v ON r.VehicleId = v.Id " &
                                "INNER JOIN Employee emp ON v.EmployeeId = emp.EmployeeId " &
                                "WHERE (v.IsDecommissioned = 0 OR v.IsDecommissioned IS NULL) " &
                                "ORDER BY v.VehicleNumber, r.LicenseType"
            Dim dtRecords As DataTable = Database.ExecuteDataTable(sql)
            Dim today As DateTime = DateTime.Today

            For Each row As DataRow In dtRecords.Rows
                Dim recordId As Integer = Convert.ToInt32(row("Id"))
                Dim licenseType As String = row("LicenseType").ToString()
                Dim expiryDateStr As String = If(row("ExpiryDate") Is DBNull.Value, "", row("ExpiryDate").ToString())
                Dim lastAlertSentStr As String = If(row("LastAlertSent") Is DBNull.Value, "", row("LastAlertSent").ToString())
                
                Dim expiryDate As DateTime
                If Not DateTime.TryParse(expiryDateStr, expiryDate) Then Continue For
                
                Dim diffDays As Integer = Convert.ToInt32(Math.Ceiling((expiryDate.Date - today).TotalDays))
                Dim lastAlertSent As DateTime = DateTime.MinValue
                Dim hasLastAlert As Boolean = DateTime.TryParse(lastAlertSentStr, lastAlertSent)
                
                Dim ownerName As String = row("OwnerName").ToString()
                Dim ownerEmail As String = row("OwnerEmail").ToString()
                Dim managerEmail As String = If(row("ManagerEmail") Is DBNull.Value, "", row("ManagerEmail").ToString())
                Dim hodEmail As String = If(row("HodEmail") Is DBNull.Value, "", row("HodEmail").ToString())
                Dim gmEmail As String = If(row("GmEmail") Is DBNull.Value, "", row("GmEmail").ToString())
                Dim cgmEmail As String = If(row("CgmEmail") Is DBNull.Value, "", row("CgmEmail").ToString())
                Dim vehicleNumber As String = row("VehicleNumber").ToString()
                Dim vehicleType As String = row("VehicleType").ToString()
                
                Dim shouldSend As Boolean = False
                Dim subject As String = ""
                Dim body As New System.Text.StringBuilder()
                
                Dim toEmails As New List(Of String)()
                Dim ccEmails As New List(Of String)()
                
                If diffDays > 0 Then
                    ' Before Expiry reminder matrix
                    Dim startDays As Integer = 0
                    Dim freqDays As Integer = 0
                    Select Case licenseType.ToUpper()
                        Case "PUCC"
                            startDays = 10
                            freqDays = 3
                        Case "INSURANCE"
                            startDays = 20
                            freqDays = 5
                        Case "FITNESS"
                            startDays = 30
                            freqDays = 7
                        Case "RC"
                            startDays = 365
                            freqDays = 30
                    End Select
                    
                    If diffDays <= startDays Then
                        ' Send if no previous alert or interval reached
                        If Not hasLastAlert OrElse (today - lastAlertSent.Date).TotalDays >= freqDays Then
                            shouldSend = True
                            subject = "Reminder: Renewal Required for " & licenseType.Replace("_", " ") & " of Vehicle " & vehicleNumber
                            
                            body.Append("<div style='font-family:Arial,sans-serif; padding:20px; border:1px solid #cbd5e1; border-radius:8px;'>")
                            body.Append("<h2 style='color:#001F5B;'>Vehicle Document Renewal Reminder</h2>")
                            body.Append("<p>Dear " & ownerName & ",</p>")
                            body.Append("<p>This is a reminder that the <strong>" & licenseType.Replace("_", " ") & "</strong> for vehicle <strong>" & vehicleNumber & "</strong> is expiring in <strong>" & diffDays & " days</strong> on " & expiryDate.ToString("dd-MMM-yyyy") & ".</p>")
                            body.Append("<p>Please renew it as soon as possible and log the new validity dates in the portal.</p>")
                            body.Append("<hr style='border:none; border-top:1px solid #cbd5e1; margin:20px 0;' />")
                            body.Append("<p style='font-size:10px; color:#64748b;'>This is a system generated email. Do not reply.</p>")
                            body.Append("</div>")
                            
                            ' Dynamic recipients before expiry
                            toEmails.Add(ownerEmail)
                            If Not String.IsNullOrEmpty(managerEmail) Then ccEmails.Add(managerEmail)
                            If Not String.IsNullOrEmpty(hr1Email) Then ccEmails.Add(hr1Email)
                            If Not String.IsNullOrEmpty(hr2Email) Then ccEmails.Add(hr2Email)
                        End If
                    End If
                Else
                    ' After Expiry / Escalation daily reminders (diffDays <= 0)
                    If Not hasLastAlert OrElse lastAlertSent.Date < today Then
                        shouldSend = True
                        subject = "ESCALATION: Expired Document Alert for Vehicle " & vehicleNumber & " - " & licenseType.Replace("_", " ")
                        
                        body.Append("<div style='font-family:Arial,sans-serif; padding:20px; border:1px solid #dc2626; border-radius:8px;'>")
                        body.Append("<h2 style='color:#dc2626;'>EXPIRED DOCUMENT ESCALATION ALERT</h2>")
                        body.Append("<p>Dear " & ownerName & ",</p>")
                        body.Append("<p>This is to inform you that the safety document <strong>" & licenseType.Replace("_", " ") & "</strong> for vehicle <strong>" & vehicleNumber & "</strong> has <strong>EXPIRED</strong> as of " & expiryDate.ToString("dd-MMM-yyyy") & " (overdue by " & Math.Abs(diffDays) & " days).</p>")
                        body.Append("<p style='font-weight:bold; color:#dc2626;'>Action is required immediately. The vehicle is blocked from entry until renewed.</p>")
                        body.Append("<hr style='border:none; border-top:1px solid #cbd5e1; margin:20px 0;' />")
                        body.Append("<p style='font-size:10px; color:#64748b;'>This is a system generated escalation. CC'ed to HOD, GM, CGM, and HR compliance team.</p>")
                        body.Append("</div>")
                        
                        ' Dynamic recipients after expiry
                        toEmails.Add(ownerEmail)
                        toEmails.Add(complianceEmail)
                        If Not String.IsNullOrEmpty(hodEmail) Then ccEmails.Add(hodEmail)
                        If Not String.IsNullOrEmpty(gmEmail) Then ccEmails.Add(gmEmail)
                        If Not String.IsNullOrEmpty(cgmEmail) Then ccEmails.Add(cgmEmail)
                        If Not String.IsNullOrEmpty(hr1Email) Then ccEmails.Add(hr1Email)
                        If Not String.IsNullOrEmpty(hr2Email) Then ccEmails.Add(hr2Email)
                    End If
                End If
                
                If shouldSend Then
                    Try
                        EmailService.SendIndividualReminderEmail(toEmails, ccEmails, subject, body.ToString())
                        
                        ' Record last alert sent date
                        Database.ExecuteNonQuery("UPDATE ComplianceRecords SET LastAlertSent = @Today, UpdatedAt = datetime('now') WHERE Id = @Id",
                            New SQLiteParameter("@Today", today.ToString("yyyy-MM-dd")),
                            New SQLiteParameter("@Id", recordId))
                    Catch ex As Exception
                        Console.WriteLine("[ComplianceCheck] Error sending alert for record " & recordId & ": " & ex.Message)
                    End Try
                End If
            Next
            Console.WriteLine("[ComplianceCheck] Reminder and escalation scan completed.")
        Catch ex As Exception
            Console.WriteLine("[ComplianceCheck] Reminder scan failed: " & ex.Message)
        End Try
    End Sub

    ' Background Scheduler Thread: runs compliance scan hourly & triggers digests/reminders at exactly 10:00 AM
    Private Shared _schedulerThread As Thread = Nothing
    Private Shared _lastDigestDate As DateTime = DateTime.MinValue

    Public Shared Sub StartBackgroundScheduler()
        If _schedulerThread IsNot Nothing AndAlso _schedulerThread.IsAlive Then Return

        _schedulerThread = New Thread(Sub()
            ' Initial 5-second delay after startup
            Thread.Sleep(5000)
            Console.WriteLine("[ComplianceCheck] Initial background compliance check starting...")
            Try
                RunComplianceCheck()
            Catch ex As Exception
                Console.WriteLine("[ComplianceCheck] Initial scan error: " & ex.Message)
            End Try

            Do
                ' Scan compliance status hourly
                Try
                    RunComplianceCheck()
                Catch ex As Exception
                    Console.WriteLine("[ComplianceCheck] Periodic hourly check error: " & ex.Message)
                End Try

                ' Trigger consolidated alerts & dynamic reminders daily at 10:00 AM
                Dim now As DateTime = DateTime.Now
                If now.Hour = 10 AndAlso now.Date > _lastDigestDate.Date Then
                    Try
                        SendDailyDigest()
                        SendReminderAndEscalationEmails()
                        _lastDigestDate = now.Date
                    Catch ex As Exception
                        Console.WriteLine("[ComplianceCheck] Scheduled digest/reminder error: " & ex.Message)
                    End Try
                End If

                ' Sleep 5 minutes before checking again
                Thread.Sleep(TimeSpan.FromMinutes(5))
            Loop
        End Sub)

        _schedulerThread.IsBackground = True
        _schedulerThread.Name = "ComplianceCheckScheduler"
        _schedulerThread.Start()
        Console.WriteLine("[ComplianceCheck] Background scheduler started.")
    End Sub
End Class
