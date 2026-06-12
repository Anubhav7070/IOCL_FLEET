Imports System
Imports System.Web

Public Class Global_asax
    Inherits HttpApplication

    Sub Application_Start(ByVal sender As Object, ByVal e As EventArgs)
        Try
            ' Run database creation and data seeding
            Seeder.Seed()
        Catch ex As Exception
            Console.WriteLine("[GLOBAL ERROR] Database seeding failed: " & ex.Message)
        End Try
    End Sub
End Class
