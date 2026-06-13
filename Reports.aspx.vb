Imports System
Imports System.Data
Imports System.Data.SQLite

Public Class ReportsPage
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Session("EmployeeId") Is Nothing Then
            Response.Redirect("~/Login.aspx", False)
            HttpContext.Current.ApplicationInstance.CompleteRequest()
            Return
        End If

        If Not IsPostBack Then
            LoadDepartments()
            LoadStats()
        End If
    End Sub

    Private Sub LoadDepartments()
        Dim dt As DataTable = Database.ExecuteDataTable("SELECT DISTINCT Department FROM Employee WHERE Department IS NOT NULL AND Department <> '' ORDER BY Department")
        ddlDept.Items.Clear()
        ddlDept.Items.Add(New System.Web.UI.WebControls.ListItem("All Departments", "0"))
        For Each row As DataRow In dt.Rows
            ddlDept.Items.Add(New System.Web.UI.WebControls.ListItem(row("Department").ToString(), row("Department").ToString()))
        Next
    End Sub

    Private Sub LoadStats()
        lblTotal.Text = Database.ExecuteScalar("SELECT COUNT(*) FROM Vehicles").ToString()
        lblTotalLicenses.Text = Database.ExecuteScalar("SELECT COUNT(*) FROM ComplianceRecords").ToString()
        Dim expiring As Integer = Convert.ToInt32(Database.ExecuteScalar("SELECT COUNT(*) FROM ComplianceRecords WHERE Status IN ('EXPIRED','HIGH_CRITICAL','MEDIUM_CRITICAL','WARNING')"))
        lblExpiring.Text = expiring.ToString()

        ' Load department breakdown dynamically
        Dim deptDt As DataTable = Database.ExecuteDataTable(
            "SELECT e.Department As Name, " &
            "COALESCE(CAST(SUM(CASE WHEN r.Status = 'ACTIVE' OR r.Status = 'WARNING' THEN 1 ELSE 0 END) * 100.0 / COUNT(r.Id) AS REAL), 100.0) As ComplianceScore, " &
            "COUNT(DISTINCT v.Id) As VehicleCount " &
            "FROM Employee e " &
            "LEFT JOIN Vehicles v ON e.EmployeeId = v.EmployeeId " &
            "LEFT JOIN ComplianceRecords r ON v.Id = r.VehicleId " &
            "WHERE e.Department IS NOT NULL AND e.Department <> '' " &
            "GROUP BY e.Department " &
            "ORDER BY ComplianceScore DESC")

        Dim totalScore As Double = 0
        For Each row As DataRow In deptDt.Rows
            totalScore += Convert.ToDouble(row("ComplianceScore"))
        Next
        Dim avgScore As Double = If(deptDt.Rows.Count > 0, totalScore / deptDt.Rows.Count, 100.0)
        lblAvgScore.Text = Math.Round(avgScore, 1).ToString("0.0")

        ' Set Division as static label
        deptDt.Columns.Add("Division", GetType(String))
        For Each row As DataRow In deptDt.Rows
            row("Division") = "Panipat Refinery"
        Next

        ' Add Id field to avoid breaking Eval("Id") in UI
        deptDt.Columns.Add("Id", GetType(String))
        For Each row As DataRow In deptDt.Rows
            row("Id") = row("Name").ToString()
        Next

        rptDepts.DataSource = deptDt
        rptDepts.DataBind()
    End Sub

    Protected Sub btnDownloadPDF_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim deptVal As String = ddlDept.SelectedValue
        Try
            Dim pdfBytes As Byte() = ReportGenerator.GenerateCompliancePdf(deptVal)
            Dim deptName As String = If(deptVal = "0", "All", ddlDept.SelectedItem.Text.Trim())
            Dim fileName As String = "IOCL_Compliance_" & deptName.Replace(" ", "_") & "_" & DateTime.Now.ToString("yyyyMMdd") & ".pdf"
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
        Dim deptVal As String = ddlDept.SelectedValue
        Try
            Dim xlsBytes As Byte() = ReportGenerator.GenerateComplianceExcel(deptVal)
            Dim deptName As String = If(deptVal = "0", "All", ddlDept.SelectedItem.Text.Trim())
            Dim fileName As String = "IOCL_Compliance_" & deptName.Replace(" ", "_") & "_" & DateTime.Now.ToString("yyyyMMdd") & ".xls"
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
        Dim deptVal As String = e.CommandArgument.ToString()
        If e.CommandName = "DeptPDF" Then
            Try
                Dim pdfBytes As Byte() = ReportGenerator.GenerateCompliancePdf(deptVal)
                Dim fileName As String = "IOCL_Dept_" & deptVal.Replace(" ", "_") & "_Report_" & DateTime.Now.ToString("yyyyMMdd") & ".pdf"
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
                Dim xlsBytes As Byte() = ReportGenerator.GenerateComplianceExcel(deptVal)
                Dim fileName As String = "IOCL_Dept_" & deptVal.Replace(" ", "_") & "_Report_" & DateTime.Now.ToString("yyyyMMdd") & ".xls"
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
