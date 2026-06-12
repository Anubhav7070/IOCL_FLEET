Imports System
Imports System.IO
Imports System.Net
Imports System.Web
Imports System.Web.Hosting
Imports System.Threading
Imports System.Collections.Generic

Public Class HttpListenerWorkerRequest
    Inherits SimpleWorkerRequest

    Private _context As HttpListenerContext
    Private _virtualDir As String
    Private _physicalDir As String
    Private _preloadedBody() As Byte
    Private _preloadedRead As Boolean

    Public Sub New(ByVal context As HttpListenerContext, ByVal virtualDir As String, ByVal physicalDir As String)
        MyBase.New(String.Empty, String.Empty, Nothing)
        _context = context
        _virtualDir = virtualDir
        _physicalDir = physicalDir
        _preloadedRead = False
    End Sub

    Private Sub PreloadBody()
        If _preloadedRead Then Return
        _preloadedRead = True
        If Not _context.Request.HasEntityBody Then Return
        Using ms As New MemoryStream()
            _context.Request.InputStream.CopyTo(ms)
            _preloadedBody = ms.ToArray()
        End Using
    End Sub

    Public Overrides Function IsEntireEntityBodyIsPreloaded() As Boolean
        Return True
    End Function

    Public Overrides Function GetPreloadedEntityBody() As Byte()
        PreloadBody()
        Return _preloadedBody
    End Function

    Public Overrides Sub EndOfRequest()
        Try
            _context.Response.OutputStream.Flush()
            _context.Response.Close()
        Catch
            ' Ignore connection resets
        End Try
    End Sub

    Public Overrides Function GetAppPath() As String
        Return _virtualDir
    End Function

    Public Overrides Function GetAppPathTranslated() As String
        Return _physicalDir
    End Function

    Public Overrides Function GetFilePath() As String
        Dim path As String = _context.Request.Url.LocalPath
        If path.StartsWith(_virtualDir, StringComparison.OrdinalIgnoreCase) Then
            path = path.Substring(_virtualDir.Length)
        End If
        If Not path.StartsWith("/") Then
            path = "/" & path
        End If
        Return path.Replace("/"c, "\"c)
    End Function

    Public Overrides Function GetFilePathTranslated() As String
        Dim path As String = GetFilePath()
        If path.StartsWith("\") Then path = path.Substring(1)
        Return System.IO.Path.Combine(_physicalDir, path)
    End Function

    Public Overrides Function GetHttpVerbName() As String
        Return _context.Request.HttpMethod
    End Function

    Public Overrides Function GetHttpVersion() As String
        Return "HTTP/" & _context.Request.ProtocolVersion.ToString()
    End Function

    Public Overrides Function GetLocalAddress() As String
        Return _context.Request.LocalEndPoint.Address.ToString()
    End Function

    Public Overrides Function GetLocalPort() As Integer
        Return _context.Request.LocalEndPoint.Port
    End Function

    Public Overrides Function GetQueryString() As String
        Dim raw As String = _context.Request.RawUrl
        Dim index As Integer = raw.IndexOf("?")
        If index >= 0 Then
            Return raw.Substring(index + 1)
        Else
            Return String.Empty
        End If
    End Function

    Public Overrides Function GetRawUrl() As String
        Return _context.Request.RawUrl
    End Function

    Public Overrides Function GetRemoteAddress() As String
        Return _context.Request.RemoteEndPoint.Address.ToString()
    End Function

    Public Overrides Function GetRemotePort() As Integer
        Return _context.Request.RemoteEndPoint.Port
    End Function

    Public Overrides Function GetUriPath() As String
        Return _context.Request.Url.LocalPath
    End Function

    Public Overrides Sub SendKnownResponseHeader(ByVal index As Integer, ByVal value As String)
        Dim headerName As String = GetKnownResponseHeaderName(index)
        Try
            _context.Response.Headers(headerName) = value
        Catch
            ' Ignore headers set after transmission
        End Try
    End Sub

    Public Overrides Sub SendUnknownResponseHeader(ByVal name As String, ByVal value As String)
        Try
            _context.Response.Headers(name) = value
        Catch
            ' Ignore headers set after transmission
        End Try
    End Sub

    Public Overrides Sub SendResponseFromFile(ByVal handle As IntPtr, ByVal offset As Long, ByVal length As Long)
        ' Simple default fallback
    End Sub

    Public Overrides Sub SendResponseFromFile(ByVal filename As String, ByVal offset As Long, ByVal length As Long)
        Try
            Using fs As New FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)
                fs.Seek(offset, SeekOrigin.Begin)
                Dim buffer(8192) As Byte
                Dim bytesRead As Integer
                Dim remaining As Long = length
                While remaining > 0
                    bytesRead = fs.Read(buffer, 0, CInt(Math.Min(buffer.Length, remaining)))
                    If bytesRead = 0 Then Exit While
                    _context.Response.OutputStream.Write(buffer, 0, bytesRead)
                    remaining -= bytesRead
                End While
            End Using
        Catch
        End Try
    End Sub

    Public Overrides Sub SendResponseFromMemory(ByVal data() As Byte, ByVal length As Integer)
        Try
            _context.Response.OutputStream.Write(data, 0, length)
        Catch
        End Try
    End Sub

    Public Overrides Sub SendStatus(ByVal statusCode As Integer, ByVal statusDescription As String)
        Try
            _context.Response.StatusCode = statusCode
            _context.Response.StatusDescription = statusDescription
        Catch
        End Try
    End Sub

    Public Overrides Function GetKnownRequestHeader(ByVal index As Integer) As String
        Dim name As String = GetKnownRequestHeaderName(index)
        Return _context.Request.Headers(name)
    End Function

    Public Overrides Function GetUnknownRequestHeader(ByVal name As String) As String
        Return _context.Request.Headers(name)
    End Function

    Public Overrides Function GetUnknownRequestHeaders() As String()()
        Dim headers As New List(Of String())()
        For Each key As String In _context.Request.Headers.AllKeys
            Dim index As Integer = GetKnownRequestHeaderIndex(key)
            If index < 0 Then
                Dim val As String = _context.Request.Headers(key)
                headers.Add(New String() {key, val})
            End If
        Next
        Return headers.ToArray()
    End Function
End Class

Public Class ASPNetHost
    Inherits MarshalByRefObject

    Private _listener As HttpListener
    Private _virtualDir As String
    Private _physicalDir As String
    Private _port As Integer

    Public Sub Start(ByVal port As Integer, ByVal virtualDir As String, ByVal physicalDir As String)
        _port = port
        _virtualDir = virtualDir
        _physicalDir = physicalDir

        _listener = New HttpListener()
        _listener.Prefixes.Add("http://localhost:" & port & "/")
        _listener.Start()

        Console.WriteLine("Server started successfully on http://localhost:" & port & "/")
        Console.WriteLine("Press Ctrl+C to terminate.")

        ThreadPool.QueueUserWorkItem(AddressOf Listen)
    End Sub

    Public Sub StopServer()
        If _listener IsNot Nothing Then
            Try
                _listener.Stop()
            Catch
            End Try
        End If
    End Sub

    Private Sub Listen(ByVal state As Object)
        While _listener.IsListening
            Try
                Dim context As HttpListenerContext = _listener.GetContext()
                ThreadPool.QueueUserWorkItem(Sub() HandleRequest(context))
            Catch
                ' Ignore listener errors when stopping
            End Try
        End While
    End Sub

    Private Sub HandleRequest(ByVal context As HttpListenerContext)
        Try
            Dim urlPath As String = context.Request.Url.LocalPath
            
            ' Serve static files directly to avoid ASP.NET mime mapping issues
            If IsStaticFile(urlPath) Then
                ServeStaticFile(context, urlPath)
                Return
            End If

            ' Otherwise, delegate to the ASP.NET runtime in the same AppDomain
            Dim wr As New HttpListenerWorkerRequest(context, _virtualDir, _physicalDir)
            HttpRuntime.ProcessRequest(wr)
        Catch ex As Exception
            Try
                context.Response.StatusCode = 500
                context.Response.ContentType = "text/html"
                Using writer As New StreamWriter(context.Response.OutputStream)
                    writer.WriteLine("<h1>Internal Server Error</h1>")
                    writer.WriteLine("<pre>" & ex.ToString() & "</pre>")
                End Using
                context.Response.Close()
            Catch
            End Try
        End Try
    End Sub

    Private Function IsStaticFile(ByVal path As String) As Boolean
        Dim ext As String = System.IO.Path.GetExtension(path).ToLower()
        If String.IsNullOrEmpty(ext) Then Return False
        Return ext <> ".aspx" AndAlso ext <> ".ashx" AndAlso ext <> ".asmx"
    End Function

    Private Sub ServeStaticFile(ByVal context As HttpListenerContext, ByVal urlPath As String)
        Dim localPath As String = urlPath.Replace("/"c, "\"c)
        If localPath.StartsWith("\") Then localPath = localPath.Substring(1)
        Dim fullPath As String = Path.Combine(_physicalDir, localPath)

        If Not File.Exists(fullPath) Then
            context.Response.StatusCode = 404
            context.Response.Close()
            Return
        End If

        Try
            Dim ext As String = Path.GetExtension(fullPath).ToLower()
            Dim mime As String = "application/octet-stream"
            Select Case ext
                Case ".css"
                    mime = "text/css"
                Case ".js"
                    mime = "application/javascript"
                Case ".png"
                    mime = "image/png"
                Case ".jpg", ".jpeg"
                    mime = "image/jpeg"
                Case ".gif"
                    mime = "image/gif"
                Case ".pdf"
                    mime = "application/pdf"
                Case ".txt"
                    mime = "text/plain"
                Case ".html", ".htm"
                    mime = "text/html"
            End Select

            context.Response.ContentType = mime
            Dim fileBytes() As Byte = File.ReadAllBytes(fullPath)
            context.Response.ContentLength64 = fileBytes.Length
            context.Response.OutputStream.Write(fileBytes, 0, fileBytes.Length)
            context.Response.Close()
        Catch
            context.Response.StatusCode = 500
            context.Response.Close()
        End Try
    End Sub
End Class
