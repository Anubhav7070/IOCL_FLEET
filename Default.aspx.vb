Imports System
Imports System.Data
Imports System.Data.SQLite
Imports System.Web
Imports System.Web.Services
Imports System.Web.Script.Serialization
Imports System.Collections.Generic

Public Class DefaultPage
    Inherits System.Web.UI.Page

    Public ChartDataJson As String = "{}"

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Session("EmployeeId") Is Nothing Then
            Response.Redirect("~/Login.aspx")
            Return
        End If

        If Not IsPostBack Then
            LoadDashboardStats()
            LoadChartsData()
            LoadDepartmentDdl()
            LoadAlerts()
            LoadRecentAuditTrails()
            If Session("Role").ToString() = "SuperAdmin" Then
                LoadVerificationDocs()
            End If
        End If
    End Sub

    Private Sub LoadDashboardStats()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Dim whereClause As String = ""
        Dim param As SQLiteParameter = Nothing

        If role = "Employee" Then
            whereClause = " WHERE EmployeeId = @EmpId"
            param = New SQLiteParameter("@EmpId", empId)
        End If

        ' Total
        Dim totalSql As String = "SELECT COUNT(*) FROM Vehicles" & whereClause
        Dim total As Integer = Convert.ToInt32(If(param IsNot Nothing, Database.ExecuteScalar(totalSql, param), Database.ExecuteScalar(totalSql)))
        lblTotalVehicles.Text = total.ToString()

        ' Fully Compliant
        Dim compliantSql As String = "SELECT COUNT(*) FROM Vehicles WHERE OverallStatus = 'FULLY_COMPLIANT'"
        If role = "Employee" Then compliantSql &= " AND EmployeeId = @EmpId"
        Dim compliant As Integer = Convert.ToInt32(If(param IsNot Nothing, Database.ExecuteScalar(compliantSql, param), Database.ExecuteScalar(compliantSql)))
        lblCompliantVehicles.Text = compliant.ToString()

        ' Warning
        Dim warningSql As String = "SELECT COUNT(*) FROM Vehicles WHERE OverallStatus = 'WARNING'"
        If role = "Employee" Then warningSql &= " AND EmployeeId = @EmpId"
        Dim warning As Integer = Convert.ToInt32(If(param IsNot Nothing, Database.ExecuteScalar(warningSql, param), Database.ExecuteScalar(warningSql)))
        lblWarningVehicles.Text = warning.ToString()

        ' Critical
        Dim criticalSql As String = "SELECT COUNT(*) FROM Vehicles WHERE OverallStatus = 'CRITICAL'"
        If role = "Employee" Then criticalSql &= " AND EmployeeId = @EmpId"
        Dim critical As Integer = Convert.ToInt32(If(param IsNot Nothing, Database.ExecuteScalar(criticalSql, param), Database.ExecuteScalar(criticalSql)))
        lblCriticalVehicles.Text = critical.ToString()

        ' Expired
        Dim expiredSql As String = "SELECT COUNT(*) FROM Vehicles WHERE OverallStatus = 'EXPIRED'"
        If role = "Employee" Then expiredSql &= " AND EmployeeId = @EmpId"
        Dim expired As Integer = Convert.ToInt32(If(param IsNot Nothing, Database.ExecuteScalar(expiredSql, param), Database.ExecuteScalar(expiredSql)))
        lblExpiredVehicles.Text = expired.ToString()

        ' Percent
        lblCompliantPercent.Text = If(total > 0, Math.Round((CDbl(compliant) / total) * 100).ToString(), "0")
    End Sub

    Private Sub LoadChartsData()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        ' 1. Fleet Status breakdown counts
        Dim totalSql As String = "SELECT OverallStatus, COUNT(*) As Cnt FROM Vehicles"
        If role = "Employee" Then totalSql &= " WHERE EmployeeId = " & empId
        totalSql &= " GROUP BY OverallStatus"

        Dim dtStatus As DataTable = Database.ExecuteDataTable(totalSql)
        Dim fullyCompliant As Integer = 0
        Dim warning As Integer = 0
        Dim critical As Integer = 0
        Dim expired As Integer = 0

        For Each row As DataRow In dtStatus.Rows
            Dim status As String = row("OverallStatus").ToString()
            Dim cnt As Integer = Convert.ToInt32(row("Cnt"))
            Select Case status
                Case "FULLY_COMPLIANT"
                    fullyCompliant = cnt
                Case "WARNING"
                    warning = cnt
                Case "CRITICAL"
                    critical = cnt
                Case "EXPIRED"
                    expired = cnt
            End Select
        Next

        ' 2. Department comparison scores
        Dim dtDepts As DataTable = Database.ExecuteDataTable("SELECT Code, ComplianceScore FROM Departments ORDER BY ComplianceScore DESC")
        Dim deptNames As New List(Of String)()
        Dim deptScores As New List(Of Double)()
        For Each row As DataRow In dtDepts.Rows
            deptNames.Add(row("Code").ToString())
            deptScores.Add(Convert.ToDouble(row("ComplianceScore")))
        Next

        Dim chartObj As New Dictionary(Of String, Object)()
        chartObj("StatusData") = New Integer() {fullyCompliant, warning, critical, expired}
        chartObj("DeptNames") = deptNames
        chartObj("DeptScores") = deptScores

        Dim serializer As New JavaScriptSerializer()
        ChartDataJson = serializer.Serialize(chartObj)
    End Sub

    Private Sub LoadDepartmentDdl()
        If ddlAlertDept Is Nothing Then Return
        
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT Id, Code FROM Departments ORDER BY Code ASC")
        ddlAlertDept.Items.Clear()
        ddlAlertDept.Items.Add(New ListItem("All Divisions", ""))
        For Each row As DataRow In dt.Rows
            ddlAlertDept.Items.Add(New ListItem(row("Code").ToString(), row("Id").ToString()))
        Next
    End Sub

    Private Sub LoadAlerts()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Dim sql As String = "SELECT r.Id, r.LicenseType, r.ExpiryDate, r.Status, v.VehicleNumber, d.Code As DepartmentCode " &
                           "FROM ComplianceRecords r " &
                           "INNER JOIN Vehicles v ON r.VehicleId = v.Id " &
                           "INNER JOIN Departments d ON v.DepartmentId = d.Id " &
                           "WHERE 1=1"

        Dim params As New List(Of SQLiteParameter)()

        If role = "Employee" Then
            sql &= " AND v.EmployeeId = @EmpId"
            params.Add(New SQLiteParameter("@EmpId", empId))
        End If

        ' Department filter (SuperAdmin only)
        If role = "SuperAdmin" AndAlso Not String.IsNullOrEmpty(ddlAlertDept.SelectedValue) Then
            sql &= " AND v.DepartmentId = @DeptId"
            params.Add(New SQLiteParameter("@DeptId", Convert.ToInt32(ddlAlertDept.SelectedValue)))
        End If

        ' Priority filter
        Dim priority As String = ddlAlertPriority.SelectedValue
        If priority = "HIGH" Then
            sql &= " AND (r.Status = 'EXPIRED' OR r.Status = 'HIGH_CRITICAL')"
        ElseIf priority = "MEDIUM" Then
            sql &= " AND r.Status = 'MEDIUM_CRITICAL'"
        ElseIf priority = "LOW" Then
            sql &= " AND r.Status = 'WARNING'"
        Else
            sql &= " AND r.Status != 'ACTIVE'"
        End If

        sql &= " ORDER BY r.ExpiryDate ASC LIMIT 25"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, params.ToArray())
        rptAlerts.DataSource = dt
        rptAlerts.DataBind()
    End Sub

    Protected Sub FilterAlerts(ByVal sender As Object, ByVal e As EventArgs)
        LoadAlerts()
    End Sub

    Private Sub LoadRecentAuditTrails()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
        
        Dim sql As String = ""
        Dim dt As DataTable

        If role = "SuperAdmin" Then
            sql = "SELECT Username, Action, Description, Timestamp FROM AuditLogs ORDER BY Id DESC LIMIT 6"
            dt = Database.ExecuteDataTable(sql)
        Else
            sql = "SELECT Username, Action, Description, Timestamp FROM AuditLogs WHERE UserId = @EmpId ORDER BY Id DESC LIMIT 6"
            dt = Database.ExecuteDataTable(sql, New SQLiteParameter("@EmpId", empId))
        End If

        ' Add a formatted time column
        dt.Columns.Add("FormattedTime", GetType(String))
        For Each row As DataRow In dt.Rows
            Dim ts As String = row("Timestamp").ToString()
            Dim dtVal As DateTime
            If DateTime.TryParse(ts, dtVal) Then
                row("FormattedTime") = dtVal.ToString("hh:mm tt")
            Else
                row("FormattedTime") = ts
            End If
        Next

        rptAuditFeed.DataSource = dt
        rptAuditFeed.DataBind()
    End Sub

    ' ── Document Verification Hub (SuperAdmin only) ──
    Private Sub LoadVerificationDocs()
        Dim sql As String = "SELECT 'VEHICLE_RC' As LicenseType, v.Id As Id, v.VehicleNumber, d.Code As DepartmentCode, doc.FileName, doc.FilePath, v.IsVerified As IsVerified " &
                           "FROM Vehicles v " &
                           "INNER JOIN Departments d ON v.DepartmentId = d.Id " &
                           "INNER JOIN Documents doc ON v.DocumentId = doc.Id " &
                           "UNION ALL " &
                           "SELECT r.LicenseType As LicenseType, r.Id As Id, v.VehicleNumber, d.Code As DepartmentCode, doc.FileName, doc.FilePath, r.IsVerified As IsVerified " &
                           "FROM ComplianceRecords r " &
                           "INNER JOIN Vehicles v ON r.VehicleId = v.Id " &
                           "INNER JOIN Departments d ON v.DepartmentId = d.Id " &
                           "INNER JOIN Documents doc ON r.DocumentId = doc.Id " &
                           "ORDER BY IsVerified ASC, Id DESC"

        Dim dt As DataTable = Database.ExecuteDataTable(sql)
        rptVerificationDocs.DataSource = dt
        rptVerificationDocs.DataBind()
    End Sub

    Protected Sub rptVerificationDocs_ItemCommand(ByVal source As Object, ByVal e As RepeaterCommandEventArgs)
        If e.CommandName = "ToggleVerify" Then
            Dim args As String() = e.CommandArgument.ToString().Split("|"c)
            Dim id As Integer = Convert.ToInt32(args(0))
            Dim currentVerified As Integer = Convert.ToInt32(args(1))
            Dim newVerified As Integer = If(currentVerified = 1, 0, 1)

            Dim item As RepeaterItem = e.Item
            Dim licenseType As String = DirectCast(item.FindControl("btnToggleVerify"), LinkButton).Text.Trim()
            
            Dim userStr As String = Session("EmployeeName").ToString()
            Dim userId As Integer = Convert.ToInt32(Session("EmployeeId"))

            ' The table has a mixed key: check type from target row in source Datatable. We can inspect the command source
            Dim sqlSelect As String = ""
            Dim isRc As Boolean = False
            
            ' Fetch item data from command target
            Dim parentRow As DataRowView = DirectCast(item.DataItem, DataRowView)
            Dim typeName As String = ""
            Dim plateNum As String = ""
            
            ' Re-query to identify the row type securely
            Dim checkSql As String = "SELECT v.VehicleNumber, 'VEHICLE_RC' As TypeVal FROM Vehicles v WHERE v.Id = " & id & " UNION ALL SELECT v.VehicleNumber, r.LicenseType As TypeVal FROM ComplianceRecords r INNER JOIN Vehicles v ON r.VehicleId = v.Id WHERE r.Id = " & id
            Dim checkDt As DataTable = Database.ExecuteDataTable(checkSql)
            If checkDt.Rows.Count > 0 Then
                typeName = checkDt.Rows(0)("TypeVal").ToString()
                plateNum = checkDt.Rows(0)("VehicleNumber").ToString()
                isRc = (typeName = "VEHICLE_RC")
            End If

            If isRc Then
                Database.ExecuteNonQuery("UPDATE Vehicles SET IsVerified = " & newVerified & ", VerifiedBy = @Admin, UpdatedAt = datetime('now') WHERE Id = " & id, New SQLiteParameter("@Admin", userStr))
                Compliance.UpdateVehicleStatus(id)
                
                Dim act As String = If(newVerified = 1, "VEHICLE_VERIFY", "VEHICLE_VERIFY_REVOKE")
                Dim desc As String = "Vehicle registration card " & If(newVerified = 1, "approved", "revoked") & " for " & plateNum & "."
                Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & userId & ", @Admin, '" & act & "', @Desc, @IP, datetime('now'));", New SQLiteParameter("@Admin", userStr), New SQLiteParameter("@Desc", desc), New SQLiteParameter("@IP", Request.UserHostAddress))
                
                ' Notify creator
                Dim creatorIdObj As Object = Database.ExecuteScalar("SELECT EmployeeId FROM Vehicles WHERE Id = " & id)
                If creatorIdObj IsNot Nothing AndAlso Not Convert.IsDBNull(creatorIdObj) Then
                    EmailService.NotifyEmployeeOfApproval(Convert.ToInt32(creatorIdObj), plateNum)
                End If
            Else
                Database.ExecuteNonQuery("UPDATE ComplianceRecords SET IsVerified = " & newVerified & ", VerifiedBy = @Admin, UpdatedAt = datetime('now') WHERE Id = " & id, New SQLiteParameter("@Admin", userStr))
                
                ' Fetch vehicle ID
                Dim vehIdObj As Object = Database.ExecuteScalar("SELECT VehicleId FROM ComplianceRecords WHERE Id = " & id)
                If vehIdObj IsNot Nothing Then
                    Compliance.UpdateVehicleStatus(Convert.ToInt32(vehIdObj))
                End If
                
                Dim act As String = If(newVerified = 1, "DOCUMENT_VERIFY", "DOCUMENT_VERIFY_REVOKE")
                Dim desc As String = "Compliance document " & typeName & " " & If(newVerified = 1, "approved", "revoked") & " for " & plateNum & "."
                Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & userId & ", @Admin, '" & act & "', @Desc, @IP, datetime('now'));", New SQLiteParameter("@Admin", userStr), New SQLiteParameter("@Desc", desc), New SQLiteParameter("@IP", Request.UserHostAddress))
            End If

            ' Refresh dashboard
            LoadDashboardStats()
            LoadChartsData()
            LoadAlerts()
            LoadVerificationDocs()
            LoadRecentAuditTrails()
        End If
    End Sub

    ' ── Manual Email Controls (Daily Digest & Alert Scan) ──

    Protected Sub btnDailyDigest_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim userId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim username As String = Session("EmployeeName").ToString()

        Try
            ' Gather statistics for the daily summaries
            Dim totalVehicles As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT COUNT(*) FROM Vehicles"))
            Dim expiredCount As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT COUNT(*) FROM ComplianceRecords WHERE Status = 'EXPIRED'"))
            Dim criticalCount As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT COUNT(*) FROM ComplianceRecords WHERE Status IN ('HIGH_CRITICAL', 'MEDIUM_CRITICAL')"))
            Dim warningCount As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT COUNT(*) FROM ComplianceRecords WHERE Status = 'WARNING'"))

            Dim depts As DataTable = Database.ExecuteDataTable("SELECT Id, Name, Code, ComplianceScore FROM Departments")
            Dim deptBreakdowns As New List(Of String)()
            For Each r As DataRow In depts.Rows
                deptBreakdowns.Add("<li><strong>" & r("Code").ToString() & "</strong> (" & r("Name").ToString() & "): " & r("ComplianceScore").ToString() & "%</li>")
            Next
            Dim breakdownsHtml As String = "<ul>" & String.Join("", deptBreakdowns) & "</ul>"

            Dim expiringList As New List(Of String)()
            Dim dtExp As DataTable = Database.ExecuteDataTable("SELECT r.LicenseType, r.ExpiryDate, r.Status, v.VehicleNumber FROM ComplianceRecords r INNER JOIN Vehicles v ON r.VehicleId = v.Id WHERE r.Status != 'ACTIVE'")
            For Each rExp As DataRow In dtExp.Rows
                expiringList.Add("<tr><td style='padding: 6px; border: 1px solid #ddd;'>" & rExp("VehicleNumber").ToString() & "</td><td style='padding: 6px; border: 1px solid #ddd;'>" & rExp("LicenseType").ToString() & "</td><td style='padding: 6px; border: 1px solid #ddd;'>" & rExp("ExpiryDate").ToString() & "</td><td style='padding: 6px; border: 1px solid #ddd; color: red;'>" & rExp("Status").ToString() & "</td></tr>")
            Next
            Dim expiringHtml As String = "<table style='width: 100%; border-collapse: collapse; margin-top: 10px;'><thead><tr style='background: #eee;'><th style='padding: 6px; border: 1px solid #ddd;'>Vehicle</th><th style='padding: 6px; border: 1px solid #ddd;'>Cert Type</th><th style='padding: 6px; border: 1px solid #ddd;'>Expiry Date</th><th style='padding: 6px; border: 1px solid #ddd;'>Status</th></tr></thead><tbody>" & String.Join("", expiringList) & "</tbody></table>"

            Dim subject As String = "IOCL Daily compliance summary Report - All Divisions"
            Dim body As String = "<h2>Daily Fleet Compliance Digest</h2>" &
                         "<p>Dear Refinery Administrator,</p>" &
                         "<p>Here is the daily fleet safety status summary:</p>" &
                         "<ul>" &
                         "<li><strong>Total Registered Fleet:</strong> " & totalVehicles & "</li>" &
                         "<li><strong>Active Alerts (Expired):</strong> <span style='color: red;'>" & expiredCount & "</span></li>" &
                         "<li><strong>Active Alerts (Critical):</strong> <span style='color: orange;'>" & criticalCount & "</span></li>" &
                         "<li><strong>Active Alerts (Warning):</strong> <span style='color: #EAB308;'>" & warningCount & "</span></li>" &
                         "</ul>" &
                         "<h3>Division Scorecard</h3>" & breakdownsHtml &
                         "<h3>Warning & Expired Certificates</h3>" & expiringHtml &
                         "<br><hr><p>This digest was manually triggered by " & username & " via the administrator control center.</p>"

            ' Notify all SuperAdmins and DeptAdmins (Employees with administrator designations)
            Dim usersToNotify As DataTable = Database.ExecuteDataTable("SELECT e.EmailId, e.EmployeeName FROM Authentication a INNER JOIN Employee e ON a.EmployeeId = e.EmployeeId")
            
            Dim sentCount As Integer = 0
            For Each row As DataRow In usersToNotify.Rows
                Dim email As String = row("EmailId").ToString()
                EmailService.SendEmail(email, subject, body)
                sentCount += 1
            Next

            Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & userId & ", @Username, 'TRIGGER_DAILY_DIGEST', 'Dispatched daily summary reports via email.', @IP, datetime('now'));", New SQLiteParameter("@Username", username), New SQLiteParameter("@IP", Request.UserHostAddress))

            pnlDispatcherStatus.Visible = True
            pnlDispatcherStatus.CssClass = "rounded-lg border p-3 text-xs font-semibold bg-emerald-50 border-emerald-200 text-emerald-700"
            lblDispatcherStatus.Text = "Daily compliance summaries successfully emailed to all " & sentCount & " refinery operators."
        
        Catch ex As Exception
            pnlDispatcherStatus.Visible = True
            pnlDispatcherStatus.CssClass = "rounded-lg border p-3 text-xs font-semibold bg-red-50 border-red-200 text-red-700"
            lblDispatcherStatus.Text = "Failed to dispatch daily digest: " & ex.Message
        End Try
    End Sub

    Protected Sub btnComplianceScan_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim userId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim username As String = Session("EmployeeName").ToString()

        Try
            ' 1. Pull all compliance records to run calculated scan
            Dim records As DataTable = Database.ExecuteDataTable("SELECT Id, VehicleId, LicenseType, LicenseNumber, ExpiryDate, Status FROM ComplianceRecords")
            Dim scanCount As Integer = 0
            Dim alertsCount As Integer = 0

            For Each r As DataRow In records.Rows
                Dim recordId As Integer = Convert.ToInt32(r("Id"))
                Dim vehicleId As Integer = Convert.ToInt32(r("VehicleId"))
                Dim expiryStr As String = r("ExpiryDate").ToString()
                Dim currentStatus As String = r("Status").ToString()

                If String.IsNullOrEmpty(expiryStr) OrElse expiryStr = "PENDING" Then Continue For

                Dim computedStatus As String = Compliance.CalculateStatus(expiryStr)
                scanCount += 1

                ' Update status if it changed
                If currentStatus <> computedStatus Then
                    Database.ExecuteNonQuery("UPDATE ComplianceRecords SET Status = @Status, UpdatedAt = datetime('now') WHERE Id = @Id", New SQLiteParameter("@Status", computedStatus), New SQLiteParameter("@Id", recordId))
                    Compliance.UpdateVehicleStatus(vehicleId)

                    ' Log database compliance alert
                    If computedStatus <> "ACTIVE" Then
                        alertsCount += 1
                        Dim plateObj As Object = Database.ExecuteScalar("SELECT VehicleNumber FROM Vehicles WHERE Id = " & vehicleId)
                        Dim deptIdObj As Object = Database.ExecuteScalar("SELECT DepartmentId FROM Vehicles WHERE Id = " & vehicleId)
                        
                        Dim plateNum As String = If(plateObj IsNot Nothing, plateObj.ToString(), "N/A")
                        Dim deptId As Integer = If(deptIdObj IsNot Nothing, Convert.ToInt32(deptIdObj), 0)

                        Dim alertMessage As String = r("LicenseType").ToString() & " certificate for vehicle " & plateNum & " is now " & computedStatus & "."
                        Dim title As String = "Compliance Alert: " & r("LicenseType").ToString()
                        Dim typeVal As String = If(computedStatus = "EXPIRED", "EXPIRED", If(computedStatus = "WARNING", "WARNING", "CRITICAL"))

                        Database.ExecuteNonQuery("INSERT INTO Notifications (VehicleId, DepartmentId, Title, Message, Type, Status, CreatedAt) VALUES (" & vehicleId & ", " & deptId & ", @Title, @Msg, '" & typeVal & "', 'UNREAD', datetime('now'))", New SQLiteParameter("@Title", title), New SQLiteParameter("@Msg", alertMessage))

                        ' Dispatch email alerts to admins
                        Dim alertSubject As String = "IOCL FLEET CRITICAL COMPLIANCE ALERT: " & plateNum & " (" & r("LicenseType").ToString() & ")"
                        Dim alertBody As String = "<h2>Critical Expiry Alert</h2>" &
                                                 "<p>The safety certificate status for vehicle <strong>" & plateNum & "</strong> has changed.</p>" &
                                                 "<ul>" &
                                                 "<li><strong>Document slot:</strong> " & r("LicenseType").ToString() & "</li>" &
                                                 "<li><strong>License number:</strong> " & r("LicenseNumber").ToString() & "</li>" &
                                                 "<li><strong>Expiry date:</strong> " & expiryStr & "</li>" &
                                                 "<li><strong>New Status:</strong> <span style='color: red; font-weight: bold;'>" & computedStatus & "</span></li>" &
                                                 "</ul>" &
                                                 "<p>Please review records and block gate pass if necessary.</p>"

                        Dim admins As DataTable = Database.ExecuteDataTable("SELECT EmailId FROM Employee e INNER JOIN Authentication a ON e.EmployeeId = a.EmployeeId WHERE a.Role = 'SuperAdmin'")
                        For Each adminRow As DataRow In admins.Rows
                            EmailService.SendEmail(adminRow("EmailId").ToString(), alertSubject, alertBody)
                        Next
                    End If
                End If
            Next

            Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & userId & ", @Username, 'TRIGGER_EMAIL_SCAN', 'Executed system-wide compliance scanning. Verified all certificates.', @IP, datetime('now'));", New SQLiteParameter("@Username", username), New SQLiteParameter("@IP", Request.UserHostAddress))

            pnlDispatcherStatus.Visible = True
            pnlDispatcherStatus.CssClass = "rounded-lg border p-3 text-xs font-semibold bg-emerald-50 border-emerald-200 text-emerald-700"
            lblDispatcherStatus.Text = "System scan finished. Scanned " & scanCount & " documents. Created " & alertsCount & " notifications and alert emails."
            
            ' Reload view
            LoadDashboardStats()
            LoadChartsData()
            LoadAlerts()
            LoadRecentAuditTrails()

        Catch ex As Exception
            pnlDispatcherStatus.Visible = True
            pnlDispatcherStatus.CssClass = "rounded-lg border p-3 text-xs font-semibold bg-red-50 border-red-200 text-red-700"
            lblDispatcherStatus.Text = "Scan failed: " & ex.Message
        End Try
    End Sub

    ' ── Shared Helpers ──

    Public Function FmtDate(ByVal d As Object) As String
        If d Is Nothing OrElse Convert.IsDBNull(d) OrElse String.IsNullOrEmpty(d.ToString()) Then Return "Pending"
        Dim dateStr As String = d.ToString()
        If dateStr = "PENDING" Then Return "Pending"
        Dim dt As DateTime
        If DateTime.TryParse(dateStr, dt) Then
            Return dt.ToString("dd-MMM-yyyy")
        End If
        Return dateStr
    End Function

    Public Function GetBadgeCSS(ByVal status As Object) As String
        If status Is Nothing Then Return "bg-slate-100 text-slate-700"
        Dim s As String = status.ToString()
        Select Case s
            Case "ACTIVE", "FULLY_COMPLIANT"
                Return "bg-emerald-100 text-emerald-700"
            Case "WARNING"
                Return "bg-yellow-100 text-yellow-700"
            Case "CRITICAL", "MEDIUM_CRITICAL", "HIGH_CRITICAL"
                Return "bg-orange-100 text-orange-700"
            Case "EXPIRED"
                Return "bg-red-100 text-red-700"
            Case Else
                Return "bg-slate-100 text-slate-700"
        End Select
    End Function

    ' ── WebMethods for Polling Notifications (Exposed in Default.aspx.vb for Master page) ──

    <WebMethod(EnableSession:=True)>
    Public Shared Function GetLatestNotifications() As String
        If HttpContext.Current.Session("EmployeeId") Is Nothing Then Return "[]"

        Dim role As String = HttpContext.Current.Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(HttpContext.Current.Session("EmployeeId"))
        
        Dim sql As String = ""
        Dim dt As DataTable

        If role = "SuperAdmin" Then
            sql = "SELECT Id, Title, Message, CreatedAt FROM Notifications WHERE Status = 'UNREAD' ORDER BY Id DESC"
            dt = Database.ExecuteDataTable(sql)
        Else
            sql = "SELECT n.Id, n.Title, n.Message, n.CreatedAt FROM Notifications n INNER JOIN Vehicles v ON n.VehicleId = v.Id WHERE n.Status = 'UNREAD' AND v.EmployeeId = @EmpId ORDER BY n.Id DESC"
            dt = Database.ExecuteDataTable(sql, New SQLiteParameter("@EmpId", empId))
        End If

        Dim list As New List(Of Dictionary(Of String, String))()
        For Each row As DataRow In dt.Rows
            Dim d As New Dictionary(Of String, String)()
            d("Id") = row("Id").ToString()
            d("Title") = row("Title").ToString()
            d("Message") = row("Message").ToString()
            d("CreatedAt") = row("CreatedAt").ToString()
            list.Add(d)
        Next

        Dim serializer As New JavaScriptSerializer()
        Return serializer.Serialize(list)
    End Function

    <WebMethod(EnableSession:=True)>
    Public Shared Sub ClearNotifications()
        If HttpContext.Current.Session("EmployeeId") Is Nothing Then Return

        Dim role As String = HttpContext.Current.Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(HttpContext.Current.Session("EmployeeId"))

        If role = "SuperAdmin" Then
            Database.ExecuteNonQuery("UPDATE Notifications SET Status = 'READ' WHERE Status = 'UNREAD'")
        Else
            Database.ExecuteNonQuery("UPDATE Notifications SET Status = 'READ' WHERE Status = 'UNREAD' AND VehicleId IN (SELECT Id FROM Vehicles WHERE EmployeeId = " & empId & ")")
        End If
    End Sub

    ' ── Reports Export Handling ──

    Protected Sub btnExportPDF_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            Dim pdfBytes As Byte() = ReportGenerator.GenerateCompliancePdf(0)
            Dim fileName As String = "IOCL_Compliance_Report_" & DateTime.Now.ToString("yyyyMMdd_HHmm") & ".pdf"
            Response.Clear()
            Response.ContentType = "application/pdf"
            Response.AddHeader("Content-Disposition", "attachment; filename=" & fileName)
            Response.AddHeader("Content-Length", pdfBytes.Length.ToString())
            Response.BinaryWrite(pdfBytes)
            Response.Flush()
            Response.End()
        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('PDF export failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    Protected Sub btnExportExcel_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            Dim xlsBytes As Byte() = ReportGenerator.GenerateComplianceExcel(0)
            Dim fileName As String = "IOCL_Compliance_Report_" & DateTime.Now.ToString("yyyyMMdd_HHmm") & ".xls"
            Response.Clear()
            Response.ContentType = "application/vnd.ms-excel"
            Response.AddHeader("Content-Disposition", "attachment; filename=" & fileName)
            Response.AddHeader("Content-Length", xlsBytes.Length.ToString())
            Response.BinaryWrite(xlsBytes)
            Response.Flush()
            Response.End()
        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Excel export failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    Protected Sub btnTriggerEmails_Click(ByVal sender As Object, ByVal e As EventArgs)
        If Session("Role").ToString() <> "SuperAdmin" Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Access denied.');", True)
            Return
        End If
        Try
            ' Run compliance check + send emails asynchronously
            Dim t As New System.Threading.Thread(Sub()
                Try
                    Compliance.RunComplianceCheck()
                    Compliance.SendDailyDigest()
                Catch ex As Exception
                    Console.WriteLine("[TriggerEmail] Error: " & ex.Message)
                End Try
            End Sub)
            t.IsBackground = True
            t.Start()
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Compliance scan and emails triggered in background. Admins will receive alerts shortly.');", True)
        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Trigger failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    Public Function GetBannerScopeText() As String
        If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "DEPT_ADMIN" Then
            Return Session("Department").ToString() & " Department"
        Else
            Return "Panipat Refinery Complex"
        End If
    End Function
End Class
