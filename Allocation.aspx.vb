Imports System
Imports System.Data
Imports System.Data.SQLite
Imports System.Web.UI
Imports System.Web.UI.WebControls

Public Class AllocationPage
    Inherits Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Session("EmployeeId") Is Nothing Then
            Response.Redirect("~/Login.aspx", False)
            Context.ApplicationInstance.CompleteRequest()
            Return
        End If

        Dim role As String = Session("Role").ToString()
        If role = "GATEMAN" OrElse role = "VIEWER" Then
            Response.Redirect("~/Default.aspx", False)
            Context.ApplicationInstance.CompleteRequest()
            Return
        End If

        If Not Page.IsPostBack Then
            txtStartDate.Text = DateTime.Today.ToString("yyyy-MM-dd")
            txtEndDate.Text = DateTime.Today.AddMonths(6).ToString("yyyy-MM-dd")
            LoadVehicles()
            BindGrids()
        End If
    End Sub

    Private Sub LoadVehicles()
        Dim role As String = Session("Role").ToString()
        Dim sql As String = "SELECT Id, VehicleNumber || ' (' || VehicleType || ')' As DisplayName FROM Vehicles WHERE (IsDecommissioned = 0 OR IsDecommissioned IS NULL) AND OverallStatus = 'Compliant'"
        Dim dt As DataTable

        If role = "DEPT_ADMIN" Then
            Dim dept As String = Session("Department").ToString()
            sql &= " AND Department = @Dept"
            dt = Database.ExecuteDataTable(sql, New SQLiteParameter("@Dept", dept))
        Else
            dt = Database.ExecuteDataTable(sql)
        End If

        ddlVehicles.DataSource = dt
        ddlVehicles.DataTextField = "DisplayName"
        ddlVehicles.DataValueField = "Id"
        ddlVehicles.DataBind()
        ddlVehicles.Items.Insert(0, New ListItem("-- Select Vehicle --", ""))
    End Sub

    Private Sub BindGrids()
        Dim role As String = Session("Role").ToString()
        
        ' 1. Active Allocations
        Dim sqlActive As String = "SELECT a.Id, v.VehicleNumber, v.VehicleType, a.Department, a.StartDate, a.EndDate, e.EmployeeName As AllocatedByName " &
                                  "FROM VehicleAllocations a " &
                                  "INNER JOIN Vehicles v ON a.VehicleId = v.Id " &
                                  "LEFT JOIN Employee e ON a.AllocatedBy = e.EmployeeId " &
                                  "WHERE a.Status = 'ACTIVE' AND (v.IsDecommissioned = 0 OR v.IsDecommissioned IS NULL)"
        Dim dtActive As DataTable
        If role = "DEPT_ADMIN" Then
            Dim dept As String = Session("Department").ToString()
            sqlActive &= " AND (a.Department = @Dept OR v.Department = @Dept)"
            dtActive = Database.ExecuteDataTable(sqlActive, New SQLiteParameter("@Dept", dept))
        Else
            dtActive = Database.ExecuteDataTable(sqlActive)
        End If
        gvActiveAllocations.DataSource = dtActive
        gvActiveAllocations.DataBind()

        ' 2. Allocation History
        Dim sqlHistory As String = "SELECT a.Id, v.VehicleNumber, a.Department, a.StartDate, a.EndDate, e.EmployeeName As AllocatedByName, a.CreatedAt, a.Status " &
                                   "FROM VehicleAllocations a " &
                                   "INNER JOIN Vehicles v ON a.VehicleId = v.Id " &
                                   "LEFT JOIN Employee e ON a.AllocatedBy = e.EmployeeId " &
                                   "WHERE a.Status <> 'ACTIVE' AND (v.IsDecommissioned = 0 OR v.IsDecommissioned IS NULL)"
        Dim dtHistory As DataTable
        If role = "DEPT_ADMIN" Then
            Dim dept As String = Session("Department").ToString()
            sqlHistory &= " AND (a.Department = @Dept OR v.Department = @Dept)"
            dtHistory = Database.ExecuteDataTable(sqlHistory, New SQLiteParameter("@Dept", dept))
        Else
            sqlHistory &= " ORDER BY a.CreatedAt DESC"
            dtHistory = Database.ExecuteDataTable(sqlHistory)
        End If
        gvAllocationHistory.DataSource = dtHistory
        gvAllocationHistory.DataBind()
    End Sub

    Protected Sub btnSubmitAllocation_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlAlert.Visible = False

        If String.IsNullOrEmpty(ddlVehicles.SelectedValue) Then
            ShowAlert("Please select a vehicle to allocate.", "bg-red-50 text-red-700 border border-red-100")
            Return
        End If

        Dim vehicleId As Integer = Convert.ToInt32(ddlVehicles.SelectedValue)
        Dim dept As String = ddlDepartments.SelectedValue
        Dim startStr As String = txtStartDate.Text.Trim()
        Dim endStr As String = txtEndDate.Text.Trim()

        Dim startDate, endDate As DateTime
        If Not DateTime.TryParse(startStr, startDate) OrElse Not DateTime.TryParse(endStr, endDate) Then
            ShowAlert("Please select valid Start and End Dates.", "bg-red-50 text-red-700 border border-red-100")
            Return
        End If

        If startDate > endDate Then
            ShowAlert("Start Date cannot be later than End Date.", "bg-red-50 text-red-700 border border-red-100")
            Return
        End If

        ' Verify if the vehicle is compliant
        Try
            Dim statusObj As Object = Database.ExecuteScalar("SELECT OverallStatus FROM Vehicles WHERE Id = @VehId", New SQLiteParameter("@VehId", vehicleId))
            Dim overallStatus As String = If(statusObj IsNot Nothing, statusObj.ToString(), "")
            If overallStatus = "Expired" OrElse overallStatus = "Non-Compliant" Then
                ShowAlert("This vehicle cannot be allocated because its overall status is " & overallStatus & ".", "bg-red-50 text-red-700 border border-red-100")
                Return
            End If
        Catch ex As Exception
            ShowAlert("Error verifying vehicle status: " & ex.Message, "bg-red-50 text-red-700 border border-red-100")
            Return
        End Try

        Try
            ' Complete any active allocations for this vehicle first
            Database.ExecuteNonQuery(
                "UPDATE VehicleAllocations SET Status = 'COMPLETED', EndDate = @EndDate WHERE VehicleId = @VehId AND Status = 'ACTIVE'",
                New SQLiteParameter("@EndDate", DateTime.Today.ToString("yyyy-MM-dd")),
                New SQLiteParameter("@VehId", vehicleId))

            ' Insert new active allocation
            Dim userId As Integer = Convert.ToInt32(Session("EmployeeId"))
            Database.ExecuteNonQuery(
                "INSERT INTO VehicleAllocations (VehicleId, Department, StartDate, EndDate, AllocatedBy, Status) VALUES (@VehId, @Dept, @Start, @End, @AllocBy, 'ACTIVE')",
                New SQLiteParameter("@VehId", vehicleId),
                New SQLiteParameter("@Dept", dept),
                New SQLiteParameter("@Start", startStr),
                New SQLiteParameter("@End", endStr),
                New SQLiteParameter("@AllocBy", userId))

            ' Update current department of the vehicle
            Database.ExecuteNonQuery(
                "UPDATE Vehicles SET Department = @Dept, UpdatedAt = datetime('now') WHERE Id = @VehId",
                New SQLiteParameter("@Dept", dept),
                New SQLiteParameter("@VehId", vehicleId))

            ' Force recalculate compliance status for the vehicle under new department scope
            Compliance.UpdateVehicleStatus(vehicleId)

            ' Log Audit
            Dim plate As String = Database.ExecuteScalar("SELECT VehicleNumber FROM Vehicles WHERE Id = @VehId", New SQLiteParameter("@VehId", vehicleId)).ToString()
            Dim username As String = Session("EmployeeName").ToString()
            Dim desc As String = "Vehicle " & plate & " allocated to department '" & dept & "' from " & startStr & " to " & endStr & "."
            Database.ExecuteNonQuery(
                "INSERT INTO AuditLogs (UserId, Username, Action, Description, VehicleId, Department, IpAddress, Timestamp) VALUES (@UserId, @Username, 'VEHICLE_ALLOCATION', @Desc, @VehId, @Dept, @IP, datetime('now'))",
                New SQLiteParameter("@UserId", userId),
                New SQLiteParameter("@Username", username),
                New SQLiteParameter("@Desc", desc),
                New SQLiteParameter("@VehId", vehicleId),
                New SQLiteParameter("@Dept", dept),
                New SQLiteParameter("@IP", Request.UserHostAddress))

            ShowAlert("Successfully allocated vehicle " & plate & " to " & dept & ".", "bg-emerald-50 text-emerald-700 border border-emerald-100")
            
            ' Reset fields
            ddlVehicles.SelectedIndex = 0
            
            LoadVehicles()
            BindGrids()
        Catch ex As Exception
            ShowAlert("Error allocating vehicle: " & ex.Message, "bg-red-50 text-red-700 border border-red-100")
        End Try
    End Sub

    Protected Sub gvActiveAllocations_RowCommand(ByVal sender As Object, ByVal e As GridViewCommandEventArgs)
        If e.CommandName = "ReleaseVehicle" Then
            pnlAlert.Visible = False
            Dim allocationId As Integer = Convert.ToInt32(e.CommandArgument)

            Try
                ' Find allocation info
                Dim dt As DataTable = Database.ExecuteDataTable(
                    "SELECT VehicleId, Department FROM VehicleAllocations WHERE Id = @Id",
                    New SQLiteParameter("@Id", allocationId))
                
                If dt.Rows.Count = 0 Then Return
                Dim vehicleId As Integer = Convert.ToInt32(dt.Rows(0)("VehicleId"))
                Dim oldDept As String = dt.Rows(0)("Department").ToString()

                ' Complete allocation
                Database.ExecuteNonQuery(
                    "UPDATE VehicleAllocations SET Status = 'RELEASED', EndDate = @Today WHERE Id = @Id",
                    New SQLiteParameter("@Today", DateTime.Today.ToString("yyyy-MM-dd")),
                    New SQLiteParameter("@Id", allocationId))

                ' Return vehicle back to HR default
                Database.ExecuteNonQuery(
                    "UPDATE Vehicles SET Department = 'PR - Human Resources', UpdatedAt = datetime('now') WHERE Id = @VehId",
                    New SQLiteParameter("@VehId", vehicleId))

                ' Recalculate compliance status
                Compliance.UpdateVehicleStatus(vehicleId)

                ' Log Audit
                Dim plate As String = Database.ExecuteScalar("SELECT VehicleNumber FROM Vehicles WHERE Id = @VehId", New SQLiteParameter("@VehId", vehicleId)).ToString()
                Dim userId As Integer = Convert.ToInt32(Session("EmployeeId"))
                Dim username As String = Session("EmployeeName").ToString()
                Dim desc As String = "Vehicle " & plate & " allocation released from department '" & oldDept & "'. Returned to HR department."
                Database.ExecuteNonQuery(
                    "INSERT INTO AuditLogs (UserId, Username, Action, Description, VehicleId, Department, IpAddress, Timestamp) VALUES (@UserId, @Username, 'VEHICLE_DEALLOCATION', @Desc, @VehId, 'PR - Human Resources', @IP, datetime('now'))",
                    New SQLiteParameter("@UserId", userId),
                    New SQLiteParameter("@Username", username),
                    New SQLiteParameter("@Desc", desc),
                    New SQLiteParameter("@VehId", vehicleId),
                    New SQLiteParameter("@IP", Request.UserHostAddress))

                ShowAlert("Successfully released vehicle " & plate & " from allocation. Returned to PR - Human Resources.", "bg-emerald-50 text-emerald-700 border border-emerald-100")
                
                LoadVehicles()
                BindGrids()
            Catch ex As Exception
                ShowAlert("Error releasing allocation: " & ex.Message, "bg-red-50 text-red-700 border border-red-100")
            End Try
        End If
    End Sub

    Public Function GetDurationString(ByVal start As Object, ByVal [end] As Object) As String
        If start Is DBNull.Value OrElse [end] Is DBNull.Value Then Return ""
        Try
            Dim startDate As DateTime = Convert.ToDateTime(start)
            Dim endDate As DateTime = Convert.ToDateTime([end])
            Dim days As Integer = Convert.ToInt32((endDate.Date - startDate.Date).TotalDays)
            If days < 30 Then
                Return days & " days duration"
            Else
                Dim months As Double = Math.Round(days / 30.0, 1)
                Return months & " months duration"
            End If
        Catch
            Return ""
        End Try
    End Function

    Private Sub ShowAlert(ByVal msg As String, ByVal cssClass As String)
        pnlAlert.Visible = True
        pnlAlert.CssClass = "rounded-lg p-4 text-xs font-semibold " & cssClass
        lblAlertMsg.Text = msg
    End Sub
End Class
