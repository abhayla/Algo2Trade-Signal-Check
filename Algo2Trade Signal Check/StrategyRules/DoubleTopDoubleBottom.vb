Imports Algo2TradeBLL
Imports System.Threading
Public Class DoubleTopDoubleBottom
    Inherits Rule
    Public Sub New(ByVal canceller As CancellationTokenSource, ByVal stockCategory As Integer, ByVal timeFrame As Integer, ByVal useHA As Boolean, ByVal stockName As String, ByVal fileName As String)
        MyBase.New(canceller, stockCategory, timeFrame, useHA, stockName, fileName)
    End Sub
    Public Overrides Async Function RunAsync(ByVal startDate As Date, ByVal endDate As Date) As Task(Of DataTable)
        Await Task.Delay(0).ConfigureAwait(False)
        Dim ret As New DataTable
        ret.Columns.Add("Date")
        ret.Columns.Add("Trading Symbol")
        ret.Columns.Add("Top/Bottom")
        ret.Columns.Add("At Day HL")

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
                        Case Common.DataBaseTable.Intraday_Cash, Common.DataBaseTable.Intraday_Commodity, Common.DataBaseTable.Intraday_Currency, Common.DataBaseTable.Intraday_Futures
                            stockPayload = _cmn.GetRawPayload(_category, stock, chkDate.AddDays(-8), chkDate)
                        Case Common.DataBaseTable.EOD_Cash, Common.DataBaseTable.EOD_Commodity, Common.DataBaseTable.EOD_Currency, Common.DataBaseTable.EOD_Futures, Common.DataBaseTable.EOD_POSITIONAL
                            stockPayload = _cmn.GetRawPayload(_category, stock, chkDate.AddDays(-200), chkDate)
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
                            Dim fractalHighPayload As Dictionary(Of Date, Decimal) = Nothing
                            Dim fractalLowPayload As Dictionary(Of Date, Decimal) = Nothing
                            Indicator.FractalBands.CalculateFractal(inputPayload, fractalHighPayload, fractalLowPayload)

                            For Each runningPayload In currentDayPayload.Keys
                                _canceller.Token.ThrowIfCancellationRequested()
                                If fractalHighPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate) > fractalHighPayload(runningPayload) Then
                                    Dim currentFractalU As Tuple(Of Date, Date) = GetFractalUFormingCandle(fractalHighPayload, runningPayload, 1)
                                    If currentFractalU IsNot Nothing AndAlso currentFractalU.Item2 = runningPayload Then
                                        Dim previousFractalU As Tuple(Of Date, Date) = GetFractalUFormingCandle(fractalHighPayload, currentFractalU.Item1, 1)
                                        If previousFractalU IsNot Nothing Then
                                            Dim row As DataRow = ret.NewRow
                                            row("Date") = inputPayload(runningPayload).PayloadDate
                                            row("Trading Symbol") = inputPayload(runningPayload).TradingSymbol
                                            row("Top/Bottom") = "Top"
                                            row("At Day HL") = False

                                            ret.Rows.Add(row)
                                        End If
                                    End If
                                End If
                                _canceller.Token.ThrowIfCancellationRequested()
                                If fractalLowPayload(currentDayPayload(runningPayload).PreviousCandlePayload.PayloadDate) < fractalHighPayload(runningPayload) Then
                                    Dim currentFractalU As Tuple(Of Date, Date) = GetFractalUFormingCandle(fractalLowPayload, runningPayload, -1)
                                    If currentFractalU IsNot Nothing AndAlso currentFractalU.Item2 = runningPayload Then
                                        Dim previousFractalU As Tuple(Of Date, Date) = GetFractalUFormingCandle(fractalLowPayload, currentFractalU.Item1, -1)
                                        If previousFractalU IsNot Nothing Then
                                            Dim row As DataRow = ret.NewRow
                                            row("Date") = inputPayload(runningPayload).PayloadDate
                                            row("Trading Symbol") = inputPayload(runningPayload).TradingSymbol
                                            row("Top/Bottom") = "Bottom"
                                            row("At Day HL") = False

                                            ret.Rows.Add(row)
                                        End If
                                    End If
                                End If
                            Next
                        End If
                    End If
                Next
            End If
            chkDate = chkDate.AddDays(1)
        End While
        Return ret
    End Function

    Private Function GetFractalUFormingCandle(ByVal fractalPayload As Dictionary(Of Date, Decimal), ByVal beforeThisTime As Date, ByVal direction As Integer) As Tuple(Of Date, Date)
        Dim ret As Tuple(Of Date, Date) = Nothing
        If fractalPayload IsNot Nothing AndAlso fractalPayload.Count > 0 Then
            Dim checkingPayload As IEnumerable(Of KeyValuePair(Of Date, Decimal)) = fractalPayload.Where(Function(x)
                                                                                                             Return x.Key <= beforeThisTime
                                                                                                         End Function)
            If checkingPayload IsNot Nothing AndAlso checkingPayload.Count > 0 Then
                Dim firstCandleTime As Date = Date.MinValue
                Dim middleCandleTime As Date = Date.MinValue
                Dim lastCandleTime As Date = Date.MinValue
                For Each runningPayload In checkingPayload.OrderByDescending(Function(x)
                                                                                 Return x.Key
                                                                             End Function)
                    If direction > 0 Then
                        If firstCandleTime = Date.MinValue Then
                            firstCandleTime = runningPayload.Key
                        Else
                            If middleCandleTime = Date.MinValue Then
                                If fractalPayload(firstCandleTime) >= runningPayload.Value Then
                                    firstCandleTime = runningPayload.Key
                                Else
                                    middleCandleTime = runningPayload.Key
                                End If
                            Else
                                If fractalPayload(middleCandleTime) = runningPayload.Value Then
                                    middleCandleTime = runningPayload.Key
                                ElseIf fractalPayload(middleCandleTime) < runningPayload.Value Then
                                    firstCandleTime = middleCandleTime
                                    middleCandleTime = runningPayload.Key
                                ElseIf fractalPayload(middleCandleTime) > runningPayload.Value Then
                                    lastCandleTime = runningPayload.Key
                                    ret = New Tuple(Of Date, Date)(lastCandleTime, firstCandleTime)
                                    Exit For
                                End If
                            End If
                        End If
                    ElseIf direction < 0 Then
                        If firstCandleTime = Date.MinValue Then
                            firstCandleTime = runningPayload.Key
                        Else
                            If middleCandleTime = Date.MinValue Then
                                If fractalPayload(firstCandleTime) <= runningPayload.Value Then
                                    firstCandleTime = runningPayload.Key
                                Else
                                    middleCandleTime = runningPayload.Key
                                End If
                            Else
                                If fractalPayload(middleCandleTime) = runningPayload.Value Then
                                    middleCandleTime = runningPayload.Key
                                ElseIf fractalPayload(middleCandleTime) > runningPayload.Value Then
                                    firstCandleTime = middleCandleTime
                                    middleCandleTime = runningPayload.Key
                                ElseIf fractalPayload(middleCandleTime) < runningPayload.Value Then
                                    lastCandleTime = runningPayload.Key
                                    ret = New Tuple(Of Date, Date)(lastCandleTime, firstCandleTime)
                                    Exit For
                                End If
                            End If
                        End If
                    End If
                Next
            End If
        End If
        Return ret
    End Function
End Class
