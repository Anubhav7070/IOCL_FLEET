Imports System
Imports System.Data
Imports System.Data.SQLite

Public Class VerifyEmail
    Inherits System.Web.UI.Page

    Private Const OTP_EXPIRY_MINUTES As Integer = 15
    Private Const RESEND_COOLDOWN_SECONDS As Integer = 120

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        ' Must be logged in
        If Session("EmployeeId") Is Nothing Then
            Response.Redirect("~/Login.aspx", False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()
            Return
        End If

        ' Must be pending email verification
        If Session("VerifyEmailPending") Is Nothing OrElse Not CBool(Session("VerifyEmailPending")) Then
            ' Already verified or no verification needed
            RedirectToPortal()
            Return
        End If
    End Sub

    Protected Sub btnVerify_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlError.Visible = False
        pnlSuccess.Visible = False

        Dim otp As String = txtOtp.Text.Trim()

        If String.IsNullOrEmpty(otp) OrElse otp.Length <> 6 Then
            ShowError("Please enter the complete 6-digit OTP.")
            Return
        End If

        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))

        Try
            Dim utcNow As String = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            Dim sql As String = "SELECT Id FROM OtpTokens WHERE EmployeeId = @Id AND Token = @Token AND TokenType = 'EMAIL_VERIFY' AND IsUsed = 0 AND ExpiresAt > @Now LIMIT 1"
            Dim result As Object = Database.ExecuteScalar(sql,
                                                          New SQLiteParameter("@Id", empId),
                                                          New SQLiteParameter("@Token", otp),
                                                          New SQLiteParameter("@Now", utcNow))

            If result Is Nothing OrElse result Is DBNull.Value Then
                ShowError("The OTP is incorrect or has expired. Please request a new OTP using the Resend button.")
                Return
            End If

            Dim tokenId As Integer = Convert.ToInt32(result)

            ' Mark token as used
            Database.ExecuteNonQuery("UPDATE OtpTokens SET IsUsed = 1 WHERE Id = @TId",
                                     New SQLiteParameter("@TId", tokenId))

            ' Clear verification flag
            Session("VerifyEmailPending") = False
            Session.Remove("OtpResentAt")

            ' Audit log
            Dim empName As String = Session("EmployeeName").ToString()
            Database.ExecuteNonQuery("INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (@Id, @User, 'EMAIL_VERIFIED', 'Employee verified email address via OTP after first-login password set.', @IP, datetime('now'))",
                                     New SQLiteParameter("@Id", empId),
                                     New SQLiteParameter("@User", empName),
                                     New SQLiteParameter("@IP", Request.UserHostAddress))

            ' Redirect to portal
            RedirectToPortal()

        Catch ex As Exception
            ShowError("An error occurred. Please try again.")
            Console.WriteLine("[VerifyEmail] Error: " & ex.Message)
        End Try
    End Sub

    Protected Sub btnResend_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlError.Visible = False
        pnlSuccess.Visible = False

        ' Enforce 2-minute cooldown
        If Session("OtpResentAt") IsNot Nothing Then
            Dim lastResent As DateTime = CDate(Session("OtpResentAt"))
            If (DateTime.UtcNow - lastResent).TotalSeconds < RESEND_COOLDOWN_SECONDS Then
                Dim waitSec As Integer = RESEND_COOLDOWN_SECONDS - CInt((DateTime.UtcNow - lastResent).TotalSeconds)
                ShowError("Please wait " & waitSec & " seconds before requesting a new OTP.")
                Return
            End If
        End If

        Dim empId As Integer = Convert.ToInt32(Session("EmployeeId"))
        Dim empName As String = Session("EmployeeName").ToString()
        Dim empEmail As String = Session("EmailId").ToString()

        If String.IsNullOrEmpty(empEmail) Then
            ShowError("No email address is registered for your account. Please contact the administrator.")
            Return
        End If

        Try
            ' Invalidate old tokens
            Database.ExecuteNonQuery("UPDATE OtpTokens SET IsUsed = 1 WHERE EmployeeId = @Id AND TokenType = 'EMAIL_VERIFY' AND IsUsed = 0",
                                     New SQLiteParameter("@Id", empId))

            ' New OTP
            Dim rng As New Random()
            Dim otp As String = rng.Next(100000, 999999).ToString()
            Dim expiresAt As String = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES).ToString("yyyy-MM-dd HH:mm:ss")

            Database.ExecuteNonQuery("INSERT INTO OtpTokens (EmployeeId, Token, TokenType, ExpiresAt, IsUsed) VALUES (@Id, @Token, 'EMAIL_VERIFY', @Exp, 0)",
                                     New SQLiteParameter("@Id", empId),
                                     New SQLiteParameter("@Token", otp),
                                     New SQLiteParameter("@Exp", expiresAt))

            EmailService.SendEmailVerificationOtp(empEmail, empName, otp)

            Session("OtpResentAt") = DateTime.UtcNow
            ShowSuccess("A new OTP has been sent to " & MaskEmail(empEmail) & ".")

        Catch ex As Exception
            ShowError("Failed to resend OTP. Please try again.")
            Console.WriteLine("[VerifyEmail Resend] Error: " & ex.Message)
        End Try
    End Sub

    Private Sub RedirectToPortal()
        If Session("Role") IsNot Nothing AndAlso Session("Role").ToString() = "GATEMAN" Then
            Response.Redirect("~/Gate.aspx", False)
        Else
            Response.Redirect("~/Default.aspx", False)
        End If
        HttpContext.Current.ApplicationInstance.CompleteRequest()
    End Sub

    Private Sub ShowError(ByVal msg As String)
        pnlError.Visible = True
        lblError.Text = msg
    End Sub

    Private Sub ShowSuccess(ByVal msg As String)
        pnlSuccess.Visible = True
        lblSuccess.Text = msg
    End Sub

    Private Function MaskEmail(ByVal email As String) As String
        Try
            Dim atIndex As Integer = email.IndexOf("@")
            If atIndex <= 1 Then Return "***@***"
            Dim local As String = email.Substring(0, atIndex)
            Dim domain As String = email.Substring(atIndex)
            Dim visible As String = local.Substring(0, Math.Min(2, local.Length))
            Return visible & New String("*"c, Math.Max(0, local.Length - 2)) & domain
        Catch
            Return "***@***"
        End Try
    End Function
End Class
