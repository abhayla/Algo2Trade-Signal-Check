Imports Algo2TradeBLL
Imports System.Threading
Public Class VolumeWithCandleRange
    Inherits Rule
    Public Sub New(ByVal canceller As CancellationTokenSource, ByVal stockCategory As String, ByVal timeFrame As Integer, ByVal useHA As Boolean, ByVal stockName As String, ByVal fileName As String)
        MyBase.New(canceller, stockCategory, timeFrame, useHA, stockName, fileName)
    End Sub
    Public Overrides Async Function RunAsync(startDate As Date, endDate As Date) As Task(Of DataTable)
        Await Task.Delay(0).ConfigureAwait(False)
        Dim ret As New DataTable
        ret.Columns.Add("Date")
        ret.Columns.Add("Instrument")
        ret.Columns.Add("Candle Range Change Percentage")
        ret.Columns.Add("Volume Change Percentage")
        ret.Columns.Add("Overall Change Percentage")

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
                            Dim tradingSymbolToken As Tuple(Of String, String) = _cmn.GetCurrentTradingSymbolWithInstrumentToken(Common.DataBaseTable.Intraday_Cash, chkDate, stock)
                            If tradingSymbolToken IsNot Nothing Then
                                stockPayload = _cmn.GetRawPayloadForSpecificTradingSymbol(Common.DataBaseTable.Intraday_Cash, tradingSymbolToken.Item2, chkDate.AddDays(-7), chkDate)
                            End If
                        Case "Currency"
                            Dim tradingSymbolToken As Tuple(Of String, String) = _cmn.GetCurrentTradingSymbolWithInstrumentToken(Common.DataBaseTable.Intraday_Currency, chkDate, stock)
                            If tradingSymbolToken IsNot Nothing Then
                                stockPayload = _cmn.GetRawPayloadForSpecificTradingSymbol(Common.DataBaseTable.Intraday_Currency, tradingSymbolToken.Item2, chkDate.AddDays(-7), chkDate)
                            End If
                        Case "Commodity"
                            Dim tradingSymbolToken As Tuple(Of String, String) = _cmn.GetCurrentTradingSymbolWithInstrumentToken(Common.DataBaseTable.Intraday_Commodity, chkDate, stock)
                            If tradingSymbolToken IsNot Nothing Then
                                stockPayload = _cmn.GetRawPayloadForSpecificTradingSymbol(Common.DataBaseTable.Intraday_Commodity, tradingSymbolToken.Item2, chkDate.AddDays(-7), chkDate)
                            End If
                        Case "Future"
                            Dim tradingSymbolToken As Tuple(Of String, String) = _cmn.GetCurrentTradingSymbolWithInstrumentToken(Common.DataBaseTable.Intraday_Futures, chkDate, stock)
                            If tradingSymbolToken IsNot Nothing Then
                                stockPayload = _cmn.GetRawPayloadForSpecificTradingSymbol(Common.DataBaseTable.Intraday_Futures, tradingSymbolToken.Item2, chkDate.AddDays(-7), chkDate)
                            End If
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
                                    Dim candleRangeChangePer As Decimal = 0
                                    If currentDayPayload(runningPayload).PreviousCandlePayload.CandleRange <> 0 Then
                                        candleRangeChangePer = ((currentDayPayload(runningPayload).CandleRange - currentDayPayload(runningPayload).PreviousCandlePayload.CandleRange) / currentDayPayload(runningPayload).PreviousCandlePayload.CandleRange) * 100
                                    End If
                                    Dim volumeChangePer As Decimal = 0
                                    If currentDayPayload(runningPayload).PreviousCandlePayload.Volume <> 0 Then
                                        volumeChangePer = ((currentDayPayload(runningPayload).Volume - currentDayPayload(runningPayload).PreviousCandlePayload.Volume) / currentDayPayload(runningPayload).PreviousCandlePayload.Volume) * 100
                                    End If
                                    Dim overallChangePercentage As Decimal = Decimal.MinValue
                                    If volumeChangePer < 0 AndAlso candleRangeChangePer > 0 Then
                                        overallChangePercentage = candleRangeChangePer - volumeChangePer
                                    ElseIf volumeChangePer > 0 AndAlso candleRangeChangePer < 0 Then
                                        overallChangePercentage = volumeChangePer - candleRangeChangePer
                                    ElseIf volumeChangePer > 0 AndAlso candleRangeChangePer > 0 Then
                                        overallChangePercentage = Math.Abs(volumeChangePer - candleRangeChangePer)
                                    Else
                                        overallChangePercentage = volumeChangePer + candleRangeChangePer
                                    End If

                                    Dim signal As Integer = 0
                                    If volumeChangePer > candleRangeChangePer Then
                                        signal = 1
                                    Else
                                        signal = -1
                                        If overallChangePercentage < 0 Then signal = 1
                                    End If
                                    Dim row As DataRow = ret.NewRow
                                    row("Date") = runningPayload
                                    row("Instrument") = currentDayPayload(runningPayload).TradingSymbol
                                    row("Candle Range Change Percentage") = Math.Round(candleRangeChangePer, 4)
                                    row("Volume Change Percentage") = Math.Round(volumeChangePer, 4)
                                    row("Overall Change Percentage") = Math.Round(overallChangePercentage * signal, 4)
                                    ret.Rows.Add(row)
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
