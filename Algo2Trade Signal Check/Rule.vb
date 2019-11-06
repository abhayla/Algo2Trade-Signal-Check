Imports Algo2TradeBLL
Imports System.Threading
Public MustInherit Class Rule
    Implements IDisposable

#Region "Events/Event handlers"
    Public Event DocumentDownloadComplete()
    Public Event DocumentRetryStatus(ByVal currentTry As Integer, ByVal totalTries As Integer)
    Public Event Heartbeat(ByVal msg As String)
    Public Event WaitingFor(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String)
    'The below functions are needed to allow the derived classes to raise the above two events
    Protected Overridable Sub OnDocumentDownloadComplete()
        RaiseEvent DocumentDownloadComplete()
    End Sub
    Protected Overridable Sub OnDocumentRetryStatus(ByVal currentTry As Integer, ByVal totalTries As Integer)
        RaiseEvent DocumentRetryStatus(currentTry, totalTries)
    End Sub
    Protected Overridable Sub OnHeartbeat(ByVal msg As String)
        RaiseEvent Heartbeat(msg)
    End Sub
    Protected Overridable Sub OnWaitingFor(ByVal elapsedSecs As Integer, ByVal totalSecs As Integer, ByVal msg As String)
        RaiseEvent WaitingFor(elapsedSecs, totalSecs, msg)
    End Sub
#End Region

    Protected ReadOnly _canceller As CancellationTokenSource
    Protected ReadOnly _category As String
    Protected ReadOnly _timeFrame As Integer
    Protected ReadOnly _useHA As Boolean
    Protected ReadOnly _instrumentName As String
    Protected ReadOnly _fileName As String
    Protected ReadOnly _cmn As Common
    Public Sub New(ByVal canceller As CancellationTokenSource,
                   ByVal stockCategory As String,
                   ByVal timeFrame As Integer,
                   ByVal useHA As Boolean,
                   ByVal stockName As String,
                   ByVal fileName As String)
        _canceller = canceller
        _category = stockCategory
        _timeFrame = timeFrame
        _useHA = useHA
        _instrumentName = stockName
        _fileName = fileName

        _cmn = New Common(_canceller)
        AddHandler _cmn.Heartbeat, AddressOf OnHeartbeat
        AddHandler _cmn.WaitingFor, AddressOf OnWaitingFor
        AddHandler _cmn.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
        AddHandler _cmn.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
    End Sub

    Public MustOverride Async Function RunAsync(ByVal startDate As Date, ByVal endDate As Date) As Task(Of DataTable)

    Protected Function CalculateBuffer(ByVal price As Decimal, ByVal floorOrCeiling As Utilities.Numbers.RoundOfType) As Decimal
        Dim bufferPrice As Decimal = Nothing
        'Assuming 1% target, we can afford to have buffer as 2.5% of that 1% target
        bufferPrice = Utilities.Numbers.NumberManipulation.ConvertFloorCeling(price * 0.01 * 0.025, 0.05, floorOrCeiling)
        Return bufferPrice
    End Function

#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects).
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
            ' TODO: set large fields to null.
        End If
        disposedValue = True
    End Sub

    ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
    'Protected Overrides Sub Finalize()
    '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        Dispose(True)
        ' TODO: uncomment the following line if Finalize() is overridden above.
        ' GC.SuppressFinalize(Me)
    End Sub
#End Region
End Class
