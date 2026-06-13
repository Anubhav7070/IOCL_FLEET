Imports System
Imports System.Data
Imports System.Data.SQLite

Public Class ForgotPassword
    Inherits System.Web.UI.Page

    Private Const OTP_EXPIRY_MINUTES As Integer = 15

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        ' Already logged in — no need for reset
        If Session("EmployeeId") IsNot Nothing Then
            Response.Redirect("~/Default.aspx", False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()
            Return
        End If
    End Sub

    Protected Sub btnSendOtp_Click(ByVal sender As Object, ByVal e As EventArgs)
        pnlError.Visible = False
        pnlSuccess.Visible = False

        Dim empNum As String = txtEmpNumber.Text.Trim()

        If String.IsNullOrEmpty(empNum) OrElse empNum.Length <> 8 Then
            ShowError("Please enter a valid 8-digit Employee Number.")
            Return
        End If

        Try
            ' Look up employee
            Dim sql As String = "SELECT e.EmployeeId, e.EmployeeName, e.EmailId " &
                                "FROM Employee e WHERE e.EmpNumber = @EmpNum LIMIT 1"
            Dim dt As DataTable = Database.ExecuteDataTable(sql, New SQLiteParameter("@EmpNum", empNum))

            ' Always show the same generic message (security: don't reveal existence)
            If dt.Rows.Count = 0 Then
                ShowSuccess("If that Employee Number is registered, a reset OTP has been sent to the associated email address.")
                txtEmpNumber.Text = ""
                Return
            End If

            Dim row As DataRow = dt.Rows(0)
            Dim empId As Integer = Convert.ToInt32(row("EmployeeId"))
            Dim empName As String = row("EmployeeName").ToString()
            Dim empEmail As String = row("EmailId").ToString()

            If String.IsNullOrEmpty(empEmail) Then
                ShowError("No email address is registered for this Employee Number. Please contact the system administrator.")
                Return
            End If

            ' Invalidate any previous unused FORGOT_PASSWORD tokens for this employee
            Database.ExecuteNonQuery("UPDATE OtpTokens SET IsUsed = 1 WHERE EmployeeId = @Id AND TokenType = 'FORGOT_PASSWORD' AND IsUsed = 0",
                                     New SQLiteParameter("@Id", empId))

            ' Generate 6-digit OTP
            Dim rng As New Random()
            Dim otp As String = rng.Next(100000, 999999).ToString()
            Dim expiresAt As String = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES).ToString("yyyy-MM-dd HH:mm:ss")

            ' Persist token
            Database.ExecuteNonQuery("INSERT INTO OtpTokens (EmployeeId, Token, TokenType, ExpiresAt, IsUsed) VALUES (@Id, @Token, 'FORGOT_PASSWORD', @Exp, 0)",
                                     New SQLiteParameter("@Id", empId),
                                     New SQLiteParameter("@Token", otp),
                                     New SQLiteParameter("@Exp", expiresAt))

            ' Send email (fire-and-forget; EmailService already logs errors)
            EmailService.SendForgotPasswordOtp(empEmail, empName, otp, OTP_EXPIRY_MINUTES)

            ' Store employee ID in session so ResetPassword.aspx knows who is resetting
            Session("ForgotPwdEmpId") = empId
            Session("ForgotPwdEmpNum") = empNum

            ' Mask email for display
            Dim maskedEmail As String = MaskEmail(empEmail)

            ' Redirect to ResetPassword page
            Response.Redirect("~/ResetPassword.aspx?sent=1&email=" & Server.UrlEncode(maskedEmail), False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()

        Catch ex As Exception
            ShowError("An error occurred. Please try again later.")
            Console.WriteLine("[ForgotPassword] Error: " & ex.Message)
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
