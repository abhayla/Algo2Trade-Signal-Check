Imports Algo2TradeBLL
Imports System.Threading
Public Class OHL
    Inherits Rule
    Public Sub New(ByVal canceller As CancellationTokenSource, ByVal stockCategory As String, ByVal timeFrame As Integer, ByVal useHA As Boolean, ByVal stockName As String, ByVal fileName As String)
        MyBase.New(canceller, stockCategory, timeFrame, useHA, stockName, fileName)
    End Sub
    Public Overrides Async Function RunAsync(ByVal startDate As Date, ByVal endDate As Date) As Task(Of DataTable)
        Await Task.Delay(0).ConfigureAwait(False)
        Dim ret As New DataTable
        ret.Columns.Add("Date")
        ret.Columns.Add("Trading Symbol")
        ret.Columns.Add("O=H")
        ret.Columns.Add("O=L")

        Dim stockData As StockSelection = New StockSelection(_canceller, _category, _cmn, _fileName)
        AddHandler stockData.Heartbeat, AddressOf OnHeartbeat
        AddHandler stockData.WaitingFor, AddressOf OnWaitingFor
        AddHandler stockData.DocumentRetryStatus, AddressOf OnDocumentRetryStatus
        AddHandler stockData.DocumentDownloadComplete, AddressOf OnDocumentDownloadComplete
        Dim chkDate As Date = startDate
        While chkDate <= endDate
            _canceller.Token.ThrowIfCancellationRequested()
            Dim stockList As List(Of String) = Nothing
            If _instrumentName Is Nothing OrElse _instrumentName = "" Then
                stockList = Await stockData.GetStockList(chkDate).ConfigureAwait(False)
            Else
                stockList = New List(Of String)
                stockList.Add(_instrumentName)
            End If
            _canceller.Token.ThrowIfCancellationRequested()
            If stockList IsNot Nothing AndAlso stockList.Count > 0 Then
                For Each stock In stockList
                    _canceller.Token.ThrowIfCancellationRequested()
                    Dim stockPayload As Dictionary(Of Date, Payload) = Nothing
                    Select Case _category
                        Case "Cash"
                            stockPayload = _cmn.GetRawPayload(Common.DataBaseTable.Intraday_Cash, stock, chkDate.AddDays(-8), chkDate)
                        Case "Currency"
                            stockPayload = _cmn.GetRawPayload(Common.DataBaseTable.Intraday_Currency, stock, chkDate.AddDays(-8), chkDate)
                        Case "Commodity"
                            stockPayload = _cmn.GetRawPayload(Common.DataBaseTable.Intraday_Commodity, stock, chkDate.AddDays(-8), chkDate)
                        Case "Future"
                            stockPayload = _cmn.GetRawPayload(Common.DataBaseTable.Intraday_Futures, stock, chkDate.AddDays(-8), chkDate)
                        Case Else
                            Throw New NotImplementedException
                    End Select
                    _canceller.Token.ThrowIfCancellationRequested()
                    If stockPayload IsNot Nothing AndAlso stockPayload.Count > 0 Then
                        Dim XMinutePayload As Dictionary(Of Date, Payload) = Nothing
                        If _timeFrame > 1 Then
                            Dim exchangeStartTime As Date = New Date(chkDate.Year, chkDate.Month, chkDate.Day, 9, 15, 0)
                            XMinutePayload = Common.ConvertPayloadsToXMinutes(stockPayload, _timeFrame, exchangeStartTime)
                        Else
                            XMinutePayload = stockPayload
                        End If
                        _canceller.Token.ThrowIfCancellationRequested()
                        Dim inputPayload As Dictionary(Of Date, Payload) = Nothing
                        If _useHA Then
                            Indicator.HeikenAshi.ConvertToHeikenAshi(XMinutePayload, inputPayload)
                        Else
                            inputPayload = XMinutePayload
                        End If
                        _canceller.Token.ThrowIfCancellationRequested()
                        Dim currentDayPayload As Dictionary(Of Date, Payload) = Nothing
                        For Each runningPayload In inputPayload.Keys
                            _canceller.Token.ThrowIfCancellationRequested()
                            If runningPayload.Date = chkDate.Date Then
                                If currentDayPayload Is Nothing Then currentDayPayload = New Dictionary(Of Date, Payload)
                                currentDayPayload.Add(runningPayload, inputPayload(runningPayload))
                            End If
                        Next

                        'Main Logic
                        If currentDayPayload IsNot Nothing AndAlso currentDayPayload.Count > 0 Then
                            _canceller.Token.ThrowIfCancellationRequested()
                            Dim eodPayload As Dictionary(Of Date, Payload) = Nothing
                            Select Case _category
                                Case "Cash"
                                    eodPayload = _cmn.GetRawPayload(Common.DataBaseTable.EOD_Cash, stock, chkDate, chkDate)
                                Case "Currency"
                                    eodPayload = _cmn.GetRawPayload(Common.DataBaseTable.EOD_Currency, stock, chkDate, chkDate)
                                Case "Commodity"
                                    eodPayload = _cmn.GetRawPayload(Common.DataBaseTable.EOD_Commodity, stock, chkDate, chkDate)
                                Case "Future"
                                    eodPayload = _cmn.GetRawPayload(Common.DataBaseTable.EOD_Futures, stock, chkDate, chkDate)
                                Case Else
                                    Throw New NotImplementedException
                            End Select
                            _canceller.Token.ThrowIfCancellationRequested()
                            If eodPayload IsNot Nothing AndAlso eodPayload.Count > 0 Then
                                Dim open As Decimal = eodPayload.LastOrDefault.Value.Open
                                Dim highBreak As Boolean = False
                                Dim lowBreak As Boolean = False

                                Dim row As DataRow = ret.NewRow
                                row("Date") = chkDate.ToString("dd-MM-yyyy")
                                row("Trading Symbol") = eodPayload.LastOrDefault.Value.TradingSymbol

                                For Each runningPayload In currentDayPayload
                                    _canceller.Token.ThrowIfCancellationRequested()
                                    If Not highBreak OrElse Not lowBreak Then
                                        If runningPayload.Value.Ticks IsNot Nothing AndAlso runningPayload.Value.Ticks.Count > 0 Then
                                            For Each runningTick In runningPayload.Value.Ticks
                                                If Not highBreak AndAlso runningTick.Open > open Then
                                                    row("O=H") = runningTick.PayloadDate.ToString("HH:mm:ss")
                                                    highBreak = True
                                                End If
                                                If Not lowBreak AndAlso runningTick.Open < open Then
                                                    row("O=L") = runningTick.PayloadDate.ToString("HH:mm:ss")
                                                    lowBreak = True
                                                End If
                                            Next
                                        End If
                                    End If
                                Next
                                ret.Rows.Add(row)
                            End If
                        End If
                    End If
                Next
            End If
            chkDate = chkDate.AddDays(1)
        End While
        Return ret
    End Function
End Class