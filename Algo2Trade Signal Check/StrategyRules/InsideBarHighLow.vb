Imports Algo2TradeBLL
Imports System.Threading
Public Class InsideBarHighLow
    Inherits Rule

    Public Sub New(ByVal canceller As CancellationTokenSource, ByVal stockCategory As String, ByVal timeFrame As Integer, ByVal useHA As Boolean, ByVal stockName As String, ByVal fileName As String)
        MyBase.New(canceller, stockCategory, timeFrame, useHA, stockName, fileName)
    End Sub
    Public Overrides Async Function RunAsync(startDate As Date, endDate As Date) As Task(Of DataTable)
        Await Task.Delay(0).ConfigureAwait(False)
        Dim ret As New DataTable
        ret.Columns.Add("Date")
        ret.Columns.Add("Instrument")
        ret.Columns.Add("High - Low")
        ret.Columns.Add("ATR")

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

                        'Main Logic
                        Dim ATRPayload As Dictionary(Of Date, Decimal) = Nothing
                        Indicator.ATR.CalculateATR(14, inputPayload, ATRPayload)
                        If currentDayPayload IsNot Nothing AndAlso currentDayPayload.Count > 0 Then
                            For Each runningPayload In currentDayPayload.Values
                                _canceller.Token.ThrowIfCancellationRequested()
                                If runningPayload.PreviousCandlePayload.PreviousCandlePayload.PayloadDate.Date = runningPayload.PayloadDate.Date Then
                                    If IsInsideBar(runningPayload) OrElse IsInsideBar(runningPayload.PreviousCandlePayload) Then
                                        Dim highestHigh As Decimal = Math.Max(runningPayload.High, Math.Max(runningPayload.PreviousCandlePayload.High, runningPayload.PreviousCandlePayload.PreviousCandlePayload.High))
                                        Dim lowestLow As Decimal = Math.Min(runningPayload.Low, Math.Min(runningPayload.PreviousCandlePayload.Low, runningPayload.PreviousCandlePayload.PreviousCandlePayload.Low))
                                        If (highestHigh - lowestLow) <= Math.Round(ATRPayload(runningPayload.PayloadDate), 2) Then
                                            Dim row As DataRow = ret.NewRow
                                            row("Date") = runningPayload.PayloadDate
                                            row("Instrument") = runningPayload.TradingSymbol
                                            row("High - Low") = highestHigh - lowestLow
                                            row("ATR") = Math.Round(ATRPayload(runningPayload.PayloadDate), 2)
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

    Private Function IsInsideBar(ByVal candle As Payload) As Boolean
        Dim ret As Boolean = False
        If candle IsNot Nothing AndAlso candle.PreviousCandlePayload IsNot Nothing Then
            If candle.High <= candle.PreviousCandlePayload.High AndAlso candle.Low >= candle.PreviousCandlePayload.Low Then
                ret = True
            End If
        End If
        Return ret
    End Function
End Class
