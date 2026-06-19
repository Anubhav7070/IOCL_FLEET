Imports System
Imports System.IO
Imports System.Data
Imports System.Data.SQLite
Imports System.Web
Imports System.Configuration

Public Class Database
    Private Shared _dbPath As String

    Shared Sub New()
        ' Determine physical database directory
        Dim appPath As String = AppDomain.CurrentDomain.BaseDirectory
        Dim appDataPath As String = Path.Combine(appPath, "App_Data")
        If Not Directory.Exists(appDataPath) Then
            Directory.CreateDirectory(appDataPath)
        End If
        ' Set DataDirectory for |DataDirectory| macro
        AppDomain.CurrentDomain.SetData("DataDirectory", appDataPath)
        _dbPath = Path.Combine(appDataPath, "iocl_compliance_forms.db")
    End Sub

    Public Shared Function GetConnectionString() As String
        Return ConfigurationManager.ConnectionStrings("SQLiteDB").ConnectionString
    End Function

    Public Shared Function GetConnection() As SQLiteConnection
        Dim conn As New SQLiteConnection(GetConnectionString())
        conn.Open()
        Return conn
    End Function

    Public Shared Function ExecuteNonQuery(ByVal sql As String, ByVal ParamArray parameters() As SQLiteParameter) As Integer
        Using conn As SQLiteConnection = GetConnection()
            Using cmd As New SQLiteCommand(sql, conn)
                If parameters IsNot Nothing Then
                    cmd.Parameters.AddRange(parameters)
                End If
                Return cmd.ExecuteNonQuery()
            End Using
        End Using
    End Function

    Public Shared Function ExecuteScalar(ByVal sql As String, ByVal ParamArray parameters() As SQLiteParameter) As Object
        Using conn As SQLiteConnection = GetConnection()
            Using cmd As New SQLiteCommand(sql, conn)
                If parameters IsNot Nothing Then
                    cmd.Parameters.AddRange(parameters)
                End If
                Return cmd.ExecuteScalar()
            End Using
        End Using
    End Function

    Public Shared Function ExecuteDataTable(ByVal sql As String, ByVal ParamArray parameters() As SQLiteParameter) As DataTable
        Dim dt As New DataTable()
        Using conn As SQLiteConnection = GetConnection()
            Using cmd As New SQLiteCommand(sql, conn)
                If parameters IsNot Nothing Then
                    cmd.Parameters.AddRange(parameters)
                End If
                Using adapter As New SQLiteDataAdapter(cmd)
                    adapter.Fill(dt)
                End Using
            End Using
        End Using
        Return dt
    End Function

    ' DB schema creation script
    Public Shared Sub InitializeDatabase()
        If File.Exists(_dbPath) Then Return

        WriteHostInfo("Initializing database for the first time...")
        SQLiteConnection.CreateFile(_dbPath)

        Using conn As SQLiteConnection = GetConnection()
            Using trans = conn.BeginTransaction()
                Try
                    ' 1. Employee Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS Employee (EmployeeId INTEGER PRIMARY KEY AUTOINCREMENT, EmpNumber TEXT UNIQUE NOT NULL, EmployeeName TEXT NOT NULL, Department TEXT NOT NULL, Designation TEXT NOT NULL, EmailId TEXT NOT NULL, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP);")

                    ' 2. Authentication Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS Authentication (AuthenticationId INTEGER PRIMARY KEY AUTOINCREMENT, EmployeeId INTEGER NOT NULL, EmployeeName TEXT NOT NULL, Role TEXT CHECK(Role IN ('SuperAdmin', 'DEPT_ADMIN', 'GATEMAN', 'VIEWER', 'Employee')) NOT NULL, Password TEXT NOT NULL, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(EmployeeId) REFERENCES Employee(EmployeeId) ON DELETE CASCADE);")

                    ' 3. Documents Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS Documents (Id INTEGER PRIMARY KEY AUTOINCREMENT, FileName TEXT NOT NULL, FilePath TEXT NOT NULL, FileType TEXT, FileSize INTEGER, UploadedBy INTEGER, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(UploadedBy) REFERENCES Employee(EmployeeId) ON DELETE SET NULL);")

                    ' 4. Vehicles Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS Vehicles (Id INTEGER PRIMARY KEY AUTOINCREMENT, VehicleNumber TEXT UNIQUE NOT NULL, VehicleType TEXT NOT NULL, Department TEXT DEFAULT 'PR - Human Resources', OwnerDepartment TEXT DEFAULT 'PR - Human Resources', DriverName TEXT, VendorName TEXT, OverallStatus TEXT DEFAULT 'Compliant', DocumentId INTEGER, LastUpdatedBy TEXT, LastUpdatedTimestamp TEXT, IsVerified INTEGER DEFAULT 0, VerifiedBy TEXT, EmployeeId INTEGER NOT NULL, OwnershipType TEXT DEFAULT 'Contractual', CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(DocumentId) REFERENCES Documents(Id) ON DELETE SET NULL, FOREIGN KEY(EmployeeId) REFERENCES Employee(EmployeeId) ON DELETE CASCADE);")

                    ' 5. ComplianceRecords Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS ComplianceRecords (Id INTEGER PRIMARY KEY AUTOINCREMENT, VehicleId INTEGER NOT NULL, LicenseType TEXT NOT NULL, LicenseNumber TEXT, IssuingAuthority TEXT, IssueDate TEXT, ExpiryDate TEXT, Status TEXT DEFAULT 'Compliant', DocumentId INTEGER, LastUpdatedBy TEXT, LastUpdatedTimestamp TEXT, IsVerified INTEGER DEFAULT 0, VerifiedBy TEXT, LastAlertSent TEXT, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(VehicleId) REFERENCES Vehicles(Id) ON DELETE CASCADE, FOREIGN KEY(DocumentId) REFERENCES Documents(Id) ON DELETE SET NULL);")

                    ' 6. RenewalHistories Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS RenewalHistories (Id INTEGER PRIMARY KEY AUTOINCREMENT, VehicleId INTEGER NOT NULL, ComplianceRecordId INTEGER NOT NULL, LicenseType TEXT NOT NULL, OldExpiryDate TEXT, NewExpiryDate TEXT, OldDocumentId INTEGER, NewDocumentId INTEGER, RenewedBy INTEGER, RenewedAt TEXT DEFAULT CURRENT_TIMESTAMP, Remarks TEXT, FOREIGN KEY(VehicleId) REFERENCES Vehicles(Id) ON DELETE CASCADE, FOREIGN KEY(ComplianceRecordId) REFERENCES ComplianceRecords(Id) ON DELETE CASCADE, FOREIGN KEY(OldDocumentId) REFERENCES Documents(Id) ON DELETE SET NULL, FOREIGN KEY(NewDocumentId) REFERENCES Documents(Id) ON DELETE SET NULL, FOREIGN KEY(RenewedBy) REFERENCES Employee(EmployeeId) ON DELETE SET NULL);")

                    ' 7. Notifications Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS Notifications (Id INTEGER PRIMARY KEY AUTOINCREMENT, VehicleId INTEGER, Department TEXT, Title TEXT NOT NULL, Message TEXT NOT NULL, Status TEXT DEFAULT 'UNREAD', Type TEXT NOT NULL, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(VehicleId) REFERENCES Vehicles(Id) ON DELETE SET NULL);")

                    ' 8. AuditLogs Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS AuditLogs (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER, Username TEXT, Action TEXT NOT NULL, Description TEXT, OldValue TEXT, NewValue TEXT, IpAddress TEXT, Timestamp TEXT DEFAULT CURRENT_TIMESTAMP, VehicleId INTEGER, Department TEXT, FOREIGN KEY(UserId) REFERENCES Employee(EmployeeId) ON DELETE SET NULL);")

                    ' 9. Reports Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS Reports (Id INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT NOT NULL, Type TEXT NOT NULL, Department TEXT, GeneratedBy INTEGER, FilePath TEXT NOT NULL, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(GeneratedBy) REFERENCES Employee(EmployeeId) ON DELETE SET NULL);")

                    ' 10. OtpTokens Table (Forgot Password + Email Verification)
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS OtpTokens (Id INTEGER PRIMARY KEY AUTOINCREMENT, EmployeeId INTEGER NOT NULL, Token TEXT NOT NULL, TokenType TEXT NOT NULL CHECK(TokenType IN ('FORGOT_PASSWORD','EMAIL_VERIFY')), ExpiresAt TEXT NOT NULL, IsUsed INTEGER NOT NULL DEFAULT 0, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(EmployeeId) REFERENCES Employee(EmployeeId) ON DELETE CASCADE);")

                    ' 11. VehicleAllocations Table
                    ExecuteSql(conn, "CREATE TABLE IF NOT EXISTS VehicleAllocations (Id INTEGER PRIMARY KEY AUTOINCREMENT, VehicleId INTEGER NOT NULL, Department TEXT NOT NULL, StartDate TEXT NOT NULL, EndDate TEXT NOT NULL, AllocatedBy INTEGER, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, Status TEXT DEFAULT 'ACTIVE', FOREIGN KEY(VehicleId) REFERENCES Vehicles(Id) ON DELETE CASCADE, FOREIGN KEY(AllocatedBy) REFERENCES Employee(EmployeeId) ON DELETE SET NULL);")

                    trans.Commit()
                Catch ex As Exception
                    trans.Rollback()
                    Throw ex
                End Try
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' Called at application start to migrate existing databases that pre-date the OtpTokens table.
    ''' Safe to call multiple times — uses CREATE TABLE IF NOT EXISTS.
    ''' </summary>
    Public Shared Sub EnsureOtpTokensTable()
        ExecuteNonQuery("CREATE TABLE IF NOT EXISTS OtpTokens (Id INTEGER PRIMARY KEY AUTOINCREMENT, EmployeeId INTEGER NOT NULL, Token TEXT NOT NULL, TokenType TEXT NOT NULL CHECK(TokenType IN ('FORGOT_PASSWORD','EMAIL_VERIFY')), ExpiresAt TEXT NOT NULL, IsUsed INTEGER NOT NULL DEFAULT 0, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY(EmployeeId) REFERENCES Employee(EmployeeId) ON DELETE CASCADE);")
    End Sub

    Public Shared Sub EnsureOwnershipTypeColumn()
        Try
            ExecuteNonQuery("ALTER TABLE Vehicles ADD COLUMN OwnershipType TEXT DEFAULT 'Contractual';")
        Catch ex As Exception
            ' Column already exists
        End Try
    End Sub

    Public Shared Sub EnsureOwnerDepartmentColumn()
        Try
            ExecuteNonQuery("ALTER TABLE Vehicles ADD COLUMN OwnerDepartment TEXT DEFAULT 'PR - Human Resources';")
        Catch ex As Exception
            ' Column already exists
        End Try
    End Sub

    Public Shared Sub EnsureVehicleAllocationsTable()
        ExecuteNonQuery("CREATE TABLE IF NOT EXISTS VehicleAllocations (Id INTEGER PRIMARY KEY AUTOINCREMENT, VehicleId INTEGER NOT NULL, Department TEXT NOT NULL, StartDate TEXT NOT NULL, EndDate TEXT NOT NULL, AllocatedBy INTEGER, CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP, Status TEXT DEFAULT 'ACTIVE', FOREIGN KEY(VehicleId) REFERENCES Vehicles(Id) ON DELETE CASCADE, FOREIGN KEY(AllocatedBy) REFERENCES Employee(EmployeeId) ON DELETE SET NULL);")
    End Sub

    Public Shared Sub EnsureLastAlertSentColumn()
        Try
            ExecuteNonQuery("ALTER TABLE ComplianceRecords ADD COLUMN LastAlertSent TEXT;")
        Catch ex As Exception
            ' Column already exists
        End Try
    End Sub

    Public Shared Sub EnsureIsDecommissionedColumn()
        Try
            ExecuteNonQuery("ALTER TABLE Vehicles ADD COLUMN IsDecommissioned INTEGER DEFAULT 0;")
        Catch ex As Exception
            ' Column already exists
        End Try
    End Sub

    Public Shared Sub EnsureSettingsTable()
        ExecuteNonQuery("CREATE TABLE IF NOT EXISTS Settings (Key TEXT PRIMARY KEY, Value TEXT);")
    End Sub

    Public Shared Function GetVisitCount() As Integer
        Try
            Dim countObj As Object = ExecuteScalar("SELECT Value FROM Settings WHERE Key='VisitCount'")
            If countObj Is Nothing OrElse Convert.IsDBNull(countObj) Then
                ExecuteNonQuery("INSERT INTO Settings (Key, Value) VALUES ('VisitCount', '1')")
                Return 1
            Else
                Return Convert.ToInt32(countObj)
            End If
        Catch
            Return 0
        End Try
    End Function

    Public Shared Sub IncrementVisitCount()
        Try
            Dim current As Integer = GetVisitCount()
            ExecuteNonQuery("UPDATE Settings SET Value = @Val WHERE Key='VisitCount'", New SQLiteParameter("@Val", (current + 1).ToString()))
        Catch
        End Try
    End Sub

    Private Shared Sub ExecuteSql(ByVal conn As SQLiteConnection, ByVal sql As String)
        Using cmd As New SQLiteCommand(sql, conn)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Private Shared Sub WriteHostInfo(ByVal msg As String)
        Console.WriteLine("[DB INFO] " & msg)
    End Sub
End Class
