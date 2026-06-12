Imports System
Imports System.Data.SQLite

Public Class DeleteVehicles
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        Try
            Database.ExecuteNonQuery("UPDATE Employee SET Department = '' WHERE EmpNumber = '10000001'")
            
            Dim hash As String = BCrypt.Net.BCrypt.HashPassword("10000001")
            Database.ExecuteNonQuery("UPDATE Authentication SET Password = @Pass WHERE EmployeeId = (SELECT EmployeeId FROM Employee WHERE EmpNumber = '10000001')", New SQLiteParameter("@Pass", hash))
            
            lblMessage.Text = "SuperAdmin updated successfully."
        Catch ex As Exception
            lblMessage.Text = "Error: " & ex.Message
        End Try
    End Sub
End Class
