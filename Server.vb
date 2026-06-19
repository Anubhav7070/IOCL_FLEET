Option Strict On
Imports System
Imports System.IO
Imports System.Web.Hosting
Imports System.Threading

Public Class Server
    Private Shared _host As ASPNetHost
    Private Shared _virtualDir As String = "/"
    Private Shared _physicalDir As String

    Public Shared Sub Main()
        Console.WriteLine("=================================================")
        Console.WriteLine("IOCL Vehicle Management - Local Web Forms Server")
        Console.WriteLine("=================================================")

        _physicalDir = AppDomain.CurrentDomain.BaseDirectory
        Console.WriteLine("Physical Directory: " & _physicalDir)

        ' Create the ASP.NET Application Host
        Try
            Dim hostType As Type = GetType(ASPNetHost)
            _host = DirectCast(ApplicationHost.CreateApplicationHost(hostType, _virtualDir, _physicalDir), ASPNetHost)
            _host.Start(8090, _virtualDir, _physicalDir)
        Catch ex As Exception
            Console.WriteLine("Error creating ASP.NET host: " & ex.Message)
            If ex.InnerException IsNot Nothing Then
                Console.WriteLine("Inner error: " & ex.InnerException.Message)
            End If
            Return
        End Try

        ' Keep main thread alive
        Dim waitHandle As New AutoResetEvent(False)
        AddHandler Console.CancelKeyPress, Sub(sender, e)
                                               e.Cancel = True
                                               waitHandle.Set()
                                           End Sub
        waitHandle.WaitOne()

        Console.WriteLine("Shutting down server...")
        _host.StopServer()
    End Sub
End Class
