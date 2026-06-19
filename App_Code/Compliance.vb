Imports System
Imports System.Data
Imports System.Data.SQLite
Imports System.Threading
Imports System.Collections.Generic

Public Class Compliance

    ' Calculate status based on license type and expiry date
    Public Shared Function CalculateStatus(ByVal licenseType As String, ByVal expiryDate As String) As String
        If String.IsNullOrEmpty(expiryDate) OrElse expiryDate = "PENDING" Then Return "Compliant"

        Dim expiry As DateTime
        If Not DateTime.TryParse(expiryDate, expiry) Then Return "Compliant"

        Dim today As DateTime = DateTime.Today
        Dim diffDays As Integer = Convert.ToInt32(Math.Ceiling((expiry.Date - today).TotalDays))

        If diffDays <= 0 Then Return "Expired"

        Dim buffer As Integer = 0
        Select Case licenseType.ToUpper()
            Case "RC"
                buffer = 30
            Case "INSURANCE"
                buffer = 10
            Case "PUCC"
                buffer = 5
            Case Else
                buffer = 0
        End Select

        If diffDays <= buffer Then Return "Non-Compliant"
        Return "Compliant"
    End Function

    ' Update overall status of a vehicle from its compliance records
    Public Shared Function UpdateVehicleStatus(ByVal vehicleId As Integer) As String
        Dim dt As DataTable = Database.ExecuteDataTable(
            "SELECT Id, LicenseType, ExpiryDate, Status FROM ComplianceRecords WHERE VehicleId = @VehId",
            New SQLiteParameter("@VehId", vehicleId))

        If dt.Rows.Count = 0 Then
            Database.ExecuteNonQuery("UPDATE Vehicles SET OverallStatus = 'Compliant', UpdatedAt = datetime('now') WHERE Id = @VehId",
                New SQLiteParameter("@VehId", vehicleId))
            Return "Compliant"
        End If

        Dim hasExpired As Boolean = False
        Dim hasNonCompliant As Boolean = False

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
                Case "Non-Compliant" : hasNonCompliant = True
            End Select
        Next

        Dim overall As String = "Compliant"
        If hasExpired Then
            overall = "Expired"
        ElseIf hasNonCompliant Then
            overall = "Non-Compliant"
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

    ' Send daily consolidated digests to Super Admins and Department Admins
    Public Shared Sub SendDailyDigest()
        Try
            Console.WriteLine("[ComplianceCheck] Preparing daily consolidated digest emails...")
            
            ' Fetch all expiring (Non-Compliant) or expired documents
            Dim sql As String = "SELECT r.LicenseType, r.ExpiryDate, r.Status, v.VehicleNumber, v.VehicleType, v.Department As AllocatedDept " &
                               "FROM ComplianceRecords r " &
                               "INNER JOIN Vehicles v ON r.VehicleId = v.Id " &
                               "WHERE r.Status IN ('Non-Compliant', 'Expired') AND (v.IsDecommissioned = 0 OR v.IsDecommissioned IS NULL) " &
                               "ORDER BY v.Department, v.VehicleNumber, r.LicenseType"
            Dim dt As DataTable = Database.ExecuteDataTable(sql)

            ' 1. Send Consolidated email to Super Admin
            Dim superAdmins As DataTable = Database.ExecuteDataTable(
                "SELECT e.EmailId, e.EmployeeName FROM Authentication a INNER JOIN Employee e ON a.EmployeeId = e.EmployeeId WHERE a.Role = 'SuperAdmin' AND e.EmailId IS NOT NULL AND e.EmailId <> ''")
            
            For Each adminRow As DataRow In superAdmins.Rows
                Dim email As String = adminRow("EmailId").ToString()
                Dim name As String = adminRow("EmployeeName").ToString()
                Try
                    EmailService.SendConsolidatedDigest(email, name, "Super Admin", dt)
                Catch ex As Exception
                    Console.WriteLine("[ComplianceCheck] Failed to send consolidated digest to Super Admin: " & email & ". Error: " & ex.Message)
                End Try
            Next

            ' 2. Send scoped consolidated emails to Department Admins
            Dim deptData As New Dictionary(Of String, List(Of DataRow))()
            For Each row As DataRow In dt.Rows
                Dim dept As String = row("AllocatedDept").ToString()
                If String.IsNullOrEmpty(dept) Then dept = "PR - Human Resources"
                If Not deptData.ContainsKey(dept) Then
                    deptData(dept) = New List(Of DataRow)()
                End If
                deptData(dept).Add(row)
            Next

            For Each kvp As KeyValuePair(Of String, List(Of DataRow)) In deptData
                Dim deptName As String = kvp.Key
                Dim deptRows As List(Of DataRow) = kvp.Value

                Dim deptAdmins As DataTable = Database.ExecuteDataTable(
                    "SELECT e.EmailId, e.EmployeeName FROM Authentication a INNER JOIN Employee e ON a.EmployeeId = e.EmployeeId WHERE a.Role = 'DEPT_ADMIN' AND e.Department = @Dept AND e.EmailId IS NOT NULL AND e.EmailId <> ''",
                    New SQLiteParameter("@Dept", deptName))

                If deptAdmins.Rows.Count > 0 Then
                    Dim deptDt As DataTable = dt.Clone()
                    For Each r As DataRow In deptRows
                        deptDt.ImportRow(r)
                    Next

                    For Each adminRow As DataRow In deptAdmins.Rows
                        Dim email As String = adminRow("EmailId").ToString()
                        Dim name As String = adminRow("EmployeeName").ToString()
                        Try
                            EmailService.SendConsolidatedDigest(email, name, deptName, deptDt)
                        Catch ex As Exception
                            Console.WriteLine("[ComplianceCheck] Failed to send consolidated digest to Dept Admin of " & deptName & ": " & email & ". Error: " & ex.Message)
                        End Try
                    Next
                End If
            Next

            Console.WriteLine("[ComplianceCheck] Consolidated digests sent.")
        Catch ex As Exception
            Console.WriteLine("[ComplianceCheck] Consolidated email dispatch failed: " & ex.Message)
        End Try
    End Sub

    ' Background Scheduler Thread: runs compliance scan hourly & triggers digest at exactly 10:00 AM
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

                ' Trigger consolidated alerts daily at 10:00 AM
                Dim now As DateTime = DateTime.Now
                If now.Hour = 10 AndAlso now.Date > _lastDigestDate.Date Then
                    Try
                        SendDailyDigest()
                        _lastDigestDate = now.Date
                    Catch ex As Exception
                        Console.WriteLine("[ComplianceCheck] Scheduled digest error: " & ex.Message)
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
