Imports Algo2TradeBLL
Imports System.Threading
Public Class OneSidedVolume
    Inherits Rule
    Public Sub New(ByVal canceller As CancellationTokenSource, ByVal stockCategory As String, ByVal timeFrame As Integer, ByVal useHA As Boolean, ByVal stockName As String, ByVal fileName As String)
        MyBase.New(canceller, stockCategory, timeFrame, useHA, stockName, fileName)
    End Sub
    Public Overrides Async Function RunAsync(ByVal startDate As Date, ByVal endDate As Date) As Task(Of DataTable)
        Await Task.Delay(0).ConfigureAwait(False)
        Dim ret As New DataTable
        ret.Columns.Add("Date")
        ret.Columns.Add("Instrument")
        ret.Columns.Add("Signal")
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
                            stockPayload = _cmn.GetRawPayload(Common.DataBaseTable.Intraday_Cash, stock, chkDate.AddDays(-7), chkDate)
                        Case "Currency"
                            stockPayload = _cmn.GetRawPayload(Common.DataBaseTable.Intraday_Currency, stock, chkDate.AddDays(-7), chkDate)
                        Case "Commodity"
                            stockPayload = _cmn.GetRawPayload(Common.DataBaseTable.Intraday_Commodity, stock, chkDate.AddDays(-7), chkDate)
                        Case "Future"
                            stockPayload = _cmn.GetRawPayload(Common.DataBaseTable.Intraday_Futures, stock, chkDate.AddDays(-7), chkDate)
                        Case Else
                            Throw New NotImplementedException
                    End Select
                    _canceller.Token.ThrowIfCancellationRequested()
                    If stockPayload IsNot Nothing AndAlso stockPayload.Count > 0 Then
                        OnHeartbeat("Processing Data")
                        Dim XMinutePayload As Dictionary(Of Date, Payload) = Nothing
                        If _timeFrame > 1 Then
                            XMinutePayload = Common.ConvertPayloadsToXMinutes(stockPayload, _timeFrame)
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
                        If currentDayPayload IsNot Nothing AndAlso currentDayPayload.Count > 0 Then
                            For Each runningPayload In currentDayPayload.Keys
                                _canceller.Token.ThrowIfCancellationRequested()
                                If currentDayPayload(runningPayload).PreviousCandlePayload IsNot Nothing Then
                                    'Momentum reversal
                                    'If currentDayPayload(runningPayload).High > currentDayPayload(runningPayload).PreviousCandlePayload.High AndAlso
                                    ' (currentDayPayload(runningPayload).PreviousCandlePayload.CandleWicks.Top / currentDayPayload(runningPayload).PreviousCandlePayload.CandleRange) >= 0.5 Then
                                    '    Dim row As DataRow = ret.NewRow
                                    '    row("Date") = runningPayload
                                    '    row("Instrument") = currentDayPayload(runningPayload).TradingSymbol
                                    '    row("Signal") = 1
                                    '    ret.Rows.Add(row)
                                    'ElseIf currentDayPayload(runningPayload).Low < currentDayPayload(runningPayload).PreviousCandlePayload.Low AndAlso
                                    ' (currentDayPayload(runningPayload).PreviousCandlePayload.CandleWicks.bottom / currentDayPayload(runningPayload).PreviousCandlePayload.CandleRange) >= 0.5 Then
                                    '    Dim row As DataRow = ret.NewRow
                                    '    row("Date") = runningPayload
                                    '    row("Instrument") = currentDayPayload(runningPayload).TradingSymbol
                                    '    row("Signal") = -1
                                    '    ret.Rows.Add(row)
                                    'End If
                                    'Morning star and evening
                                    'If currentDayPayload(runningPayload).DojiCandle AndAlso
                                    '    ((currentDayPayload(runningPayload).Open >= currentDayPayload(runningPayload).PreviousCandlePayload.High AndAlso
                                    '    currentDayPayload(runningPayload).Close >= currentDayPayload(runningPayload).PreviousCandlePayload.High) OrElse
                                    '    (currentDayPayload(runningPayload).Open <= currentDayPayload(runningPayload).PreviousCandlePayload.Low AndAlso
                                    '    currentDayPayload(runningPayload).Close <= currentDayPayload(runningPayload).PreviousCandlePayload.Low)) Then
                                    '    Dim row As DataRow = ret.NewRow
                                    '    row("Date") = runningPayload
                                    '    row("Instrument") = currentDayPayload(runningPayload).TradingSymbol
                                    '    row("Signal") = 1
                                    '    ret.Rows.Add(row)
                                    'End If

                                    'Similar volume but much weaker candle - check this
                                    If Math.Abs((currentDayPayload(runningPayload).Volume / currentDayPayload(runningPayload).PreviousCandlePayload.Volume) - 1) <= 0.2 AndAlso
                                    currentDayPayload(runningPayload).CandleRange / currentDayPayload(runningPayload).PreviousCandlePayload.CandleRange <= 0.3 AndAlso
                                    Not currentDayPayload(runningPayload).DeadCandle AndAlso
                                    Not currentDayPayload(runningPayload).IsMaribazu Then
                                        Dim row As DataRow = ret.NewRow
                                        row("Date") = runningPayload
                                        row("Instrument") = currentDayPayload(runningPayload).TradingSymbol
                                        row("Signal") = 1
                                        ret.Rows.Add(row)
                                    End If


                                    'Similar volume but much stronger candle
                                    'If Math.Abs((currentDayPayload(runningPayload).Volume / currentDayPayload(runningPayload).PreviousCandlePayload.Volume) - 1) <= 0.2 AndAlso
                                    '    currentDayPayload(runningPayload).CandleWicksPercentage.Top <= 0.05 AndAlso
                                    '    currentDayPayload(runningPayload).CandleWicksPercentage.Bottom <= 0.05 AndAlso
                                    '    currentDayPayload(runningPayload).PreviousCandlePayload.DojiCandle AndAlso
                                    '    currentDayPayload(runningPayload).High >= currentDayPayload(runningPayload).PreviousCandlePayload.High AndAlso
                                    '    currentDayPayload(runningPayload).Low <= currentDayPayload(runningPayload).PreviousCandlePayload.Low Then
                                    '    Dim row As DataRow = ret.NewRow
                                    '    row("Date") = runningPayload
                                    '    row("Instrument") = currentDayPayload(runningPayload).TradingSymbol
                                    '    row("Signal") = 1
                                    '    ret.Rows.Add(row)
                                    'End If


                                    'Half volume but double range
                                    'If currentDayPayload(runningPayload).Volume / currentDayPayload(runningPayload).PreviousCandlePayload.Volume <= 0.67 AndAlso
                                    '    currentDayPayload(runningPayload).CandleRange > currentDayPayload(runningPayload).PreviousCandlePayload.CandleRange AndAlso
                                    '    Not currentDayPayload(runningPayload).DojiCandle AndAlso
                                    '    currentDayPayload(runningPayload).PreviousCandlePayload.DojiCandle Then
                                    '    Dim row As DataRow = ret.NewRow
                                    '    row("Date") = runningPayload
                                    '    row("Instrument") = currentDayPayload(runningPayload).TradingSymbol
                                    '    row("Signal") = 1
                                    '    ret.Rows.Add(row)
                                    'End If
                                    'If currentDayPayload(runningPayload).Volume > currentDayPayload(runningPayload).PreviousCandlePayload.Volume * 3 AndAlso
                                    '    currentDayPayload(runningPayload).DojiCandle AndAlso
                                    '    Not currentDayPayload(runningPayload).PreviousCandlePayload.DojiCandle Then
                                    '    Dim row As DataRow = ret.NewRow
                                    '    row("Date") = runningPayload
                                    '    row("Instrument") = currentDayPayload(runningPayload).TradingSymbol
                                    '    row("Signal") = 1
                                    '    ret.Rows.Add(row)
                                    'End If
                                    '((currentDayPayload(runningPayload).CandleColor = Color.Red And currentDayPayload(runningPayload).PreviousCandlePayload.CandleColor = Color.Green) OrElse
                                    '(currentDayPayload(runningPayload).CandleColor = Color.Green And currentDayPayload(runningPayload).PreviousCandlePayload.CandleColor = Color.Red)) Then
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
End Class
