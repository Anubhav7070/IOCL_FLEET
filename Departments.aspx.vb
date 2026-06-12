Imports System
Imports System.Data
Imports System.Data.SQLite
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls

Public Class DepartmentsPage
    Inherits Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        ' Restrict access strictly to SuperAdmin
        If Session("Role") Is Nothing OrElse Session("Role").ToString() <> "SuperAdmin" Then
            Response.Redirect("~/Default.aspx")
        End If

        If Not IsPostBack Then
            LoadDepartments()
        End If
    End Sub

    Private Sub LoadDepartments()
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT * FROM Departments ORDER BY Name")
        If dt.Rows.Count = 0 Then
            pnlNoDepts.Visible = True
            rptDepts.Visible = False
        Else
            pnlNoDepts.Visible = False
            rptDepts.Visible = True
            rptDepts.DataSource = dt
            rptDepts.DataBind()
        End If
    End Sub

    Protected Sub btnAddDept_Click(ByVal sender As Object, ByVal e As EventArgs)
        hdnDeptId.Value = ""
        txtDeptCode.Text = ""
        txtDeptName.Text = ""
        txtDivision.Text = "Panipat Refinery"
        txtDescription.Text = ""
        lblFormTitle.Text = "Create Department"
        pnlModal.Visible = True
    End Sub

    Protected Sub lnkEdit_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim btn As LinkButton = CType(sender, LinkButton)
        Dim deptId As Integer = Convert.ToInt32(btn.CommandArgument)
        
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT * FROM Departments WHERE Id = @Id LIMIT 1", New SQLiteParameter("@Id", deptId))
        If dt.Rows.Count = 0 Then Return

        Dim row As DataRow = dt.Rows(0)
        hdnDeptId.Value = deptId.ToString()
        txtDeptCode.Text = row("Code").ToString()
        txtDeptName.Text = row("Name").ToString()
        txtDivision.Text = row("Division").ToString()
        txtDescription.Text = If(row("Description") Is DBNull.Value, "", row("Description").ToString())
        
        lblFormTitle.Text = "Edit Department"
        pnlModal.Visible = True
    End Sub

    Protected Sub btnCancel_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlModal.Visible = False
    End Sub

    Protected Sub btnSave_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim deptIdStr As String = hdnDeptId.Value
        Dim code As String = txtDeptCode.Text.Trim().ToUpper()
        Dim name As String = txtDeptName.Text.Trim()
        Dim division As String = txtDivision.Text.Trim()
        Dim desc As String = txtDescription.Text.Trim()
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim username As String = Session("EmployeeName").ToString()

        If String.IsNullOrEmpty(code) OrElse String.IsNullOrEmpty(name) OrElse String.IsNullOrEmpty(division) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Code, Name and Division are required.');", True)
            Return
        End If

        Try
            If String.IsNullOrEmpty(deptIdStr) Then
                ' 1. Create - Check duplicates
                Dim count As Object = Database.ExecuteScalar("SELECT COUNT(*) FROM Departments WHERE Code=@Code OR Name=@Name", 
                                                             New SQLiteParameter("@Code", code), New SQLiteParameter("@Name", name))
                If Convert.ToInt32(count) > 0 Then
                    ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Department Code or Name already exists.');", True)
                    Return
                End If

                Dim sqlInsertDept As String = "INSERT INTO Departments (Code, Name, Division, Description, ComplianceScore, CreatedAt, UpdatedAt) VALUES (@Code, @Name, @Division, @Desc, 100.0, datetime('now'), datetime('now'));"
                Database.ExecuteNonQuery(sqlInsertDept,
                    New SQLiteParameter("@Code", code),
                    New SQLiteParameter("@Name", name),
                    New SQLiteParameter("@Division", division),
                    New SQLiteParameter("@Desc", desc))

                ' Log Audit
                Dim sqlCreateAudit As String = "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & empId & ", @User, 'DEPARTMENT_CREATE', 'Created new department: ' || @Name, @IP, datetime('now'));"
                Database.ExecuteNonQuery(sqlCreateAudit, New SQLiteParameter("@User", username), New SQLiteParameter("@Name", name), New SQLiteParameter("@IP", Request.UserHostAddress))

            Else
                ' 2. Edit - Check duplicates excluding current
                Dim deptId As Integer = Convert.ToInt32(deptIdStr)
                Dim count As Object = Database.ExecuteScalar("SELECT COUNT(*) FROM Departments WHERE (Code=@Code OR Name=@Name) AND Id <> @Id", 
                                                             New SQLiteParameter("@Code", code), New SQLiteParameter("@Name", name), New SQLiteParameter("@Id", deptId))
                If Convert.ToInt32(count) > 0 Then
                    ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Another Department with this Code or Name already exists.');", True)
                    Return
                End If

                Dim sqlUpdateDept As String = "UPDATE Departments SET Code=@Code, Name=@Name, Division=@Division, Description=@Desc, UpdatedAt=datetime('now') WHERE Id=@Id"
                Database.ExecuteNonQuery(sqlUpdateDept,
                    New SQLiteParameter("@Code", code),
                    New SQLiteParameter("@Name", name),
                    New SQLiteParameter("@Division", division),
                    New SQLiteParameter("@Desc", desc),
                    New SQLiteParameter("@Id", deptId))

                ' Log Audit
                Dim sqlUpdateAudit As String = "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & empId & ", @User, 'DEPARTMENT_UPDATE', 'Updated department: ' || @Name, @IP, datetime('now'));"
                Database.ExecuteNonQuery(sqlUpdateAudit, New SQLiteParameter("@User", username), New SQLiteParameter("@Name", name), New SQLiteParameter("@IP", Request.UserHostAddress))
            End If

            pnlModal.Visible = False
            LoadDepartments()
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Department saved successfully!');", True)

        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Save failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    Protected Sub lnkDelete_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim btn As LinkButton = CType(sender, LinkButton)
        Dim deptId As Integer = Convert.ToInt32(btn.CommandArgument)
        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim username As String = Session("EmployeeName").ToString()

        Try
            ' Verify no vehicles are associated
            Dim count As Object = Database.ExecuteScalar("SELECT COUNT(*) FROM Vehicles WHERE DepartmentId = @Id", New SQLiteParameter("@Id", deptId))
            If Convert.ToInt32(count) > 0 Then
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Cannot delete department because vehicles are currently associated with it.');", True)
                Return
            End If

            ' Get name for audit
            Dim name As String = Database.ExecuteScalar("SELECT Name FROM Departments WHERE Id = @Id", New SQLiteParameter("@Id", deptId)).ToString()

            Database.ExecuteNonQuery("DELETE FROM Departments WHERE Id = @Id", New SQLiteParameter("@Id", deptId))

            ' Log Audit
            Dim sqlDeleteAudit As String = "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & empId & ", @User, 'DEPARTMENT_DELETE', 'Deleted department: ' || @Name, @IP, datetime('now'));"
            Database.ExecuteNonQuery(sqlDeleteAudit, New SQLiteParameter("@User", username), New SQLiteParameter("@Name", name), New SQLiteParameter("@IP", Request.UserHostAddress))

            LoadDepartments()
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Department deleted successfully.');", True)

        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Deletion failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    ' Scanner Actions
    Protected Sub lnkScan_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim btn As LinkButton = CType(sender, LinkButton)
        Dim deptId As Integer = Convert.ToInt32(btn.CommandArgument)
        
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT Name, Code FROM Departments WHERE Id = @Id LIMIT 1", New SQLiteParameter("@Id", deptId))
        If dt.Rows.Count = 0 Then Return

        hdnScannerDeptId.Value = deptId.ToString()
        lblScannerDeptName.Text = dt.Rows(0)("Name").ToString()
        lblScannerDeptCode.Text = dt.Rows(0)("Code").ToString()

        pnlScannerModal.Visible = True
        pnlScannerCam.Visible = True
        pnlScannerResult.Visible = False

        ClientScript.RegisterStartupScript(Me.GetType(), "StartScanner", "setTimeout(startDeptCameraScanner, 200);", True)
    End Sub

    Protected Sub lnkCloseScanner_Click(ByVal sender As Object, ByVal e As EventArgs)
        ClientScript.RegisterStartupScript(Me.GetType(), "StopScanner", "stopDeptCameraScanner();", True)
        hdnScannerDeptId.Value = ""
        pnlScannerModal.Visible = False
    End Sub

    Protected Sub btnResetScan_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlScannerCam.Visible = True
        pnlScannerResult.Visible = False
        ClientScript.RegisterStartupScript(Me.GetType(), "StartScanner", "setTimeout(startDeptCameraScanner, 200);", True)
    End Sub

    Protected Sub btnProcessScan_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim scVehId As String = hdnScannedVehicleId.Value
        If String.IsNullOrEmpty(scVehId) Then Return

        Try
            Dim vehId As Integer = Convert.ToInt32(scVehId)
            
            ' Fetch verified status
            Dim sql As String = "SELECT v.*, d.Name As DeptName FROM Vehicles v INNER JOIN Departments d ON v.DepartmentId = d.Id WHERE v.Id = @Id LIMIT 1"
            Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@Id", vehId))
            If dt.Rows.Count = 0 Then
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Scanned vehicle not found in database.');", True)
                Return
            End If

            Dim row As DataRow = dt.Rows(0)

            ' Ensure status is recalculated
            Compliance.UpdateVehicleStatus(vehId)

            ' Re-query vehicle
            Dim dtUpdated As DataTable = Database.ExecuteDataTable("SELECT OverallStatus, IsVerified FROM Vehicles WHERE Id = " & vehId)
            Dim overallStatus As String = dtUpdated.Rows(0)("OverallStatus").ToString()
            Dim isVerified As Boolean = Convert.ToBoolean(dtUpdated.Rows(0)("IsVerified"))

            lblScanPlate.Text = row("VehicleNumber").ToString()
            lblScanType.Text = row("VehicleType").ToString()
            lblScanDriver.Text = If(row("DriverName") Is DBNull.Value OrElse String.IsNullOrEmpty(row("DriverName").ToString()), "N/A", row("DriverName").ToString())
            lblScanVendor.Text = If(row("VendorName") Is DBNull.Value OrElse String.IsNullOrEmpty(row("VendorName").ToString()), "N/A", row("VendorName").ToString())

            If overallStatus = "FULLY_COMPLIANT" AndAlso isVerified Then
                pnlScanCleared.Visible = True
                pnlScanDenied.Visible = False
            Else
                pnlScanCleared.Visible = False
                pnlScanDenied.Visible = True
            End If

            ' Load compliance checklist
            Dim dtDocs As DataTable = Database.ExecuteDataTable("SELECT LicenseType, ExpiryDate, Status FROM ComplianceRecords WHERE VehicleId = " & vehId & " ORDER BY LicenseType")
            rptScanChecklist.DataSource = dtDocs
            rptScanChecklist.DataBind()

            pnlScannerCam.Visible = False
            pnlScannerResult.Visible = True

        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Processing scanned QR failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    ' Helpers
    Public Function GetScoreStyle(ByVal score As Double) As String
        If score >= 90 Then
            Return "text-emerald-600 bg-emerald-50 border-emerald-250"
        ElseIf score >= 75 Then
            Return "text-yellow-600 bg-yellow-50 border-yellow-250"
        Else
            Return "text-red-600 bg-red-50 border-red-250"
        End If
    End Function

    Public Function FmtDate(ByVal dateObj As Object) As String
        If dateObj Is Nothing OrElse Convert.IsDBNull(dateObj) OrElse String.IsNullOrEmpty(dateObj.ToString()) Then
            Return "PENDING"
        End If
        Dim dt As DateTime
        If DateTime.TryParse(dateObj.ToString(), dt) Then
            Return dt.ToString("dd-MMM-yyyy")
        End If
        Return dateObj.ToString()
    End Function
End Class
