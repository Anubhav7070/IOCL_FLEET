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
            Dim todayStr As String = today.ToString("yyyy-MM-dd")

            Dim dt As DataTable = Database.ExecuteDataTable(
                "SELECT r.Id, r.VehicleId, r.LicenseType, r.ExpiryDate, r.Status, r.LastAlertSent, " &
                "v.VehicleNumber, v.VehicleType, v.Department As DeptName " &
                "FROM ComplianceRecords r " &
                "INNER JOIN Vehicles v ON r.VehicleId = v.Id")

            ' Step 1: Update statuses for all records
            For Each row As DataRow In dt.Rows
                Dim expiryDate As String = row("ExpiryDate").ToString()
                If String.IsNullOrEmpty(expiryDate) OrElse expiryDate = "PENDING" Then Continue For

                Dim currentStatus As String = row("Status").ToString()
                Dim computedStatus As String = CalculateStatus(expiryDate)
                Dim recordId As Integer = Convert.ToInt32(row("Id"))
                Dim vehicleId As Integer = Convert.ToInt32(row("VehicleId"))

                If currentStatus <> computedStatus Then
                    Database.ExecuteNonQuery(
                        "UPDATE ComplianceRecords SET Status = @Status, LastUpdatedTimestamp = datetime('now'), UpdatedAt = datetime('now') WHERE Id = @Id",
                        New SQLiteParameter("@Status", computedStatus),
                        New SQLiteParameter("@Id", recordId))
                    UpdateVehicleStatus(vehicleId)
                    ' Refresh value in DataTable for grouping step below
                    row("Status") = computedStatus
                End If
            Next

            ' Step 2: Group non-active records by vehicle and send ONE merged notification per vehicle
            ' Build a dictionary: VehicleId -> list of alert rows
            Dim vehicleGroups As New Dictionary(Of Integer, List(Of DataRow))()
            For Each row As DataRow In dt.Rows
                Dim status As String = row("Status").ToString()
                If status = "ACTIVE" Then Continue For
                Dim expiryDate As String = row("ExpiryDate").ToString()
                If String.IsNullOrEmpty(expiryDate) OrElse expiryDate = "PENDING" Then Continue For

                Dim vehicleId As Integer = Convert.ToInt32(row("VehicleId"))
                Dim lastAlertSent As String = If(row("LastAlertSent") Is DBNull.Value, "", row("LastAlertSent").ToString())
                ' Only include records that haven't been alerted today
                If lastAlertSent = todayStr Then Continue For

                If Not vehicleGroups.ContainsKey(vehicleId) Then
                    vehicleGroups(vehicleId) = New List(Of DataRow)()
                End If
                vehicleGroups(vehicleId).Add(row)
            Next

            Dim alertCount As Integer = 0

            For Each kvp As KeyValuePair(Of Integer, List(Of DataRow)) In vehicleGroups
                Dim vehicleId As Integer = kvp.Key
                Dim alertRows As List(Of DataRow) = kvp.Value
                If alertRows.Count = 0 Then Continue For

                Dim firstRow As DataRow = alertRows(0)
                Dim vehicleNumber As String = firstRow("VehicleNumber").ToString()
                Dim vehType As String = firstRow("VehicleType").ToString()
                Dim deptName As String = firstRow("DeptName").ToString()

                ' Build combined message for in-app notification
                Dim docList As String = String.Join(", ", alertRows.ConvertAll(Function(r) r("LicenseType").ToString().Replace("_", " ")).ToArray())
                Dim notifTitle As String = "Compliance Alert: " & vehicleNumber & " (" & alertRows.Count & " document(s))"
                Dim notifMsg As String = "Vehicle " & vehicleNumber & " has " & alertRows.Count & " expiring/expired certificate(s): " & docList & "."

                ' Insert ONE merged notification for the vehicle
                Database.ExecuteNonQuery(
                    "INSERT INTO Notifications (VehicleId, Department, Title, Message, Type, Status, CreatedAt) VALUES (@VehId, @Dept, @Title, @Msg, 'CRITICAL', 'UNREAD', datetime('now'))",
                    New SQLiteParameter("@VehId", vehicleId),
                    New SQLiteParameter("@Dept", deptName),
                    New SQLiteParameter("@Title", notifTitle),
                    New SQLiteParameter("@Msg", notifMsg))

                alertCount += 1

                ' Build a DataTable of alert docs to pass to merged email
                Dim docsDt As New DataTable()
                docsDt.Columns.Add("LicenseType", GetType(String))
                docsDt.Columns.Add("ExpiryDate", GetType(String))
                docsDt.Columns.Add("Status", GetType(String))
                For Each alertRow As DataRow In alertRows
                    Dim newRow As DataRow = docsDt.NewRow()
                    newRow("LicenseType") = alertRow("LicenseType").ToString()
                    newRow("ExpiryDate") = alertRow("ExpiryDate").ToString()
                    newRow("Status") = alertRow("Status").ToString()
                    docsDt.Rows.Add(newRow)
                Next

                ' Send merged email to vehicle owner + SuperAdmins (deduped)
                Try
                    Dim sentEmails As New List(Of String)()

                    ' 1. Vehicle owner
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
                                EmailService.SendMergedComplianceAlert(email, name, vehicleNumber, vehType, deptName, docsDt)
                            Catch emailEx As Exception
                                Console.WriteLine("[ComplianceCheck] Failed to email owner " & email & ": " & emailEx.Message)
                            End Try
                        End If
                    Next

                    ' 2. All SuperAdmins
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
                                EmailService.SendMergedComplianceAlert(email, name, vehicleNumber, vehType, deptName, docsDt)
                            Catch emailEx As Exception
                                Console.WriteLine("[ComplianceCheck] Failed to email SuperAdmin " & email & ": " & emailEx.Message)
                            End Try
                        End If
                    Next

                    ' Mark all alerted records as sent today
                    For Each alertRow As DataRow In alertRows
                        Dim recordId As Integer = Convert.ToInt32(alertRow("Id"))
                        Database.ExecuteNonQuery(
                            "UPDATE ComplianceRecords SET LastAlertSent = @LastAlert, UpdatedAt = datetime('now') WHERE Id = @Id",
                            New SQLiteParameter("@LastAlert", todayStr),
                            New SQLiteParameter("@Id", recordId))
                    Next

                Catch userEx As Exception
                    Console.WriteLine("[ComplianceCheck] Failed to send merged alert for vehicle " & vehicleNumber & ": " & userEx.Message)
                End Try
            Next

            Console.WriteLine("[ComplianceCheck] Scan complete. Generated " & alertCount & " vehicle-grouped alerts.")

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
