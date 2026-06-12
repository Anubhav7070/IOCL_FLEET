Imports System
Imports System.Data
Imports System.Data.SQLite

Public Class ReportsPage
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Session("EmployeeId") Is Nothing Then
            Response.Redirect("~/Login.aspx")
            Return
        End If

        If Not IsPostBack Then
            LoadDepartments()
            LoadStats()
        End If
    End Sub

    Private Sub LoadDepartments()
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT Id, Code, Name FROM Departments ORDER BY Code")
        ddlDept.Items.Clear()
        ddlDept.Items.Add(New System.Web.UI.WebControls.ListItem("All Departments", "0"))
        For Each row As DataRow In dt.Rows
            ddlDept.Items.Add(New System.Web.UI.WebControls.ListItem(row("Code").ToString() & " - " & row("Name").ToString(), row("Id").ToString()))
        Next
    End Sub

    Private Sub LoadStats()
        lblTotal.Text = Database.ExecuteScalar("SELECT COUNT(*) FROM Vehicles").ToString()
        lblTotalLicenses.Text = Database.ExecuteScalar("SELECT COUNT(*) FROM ComplianceRecords").ToString()
        Dim expiring As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT COUNT(*) FROM ComplianceRecords WHERE Status IN ('EXPIRED','HIGH_CRITICAL','MEDIUM_CRITICAL','WARNING')"))
        lblExpiring.Text = expiring.ToString()
        Dim avgScore As Object = Database.ExecuteScalar("SELECT AVG(ComplianceScore) FROM Departments")
        lblAvgScore.Text = If(avgScore Is Nothing OrElse Convert.IsDBNull(avgScore), "0", Math.Round(Convert.ToDouble(avgScore), 1).ToString("0.0"))

        ' Load department breakdown
        Dim deptDt As DataTable = Database.ExecuteDataTable(
            "SELECT d.Id, d.Name, d.Division, d.ComplianceScore, COUNT(DISTINCT v.Id) As VehicleCount " &
            "FROM Departments d LEFT JOIN Vehicles v ON v.DepartmentId = d.Id " &
            "GROUP BY d.Id, d.Name, d.Division, d.ComplianceScore ORDER BY d.ComplianceScore DESC")
        rptDepts.DataSource = deptDt
        rptDepts.DataBind()
    End Sub

    Protected Sub btnDownloadPDF_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim deptId As Integer = Convert.ToInt32(ddlDept.SelectedValue)
        Try
            Dim pdfBytes As Byte() = ReportGenerator.GenerateCompliancePdf(deptId)
            Dim deptName As String = If(deptId = 0, "All", ddlDept.SelectedItem.Text.Split("-"c)(0).Trim())
            Dim fileName As String = "IOCL_Compliance_" & deptName & "_" & DateTime.Now.ToString("yyyyMMdd") & ".pdf"
            Response.Clear()
            Response.ContentType = "application/pdf"
            Response.AddHeader("Content-Disposition", "attachment; filename=" & fileName)
            Response.AddHeader("Content-Length", pdfBytes.Length.ToString())
            Response.BinaryWrite(pdfBytes)
            Response.Flush()
            Response.End()
        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('PDF generation failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    Protected Sub btnDownloadExcel_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim deptId As Integer = Convert.ToInt32(ddlDept.SelectedValue)
        Try
            Dim xlsBytes As Byte() = ReportGenerator.GenerateComplianceExcel(deptId)
            Dim deptName As String = If(deptId = 0, "All", ddlDept.SelectedItem.Text.Split("-"c)(0).Trim())
            Dim fileName As String = "IOCL_Compliance_" & deptName & "_" & DateTime.Now.ToString("yyyyMMdd") & ".xls"
            Response.Clear()
            Response.ContentType = "application/vnd.ms-excel"
            Response.AddHeader("Content-Disposition", "attachment; filename=" & fileName)
            Response.AddHeader("Content-Length", xlsBytes.Length.ToString())
            Response.BinaryWrite(xlsBytes)
            Response.Flush()
            Response.End()
        Catch ex As Exception
            ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Excel generation failed: " & Server.HtmlEncode(ex.Message) & "');", True)
        End Try
    End Sub

    Protected Sub rptDepts_Command(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.CommandEventArgs)
        Dim deptId As Integer = Convert.ToInt32(e.CommandArgument)
        If e.CommandName = "DeptPDF" Then
            Try
                Dim pdfBytes As Byte() = ReportGenerator.GenerateCompliancePdf(deptId)
                Dim fileName As String = "IOCL_Dept_" & deptId & "_Report_" & DateTime.Now.ToString("yyyyMMdd") & ".pdf"
                Response.Clear()
                Response.ContentType = "application/pdf"
                Response.AddHeader("Content-Disposition", "attachment; filename=" & fileName)
                Response.AddHeader("Content-Length", pdfBytes.Length.ToString())
                Response.BinaryWrite(pdfBytes)
                Response.Flush()
                Response.End()
            Catch ex As Exception
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Error: " & Server.HtmlEncode(ex.Message) & "');", True)
            End Try
        ElseIf e.CommandName = "DeptExcel" Then
            Try
                Dim xlsBytes As Byte() = ReportGenerator.GenerateComplianceExcel(deptId)
                Dim fileName As String = "IOCL_Dept_" & deptId & "_Report_" & DateTime.Now.ToString("yyyyMMdd") & ".xls"
                Response.Clear()
                Response.ContentType = "application/vnd.ms-excel"
                Response.AddHeader("Content-Disposition", "attachment; filename=" & fileName)
                Response.AddHeader("Content-Length", xlsBytes.Length.ToString())
                Response.BinaryWrite(xlsBytes)
                Response.Flush()
                Response.End()
            Catch ex As Exception
                ClientScript.RegisterStartupScript(Me.GetType(), "Alert", "alert('Error: " & Server.HtmlEncode(ex.Message) & "');", True)
            End Try
        End If
    End Sub

    Public Function GetScoreClass(ByVal scoreObj As Object) As String
        If scoreObj Is Nothing OrElse Convert.IsDBNull(scoreObj) Then Return "text-slate-600"
        Dim score As Double = Convert.ToDouble(scoreObj)
        If score >= 80 Then Return "text-emerald-600"
        If score >= 60 Then Return "text-yellow-600"
        Return "text-red-600"
    End Function
End Class
