<%@ WebHandler Language="VB" Class="ReportsHandler" %>

Imports System
Imports System.Web

Public Class ReportsHandler
    Implements IHttpHandler

    Public Sub ProcessRequest(ByVal context As HttpContext) Implements IHttpHandler.ProcessRequest
        ' Auth check
        If context.Session("EmployeeId") Is Nothing Then
            context.Response.StatusCode = 401
            context.Response.End()
            Return
        End If

        Dim fmt As String = If(context.Request.QueryString("format"), "pdf")
        Dim deptIdStr As String = If(context.Request.QueryString("deptId"), "0")
        Dim deptId As Integer = 0
        Integer.TryParse(deptIdStr, deptId)

        Try
            If fmt = "excel" Then
                Dim excelBytes As Byte() = ReportGenerator.GenerateComplianceExcel(deptId)
                Dim fileName As String = "IOCL_Compliance_Report_" & DateTime.Now.ToString("yyyyMMdd_HHmm") & ".xls"
                context.Response.ContentType = "application/vnd.ms-excel"
                context.Response.AddHeader("Content-Disposition", "attachment; filename=" & fileName)
                context.Response.AddHeader("Content-Length", excelBytes.Length.ToString())
                context.Response.BinaryWrite(excelBytes)
            Else
                Dim pdfBytes As Byte() = ReportGenerator.GenerateCompliancePdf(deptId)
                Dim fileName As String = "IOCL_Compliance_Report_" & DateTime.Now.ToString("yyyyMMdd_HHmm") & ".pdf"
                context.Response.ContentType = "application/pdf"
                context.Response.AddHeader("Content-Disposition", "attachment; filename=" & fileName)
                context.Response.AddHeader("Content-Length", pdfBytes.Length.ToString())
                context.Response.BinaryWrite(pdfBytes)
            End If

            context.Response.Flush()
            context.Response.End()
        Catch ex As Exception
            context.Response.StatusCode = 500
            context.Response.Write("Report generation failed: " & ex.Message)
        End Try
    End Sub

    Public ReadOnly Property IsReusable() As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class
