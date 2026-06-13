Imports System
Imports System.Data
Imports System.Data.SQLite
Imports BCrypt.Net

Public Class ResetPassword
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        ' Already logged in
        If Session("EmployeeId") IsNot Nothing Then
            Response.Redirect("~/Default.aspx", False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()
            Return
        End If

        ' Must have gone through ForgotPassword.aspx first
        If Session("ForgotPwdEmpId") Is Nothing Then
            Response.Redirect("~/ForgotPassword.aspx", False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()
            Return
        End If

        If Not IsPostBack Then
            ' Show email hint if passed via query string
            Dim sentEmail As String = Request.QueryString("email")
            If Not String.IsNullOrEmpty(sentEmail) Then
                pnlEmailBadge.Visible = True
                lblEmailHint.Text = "OTP sent to " & Server.HtmlEncode(sentEmail)
            End If
        End If
    End Sub

    Protected Sub btnReset_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlError.Visible = False
        pnlSuccess.Visible = False

        Dim otp As String = txtOtp.Text.Trim()
        Dim newPass As String = txtNewPassword.Text
        Dim confirmPass As String = txtConfirmPassword.Text

        ' Validation
        If String.IsNullOrEmpty(otp) OrElse otp.Length <> 6 Then
            ShowError("Please enter the 6-digit OTP from your email.")
            Return
        End If

        If String.IsNullOrEmpty(newPass) OrElse String.IsNullOrEmpty(confirmPass) Then
            ShowError("Please fill in both password fields.")
            Return
        End If

        If newPass <> confirmPass Then
            ShowError("Passwords do not match. Please try again.")
            Return
        End If

        If newPass.Length < 6 Then
            ShowError("Password must be at least 6 characters long.")
            Return
        End If

        Dim empId As Integer = Convert.ToInt32(Session("ForgotPwdEmpId"))
        Dim empNum As String = Session("ForgotPwdEmpNum").ToString()

        If newPass = empNum Then
            ShowError("Your new password cannot be your Employee ID. Please choose a secure password.")
            Return
        End If

        Try
            ' Validate OTP
            Dim utcNow As String = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            Dim sql As String = "SELECT Id FROM OtpTokens WHERE EmployeeId = @Id AND Token = @Token AND TokenType = 'FORGOT_PASSWORD' AND IsUsed = 0 AND ExpiresAt > @Now LIMIT 1"
            Dim result As Object = Database.ExecuteScalar(sql,
                                                          New SQLiteParameter("@Id", empId),
                                                          New SQLiteParameter("@Token", otp),
                                                          New SQLiteParameter("@Now", utcNow))

            If result Is Nothing OrElse result Is DBNull.Value Then
                ShowError("The OTP is invalid or has expired. Please request a new one.")
                Return
            End If

            Dim tokenId As Integer = Convert.ToInt32(result)

            ' Mark token as used
            Database.ExecuteNonQuery("UPDATE OtpTokens SET IsUsed = 1 WHERE Id = @TId",
                                     New SQLiteParameter("@TId", tokenId))

            ' Update password
            Dim hash As String = BCrypt.Net.BCrypt.HashPassword(newPass)
            Database.ExecuteNonQuery("UPDATE Authentication SET Password = @Pass WHERE EmployeeId = @Id",
                                     New SQLiteParameter("@Pass", hash),
                                     New SQLiteParameter("@Id", empId))

            ' Audit log
            Dim nameDt As DataTable = Database.ExecuteDataTable("SELECT EmployeeName FROM Employee WHERE EmployeeId = @Id",
                                                                 New SQLiteParameter("@Id", empId))
            Dim empName As String = If(nameDt.Rows.Count > 0, nameDt.Rows(0)("EmployeeName").ToString(), "Unknown")

            Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (@Id, @User, 'PASSWORD_RESET', 'User reset password via Forgot Password OTP flow.', @IP, datetime('now'))",
                                     New SQLiteParameter("@Id", empId),
                                     New SQLiteParameter("@User", empName),
                                     New SQLiteParameter("@IP", Request.UserHostAddress))

            ' Clear session flags
            Session.Remove("ForgotPwdEmpId")
            Session.Remove("ForgotPwdEmpNum")

            ' Redirect to login with success flag
            Response.Redirect("~/Login.aspx?reset=1", False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()

        Catch ex As Exception
            ShowError("An error occurred. Please try again.")
            Console.WriteLine("[ResetPassword] Error: " & ex.Message)
        End Try
    End Sub

    Private Sub ShowError(ByVal msg As String)
        pnlError.Visible = True
        lblError.Text = msg
    End Sub

    Private Sub ShowSuccess(ByVal msg As String)
        pnlSuccess.Visible = True
        lblSuccess.Text = msg
    End Sub
End Class
