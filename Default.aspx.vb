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
            Response.Redirect("~/Login.aspx", False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()
            Return
        End If

        ' Always load stats and charts to ensure they are fresh and populated on every request
        Try
            LoadDashboardStats()
        Catch ex As Exception
            ' Keep default label values (0) on error
        End Try
        Try
            LoadChartsData()
        Catch ex As Exception
            ' Keep empty chart on error
        End Try

        ' Set card attributes on every page load to guarantee client-side click events are registered
        pnlTotalVehiclesCard.Attributes("onclick") = "document.getElementById('" & btnTotalVehiclesClick.ClientID & "').click();"
        pnlCompliantCard.Attributes("onclick") = "document.getElementById('" & btnCompliantClick.ClientID & "').click();"
        pnlNonCompliantCard.Attributes("onclick") = "document.getElementById('" & btnNonCompliantClick.ClientID & "').click();"
        pnlExpiredCard.Attributes("onclick") = "document.getElementById('" & btnExpiredClick.ClientID & "').click();"

        If Not IsPostBack Then
            LoadDepartmentDdl()
            RefreshSummaries()
            ViewState("ActiveView") = "Total"
            BindActiveView()
            If Session("Role").ToString() = "SuperAdmin" Then
                LoadVerificationDocs()
            End If
        End If
    End Sub

    Private Sub LoadDashboardStats()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim dept As String = If(Session("Department") IsNot Nothing, Session("Department").ToString(), "")

        ' Build scoped WHERE clause based on role or department filter
        Dim scopeWhere As String = ""
        Dim scopeParam As SQLiteParameter = Nothing
        If role = "Employee" Then
            scopeWhere = " AND EmployeeId = @ScopeId"
            scopeParam = New SQLiteParameter("@ScopeId", empId)
        ElseIf role = "DEPT_ADMIN" Then
            scopeWhere = " AND Department = @ScopeId"
            scopeParam = New SQLiteParameter("@ScopeId", dept)
        ElseIf role = "SuperAdmin" AndAlso ddlAlertDept IsNot Nothing AndAlso Not String.IsNullOrEmpty(ddlAlertDept.SelectedValue) Then
            scopeWhere = " AND Department = @ScopeId"
            scopeParam = New SQLiteParameter("@ScopeId", ddlAlertDept.SelectedValue)
        End If

        Dim ExecScalar As Func(Of String, Integer) = Function(sql)
            Return Convert.ToInt32(If(scopeParam IsNot Nothing, Database.ExecuteScalar(sql, scopeParam), Database.ExecuteScalar(sql)))
        End Function

        ' Total
        Dim total As Integer = ExecScalar("SELECT COUNT(*) FROM Vehicles WHERE (IsDecommissioned = 0 OR IsDecommissioned IS NULL)" & scopeWhere)
        lblTotalVehicles.Text = total.ToString()

        ' Fully Compliant
        Dim compliant As Integer = ExecScalar("SELECT COUNT(*) FROM Vehicles WHERE OverallStatus = 'Compliant' AND (IsDecommissioned = 0 OR IsDecommissioned IS NULL)" & scopeWhere)
        lblCompliantVehicles.Text = compliant.ToString()

        ' Non-Compliant
        Dim nonCompliant As Integer = ExecScalar("SELECT COUNT(*) FROM Vehicles WHERE OverallStatus = 'Non-Compliant' AND (IsDecommissioned = 0 OR IsDecommissioned IS NULL)" & scopeWhere)
        lblNonCompliantVehicles.Text = nonCompliant.ToString()

        ' Expired
        Dim expired As Integer = ExecScalar("SELECT COUNT(*) FROM Vehicles WHERE OverallStatus = 'Expired' AND (IsDecommissioned = 0 OR IsDecommissioned IS NULL)" & scopeWhere)
        lblExpiredVehicles.Text = expired.ToString()

        ' Percent
        lblCompliantPercent.Text = If(total > 0, Math.Round((CDbl(compliant) / total) * 100).ToString(), "0")
    End Sub

    Private Sub LoadChartsData()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim userDept As String = If(Session("Department") IsNot Nothing, Session("Department").ToString(), "")

        ' Query department vehicle counts
        Dim sqlDepts As String = "SELECT Department, COUNT(*) As Cnt FROM Vehicles WHERE Department IS NOT NULL AND Department <> '' AND (IsDecommissioned = 0 OR IsDecommissioned IS NULL)"
        Dim dtDepts As DataTable

        If role = "Employee" Then
            sqlDepts &= " AND EmployeeId = @ScopeId GROUP BY Department"
            dtDepts = Database.ExecuteDataTable(sqlDepts, New SQLiteParameter("@ScopeId", empId))
        ElseIf role = "DEPT_ADMIN" Then
            sqlDepts &= " AND Department = @ScopeId GROUP BY Department"
            dtDepts = Database.ExecuteDataTable(sqlDepts, New SQLiteParameter("@ScopeId", userDept))
        Else
            sqlDepts &= " GROUP BY Department"
            dtDepts = Database.ExecuteDataTable(sqlDepts)
        End If

        Dim deptNames As New List(Of String)()
        Dim deptCounts As New List(Of Integer)()
        For Each row As DataRow In dtDepts.Rows
            deptNames.Add(row("Department").ToString())
            deptCounts.Add(Convert.ToInt32(row("Cnt")))
        Next

        Dim chartObj As New Dictionary(Of String, Object)()
        chartObj("DeptNames") = deptNames
        chartObj("DeptCounts") = deptCounts

        Dim serializer As New JavaScriptSerializer()
        ChartDataJson = serializer.Serialize(chartObj)
    End Sub

    Private Sub LoadDepartmentDdl()
        If ddlAlertDept Is Nothing Then Return
        
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT DISTINCT Department As Code FROM Vehicles WHERE Department IS NOT NULL AND Department <> '' UNION SELECT DISTINCT Department As Code FROM Employee WHERE Department IS NOT NULL AND Department <> '' ORDER BY Code ASC")
        ddlAlertDept.Items.Clear()
        ddlAlertDept.Items.Add(New ListItem("All Divisions", ""))
        For Each row As DataRow In dt.Rows
            ddlAlertDept.Items.Add(New ListItem(row("Code").ToString(), row("Code").ToString()))
        Next
    End Sub

    Private Sub LoadVehicleTypeSummary()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Dim sql As String = "SELECT VehicleType, COUNT(*) As Cnt FROM Vehicles WHERE (IsDecommissioned = 0 OR IsDecommissioned IS NULL)"
        Dim params As New List(Of SQLiteParameter)()

        If role = "Employee" Then
            sql &= " AND EmployeeId = @EmpId"
            params.Add(New SQLiteParameter("@EmpId", empId))
        ElseIf role = "DEPT_ADMIN" Then
            Dim deptScope As String = If(Session("Department") IsNot Nothing, Session("Department").ToString(), "")
            If Not String.IsNullOrEmpty(deptScope) Then
                sql &= " AND Department = @DeptScope"
                params.Add(New SQLiteParameter("@DeptScope", deptScope))
            End If
        End If

        sql &= " GROUP BY VehicleType ORDER BY Cnt DESC"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, params.ToArray())
        rptVehicleTypes.DataSource = dt
        rptVehicleTypes.DataBind()
    End Sub

    Private Sub LoadDepartmentSummary()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Dim sql As String = "SELECT Department, COUNT(*) As Cnt FROM Vehicles WHERE Department IS NOT NULL AND Department <> '' AND (IsDecommissioned = 0 OR IsDecommissioned IS NULL)"
        Dim params As New List(Of SQLiteParameter)()

        If role = "Employee" Then
            sql &= " AND EmployeeId = @EmpId"
            params.Add(New SQLiteParameter("@EmpId", empId))
        ElseIf role = "DEPT_ADMIN" Then
            Dim deptScope As String = If(Session("Department") IsNot Nothing, Session("Department").ToString(), "")
            If Not String.IsNullOrEmpty(deptScope) Then
                sql &= " AND Department = @DeptScope"
                params.Add(New SQLiteParameter("@DeptScope", deptScope))
            End If
        End If

        sql &= " GROUP BY Department ORDER BY Cnt DESC"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, params.ToArray())
        rptDepartments.DataSource = dt
        rptDepartments.DataBind()
    End Sub

    Protected Sub rptDepartments_ItemCommand(ByVal source As Object, ByVal e As RepeaterCommandEventArgs)
        If e.CommandName = "SelectDept" Then
            Dim dept As String = e.CommandArgument.ToString()
            ViewState("SelectedDeptForBreakdown") = dept
            LoadDeptBreakdown(dept)

            ' Also filter the bottom vehicles table by this department!
            If ddlAlertDept IsNot Nothing Then
                If ddlAlertDept.Items.FindByValue(dept) Is Nothing Then
                    ddlAlertDept.Items.Add(New ListItem(dept, dept))
                End If
                ddlAlertDept.SelectedValue = dept
                BindActiveView()
            End If
        End If
    End Sub

    Private Sub LoadDeptBreakdown(ByVal dept As String)
        If String.IsNullOrEmpty(dept) Then
            pnlDeptBreakdown.Visible = False
            pnlNoDeptBreakdown.Visible = True
            Return
        End If

        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Dim sql As String = "SELECT VehicleType, COUNT(*) As Cnt FROM Vehicles WHERE Department = @Dept AND (IsDecommissioned = 0 OR IsDecommissioned IS NULL)"
        Dim params As New List(Of SQLiteParameter)()
        params.Add(New SQLiteParameter("@Dept", dept))

        If role = "Employee" Then
            sql &= " AND EmployeeId = @EmpId"
            params.Add(New SQLiteParameter("@EmpId", empId))
        ElseIf role = "DEPT_ADMIN" Then
            Dim deptScope As String = If(Session("Department") IsNot Nothing, Session("Department").ToString(), "")
            If Not String.IsNullOrEmpty(deptScope) Then
                sql &= " AND Department = @DeptScope"
                params.Add(New SQLiteParameter("@DeptScope", deptScope))
            End If
        End If

        sql &= " GROUP BY VehicleType ORDER BY Cnt DESC"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, params.ToArray())
        lblBreakdownDeptName.Text = dept
        rptDeptBreakdown.DataSource = dt
        rptDeptBreakdown.DataBind()

        pnlDeptBreakdown.Visible = True
        pnlNoDeptBreakdown.Visible = False
    End Sub

    Private Sub RefreshSummaries()
        LoadVehicleTypeSummary()
        LoadDepartmentSummary()
        If ViewState("SelectedDeptForBreakdown") IsNot Nothing Then
            LoadDeptBreakdown(ViewState("SelectedDeptForBreakdown").ToString())
        Else
            pnlDeptBreakdown.Visible = False
            pnlNoDeptBreakdown.Visible = True
        End If
    End Sub

    ' ── Active View Tab Controls ──

    Protected Sub lnkTotalVehicles_Click(ByVal sender As Object, ByVal e As EventArgs)
        Response.Redirect("~/Vehicles.aspx", False)
        HttpContext.Current.ApplicationInstance.CompleteRequest()
    End Sub

    Protected Sub lnkCompliantVehicles_Click(ByVal sender As Object, ByVal e As EventArgs)
        ViewState("ActiveView") = "Compliant"
        BindActiveView()
    End Sub

    Protected Sub lnkNonCompliantVehicles_Click(ByVal sender As Object, ByVal e As EventArgs)
        ViewState("ActiveView") = "NonCompliant"
        BindActiveView()
    End Sub

    Protected Sub lnkExpiredVehicles_Click(ByVal sender As Object, ByVal e As EventArgs)
        ViewState("ActiveView") = "Expired"
        BindActiveView()
    End Sub

    Protected Sub FilterAlerts(ByVal sender As Object, ByVal e As EventArgs)
        LoadDashboardStats()
        BindActiveView()
    End Sub

    Private Sub ResetCardStyles()
        pnlTotalVehiclesCard.CssClass = "rounded-xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md hover:border-blue-400 transition-all w-full flex items-center justify-between cursor-pointer"
        pnlCompliantCard.CssClass = "rounded-xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md hover:border-emerald-400 transition-all w-full flex items-center justify-between cursor-pointer"
        pnlNonCompliantCard.CssClass = "rounded-xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md hover:border-orange-400 transition-all w-full flex items-center justify-between cursor-pointer"
        pnlExpiredCard.CssClass = "rounded-xl border border-slate-200 bg-white p-5 shadow-sm hover:shadow-md hover:border-red-400 transition-all w-full flex items-center justify-between cursor-pointer"
    End Sub

    Private Sub BindActiveView()
        Dim active As String = If(ViewState("ActiveView") IsNot Nothing, ViewState("ActiveView").ToString(), "Total")

        ' Toggle Visibility of Panels
        pnlTotalVehiclesView.Visible = (active = "Total")
        pnlCompliantVehiclesView.Visible = (active = "Compliant")
        pnlNonCompliantView.Visible = (active = "NonCompliant")
        pnlExpiredView.Visible = (active = "Expired")

        pnlMetricsSummary.Visible = (active = "Total")
        pnlVerificationHub.Visible = (active = "Total") AndAlso (Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "SuperAdmin")

        ResetCardStyles()

        Select Case active
            Case "Total"
                lblActiveTabTitle.Text = "Registered Vehicles"
                lblActiveTabDesc.Text = "Directory of all registered refinery vehicles"
                LoadTotalVehiclesList()
                pnlTotalVehiclesCard.CssClass = "rounded-xl border-2 border-blue-500 bg-blue-50/30 p-5 shadow-md transition-all w-full flex items-center justify-between cursor-pointer"

            Case "Compliant"
                lblActiveTabTitle.Text = "Compliant Vehicles"
                lblActiveTabDesc.Text = "Directory of all compliant refinery vehicles (windshield clearance green)"
                LoadCompliantVehiclesList()
                pnlCompliantCard.CssClass = "rounded-xl border-2 border-emerald-500 bg-emerald-50/30 p-5 shadow-md transition-all w-full flex items-center justify-between cursor-pointer"

            Case "NonCompliant"
                lblActiveTabTitle.Text = "Non-Compliant Documents"
                lblActiveTabDesc.Text = "Compliance certificates within warning thresholds (renewal needed)"
                LoadScopedDocumentRepeater(rptNonCompliantRC, "Non-Compliant", "RC")
                LoadScopedDocumentRepeater(rptNonCompliantInsurance, "Non-Compliant", "INSURANCE")
                LoadScopedDocumentRepeater(rptNonCompliantPUCC, "Non-Compliant", "PUCC")
                pnlNonCompliantCard.CssClass = "rounded-xl border-2 border-orange-500 bg-orange-50/30 p-5 shadow-md transition-all w-full flex items-center justify-between cursor-pointer"

            Case "Expired"
                lblActiveTabTitle.Text = "Expired Certificates"
                lblActiveTabDesc.Text = "Expired safety records (gate entry blocked)"
                LoadScopedDocumentRepeater(rptExpiredRC, "Expired", "RC")
                LoadScopedDocumentRepeater(rptExpiredInsurance, "Expired", "INSURANCE")
                LoadScopedDocumentRepeater(rptExpiredPUCC, "Expired", "PUCC")
                pnlExpiredCard.CssClass = "rounded-xl border-2 border-red-500 bg-red-50/30 p-5 shadow-md transition-all w-full flex items-center justify-between cursor-pointer"
        End Select
    End Sub

    Private Sub LoadTotalVehiclesList()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Dim sql As String = "SELECT v.Id, v.VehicleNumber, v.VehicleType, v.OverallStatus, v.Department As DepartmentName " &
                           "FROM Vehicles v"

        Dim whereClauses As New List(Of String)()
        Dim params As New List(Of SQLiteParameter)()

        whereClauses.Add("(v.IsDecommissioned = 0 OR v.IsDecommissioned IS NULL)")

        If role = "Employee" Then
            whereClauses.Add("v.EmployeeId = @EmpId")
            params.Add(New SQLiteParameter("@EmpId", empId))
        ElseIf role = "DEPT_ADMIN" Then
            Dim deptScope As String = If(Session("Department") IsNot Nothing, Session("Department").ToString(), "")
            If Not String.IsNullOrEmpty(deptScope) Then
                whereClauses.Add("v.Department = @DeptScope")
                params.Add(New SQLiteParameter("@DeptScope", deptScope))
            End If
        End If

        ' Apply department dropdown filter if it exists and SuperAdmin is viewing
        If role = "SuperAdmin" AndAlso ddlAlertDept IsNot Nothing AndAlso Not String.IsNullOrEmpty(ddlAlertDept.SelectedValue) Then
            whereClauses.Add("v.Department = @Dept")
            params.Add(New SQLiteParameter("@Dept", ddlAlertDept.SelectedValue))
        End If

        If whereClauses.Count > 0 Then
            sql &= " WHERE " & String.Join(" AND ", whereClauses.ToArray())
        End If

        sql &= " ORDER BY v.VehicleNumber"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, params.ToArray())
        rptTotalVehicles.DataSource = dt
        rptTotalVehicles.DataBind()
    End Sub

    Private Sub LoadCompliantVehiclesList()
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Dim sql As String = "SELECT v.Id, v.VehicleNumber, v.VehicleType, v.OverallStatus, v.Department As DepartmentName " &
                           "FROM Vehicles v"

        Dim whereClauses As New List(Of String)()
        Dim params As New List(Of SQLiteParameter)()

        whereClauses.Add("v.OverallStatus = 'Compliant'")
        whereClauses.Add("(v.IsDecommissioned = 0 OR v.IsDecommissioned IS NULL)")

        If role = "Employee" Then
            whereClauses.Add("v.EmployeeId = @EmpId")
            params.Add(New SQLiteParameter("@EmpId", empId))
        ElseIf role = "DEPT_ADMIN" Then
            Dim deptScope As String = If(Session("Department") IsNot Nothing, Session("Department").ToString(), "")
            If Not String.IsNullOrEmpty(deptScope) Then
                whereClauses.Add("v.Department = @DeptScope")
                params.Add(New SQLiteParameter("@DeptScope", deptScope))
            End If
        End If

        ' Apply department dropdown filter if it exists and SuperAdmin is viewing
        If role = "SuperAdmin" AndAlso ddlAlertDept IsNot Nothing AndAlso Not String.IsNullOrEmpty(ddlAlertDept.SelectedValue) Then
            whereClauses.Add("v.Department = @Dept")
            params.Add(New SQLiteParameter("@Dept", ddlAlertDept.SelectedValue))
        End If

        If whereClauses.Count > 0 Then
            sql &= " WHERE " & String.Join(" AND ", whereClauses.ToArray())
        End If

        sql &= " ORDER BY v.VehicleNumber"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, params.ToArray())
        rptCompliantVehicles.DataSource = dt
        rptCompliantVehicles.DataBind()
    End Sub

    Private Sub LoadScopedDocumentRepeater(ByVal rpt As Repeater, ByVal status As String, ByVal licType As String)
        Dim role As String = Session("Role").ToString()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Dim sql As String = "SELECT r.Id, r.LicenseType, r.ExpiryDate, r.Status, v.VehicleNumber, v.Department As DepartmentName " &
                           "FROM ComplianceRecords r " &
                           "INNER JOIN Vehicles v ON r.VehicleId = v.Id " &
                           "WHERE r.Status = @Status AND r.LicenseType = @LicType AND (v.IsDecommissioned = 0 OR v.IsDecommissioned IS NULL)"

        Dim params As New List(Of SQLiteParameter)()
        params.Add(New SQLiteParameter("@Status", status))
        params.Add(New SQLiteParameter("@LicType", licType))

        If role = "Employee" Then
            sql &= " AND v.EmployeeId = @EmpId"
            params.Add(New SQLiteParameter("@EmpId", empId))
        ElseIf role = "DEPT_ADMIN" Then
            Dim deptScope As String = If(Session("Department") IsNot Nothing, Session("Department").ToString(), "")
            If Not String.IsNullOrEmpty(deptScope) Then
                sql &= " AND v.Department = @DeptScope"
                params.Add(New SQLiteParameter("@DeptScope", deptScope))
            End If
        End If

        ' Apply department dropdown filter if it exists and SuperAdmin is viewing
        If role = "SuperAdmin" AndAlso ddlAlertDept IsNot Nothing AndAlso Not String.IsNullOrEmpty(ddlAlertDept.SelectedValue) Then
            sql &= " AND v.Department = @Dept"
            params.Add(New SQLiteParameter("@Dept", ddlAlertDept.SelectedValue))
        End If

        sql &= " ORDER BY r.ExpiryDate ASC"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, params.ToArray())
        rpt.DataSource = dt
        rpt.DataBind()
    End Sub

    ' ── Document Verification Hub (SuperAdmin only) ──
    Private Sub LoadVerificationDocs()
        Dim sql As String = "SELECT 'VEHICLE_RC' As LicenseType, v.Id As Id, v.VehicleNumber, v.Department As DepartmentCode, doc.FileName, doc.FilePath, v.IsVerified As IsVerified " &
                           "FROM Vehicles v " &
                           "INNER JOIN Documents doc ON v.DocumentId = doc.Id " &
                           "WHERE (v.IsDecommissioned = 0 OR v.IsDecommissioned IS NULL) " &
                           "UNION ALL " &
                           "SELECT r.LicenseType As LicenseType, r.Id As Id, v.VehicleNumber, v.Department As DepartmentCode, doc.FileName, doc.FilePath, r.IsVerified As IsVerified " &
                           "FROM ComplianceRecords r " &
                           "INNER JOIN Vehicles v ON r.VehicleId = v.Id " &
                           "INNER JOIN Documents doc ON r.DocumentId = doc.Id " &
                           "WHERE (v.IsDecommissioned = 0 OR v.IsDecommissioned IS NULL) " &
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
            Dim userStr As String = Session("EmployeeName").ToString()
            Dim userId As Integer = Convert.ToInt32(Session("EmployeeId"))

            Dim typeName As String = ""
            Dim plateNum As String = ""
            Dim isRc As Boolean = False
            
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
                
                ' Notify employee of approval
                If newVerified = 1 Then
                    Dim ownerIdObj = Database.ExecuteScalar("SELECT EmployeeId FROM Vehicles v INNER JOIN ComplianceRecords r ON v.Id = r.VehicleId WHERE r.Id = " & id)
                    If ownerIdObj IsNot Nothing AndAlso Not Convert.IsDBNull(ownerIdObj) Then
                        EmailService.NotifyEmployeeOfDocumentApproval(Convert.ToInt32(ownerIdObj), plateNum, typeName)
                    End If
                End If
            End If

            ' Refresh dashboard
            LoadDashboardStats()
            LoadChartsData()
            RefreshSummaries()
            BindActiveView()
            LoadVerificationDocs()
        End If
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
            Case "Compliant"
                Return "bg-emerald-100 text-emerald-700"
            Case "Non-Compliant"
                Return "bg-orange-100 text-orange-700"
            Case "Expired"
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
            sql = "SELECT n.Id, n.Title, n.Message, n.CreatedAt FROM Notifications n INNER JOIN Vehicles v ON n.VehicleId = v.Id WHERE n.Status = 'UNREAD' AND v.EmployeeId = @EmpId AND (v.IsDecommissioned = 0 OR v.IsDecommissioned IS NULL) ORDER BY n.Id DESC"
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
            Database.ExecuteNonQuery("UPDATE Notifications SET Status = 'READ' WHERE Status = 'UNREAD' AND VehicleId IN (SELECT Id FROM Vehicles WHERE EmployeeId = " & empId & " AND (IsDecommissioned = 0 OR IsDecommissioned IS NULL))")
        End If
    End Sub

    ' ── Reports Export Handling ──

    Protected Sub btnExportPDF_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            Dim pdfBytes As Byte() = ReportGenerator.GenerateCompliancePdf(Nothing)
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
            Dim xlsBytes As Byte() = ReportGenerator.GenerateComplianceExcel(Nothing)
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

    Public Function GetBannerScopeText() As String
        If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "DEPT_ADMIN" Then
            Return Session("Department").ToString() & " Department"
        Else
            Return "Panipat Refinery Complex"
        End If
    End Function
End Class
