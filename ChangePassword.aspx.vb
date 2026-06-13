Imports System
Imports System.Data.SQLite
Imports BCrypt.Net

Public Class ChangePasswordPage
    Inherits System.Web.UI.Page

    Private Const OTP_EXPIRY_MINUTES As Integer = 15

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Session("EmployeeId") Is Nothing Then
            Response.Redirect("~/Login.aspx", False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()
            Return
        End If
        
        ' If they don't actually need to change, let them go back
        If Session("MustChangePassword") Is Nothing OrElse Not CBool(Session("MustChangePassword")) Then
            Response.Redirect("~/Default.aspx", False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()
            Return
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
            
            ' Audit log for password change
            Dim username As String = Session("EmployeeName").ToString()
            Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & empId & ", @User, 'PASSWORD_CHANGE', 'User updated their default password on first login.', @IP, datetime('now'));",
                                     New SQLiteParameter("@User", username),
                                     New SQLiteParameter("@IP", Request.UserHostAddress))
            
            ' Clear the must-change-password flag
            Session("MustChangePassword") = False

            ' ── EMAIL VERIFICATION OTP ─────────────────────────────────────────
            ' Generate and send an OTP so the employee confirms their email address
            Dim empEmail As String = If(Session("EmailId") IsNot Nothing, Session("EmailId").ToString(), "")

            If Not String.IsNullOrEmpty(empEmail) Then
                ' Invalidate any previous pending EMAIL_VERIFY tokens
                Database.ExecuteNonQuery("UPDATE OtpTokens SET IsUsed = 1 WHERE EmployeeId = @Id AND TokenType = 'EMAIL_VERIFY' AND IsUsed = 0",
                                         New SQLiteParameter("@Id", empId))

                ' Generate 6-digit OTP
                Dim rng As New Random()
                Dim otp As String = rng.Next(100000, 999999).ToString()
                Dim expiresAt As String = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES).ToString("yyyy-MM-dd HH:mm:ss")

                ' Persist token
                Database.ExecuteNonQuery("INSERT INTO OtpTokens (EmployeeId, Token, TokenType, ExpiresAt, IsUsed) VALUES (@Id, @Token, 'EMAIL_VERIFY', @Exp, 0)",
                                         New SQLiteParameter("@Id", empId),
                                         New SQLiteParameter("@Token", otp),
                                         New SQLiteParameter("@Exp", expiresAt))

                ' Send verification email
                EmailService.SendEmailVerificationOtp(empEmail, username, otp)

                ' Signal that email verification is required before entering the portal
                Session("VerifyEmailPending") = True
                Session("OtpResentAt") = DateTime.UtcNow

                Response.Redirect("~/VerifyEmail.aspx", False)
                HttpContext.Current.ApplicationInstance.CompleteRequest()
            Else
                ' No email on record — skip OTP and go straight to portal
                Session("VerifyEmailPending") = False
                If Session("Role").ToString() = "GATEMAN" Then
                    Response.Redirect("~/Gate.aspx", False)
                Else
                    Response.Redirect("~/Default.aspx", False)
                End If
                HttpContext.Current.ApplicationInstance.CompleteRequest()
            End If
            ' ────────────────────────────────────────────────────────────────────
            
        Catch ex As Exception
            ShowError("An error occurred: " & ex.Message)
        End Try
    End Sub

    Private Sub ShowError(ByVal msg As String)
        pnlError.Visible = True
        lblError.Text = msg
    End Sub
End Class
