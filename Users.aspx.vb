Imports System
Imports System.Data
Imports System.Data.SQLite
Imports System.Web
Imports System.Web.UI
Imports System.Web.UI.WebControls
Imports System.Collections.Generic
Imports BCrypt.Net

Public Class UsersPage
    Inherits Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Session("EmployeeId") Is Nothing Then
            Response.Redirect("~/Login.aspx", False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()
            Return
        End If
        
        Dim currentEmpId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim currentEmpNo As String = Database.ExecuteScalar("SELECT EmpNumber FROM Employee WHERE EmployeeId = @Id", New SQLiteParameter("@Id", currentEmpId)).ToString()
        
        ' Restrict access strictly to the primary SuperAdmin (10000001)
        If currentEmpNo <> "10000001" Then
            Response.Redirect("~/Default.aspx", False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()
            Return
        End If

        If Not IsPostBack Then
            LoadEmployees()
            PopulateDepartmentsDropdown()
        End If
    End Sub

    Private Sub LoadEmployees()
        Dim sql As String = "SELECT e.EmployeeId, e.EmpNumber, e.EmployeeName, e.Department, e.Designation, a.Role, e.EmailId, e.ManagerEmail, e.HodEmail, e.GmEmail, e.CgmEmail, e.Status FROM Employee e INNER JOIN Authentication a ON e.EmployeeId = a.EmployeeId"
        
        Dim whereClauses As New List(Of String)()
        Dim parameters As New List(Of SQLiteParameter)()

        If Not String.IsNullOrEmpty(txtEmpSearch.Text.Trim()) Then
            whereClauses.Add("(e.EmployeeName LIKE @Search OR e.EmpNumber LIKE @Search)")
            parameters.Add(New SQLiteParameter("@Search", "%" & txtEmpSearch.Text.Trim() & "%"))
        End If

        If whereClauses.Count > 0 Then
            sql &= " WHERE " & String.Join(" AND ", whereClauses.ToArray())
        End If

        sql &= " ORDER BY e.EmployeeName"

        Dim dt As DataTable = Database.ExecuteDataTable(sql, parameters.ToArray())
        If dt.Rows.Count = 0 Then
            pnlNoUsers.Visible = True
            rptUsers.Visible = False
        Else
            pnlNoUsers.Visible = False
            rptUsers.Visible = True
            rptUsers.DataSource = dt
            rptUsers.DataBind()
        End If
    End Sub

    Private Sub PopulateDepartmentsDropdown()
        ddlEmpDept.Items.Clear()
        
        Dim uniqueDepts As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        
        ' Predefined PR (Panipat Refinery) and PNC (Panipat Naphtha Cracker) departments
        Dim defaultDepts As String() = {
            "PR - Refinery Operations",
            "PR - Fire & Safety",
            "PR - Chemical & Laboratory",
            "PR - Mechanical Maintenance",
            "PR - Electrical Maintenance",
            "PR - Instrumentation Maintenance",
            "PR - Technical Services",
            "PR - Materials Management & Logistics",
            "PR - Finance & Accounts",
            "PR - Human Resources",
            "PR - Security",
            "PNC - Cracker Operations",
            "PNC - Fire & Safety",
            "PNC - Chemical & Testing",
            "PNC - Mechanical Maintenance",
            "PNC - Electrical Maintenance",
            "PNC - Instrumentation Maintenance",
            "PNC - Technical Services",
            "PNC - Logistics & Warehousing"
        }
        
        For Each dept As String In defaultDepts
            uniqueDepts.Add(dept)
        Next
        
        ' Add any other departments currently in the database to prevent data loss
        Try
            Dim dt As DataTable = Database.ExecuteDataTable("SELECT DISTINCT Department FROM Employee WHERE Department IS NOT NULL AND Department != ''")
            For Each row As DataRow In dt.Rows
                uniqueDepts.Add(row("Department").ToString())
            Next
        Catch ex As Exception
            ' Fallback if DB query fails
        End Try
        
        ' Sort departments alphabetically
        Dim sortedDepts As New List(Of String)(uniqueDepts)
        sortedDepts.Sort()
        
        For Each dept As String In sortedDepts
            ddlEmpDept.Items.Add(New ListItem(dept, dept))
        Next
    End Sub

    Protected Sub btnFilter_Click(ByVal sender As Object, ByVal e As EventArgs)
        LoadEmployees()
        pnlEdit.Visible = False
    End Sub

    Protected Sub btnReset_Click(ByVal sender As Object, ByVal e As EventArgs)
        txtEmpSearch.Text = ""
        LoadEmployees()
        pnlEdit.Visible = False
    End Sub

    Protected Sub btnNewEmp_Click(ByVal sender As Object, ByVal e As EventArgs)
        hdnEmpId.Value = ""
        txtEmpNo.Text = ""
        txtEmpNo.ReadOnly = False
        txtEmpName.Text = ""
        txtEmpDesg.Text = ""
        txtEmpEmail.Text = ""
        ddlEmpRole.SelectedIndex = 0
        txtEmpPassword.Text = ""
        txtManagerEmail.Text = ""
        txtHodEmail.Text = ""
        txtGmEmail.Text = ""
        txtCgmEmail.Text = ""
        ddlStatus.SelectedIndex = 0
        
        ToggleDepartmentScopeControl()
        
        lblFormTitle.Text = "Register Operator Account"
        pnlEdit.Visible = True
    End Sub

    Protected Sub lnkEdit_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim btn As LinkButton = CType(sender, LinkButton)
        Dim empId As Integer = Convert.ToInt32(btn.CommandArgument)

        Dim sql As String = "SELECT e.*, a.Role FROM Employee e INNER JOIN Authentication a ON e.EmployeeId = a.EmployeeId WHERE e.EmployeeId = @Id LIMIT 1"
        Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@Id", empId))
        If dt.Rows.Count = 0 Then Return

        Dim row As DataRow = dt.Rows(0)
        hdnEmpId.Value = empId.ToString()
        txtEmpNo.Text = row("EmpNumber").ToString()
        txtEmpNo.ReadOnly = True ' Prevent modifying employee number
        txtEmpName.Text = row("EmployeeName").ToString()
        txtEmpDesg.Text = row("Designation").ToString()
        txtEmpEmail.Text = row("EmailId").ToString()
        
        ddlEmpRole.SelectedValue = row("Role").ToString()
        
        txtManagerEmail.Text = If(row("ManagerEmail") Is DBNull.Value, "", row("ManagerEmail").ToString())
        txtHodEmail.Text = If(row("HodEmail") Is DBNull.Value, "", row("HodEmail").ToString())
        txtGmEmail.Text = If(row("GmEmail") Is DBNull.Value, "", row("GmEmail").ToString())
        txtCgmEmail.Text = If(row("CgmEmail") Is DBNull.Value, "", row("CgmEmail").ToString())
        Dim statusVal As String = If(row("Status") Is DBNull.Value, "Active", row("Status").ToString())
        Try
            ddlStatus.SelectedValue = statusVal
        Catch
            ddlStatus.SelectedIndex = 0
        End Try
        
        Try
            ddlEmpDept.SelectedValue = row("Department").ToString()
        Catch
            ' If not in dropdown (or NULL)
        End Try

        ToggleDepartmentScopeControl()

        txtEmpPassword.Text = ""
        lblFormTitle.Text = "Edit Operator Account"
        pnlEdit.Visible = True
    End Sub

    Protected Sub btnCancel_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlEdit.Visible = False
    End Sub

    Protected Sub ddlEmpRole_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs)
        ToggleDepartmentScopeControl()
    End Sub

    Private Sub ToggleDepartmentScopeControl()
        Dim role As String = ddlEmpRole.SelectedValue
        ' SuperAdmin and GATEMAN are global roles — no department scope needed
        If role = "SuperAdmin" OrElse role = "GATEMAN" OrElse role = "VIEWER" Then
            ddlEmpDept.Visible = False
            lblDeptGlobal.Visible = True
        Else
            ' Employee and DEPT_ADMIN require a department assignment
            ddlEmpDept.Visible = True
            lblDeptGlobal.Visible = False
        End If
    End Sub

    Protected Sub btnSave_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim empIdStr As String = hdnEmpId.Value
        Dim empNo As String = txtEmpNo.Text.Trim()
        Dim name As String = txtEmpName.Text.Trim()
        Dim desg As String = txtEmpDesg.Text.Trim()
        Dim email As String = txtEmpEmail.Text.Trim()
        Dim role As String = ddlEmpRole.SelectedValue
        Dim password As String = txtEmpPassword.Text
        Dim managerEmail As String = txtManagerEmail.Text.Trim()
        Dim hodEmail As String = txtHodEmail.Text.Trim()
        Dim gmEmail As String = txtGmEmail.Text.Trim()
        Dim cgmEmail As String = txtCgmEmail.Text.Trim()
        Dim status As String = ddlStatus.SelectedValue
        Dim adminId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim adminName As String = Session("EmployeeName").ToString()

        ' Determine department string
        Dim dept As String = ""
        If role = "SuperAdmin" OrElse role = "GATEMAN" Then
            dept = ""
        Else
            dept = ddlEmpDept.SelectedValue
        End If

        ' Validations
        If String.IsNullOrEmpty(empNo) OrElse String.IsNullOrEmpty(name) OrElse String.IsNullOrEmpty(desg) OrElse String.IsNullOrEmpty(email) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Employee Number, Name, Designation and Email are required.');", True)
            Return
        End If

        If empNo.Length <> 8 OrElse Not IsNumericOnly(empNo) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Employee Number must be exactly 8 digits and numeric only.');", True)
            Return
        End If

        Try
            If String.IsNullOrEmpty(empIdStr) Then
                ' 1. Create Employee
                Dim count As Object = Database.ExecuteScalar("SELECT COUNT(*) FROM Employee WHERE EmpNumber=@No", New SQLiteParameter("@No", empNo))
                If Convert.ToInt32(count) > 0 Then
                    ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Employee Number already registered.');", True)
                    Return
                End If

                ' Password is no longer required on creation, it defaults to the EmpNumber
                Dim defaultPassword As String = If(String.IsNullOrEmpty(password), empNo, password)

                ' Save Employee
                Dim sqlInsertEmp As String = "INSERT INTO Employee (EmpNumber, EmployeeName, Department, Designation, EmailId, ManagerEmail, HodEmail, GmEmail, CgmEmail, Status) VALUES (@No, @Name, @Dept, @Desg, @Email, @ManagerEmail, @HodEmail, @GmEmail, @CgmEmail, @Status);"
                Database.ExecuteNonQuery(sqlInsertEmp,
                    New SQLiteParameter("@No", empNo),
                    New SQLiteParameter("@Name", name),
                    New SQLiteParameter("@Dept", dept),
                    New SQLiteParameter("@Desg", desg),
                    New SQLiteParameter("@Email", email),
                    New SQLiteParameter("@ManagerEmail", managerEmail),
                    New SQLiteParameter("@HodEmail", hodEmail),
                    New SQLiteParameter("@GmEmail", gmEmail),
                    New SQLiteParameter("@CgmEmail", cgmEmail),
                    New SQLiteParameter("@Status", status))

                Dim newEmpId As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT EmployeeId FROM Employee WHERE EmpNumber=@No", New SQLiteParameter("@No", empNo)))

                ' Hash password & create Authentication
                Dim hash As String = BCrypt.Net.BCrypt.HashPassword(defaultPassword)
                Dim sqlInsertAuth As String = "INSERT INTO Authentication (EmployeeId, EmployeeName, Role, Password) VALUES (" & newEmpId & ", @Name, @Role, @Pass);"
                Database.ExecuteNonQuery(sqlInsertAuth,
                    New SQLiteParameter("@Name", name),
                    New SQLiteParameter("@Role", role),
                    New SQLiteParameter("@Pass", hash))

                ' Log Audit
                Dim sqlCreateAudit As String = "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & adminId & ", @Admin, 'EMPLOYEE_CREATE', 'Registered employee ' || @Name || ' (' || @No || ')', @IP, datetime('now'));"
                Database.ExecuteNonQuery(sqlCreateAudit, New SQLiteParameter("@Admin", adminName), New SQLiteParameter("@Name", name), New SQLiteParameter("@No", empNo), New SQLiteParameter("@IP", Request.UserHostAddress))

                ' Notify all SuperAdmins of the new user
                Try
                    EmailService.NotifySuperAdminOfNewUser(name, empNo, role, dept)
                Catch
                End Try

            Else
                ' 2. Edit Employee
                Dim empId As Integer = Convert.ToInt32(empIdStr)

                ' Prevent changing role of primary SuperAdmin
                Dim targetEmpNumber As String = Database.ExecuteScalar("SELECT EmpNumber FROM Employee WHERE EmployeeId = @Id", New SQLiteParameter("@Id", empId)).ToString()
                If targetEmpNumber = "10000001" AndAlso role <> "SuperAdmin" Then
                    ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('The primary SuperAdmin role cannot be changed.');", True)
                    Return
                End If

                ' Update Employee details
                Dim sqlUpdateEmp As String = "UPDATE Employee SET EmployeeName = @Name, Department = @Dept, Designation = @Desg, EmailId = @Email, ManagerEmail = @ManagerEmail, HodEmail = @HodEmail, GmEmail = @GmEmail, CgmEmail = @CgmEmail, Status = @Status WHERE EmployeeId = @Id"
                Database.ExecuteNonQuery(sqlUpdateEmp,
                    New SQLiteParameter("@Name", name),
                    New SQLiteParameter("@Dept", dept),
                    New SQLiteParameter("@Desg", desg),
                    New SQLiteParameter("@Email", email),
                    New SQLiteParameter("@ManagerEmail", managerEmail),
                    New SQLiteParameter("@HodEmail", hodEmail),
                    New SQLiteParameter("@GmEmail", gmEmail),
                    New SQLiteParameter("@CgmEmail", cgmEmail),
                    New SQLiteParameter("@Status", status),
                    New SQLiteParameter("@Id", empId))

                ' Update Authentication details
                Dim sqlUpdateAuth As String = "UPDATE Authentication SET EmployeeName = @Name, Role = @Role WHERE EmployeeId = @Id"
                Database.ExecuteNonQuery(sqlUpdateAuth,
                    New SQLiteParameter("@Name", name),
                    New SQLiteParameter("@Role", role),
                    New SQLiteParameter("@Id", empId))

                ' Reset Password if provided
                If Not String.IsNullOrEmpty(password) Then
                    Dim hash As String = BCrypt.Net.BCrypt.HashPassword(password)
                    Dim sqlUpdatePass As String = "UPDATE Authentication SET Password = @Pass WHERE EmployeeId = @Id"
                    Database.ExecuteNonQuery(sqlUpdatePass, New SQLiteParameter("@Pass", hash), New SQLiteParameter("@Id", empId))
                End If

                ' Log Audit
                Dim sqlUpdateAudit As String = "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & adminId & ", @Admin, 'EMPLOYEE_UPDATE', 'Updated employee details: ' || @Name, @IP, datetime('now'));"
                Database.ExecuteNonQuery(sqlUpdateAudit, New SQLiteParameter("@Admin", adminName), New SQLiteParameter("@Name", name), New SQLiteParameter("@IP", Request.UserHostAddress))
            End If

            pnlEdit.Visible = False
            LoadEmployees()
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Employee registry updated successfully!');", True)

        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Save failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    Protected Sub btnConfig_Click(ByVal sender As Object, ByVal e As EventArgs)
        ' Load System Configuration
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT * FROM SystemConfiguration LIMIT 1")
        If dt.Rows.Count > 0 Then
            Dim row As DataRow = dt.Rows(0)
            txtHr1Name.Text = If(row("Hr1Name") Is DBNull.Value, "", row("Hr1Name").ToString())
            txtHr1Email.Text = If(row("Hr1Email") Is DBNull.Value, "", row("Hr1Email").ToString())
            txtHr2Name.Text = If(row("Hr2Name") Is DBNull.Value, "", row("Hr2Name").ToString())
            txtHr2Email.Text = If(row("Hr2Email") Is DBNull.Value, "", row("Hr2Email").ToString())
            txtCentralEmail.Text = If(row("CentralComplianceEmail") Is DBNull.Value, "", row("CentralComplianceEmail").ToString())
        Else
            txtHr1Name.Text = ""
            txtHr1Email.Text = ""
            txtHr2Name.Text = ""
            txtHr2Email.Text = ""
            txtCentralEmail.Text = ""
        End If
        pnlConfig.Visible = True
    End Sub

    Protected Sub btnCancelConfig_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlConfig.Visible = False
    End Sub

    Protected Sub btnSaveConfig_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim hr1Name As String = txtHr1Name.Text.Trim()
        Dim hr1Email As String = txtHr1Email.Text.Trim()
        Dim hr2Name As String = txtHr2Name.Text.Trim()
        Dim hr2Email As String = txtHr2Email.Text.Trim()
        Dim centralEmail As String = txtCentralEmail.Text.Trim()
        Dim adminId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim adminName As String = Session("EmployeeName").ToString()

        If String.IsNullOrEmpty(hr1Name) OrElse String.IsNullOrEmpty(hr1Email) OrElse String.IsNullOrEmpty(hr2Name) OrElse String.IsNullOrEmpty(hr2Email) OrElse String.IsNullOrEmpty(centralEmail) Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('All fields are required.');", True)
            Return
        End If

        Try
            Dim count As Object = Database.ExecuteScalar("SELECT COUNT(*) FROM SystemConfiguration")
            If Convert.ToInt32(count) > 0 Then
                Database.ExecuteNonQuery(
                    "UPDATE SystemConfiguration SET Hr1Name=@Name1, Hr1Email=@Email1, Hr2Name=@Name2, Hr2Email=@Email2, CentralComplianceEmail=@CentralEmail WHERE Id=1",
                    New SQLiteParameter("@Name1", hr1Name),
                    New SQLiteParameter("@Email1", hr1Email),
                    New SQLiteParameter("@Name2", hr2Name),
                    New SQLiteParameter("@Email2", hr2Email),
                    New SQLiteParameter("@CentralEmail", centralEmail))
            Else
                Database.ExecuteNonQuery(
                    "INSERT INTO SystemConfiguration (Hr1Name, Hr1Email, Hr2Name, Hr2Email, CentralComplianceEmail) VALUES (@Name1, @Email1, @Name2, @Email2, @CentralEmail)",
                    New SQLiteParameter("@Name1", hr1Name),
                    New SQLiteParameter("@Email1", hr1Email),
                    New SQLiteParameter("@Name2", hr2Name),
                    New SQLiteParameter("@Email2", hr2Email),
                    New SQLiteParameter("@CentralEmail", centralEmail))
            End If

            ' Log Audit
            Dim sqlAudit As String = "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & adminId & ", @Admin, 'SYSTEM_CONFIG_UPDATE', 'Updated system configurations for HR1, HR2, and Compliance.', @IP, datetime('now'));"
            Database.ExecuteNonQuery(sqlAudit, New SQLiteParameter("@Admin", adminName), New SQLiteParameter("@IP", Request.UserHostAddress))

            pnlConfig.Visible = False
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('System Configuration updated successfully!');", True)
        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Save configuration failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    ' Log Audit for employee updates

    Protected Sub lnkDelete_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim btn As LinkButton = CType(sender, LinkButton)
        Dim empId As Integer = Convert.ToInt32(btn.CommandArgument)
        Dim adminId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim adminName As String = Session("EmployeeName").ToString()

        If empId = adminId Then
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Cannot delete your own logged-in administrator account!');", True)
            Return
        End If

        Try
            ' Prevent deletion of the primary SuperAdmin
            Dim targetEmpNumber As String = Database.ExecuteScalar("SELECT EmpNumber FROM Employee WHERE EmployeeId = @Id", New SQLiteParameter("@Id", empId)).ToString()
            If targetEmpNumber = "10000001" Then
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('The primary SuperAdmin account cannot be deleted!');", True)
                Return
            End If

            ' Verify no vehicles are owned by this employee
            Dim count As Object = Database.ExecuteScalar("SELECT COUNT(*) FROM Vehicles WHERE EmployeeId = @Id", New SQLiteParameter("@Id", empId))
            If Convert.ToInt32(count) > 0 Then
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Cannot delete employee because there are vehicles currently linked to them.');", True)
                Return
            End If

            ' Get details for audit
            Dim dt As DataTable = Database.ExecuteDataTable("SELECT EmployeeName, EmpNumber FROM Employee WHERE EmployeeId=@Id", New SQLiteParameter("@Id", empId))
            Dim empName As String = dt.Rows(0)("EmployeeName").ToString()
            Dim empNo As String = dt.Rows(0)("EmpNumber").ToString()

            ' Delete Employee (Cascade handles Authentication)
            Database.ExecuteNonQuery("DELETE FROM Employee WHERE EmployeeId = @Id", New SQLiteParameter("@Id", empId))

            ' Log Audit
            Dim sqlDeleteAudit As String = "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & adminId & ", @Admin, 'EMPLOYEE_DELETE', 'Deleted employee record: ' || @Name || ' (' || @No || ')', @IP, datetime('now'));"
            Database.ExecuteNonQuery(sqlDeleteAudit, New SQLiteParameter("@Admin", adminName), New SQLiteParameter("@Name", empName), New SQLiteParameter("@No", empNo), New SQLiteParameter("@IP", Request.UserHostAddress))

            LoadEmployees()
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Employee record successfully deleted.');", True)

        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Deletion failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    Private Function IsNumericOnly(ByVal val As String) As Boolean
        For Each c As Char In val
            If Not Char.IsDigit(c) Then Return False
        Next
        Return True
    End Function
End Class
