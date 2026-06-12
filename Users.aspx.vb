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
            Response.Redirect("~/Login.aspx")
            Return
        End If
        
        Dim currentEmpId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim currentEmpNo As String = Database.ExecuteScalar("SELECT EmpNumber FROM Employee WHERE EmployeeId = @Id", New SQLiteParameter("@Id", currentEmpId)).ToString()
        
        ' Restrict access strictly to the primary SuperAdmin (10000001)
        If currentEmpNo <> "10000001" Then
            Response.Redirect("~/Default.aspx")
            Return
        End If

        If Not IsPostBack Then
            LoadEmployees()
            PopulateDepartmentsDropdown()
        End If
    End Sub

    Private Sub LoadEmployees()
        Dim sql As String = "SELECT e.EmployeeId, e.EmpNumber, e.EmployeeName, e.Department, e.Designation, a.Role, e.EmailId FROM Employee e INNER JOIN Authentication a ON e.EmployeeId = a.EmployeeId"
        
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
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT DISTINCT Department AS Name FROM Employee WHERE Department IS NOT NULL AND Department != '' ORDER BY Department")
        For Each row As DataRow In dt.Rows
            ddlEmpDept.Items.Add(New ListItem(row("Name").ToString(), row("Name").ToString()))
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
        If role = "SuperAdmin" OrElse role = "GATEMAN" Then
            ddlEmpDept.Visible = False
            lblDeptGlobal.Visible = True
        Else
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
                Dim sqlInsertEmp As String = "INSERT INTO Employee (EmpNumber, EmployeeName, Department, Designation, EmailId) VALUES (@No, @Name, @Dept, @Desg, @Email);"
                Database.ExecuteNonQuery(sqlInsertEmp,
                    New SQLiteParameter("@No", empNo),
                    New SQLiteParameter("@Name", name),
                    New SQLiteParameter("@Dept", dept),
                    New SQLiteParameter("@Desg", desg),
                    New SQLiteParameter("@Email", email))

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
                Dim sqlUpdateEmp As String = "UPDATE Employee SET EmployeeName = @Name, Department = @Dept, Designation = @Desg, EmailId = @Email WHERE EmployeeId = @Id"
                Database.ExecuteNonQuery(sqlUpdateEmp,
                    New SQLiteParameter("@Name", name),
                    New SQLiteParameter("@Dept", dept),
                    New SQLiteParameter("@Desg", desg),
                    New SQLiteParameter("@Email", email),
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
