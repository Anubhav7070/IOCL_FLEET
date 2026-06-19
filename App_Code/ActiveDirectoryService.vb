Imports System
Imports System.Configuration
Imports System.DirectoryServices.AccountManagement
Imports System.Data
Imports System.Data.SQLite
Imports BCrypt.Net

' =============================================================================
'  ActiveDirectoryService.vb
'  Controls how users are authenticated. Driven by web.config keys:
'
'    ADAuthenticationMode  "Real"  -> validates against Active Directory
'                          "Mock"  -> validates against local SQLite DB (dev/test)
'    ADDomain              Your AD domain name   e.g. "iocl.local"
'    ADLDAPServer          Optional LDAP path    e.g. "ldap://dc01.iocl.local:389"
'                          Leave empty for automatic domain controller discovery.
' =============================================================================
Public Class ActiveDirectoryService

    ''' <summary>
    ''' Main entry point - returns True if credentials are valid, False otherwise.
    ''' </summary>
    Public Shared Function Authenticate(ByVal username As String, ByVal password As String) As Boolean
        Dim mode   As String = ConfigurationManager.AppSettings("ADAuthenticationMode")
        Dim domain As String = ConfigurationManager.AppSettings("ADDomain")

        ' Defensive defaults
        If String.IsNullOrEmpty(mode)   Then mode   = "Mock"
        If String.IsNullOrEmpty(domain) Then domain = "iocl.local"

        If mode.Equals("Real", StringComparison.OrdinalIgnoreCase) Then
            Return AuthenticateAgainstAD(username, password, domain)
        Else
            Return AuthenticateAgainstLocalDB(username, password)
        End If
    End Function

    ' -------------------------------------------------------------------------
    '  REAL AD AUTHENTICATION
    '  Uses System.DirectoryServices.AccountManagement to validate the
    '  user's credentials against the corporate Active Directory.
    '
    '  If ADLDAPServer is set in web.config, that LDAP path is used directly.
    '  Otherwise, .NET auto-discovers the nearest domain controller.
    ' -------------------------------------------------------------------------
    Private Shared Function AuthenticateAgainstAD(ByVal username As String,
                                                   ByVal password As String,
                                                   ByVal domain   As String) As Boolean
        Dim ldapServer As String = ConfigurationManager.AppSettings("ADLDAPServer")

        Try
            Dim pc As PrincipalContext

            If Not String.IsNullOrEmpty(ldapServer) Then
                ' Use explicit LDAP server address from config (recommended for intranet deployments
                ' where DNS-based DC discovery may not be available or reliable)
                Console.WriteLine("[AD AUTH] Connecting via explicit LDAP server: " & ldapServer)
                pc = New PrincipalContext(ContextType.Domain, domain, ldapServer)
            Else
                ' Let .NET auto-discover the domain controller via DNS SRV records
                Console.WriteLine("[AD AUTH] Connecting via auto-discovered DC for domain: " & domain)
                pc = New PrincipalContext(ContextType.Domain, domain)
            End If

            Using pc
                Dim result As Boolean = pc.ValidateCredentials(username, password)
                If result Then
                    Console.WriteLine("[AD AUTH] SUCCESS - User authenticated: " & username)
                Else
                    Console.WriteLine("[AD AUTH] FAILED - Invalid AD credentials for: " & username)
                End If
                Return result
            End Using

        Catch ex As System.DirectoryServices.AccountManagement.PrincipalServerDownException
            ' Domain controller is unreachable - network/firewall issue
            Console.WriteLine("[AD AUTH ERROR] Domain controller unreachable for domain '" & domain & "': " & ex.Message)
            Console.WriteLine("[AD AUTH ERROR] Check: (1) ADDomain in web.config is correct, " &
                              "(2) ADLDAPServer points to a reachable DC, " &
                              "(3) Port 389 (LDAP) or 636 (LDAPS) is open in the firewall.")
            Return False

        Catch ex As System.DirectoryServices.AccountManagement.PrincipalException
            Console.WriteLine("[AD AUTH ERROR] PrincipalContext error: " & ex.Message)
            Return False

        Catch ex As Exception
            Console.WriteLine("[AD AUTH ERROR] Unexpected error during AD validation: " & ex.Message)
            Return False
        End Try
    End Function

    ' -------------------------------------------------------------------------
    '  MOCK / LOCAL DB AUTHENTICATION  (development and testing only)
    '  Validates username + password against the BCrypt-hashed password
    '  stored in the local SQLite Authentication table.
    ' -------------------------------------------------------------------------
    Private Shared Function AuthenticateAgainstLocalDB(ByVal username As String,
                                                        ByVal password As String) As Boolean
        Try
            Dim sql   As String          = ""
            Dim param As SQLiteParameter = Nothing

            Dim isEmpNum As Boolean = (username.Length = 8 AndAlso IsNumericOnly(username))

            If isEmpNum Then
                sql   = "SELECT a.Password FROM Authentication a " &
                        "INNER JOIN Employee e ON a.EmployeeId = e.EmployeeId " &
                        "WHERE e.EmpNumber = @Input LIMIT 1;"
                param = New SQLiteParameter("@Input", username)
            Else
                sql   = "SELECT a.Password FROM Authentication a " &
                        "INNER JOIN Employee e ON a.EmployeeId = e.EmployeeId " &
                        "WHERE LOWER(e.EmployeeName) = LOWER(@Input) " &
                        "   OR LOWER(REPLACE(e.EmployeeName, ' ', '')) = LOWER(@Input) LIMIT 1;"
                param = New SQLiteParameter("@Input", username)
            End If

            Dim dt As DataTable = Database.ExecuteDataTable(sql, param)
            If dt.Rows.Count = 0 Then Return False

            Dim storedHash As String = dt.Rows(0)("Password").ToString()
            Return BCrypt.Net.BCrypt.Verify(password, storedHash)

        Catch ex As Exception
            Console.WriteLine("[AD MOCK ERROR] Local DB authentication error: " & ex.Message)
            Return False
        End Try
    End Function

    ' -------------------------------------------------------------------------
    '  HELPER: test whether a string contains only digits (0-9)
    ' -------------------------------------------------------------------------
    Private Shared Function IsNumericOnly(ByVal val As String) As Boolean
        For Each c As Char In val
            If Not Char.IsDigit(c) Then Return False
        Next
        Return True
    End Function

End Class
