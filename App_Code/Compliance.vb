Imports System
Imports System.Data
Imports System.Data.SQLite
Imports System.Threading
Imports System.Collections.Generic

''' <summary>
''' Core compliance business logic + background scheduler.
''' Ported from ComplianceCheckHostedService.cs and ComplianceService.cs.
''' </summary>
Public Class Compliance

    ' ─────────────────────────────────────────────────────────────────────────
    ' Status Calculation (matches original 30/15/8/0 thresholds)
    ' ─────────────────────────────────────────────────────────────────────────
    Public Shared Function CalculateStatus(ByVal expiryDate As String) As String
        If String.IsNullOrEmpty(expiryDate) OrElse expiryDate = "PENDING" Then Return "ACTIVE"

        Dim expiry As DateTime
        If Not DateTime.TryParse(expiryDate, expiry) Then Return "ACTIVE"

        Dim today As DateTime = DateTime.Today
        Dim diffDays As Integer = Convert.ToInt32(Math.Ceiling((expiry.Date - today).TotalDays))

        If diffDays <= 0 Then Return "EXPIRED"
        If diffDays <= 7 Then Return "HIGH_CRITICAL"
        If diffDays <= 15 Then Return "MEDIUM_CRITICAL"
        If diffDays <= 30 Then Return "WARNING"
        Return "ACTIVE"
    End Function

    ' ─────────────────────────────────────────────────────────────────────────
    ' Update OverallStatus of a vehicle from its compliance records
    ' ─────────────────────────────────────────────────────────────────────────
    Public Shared Function UpdateVehicleStatus(ByVal vehicleId As Integer) As String
        Dim dt As DataTable = Database.ExecuteDataTable(
            "SELECT Id, ExpiryDate, Status FROM ComplianceRecords WHERE VehicleId = @VehId",
            New SQLiteParameter("@VehId", vehicleId))

        If dt.Rows.Count = 0 Then
            Database.ExecuteNonQuery("UPDATE Vehicles SET OverallStatus = 'FULLY_COMPLIANT', UpdatedAt = datetime('now') WHERE Id = @VehId",
                New SQLiteParameter("@VehId", vehicleId))
            Return "FULLY_COMPLIANT"
        End If

        Dim hasExpired As Boolean = False
        Dim hasCritical As Boolean = False
        Dim hasWarning As Boolean = False

        For Each row As DataRow In dt.Rows
            Dim recordId As Integer = Convert.ToInt32(row("Id"))
            Dim expiryDate As String = row("ExpiryDate").ToString()
            Dim currentStatus As String = row("Status").ToString()
            Dim computedStatus As String = CalculateStatus(expiryDate)

            If currentStatus <> computedStatus Then
                Database.ExecuteNonQuery(
                    "UPDATE ComplianceRecords SET Status = @Status, UpdatedAt = datetime('now') WHERE Id = @Id",
                    New SQLiteParameter("@Status", computedStatus),
                    New SQLiteParameter("@Id", recordId))
            End If

            Select Case computedStatus
                Case "EXPIRED" : hasExpired = True
                Case "HIGH_CRITICAL", "MEDIUM_CRITICAL" : hasCritical = True
                Case "WARNING" : hasWarning = True
            End Select
        Next

        Dim overall As String = "FULLY_COMPLIANT"
        If hasExpired Then
            overall = "EXPIRED"
        ElseIf hasCritical Then
            overall = "CRITICAL"
        ElseIf hasWarning Then
            overall = "WARNING"
        End If

        Database.ExecuteNonQuery(
            "UPDATE Vehicles SET OverallStatus = @Overall, UpdatedAt = datetime('now') WHERE Id = @VehId",
            New SQLiteParameter("@Overall", overall),
            New SQLiteParameter("@VehId", vehicleId))

        Return overall
    End Function



    ' ─────────────────────────────────────────────────────────────────────────
    ' Full compliance scan — updates statuses, creates notifications, sends emails
    ' Ported from ComplianceCheckHostedService.cs::RunComplianceCheck()
    ' ─────────────────────────────────────────────────────────────────────────
    Public Shared Sub RunComplianceCheck()
        Try
            Console.WriteLine("[ComplianceCheck] Starting compliance scan...")
            Dim today As DateTime = DateTime.Today

            Dim dt As DataTable = Database.ExecuteDataTable(
                "SELECT r.Id, r.VehicleId, r.LicenseType, r.ExpiryDate, r.Status, " &
                "v.VehicleNumber, v.VehicleType, v.Department As DeptName " &
                "FROM ComplianceRecords r " &
                "INNER JOIN Vehicles v ON r.VehicleId = v.Id")

            Dim alertCount As Integer = 0

            For Each row As DataRow In dt.Rows
                Dim expiryDate As String = row("ExpiryDate").ToString()
                If String.IsNullOrEmpty(expiryDate) OrElse expiryDate = "PENDING" Then Continue For

                Dim currentStatus As String = row("Status").ToString()
                Dim computedStatus As String = CalculateStatus(expiryDate)

                If currentStatus <> computedStatus Then
                    Dim recordId As Integer = Convert.ToInt32(row("Id"))
                    Dim vehicleId As Integer = Convert.ToInt32(row("VehicleId"))

                    Database.ExecuteNonQuery(
                        "UPDATE ComplianceRecords SET Status = @Status, LastUpdatedTimestamp = datetime('now'), UpdatedAt = datetime('now') WHERE Id = @Id",
                        New SQLiteParameter("@Status", computedStatus),
                        New SQLiteParameter("@Id", recordId))

                    UpdateVehicleStatus(vehicleId)

                    If computedStatus <> "ACTIVE" Then
                        alertCount += 1
                        Dim expiry As DateTime = DateTime.Parse(expiryDate)
                        Dim diffDays As Integer = Convert.ToInt32(Math.Ceiling((expiry.Date - today).TotalDays))
                        Dim vehicleNumber As String = row("VehicleNumber").ToString()
                        Dim licenseType As String = row("LicenseType").ToString()
                        Dim deptName As String = row("DeptName").ToString()
                        Dim vehType As String = row("VehicleType").ToString()

                        Dim alertMsg As String = licenseType & " certificate for vehicle " & vehicleNumber & " is now " & computedStatus & " (" & diffDays & " days remaining)."
                        Dim notifType As String = If(computedStatus = "EXPIRED", "EXPIRED", If(computedStatus = "WARNING", "WARNING", "CRITICAL"))

                        ' Create notification record
                        Database.ExecuteNonQuery(
                            "INSERT INTO Notifications (VehicleId, Department, Title, Message, Type, Status, CreatedAt) VALUES (@VehId, @Dept, @Title, @Msg, @Type, 'UNREAD', datetime('now'))",
                            New SQLiteParameter("@VehId", vehicleId),
                            New SQLiteParameter("@Dept", deptName),
                            New SQLiteParameter("@Title", "Compliance Alert: " & licenseType),
                            New SQLiteParameter("@Msg", alertMsg),
                            New SQLiteParameter("@Type", notifType))

                        ' Send email alerts ONLY to:
                        '   1. The employee who registered/added this vehicle
                        '   2. All SuperAdmin accounts
                        Try
                            Dim sentEmails As New List(Of String)()

                            ' --- Get vehicle owner (the person who added it) ---
                            Dim ownerDt As DataTable = Database.ExecuteDataTable(
                                "SELECT e.EmailId, e.EmployeeName FROM Vehicles v " &
                                "INNER JOIN Employee e ON v.EmployeeId = e.EmployeeId " &
                                "WHERE v.Id = @VehId LIMIT 1",
                                New SQLiteParameter("@VehId", vehicleId))

                            For Each ownerRow As DataRow In ownerDt.Rows
                                Dim email As String = ownerRow("EmailId").ToString()
                                Dim name As String = ownerRow("EmployeeName").ToString()
                                If Not String.IsNullOrEmpty(email) AndAlso Not sentEmails.Contains(email.ToLower()) Then
                                    sentEmails.Add(email.ToLower())
                                    Try
                                        EmailService.SendComplianceAlert(email, name, vehicleNumber, vehType, deptName,
                                            licenseType, expiryDate, diffDays, computedStatus)
                                    Catch emailEx As Exception
                                        Console.WriteLine("[ComplianceCheck] Failed to email owner " & email & ": " & emailEx.Message)
                                    End Try
                                End If
                            Next

                            ' --- Get all SuperAdmins ---
                            Dim adminDt As DataTable = Database.ExecuteDataTable(
                                "SELECT e.EmailId, e.EmployeeName FROM Authentication a " &
                                "INNER JOIN Employee e ON a.EmployeeId = e.EmployeeId " &
                                "WHERE a.Role = 'SuperAdmin'")

                            For Each adminRow As DataRow In adminDt.Rows
                                Dim email As String = adminRow("EmailId").ToString()
                                Dim name As String = adminRow("EmployeeName").ToString()
                                If Not String.IsNullOrEmpty(email) AndAlso Not sentEmails.Contains(email.ToLower()) Then
                                    sentEmails.Add(email.ToLower())
                                    Try
                                        EmailService.SendComplianceAlert(email, name, vehicleNumber, vehType, deptName,
                                            licenseType, expiryDate, diffDays, computedStatus)
                                    Catch emailEx As Exception
                                        Console.WriteLine("[ComplianceCheck] Failed to email SuperAdmin " & email & ": " & emailEx.Message)
                                    End Try
                                End If
                            Next

                        Catch userEx As Exception
                            Console.WriteLine("[ComplianceCheck] Failed to query recipients for alert emails: " & userEx.Message)
                        End Try
                    End If
                End If
            Next

            Console.WriteLine("[ComplianceCheck] Scan complete. Generated " & alertCount & " new alerts.")

        Catch ex As Exception
            Console.WriteLine("[ComplianceCheck] Error during scan: " & ex.Message)
        End Try
    End Sub

    ' ─────────────────────────────────────────────────────────────────────────
    ' Send daily digest emails to SuperAdmins and dept admins
    ' Ported from ComplianceCheckHostedService.cs::SendDailyDigestEmailsHosted()
    ' ─────────────────────────────────────────────────────────────────────────
    Public Shared Sub SendDailyDigest()
        Try
            Console.WriteLine("[ComplianceCheck] Sending daily digest emails...")

            ' Fleet summary
            Dim vehiclesDt As DataTable = Database.ExecuteDataTable("SELECT OverallStatus FROM Vehicles")
            Dim totalVehicles As Integer = vehiclesDt.Rows.Count
            Dim expiredCount As Integer = 0, criticalCount As Integer = 0, warningCount As Integer = 0
            For Each row As DataRow In vehiclesDt.Rows
                Select Case row("OverallStatus").ToString()
                    Case "EXPIRED" : expiredCount += 1
                    Case "CRITICAL" : criticalCount += 1
                    Case "WARNING" : warningCount += 1
                End Select
            Next

            Dim deptsDt As DataTable = Database.ExecuteDataTable(
                "SELECT e.Department As Name, COUNT(DISTINCT v.Id) As VehicleCount, " &
                "COALESCE(CAST(SUM(CASE WHEN r.Status = 'ACTIVE' OR r.Status = 'WARNING' THEN 1 ELSE 0 END) * 100.0 / COUNT(r.Id) AS REAL), 100.0) As ComplianceScore " &
                "FROM Employee e " &
                "LEFT JOIN Vehicles v ON e.EmployeeId = v.EmployeeId " &
                "LEFT JOIN ComplianceRecords r ON v.Id = r.VehicleId " &
                "WHERE e.Department IS NOT NULL AND e.Department <> '' " &
                "GROUP BY e.Department " &
                "ORDER BY ComplianceScore DESC")

            ' Add dynamic Id column for template compatibility
            deptsDt.Columns.Add("Id", GetType(String))
            For Each r As DataRow In deptsDt.Rows
                r("Id") = r("Name").ToString()
            Next

            Dim expiringDt As DataTable = Database.ExecuteDataTable(
                "SELECT r.Id, r.VehicleId, r.LicenseType, r.LicenseNumber, r.IssuingAuthority, r.ExpiryDate, r.Status, " &
                "v.VehicleNumber, v.Department As DeptName " &
                "FROM ComplianceRecords r " &
                "INNER JOIN Vehicles v ON r.VehicleId = v.Id " &
                "WHERE r.Status IN ('EXPIRED', 'HIGH_CRITICAL', 'MEDIUM_CRITICAL', 'WARNING') " &
                "ORDER BY r.ExpiryDate")

            Dim usersDt As DataTable = Database.ExecuteDataTable(
                "SELECT e.EmailId, e.EmployeeName, a.Role, e.Department FROM Authentication a " &
                "INNER JOIN Employee e ON a.EmployeeId = e.EmployeeId " &
                "WHERE a.Role IN ('SuperAdmin', 'Admin') AND e.EmailId IS NOT NULL AND e.EmailId <> ''")

            For Each userRow As DataRow In usersDt.Rows
                Try
                    Dim email As String = userRow("EmailId").ToString()
                    Dim name As String = userRow("EmployeeName").ToString()
                    Dim role As String = userRow("Role").ToString()
                    Dim dept As String = userRow("Department").ToString()

                    Dim userDeptsDt As DataTable = deptsDt
                    Dim userExpiringDt As DataTable = expiringDt
                    Dim userTotal As Integer = totalVehicles
                    Dim userExpired As Integer = expiredCount
                    Dim userCritical As Integer = criticalCount
                    Dim userWarning As Integer = warningCount

                    ' Scope DEPT_ADMIN to their own department
                    If role = "Admin" AndAlso Not String.IsNullOrEmpty(dept) Then
                        Dim filteredDepts As DataTable = deptsDt.Clone()
                        For Each row As DataRow In deptsDt.Rows
                            If row("Name").ToString() = dept Then filteredDepts.ImportRow(row)
                        Next
                        userDeptsDt = filteredDepts

                        Dim filteredExpiring As DataTable = expiringDt.Clone()
                        userExpired = 0 : userCritical = 0 : userWarning = 0 : userTotal = 0
                        For Each row As DataRow In expiringDt.Rows
                            If row("DeptName").ToString() = dept Then
                                filteredExpiring.ImportRow(row)
                                Select Case row("Status").ToString()
                                    Case "EXPIRED" : userExpired += 1
                                    Case "HIGH_CRITICAL", "MEDIUM_CRITICAL" : userCritical += 1
                                    Case "WARNING" : userWarning += 1
                                End Select
                            End If
                        Next
                        userExpiringDt = filteredExpiring

                        Dim vCount As Object = Database.ExecuteScalar(
                            "SELECT COUNT(*) FROM Vehicles WHERE Department = @Dept",
                            New SQLiteParameter("@Dept", dept))
                        userTotal = If(vCount IsNot Nothing, Convert.ToInt32(vCount), 0)
                    End If

                    If Not String.IsNullOrEmpty(email) Then
                        EmailService.SendDailySummary(email, name, userTotal, userExpired, userCritical, userWarning, userDeptsDt, userExpiringDt)
                    End If
                Catch ex As Exception
                    Console.WriteLine("[ComplianceCheck] Digest email error for " & userRow("EmailId").ToString() & ": " & ex.Message)
                End Try
            Next

        Catch ex As Exception
            Console.WriteLine("[ComplianceCheck] Daily digest failed: " & ex.Message)
        End Try
    End Sub

    ' ─────────────────────────────────────────────────────────────────────────
    ' Background Timer — runs compliance check on startup + every 12 hours
    ' Called from Global.asax Application_Start
    ' ─────────────────────────────────────────────────────────────────────────
    Private Shared _schedulerThread As Thread = Nothing
    Private Shared _lastDigestDate As DateTime = DateTime.MinValue

    Public Shared Sub StartBackgroundScheduler()
        If _schedulerThread IsNot Nothing AndAlso _schedulerThread.IsAlive Then Return

        _schedulerThread = New Thread(Sub()
            ' Initial 5-second delay after startup
            Thread.Sleep(5000)
            Console.WriteLine("[ComplianceCheck] Initial scan starting...")
            Try
                RunComplianceCheck()
            Catch ex As Exception
                Console.WriteLine("[ComplianceCheck] Initial scan error: " & ex.Message)
            End Try

            Do
                ' Check if daily digest should be sent (once per calendar day)
                If DateTime.Today > _lastDigestDate.Date Then
                    Try
                        SendDailyDigest()
                        _lastDigestDate = DateTime.Today
                    Catch ex As Exception
                        Console.WriteLine("[ComplianceCheck] Digest error: " & ex.Message)
                    End Try
                End If

                ' Sleep 12 hours then repeat
                Thread.Sleep(TimeSpan.FromHours(12))
                Console.WriteLine("[ComplianceCheck] Periodic 12h scan starting...")
                Try
                    RunComplianceCheck()
                Catch ex As Exception
                    Console.WriteLine("[ComplianceCheck] Periodic scan error: " & ex.Message)
                End Try
            Loop
        End Sub)

        _schedulerThread.IsBackground = True
        _schedulerThread.Name = "ComplianceCheckScheduler"
        _schedulerThread.Start()
        Console.WriteLine("[ComplianceCheck] Background scheduler started.")
    End Sub
End Class
