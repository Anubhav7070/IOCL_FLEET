Imports System
Imports System.Data.SQLite
Imports BCrypt.Net

Public Class ChangePasswordPage
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Session("EmployeeId") Is Nothing Then
            Response.Redirect("~/Login.aspx")
            Return
        End If
        
        ' If they don't actually need to change, let them go back
        If Session("MustChangePassword") Is Nothing OrElse Not CBool(Session("MustChangePassword")) Then
            Response.Redirect("~/Default.aspx")
        End If
    End Sub

    Protected Sub btnUpdate_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlError.Visible = False
        Dim newPass As String = txtNewPassword.Text
        Dim confirmPass As String = txtConfirmPassword.Text

        If String.IsNullOrEmpty(newPass) OrElse String.IsNullOrEmpty(confirmPass) Then
            ShowError("Please fill out both password fields.")
            Return
        End If

        If newPass <> confirmPass Then
            ShowError("Passwords do not match. Please try again.")
            Return
        End If
        
        If newPass = Session("EmpNumber").ToString() Then
            ShowError("Your new password cannot be your Employee ID. Please choose a secure password.")
            Return
        End If
        
        If newPass.Length < 6 Then
            ShowError("Password must be at least 6 characters long.")
            Return
        End If

        Try
            Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
            Dim hash As String = BCrypt.Net.BCrypt.HashPassword(newPass)
            
            Dim sqlUpdate As String = "UPDATE Authentication SET Password = @Pass WHERE EmployeeId = @Id"
            Database.ExecuteNonQuery(sqlUpdate, New SQLiteParameter("@Pass", hash), New SQLiteParameter("@Id", empId))
            
            ' Log audit
            Dim username As String = Session("EmployeeName").ToString()
            Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & empId & ", @User, 'PASSWORD_CHANGE', 'User updated their default password.', @IP, datetime('now'));", New SQLiteParameter("@User", username), New SQLiteParameter("@IP", Request.UserHostAddress))
            
            ' Clear flag and redirect
            Session("MustChangePassword") = False
            
            If Session("Role").ToString() = "GATEMAN" Then
                Response.Redirect("~/Gate.aspx")
            Else
                Response.Redirect("~/Default.aspx")
            End If
            
        Catch ex As Exception
            ShowError("An error occurred: " & ex.Message)
        End Try
    End Sub

    Private Sub ShowError(ByVal msg As String)
        pnlError.Visible = True
        lblError.Text = msg
    End Sub
End Class
