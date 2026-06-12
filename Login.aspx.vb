Imports System
Imports System.Data
Imports System.Data.SQLite
Imports BCrypt.Net

Public Class Login
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Not IsPostBack Then
            Try
                ' Auto-patch SuperAdmin credentials and department to match new logic
                Dim hash As String = BCrypt.Net.BCrypt.HashPassword("10000001")
                Database.ExecuteNonQuery("UPDATE Employee SET Department = '' WHERE EmpNumber = '10000001'")
                Database.ExecuteNonQuery("UPDATE Authentication SET Password = @Pass WHERE EmployeeId IN (SELECT EmployeeId FROM Employee WHERE EmpNumber = '10000001')", New SQLiteParameter("@Pass", hash))
            Catch
            End Try

            If Session("EmployeeId") IsNot Nothing Then
                Response.Redirect("~/Default.aspx")
            End If
        End If
    End Sub

    Protected Sub btnLogin_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlError.Visible = False
        Dim input As String = txtUsername.Text.Trim()
        Dim password As String = txtPassword.Text

        If String.IsNullOrEmpty(input) OrElse String.IsNullOrEmpty(password) Then
            ShowError("Please enter both Username/Employee Number and Password.")
            Return
        End If

        Try
            Dim sql As String = ""
            Dim param As SQLiteParameter = Nothing
            
            Dim isEmpNum As Boolean = (input.Length = 8 AndAlso IsNumericOnly(input))
            
            If isEmpNum Then
                sql = "SELECT a.Password, a.Role, e.EmployeeId, e.EmployeeName, e.Department, e.EmailId, e.EmpNumber FROM Authentication a INNER JOIN Employee e ON a.EmployeeId = e.EmployeeId WHERE e.EmpNumber = @Input LIMIT 1;"
                param = New SQLiteParameter("@Input", input)
            Else
                sql = "SELECT a.Password, a.Role, e.EmployeeId, e.EmployeeName, e.Department, e.EmailId, e.EmpNumber FROM Authentication a INNER JOIN Employee e ON a.EmployeeId = e.EmployeeId WHERE LOWER(e.EmployeeName) = LOWER(@Input) OR LOWER(REPLACE(e.EmployeeName, ' ', '')) = LOWER(@Input) LIMIT 1;"
                param = New SQLiteParameter("@Input", input)
            End If
            
            Dim dt As DataTable = Database.ExecuteDataTable(sql, param)

            If dt.Rows.Count = 0 Then
                ShowError("Invalid credentials. Please verify your username or Employee ID.")
                Return
            End If

            Dim row As DataRow = dt.Rows(0)
            Dim storedHash As String = row("Password").ToString()

            If Not BCrypt.Net.BCrypt.Verify(password, storedHash) Then
                ShowError("Invalid credentials. Please check your password.")
                Return
            End If

            ' Initialize session variables
            Session("EmployeeId") = row("EmployeeId")
            Session("EmpNumber") = row("EmpNumber").ToString()
            Session("EmployeeName") = row("EmployeeName").ToString()
            Session("Role") = row("Role").ToString()
            Session("Department") = row("Department").ToString()
            Session("EmailId") = row("EmailId").ToString()

            If password = row("EmpNumber").ToString() Then
                Session("MustChangePassword") = True
            Else
                Session("MustChangePassword") = False
            End If

            ' Log login event
            Dim userId As Integer = Convert.ToInt32(row("EmployeeId"))
            Dim username As String = row("EmployeeName").ToString()
            
            Dim sqlLoginAudit As String = "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & userId & ", @Username, 'USER_LOGIN', 'User logged in successfully.', @IP, datetime('now'));"
            Database.ExecuteNonQuery(sqlLoginAudit, New SQLiteParameter("@Username", username), New SQLiteParameter("@IP", Request.UserHostAddress))

            ' Redirect GATEMAN to Gate Entry, others to Dashboard
            If Session("Role").ToString() = "GATEMAN" Then
                Response.Redirect("~/Gate.aspx")
            Else
                Response.Redirect("~/Default.aspx")
            End If

        Catch ex As Exception
            ShowError("An error occurred during authentication: " & ex.Message)
        End Try
    End Sub

    Private Sub ShowError(ByVal message As String)
        pnlError.Visible = True
        lblError.Text = message
    End Sub

    Private Function IsNumericOnly(ByVal val As String) As Boolean
        For Each c As Char In val
            If Not Char.IsDigit(c) Then Return False
        Next
        Return True
    End Function
End Class
