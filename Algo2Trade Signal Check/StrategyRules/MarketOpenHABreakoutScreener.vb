Imports Algo2TradeBLL
Imports System.Threading

Public Class MarketOpenHABreakoutScreener
    Inherits Rule
    Public Sub New(ByVal canceller As CancellationTokenSource, ByVal stockCategory As String, ByVal timeFrame As Integer, ByVal useHA As Boolean, ByVal stockName As String, ByVal fileName As String)
        MyBase.New(canceller, stockCategory, timeFrame, useHA, stockName, fileName)
    End Sub
    Public Overrides Async Function RunAsync(ByVal startDate As Date, ByVal endDate As Date) As Task(Of DataTable)
        Await Task.Delay(0).ConfigureAwait(False)
        Dim ret As New DataTable
        ret.Columns.Add("Date")
        ret.Columns.Add("Trading Symbol")
        ret.Columns.Add("Open")
        ret.Columns.Add("Low")
        ret.Columns.Add("High")
        ret.Columns.Add("Close")
        ret.Columns.Add("Volume")

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
                Dim counter As Integer = 0
                For Each stock In stockList
                    _canceller.Token.ThrowIfCancellationRequested()
                    counter += 1
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
                    OnHeartbeat(String.Format("Processing for {0} ({1}/{2})", stock, counter, stockList.Count))
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
                            Dim firstCandle As Boolean = True
                            For Each runningPayload In currentDayPayload.Keys
                                _canceller.Token.ThrowIfCancellationRequested()
                                If Not firstCandle Then
                                    If currentDayPayload(runningPayload).PreviousCandlePayload.CandleColor = Color.Green AndAlso
                                        currentDayPayload(runningPayload).Low < currentDayPayload(runningPayload).PreviousCandlePayload.Low Then
                                        Dim row As DataRow = ret.NewRow
                                        row("Date") = currentDayPayload(runningPayload).PayloadDate
                                        row("Trading Symbol") = currentDayPayload(runningPayload).TradingSymbol
                                        row("Open") = currentDayPayload(runningPayload).Open
                                        row("Low") = currentDayPayload(runningPayload).Low
                                        row("High") = currentDayPayload(runningPayload).High
                                        row("Close") = currentDayPayload(runningPayload).Close
                                        row("Volume") = currentDayPayload(runningPayload).Volume
                                        ret.Rows.Add(row)
                                    ElseIf currentDayPayload(runningPayload).PreviousCandlePayload.CandleColor = Color.Red AndAlso
                                        currentDayPayload(runningPayload).High > currentDayPayload(runningPayload).PreviousCandlePayload.High Then
                                        Dim row As DataRow = ret.NewRow
                                        row("Date") = currentDayPayload(runningPayload).PayloadDate
                                        row("Trading Symbol") = currentDayPayload(runningPayload).TradingSymbol
                                        row("Open") = currentDayPayload(runningPayload).Open
                                        row("Low") = currentDayPayload(runningPayload).Low
                                        row("High") = currentDayPayload(runningPayload).High
                                        row("Close") = currentDayPayload(runningPayload).Close
                                        row("Volume") = currentDayPayload(runningPayload).Volume
                                        ret.Rows.Add(row)
                                    End If
                                    Exit For
                                End If
                                firstCandle = False
                            Next
                        End If
                    End If
                Next
            End If
            chkDate = chkDate.AddDays(1)
        End While
        Return ret
    End Function
End Class
