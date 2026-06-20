<%@ Application Language="VB" %>

<script runat="server">
    Sub Application_Start(ByVal sender As Object, ByVal e As EventArgs)
        ' Initialize database schema & seed data
        Try
            Database.InitializeDatabase()
            Database.EnsureSettingsTable()
            Database.EnsureOtpTokensTable()   ' Migrate existing DBs to add OtpTokens table
            Database.EnsureOwnerDepartmentColumn()
            Database.EnsureVehicleAllocationsTable()
            Database.EnsureDocumentHistoryTable()
            Database.EnsureLastAlertSentColumn()
            Database.EnsureIsDecommissionedColumn()
            Seeder.Seed()
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine("[STARTUP] DB init error: " & ex.Message)
        End Try

        ' Start background 12h compliance scheduler
        Try
            Compliance.StartBackgroundScheduler()
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine("[STARTUP] Scheduler start error: " & ex.Message)
        End Try
    End Sub

    Sub Application_BeginRequest(ByVal sender As Object, ByVal e As EventArgs)
        Dim path As String = Request.AppRelativeCurrentExecutionFilePath
        ' Redirect bare root "/" to Login page
        If path = "~/" OrElse path = "~" Then
            Response.Redirect("~/Login.aspx", True)
        End If
    End Sub

    Sub Application_Error(ByVal sender As Object, ByVal e As EventArgs)
        Dim ex As Exception = Server.GetLastError()
        If ex IsNot Nothing Then
            System.Diagnostics.Debug.WriteLine("[APP ERROR] " & ex.ToString())
        End If
    End Sub
    ' Touch to recycle app pool
</script>
