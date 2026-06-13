Imports System
Imports System.Web
Imports System.Web.UI
Imports System.Data.SQLite

Public Class Site
    Inherits MasterPage

    Protected Sub Page_Init(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Init
        ' Verify user session context (exclude Login page and public Verify page)
        Dim localPath As String = Request.Url.LocalPath.ToLower()
        
        Dim isPublic As Boolean = localPath.Contains("login.aspx") OrElse 
                                  localPath.Contains("verify.aspx")
        
        If Not isPublic AndAlso Session("EmployeeId") Is Nothing Then
            Response.Redirect("~/Login.aspx", False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()
            Return
        End If
        
        ' Enforce mandatory password change if using default password
        If Not isPublic AndAlso Not localPath.Contains("changepassword.aspx") Then
            If Session("MustChangePassword") IsNot Nothing AndAlso CBool(Session("MustChangePassword")) Then
                Response.Redirect("~/ChangePassword.aspx", False)
                HttpContext.Current.ApplicationInstance.CompleteRequest()
                Return
            End If
        End If
    End Sub

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Session("EmployeeId") IsNot Nothing Then
            Dim name As String = Session("EmployeeName").ToString()
            Dim role As String = Session("Role").ToString()
            
            lblUser.Text = name
            lblRole.Text = role.Replace("_", " ")
            
            ' Compute initials for the avatar bubble
            Dim initials As String = name
            If initials.Contains(" ") Then
                Dim parts As String() = initials.Split(" "c)
                If parts.Length > 1 AndAlso parts(0).Length > 0 AndAlso parts(1).Length > 0 Then
                    lblInitials.Text = (parts(0)(0) & parts(1)(0)).ToString().ToUpper()
                ElseIf parts(0).Length > 0 Then
                    lblInitials.Text = parts(0).Substring(0, Math.Min(2, parts(0).Length)).ToUpper()
                End If
            Else
                lblInitials.Text = initials.Substring(0, Math.Min(2, initials.Length)).ToUpper()
            End If
        End If
    End Sub

    Protected Sub lnkLogout_Click(ByVal sender As Object, ByVal e As EventArgs)
        Try
            If Session("EmployeeId") IsNot Nothing Then
                Dim userId As Integer = Convert.ToInt32(Session("EmployeeId"))
                Dim username As String = Session("EmployeeName").ToString()
                Dim sqlLogout As String = "INSERT INTO AuditLogs (UserId, Username, Action, Description, IpAddress, Timestamp) VALUES (" & userId & ", @Username, 'USER_LOGOUT', 'User logged out successfully.', @IP, datetime('now'));"
                Database.ExecuteNonQuery(sqlLogout, New SQLiteParameter("@Username", username), New SQLiteParameter("@IP", Request.UserHostAddress))
            End If
        Catch
        End Try

        Session.Clear()
        Session.Abandon()
        Response.Redirect("~/Login.aspx", False)
        HttpContext.Current.ApplicationInstance.CompleteRequest()
    End Sub

    Public Function GetActiveCSS(ByVal pageName As String) As String
        Dim localPath As String = Request.Url.LocalPath
        If localPath.EndsWith(pageName, StringComparison.OrdinalIgnoreCase) Then
            Return "text-[#F47920] border-[#F47920]"
        End If
        Return "border-transparent hover:text-[#F47920] hover:border-[#F47920]/50"
    End Function
End Class
